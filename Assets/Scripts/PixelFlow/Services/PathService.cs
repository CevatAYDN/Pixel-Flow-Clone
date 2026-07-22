using System.Collections.Generic;
using UnityEngine;
using PixelFlow.Models;
using PixelFlow.Data;
using Nexus.Core;
using System.Threading;
using System.Threading.Tasks;

namespace PixelFlow.Services
{
    public class PathService : IPathService, INexusService
    {
        [Inject] public IGridModel GridModel { get; set; }
        [Inject] public IGameSessionModel GameSessionModel { get; set; }

        /// <summary>
        /// GDD §4.2: Hedef hücreye belirtilen renkte yol çizilebilir mi kontrolü.
        /// </summary>
        public bool CanDrawPath(ColorType color, Vector2Int from, Vector2Int to)
        {
            if (GridModel == null) return false;
            if (to.x < 0 || to.x >= GridModel.Width || to.y < 0 || to.y >= GridModel.Height)
                return false;
            var cell = GridModel.Grid[to.x, to.y];
            // Node hücresi — source ise geçilebilir, değilse sadece eşleşen renkte
            if (cell.State == CellState.Node)
            {
                return cell.Color == ColorType.None || cell.Color == color;
            }
            // Obstacle — geçilemez
            if (cell.State == CellState.Obstacle && 
                cell.ObstacleType != ObstacleType.OneWay && 
                cell.ObstacleType != ObstacleType.Ferry && 
                cell.ObstacleType != ObstacleType.NarrowPass)
                return false;
            // Max 2 farklı renk kontrolü
            if (cell.PathColorCount >= 2 && !cell.HasPathColor(color))
                return false;
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
                    path.Add(from);
            }
            if (!path.Contains(to))
                path.Add(to);

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
            for (int i = path.Count - 1; i > stopIndex; i--)
            {
                ClearCell(color, path[i]);
                path.RemoveAt(i);
            }
        }

        public void ClearPath(ColorType color)
        {
            if (GridModel == null || !GridModel.Paths.ContainsKey(color)) return;
            ClearRange(color, -1);
        }

        public void BacktrackPath(ColorType color, Vector2Int toPos)
        {
            if (GridModel == null || !GridModel.Paths.ContainsKey(color)) return;
            var path = GridModel.Paths[color];
            int idx = path.LastIndexOf(toPos);
            if (idx == -1) return;
            ClearRange(color, idx);
        }

        public void BreakPath(ColorType color, Vector2Int breakPos)
        {
            if (GridModel == null || !GridModel.Paths.ContainsKey(color)) return;
            var path = GridModel.Paths[color];
            int idx = path.LastIndexOf(breakPos);
            if (idx == -1) return;
            ClearRange(color, idx);
        }

        public ValueTask InitializeAsync(CancellationToken ct) => default;
        public void OnDispose() { }
    }
}
