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
    /// </summary>
    public interface ILevelLoaderService
    {
        void LoadLevel(LoadLevelSignal signal, IGridModel grid, ILevelModel level,
            IGameSessionModel session, IHintModel hints, IGameHistoryService history,
            IObstacleService obstacle, ITutorialDriver tutorial,
            ISignalBus signalBus, IGameStateModel state,
            ISaveThrottler saveThrottler, IPlayerPrefsService prefs,
            ILoggerService logger);
    }

    public class LevelLoaderService : ILevelLoaderService, INexusService
    {
        public ValueTask InitializeAsync(CancellationToken ct) => default;
        public void OnDispose() { }

        public void LoadLevel(LoadLevelSignal signal,
            IGridModel gridModel, ILevelModel levelModel,
            IGameSessionModel sessionModel, IHintModel hintModel,
            IGameHistoryService history, IObstacleService obstacle,
            ITutorialDriver tutorial, ISignalBus signalBus,
            IGameStateModel state, ISaveThrottler saveThrottler,
            IPlayerPrefsService prefs, ILoggerService logger)
        {
            if (signal.LevelToLoad == null)
            {
                logger?.LogError("[PixelFlow.LevelLoaderService] ERROR: LoadLevelSignal received with null LevelToLoad.");
                return;
            }

            var ld = signal.LevelToLoad;
            int nodeCount = ld.initialNodes?.Count ?? 0;
            int bridgeCount = ld.bridgePositions?.Count ?? 0;
            int obstacleCount = ld.obstacles?.Count ?? 0;

            logger?.Log($"[PixelFlow.LevelLoaderService] ▶ Loading Level {ld.levelIndex + 1} ({ld.name}, Grid: {ld.width}x{ld.height}, Nodes: {nodeCount}, Bridges: {bridgeCount}, Obstacles: {obstacleCount})");

            levelModel.SetLevel(ld);
            gridModel.Initialize(ld.width, ld.height);

            // Place Initial Nodes
            if (ld.initialNodes != null)
            {
                foreach (var node in ld.initialNodes)
                {
                    if (node.position.x >= 0 && node.position.x < gridModel.Width &&
                        node.position.y >= 0 && node.position.y < gridModel.Height)
                    {
                        var cell = gridModel.Grid[node.position.x, node.position.y];
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
                    if (bridgePos.x >= 0 && bridgePos.x < gridModel.Width &&
                        bridgePos.y >= 0 && bridgePos.y < gridModel.Height)
                    {
                        var cell = gridModel.Grid[bridgePos.x, bridgePos.y];
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
                    if (obs.position.x >= 0 && obs.position.x < gridModel.Width &&
                        obs.position.y >= 0 && obs.position.y < gridModel.Height)
                    {
                        var cell = gridModel.Grid[obs.position.x, obs.position.y];
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
                    if (owc.position.x >= 0 && owc.position.x < gridModel.Width &&
                        owc.position.y >= 0 && owc.position.y < gridModel.Height)
                    {
                        var cell = gridModel.Grid[owc.position.x, owc.position.y];
                        cell.State = CellState.Empty;
                        cell.ObstacleType = ObstacleType.OneWay;
                    }
                }
            }

            // Clear history for fresh level
            history.Clear();

            // Session setup with viaduct bonus
            int viaductBonus = ld.levelIndex / 10;
            int totalViaducts = ld.viaductLimit + viaductBonus;
            sessionModel.StartSession(ld.levelIndex, totalViaducts, ld.flowScoreThreshold, true);

            hintModel.ResetSessionHints();
            obstacle?.InitializeFromLevel(ld);
            tutorial?.OnLevelLoaded(ld.levelIndex);

            // Finalize
            signalBus.Fire(new GridUpdatedSignal());
            state.SetState(GameState.Playing);
            saveThrottler?.TryRequestSave(() => GridStateSerializer.Save(gridModel, sessionModel, levelModel, prefs));

            logger?.Log($"[PixelFlow.LevelLoaderService] ✔ Level {ld.levelIndex + 1} loaded successfully.");
        }
    }
}