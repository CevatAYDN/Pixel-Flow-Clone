using System.Collections.Generic;
using UnityEngine;
using PixelFlow.Models;
using PixelFlow.Data;
using PixelFlow.Signals;
using PixelFlow.Views;
using Nexus.Core;
using Nexus.Core.Services;

namespace PixelFlow.Services
{
    /// <summary>
    /// Araç hareket mantığını yönetir: path takibi, spline interpolasyonu,
    /// tren/vagon dönüşümleri, narrow pass geçişleri, flow score güncellemeleri.
    /// VehicleSimulator'dan ayrıştırıldı (Single Responsibility).
    ///
    /// Performans: Catmull-Rom spline kontrol noktaları önbelleğe alınır.
    /// Her (ColorType, segmentIndex) çifti için 4 kontrol noktası bir kere
    /// hesaplanır ve path değişene kadar önbellekte kalır.
    /// </summary>
    public class VehicleMovementService
    {
        private readonly IGridModel _gridModel;
        private readonly IGameStateModel _gameStateModel;
        private readonly IGameSessionModel _gameSessionModel;
        private readonly ISignalBus _signalBus;
        private readonly IAudioService _audioService;
        private readonly IObstacleService _obstacleService;
        private readonly GameConfig _config;

        // ─── Spline Segment Cache ───────────────────────────────────────────
        // Her (ColorType, segmentIndex) için 4 Catmull-Rom kontrol noktası.
        // Cache, path değiştiğinde (vehicles cleared) InvalidateSplineCache()
        // ile temizlenir.
        private struct SegmentCacheEntry
        {
            public Vector3 P0;
            public Vector3 P1;
            public Vector3 P2;
            public Vector3 P3;
        }

        private Dictionary<(ColorType color, int segment), SegmentCacheEntry> _splineCache
            = new Dictionary<(ColorType, int), SegmentCacheEntry>();

        /// <summary>
        /// Spline önbelleğini temizler. Path değiştiğinde (çizim, undo/redo,
        /// level yükleme) VehicleSimulator tarafından çağrılır.
        /// </summary>
        public void InvalidateSplineCache()
        {
            Nexus.Core.Services.NexusLog.Info("VehicleMovementService", "InvalidateSplineCache", "?", "Spline cache fully invalidated.");
            _splineCache.Clear();
        }

        /// <summary>
        /// Belirtilen renk için tüm cache girdilerini temizler.
        /// Sadece tek bir renk değiştiğinde kullanılır.
        /// </summary>
        // Reusable list for batch key removal — GC alloc'u önlemek için field'a çıkarıldı
        private readonly List<(ColorType color, int segment)> _keysToRemove = new List<(ColorType, int)>();

        public void InvalidateSplineCache(ColorType color)
        {
            if (_splineCache.Count == 0) return;
            
            _keysToRemove.Clear();
            foreach (var key in _splineCache.Keys)
            {
                if (key.color == color)
                    _keysToRemove.Add(key);
            }
            for (int i = 0; i < _keysToRemove.Count; i++)
                _splineCache.Remove(_keysToRemove[i]);
            _keysToRemove.Clear();
            Nexus.Core.Services.NexusLog.Info("VehicleMovementService", "InvalidateSplineCache", "?", "Spline cache invalidated for color: " + color);
        }

        public VehicleMovementService(
            IGridModel gridModel,
            IGameStateModel gameStateModel,
            IGameSessionModel gameSessionModel,
            ISignalBus signalBus,
            IAudioService audioService,
            IObstacleService obstacleService,
            GameConfig config)
        {
            _gridModel = gridModel;
            _gameStateModel = gameStateModel;
            _gameSessionModel = gameSessionModel;
            _signalBus = signalBus;
            _audioService = audioService;
            _obstacleService = obstacleService;
            _config = config;
        }

        private float ConfigMaxProgressPerFrame => _config != null ? _config.MaxProgressPerFrame : 0.25f;

