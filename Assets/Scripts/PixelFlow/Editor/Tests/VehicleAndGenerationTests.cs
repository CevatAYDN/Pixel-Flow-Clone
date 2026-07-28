using NUnit.Framework;
using Nexus.Core;
using PixelFlow.Data;
using PixelFlow.Models;
using PixelFlow.Services;
using PixelFlow.Signals;
using PixelFlow.Views;
using System.Collections.Generic;
using UnityEngine;
using static PixelFlow.Editor.Tests.GameTestContext;

namespace PixelFlow.Editor.Tests
{
    /// <summary>
    /// VehicleVisualFactory ve VehiclePartPool test'leri.
    /// Procedural araç üretimi, config-driven parametreler, pool yönetimi.
    /// </summary>
    [TestFixture]
    public class VehicleVisualFactoryTests
    {
        private NexusTestContext _ctx;
        private VehicleMaterialConfigAsset _materialConfig;
        private VehicleVisualConfigAsset _visualConfig;

        [SetUp]
        public void SetUp()
        {
            _ctx = CreateGameContext();

            // Test için config oluştur
            _materialConfig = ScriptableObject.CreateInstance<VehicleMaterialConfigAsset>();
            _materialConfig.name = "TestVehicleMaterialConfig";

            _visualConfig = ScriptableObject.CreateInstance<VehicleVisualConfigAsset>();
            _visualConfig.name = "TestVehicleVisualConfig";

            // VehicleVisualFactory'ı test config ile başlat
            VehicleVisualFactory.Initialize(_materialConfig, _visualConfig);

            // VehiclePartPool'u test config ile başlat
            var gameConfig = ScriptableObject.CreateInstance<GameConfig>();
            gameConfig.VehiclePartPoolCubes = 64;
            gameConfig.VehiclePartPoolCylinders = 32;
            VehiclePartPool.SetConfig(gameConfig);
        }

        [TearDown]
        public void TearDown()
        {
            _ctx?.Dispose();
            _ctx = null;
        }

        [Test]
        public void Initialize_WithNullConfig_DoesNotThrow()
        {
            // Null config ile initialize — hata vermemeli
            Assert.DoesNotThrow(() => VehicleVisualFactory.Initialize(null, null));
        }

        [Test]
        public void Initialize_WithConfig_CachesConfig()
        {
            // Config ile initialize — config cache'de olmalı
            Assert.DoesNotThrow(() => VehicleVisualFactory.Initialize(_materialConfig, _visualConfig));
        }

        [Test]
        public void CreateCar3D_WithNullVisualConfig_ThrowsDataValidationException()
        {
            VehicleVisualFactory.Initialize(_materialConfig, null);
            var root = new GameObject("TestCarRoot");
            Assert.Throws<PixelFlow.Data.DataValidationException>(() => VehicleVisualFactory.CreateCar3D(root, ColorType.Red));
            Object.DestroyImmediate(root);
        }

        [Test]
        public void ApplyColorToRenderers_WithNullRenderer_DoesNotThrow()
        {
            // Null renderer array ile — hata vermemeli
            var mpb = new MaterialPropertyBlock();
            Assert.DoesNotThrow(() => VehicleVisualFactory.ApplyColorToRenderers(ColorType.Red, null, mpb));
        }

        [Test]
        public void ApplyColorToRenderers_WithNullMpb_DoesNotThrow()
        {
            // Null mpb ile — hata vermemeli
            Assert.DoesNotThrow(() => VehicleVisualFactory.ApplyColorToRenderers(ColorType.Red, new Renderer[0], null));
        }

        [Test]
        public void RecycleVehicle_WithNullRoot_DoesNotThrow()
        {
            // Null root ile — hata vermemeli
            Assert.DoesNotThrow(() => VehicleVisualFactory.RecycleVehicle(null));
        }

