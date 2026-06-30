using Nexus.Core;
using PixelFlow.Models;
using PixelFlow.Signals;
using PixelFlow.Data;
using PixelFlow.Services;
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
        [Inject] public IPathService PathService { get; set; }
        [Inject] public IGameStateModel GameStateModel { get; set; }
        [Inject] public IGameHistoryService HistoryService { get; set; }

        private void RecordHistory()
        {
            HistoryService.Record(GridModel);
        }

        public void Execute(InputInteractionSignal signal)
        {
            if (GameStateModel.CurrentState != GameState.Playing)
            {
                return;
            }

            if (signal.GridPosition.x < 0 || signal.GridPosition.y < 0 || signal.GridPosition.x >= GridModel.Width || signal.GridPosition.y >= GridModel.Height)
                return;

            var currentCell = GridModel.Grid[signal.GridPosition.x, signal.GridPosition.y];

            if (signal.Type == InputType.PointerDown)
            {
                if (currentCell.Color != ColorType.None)
                {
                    if (GridModel.LockedColors.Contains(currentCell.Color))
                        return;

                    RecordHistory();

                    GridModel.ActiveColor = currentCell.Color;
                    GridModel.LastPosition = signal.GridPosition;

                    if (!GridModel.Paths.ContainsKey(GridModel.ActiveColor))
                    {
                        GridModel.Paths[GridModel.ActiveColor] = new List<Vector2Int>();
                    }

                    if (currentCell.State == CellState.Node)
                    {
                        PathService.ClearPath(GridModel.ActiveColor);
                        GridModel.Paths[GridModel.ActiveColor].Add(signal.GridPosition);
                    }
                    else if (currentCell.State == CellState.Path || currentCell.State == CellState.Bridge)
                    {
                        PathService.BacktrackPath(GridModel.ActiveColor, signal.GridPosition);
                    }
                    SignalBus.Fire(new GridUpdatedSignal());
                }
            }
            else if (signal.Type == InputType.Drag)
            {
                if (GridModel.ActiveColor == ColorType.None)
                    return;

                if (signal.GridPosition.x < 0 || signal.GridPosition.y < 0 || signal.GridPosition.x >= GridModel.Width || signal.GridPosition.y >= GridModel.Height)
                    return;

                int distance = Mathf.Abs(signal.GridPosition.x - GridModel.LastPosition.x) + Mathf.Abs(signal.GridPosition.y - GridModel.LastPosition.y);
                if (distance != 1)
                    return;

                var path = GridModel.Paths[GridModel.ActiveColor];

                if (path.Count > 1 && path[path.Count - 2] == signal.GridPosition)
                {
                    RecordHistory();
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
                    RecordHistory();
                    currentCell.Color = GridModel.ActiveColor;
                    currentCell.State = CellState.Path;
                    path.Add(signal.GridPosition);
                    GridModel.LastPosition = signal.GridPosition;
                    SignalBus.Fire(new GridUpdatedSignal());
                    SoundModel.PlayDrawSound(path.Count);
                }
                else if (currentCell.State == CellState.Node && currentCell.Color == GridModel.ActiveColor)
                {
                    RecordHistory();
                    path.Add(signal.GridPosition);
                    GridModel.LastPosition = signal.GridPosition;
                    GridModel.ActiveColor = ColorType.None;
                    SignalBus.Fire(new GridUpdatedSignal());
                    SignalBus.Fire(new CheckWinConditionSignal());
                }
                else if (currentCell.State == CellState.Bridge)
                {
                    RecordHistory();
                    path.Add(signal.GridPosition);
                    GridModel.LastPosition = signal.GridPosition;
                    SignalBus.Fire(new GridUpdatedSignal());
                }
                else if (currentCell.Color != ColorType.None && currentCell.Color != GridModel.ActiveColor && currentCell.State == CellState.Path)
                {
                    RecordHistory();
                    PathService.BreakPath(currentCell.Color, signal.GridPosition);
                    currentCell.Color = GridModel.ActiveColor;
                    currentCell.State = CellState.Path;
                    path.Add(signal.GridPosition);
                    GridModel.LastPosition = signal.GridPosition;
                    SignalBus.Fire(new GridUpdatedSignal());
                }
            }
            else if (signal.Type == InputType.PointerUp)
            {
                if (GridModel.ActiveColor != ColorType.None)
                {
                    RecordHistory();
                    GridModel.ActiveColor = ColorType.None;
                    GridModel.LastPosition = new Vector2Int(-1, -1);
                }
            }
        }

        public void Reset()
        {
            // Do not reset GridModel singleton values in command Reset, as commands are recycled per signal execution
        }
    }
}
