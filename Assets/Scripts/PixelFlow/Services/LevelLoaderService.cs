using Nexus.Core;
using Nexus.Core.Services;
using PixelFlow.Data;
using PixelFlow.Models;
using PixelFlow.Signals;
using System.Threading;
using System.Threading.Tasks;

namespace PixelFlow.Services
{
    /// <summary>
    /// GDD §8: Level yükleme sorumluluğunu LoadLevelCommand'den ayıran
    /// dedicated servis. Grid'in initialize edilmesi, node/bridge/obstacle/OneWay
    /// yerleştirilmesi, session başlatılması ve ilgili servislerin initialize
    /// edilmesi burada yönetilir.
    /// 
    /// Tüm bağımlılıklar [Inject] ile DI'dan çözülür — 13 parametreli LoadLevel
    /// metodu artık sadece LoadLevelSignal alır.
    /// </summary>
    public interface ILevelLoaderService
    {
        void LoadLevel(LoadLevelSignal signal);
    }

    public class LevelLoaderService : ILevelLoaderService, INexusService
    {
        [Inject] public IGridModel GridModel { get; set; }
        [Inject] public ILevelModel LevelModel { get; set; }
        [Inject] public IGameSessionModel GameSessionModel { get; set; }
        [Inject] public IHintModel HintModel { get; set; }
        [Inject] public IGameHistoryService HistoryService { get; set; }
        [Inject] public IObstacleService ObstacleService { get; set; }
        [Inject] public ITutorialDriver TutorialDriver { get; set; }
        [Inject] public ISignalBus SignalBus { get; set; }
        [Inject] public IGameStateModel GameStateModel { get; set; }
        [Inject] public ISaveThrottler SaveThrottler { get; set; }
        [Inject] public IPlayerPrefsService PlayerPrefsService { get; set; }
        [Inject] public ILoggerService LoggerService { get; set; }
        [Inject, OptionalInject] public EconomyConfigAsset EconomyConfig { get; set; }

        public ValueTask InitializeAsync(CancellationToken ct) => default;
        public void OnDispose() { }

        public void LoadLevel(LoadLevelSignal signal)
        {
            if (signal.LevelToLoad == null)
            {
                LoggerService?.LogError("[PixelFlow.LevelLoaderService] ERROR: LoadLevelSignal received with null LevelToLoad.");
                return;
            }

            var ld = signal.LevelToLoad;
            int nodeCount = ld.initialNodes?.Count ?? 0;
            int bridgeCount = ld.bridgePositions?.Count ?? 0;
            int obstacleCount = ld.obstacles?.Count ?? 0;

            LoggerService?.Log($"[PixelFlow.LevelLoaderService] ▶ Loading Level {ld.levelIndex + 1} ({ld.name}, Grid: {ld.width}x{ld.height}, Nodes: {nodeCount}, Bridges: {bridgeCount}, Obstacles: {obstacleCount})");

            LevelModel.SetLevel(ld);
            GridModel.Initialize(ld.width, ld.height);

            // Place Initial Nodes
            if (ld.initialNodes != null)
            {
                foreach (var node in ld.initialNodes)
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

            // Place Bridges (Pre-placed viaducts)
            if (ld.bridgePositions != null)
            {
                foreach (var bridgePos in ld.bridgePositions)
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

            // Place Obstacles
            if (ld.obstacles != null)
            {
                foreach (var obs in ld.obstacles)
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

            // Place OneWay cells (drawable cells with directional constraint)
            if (ld.oneWayCells != null)
            {
                foreach (var owc in ld.oneWayCells)
                {
                    if (owc.position.x >= 0 && owc.position.x < GridModel.Width &&
                        owc.position.y >= 0 && owc.position.y < GridModel.Height)
                    {
                        var cell = GridModel.Grid[owc.position.x, owc.position.y];
                        cell.State = CellState.Empty;
                        cell.ObstacleType = ObstacleType.OneWay;
                    }
                }
            }

            // Clear history for fresh level
            HistoryService.Clear();

            // Session setup with viaduct bonus (GDD §9 — EconomyConfigAsset)
            int viaductBonus = EconomyConfig != null
                ? EconomyConfig.CalculateViaductBonus(ld.levelIndex)
                : ld.levelIndex / 10;
            int totalViaducts = ld.viaductLimit + viaductBonus;
            GameSessionModel.StartSession(ld.levelIndex, totalViaducts, ld.flowScoreThreshold, true);

            HintModel.ResetSessionHints();
            ObstacleService?.InitializeFromLevel(ld);
            TutorialDriver?.OnLevelLoaded(ld.levelIndex);

            // Finalize
            SignalBus.Fire(new GridUpdatedSignal());
            GameStateModel.SetState(GameState.Playing);
            SaveThrottler?.TryRequestSave(() => GridStateSerializer.Save(GridModel, GameSessionModel, LevelModel, PlayerPrefsService));

            LoggerService?.Log($"[PixelFlow.LevelLoaderService] ✔ Level {ld.levelIndex + 1} loaded successfully.");
        }
    }
}
