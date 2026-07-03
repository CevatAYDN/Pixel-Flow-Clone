using Nexus.Core;
using PixelFlow.Models;
using PixelFlow.Signals;
using PixelFlow.Services;

namespace PixelFlow.Commands
{
    // Kayıt: GameContextLifecycle.OnConfigure'da fluent API ile yapılıyor.
    public class LoadLevelCommand : ICommand<LoadLevelSignal>, IResettable
    {
        [Inject] public IGridModel GridModel { get; set; }
        [Inject] public ILevelModel LevelModel { get; set; }
        [Inject] public IGameStateModel GameStateModel { get; set; }
        [Inject] public IGameSessionModel GameSessionModel { get; set; }
        [Inject] public IHintModel HintModel { get; set; }
        [Inject] public ISignalBus SignalBus { get; set; }
        [Inject] public IGameHistoryService HistoryService { get; set; }
        [Inject] public ICityEconomyModel CityEconomyModel { get; set; }
        [Inject] public IObstacleService ObstacleService { get; set; }
        [Inject] public ISaveThrottler SaveThrottler { get; set; }
        [Inject] public ITutorialDriver TutorialDriver { get; set; }

        public void Execute(LoadLevelSignal signal)
        {
            UnityEngine.Debug.Log($"[LoadLevelCommand] Loading level: {signal.LevelToLoad.name} ({signal.LevelToLoad.width}x{signal.LevelToLoad.height})");
            LevelModel.SetLevel(signal.LevelToLoad);
            GridModel.Initialize(signal.LevelToLoad.width, signal.LevelToLoad.height);

            if (signal.LevelToLoad.initialNodes != null)
            {
                foreach (var node in signal.LevelToLoad.initialNodes)
                {
                    if (node.position.x >= 0 && node.position.x < GridModel.Width &&
                        node.position.y >= 0 && node.position.y < GridModel.Height)
                    {
                        var cell = GridModel.Grid[node.position.x, node.position.y];
                        cell.State = CellState.Node;
                        cell.Color = node.color;
                        if (!cell.PathColors.Contains(node.color))
                        {
                            cell.PathColors.Add(node.color);
                        }
                    }
                }
            }

            if (signal.LevelToLoad.bridgePositions != null)
            {
                foreach (var bridgePos in signal.LevelToLoad.bridgePositions)
                {
                    if (bridgePos.x >= 0 && bridgePos.x < GridModel.Width &&
                        bridgePos.y >= 0 && bridgePos.y < GridModel.Height)
                    {
                        var cell = GridModel.Grid[bridgePos.x, bridgePos.y];
                        cell.State = CellState.Bridge;
                        cell.HasViaduct = true;
                    }
                }
            }

            if (signal.LevelToLoad.obstacles != null)
            {
                foreach (var obs in signal.LevelToLoad.obstacles)
                {
                    if (obs.position.x >= 0 && obs.position.x < GridModel.Width &&
                        obs.position.y >= 0 && obs.position.y < GridModel.Height)
                    {
                        var cell = GridModel.Grid[obs.position.x, obs.position.y];
                        cell.State = CellState.Obstacle;
                        cell.ObstacleType = obs.type;
                    }
                }
            }

            HistoryService.Clear();
            int totalViaducts = signal.LevelToLoad.viaductLimit + (CityEconomyModel != null ? CityEconomyModel.ViaductBonus : 0);
            GameSessionModel.StartSession(totalViaducts);
            HintModel.ResetSessionHints();
            ObstacleService?.InitializeFromLevel(signal.LevelToLoad);
            TutorialDriver?.OnLevelLoaded(signal.LevelToLoad.levelIndex);

            SignalBus.Fire(new GridUpdatedSignal());
            GameStateModel.SetState(GameState.Playing);
            SaveThrottler?.TryRequestSave(GridModel, GameSessionModel, LevelModel);
        }

        public void Reset()
        {
            // Injected dependencies are automatically cleared by the framework's CommandPool
        }
    }
}
