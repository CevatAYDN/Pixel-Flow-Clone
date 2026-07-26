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
        [Inject, OptionalInject] public GameConfig Config { get; set; }

        public ValueTask InitializeAsync(CancellationToken ct) => default;
        public void OnDispose() { }

        public LevelData GenerateDailyCrisisLevel(int crisisIndex)
        {
            LoggerService?.Log($"[PixelFlow.DailyCrisisService] Generating daily crisis level for crisisIndex: {crisisIndex}.");

            if (DailyCrisisModel == null)
                throw new DataValidationException("DailyCrisisModel is null in DailyCrisisService!");

            if (Config == null)
                throw new DataValidationException("GameConfig is null in DailyCrisisService!");

            int seedBase = Config.DailyCrisisSeedBase > 0 ? Config.DailyCrisisSeedBase : DailyCrisisModel.CurrentDailySeed;
            int seed = seedBase + crisisIndex * Config.DailyCrisisSeedFactor;
            var solver = new RuntimePathSolver { Config = Config };
            var generator = new ProceduralLevelGenerator(solver, seed);

            DifficultyParams param;
            switch (crisisIndex)
            {
                case 0: // Kolay kriz
                    param = Config.DailyCrisisEasy;
                    break;
                case 1: // Orta kriz
                    param = Config.DailyCrisisMedium;
                    break;
                case 2: // Zor kriz
                default:
                    param = Config.DailyCrisisHard;
                    break;
            }

            LoggerService?.Log($"[PixelFlow.DailyCrisisService] Parameters resolved: Width={param.gridWidth}, Height={param.gridHeight}, Colors={param.colorCount}, Bridges={param.bridgeCount}, FullCoverage={param.requireFullGridCoverage}. Starting generator...");

            var level = generator.Generate(param);
            if (level != null)
            {
                level.levelIndex = Config.DailyCrisisLevelIndexOffset + crisisIndex;
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
