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
                if (cell.PathColors.Contains(color))
                    cell.PathColors.Remove(color);

                if (cell.HasViaduct && cell.PathColors.Count < 2)
                {
                    cell.HasViaduct = false;
                    cell.UnderColor = ColorType.None;
                    cell.OverColor = ColorType.None;
                    if (cell.State == CellState.Bridge)
                        cell.State = cell.PathColors.Count > 0 ? CellState.Path : CellState.Empty;
                    GameSessionModel.RefundViaduct();
                }

                if (cell.PathColors.Count == 0)
                {
                    if (cell.State == CellState.Path || cell.State == CellState.Bridge)
                    {
                        cell.State = CellState.Empty;
                        cell.Color = ColorType.None;
                    }
                }
                else if (cell.Color == color && cell.State != CellState.Node)
                {
                    cell.Color = cell.PathColors[0];
                }
            }
            path.Clear();
        }

        public void BacktrackPath(ColorType color, Vector2Int toPos)
        {
            if (GridModel == null || !GridModel.Paths.ContainsKey(color)) return;
            var path = GridModel.Paths[color];
            int idx = path.IndexOf(toPos);
            if (idx == -1) return;

            for (int i = path.Count - 1; i > idx; i--)
            {
                var pos = path[i];
                var cell = GridModel.Grid[pos.x, pos.y];
                if (cell.PathColors.Contains(color))
                    cell.PathColors.Remove(color);

                if (cell.HasViaduct && cell.PathColors.Count < 2)
                {
                    cell.HasViaduct = false;
                    cell.UnderColor = ColorType.None;
                    cell.OverColor = ColorType.None;
                    if (cell.State == CellState.Bridge)
                        cell.State = cell.PathColors.Count > 0 ? CellState.Path : CellState.Empty;
                    GameSessionModel.RefundViaduct();
                }

                if (cell.PathColors.Count == 0)
                {
                    if (cell.State == CellState.Path || cell.State == CellState.Bridge)
                    {
                        cell.State = CellState.Empty;
                        cell.Color = ColorType.None;
                    }
                }
                else if (cell.Color == color && cell.State != CellState.Node)
                {
                    cell.Color = cell.PathColors[0];
                }
                path.RemoveAt(i);
            }
        }

        public void BreakPath(ColorType color, Vector2Int breakPos)
        {
            BacktrackPath(color, breakPos);
        }

        public ValueTask InitializeAsync(CancellationToken ct) => default;
        public void OnDispose() { }
    }
}
