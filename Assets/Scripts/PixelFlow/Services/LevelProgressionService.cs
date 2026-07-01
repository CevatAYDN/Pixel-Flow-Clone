using System.Collections.Generic;
using Nexus.Core;
using PixelFlow.Data;
using System.Threading;
using System.Threading.Tasks;

namespace PixelFlow.Services
{
    public interface ILevelProgressionService
    {
        LevelData GetOrGenerateLevel(int levelIndex);
        DifficultyParams GetDifficultyForLevel(int levelIndex);
        int LevelsPerDifficulty { get; }
    }

    /// <summary>
    /// Zorluk progresyon algoritması. Level index'ine göre zorluk parametrelerini
    /// belirler ve gerekirse ProceduralLevelGenerator ile level üretir.
    /// 
    /// Progresyon eğrisi (her kademede LevelsPerDifficulty level):
    ///   Easy    (1-3)   : 5x5,  3 renk, 0 bridge
    ///   Medium  (4-7)   : 6x6,  4 renk, 1 bridge
    ///   Hard    (8-12)  : 7x7,  5 renk, 2 bridge
    ///   Expert  (13-18) : 8x8,  6 renk, 3 bridge
    ///   Master  (19+)   : 10x10, 8 renk, 4 bridge
    /// </summary>
    public sealed class LevelProgressionService : ILevelProgressionService, INexusService
    {
        private readonly ProceduralLevelGenerator _generator;
        private readonly Dictionary<int, LevelData> _generatedCache;
        private readonly List<DifficultyParams> _difficultyCurve;

        public int LevelsPerDifficulty => 5;

        [Inject]
        public LevelProgressionService() : this(new ProceduralLevelGenerator(new RuntimePathSolver())) { }

        internal LevelProgressionService(ProceduralLevelGenerator generator)
        {
            _generator = generator;
            _generatedCache = new Dictionary<int, LevelData>();

            _difficultyCurve = new List<DifficultyParams>
            {
                DifficultyParams.Easy,     //  0-4   (index 1-5)
                DifficultyParams.Medium,   //  5-9   (index 6-10)
                DifficultyParams.Hard,     // 10-14  (index 11-15)
                DifficultyParams.Expert,   // 15-19  (index 16-20)
                DifficultyParams.Master,   // 20+    (index 21+)
            };
        }

        public DifficultyParams GetDifficultyForLevel(int levelIndex)
        {
            int tier = levelIndex / LevelsPerDifficulty;
            if (tier >= _difficultyCurve.Count)
                tier = _difficultyCurve.Count - 1;
            return _difficultyCurve[tier];
        }

        public LevelData GetOrGenerateLevel(int levelIndex)
        {
            if (_generatedCache.TryGetValue(levelIndex, out var cached))
                return cached;

            var param = GetDifficultyForLevel(levelIndex);
            var level = _generator.Generate(param);
            if (level != null)
            {
                level.levelIndex = levelIndex;
                _generatedCache[levelIndex] = level;
            }

                return level;
        }

        public ValueTask InitializeAsync(CancellationToken ct) => default;
        public void OnDispose() { _generatedCache.Clear(); }
    }
}