        public void UpdateMovement(List<VehicleInstance> activeVehicles, float deltaTime)
        {
            bool isPlaying = _gameStateModel.CurrentState == GameState.Playing;
            float baseAlpha = isPlaying ? (0.45f + Mathf.Sin(Time.time * 6f) * 0.25f) : 1f;

            // ── Ghost alpha: GPU tabanlı — 1 Shader.SetGlobalFloat, 0 SetPropertyBlock ──
            // Eskiden: 160+ SetPropertyBlock/frame (60fps × 8 renderer × 20 araç)
            // Şimdi: 1 Shader.SetGlobalFloat/frame
            // VehicleGhost.shader, _PixelFlow_GhostAlpha global değerini okuyup alpha'ya uygular
            // SRP Batcher ile tam uyumlu: per-instance _Color (spawn'da set edilir) değişmez
            Shader.SetGlobalFloat("_PixelFlow_GhostAlpha", baseAlpha);

            for (int i = activeVehicles.Count - 1; i >= 0; i--)
            {
                var v = activeVehicles[i];
                if (v.Visual == null)
                {
                    activeVehicles.RemoveAt(i);
                    continue;
                }

                // Narrow pass enter/leave tracking
                UpdateNarrowPassTracking(v);

                // Ghost alpha GPU'da VehicleGhost.shader tarafından yönetilir
                // Per-frame SetPropertyBlock tamamen kalktı — tüm araç renderer'ları
                // SRP Batcher ile tek batch'te toplanabilir

                // Distance progression
                v.TotalDistance += Mathf.Min(v.Speed * deltaTime, ConfigMaxProgressPerFrame);
                float maxAllowedDist = (v.Style == VehicleStyle.Train) ? (v.Path.Count - 1) + 0.85f : (v.Path.Count - 1);

                if (v.TotalDistance >= maxAllowedDist)
                {
                    CompleteVehicleMovement(v, activeVehicles, i);
                    continue;
                }

                // Calculate position along path
                float locoDist = Mathf.Min(v.TotalDistance, v.Path.Count - 1);
                float zOffset = GetZOffset(v.Path[Mathf.Clamp(Mathf.FloorToInt(locoDist), 0, v.Path.Count - 1)], v.Color);

                Vector3 basePos = EvaluatePathPosition(v.Path, locoDist, v.Color, zOffset);
                Vector3 tangent = EvaluatePathTangent(v.Path, locoDist, v.Color);
                v.CurrentPosition = basePos;

                if (v.Style == VehicleStyle.Train)
                {
                    UpdateTrainTransform(v, basePos, tangent, zOffset, deltaTime, locoDist);
                }
                else
                {
                    UpdateCarTransform(v, basePos, tangent, locoDist, deltaTime);
                }
            }
        }

        private void UpdateNarrowPassTracking(VehicleInstance v)
        {
            if (_obstacleService == null) return;

            if (v.SegmentIndex > 0 && v.SegmentIndex - 1 < v.Path.Count)
            {
                Vector2Int prevCell = v.Path[v.SegmentIndex - 1];
                if (_obstacleService.IsNarrowPass(prevCell))
                {
                    _obstacleService.OnVehicleLeftNarrowPass(prevCell, v.Color);
                    Nexus.Core.Services.NexusLog.Info("VehicleMovementService", "UpdateNarrowPassTracking", "?", "Vehicle of color " + v.Color + " left narrow pass at " + prevCell);
                }
            }
            if (v.SegmentIndex < v.Path.Count && v.Progress < 0.1f)
            {
                Vector2Int curCell = v.Path[v.SegmentIndex];
                if (_obstacleService.IsNarrowPass(curCell))
                {
                    _obstacleService.OnVehicleEnteredNarrowPass(curCell, v.Color);
                    Nexus.Core.Services.NexusLog.Info("VehicleMovementService", "UpdateNarrowPassTracking", "?", "Vehicle of color " + v.Color + " entered narrow pass at " + curCell);
                }
            }
        }

