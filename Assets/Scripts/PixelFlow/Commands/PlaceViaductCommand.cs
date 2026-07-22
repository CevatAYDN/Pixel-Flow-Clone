using System.Collections.Generic;
using Nexus.Core;
using Nexus.Core.Services;
using PixelFlow.Models;
using PixelFlow.Signals;
using PixelFlow.Data;
using PixelFlow.Services;
using UnityEngine;

namespace PixelFlow.Commands
{
    public class PlaceViaductCommand : ICommand<PlaceViaductSignal>, IResettable
    {
        [Inject] public IGridModel GridModel { get; set; }
        [Inject] public IGameSessionModel GameSessionModel { get; set; }
        [Inject] public IGameStateModel GameStateModel { get; set; }
            [Inject] public ISignalBus SignalBus { get; set; }
        [Inject] public IGameHistoryService HistoryService { get; set; }
        [Inject] public ISaveThrottler SaveThrottler { get; set; }
        [Inject] public ILevelModel LevelModel { get; set; }
        [Inject] public IHapticService HapticService { get; set; }
        [Inject] public ILoggerService LoggerService { get; set; }
        [Inject] public IPlayerPrefsService PlayerPrefsService { get; set; }

        public void Execute(PlaceViaductSignal signal)
        {
            Vector2Int pos = signal.Position;
            LoggerService?.Log($"[PixelFlow.PlaceViaductCommand] Attempting to place viaduct at {pos}. Current state: {GameStateModel.CurrentState}");

            if (GameStateModel.CurrentState != GameState.Playing && GameStateModel.CurrentState != GameState.Paused)
            {
                LoggerService?.LogWarning($"[PixelFlow.PlaceViaductCommand] Aborted: Cannot place viaduct while in state {GameStateModel.CurrentState}");
                return;
            }

            if (pos.x < 0 || pos.y < 0 || pos.x >= GridModel.Width || pos.y >= GridModel.Height)
            {
                LoggerService?.LogError($"[PixelFlow.PlaceViaductCommand] Aborted: Position {pos} is out of bounds (Grid size: {GridModel.Width}x{GridModel.Height})");
                return;
            }

            var cell = GridModel.Grid[pos.x, pos.y];

            // Viyadük sadece en az 2 yolun kesiştiği yerlere ve henüz viyadük olmayan hücrelere konulabilir
            if (cell.PathColorCount < 2 || cell.PathColorCount > BridgeValidationUtility.MaxPathsPerBridge || cell.HasViaduct)
            {
                LoggerService?.LogWarning($"[PixelFlow.PlaceViaductCommand] Cannot place viaduct at {pos}. PathColorCount: {cell.PathColorCount}, HasViaduct: {cell.HasViaduct}");
                return;
            }

            // Viyadük limitini kontrol et ve harca
            LoggerService?.Log($"[PixelFlow.PlaceViaductCommand] Checking viaduct limits. Max: {GameSessionModel.MaxViaducts}, Available: {GameSessionModel.AvailableViaducts}");
            if (GameSessionModel.TryUseViaduct())
            {
                HistoryService.Record(GridModel, GameSessionModel);

                cell.HasViaduct = true;
                cell.State = CellState.Bridge;

                var colors = new ColorType[2];
                int ci = 0;
                foreach (var pc in cell.GetPathColors())
                {
                    if (ci < 2) colors[ci++] = pc;
                    else break;
                }
                List<Vector2Int> pathA = null;
                List<Vector2Int> pathB = null;
                bool hasPathA = GridModel.Paths != null && GridModel.Paths.TryGetValue(colors[0], out pathA);
                bool hasPathB = GridModel.Paths != null && GridModel.Paths.TryGetValue(colors[1], out pathB);

                if (hasPathA && pathA != null)
                {
                    var dirA = BridgeValidationUtility.GetCrossingDirection(pathA, pos);
                    if (dirA.x != 0)
                    {
                        cell.UnderColor = colors[0];
                        cell.OverColor = colors[1];
                    }
                    else
                    {
                        cell.UnderColor = colors[1];
                        cell.OverColor = colors[0];
                    }
                }
                else
                {
                    cell.UnderColor = colors[0];
                    cell.OverColor = colors[1];
                }

                LoggerService?.Log($"[PixelFlow.PlaceViaductCommand] Successfully placed viaduct at {pos}. Under: {cell.UnderColor}, Over: {cell.OverColor}. Remaining viaducts: {GameSessionModel.AvailableViaducts}");
                
                // Eğer kriz durumunda duraklatılmışsak, kazadan önceki orijinal oyun durumuna geri dön!
                if (GameStateModel.CurrentState == GameState.Paused)
                {
                    var targetState = GameStateModel.PreviousState == GameState.Simulating 
                        ? GameState.Simulating 
                        : GameState.Playing;
                    LoggerService?.Log($"[PixelFlow.PlaceViaductCommand] Resuming game from Paused state to: {targetState}");
                    GameStateModel.SetState(targetState);
                }
                
                SignalBus.Fire(new GridUpdatedSignal());
                SaveHelper.TrySave(SaveThrottler, GridModel, GameSessionModel, LevelModel, PlayerPrefsService);
                HapticService?.Vibrate(HapticType.Heavy);
            }
            else
            {
                LoggerService?.LogWarning("[PixelFlow.PlaceViaductCommand] Aborted: Out of viaducts limit!");
                SignalBus.Fire(new ViaductExhaustedSignal());
            }
        }

        public void Reset()
        {
        }
    }
}