        [Test]
        public void GetColor_ReturnsValidColor_ForAllTypes()
        {
            // Tüm ColorType'lar için geçerli renk döndürmeli
            foreach (ColorType color in System.Enum.GetValues(typeof(ColorType)))
            {
                if (color == ColorType.None) continue;

                var c = CellView.GetColor(color);
                Assert.GreaterOrEqual(c.r, 0f, $"ColorType.{color}.r >= 0");
                Assert.LessOrEqual(c.r, 1f, $"ColorType.{color}.r <= 1");
                Assert.GreaterOrEqual(c.g, 0f, $"ColorType.{color}.g >= 0");
                Assert.LessOrEqual(c.g, 1f, $"ColorType.{color}.g <= 1");
                Assert.GreaterOrEqual(c.b, 0f, $"ColorType.{color}.b >= 0");
                Assert.LessOrEqual(c.b, 1f, $"ColorType.{color}.b <= 1");
                Assert.AreEqual(1f, c.a, $"ColorType.{color}.a == 1");
            }
        }

        [Test]
        public void VehiclePartPool_SetConfig_PersistsConfig()
        {
            // Config ayarlandıktan sonra pool config'i korumalı
            var config = ScriptableObject.CreateInstance<GameConfig>();
            config.VehiclePartPoolCubes = 128;
            config.VehiclePartPoolCylinders = 64;

            VehiclePartPool.SetConfig(config);

            // Pool initialize edildiğinde config değerlerini kullanmalı
            Assert.DoesNotThrow(() => VehiclePartPool.Initialize());
        }

        [Test]
        public void VehicleVisualConfigAsset_DefaultValues_Reasonable()
        {
            // Varsayılan değerler makul olmalı
            var asset = ScriptableObject.CreateInstance<VehicleVisualConfigAsset>();

            Assert.Greater(asset.CarBodySize.x, 0f, "CarBodySize.x > 0");
            Assert.Greater(asset.CarBodySize.y, 0f, "CarBodySize.y > 0");
            Assert.Greater(asset.CarBodySize.z, 0f, "CarBodySize.z > 0");

            Assert.Greater(asset.TrainBodySize.x, 0f, "TrainBodySize.x > 0");
            Assert.Greater(asset.TrainBodySize.y, 0f, "TrainBodySize.y > 0");
            Assert.Greater(asset.TrainBodySize.z, 0f, "TrainBodySize.z > 0");

            Assert.Greater(asset.CarWheelXPositions.Count, 0, "CarWheelXPositions boş olmamalı");
            Assert.Greater(asset.TrainLocoWheelPositions.Count, 0, "TrainLocoWheelPositions boş olmamalı");
        }

        [Test]
        public void VehicleVisualConfigAsset_VisualMode_CanBeToggled()
        {
            var asset = ScriptableObject.CreateInstance<VehicleVisualConfigAsset>();
            Assert.AreEqual(VehicleVisualMode.Mode3D_ToyMesh, asset.VisualMode, "Varsayılan mod 3D Toy Mesh olmalı");

            asset.VisualMode = VehicleVisualMode.Mode2D_FlatSprite;
            Assert.AreEqual(VehicleVisualMode.Mode2D_FlatSprite, asset.VisualMode, "2D Flat Sprite moduna güncellenebilmeli");
        }
    }

    /// <summary>
    /// ProceduralLevelGenerator ve GenerateLevels test'leri.
    /// Seviye üretimi, solver doğrulama, difficulty params.
    /// </summary>
    [TestFixture]
    public class VehicleAndGenerationProceduralLevelGeneratorTests
    {
        private RuntimePathSolver _solver;
        private ProceduralLevelGenerator _generator;

        [SetUp]
        public void SetUp()
        {
            _solver = CreateTestRuntimePathSolver();
            _generator = new ProceduralLevelGenerator(_solver);
        }

        [Test]
        public void Generate_EasyLevel_Solvable()
        {
            // Kolay seviye — çözülebilir olmalı
            var param = new DifficultyParams(5, 5, 1, 0, false);
            var level = _generator.Generate(param);

            Assert.IsNotNull(level, "Level null olmamalı");
            Assert.AreEqual(5, level.width, "Grid width 5 olmalı");
            Assert.AreEqual(5, level.height, "Grid height 5 olmalı");
            Assert.AreEqual(1, level.initialNodes.Count / 2, "1 renk = 2 node");

            // Solver testi
            Assert.IsTrue(_solver.Solve(level, out _), "Seviye çözülebilir olmalı");
        }

