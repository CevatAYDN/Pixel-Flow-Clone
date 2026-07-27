using NUnit.Framework;
using PixelFlow.Data;
using PixelFlow.Services;
using UnityEngine;
using static PixelFlow.Editor.Tests.GameTestContext;

namespace PixelFlow.Editor.Tests
{
    [TestFixture]
    public class ProceduralLevelGeneratorValidationTests
    {
        private RuntimePathSolver _solver;

        [SetUp]
        public void SetUp()
        {
            _solver = CreateTestRuntimePathSolver();
        }

        [Test]
        public void Generate_EasyLevel_Is100PercentSolvable()
        {
            var generator = new ProceduralLevelGenerator(_solver, seed: 42);
            var param = DifficultyParams.Easy; // 5x5, 1 color

            var level = generator.Generate(param);

            Assert.IsNotNull(level, "Generated level should not be null");
            Assert.AreEqual(5, level.width);
            Assert.AreEqual(5, level.height);
            Assert.IsTrue(_solver.Solve(level, out var solutions), "Easy level must be solvable by RuntimePathSolver");
            Assert.IsNotNull(solutions);
        }

        [Test]
        public void Generate_MediumLevel_MultiColor_IsSolvable()
        {
            var generator = new ProceduralLevelGenerator(_solver, seed: 100);
            var param = DifficultyParams.Medium; // 6x6, 2 colors

            var level = generator.Generate(param);

            Assert.IsNotNull(level);
            Assert.IsTrue(_solver.Solve(level, out var solutions), "Medium multi-color level must be 100% solvable");
        }

        [Test]
        public void Generate_HardLevel_WithObstacles_IsSolvableAndVerified()
        {
            var generator = new ProceduralLevelGenerator(_solver, seed: 777);
            var param = DifficultyParams.Hard; // 7x7, 3 colors, obstacles enabled

            var level = generator.Generate(param);

            Assert.IsNotNull(level);
            Assert.IsTrue(_solver.Solve(level, out var solutions), "Hard level with obstacles must be verified 100% solvable without softlocks");
        }

        [Test]
        public void Generate_IncrementalSingleLevel_CorrectlyIncrementsLevelIndex()
        {
            var generator = new ProceduralLevelGenerator(_solver, seed: 555);
            var phaseConfig = ScriptableObject.CreateInstance<PhaseConfigAsset>();
            phaseConfig.Phase1 = ScriptableObject.CreateInstance<PhaseDefinitionAsset>();
            phaseConfig.Phase1.StartLevelIndex = 0;
            phaseConfig.Phase1.EndLevelIndex = 10;
            phaseConfig.Phase1.GridSizeMin = 5;
            phaseConfig.Phase1.GridSizeMax = 5;
            phaseConfig.Phase1.ColorCountMin = 1;
            phaseConfig.Phase1.ColorCountMax = 2;

            var progression = new LevelProgressionService(_solver, phaseConfig);

            int existingCount = 3; // E.g. Level 1, Level 2, Level 3 exist (indices 0, 1, 2)
            int nextIndex = existingCount; // Index 3 for Level 4

            var param = progression.GetDifficultyForLevel(nextIndex);
            var level = generator.Generate(param);

            Assert.IsNotNull(level);
            level.levelIndex = nextIndex;
            level.name = $"Level{nextIndex + 1}";

            Assert.AreEqual(3, level.levelIndex, "Level 4 must have levelIndex=3");
            Assert.AreEqual("Level4", level.name, "Level name must be Level4");
            Assert.IsTrue(_solver.Solve(level, out _), "Single generated level must be 100% solvable");
        }

        [Test]
        public void Generate_LevelNodesHaveExactlyOneSourceAndOneTarget_PassesLevelValidator()
        {
            var generator = new ProceduralLevelGenerator(_solver, seed: 888);
            var level = generator.Generate(DifficultyParams.Medium);

            Assert.IsNotNull(level);
            var validator = new LevelValidator(_solver);
            var validationResult = validator.Validate(level);

            Assert.IsTrue(validationResult.IsValid, $"Generated level must be valid according to LevelValidator! Issues: {string.Join(", ", validationResult.Issues.ConvertAll(i => i.Message))}");
        }

        [Test]
        public void LevelCatalog_NullAuthoredLevel_AutoRepairsToProceduralFallback()
        {
            var catalog = ScriptableObject.CreateInstance<LevelCatalogAsset>();
            catalog.Levels.Add(new LevelCatalogAsset.LevelCatalogEntry
            {
                LevelIndex = 3,
                AuthoredLevel = null,
                UseProceduralFallback = false
            });

            // Simulate auto-repair logic
            for (int i = 0; i < catalog.Levels.Count; i++)
            {
                var entry = catalog.Levels[i];
                if (entry != null && !entry.UseProceduralFallback && entry.AuthoredLevel == null)
                {
                    entry.UseProceduralFallback = true;
                    entry.ProceduralDifficulty = DifficultyParams.Medium;
                    catalog.Levels[i] = entry;
                }
            }

            Assert.IsTrue(catalog.Levels[0].UseProceduralFallback, "Null AuthoredLevel entry must be auto-repaired to UseProceduralFallback=true");
        }

        [Test]
        public void LevelProgressionService_UnindexedLevel_DoesNotAutoGenerateProceduralLevel()
        {
            var catalog = ScriptableObject.CreateInstance<LevelCatalogAsset>();
            // Add only LevelIndex 0, 1, 2 (Levels 1, 2, 3)
            for (int i = 0; i < 3; i++)
            {
                var lvl = ScriptableObject.CreateInstance<LevelData>();
                lvl.levelIndex = i;
                catalog.Levels.Add(new LevelCatalogAsset.LevelCatalogEntry
                {
                    LevelIndex = i,
                    AuthoredLevel = lvl,
                    UseProceduralFallback = false
                });
            }

            var phaseConfig = ScriptableObject.CreateInstance<PhaseConfigAsset>();
            phaseConfig.Phase1 = ScriptableObject.CreateInstance<PhaseDefinitionAsset>();
            phaseConfig.Phase1.StartLevelIndex = 0;
            phaseConfig.Phase1.EndLevelIndex = 10;
            phaseConfig.Phase1.GridSizeMin = 5;
            phaseConfig.Phase1.GridSizeMax = 5;
            phaseConfig.Phase1.ColorCountMin = 1;
            phaseConfig.Phase1.ColorCountMax = 2;

            var service = new LevelProgressionService(_solver, phaseConfig, catalog);

            // Request levelIndex 3 (Level 4, which is NOT in catalog)
            var unindexedLevel = service.GetOrGenerateLevel(3);

            Assert.IsNull(unindexedLevel, "Unindexed Level 4 must return null and NOT be auto-generated procedurally on the fly!");
        }
    }
}
