using System.Collections.Generic;
using UnityEngine;
using PixelFlow.Models;
using PixelFlow.Data;
using Nexus.Core;
using Nexus.Core.Services;
using System.Threading;
using System.Threading.Tasks;

namespace PixelFlow.Services
{
    public class PathService : IPathService, INexusService
    {
        [Inject] public IGridModel GridModel { get; set; }
        [Inject] public IGameSessionModel GameSessionModel { get; set; }
        [Inject] public IPowerUpService PowerUpService { get; set; }
        [Inject] public ILoggerService LoggerService { get; set; }

        /// <summary>
        /// GDD §4.2: Hedef hücreye belirtilen renkte yol çizilebilir mi kontrolü.
        /// </summary>
        public bool CanDrawPath(ColorType color, Vector2Int from, Vector2Int to)
        {
            if (GridModel == null)
            {
                LoggerService?.LogWarning("[PixelFlow.PathService] CanDrawPath aborted: GridModel is null.");
                return false;
            }
            if (to.x < 0 || to.x >= GridModel.Width || to.y < 0 || to.y >= GridModel.Height)
            {
                LoggerService?.LogWarning($"[PixelFlow.PathService] CanDrawPath: Out of bounds position {to}.");
                return false;
            }
            var cell = GridModel.Grid[to.x, to.y];
            // Node hücresi — source ise geçilebilir, değilse sadece eşleşen renkte
            if (cell.State == CellState.Node)
            {
                bool allowed = cell.Color == ColorType.None || cell.Color == color;
                if (!allowed)
                {
                    LoggerService?.LogWarning($"[PixelFlow.PathService] CanDrawPath blocked: target is Node of color {cell.Color}, trying to connect color {color}.");
                }
                return allowed;
            }
            // Obstacle — geçilemez
            if (cell.State == CellState.Obstacle && 
                cell.ObstacleType != ObstacleType.OneWay && 
                cell.ObstacleType != ObstacleType.Ferry && 
                cell.ObstacleType != ObstacleType.NarrowPass)
            {
                LoggerService?.LogWarning($"[PixelFlow.PathService] CanDrawPath blocked: static obstacle of type {cell.ObstacleType} at {to}.");
                return false;
            }
            // Max 2 farklı renk kontrolü
            if (cell.PathColorCount >= 2 && !cell.HasPathColor(color))
            {
                LoggerService?.LogWarning($"[PixelFlow.PathService] CanDrawPath blocked: target cell {to} already contains 2 paths and doesn't contain color {color}.");
                return false;
            }
            return true;
        }

        /// <summary>
        /// GDD §4.2: Grid üzerine belirtilen renkte yol segmenti çiz.
        /// </summary>
        /// <summary>
        /// GDD §5.2: Rainbow Road segmenti için tüm renkleri hücreye ekler.
        /// </summary>
        private void ApplyRainbowColors(CellData cell)
        {
            var allColors = new ColorType[]
            {
                ColorType.Red, ColorType.Green, ColorType.Blue,
                ColorType.Yellow, ColorType.Purple
            };
            foreach (var c in allColors)
            {
                cell.AddPathColor(c);
            }
            cell.IsRainbowRoad = true;
        }

        public void DrawPath(ColorType color, Vector2Int from, Vector2Int to)
        {
            if (GridModel == null) return;
            if (to.x < 0 || to.x >= GridModel.Width || to.y < 0 || to.y >= GridModel.Height)
            {
                LoggerService?.LogWarning($"[PixelFlow.PathService] DrawPath: Out of bounds position {to}.");
                return;
            }
            if (!GridModel.Paths.ContainsKey(color))
                GridModel.Paths[color] = new List<Vector2Int>();
            var path = GridModel.Paths[color];
            if (path.Count == 0 || path[^1] != from)
            {
                if (!path.Contains(from))
                {
                    LoggerService?.Log($"[PixelFlow.PathService] DrawPath: starting path for color {color} at {from}.");
                    path.Add(from);
                }
            }
            if (!path.Contains(to))
            {
                LoggerService?.Log($"[PixelFlow.PathService] DrawPath: adding segment for color {color} from {from} to {to}.");
                path.Add(to);
            }

            var cell = GridModel.Grid[to.x, to.y];

            // Rainbow Road kontrolü: aktifse tüm renkleri ekle
            bool isRainbow = PowerUpService != null && PowerUpService.TryConsumeRainbowRoadSegment();
            if (isRainbow)
            {
                LoggerService?.Log($"[PixelFlow.PathService] Rainbow Road aktif! {to} hücresine tüm renkler ekleniyor. Kalan rainbow hakkı: {PowerUpService.RainbowRoadUses}");
                ApplyRainbowColors(cell);
            }
            else
            {
                cell.AddPathColor(color);
            }

            if (cell.State == CellState.Empty)
            {
                cell.State = CellState.Path;
                cell.Color = color;
            }
        }

        public List<Vector2Int> GetPathCells(ColorType color)
        {
            if (GridModel == null || !GridModel.Paths.ContainsKey(color))
                return new List<Vector2Int>();
            return new List<Vector2Int>(GridModel.Paths[color]);
        }

