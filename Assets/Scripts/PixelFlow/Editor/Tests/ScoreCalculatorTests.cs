using NUnit.Framework;
using PixelFlow.Services;

namespace PixelFlow.Editor.Tests
{
    [TestFixture]
    public class ScoreCalculatorTests
    {
        [Test]
        public void Calculate_NoHints_FastTime_HighScore()
        {
            var (score, stars) = ScoreCalculator.Calculate(5, 5, 5f, 0, 5, 0);
            Assert.Greater(score, 2000, "Score should be near maximum");
            Assert.AreEqual(3, stars);
        }

        [Test]
        public void Calculate_WithHints_ReducesScore()
        {
            var (scoreNoHints, _) = ScoreCalculator.Calculate(5, 5, 5f, 0, 5, 0);
            var (scoreWithHints, _) = ScoreCalculator.Calculate(5, 5, 5f, 3, 5, 0);
            Assert.Greater(scoreNoHints, scoreWithHints, "Using hints should reduce score");
        }

        [Test]
        public void Calculate_MaximumHintPenalty_CapsAtZero()
        {
            var (score, stars) = ScoreCalculator.Calculate(5, 5, 1f, 15, 20, 3);
            Assert.AreEqual(0, score, "Score should be 0 when penalty saturates");
            Assert.AreEqual(1, stars, "Should still earn 1 star for completing");
        }

        [Test]
        public void Calculate_MinimumTimeMultiplier_IsTwentyFivePercent()
        {
            var (score, _) = ScoreCalculator.Calculate(5, 5, 200f, 0, 5, 0);
            float baseScore = 5 * 5 * 100f;
            float expectedMin = baseScore * 0.25f;
            Assert.AreEqual((int)(expectedMin + 0.5f), score);
        }

        [Test]
        public void Calculate_IdealTime_FullMultiplier()
        {
            float idealTime = 5 * 5 * 0.5f;
            var (score, _) = ScoreCalculator.Calculate(5, 5, idealTime, 0, 5, 0);
            float baseScore = 5 * 5 * 100f;
            Assert.AreEqual((int)(baseScore + 0.5f), score);
        }

        [Test]
        public void Calculate_WithViaducts_ReducesScore()
        {
            var (scoreNoViaducts, _) = ScoreCalculator.Calculate(5, 5, 5f, 0, 5, 0);
            var (scoreWithViaducts, _) = ScoreCalculator.Calculate(5, 5, 5f, 0, 5, 3);
            Assert.Greater(scoreNoViaducts, scoreWithViaducts, "Using viaducts should reduce score");
        }

        [Test]
        public void Calculate_ZeroViaducts_NoPenalty()
        {
            var (score, _) = ScoreCalculator.Calculate(5, 5, 5f, 0, 5, 0);
            float baseScore = 5 * 5 * 100f;
            Assert.GreaterOrEqual(score, (int)baseScore, "Score should be >= base with 0 viaducts and fast time");
        }

        [Test]
        public void Calculate_Stars_ThreeStars()
        {
            var (_, stars) = ScoreCalculator.Calculate(5, 5, 5f, 0, 5, 0);
            Assert.AreEqual(3, stars);
        }

        [Test]
        public void Calculate_Stars_TwoStars()
        {
            var (_, stars) = ScoreCalculator.Calculate(10, 10, 300f, 3, 10, 5);
            Assert.AreEqual(2, stars, "Moderate play should earn 2 stars");
        }

        [Test]
        public void Calculate_Stars_OneStar_WhenScoreLow()
        {
            var (score, stars) = ScoreCalculator.Calculate(5, 5, 999f, 10, 10, 10);
            Assert.AreEqual(1, stars, "Poor play should earn 1 star");
            Assert.GreaterOrEqual(score, 0, "Score should never go below 0");
        }

        [Test]
        public void Calculate_VerySmallGrid_NoCrash()
        {
            var (score, stars) = ScoreCalculator.Calculate(2, 2, 5f, 0, 3, 0);
            Assert.Greater(score, 0);
            Assert.GreaterOrEqual(stars, 1);
        }

        [Test]
        public void Calculate_VeryLargeGrid_NoCrash()
        {
            var (score, _) = ScoreCalculator.Calculate(20, 20, 60f, 0, 10, 0);
            Assert.Greater(score, 0);
        }
    }
}
