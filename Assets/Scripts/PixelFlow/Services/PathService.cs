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

        public void ClearPath(ColorType color)
        {
            if (GridModel == null || !GridModel.Paths.ContainsKey(color)) return;
            var path = GridModel.Paths[color];
            foreach (var pos in path)
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
            path.Clear();
        }

        public void BacktrackPath(ColorType color, Vector2Int toPos)
        {
            if (GridModel == null || !GridModel.Paths.ContainsKey(color)) return;
            var path = GridModel.Paths[color];
            int idx = path.LastIndexOf(toPos);
            if (idx == -1) return;

            for (int i = path.Count - 1; i > idx; i--)
            {
                var pos = path[i];
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
                path.RemoveAt(i);
            }
        }

        public void BreakPath(ColorType color, Vector2Int breakPos)
        {
            if (GridModel == null || !GridModel.Paths.ContainsKey(color)) return;
            var path = GridModel.Paths[color];
            int idx = path.LastIndexOf(breakPos);
            if (idx == -1) return;

            for (int i = path.Count - 1; i > idx; i--)
            {
                var pos = path[i];
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
                path.RemoveAt(i);
            }
        }

        public ValueTask InitializeAsync(CancellationToken ct) => default;
        public void OnDispose() { }
    }
}
