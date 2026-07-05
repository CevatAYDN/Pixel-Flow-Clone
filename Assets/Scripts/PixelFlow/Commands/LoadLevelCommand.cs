using Nexus.Core;
using Nexus.Core.Services;
using PixelFlow.Data;
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
        [Inject] public ILoggerService LoggerService { get; set; }
        [Inject] public IPlayerPrefsService PlayerPrefsService { get; set; }

        public void Execute(LoadLevelSignal signal)
        {
            if (signal.LevelToLoad == null)
            {
                LoggerService?.LogError("[PixelFlow.LoadLevelCommand] ERROR: LoadLevelSignal received with null LevelToLoad.");
                return;
            }

            int nodeCount = signal.LevelToLoad.initialNodes != null ? signal.LevelToLoad.initialNodes.Count : 0;
            int bridgeCount = signal.LevelToLoad.bridgePositions != null ? signal.LevelToLoad.bridgePositions.Count : 0;
            int obstacleCount = signal.LevelToLoad.obstacles != null ? signal.LevelToLoad.obstacles.Count : 0;

            LoggerService?.Log($"[PixelFlow.LoadLevelCommand] ▶ Executing LoadLevel: Level {signal.LevelToLoad.levelIndex + 1} ({signal.LevelToLoad.name}, Grid: {signal.LevelToLoad.width}x{signal.LevelToLoad.height}, Nodes: {nodeCount}, Bridges: {bridgeCount}, Obstacles: {obstacleCount})");
            
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
                if (!cell.HasPathColor(node.color))
                {
                    cell.AddPathColor(node.color);
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

            if (signal.LevelToLoad.oneWayCells != null)
            {
                foreach (var owc in signal.LevelToLoad.oneWayCells)
                {
                    if (owc.position.x >= 0 && owc.position.x < GridModel.Width &&
                        owc.position.y >= 0 && owc.position.y < GridModel.Height)
                    {
                        var cell = GridModel.Grid[owc.position.x, owc.position.y];
                        // OneWay hücreleri çizilebilir boş hücrelerdir, sadece yön kısıtı vardır.
                        // State'i Empty bırakıyoruz ama ObstacleType.OneWay atayarak yönü belirtiyoruz.
                        cell.State = CellState.Empty;
                        cell.ObstacleType = ObstacleType.OneWay;
                    }
                }
            }

            HistoryService.Clear();
            int totalViaducts = signal.LevelToLoad.viaductLimit + (CityEconomyModel != null ? CityEconomyModel.ViaductBonus : 0);
            GameSessionModel.StartSession(totalViaducts, signal.LevelToLoad.flowScoreThreshold);
            HintModel.ResetSessionHints();
            ObstacleService?.InitializeFromLevel(signal.LevelToLoad);
            TutorialDriver?.OnLevelLoaded(signal.LevelToLoad.levelIndex);

            SignalBus.Fire(new GridUpdatedSignal());
            GameStateModel.SetState(GameState.Playing);
            SaveThrottler?.TryRequestSave(() => GridStateSerializer.Save(GridModel, GameSessionModel, LevelModel, PlayerPrefsService));
            UnityEngine.Debug.Log($"[PixelFlow.LoadLevelCommand] ✔ Level {signal.LevelToLoad.levelIndex + 1} loaded successfully. State: GameState.Playing.");
        }

        public void Reset()
        {
            // Injected dependencies are automatically cleared by the framework's CommandPool
        }
    }
}
