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
                    : currentCell.PathColors.Count > 0 ? currentCell.PathColors[0]
                    : ColorType.None;

                if (clickedColor != ColorType.None)
                {
                    if (GridModel.LockedColors.Contains(clickedColor))
                        return;

                    RecordHistory();

                    GridModel.ActiveColor = clickedColor;
                    GridModel.LastPosition = signal.GridPosition;

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
                    PathService.BacktrackPath(GridModel.ActiveColor, signal.GridPosition);
                    GridModel.LastPosition = signal.GridPosition;
                    SignalBus.Fire(new GridUpdatedSignal());
                    RequestSave();
                    return;
                }

                // Bridge'den çıkış yön kontrolü: girilen yönde düz devam zorunlu
                var lastCell = GridModel.Grid[GridModel.LastPosition.x, GridModel.LastPosition.y];
                if ((lastCell.HasViaduct || lastCell.State == CellState.Bridge) && path.Count >= 2)
                {
                    Vector2Int bridgePos = GridModel.LastPosition;
                    Vector2Int entryDir = bridgePos - path[path.Count - 2];
                    Vector2Int moveDir = signal.GridPosition - bridgePos;
                    if (moveDir != entryDir)
                        return;
                }

                if (currentCell.State == CellState.Obstacle)
                    return;

                if (ObstacleService != null && ObstacleService.IsOneWay(signal.GridPosition, GridModel.ActiveColor, signal.GridPosition - GridModel.LastPosition))
                {
                    return;
                }

                if (currentCell.State == CellState.Empty)
                {
                    RecordHistory();
                    currentCell.Color = GridModel.ActiveColor;
                    currentCell.State = CellState.Path;
                    if (!currentCell.PathColors.Contains(GridModel.ActiveColor))
                    {
                        currentCell.PathColors.Add(GridModel.ActiveColor);
                    }
                    path.Add(signal.GridPosition);
                    GridModel.LastPosition = signal.GridPosition;
                    SignalBus.Fire(new GridUpdatedSignal());
                    SoundModel.PlayDrawSound(path.Count);
                    RequestSave();
                }
                else if (currentCell.State == CellState.Node && currentCell.Color == GridModel.ActiveColor)
                {
                    if (path.Count > 0 && path[path.Count - 1] == signal.GridPosition) return;

                    RecordHistory();
                    if (!currentCell.PathColors.Contains(GridModel.ActiveColor))
                    {
                        currentCell.PathColors.Add(GridModel.ActiveColor);
                    }
                    path.Add(signal.GridPosition);
                    GridModel.LastPosition = signal.GridPosition;
                    GridModel.ActiveColor = ColorType.None;
                    SignalBus.Fire(new GridUpdatedSignal());
                    SignalBus.Fire(new CheckWinConditionSignal());
                    HapticService?.Vibrate(HapticType.Medium);
                    RequestSave();
                }
                else if (currentCell.State == CellState.Path || currentCell.State == CellState.Bridge)
                {
                    if (currentCell.PathColors.Contains(GridModel.ActiveColor))
                        return;

                    Vector2Int entryDir = signal.GridPosition - GridModel.LastPosition;

                    if (currentCell.HasViaduct || currentCell.State == CellState.Bridge)
                    {
                        if (currentCell.PathColors.Count > 0)
                        {
                            ColorType existingColor = currentCell.PathColors[0];
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
                        if (!currentCell.PathColors.Contains(GridModel.ActiveColor))
                        {
                            currentCell.PathColors.Add(GridModel.ActiveColor);
                        }
                        currentCell.OverColor = GridModel.ActiveColor;
                        path.Add(signal.GridPosition);
                        GridModel.LastPosition = signal.GridPosition;
                        SignalBus.Fire(new GridUpdatedSignal());
                        RequestSave();
                    }
                    else
                    {
                        RecordHistory();
                        if (!currentCell.PathColors.Contains(GridModel.ActiveColor))
                        {
                            currentCell.PathColors.Add(GridModel.ActiveColor);
                        }
                        if (currentCell.Color == ColorType.None)
                        {
                            currentCell.Color = GridModel.ActiveColor;
                        }
                        path.Add(signal.GridPosition);
                        GridModel.LastPosition = signal.GridPosition;

                        if (currentCell.PathColors.Count >= 2 && !currentCell.HasViaduct)
                        {
                            SignalBus.Fire(new PathIntersectionWarningSignal { Position = signal.GridPosition });
                        }

                        SignalBus.Fire(new GridUpdatedSignal());
                        SoundModel.PlayDrawSound(path.Count);
                        RequestSave();
                    }
                }
            }
            else if (signal.Type == InputType.PointerUp)
            {
                if (GridModel.ActiveColor != ColorType.None)
                {
                    RecordHistory();
                    GridModel.ActiveColor = ColorType.None;
                    GridModel.LastPosition = new Vector2Int(-1, -1);
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
