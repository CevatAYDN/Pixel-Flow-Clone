using System.Threading;
using System.Threading.Tasks;
using Nexus.Core;
using Nexus.Core.Services;
using PixelFlow.Data;
using PixelFlow.Models;

namespace PixelFlow.Services
{
    public interface IDailyCrisisService
    {
        LevelData GenerateDailyCrisisLevel(int crisisIndex);
    }

    public class DailyCrisisService : IDailyCrisisService, INexusService
    {
        [Inject] public IDailyCrisisModel DailyCrisisModel { get; set; }
        [Inject] public ILoggerService LoggerService { get; set; }

        public ValueTask InitializeAsync(CancellationToken ct) => default;
        public void OnDispose() { }

        public LevelData GenerateDailyCrisisLevel(int crisisIndex)
        {
            LoggerService?.Log($"[PixelFlow.DailyCrisisService] Generating daily crisis level for crisisIndex: {crisisIndex}.");
            int seed = (DailyCrisisModel != null ? DailyCrisisModel.CurrentDailySeed : 20260705) + crisisIndex * 777;
            var solver = new RuntimePathSolver();
            var generator = new ProceduralLevelGenerator(solver, seed);

            DifficultyParams param;
            switch (crisisIndex)
            {
                case 0: // Easy crisis (10x10, 3 colors, 2 bridges)
                    param = DifficultyParams.Phase3_Default;
                    param.gridWidth = 10;
                    param.gridHeight = 10;
                    param.colorCount = 3;
                    param.bridgeCount = 2;
                    param.requireFullGridCoverage = false;
                    break;
                case 1: // Medium crisis (10x10, 4 colors, 3 bridges)
                    param = DifficultyParams.Phase3_Default;
                    param.gridWidth = 10;
                    param.gridHeight = 10;
                    param.colorCount = 4;
                    param.bridgeCount = 3;
                    param.requireFullGridCoverage = false;
                    break;
                case 2: // Hard crisis (10x10, 4 colors, 4 bridges, full coverage)
                default:
                    param = DifficultyParams.Phase4_Endgame;
                    param.gridWidth = 10;
                    param.gridHeight = 10;
                    param.colorCount = 4;
                    param.bridgeCount = 4;
                    param.requireFullGridCoverage = true;
                    break;
            }

            LoggerService?.Log($"[PixelFlow.DailyCrisisService] Parameters resolved: Width={param.gridWidth}, Height={param.gridHeight}, Colors={param.colorCount}, Bridges={param.bridgeCount}, FullCoverage={param.requireFullGridCoverage}. Starting generator...");

            var level = generator.Generate(param);
            if (level != null)
            {
                level.levelIndex = 900 + crisisIndex;
                level.name = $"DailyCrisis_{crisisIndex + 1}";
                LoggerService?.Log($"[PixelFlow.DailyCrisisService] Generated level successfully: index={level.levelIndex}, name={level.name}.");
            }
            else
            {
                LoggerService?.LogError($"[PixelFlow.DailyCrisisService] Failed to generate daily crisis level for crisisIndex: {crisisIndex}.");
            }
            return level;
        }
    }
}
