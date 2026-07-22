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
        public void DrawPath(ColorType color, Vector2Int from, Vector2Int to)
        {
            if (GridModel == null) return;
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
            cell.AddPathColor(color);
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