        [Test]
        public void Generate_MediumLevel_Solvable()
        {
            // Orta seviye — çözülebilir olmalı
            var param = new DifficultyParams(6, 6, 2, 0, false);
            var level = _generator.Generate(param, maxAttempts: 10);

            Assert.IsNotNull(level, "Level null olmamalı");
            Assert.AreEqual(6, level.width, "Grid width 6 olmalı");
            Assert.AreEqual(6, level.height, "Grid height 6 olmalı");

            // Solver testi
            Assert.IsTrue(_solver.Solve(level, out _), "Seviye çözülebilir olmalı");
        }

        [Test]
        public void Generate_BridgeLevel_Solvable()
        {
            // Köprülü seviye — çözülebilir olmalı
            var param = new DifficultyParams(7, 7, 2, 2, false);
            var level = _generator.Generate(param, maxAttempts: 10);

            Assert.IsNotNull(level, "Level null olmamalı");
            Assert.GreaterOrEqual(level.bridgePositions.Count, 0, "Köprü listesi null olmamalı");

            // Solver testi
            Assert.IsTrue(_solver.Solve(level, out _), "Köprülü seviye çözülebilir olmalı");
        }

        [Test]
        public void Generate_HardLevel_Solvable()
        {
            // Zor seviye — engeller + one-way + köprü — çözülebilir olmalı
            var param = new DifficultyParams(6, 6, 2, 1, true, true, false, false);
            var level = _generator.Generate(param, maxAttempts: 5);

            Assert.IsNotNull(level, "Level null olmamalı");

            // Solver testi
            Assert.IsTrue(_solver.Solve(level, out _), "Zor seviye çözülebilir olmalı");
        }

        [Test]
        public void Generate_FallbackLevel_WhenMaxAttemptsReached()
        {
            // Zor parametre ile maxAttempts=1 — fallback level döndürmeli
            var param = new DifficultyParams(6, 6, 4, 3, true, true, true, true);
            var level = _generator.Generate(param, maxAttempts: 1);

            // Null bile olsa fallback döndürmeli
            Assert.IsNotNull(level, "Fallback level null olmamalı");
        }

        [Test]
        public void CalculateDifficultyScore_PositiveValue()
        {
            // Zorluk skoru pozitif olmalı
            var param = new DifficultyParams(6, 6, 2, 1, false);
            var level = _generator.Generate(param, maxAttempts: 5);

            int score = _generator.CalculateDifficultyScore(level, param);
            Assert.Greater(score, 0, "Zorluk skoru pozitif olmalı");
        }

        [Test]
        public void DifficultyParams_StructSerialization()
        {
            // DifficultyParams struct serialization testi
            var param = new DifficultyParams(7, 7, 3, 2, true, true, false, false);

            Assert.AreEqual(7, param.gridWidth, "gridWidth 7 olmalı");
            Assert.AreEqual(7, param.gridHeight, "gridHeight 7 olmalı");
            Assert.AreEqual(3, param.colorCount, "colorCount 3 olmalı");
            Assert.AreEqual(2, param.bridgeCount, "bridgeCount 2 olmalı");
            Assert.IsTrue(param.requireFullGridCoverage, "requireFullGridCoverage true olmalı");
            Assert.IsTrue(param.obstaclesEnabled, "obstaclesEnabled true olmalı");
            Assert.IsFalse(param.ferryEnabled, "ferryEnabled false olmalı");
        }

        [Test]
        public void Generate_LevelHasRequiredFields()
        {
            // Üretilen seviyede gerekli alanlar dolu olmalı
            var param = new DifficultyParams(6, 6, 2, 1, false);
            var level = _generator.Generate(param);

            Assert.IsNotNull(level.initialNodes, "initialNodes null olmamalı");
            Assert.Greater(level.initialNodes.Count, 0, "initialNodes boş olmamalı");
            Assert.IsNotNull(level.solutions, "solutions null olmamalı");
            Assert.GreaterOrEqual(level.levelIndex, 0, "levelIndex >= 0");
        }
    }

    /// <summary>
    /// LevelCatalog test'leri.
    /// Katalog lookup, authored/procedural ayrımı.
    /// </summary>
    [TestFixture]
    public class LevelCatalogTests
    {
        private LevelCatalogAsset _catalog;