        private void CompleteVehicleMovement(VehicleInstance v, List<VehicleInstance> activeVehicles, int index)
        {
            // Narrow pass cleanup at path end
            if (_obstacleService != null && v.Path.Count > 0)
            {
                Vector2Int endCell = v.Path[v.Path.Count - 1];
                if (_obstacleService.IsNarrowPass(endCell))
                    _obstacleService.OnVehicleLeftNarrowPass(endCell, v.Color);
            }

            // Flow score increment in simulation mode
            if (_gameStateModel.CurrentState == GameState.Simulating && _gameSessionModel != null)
            {
                _gameSessionModel.IncrementFlowScore();
                _signalBus?.Fire(new FlowScoreUpdatedSignal
                {
                    CurrentScore = _gameSessionModel.CurrentFlowScore,
                    TargetScore = _gameSessionModel.TargetFlowScore
                });
                _audioService?.PlaySfx(SfxType.UIClick);
            }

            // ÖNCE parçaları pool'a geri ver, SONRA root'u destroy et
            // SafeDestroy sadece Destroy çağırır, pool'a geri vermez → pool depletion → GC spike
            VehicleVisualFactory.RecycleVehicle(v.Visual);
            Nexus.Core.Services.NexusLog.Info("VehicleMovementService", "CompleteVehicleMovement", "?", "Vehicle of color " + v.Color + " completed its path. Recycled visual.");
            activeVehicles.RemoveAt(index);
        }

        private void UpdateTrainTransform(VehicleInstance v, Vector3 basePos, Vector3 tangent, float zOffset, float deltaTime, float locoDist)
        {
            v.Visual.transform.localPosition = Vector3.zero;
            float bobbing = Mathf.Sin(Time.time * 8f + v.GetHashCode()) * 0.01f;

            // 1. Locomotive
            Vector3 locoPos = basePos;
            locoPos.z = zOffset + bobbing;
            if (v.LocoTransform != null)
            {
                v.LocoTransform.localPosition = locoPos;
                if (tangent.sqrMagnitude > 0.001f)
                {
                    float targetAngle = Mathf.Atan2(tangent.y, tangent.x) * Mathf.Rad2Deg;
                    v.LocoTransform.localRotation = Quaternion.Slerp(
                        v.LocoTransform.localRotation,
                        Quaternion.Euler(0f, 0f, targetAngle),
                        deltaTime * 25f);
                }
            }

            // 2. Wagon 1
            Vector3 w1Pos;
            bool w1Active = UpdateCarriageTransform(v.Wagon1Transform, v.Path, v.TotalDistance - 0.42f, v.Color, zOffset, deltaTime, out w1Pos);

            // 3. Coupler 1
            if (v.Coupler1Transform != null)
            {
                if (v.LocoTransform != null && w1Active)
                {
                    if (!v.Coupler1Transform.gameObject.activeSelf)
                        v.Coupler1Transform.gameObject.SetActive(true);
                    Vector3 c1Pos = (locoPos + w1Pos) * 0.5f;
                    v.Coupler1Transform.localPosition = c1Pos;
                    Vector3 dir1 = locoPos - w1Pos;
                    if (dir1.sqrMagnitude > 0.001f)
                    {
                        float c1Angle = Mathf.Atan2(dir1.y, dir1.x) * Mathf.Rad2Deg;
                        v.Coupler1Transform.localRotation = Quaternion.Euler(0f, 0f, c1Angle);
                    }
                }
                else
                {
                    if (v.Coupler1Transform.gameObject.activeSelf)
                        v.Coupler1Transform.gameObject.SetActive(false);
                }
            }

            // 4. Wagon 2
            Vector3 w2Pos;
            bool w2Active = UpdateCarriageTransform(v.Wagon2Transform, v.Path, v.TotalDistance - 0.84f, v.Color, zOffset, deltaTime, out w2Pos);

            // 5. Coupler 2
            if (v.Coupler2Transform != null)
            {
                if (w1Active && w2Active)
                {
                    if (!v.Coupler2Transform.gameObject.activeSelf)
                        v.Coupler2Transform.gameObject.SetActive(true);
                    Vector3 c2Pos = (w1Pos + w2Pos) * 0.5f;
                    v.Coupler2Transform.localPosition = c2Pos;
                    Vector3 dir2 = w1Pos - w2Pos;
                    if (dir2.sqrMagnitude > 0.001f)
                    {
                        float c2Angle = Mathf.Atan2(dir2.y, dir2.x) * Mathf.Rad2Deg;
                        v.Coupler2Transform.localRotation = Quaternion.Euler(0f, 0f, c2Angle);
                    }
                }
                else
                {
                    if (v.Coupler2Transform.gameObject.activeSelf)
                        v.Coupler2Transform.gameObject.SetActive(false);
                }
            }
        }

