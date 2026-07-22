using System.Collections.Generic;
using UnityEngine;
using PixelFlow.Models;
using PixelFlow.Data;
using Nexus.Core;
using Nexus.Core.Services;

namespace PixelFlow.Services
{
    public class GridStateSerializer
    {
        private const string PrefKey = "NT_PuzzleSave_";

        [System.Serializable]
        public class GridSaveData
        {
            public int levelIndex;
            public int width;
            public int height;
            public List<CellSaveData> cells = new List<CellSaveData>();
            public List<PathSaveData> paths = new List<PathSaveData>();
            public int availableViaducts;
            public int maxViaducts;
            public float elapsedTime;
            public int score;
            public int stars;
            public int activeColor;   // (int)ColorType
            public int lastPosX;
            public int lastPosY;
            public List<int> lockedColors = new List<int>();
        }

        [System.Serializable]
        public class CellSaveData
        {
            public int x, y;
            public int state;
            public int color;
            public List<int> pathColors = new List<int>();
            public bool hasViaduct;
            public int underColor;
            public int overColor;
            public int obstacleType;
        }

        [System.Serializable]
        public class PathSaveData
        {
            public int color;
            public List<Vector2Int> positions = new List<Vector2Int>();
        }

        private static ILoggerService Logger => NexusRuntime.Logger;

        public static void Save(IGridModel grid, IGameSessionModel session, ILevelModel level, IPlayerPrefsService prefs = null)
        {
            if (grid == null) return;

            var data = new GridSaveData
            {
                levelIndex = level != null && level.CurrentLevel != null ? level.CurrentLevel.levelIndex : 0,
                width = grid.Width,
                height = grid.Height,
                availableViaducts = session != null ? session.AvailableViaducts : 0,
                maxViaducts = session != null ? session.MaxViaducts : 0,
                elapsedTime = session != null ? session.ElapsedTime : 0f,
                score = session != null ? session.Score : 0,
                stars = session != null ? session.StarsEarned : 0,
                activeColor = grid.ActiveColor != null ? (int)grid.ActiveColor.Value : 0,
                lastPosX = grid.LastPosition != null ? grid.LastPosition.Value.x : -1,
                lastPosY = grid.LastPosition != null ? grid.LastPosition.Value.y : -1,
            };

            if (grid.LockedColors != null)
            {
                foreach (var lc in grid.LockedColors)
                    data.lockedColors.Add((int)lc);
            }

            for (int x = 0; x < grid.Width; x++)
            {
                for (int y = 0; y < grid.Height; y++)
                {
                    var cell = grid.Grid[x, y];
                    var csd = new CellSaveData
                    {
                        x = x, y = y,
                        state = (int)cell.State,
                        color = (int)cell.Color,
                        hasViaduct = cell.HasViaduct,
                        underColor = (int)cell.UnderColor,
                        overColor = (int)cell.OverColor,
                        obstacleType = (int)cell.ObstacleType
                    };
                    foreach (var pc in cell.GetPathColors())
                        csd.pathColors.Add((int)pc);
                    data.cells.Add(csd);
                }
            }

            foreach (var kvp in grid.Paths)
            {
                data.paths.Add(new PathSaveData
                {
                    color = (int)kvp.Key,
                    positions = new List<Vector2Int>(kvp.Value)
                });
            }

            string json = JsonUtility.ToJson(data);
            if (prefs != null)
            {
                prefs.SetString(PrefKey, json);
                prefs.Save();
            }
            else
            {
                PlayerPrefs.SetString(PrefKey, json);
                PlayerPrefs.Save();
            }
            Logger?.Log($"[PixelFlow.GridStateSerializer] 💾 Game state saved: Level {data.levelIndex + 1} ({data.width}x{data.height}, Cells: {data.cells.Count}, Active Paths: {data.paths.Count}, Score: {data.score})");
        }

