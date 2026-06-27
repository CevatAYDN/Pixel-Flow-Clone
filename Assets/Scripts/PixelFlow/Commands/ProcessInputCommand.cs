using Nexus.Core;
using PixelFlow.Models;
using PixelFlow.Signals;
using PixelFlow.Data;
using UnityEngine;
using System.Collections.Generic;

namespace PixelFlow.Commands
{
    // Kayıt: GameContextLifecycle.OnConfigure'da fluent API ile yapılıyor.
    public class ProcessInputCommand : ICommand<InputInteractionSignal>, IResettable
    {
        [Inject] public IGridModel GridModel { get; set; }
        [Inject] public ISignalBus SignalBus { get; set; }
        [Inject] public ISoundModel SoundModel { get; set; }

        public void Execute(InputInteractionSignal signal)
        {
            UnityEngine.Debug.Log($"[ProcessInputCommand] Execute: Type={signal.Type}, Pos={signal.GridPosition}, ActiveColor={GridModel.ActiveColor}");

            if (signal.GridPosition.x < 0 || signal.GridPosition.y < 0 || signal.GridPosition.x >= GridModel.Width || signal.GridPosition.y >= GridModel.Height)
                return;

            var currentCell = GridModel.Grid[signal.GridPosition.x, signal.GridPosition.y];

            if (signal.Type == InputType.PointerDown)
            {
                UnityEngine.Debug.Log($"[ProcessInputCommand] PointerDown on ({signal.GridPosition.x}, {signal.GridPosition.y}), Color: {currentCell.Color}, State: {currentCell.State}");
                
                if (currentCell.Color != ColorType.None)
                {
                    // Skip interaction if color is locked (e.g. by hint)
                    if (GridModel.LockedColors.Contains(currentCell.Color))
                    {
                        UnityEngine.Debug.Log($"[ProcessInputCommand] Color {currentCell.Color} is locked by hint, ignoring input");
                        return;
                    }

                    GridModel.ActiveColor = currentCell.Color;
                    GridModel.LastPosition = signal.GridPosition;

                    if (!GridModel.Paths.ContainsKey(GridModel.ActiveColor))
                    {
                        GridModel.Paths[GridModel.ActiveColor] = new List<Vector2Int>();
                    }

                    if (currentCell.State == CellState.Node)
                    {
                        ClearPath(GridModel.ActiveColor);
                        GridModel.Paths[GridModel.ActiveColor].Add(signal.GridPosition);
                    }
                    else if (currentCell.State == CellState.Path || currentCell.State == CellState.Bridge)
                    {
                        BacktrackPath(GridModel.ActiveColor, signal.GridPosition);
                    }
                    SignalBus.Fire(new GridUpdatedSignal());
                }
            }
            else if (signal.Type == InputType.Drag)
            {
                UnityEngine.Debug.Log($"[ProcessInputCommand] Drag received at ({signal.GridPosition.x}, {signal.GridPosition.y}). ActiveColor={GridModel.ActiveColor}, LastPos={GridModel.LastPosition}");

                if (GridModel.ActiveColor == ColorType.None)
                {
                    UnityEngine.Debug.Log("[ProcessInputCommand] Drag ignored: ActiveColor is None");
                    return;
                }

                if (signal.GridPosition.x < 0 || signal.GridPosition.y < 0 || signal.GridPosition.x >= GridModel.Width || signal.GridPosition.y >= GridModel.Height)
                    return;

                int distance = Mathf.Abs(signal.GridPosition.x - GridModel.LastPosition.x) + Mathf.Abs(signal.GridPosition.y - GridModel.LastPosition.y);
                if (distance != 1)
                {
                    UnityEngine.Debug.Log($"[ProcessInputCommand] Drag ignored: distance={distance} != 1");
                    return;
                }

                var path = GridModel.Paths[GridModel.ActiveColor];

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
                    GridModel.LastPosition = signal.GridPosition;
                    SignalBus.Fire(new GridUpdatedSignal());
                    return;
                }

                if (currentCell.State == CellState.Empty)
                {
                    currentCell.Color = GridModel.ActiveColor;
                    currentCell.State = CellState.Path;
                    path.Add(signal.GridPosition);
                    GridModel.LastPosition = signal.GridPosition;
                    SignalBus.Fire(new GridUpdatedSignal());
                    SoundModel.PlayDrawSound(path.Count);
                }
                else if (currentCell.State == CellState.Node && currentCell.Color == GridModel.ActiveColor)
                {
                    path.Add(signal.GridPosition);
                    GridModel.LastPosition = signal.GridPosition;
                    GridModel.ActiveColor = ColorType.None;
                    SignalBus.Fire(new GridUpdatedSignal());
                    SignalBus.Fire(new CheckWinConditionSignal());
                }
                else if (currentCell.State == CellState.Bridge)
                {
                    path.Add(signal.GridPosition);
                    GridModel.LastPosition = signal.GridPosition;
                    SignalBus.Fire(new GridUpdatedSignal());
                }
                else if (currentCell.Color != ColorType.None && currentCell.Color != GridModel.ActiveColor && currentCell.State == CellState.Path)
                {
                    BreakPath(currentCell.Color, signal.GridPosition);
                    currentCell.Color = GridModel.ActiveColor;
                    currentCell.State = CellState.Path;
                    path.Add(signal.GridPosition);
                    GridModel.LastPosition = signal.GridPosition;
                    SignalBus.Fire(new GridUpdatedSignal());
                }
            }
            else if (signal.Type == InputType.PointerUp)
            {
                GridModel.ActiveColor = ColorType.None;
                GridModel.LastPosition = new Vector2Int(-1, -1);
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

        public void Reset()
        {
            // Do not reset GridModel singleton values in command Reset, as commands are recycled per signal execution
        }
    }
}