        /// <summary>
        /// Clears a single cell's path data for the given color, handles viaduct refund
        /// and state transitions. Extracted to eliminate duplication across Clear/Backtrack/Break.
        /// </summary>
        private void ClearCell(ColorType color, Vector2Int pos)
        {
            var cell = GridModel.Grid[pos.x, pos.y];
            LoggerService?.Log($"[PixelFlow.PathService] Clearing cell {pos} path data for color {color}. Previous PathColors count: {cell.PathColorCount}");
            if (cell.HasPathColor(color))
                cell.RemovePathColor(color);

            // Eğer tüm renkler temizlendiyse Rainbow Road flag'ini de sıfırla
            if (cell.PathColorCount == 0)
                cell.IsRainbowRoad = false;

            if (cell.HasViaduct && cell.PathColorCount < 2)
            {
                cell.HasViaduct = false;
                cell.UnderColor = ColorType.None;
                cell.OverColor = ColorType.None;
                if (cell.State == CellState.Bridge)
                    cell.State = cell.PathColorCount > 0 ? CellState.Path : CellState.Empty;
                GameSessionModel.RefundViaduct();
                LoggerService?.Log($"[PixelFlow.PathService] Viaduct refunded at {pos} because path count dropped below 2. Available: {GameSessionModel.AvailableViaducts}");
            }

            if (cell.PathColorCount == 0)
            {
                if (cell.State == CellState.Path || cell.State == CellState.Bridge)
                {
                    cell.State = CellState.Empty;
                    cell.Color = ColorType.None;
                }
            }
            else if (cell.Color == color && cell.State != CellState.Node)
            {
                cell.Color = cell.FirstPathColor;
            }
        }

        /// <summary>
        /// Clears cells from the path starting at the end until (but not including) the stopIndex.
        /// Used by all three path-clearing operations to avoid code duplication.
        /// </summary>
        private void ClearRange(ColorType color, int stopIndex)
        {
            var path = GridModel.Paths[color];
            LoggerService?.Log($"[PixelFlow.PathService] ClearRange for color {color} from index {path.Count - 1} down to {stopIndex + 1}.");
            for (int i = path.Count - 1; i > stopIndex; i--)
            {
                ClearCell(color, path[i]);
                path.RemoveAt(i);
            }
        }

        /// <summary>
        /// Tüm renklere ait tüm yolları temizler.
        /// Node ve Obstacle hücreleri korunur, Path→Empty döner.
        /// Viyadükler KORUNUR — Clear Jam kullan-at power-up'tır, viaduct iade etmez.
        /// ClearCell kullanmaz çünkü ClearCell viaduct iadesi yapar.
        /// </summary>
        public void ClearAllPaths()
        {
            if (GridModel == null) return;
            LoggerService?.Log("[PixelFlow.PathService] ClearAllPaths: Tüm yollar temizleniyor.");

            // Path kayıtlarını temizle
            GridModel.Paths.Clear();

            // Hücre state'lerini sıfırla (viaduct korunarak)
            for (int x = 0; x < GridModel.Width; x++)
            {
                for (int y = 0; y < GridModel.Height; y++)
                {
                    var cell = GridModel.Grid[x, y];

                    // Node ve Obstacle — korunur
                    if (cell.State == CellState.Node || cell.State == CellState.Obstacle)
                        continue;

                    // Bridge (viaduct) — path renklerini temizle, viaduct state'ini koru
                    if (cell.State == CellState.Bridge)
                    {
                        cell.PathColorsMask = 0;
                        cell.Color = ColorType.None;
                        cell.UnderColor = ColorType.None;
                        cell.OverColor = ColorType.None;
                        cell.IsRainbowRoad = false;
                        // HasViaduct ve State korunur
                        continue;
                    }

                    // Path / Empty — tamamen sıfırla
                    cell.State = CellState.Empty;
                    cell.Color = ColorType.None;
                    cell.PathColorsMask = 0;
                    cell.IsRainbowRoad = false;
                }
            }

            // Model state'lerini sıfırla
            GridModel.ActiveColor.Value = ColorType.None;
            GridModel.LastPosition.Value = new UnityEngine.Vector2Int(-1, -1);
            GridModel.LastCrashPosition.Value = new UnityEngine.Vector2Int(-1, -1);
            GridModel.LockedColors.Clear();

            LoggerService?.Log("[PixelFlow.PathService] ClearAllPaths: Tüm yollar başarıyla temizlendi. Viyadükler korundu.");
        }

        public void ClearPath(ColorType color)
        {
            if (GridModel == null || !GridModel.Paths.ContainsKey(color)) return;
            var path = GridModel.Paths[color];
            LoggerService?.Log($"[PixelFlow.PathService] ClearPath requested for color {color}. Current path length: {path.Count}");
            ClearRange(color, -1);
        }

        public void BacktrackPath(ColorType color, Vector2Int toPos)
        {
            if (GridModel == null || !GridModel.Paths.ContainsKey(color)) return;
            var path = GridModel.Paths[color];
            LoggerService?.Log($"[PixelFlow.PathService] BacktrackPath requested for color {color} to cell {toPos}. Current path length: {path.Count}");
            int idx = path.LastIndexOf(toPos);
            if (idx == -1)
            {
                LoggerService?.LogWarning($"[PixelFlow.PathService] BacktrackPath: cell {toPos} not found in path for color {color}.");
                return;
            }
            ClearRange(color, idx);
        }

        public void BreakPath(ColorType color, Vector2Int breakPos)
        {
            if (GridModel == null || !GridModel.Paths.ContainsKey(color)) return;
            var path = GridModel.Paths[color];
            LoggerService?.Log($"[PixelFlow.PathService] BreakPath requested for color {color} at cell {breakPos}. Current path length: {path.Count}");
            int idx = path.LastIndexOf(breakPos);
            if (idx == -1)
            {
                LoggerService?.LogWarning($"[PixelFlow.PathService] BreakPath: cell {breakPos} not found in path for color {color}.");
                return;
            }
            ClearRange(color, idx);
        }

        public ValueTask InitializeAsync(CancellationToken ct) => default;
        public void OnDispose() { }
    }
}