        /// <summary>
        /// Kayıtlı oyun state'ini yükler. Kayıt yoksa null döner.
        /// Çağıran, GridModel/GameSessionModel'a uygulamakla yükümlüdür.
        /// </summary>
        public static GridSaveData Load(IPlayerPrefsService prefs = null)
        {
            bool hasKey = prefs != null ? prefs.HasKey(PrefKey) : PlayerPrefs.HasKey(PrefKey);
            if (!hasKey) return null;
            string json = prefs != null ? prefs.GetString(PrefKey, "") : PlayerPrefs.GetString(PrefKey, "");
            if (string.IsNullOrEmpty(json)) return null;
            try
            {
                var loaded = JsonUtility.FromJson<GridSaveData>(json);
                if (loaded != null)
                {
                    Logger?.Log($"[PixelFlow.GridStateSerializer] 📖 Save file loaded successfully: Level {loaded.levelIndex + 1} ({loaded.width}x{loaded.height}, Cells: {loaded.cells.Count}, Paths: {loaded.paths.Count})");
                }
                return loaded;
            }
            catch (System.Exception ex)
            {
                Logger?.LogWarning($"[PixelFlow.GridStateSerializer] Failed to parse save JSON: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Save verisini GridModel'e uygular. Önce Initialize(w,h) yapar,
        /// sonra tüm cell'leri ve path'leri geri yazar.
        /// </summary>
        public static void ApplyToGrid(GridSaveData data, IGridModel grid)
        {
            if (data == null || grid == null) return;
            grid.Initialize(data.width, data.height);

            for (int i = 0; i < data.cells.Count; i++)
            {
                var csd = data.cells[i];
                if (csd.x < 0 || csd.x >= data.width || csd.y < 0 || csd.y >= data.height) continue;
                var cell = grid.Grid[csd.x, csd.y];
                cell.State = (CellState)csd.state;
                cell.Color = (ColorType)csd.color;
                cell.HasViaduct = csd.hasViaduct;
                cell.UnderColor = (ColorType)csd.underColor;
                cell.OverColor = (ColorType)csd.overColor;
                cell.ClearPathColors();
                foreach (var pc in csd.pathColors)
                    cell.AddPathColor((ColorType)pc);
                cell.ObstacleType = (ObstacleType)csd.obstacleType;
            }

            grid.Paths.Clear();
            foreach (var psd in data.paths)
            {
                var list = new List<Vector2Int>(psd.positions);
                grid.Paths[(ColorType)psd.color] = list;
            }

            grid.LockedColors.Clear();
            foreach (var lc in data.lockedColors)
                grid.LockedColors.Add((ColorType)lc);

            grid.ActiveColor.Value = (ColorType)data.activeColor;
            grid.LastPosition.Value = new Vector2Int(data.lastPosX, data.lastPosY);

            // ---- Rebuild PathColorsMask from grid.Paths ----
            // Eski save formatlarında pathColors listesi boş olabilir; ayrıca
            // cell-level ve path-level veri arasında tutarlılık sağlar.
            for (int x = 0; x < grid.Width; x++)
                for (int y = 0; y < grid.Height; y++)
                    grid.Grid[x, y].ClearPathColors();

            foreach (var kvp in grid.Paths)
            {
                byte bit = (byte)(1 << (int)kvp.Key);
                foreach (var pos in kvp.Value)
                {
                    if (pos.x >= 0 && pos.x < grid.Width && pos.y >= 0 && pos.y < grid.Height)
                        grid.Grid[pos.x, pos.y].PathColorsMask |= bit;
                }
            }
        }

        public static bool IsSaveDataValidForLevel(GridSaveData save, LevelData level)
        {
            if (save == null || level == null) return false;
            if (save.width != level.width || save.height != level.height) return false;

            var levelNodes = new Dictionary<Vector2Int, ColorType>();
            foreach (var n in level.initialNodes)
            {
                levelNodes[n.position] = n.color;
            }

            int matchedNodes = 0;

            foreach (var csd in save.cells)
            {
                var pos = new Vector2Int(csd.x, csd.y);
                bool hasLevelNode = levelNodes.TryGetValue(pos, out var expectedColor);
                bool isSaveNode = (CellState)csd.state == CellState.Node;

                if (isSaveNode)
                {
                    if (!hasLevelNode || expectedColor != (ColorType)csd.color)
                    {
                        return false;
                    }
                    matchedNodes++;
                }
                else
                {
                    if (hasLevelNode)
                    {
                        return false;
                    }
                }
            }

            return matchedNodes == level.initialNodes.Count;
        }

        public static void EnsureInitialNodesOnGrid(LevelData level, IGridModel grid)
        {
            if (level == null || level.initialNodes == null || grid == null || grid.Grid == null) return;

            foreach (var node in level.initialNodes)
            {
                if (node.position.x >= 0 && node.position.x < grid.Width &&
                    node.position.y >= 0 && node.position.y < grid.Height)
                {
                    var cell = grid.Grid[node.position.x, node.position.y];
                    if (cell.State == CellState.Empty)
                    {
                        cell.State = CellState.Node;
                        cell.Color = node.color;
                    }
                    if (!cell.HasPathColor(node.color))
                    {
                        cell.AddPathColor(node.color);
                    }
                }
            }
        }

        public static bool HasSavedGame(IPlayerPrefsService prefs = null)
        {
            return prefs != null ? prefs.HasKey(PrefKey) : PlayerPrefs.HasKey(PrefKey);
        }

        public static void ClearSave(IPlayerPrefsService prefs = null)
        {
            if (prefs != null)
            {
                prefs.DeleteKey(PrefKey);
                prefs.Save();
            }
            else
            {
                PlayerPrefs.DeleteKey(PrefKey);
                PlayerPrefs.Save();
            }
        }
    }
}
