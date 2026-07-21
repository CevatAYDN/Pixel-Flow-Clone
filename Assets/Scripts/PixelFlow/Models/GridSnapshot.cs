using System.Collections.Generic;
using PixelFlow.Data;
using UnityEngine;

namespace PixelFlow.Models
{
    /// <summary>
    /// GridModel'in tam state'ini + GameSessionModel state'ini immutable olarak yakalar.
    /// </summary>
    public readonly struct GridSnapshot
    {
        public int Width { get; }
        public int Height { get; }
        public CellState[,] CellStates { get; }
        public ColorType[,] CellColors { get; }
        public byte[,] CellPathColorMasks { get; }
        public bool[,] CellHasViaduct { get; }
        public ColorType[,] CellUnderColor { get; }
        public ColorType[,] CellOverColor { get; }
        public ObstacleType[,] CellObstacleTypes { get; }
        public IReadOnlyDictionary<ColorType, IReadOnlyList<Vector2Int>> Paths { get; }
        public HashSet<ColorType> LockedColors { get; }
        public ColorType ActiveColor { get; }
        public Vector2Int LastPosition { get; }

        // Session state fields — captured/restored alongside grid state
        public int SessionScore { get; }
        public int SessionAvailableViaducts { get; }
        public int SessionMaxViaducts { get; }
        public float SessionElapsedTime { get; }
        public int SessionStarsEarned { get; }
        public int SessionCurrentFlowScore { get; }
        public int SessionTargetFlowScore { get; }
        public bool SessionHasUsedCrisisUndo { get; }
        public int SessionRetryCount { get; }
        public bool HasSessionState { get; }

        private GridSnapshot(
            int width, int height,
            CellState[,] cellStates, ColorType[,] cellColors,
            byte[,] cellPathColorMasks, bool[,] cellHasViaduct,
            ColorType[,] cellUnderColor, ColorType[,] cellOverColor,
            ObstacleType[,] cellObstacleTypes,
            IReadOnlyDictionary<ColorType, IReadOnlyList<Vector2Int>> paths,
            HashSet<ColorType> lockedColors,
            ColorType activeColor,
            Vector2Int lastPosition,
            // Session state
            int sessionScore, int sessionAvailableViaducts, int sessionMaxViaducts,
            float sessionElapsedTime, int sessionStarsEarned,
            int sessionCurrentFlowScore, int sessionTargetFlowScore,
            bool sessionHasUsedCrisisUndo, int sessionRetryCount,
            bool hasSessionState)
        {
            Width = width;
            Height = height;
            CellStates = cellStates;
            CellColors = cellColors;
            CellPathColorMasks = cellPathColorMasks;
            CellHasViaduct = cellHasViaduct;
            CellUnderColor = cellUnderColor;
            CellOverColor = cellOverColor;
            CellObstacleTypes = cellObstacleTypes;
            Paths = paths;
            LockedColors = lockedColors;
            ActiveColor = activeColor;
            LastPosition = lastPosition;

            SessionScore = sessionScore;
            SessionAvailableViaducts = sessionAvailableViaducts;
            SessionMaxViaducts = sessionMaxViaducts;
            SessionElapsedTime = sessionElapsedTime;
            SessionStarsEarned = sessionStarsEarned;
            SessionCurrentFlowScore = sessionCurrentFlowScore;
            SessionTargetFlowScore = sessionTargetFlowScore;
            SessionHasUsedCrisisUndo = sessionHasUsedCrisisUndo;
            SessionRetryCount = sessionRetryCount;
            HasSessionState = hasSessionState;
        }

        /// <summary>
        /// GridModel'den anlık snapshot alır. Deep-copy yapar.
        /// </summary>
        public static GridSnapshot Capture(IGridModel grid)
        {
            return Capture(grid, null);
        }

        /// <summary>
        /// GridModel + GameSessionModel'den anlık snapshot alır.
        /// </summary>
        public static GridSnapshot Capture(IGridModel grid, IGameSessionModel session)
        {
            int w = grid.Width;
            int h = grid.Height;

            var cellStates = new CellState[w, h];
            var cellColors = new ColorType[w, h];
            var cellPathColorMasks = new byte[w, h];
            var cellHasViaduct = new bool[w, h];
            var cellUnderColors = new ColorType[w, h];
            var cellOverColors = new ColorType[w, h];
            var cellObstacleTypes = new ObstacleType[w, h];

            for (int x = 0; x < w; x++)
            {
                for (int y = 0; y < h; y++)
                {
                    var cell = grid.Grid[x, y];
                    cellStates[x, y] = cell.State;
                    cellColors[x, y] = cell.Color;
                    cellPathColorMasks[x, y] = cell.PathColorsMask;
                    cellHasViaduct[x, y] = cell.HasViaduct;
                    cellUnderColors[x, y] = cell.UnderColor;
                    cellOverColors[x, y] = cell.OverColor;
                    cellObstacleTypes[x, y] = cell.ObstacleType;
                }
            }

            var paths = new Dictionary<ColorType, IReadOnlyList<Vector2Int>>();
            foreach (var kvp in grid.Paths)
            {
                paths[kvp.Key] = new List<Vector2Int>(kvp.Value);
            }

            var locked = new HashSet<ColorType>(grid.LockedColors);

            bool hasSession = session != null;

            return new GridSnapshot(
                w, h,
                cellStates, cellColors,
                cellPathColorMasks, cellHasViaduct, cellUnderColors, cellOverColors,
                cellObstacleTypes,
                paths,
                locked,
                grid.ActiveColor.Value,
                grid.LastPosition.Value,
                // Session state
                hasSession ? session.Score : 0,
                hasSession ? session.AvailableViaducts : 0,
                hasSession ? session.MaxViaducts : 0,
                hasSession ? session.ElapsedTime : 0f,
                hasSession ? session.StarsEarned : 0,
                hasSession ? session.CurrentFlowScore : 0,
                hasSession ? session.TargetFlowScore : 0,
                hasSession ? session.HasUsedCrisisUndo : false,
                hasSession ? session.RetryCount : 0,
                hasSession
            );
        }

        /// <summary>
        /// Bu snapshot'ı GridModel'e uygular. Tüm state'i overwrite eder.
        /// </summary>
        public void ApplyTo(IGridModel grid)
        {
            grid.Initialize(Width, Height);

            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    var cell = grid.Grid[x, y];
                    cell.State = CellStates[x, y];
                    cell.Color = CellColors[x, y];
                    cell.PathColorsMask = CellPathColorMasks[x, y];
                    cell.HasViaduct = CellHasViaduct[x, y];
                    cell.UnderColor = CellUnderColor[x, y];
                    cell.OverColor = CellOverColor[x, y];
                    cell.ObstacleType = CellObstacleTypes[x, y];
                }
            }

            grid.Paths.Clear();
            foreach (var kvp in Paths)
            {
                grid.Paths[kvp.Key] = new List<Vector2Int>(kvp.Value);
            }

            grid.LockedColors.Clear();
            foreach (var c in LockedColors)
            {
                grid.LockedColors.Add(c);
            }

            grid.ActiveColor.Value = ActiveColor;
            grid.LastPosition.Value = LastPosition;
        }

        /// <summary>
        /// Session state'ini GameSessionModel'e geri yükler.
        /// </summary>
        public void ApplySessionTo(IGameSessionModel session)
        {
            if (!HasSessionState || session == null) return;
            session.ApplySave(SessionAvailableViaducts, SessionMaxViaducts, SessionElapsedTime, SessionScore, SessionStarsEarned, 0);
        }
    }
}
