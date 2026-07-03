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
        private class GridSaveData
        {
            public int levelIndex;
            public int width;
            public int height;
            public List<CellSaveData> cells = new List<CellSaveData>();
            public List<PathSaveData> paths = new List<PathSaveData>();
            public int availableViaducts;
            public int maxViaducts;
            public float elapsedTime;
        }

        [System.Serializable]
        private class CellSaveData
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
        private class PathSaveData
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
                elapsedTime = session.ElapsedTime
            };

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