        [SetUp]
        public void SetUp()
        {
            _catalog = ScriptableObject.CreateInstance<LevelCatalogAsset>();
            _catalog.name = "TestLevelCatalog";
        }

        [Test]
        public void TryGetEntry_ValidIndex_ReturnsTrue()
        {
            // Geçerli index ile entry döndürmeli
            var entry = new LevelCatalogAsset.LevelCatalogEntry
            {
                LevelIndex = 0,
                UseProceduralFallback = false
            };
            _catalog.Levels.Add(entry);

            Assert.IsTrue(_catalog.TryGetEntry(0, out var result));
            Assert.AreEqual(0, result.LevelIndex);
        }

        [Test]
        public void TryGetEntry_InvalidIndex_ReturnsFalse()
        {
            // Geçersiz index ile false döndürmeli
            Assert.IsFalse(_catalog.TryGetEntry(999, out _));
        }

        [Test]
        public void GetAuthoredLevel_AuthoredEntry_ReturnsLevel()
        {
            // Authored entry için LevelData döndürmeli
            var level = ScriptableObject.CreateInstance<LevelData>();
            level.name = "TestLevel";

            var entry = new LevelCatalogAsset.LevelCatalogEntry
            {
                LevelIndex = 0,
                AuthoredLevel = level,
                UseProceduralFallback = false
            };
            _catalog.Levels.Add(entry);

            var result = _catalog.GetAuthoredLevel(0);
            Assert.IsNotNull(result);
            Assert.AreEqual("TestLevel", result.name);
        }

        [Test]
        public void GetAuthoredLevel_ProceduralEntry_ReturnsNull()
        {
            // Procedural entry için null döndürmeli
            var entry = new LevelCatalogAsset.LevelCatalogEntry
            {
                LevelIndex = 0,
                UseProceduralFallback = true
            };
            _catalog.Levels.Add(entry);

            Assert.IsNull(_catalog.GetAuthoredLevel(0));
        }

        [Test]
        public void TryGetProceduralParams_ProceduralEntry_ReturnsParams()
        {
            // Procedural entry için params döndürmeli
            var param = new DifficultyParams(8, 8, 3, 2, true);
            var entry = new LevelCatalogAsset.LevelCatalogEntry
            {
                LevelIndex = 0,
                UseProceduralFallback = true,
                ProceduralDifficulty = param
            };
            _catalog.Levels.Add(entry);

            Assert.IsTrue(_catalog.TryGetProceduralParams(0, out var result));
            Assert.AreEqual(8, result.gridWidth);
            Assert.AreEqual(3, result.colorCount);
        }

        [Test]
        public void TryGetProceduralParams_AuthoredEntry_ReturnsFalse()
        {
            // Authored entry için false döndürmeli
            var entry = new LevelCatalogAsset.LevelCatalogEntry
            {
                LevelIndex = 0,
                UseProceduralFallback = false
            };
            _catalog.Levels.Add(entry);

            Assert.IsFalse(_catalog.TryGetProceduralParams(0, out _));
        }

        [Test]
        public void AuthoredLevelCount_CorrectCount()
        {
            // Authored level sayısı doğru olmalı
            var authoredLevelA = ScriptableObject.CreateInstance<LevelData>();
            authoredLevelA.levelIndex = 0;
            var authoredLevelB = ScriptableObject.CreateInstance<LevelData>();
            authoredLevelB.levelIndex = 2;
            _catalog.Levels.Add(new LevelCatalogAsset.LevelCatalogEntry
            {
                LevelIndex = 0,
                AuthoredLevel = authoredLevelA,
                UseProceduralFallback = false
            });
            _catalog.Levels.Add(new LevelCatalogAsset.LevelCatalogEntry
            {
                LevelIndex = 1,
                UseProceduralFallback = true
            });
            _catalog.Levels.Add(new LevelCatalogAsset.LevelCatalogEntry
            {
                LevelIndex = 2,
                AuthoredLevel = authoredLevelB,
                UseProceduralFallback = false
            });

            Assert.AreEqual(2, _catalog.AuthoredLevelCount);
        }
    }
}
