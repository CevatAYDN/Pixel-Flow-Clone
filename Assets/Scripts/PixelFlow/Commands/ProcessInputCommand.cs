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
        [Inject] public IGameSessionModel GameSessionModel { get; set; }
        [Inject] public ILevelModel LevelModel { get; set; }
        [Inject] public ISaveThrottler SaveThrottler { get; set; }
        [Inject] public IHapticService HapticService { get; set; }
        [Inject] public IObstacleService ObstacleService { get; set; }

        private void RecordHistory()
        {
            HistoryService.Record(GridModel);
        }

        private void RequestSave()
        {
            SaveThrottler?.TryRequestSave(GridModel, GameSessionModel, LevelModel);
        }

        public void Execute(InputInteractionSignal signal)
        {
            var state = GameStateModel.CurrentState;
            if (state != GameState.Playing && state != GameState.Paused)
            {
                return;
            }

            if (signal.GridPosition.x < 0 || signal.GridPosition.y < 0 || signal.GridPosition.x >= GridModel.Width || signal.GridPosition.y >= GridModel.Height)
                return;

            var currentCell = GridModel.Grid[signal.GridPosition.x, signal.GridPosition.y];

            if (state == GameState.Paused)
            {
                if (signal.Type == InputType.PointerDown)
                {
                    if (currentCell.PathColors.Count >= 2 && !currentCell.HasViaduct)
                    {
                        SignalBus.Fire(new PlaceViaductSignal { Position = signal.GridPosition });
                        HapticService?.Vibrate(HapticType.Medium);
                    }
                }
                return;
            }

            if (signal.Type == InputType.PointerDown)
            {
                ColorType clickedColor = currentCell.Color != ColorType.None ? currentCell.Color
                    : currentCell.PathColors.Count > 0 ? System.Linq.Enumerable.First(currentCell.PathColors)
                    : ColorType.None;

                if (clickedColor != ColorType.None)
                {
                    if (GridModel.LockedColors.Contains(clickedColor))
                        return;

                    RecordHistory();

                    GridModel.ActiveColor.Value = clickedColor;
                    GridModel.LastPosition.Value = signal.GridPosition;

                    if (!GridModel.Paths.ContainsKey(clickedColor))
                    {
                        GridModel.Paths[clickedColor] = new List<Vector2Int>();
                    }

                    if (currentCell.State == CellState.Node)
                    {
                        PathService.ClearPath(clickedColor);
                        GridModel.Paths[clickedColor].Add(signal.GridPosition);
                    }
                    else if (currentCell.State == CellState.Path || currentCell.State == CellState.Bridge)
                    {
                        PathService.BacktrackPath(clickedColor, signal.GridPosition);
                    }
                    SignalBus.Fire(new GridUpdatedSignal());
                    RequestSave();
                    HapticService?.Vibrate(HapticType.Light);
                }
            }
            else if (signal.Type == InputType.Drag)
            {
                if (GridModel.ActiveColor.Value == ColorType.None)
                    return;

                if (signal.GridPosition.x < 0 || signal.GridPosition.y < 0 || signal.GridPosition.x >= GridModel.Width || signal.GridPosition.y >= GridModel.Height)
                    return;

                int distance = Mathf.Abs(signal.GridPosition.x - GridModel.LastPosition.Value.x) + Mathf.Abs(signal.GridPosition.y - GridModel.LastPosition.Value.y);
                if (distance != 1)
                    return;

                var path = GridModel.Paths[GridModel.ActiveColor.Value];

                if (path.Count > 1 && path[path.Count - 2] == signal.GridPosition)
                {
                    RecordHistory();
                    PathService.BacktrackPath(GridModel.ActiveColor.Value, signal.GridPosition);
                    GridModel.LastPosition.Value = signal.GridPosition;
                    SignalBus.Fire(new GridUpdatedSignal());
                    RequestSave();
                    return;
                }

                // Bridge'den çıkış yön kontrolü: girilen yönde düz devam zorunlu
                var lastCell = GridModel.Grid[GridModel.LastPosition.Value.x, GridModel.LastPosition.Value.y];
                if ((lastCell.HasViaduct || lastCell.State == CellState.Bridge) && path.Count >= 2)
                {
                    Vector2Int bridgePos = GridModel.LastPosition.Value;
                    Vector2Int entryDir = bridgePos - path[path.Count - 2];
                    Vector2Int moveDir = signal.GridPosition - bridgePos;
                    if (moveDir != entryDir)
                        return;
                }

                if (currentCell.State == CellState.Obstacle)
                    return;

                if (ObstacleService != null && ObstacleService.IsOneWay(signal.GridPosition, GridModel.ActiveColor.Value, signal.GridPosition - GridModel.LastPosition.Value))
                {
                    return;
                }

                if (currentCell.State == CellState.Empty)
                {
                    RecordHistory();
                    currentCell.Color = GridModel.ActiveColor.Value;
                    currentCell.State = CellState.Path;
                    if (!currentCell.PathColors.Contains(GridModel.ActiveColor.Value))
                    {
                        currentCell.PathColors.Add(GridModel.ActiveColor.Value);
                    }
                    path.Add(signal.GridPosition);
                    GridModel.LastPosition.Value = signal.GridPosition;
                    SignalBus.Fire(new GridUpdatedSignal());
                    SoundModel.PlayDrawSound(path.Count);
                    RequestSave();
                }
                else if (currentCell.State == CellState.Node && currentCell.Color == GridModel.ActiveColor.Value)
                {
                    if (path.Count > 0 && path[path.Count - 1] == signal.GridPosition) return;

                    RecordHistory();
                    if (!currentCell.PathColors.Contains(GridModel.ActiveColor.Value))
                    {
                        currentCell.PathColors.Add(GridModel.ActiveColor.Value);
                    }
                    path.Add(signal.GridPosition);
                    GridModel.LastPosition.Value = signal.GridPosition;
                    GridModel.ActiveColor.Value = ColorType.None;
                    SignalBus.Fire(new GridUpdatedSignal());
                    SignalBus.Fire(new CheckWinConditionSignal());
                    HapticService?.Vibrate(HapticType.Medium);
                    RequestSave();
                }
                else if (currentCell.State == CellState.Path || currentCell.State == CellState.Bridge)
                {
                    if (currentCell.PathColors.Contains(GridModel.ActiveColor.Value))
                        return;

                    Vector2Int entryDir = signal.GridPosition - GridModel.LastPosition.Value;

                    if (currentCell.HasViaduct || currentCell.State == CellState.Bridge)
                    {
                        if (currentCell.PathColors.Count >= BridgeValidationUtility.MaxPathsPerBridge)
                            return;

                        if (currentCell.PathColors.Count > 0)
                        {
                            ColorType existingColor = System.Linq.Enumerable.First(currentCell.PathColors);
                            if (GridModel.Paths.TryGetValue(existingColor, out var otherPath))
                            {
                                if (!BridgeValidationUtility.IsValidBridgeCrossing(
                                    otherPath, path, signal.GridPosition, entryDir))
                                {
                                    return;
                                }
                            }
                        }

                        RecordHistory();
                        if (!currentCell.PathColors.Contains(GridModel.ActiveColor.Value))
                        {
                            currentCell.PathColors.Add(GridModel.ActiveColor.Value);
                        }
                        currentCell.OverColor = GridModel.ActiveColor.Value;
                        path.Add(signal.GridPosition);
                        GridModel.LastPosition.Value = signal.GridPosition;
                        SignalBus.Fire(new GridUpdatedSignal());
                        RequestSave();
                    }
                    else
                    {
                        // Non-bridge path cell occupied by another color — block extension
                        return;
                    }
                }
            }
            else if (signal.Type == InputType.PointerUp)
            {
                if (GridModel.ActiveColor.Value != ColorType.None)
                {
                    RecordHistory();
                    GridModel.ActiveColor.Value = ColorType.None;
                    GridModel.LastPosition.Value = new Vector2Int(-1, -1);
                    RequestSave();
                }
            }
        }

        public void Reset()
        {
            // Do not reset GridModel singleton values in command Reset, as commands are recycled per signal execution
        }
    }
}
