using System.Collections.Generic;
using UnityEngine;
using PixelFlow.Models;
using PixelFlow.Data;
using Nexus.Core;

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
        }

        [System.Serializable]
        public class PathSaveData
        {
            public int color;
            public List<Vector2Int> positions = new List<Vector2Int>();
        }

        public static void Save(IGridModel grid, IGameSessionModel session, ILevelModel level)
        {
            var data = new GridSaveData
            {
                levelIndex = level.CurrentLevel != null ? level.CurrentLevel.levelIndex : 0,
                width = grid.Width,
                height = grid.Height,
                availableViaducts = session.AvailableViaducts,
                maxViaducts = session.MaxViaducts,
                elapsedTime = session.ElapsedTime,
                score = session.Score,
                stars = session.StarsEarned,
                activeColor = (int)grid.ActiveColor,
                lastPosX = grid.LastPosition.x,
                lastPosY = grid.LastPosition.y,
            };

            foreach (var lc in grid.LockedColors)
                data.lockedColors.Add((int)lc);

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
                        overColor = (int)cell.OverColor
                    };
                    foreach (var pc in cell.PathColors)
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
            PlayerPrefs.SetString(PrefKey, json);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Kayıtlı oyun state'ini yükler. Kayıt yoksa null döner.
        /// Çağıran, GridModel/GameSessionModel'a uygulamakla yükümlüdür.
        /// </summary>
        public static GridSaveData Load()
        {
            if (!PlayerPrefs.HasKey(PrefKey)) return null;
            string json = PlayerPrefs.GetString(PrefKey, "");
            if (string.IsNullOrEmpty(json)) return null;
            try
            {
                return JsonUtility.FromJson<GridSaveData>(json);
            }
            catch
            {
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
                cell.PathColors.Clear();
                foreach (var pc in csd.pathColors)
                    cell.PathColors.Add((ColorType)pc);
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

            grid.ActiveColor = (ColorType)data.activeColor;
            grid.LastPosition = new Vector2Int(data.lastPosX, data.lastPosY);
        }

        public static bool HasSavedGame()
        {
            return PlayerPrefs.HasKey(PrefKey);
        }

        public static void ClearSave()
        {
            PlayerPrefs.DeleteKey(PrefKey);
            PlayerPrefs.Save();
        }
    }
}
