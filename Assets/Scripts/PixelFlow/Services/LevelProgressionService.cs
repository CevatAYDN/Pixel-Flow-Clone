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
    /// GDD §3.6: Phase konfigürasyonu PhaseDefinitionAsset ScriptableObject'ten
    /// okunur — editör tarafından data-driven yönetilir.
    /// </summary>
    public sealed class LevelProgressionService : ILevelProgressionService, INexusService
    {
        private readonly ProceduralLevelGenerator _generator;
        private readonly Dictionary<int, LevelData> _generatedCache;
        private PhaseConfigAsset _phaseConfig;

        public int LevelsPerDifficulty => 5;

        [Inject]
        public LevelProgressionService() : this(new ProceduralLevelGenerator(new RuntimePathSolver()))
        {
            // GDD §3.6: Resources'tan PhaseConfigAsset yükle (editörde oluşturulmuş olmalı)
            _phaseConfig = Resources.Load<PhaseConfigAsset>("PhaseConfig");
        }

        internal LevelProgressionService(ProceduralLevelGenerator generator)
        {
            _generator = generator;
            _generatedCache = new Dictionary<int, LevelData>();
        }

        public DifficultyParams GetDifficultyForLevel(int levelIndex)
        {
            // GDD §3.6: Önce ScriptableObject asset, yoksa struct fallback
            PhaseDefinition phase;
            if (_phaseConfig != null)
            {
                var phaseAsset = _phaseConfig.GetPhaseForLevel(levelIndex);
                if (phaseAsset != null)
                    phase = phaseAsset.ToStruct();
                else
                    phase = PhaseDefinition.GetPhaseForLevel(levelIndex);
            }
            else
            {
                phase = PhaseDefinition.GetPhaseForLevel(levelIndex);
            }

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
