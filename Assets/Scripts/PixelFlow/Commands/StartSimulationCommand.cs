using Nexus.Core;
using Nexus.Core.Services;
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
        [Inject] public ILoggerService LoggerService { get; set; }

        public void Execute(PixelFlow.Signals.StartSimulationSignal signal)
        {
            var state = GameStateModel.CurrentState;
            LoggerService?.Log($"[PixelFlow.StartSimulationCommand] StartSimulationSignal received. Current state: {state}");

            if (state != GameState.Playing)
            {
                LoggerService?.LogWarning($"[PixelFlow.StartSimulationCommand] Aborted: Cannot start simulation from state {state}. Simulation requires state Playing.");
                return;
            }

            LoggerService?.Log("[PixelFlow.StartSimulationCommand] Requesting VehicleSimulator to start simulation phase.");
            VehicleSimulator.StartSimulationPhase();
            // GameState.Simulating state'i VehicleSimulator.StartSimulationPhase() içinde set edilir
        }

        public void Reset() { }
    }
}
