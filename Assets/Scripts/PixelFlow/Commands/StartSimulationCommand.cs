using Nexus.Core;
using PixelFlow.Models;
using PixelFlow.Services;

namespace PixelFlow.Commands
{
    /// <summary>
    /// GDD §8: Tüm yollar bağlandığında simülasyon fazını başlatır.
    /// CheckWinConditionCommand tarafından ateşlenen StartSimulationSignal'i işler.
    /// </summary>
    public class StartSimulationCommand : ICommand<PixelFlow.Signals.StartSimulationSignal>, IResettable
    {
        [Inject] public IVehicleSimulator VehicleSimulator { get; set; }
        [Inject] public IGameStateModel GameStateModel { get; set; }

        public void Execute(PixelFlow.Signals.StartSimulationSignal signal)
        {
            if (GameStateModel.CurrentState != GameState.Playing)
                return;

            VehicleSimulator.StartSimulationPhase();
            // GameState.Simulating state'i VehicleSimulator.StartSimulationPhase() içinde set edilir
        }

        public void Reset() { }
    }
}
