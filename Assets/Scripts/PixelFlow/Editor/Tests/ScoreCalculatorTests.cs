using NUnit.Framework;
using Nexus.Core;
using Nexus.Core.Services;
using PixelFlow.Services;
using PixelFlow.Data;
using PixelFlow.Models;
using UnityEngine;
using static PixelFlow.Editor.Tests.GameTestContext;

namespace PixelFlow.Editor.Tests
{
    [TestFixture]
    public class ScoreCalculatorTests
    {
        private NexusTestContext _ctx;
        private IScoreCalculator _scoreCalculator;
        private IGameSessionModel _sessionModel;
        private IEconomyService _economyService;
        private EconomyConfigAsset _economyConfig;

        [SetUp]
        public void SetUp()
        {
            _ctx = NexusTestHarness.CreateContext(builder =>
            {
                builder.Bind<IPlayerPrefsService, InMemoryPlayerPrefsService>();
                builder.BindInstance(CreateTestGameConfig());
                builder.BindInstance(CreateTestStorageKeysConfig());

                _economyConfig = ScriptableObject.CreateInstance<EconomyConfigAsset>();
                _economyConfig.BaseScorePerCell = 100f;
                _economyConfig.IdealTimeFactor = 0.5f;
                _economyConfig.MinTimeMultiplier = 0.25f;
                _economyConfig.HintPenaltyPerUse = 0.1f;
                _economyConfig.ViaductPenaltyPerUse = 0.1f;
                builder.BindInstance(_economyConfig);

                var economyService = new Nexus.Core.Services.EconomyService();
                builder.BindService<IEconomyService, Nexus.Core.Services.EconomyService>();

                builder.BindService<IScoreCalculator, ScoreCalculator>();
                builder.BindReactiveModel<IGameSessionModel, GameSessionModel>();
            });

            _scoreCalculator = _ctx.Context.Container.Resolve<IScoreCalculator>();
            _sessionModel = _ctx.GetModel<IGameSessionModel>();
            _economyService = _ctx.Context.Container.Resolve<IEconomyService>();
        }

        [TearDown]
        public void TearDown()
        {
            _ctx?.Dispose();
        }

        [Test]
        public void Calculate_NoHints_FastTime_HighScore()
        {
            var (score, stars) = _scoreCalculator.Calculate(5, 5, 5f, 0, 5, 0);
            Assert.Greater(score, 2000, "Score should be near maximum");
            Assert.AreEqual(3, stars);
        }

        [Test]
        public void Calculate_WithHints_ReducesScore()
        {
            var (scoreNoHints, _) = _scoreCalculator.Calculate(5, 5, 5f, 0, 5, 0);
            var (scoreWithHints, _) = _scoreCalculator.Calculate(5, 5, 5f, 3, 5, 0);
            Assert.Greater(scoreNoHints, scoreWithHints, "Using hints should reduce score");
        }

        [Test]
        public void Calculate_MaximumHintPenalty_CapsAtZero()
        {
            var (score, stars) = _scoreCalculator.Calculate(5, 5, 1f, 15, 20, 3);
            Assert.AreEqual(0, score, "Score should be 0 when penalty saturates");
            Assert.AreEqual(1, stars, "Should still earn 1 star for completing");
        }

        [Test]
        public void Calculate_MinimumTimeMultiplier_IsTwentyFivePercent()
        {
            var (score, _) = _scoreCalculator.Calculate(5, 5, 200f, 0, 5, 0);
            float baseScore = 5 * 5 * 100f;
            float expectedMin = baseScore * 0.25f;
            Assert.AreEqual((int)(expectedMin + 0.5f), score);
        }

        [Test]
        public void Calculate_IdealTime_FullMultiplier()
        {
            float idealTime = 5 * 5 * 0.5f;
            var (score, _) = _scoreCalculator.Calculate(5, 5, idealTime, 0, 5, 0);
            float baseScore = 5 * 5 * 100f;
            Assert.AreEqual((int)(baseScore + 0.5f), score);
        }

        [Test]
        public void Calculate_WithViaducts_ReducesScore()
        {
            var (scoreNoViaducts, _) = _scoreCalculator.Calculate(5, 5, 5f, 0, 5, 0);
            var (scoreWithViaducts, _) = _scoreCalculator.Calculate(5, 5, 5f, 0, 5, 3);
            Assert.Greater(scoreNoViaducts, scoreWithViaducts, "Using viaducts should reduce score");
        }

        [Test]
        public void Calculate_ZeroViaducts_NoPenalty()
        {
            var (score, _) = _scoreCalculator.Calculate(5, 5, 5f, 0, 5, 0);
            float baseScore = 5 * 5 * 100f;
            Assert.GreaterOrEqual(score, (int)baseScore, "Score should be >= base with 0 viaducts and fast time");
        }

        [Test]
        public void Calculate_Stars_ThreeStars()
        {
            var (_, stars) = _scoreCalculator.Calculate(5, 5, 5f, 0, 5, 0);
            Assert.AreEqual(3, stars);
        }

        [Test]
        public void Calculate_Stars_TwoStars()
        {
            var (_, stars) = _scoreCalculator.Calculate(10, 10, 300f, 3, 10, 2);
            Assert.AreEqual(2, stars, "Using <= 2 viaducts earns 2 stars per GDD §2.8.");
        }

        [Test]
        public void Calculate_Stars_OneStar_WhenScoreLow()
        {
            var (score, stars) = _scoreCalculator.Calculate(5, 5, 999f, 10, 10, 10);
            Assert.AreEqual(1, stars, "Poor play should earn 1 star");
            Assert.GreaterOrEqual(score, 0, "Score should never go below 0");
        }

        [Test]
        public void Calculate_VerySmallGrid_NoCrash()
        {
            var (score, stars) = _scoreCalculator.Calculate(2, 2, 5f, 0, 3, 0);
            Assert.Greater(score, 0);
            Assert.GreaterOrEqual(stars, 1);
        }

        [Test]
        public void Calculate_VeryLargeGrid_NoCrash()
        {
            var (score, _) = _scoreCalculator.Calculate(20, 20, 60f, 0, 10, 0);
            Assert.Greater(score, 0);
        }

        [Test]
        public void Calculate_FollowsGddStarRules()
        {
            // 0 viaducts used -> 3 stars (Perfect Flow)
            var (_, stars0) = _scoreCalculator.Calculate(5, 5, 10f, 0, 3, 0);
            Assert.AreEqual(3, stars0, "0 viaducts must yield 3 stars per GDD §2.8.");

            // 1 or 2 viaducts used -> 2 stars
            var (_, stars1) = _scoreCalculator.Calculate(5, 5, 10f, 0, 3, 1);
            Assert.AreEqual(2, stars1, "1 viaduct must yield 2 stars per GDD §2.8.");

            var (_, stars2) = _scoreCalculator.Calculate(5, 5, 10f, 0, 3, 2);
            Assert.AreEqual(2, stars2, "2 viaducts must yield 2 stars per GDD §2.8.");

            // 3+ viaducts used -> 1 star
            var (_, stars3) = _scoreCalculator.Calculate(5, 5, 10f, 0, 3, 3);
            Assert.AreEqual(1, stars3, "3+ viaducts must yield 1 star per GDD §2.8.");
        }
    }
}