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
            int seed = (DailyCrisisModel != null ? DailyCrisisModel.CurrentDailySeed : 20260705) + crisisIndex * 777;
            var solver = new RuntimePathSolver();
            var generator = new ProceduralLevelGenerator(solver, seed);

            // game_plan.md §15.9 KURAL 1/4: Zorluk değerleri hardcode edilmez, GameConfig'ten okunur.
            if (Config == null)
                throw new DataValidationException("GameConfig erişilemedi! DailyCrisisService zorluk parametrelerini yükleyemiyor.");

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