        private void UpdateCarTransform(VehicleInstance v, Vector3 basePos, Vector3 tangent, float locoDist, float deltaTime)
        {
            Vector3 nextTangent = EvaluatePathTangent(v.Path, Mathf.Min(locoDist + 0.1f, v.Path.Count - 1), v.Color);
            float baseAngle = tangent.sqrMagnitude > 0.001f ? Mathf.Atan2(tangent.y, tangent.x) * Mathf.Rad2Deg : 0f;
            float nextAngle = nextTangent.sqrMagnitude > 0.001f ? Mathf.Atan2(nextTangent.y, nextTangent.x) * Mathf.Rad2Deg : baseAngle;
            float deltaAngle = Mathf.DeltaAngle(baseAngle, nextAngle);

            float bobbing = Mathf.Sin(Time.time * 12f + v.GetHashCode()) * 0.02f;
            Vector3 finalPos = v.CurrentPosition;
            finalPos.z += bobbing;
            v.Visual.transform.localPosition = finalPos;

            if (tangent.sqrMagnitude > 0.001f)
            {
                float angle = Mathf.Atan2(tangent.y, tangent.x) * Mathf.Rad2Deg;
                float rollBank = Mathf.Clamp(-deltaAngle * 0.3f, -15f, 15f);
                v.Visual.transform.rotation = Quaternion.Slerp(
                    v.Visual.transform.rotation,
                    Quaternion.Euler(0f, 0f, angle + rollBank),
                    deltaTime * 25f);
            }
        }

        private bool UpdateCarriageTransform(Transform tForm, List<Vector2Int> path, float targetDist, ColorType color, float zOffset, float deltaTime, out Vector3 worldPos)
        {
            worldPos = Vector3.zero;
            if (tForm == null) return false;

            if (targetDist < 0f)
            {
                if (tForm.gameObject.activeSelf)
                    tForm.gameObject.SetActive(false);
                return false;
            }

            if (!tForm.gameObject.activeSelf)
                tForm.gameObject.SetActive(true);
            float clampedDist = Mathf.Min(targetDist, path.Count - 1);
            Vector3 pos = EvaluatePathPosition(path, clampedDist, color, zOffset);
            Vector3 tangent = EvaluatePathTangent(path, clampedDist, color);
            worldPos = pos;

            tForm.localPosition = pos;
            if (tangent.sqrMagnitude > 0.001f)
            {
                float targetAngle = Mathf.Atan2(tangent.y, tangent.x) * Mathf.Rad2Deg;
                tForm.localRotation = Quaternion.Slerp(
                    tForm.localRotation,
                    Quaternion.Euler(0f, 0f, targetAngle),
                    deltaTime * 25f);
            }

            return true;
        }

        /// <summary>
        /// Spline segmenti için 4 kontrol noktasını cache'den alır veya hesaplar.
        /// </summary>
        private SegmentCacheEntry GetOrComputeSegment(int segmentIndex, List<Vector2Int> path, ColorType color)
        {
            var key = (color, segmentIndex);
            if (_splineCache.TryGetValue(key, out var cached))
                return cached;

            // Cache miss — hesapla ve önbelleğe ekle
            var entry = new SegmentCacheEntry
            {
                P0 = GetSplineControlPoint(path, segmentIndex - 1, color),
                P1 = GetSplineControlPoint(path, segmentIndex, color),
                P2 = GetSplineControlPoint(path, segmentIndex + 1, color),
                P3 = GetSplineControlPoint(path, segmentIndex + 2, color)
            };
            _splineCache[key] = entry;
            return entry;
        }

