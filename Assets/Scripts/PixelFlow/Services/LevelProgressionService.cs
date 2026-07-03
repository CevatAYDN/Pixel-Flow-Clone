using System.Collections.Generic;
using Nexus.Core;
using PixelFlow.Data;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

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

        public int LevelsPerDifficulty => 5;

        [Inject]
        public LevelProgressionService() : this(new ProceduralLevelGenerator(new RuntimePathSolver())) { }

        internal LevelProgressionService(ProceduralLevelGenerator generator)
        {
            _generator = generator;
            _generatedCache = new Dictionary<int, LevelData>();
        }

        public DifficultyParams GetDifficultyForLevel(int levelIndex)
        {
            // GDD §3.5: 4 faz progresyon eğrisi.
            // Faz 1: 0-11 (Seviye 1-12), 5×5→6×6, 1-2 renk, kaza yok, viyadük yok
            // Faz 2: 12-27 (Seviye 13-28), 7×7, 2-3 renk, kaza+viyadük (3 hak)
            // Faz 3: 28-44 (Seviye 29-45), 8-9×8-9, 3-4 renk, engeller, full coverage
            // Faz 4: 45-59 (Seviye 46-60), 10×10, 4-5 renk, tüm engeller
            var phase = PhaseDefinition.GetPhaseForLevel(levelIndex);
            return PhaseToDifficulty(phase, levelIndex);
        }

        private static DifficultyParams PhaseToDifficulty(PhaseDefinition phase, int levelIndex)
        {
            int span = phase.EndLevelIndex - phase.StartLevelIndex + 1;
            float progress = span > 0 ? (float)(levelIndex - phase.StartLevelIndex) / span : 0f;

            int gridSize = Mathf.RoundToInt(Mathf.Lerp(phase.GridSizeMin, phase.GridSizeMax, progress));
            int colorCount = Mathf.RoundToInt(Mathf.Lerp(phase.ColorCountMin, phase.ColorCountMax, progress));
            int bridgeCount = Mathf.RoundToInt(Mathf.Lerp(phase.BridgeCountMin, phase.BridgeCountMax, progress));

            return new DifficultyParams(
                gridSize, gridSize, colorCount, bridgeCount,
                phase.RequireFullCoverage,
                phase.ObstaclesEnabled,
                phase.FerryEnabled,
                phase.NarrowPassEnabled);
        }

        public LevelData GetOrGenerateLevel(int levelIndex)
        {
            if (_generatedCache.TryGetValue(levelIndex, out var cached))
                return cached;

            LevelData level = Resources.Load<LevelData>($"Levels/Level{levelIndex + 1}");
            if (level == null)
            {
                level = Resources.Load<LevelData>($"Levels/Level{levelIndex}");
            }
            if (level == null)
            {
                var allLevels = Resources.LoadAll<LevelData>("Levels");
                foreach (var l in allLevels)
                {
                    if (l != null && l.levelIndex == levelIndex)
                    {
                        level = l;
                        break;
                    }
                }
            }
            if (level == null)
            {
                var packs = Resources.LoadAll<LevelPack>("Levels");
                if (packs != null)
                {
                    foreach (var pack in packs)
                    {
                        if (pack != null && pack.levels != null)
                        {
                            foreach (var l in pack.levels)
                            {
                                if (l != null && l.levelIndex == levelIndex)
                                {
                                    level = l;
                                    break;
                                }
                            }
                        }
                        if (level != null) break;
                    }
                }
            }

            if (level != null)
            {
                Debug.Log($"[PixelFlow.LevelProgressionService] Loaded handcrafted LevelData asset for index {levelIndex}: '{level.name}' ({level.width}x{level.height})");
            }

            if (level == null)
            {
                Debug.Log($"[PixelFlow.LevelProgressionService] No handcrafted LevelData asset found for index {levelIndex}. Generating procedurally...");
                var param = GetDifficultyForLevel(levelIndex);
                level = _generator.Generate(param);
                if (level != null)
                {
                    level.levelIndex = levelIndex;
                    Debug.Log($"[PixelFlow.LevelProgressionService] Procedurally generated Level {levelIndex + 1} ({param.gridWidth}x{param.gridHeight}, {param.colorCount} colors).");
                }
                else
                {
                    Debug.LogError($"[PixelFlow.LevelProgressionService] ERROR: Procedural generator failed to generate level for index {levelIndex}.");
                }
            }

            if (level != null)
            {
                _generatedCache[levelIndex] = level;
            }

            return level;
        }

        public ValueTask InitializeAsync(CancellationToken ct) => default;
        public void OnDispose() { _generatedCache.Clear(); }
    }
}
