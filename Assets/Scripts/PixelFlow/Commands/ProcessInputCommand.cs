using Nexus.Core;
using PixelFlow.Models;
using PixelFlow.Signals;
using PixelFlow.Data;
using UnityEngine;
using System.Collections.Generic;

namespace PixelFlow.Commands
{
    public class ProcessInputCommand : ICommand<InputInteractionSignal>
    {
        [Inject] public IGridModel GridModel { get; set; }
        [Inject] public ISignalBus SignalBus { get; set; }
        [Inject] public ISoundModel SoundModel { get; set; }

        private static ColorType _activeColor = ColorType.None;
        private static Vector2Int _lastPos = new Vector2Int(-1, -1);

        public void Execute(InputInteractionSignal signal)
        {
            if (signal.Type == InputType.PointerDown)
            {
                if (signal.GridPosition.x < 0 || signal.GridPosition.y < 0 || signal.GridPosition.x >= GridModel.Width || signal.GridPosition.y >= GridModel.Height)
                    return;

                var cell = GridModel.Grid[signal.GridPosition.x, signal.GridPosition.y];
                if (cell.Color != ColorType.None)
                {
                    _activeColor = cell.Color;
                    _lastPos = signal.GridPosition;
                    
                    if (!GridModel.Paths.ContainsKey(_activeColor))
                        GridModel.Paths[_activeColor] = new List<Vector2Int>();
                    else
                    {
                        if (cell.State == CellState.Node)
                        {
                            ClearPath(_activeColor);
                            GridModel.Paths[_activeColor].Add(signal.GridPosition);
                        }
                        else if (cell.State == CellState.Path || cell.State == CellState.Bridge)
                        {
                            BacktrackPath(_activeColor, signal.GridPosition);
                        }
                    }
                    GridModel.UpdateGrid();
                    SignalBus.Fire(new GridUpdatedSignal());
                }
            }
            else if (signal.Type == InputType.Drag)
            {
                if (_activeColor == ColorType.None) return;
                
                if (signal.GridPosition.x < 0 || signal.GridPosition.y < 0 || signal.GridPosition.x >= GridModel.Width || signal.GridPosition.y >= GridModel.Height)
                    return;

                if (Mathf.Abs(signal.GridPosition.x - _lastPos.x) + Mathf.Abs(signal.GridPosition.y - _lastPos.y) != 1)
                    return;

                var currentCell = GridModel.Grid[signal.GridPosition.x, signal.GridPosition.y];
                var path = GridModel.Paths[_activeColor];

                if (path.Count > 1 && path[path.Count - 2] == signal.GridPosition)
                {
                    var removedPos = path[path.Count - 1];
                    path.RemoveAt(path.Count - 1);
                    var removedCell = GridModel.Grid[removedPos.x, removedPos.y];
                    if (removedCell.State != CellState.Node)
                    {
                        removedCell.Color = ColorType.None;
                        removedCell.State = CellState.Empty;
                    }
                    _lastPos = signal.GridPosition;
                    GridModel.UpdateGrid();
                    SignalBus.Fire(new GridUpdatedSignal());
                    return;
                }

                if (currentCell.State == CellState.Empty)
                {
                    currentCell.Color = _activeColor;
                    currentCell.State = CellState.Path;
                    path.Add(signal.GridPosition);
                    _lastPos = signal.GridPosition;
                    GridModel.UpdateGrid();
                    SignalBus.Fire(new GridUpdatedSignal());
                    SoundModel.PlayDrawSound(path.Count);
                }
                else if (currentCell.State == CellState.Node && currentCell.Color == _activeColor)
                {
                    path.Add(signal.GridPosition);
                    _lastPos = signal.GridPosition;
                    _activeColor = ColorType.None; 
                    GridModel.UpdateGrid();
                    SignalBus.Fire(new GridUpdatedSignal());
                    SignalBus.Fire(new CheckWinConditionSignal());
                }
                else if (currentCell.State == CellState.Bridge)
                {
                    path.Add(signal.GridPosition);
                    _lastPos = signal.GridPosition;
                    GridModel.UpdateGrid();
                    SignalBus.Fire(new GridUpdatedSignal());
                }
                else if (currentCell.Color != ColorType.None && currentCell.Color != _activeColor && currentCell.State == CellState.Path)
                {
                    BreakPath(currentCell.Color, signal.GridPosition);
                    currentCell.Color = _activeColor;
                    currentCell.State = CellState.Path;
                    path.Add(signal.GridPosition);
                    _lastPos = signal.GridPosition;
                    GridModel.UpdateGrid();
                    SignalBus.Fire(new GridUpdatedSignal());
                }
            }
            else if (signal.Type == InputType.PointerUp)
            {
                _activeColor = ColorType.None;
                _lastPos = new Vector2Int(-1, -1);
            }
        }

        private void ClearPath(ColorType color)
        {
            var path = GridModel.Paths[color];
            foreach (var pos in path)
            {
                var cell = GridModel.Grid[pos.x, pos.y];
                if (cell.State == CellState.Path)
                {
                    cell.State = CellState.Empty;
                    cell.Color = ColorType.None;
                }
            }
            path.Clear();
        }

        private void BacktrackPath(ColorType color, Vector2Int toPos)
        {
            var path = GridModel.Paths[color];
            int idx = path.IndexOf(toPos);
            if (idx == -1) return;

            for (int i = path.Count - 1; i > idx; i--)
            {
                var pos = path[i];
                var cell = GridModel.Grid[pos.x, pos.y];
                if (cell.State == CellState.Path)
                {
                    cell.State = CellState.Empty;
                    cell.Color = ColorType.None;
                }
                path.RemoveAt(i);
            }
        }

        private void BreakPath(ColorType color, Vector2Int breakPos)
        {
            BacktrackPath(color, breakPos);
        }
    }
}