        private Vector3 EvaluatePathPosition(List<Vector2Int> path, float pathDistance, ColorType color, float zOffset)
        {
            if (path == null || path.Count == 0) return Vector3.zero;
            if (path.Count == 1) return new Vector3(path[0].x, path[0].y, zOffset);

            float clampedDist = Mathf.Clamp(pathDistance, 0f, path.Count - 1);
            int seg = Mathf.FloorToInt(clampedDist);
            if (seg >= path.Count - 1)
            {
                seg = path.Count - 2;
                clampedDist = path.Count - 1;
            }
            float t = clampedDist - seg;

            var cache = GetOrComputeSegment(seg, path, color);

            Vector3 pos = CatmullRom(cache.P0, cache.P1, cache.P2, cache.P3, t);
            pos.z = zOffset;
            return pos;
        }

        private Vector3 EvaluatePathTangent(List<Vector2Int> path, float pathDistance, ColorType color)
        {
            if (path == null || path.Count < 2) return Vector3.right;

            float clampedDist = Mathf.Clamp(pathDistance, 0f, path.Count - 1);
            int seg = Mathf.FloorToInt(clampedDist);
            if (seg >= path.Count - 1)
            {
                seg = path.Count - 2;
                clampedDist = path.Count - 1;
            }
            float t = clampedDist - seg;

            var cache = GetOrComputeSegment(seg, path, color);

            return CatmullRomTangent(cache.P0, cache.P1, cache.P2, cache.P3, t);
        }

        private float GetZOffset(Vector2Int gridPos, ColorType color)
        {
            if (gridPos.x >= 0 && gridPos.x < _gridModel.Width && gridPos.y >= 0 && gridPos.y < _gridModel.Height)
            {
                var cell = _gridModel.Grid[gridPos.x, gridPos.y];
                if (cell.HasViaduct && cell.OverColor == color)
                    return -0.4f; // Over: Yükseltilmiş yol (GDD §4.4)
                if (cell.HasViaduct && cell.UnderColor == color)
                    return -0.1f; // Under: Alçaltılmış yol (GDD §4.4)
            }
            return -0.2f; // Normal yol
        }

        private Vector3 GetSplineControlPoint(List<Vector2Int> path, int index, ColorType color)
        {
            if (path == null || path.Count == 0) return Vector3.zero;
            if (path.Count == 1)
            {
                Vector2Int p = path[0];
                return new Vector3(p.x, p.y, GetZOffset(p, color));
            }

            if (index < 0)
            {
                Vector2Int pos0 = path[0];
                Vector2Int pos1 = path[1];
                Vector3 v0 = new Vector3(pos0.x, pos0.y, GetZOffset(pos0, color));
                Vector3 v1 = new Vector3(pos1.x, pos1.y, GetZOffset(pos1, color));
                return v0 + (v0 - v1) * Mathf.Abs(index);
            }
            if (index >= path.Count)
            {
                Vector2Int posEnd = path[path.Count - 1];
                Vector2Int posPrev = path[path.Count - 2];
                Vector3 vEnd = new Vector3(posEnd.x, posEnd.y, GetZOffset(posEnd, color));
                Vector3 vPrev = new Vector3(posPrev.x, posPrev.y, GetZOffset(posPrev, color));
                return vEnd + (vEnd - vPrev) * (index - path.Count + 1);
            }

            Vector2Int gridPos = path[index];
            return new Vector3(gridPos.x, gridPos.y, GetZOffset(gridPos, color));
        }

        private static Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {
            float t2 = t * t;
            float t3 = t2 * t;
            return 0.5f * ((2f * p1) +
                           (-p0 + p2) * t +
                           (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 +
                           (-p0 + 3f * p1 - 3f * p2 + p3) * t3);
        }

        private static Vector3 CatmullRomTangent(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {
            float t2 = t * t;
            return 0.5f * ((-p0 + p2) +
                           2f * (2f * p0 - 5f * p1 + 4f * p2 - p3) * t +
                           3f * (-p0 + 3f * p1 - 3f * p2 + p3) * t2);
        }

        private static void SafeDestroy(UnityEngine.Object obj)
        {
            if (obj == null) return;
            if (Application.isPlaying)
                UnityEngine.Object.Destroy(obj);
            else
                UnityEngine.Object.DestroyImmediate(obj);
        }
    }
}
