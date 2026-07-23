using System;
using Nexus.Core;
using Nexus.Core.Services;
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
        [Inject] public IPlayerPrefsService PlayerPrefsService { get; set; }
        [Inject] public ILoggerService LoggerService { get; set; }

        // Batched history bypass edilerek her adımda snapshot kaydı sağlanır (Testler ve oyun hassasiyeti için)
        private void EnsureHistoryRecorded()
        {
            HistoryService.Record(GridModel, GameSessionModel);
        }

        // Önbelleğe alınmış save Action — her RequestSave()'de yeni closure alloc'u önler
        private Action _cachedSaveAction;

        private void RequestSave()
        {
            if (_cachedSaveAction == null)
                _cachedSaveAction = () => GridStateSerializer.Save(GridModel, GameSessionModel, LevelModel, PlayerPrefsService);
            SaveThrottler?.TryRequestSave(_cachedSaveAction);
        }

        public void Execute(InputInteractionSignal signal)
        {
            var state = GameStateModel.CurrentState;

            // HATA FIX: Simülasyon modunda grid tıklamaları state'i değiştirmez.
            // Sadece pause/durdur butonu ile simülasyon kesilebilir.
            if (state == GameState.Simulating)
                return;

            if (state != GameState.Playing && state != GameState.Paused)
            {
                return;
            }

            if (signal.GridPosition.x < 0 || signal.GridPosition.y < 0 || signal.GridPosition.x >= GridModel.Width || signal.GridPosition.y >= GridModel.Height)
            {
                LoggerService?.LogWarning($"[PixelFlow.ProcessInputCommand] Input position out of bounds: {signal.GridPosition}");
                return;
            }

            var currentCell = GridModel.Grid[signal.GridPosition.x, signal.GridPosition.y];

            if (state == GameState.Paused)
            {
                if (signal.Type == InputType.PointerDown)
                {
                    LoggerService?.Log($"[PixelFlow.ProcessInputCommand] PointerDown in Paused state at {signal.GridPosition}. PathColorCount: {currentCell.PathColorCount}, HasViaduct: {currentCell.HasViaduct}");
                    if (currentCell.PathColorCount >= 2 && !currentCell.HasViaduct)
                    {
                        LoggerService?.Log($"[PixelFlow.ProcessInputCommand] Firing PlaceViaductSignal for position {signal.GridPosition}.");
                        SignalBus.Fire(new PlaceViaductSignal { Position = signal.GridPosition });
                        HapticService?.Vibrate(HapticType.Medium);
                        return;
                    }
                    else
                    {
                        LoggerService?.Log($"[PixelFlow.ProcessInputCommand] PointerDown in Paused state on non-crossing cell {signal.GridPosition}. Recovering from crisis, reverting to Playing state.");
                        GameSessionModel?.MarkCrisisUndoUsed();
                        GameStateModel.SetState(GameState.Playing);
                        state = GameState.Playing;
                    }
                }
                else
                {
                    return;
                }
            }

            if (signal.Type == InputType.PointerDown)
            {
                ColorType clickedColor = currentCell.Color != ColorType.None ? currentCell.Color
                    : currentCell.PathColorCount > 0 ? currentCell.FirstPathColor
                    : ColorType.None;

                LoggerService?.Log($"[PixelFlow.ProcessInputCommand] PointerDown at {signal.GridPosition}. ColorType resolved: {clickedColor}");

                if (clickedColor != ColorType.None)
                {
                    if (GridModel.LockedColors.Contains(clickedColor))
                    {
                        LoggerService?.LogWarning($"[PixelFlow.ProcessInputCommand] Color {clickedColor} is locked. Interaction blocked.");
                        return;
                    }

                    EnsureHistoryRecorded();

                    GridModel.ActiveColor.Value = clickedColor;
                    GridModel.LastPosition.Value = signal.GridPosition;

                    if (!GridModel.Paths.ContainsKey(clickedColor))
                    {
                        GridModel.Paths[clickedColor] = new List<Vector2Int>();
                    }

                    if (currentCell.State == CellState.Node)
                    {
                        LoggerService?.Log($"[PixelFlow.ProcessInputCommand] Starting new path for color {clickedColor} from source node {signal.GridPosition}.");
                        PathService.ClearPath(clickedColor);
                        GridModel.Paths[clickedColor].Add(signal.GridPosition);
                    }
                    else if (currentCell.State == CellState.Path || currentCell.State == CellState.Bridge)
                    {
                        LoggerService?.Log($"[PixelFlow.ProcessInputCommand] Backtracking path for color {clickedColor} from cell {signal.GridPosition}.");
                        PathService.BacktrackPath(clickedColor, signal.GridPosition);
                    }
                    // GridUpdatedSignal ATLANIR — path henüz tamamlanmadı (sadece 1 hücre).
                    // İlk Drag olayı zaten bu sinyali fırlatır ve görsel güncellenir.
                    // RequestSave();  ← kaldırıldı: henüz kaydedilecek path yok
                    HapticService?.Vibrate(HapticType.Light);
                }
            }
            else if (signal.Type == InputType.Drag)
            {
                if (GridModel.ActiveColor.Value == ColorType.None)
                    return;

                if (signal.GridPosition.x < 0 || signal.GridPosition.y < 0 || signal.GridPosition.x >= GridModel.Width || signal.GridPosition.y >= GridModel.Height)
                {
                    LoggerService?.LogWarning($"[PixelFlow.ProcessInputCommand] Drag position out of bounds: {signal.GridPosition}");
                    return;
                }

                int distance = Mathf.Abs(signal.GridPosition.x - GridModel.LastPosition.Value.x) + Mathf.Abs(signal.GridPosition.y - GridModel.LastPosition.Value.y);
                if (distance != 1)
                    return;

                if (!GridModel.Paths.TryGetValue(GridModel.ActiveColor.Value, out var path))
                {
                    LoggerService?.LogWarning($"[PixelFlow.ProcessInputCommand] Drag aborted: no path found for active color {GridModel.ActiveColor.Value}.");
                    GridModel.ActiveColor.Value = ColorType.None;
                    return;
                }

                if (path.Contains(signal.GridPosition))
                {
                    LoggerService?.Log($"[PixelFlow.ProcessInputCommand] Drag backtrack to path position {signal.GridPosition} for color {GridModel.ActiveColor.Value}.");
                    EnsureHistoryRecorded();
                    PathService.BacktrackPath(GridModel.ActiveColor.Value, signal.GridPosition);
                    GridModel.LastPosition.Value = signal.GridPosition;
                    SignalBus.Fire(new GridUpdatedSignal());
                    // RequestSave();  ← kaldırıldı: backtrack intermediate, save gerekmez
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
                    {
                        LoggerService?.LogWarning($"[PixelFlow.ProcessInputCommand] Drag blocked: Bridge exit direction must continue straight. EntryDir: {entryDir}, MoveDir: {moveDir} at {bridgePos}");
                        return;
                    }
                }

                if (currentCell.State == CellState.Obstacle)
                {
                    LoggerService?.LogWarning($"[PixelFlow.ProcessInputCommand] Drag blocked: Hit static obstacle at {signal.GridPosition}.");
                    return;
                }

                Vector2Int drawMoveDir = signal.GridPosition - GridModel.LastPosition.Value;
                if (ObstacleService != null)
                {
                    if (ObstacleService.IsOneWay(signal.GridPosition, drawMoveDir) ||
                        ObstacleService.IsOneWay(GridModel.LastPosition.Value, drawMoveDir))
                    {
                        LoggerService?.LogWarning($"[PixelFlow.ProcessInputCommand] Drag blocked: OneWay constraint violation entering {signal.GridPosition} from direction {drawMoveDir}.");
                        return;
                    }
                }

                bool isDrawableObstacle = currentCell.State == CellState.Obstacle &&
                    (currentCell.ObstacleType == ObstacleType.Ferry || currentCell.ObstacleType == ObstacleType.NarrowPass);

                if (currentCell.State == CellState.Empty || (isDrawableObstacle && currentCell.PathColorCount == 0))
                {
                    LoggerService?.Log($"[PixelFlow.ProcessInputCommand] Drag extending path for color {GridModel.ActiveColor.Value} to {signal.GridPosition}.");
                    EnsureHistoryRecorded();
                    currentCell.Color = GridModel.ActiveColor.Value;
                    currentCell.State = CellState.Path;
                    if (!currentCell.HasPathColor(GridModel.ActiveColor.Value))
                    {
                        currentCell.AddPathColor(GridModel.ActiveColor.Value);
                    }
                    path.Add(signal.GridPosition);
                    GridModel.LastPosition.Value = signal.GridPosition;
                    SignalBus.Fire(new GridUpdatedSignal());
                    SoundModel.PlayDrawSound(path.Count);
                    // Batched save: drag intermetiate steps atlanır — PointerUp veya path
                    // complete kaydeder. SaveThrottler 2s throttle ile zaten bekletiyor.
                    // RequestSave();  ← kaldırıldı: intermediate drag step, save gerekmez
                }
                else if (currentCell.State == CellState.Node && currentCell.Color == GridModel.ActiveColor.Value)
                {
                    if (path.Count > 0 && path[path.Count - 1] == signal.GridPosition) return;

                    LoggerService?.Log($"[PixelFlow.ProcessInputCommand] Drag completed connection for color {GridModel.ActiveColor.Value} at target node {signal.GridPosition}.");
                    EnsureHistoryRecorded();
                    if (!currentCell.HasPathColor(GridModel.ActiveColor.Value))
                    {
                        currentCell.AddPathColor(GridModel.ActiveColor.Value);
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
                    if (currentCell.HasPathColor(GridModel.ActiveColor.Value))
                        return;

                    Vector2Int entryDir = signal.GridPosition - GridModel.LastPosition.Value;

                    if (currentCell.PathColorCount > 0)
                    {
                        ColorType existingColor = currentCell.FirstPathColor;
                        if (GridModel.Paths.TryGetValue(existingColor, out var otherPath))
                        {
                            if (!BridgeValidationUtility.IsValidBridgeCrossing(
                                otherPath, path, signal.GridPosition, entryDir))
                            {
                                LoggerService?.Log($"[PixelFlow.ProcessInputCommand] Path crossing conflict at {signal.GridPosition} between {GridModel.ActiveColor.Value} and {existingColor}. Backtracking conflicting path.");
                                EnsureHistoryRecorded();
                                PathService.BacktrackPath(existingColor, signal.GridPosition);
                            }
                        }
                    }

                    if (currentCell.PathColorCount >= BridgeValidationUtility.MaxPathsPerBridge)
                    {
                        // If still full, backtrack the conflicting color to make space
                        ColorType firstColor = currentCell.FirstPathColor;
                        LoggerService?.Log($"[PixelFlow.ProcessInputCommand] Cell at {signal.GridPosition} remains full after backtrack. Forcing backtrack of {firstColor} color.");
                        EnsureHistoryRecorded();
                        PathService.BacktrackPath(firstColor, signal.GridPosition);
                    }

                    if (currentCell.PathColorCount >= BridgeValidationUtility.MaxPathsPerBridge)
                    {
                        LoggerService?.LogWarning($"[PixelFlow.ProcessInputCommand] Drag blocked at {signal.GridPosition}: Cell already occupied by max paths.");
                        Nexus.Core.Services.NexusLog.Warn("ProcessInputCommand", "HandleDrag", "?", "Cell already occupied by max paths. Drawing blocked.");
                        return;
                    }

                    LoggerService?.Log($"[PixelFlow.ProcessInputCommand] Drag crossing at {signal.GridPosition} for color {GridModel.ActiveColor.Value}. Existing path color count: {currentCell.PathColorCount}");
                    EnsureHistoryRecorded();
                    if (currentCell.PathColorCount == 0)
                    {
                        currentCell.UnderColor = GridModel.ActiveColor.Value;
                    }
                    else
                    {
                        currentCell.OverColor = GridModel.ActiveColor.Value;
                    }

                    currentCell.AddPathColor(GridModel.ActiveColor.Value);
                    path.Add(signal.GridPosition);
                    GridModel.LastPosition.Value = signal.GridPosition;

                    if (!currentCell.HasViaduct && currentCell.PathColorCount >= 2)
                    {
                        LoggerService?.Log($"[PixelFlow.ProcessInputCommand] Firing PathIntersectionWarningSignal for {signal.GridPosition} due to viaductless crossing.");
                        SignalBus.Fire(new PathIntersectionWarningSignal { Position = signal.GridPosition });
                    }

                    SignalBus.Fire(new GridUpdatedSignal());
                    // RequestSave();  ← kaldırıldı: bridge crossing intermediate, save gerekmez
                }
            }
            else if (signal.Type == InputType.PointerUp)
            {
                if (GridModel.ActiveColor.Value != ColorType.None)
                {
                    LoggerService?.Log($"[PixelFlow.ProcessInputCommand] PointerUp. Resetting ActiveColor from {GridModel.ActiveColor.Value}. Requesting Save.");
                    EnsureHistoryRecorded();
                    GridModel.ActiveColor.Value = ColorType.None;
                    GridModel.LastPosition.Value = new Vector2Int(-1, -1);
                    RequestSave();
                }
                else
                {
                }
            }
        }

        public void Reset()
        {
            // Do not reset GridModel singleton values in command Reset, as commands are recycled per signal execution
        }
    }
}
