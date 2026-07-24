using System.Collections.Generic;
using Nexus.Core;
using Nexus.Core.Services;
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
    /// GDD §3.6: Phase konfigürasyonu PhaseDefinitionAsset ScriptableObject'ten
    /// okunur — editör tarafından data-driven yönetilir.
    /// 
    /// Level çözümleme: Önce LevelCatalogAsset'e bakar, bulamazsa eski Resources.Load
    /// zincirine düşer (geriye uyumluluk).
    /// </summary>
    public sealed class LevelProgressionService : ILevelProgressionService, INexusService
    {
        private readonly ProceduralLevelGenerator _generator;
        private readonly Dictionary<int, LevelData> _generatedCache;
        private PhaseConfigAsset _phaseConfig;
        private LevelCatalogAsset _levelCatalog;
        private readonly ILoggerService _logger;

        public int LevelsPerDifficulty => 5;

        [Inject]
        public LevelProgressionService(IPathSolver pathSolver,
            [OptionalInject] PhaseConfigAsset phaseConfig = null,
            [OptionalInject] LevelCatalogAsset levelCatalog = null)
        {
            _generator = new ProceduralLevelGenerator(pathSolver);
            _generatedCache = new Dictionary<int, LevelData>();
            _logger = NexusRuntime.Logger;
            // game_plan.md §15.9 KURAL 7: Config asset'ler DI üzerinden gelir,
            // Resources.Load kullanılmaz. Null ise editörde henüz oluşturulmamış demektir.
            _phaseConfig = phaseConfig;
            _levelCatalog = levelCatalog;
        }

        // Eski overload — geriye uyumluluk (testler için)
        internal LevelProgressionService(ProceduralLevelGenerator generator) : this(generator, null) { }

        internal LevelProgressionService(ProceduralLevelGenerator generator, ILoggerService logger)
        {
            _generator = generator;
            _generatedCache = new Dictionary<int, LevelData>();
            _logger = logger ?? NexusRuntime.Logger;
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

            int gridSize = UnityEngine.Mathf.RoundToInt(UnityEngine.Mathf.Lerp(phase.GridSizeMin, phase.GridSizeMax, progress));
            int colorCount = UnityEngine.Mathf.RoundToInt(UnityEngine.Mathf.Lerp(phase.ColorCountMin, phase.ColorCountMax, progress));
            int bridgeCount = UnityEngine.Mathf.RoundToInt(UnityEngine.Mathf.Lerp(phase.BridgeCountMin, phase.BridgeCountMax, progress));

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

            LevelData level = null;

            // GDD §3.6: Önce LevelCatalogAsset'e bak
            if (_levelCatalog != null)
            {
                level = _levelCatalog.GetAuthoredLevel(levelIndex);
            }

            // Fallback: eski Resources.Load zinciri (katalogda yok veya asset null)
            if (level == null)
            {
                level = UnityEngine.Resources.Load<LevelData>($"Levels/Level{levelIndex + 1}");
            }
            if (level == null)
            {
                level = UnityEngine.Resources.Load<LevelData>($"Levels/Level{levelIndex}");
            }
            if (level == null)
            {
                var allLevels = UnityEngine.Resources.LoadAll<LevelData>("Levels");
                if (allLevels != null)
                {
                    foreach (var l in allLevels)
                    {
                        if (l != null && l.levelIndex == levelIndex)
                        {
                            level = l;
                            break;
                        }
                    }
                }
            }
            if (level == null)
            {
                var packs = UnityEngine.Resources.LoadAll<LevelPack>("Levels");
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
                _logger?.Log($"[PixelFlow.LevelProgressionService] Loaded handcrafted LevelData asset for index {levelIndex}: '{level.name}' ({level.width}x{level.height})");
            }

            if (level == null)
            {
                // Önce katalogda procedural parametre var mı kontrol et
                DifficultyParams param;
                if (_levelCatalog != null && _levelCatalog.TryGetProceduralParams(levelIndex, out param))
                {
                    _logger?.Log($"[PixelFlow.LevelProgressionService] Generating level {levelIndex} with catalog-defined procedural params.");
                }
                else
                {
                    param = GetDifficultyForLevel(levelIndex);
                }

                _logger?.Log($"[PixelFlow.LevelProgressionService] No handcrafted LevelData asset found for index {levelIndex}. Generating procedurally...");
                level = _generator.Generate(param);
                if (level != null)
                {
                    level.levelIndex = levelIndex;
                    _logger?.Log($"[PixelFlow.LevelProgressionService] Procedurally generated Level {levelIndex + 1} ({param.gridWidth}x{param.gridHeight}, {param.colorCount} colors).");
                }
                else
                {
                    _logger?.LogError($"[PixelFlow.LevelProgressionService] ERROR: Procedural generator failed to generate level for index {levelIndex}.");
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
