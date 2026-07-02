using Nexus.Core;
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

        public void Execute(PlaceViaductSignal signal)
        {
            if (GameStateModel.CurrentState != GameState.Playing && GameStateModel.CurrentState != GameState.Paused)
            {
                return;
            }

            Vector2Int pos = signal.Position;
            if (pos.x < 0 || pos.y < 0 || pos.x >= GridModel.Width || pos.y >= GridModel.Height)
                return;

            var cell = GridModel.Grid[pos.x, pos.y];

            // Viyadük sadece en az 2 yolun kesiştiği yerlere ve henüz viyadük olmayan hücrelere konulabilir
            if (cell.PathColors.Count < 2 || cell.PathColors.Count > BridgeValidationUtility.MaxPathsPerBridge || cell.HasViaduct)
            {
                Debug.LogWarning($"[PlaceViaductCommand] Cannot place viaduct at {pos}. Paths: {cell.PathColors.Count}, HasViaduct: {cell.HasViaduct}");
                return;
            }

            // Viyadük limitini kontrol et ve harca
            if (GameSessionModel.TryUseViaduct())
            {
                cell.HasViaduct = true;
                cell.State = CellState.Bridge;
                
                // İlk eklenen alttan, ikinci eklenen üstten geçer
                cell.UnderColor = cell.PathColors[0];
                cell.OverColor = cell.PathColors[1];

                Debug.Log($"[PlaceViaductCommand] Placed viaduct at {pos}. Under: {cell.UnderColor}, Over: {cell.OverColor}. Remaining viaducts: {GameSessionModel.AvailableViaducts}");
                
                // Eğer kriz durumunda duraklatılmışsak, kazadan önceki orijinal oyun durumuna geri dön!
                if (GameStateModel.CurrentState == GameState.Paused)
                {
                    var targetState = GameStateModel.PreviousState == GameState.Simulating 
                        ? GameState.Simulating 
                        : GameState.Playing;
                    GameStateModel.SetState(targetState);
                }
                
                SignalBus.Fire(new GridUpdatedSignal());
            }
            else
            {
                Debug.LogWarning("[PlaceViaductCommand] Out of viaducts!");
                // İleride UI üzerinde "Viyadük Hakkınız Bitti, Reklam İzle" pop-up'ı tetiklemek için sinyal fırlatılabilir.
            }
        }

        public void Reset()
        {
        }
    }
}
