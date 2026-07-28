using PixelFlow.Data;
using PixelFlow.Models;
using Nexus.Core;
using Nexus.Core.Services;
using System.Threading;
using System.Threading.Tasks;

namespace PixelFlow.Services
{
    public interface IScoreCalculator
    {
        (int finalScore, int stars) Calculate(
            int gridWidth, int gridHeight,
            double elapsedTime, int hintsUsed, int totalHintsAvailable, int viaductsUsed);
    }

    public class ScoreCalculator : IScoreCalculator, INexusService
    {
        [Inject] public EconomyConfigAsset Config { get; set; }

        public ValueTask InitializeAsync(CancellationToken ct) => default;
        public void OnDispose() { }

        public (int finalScore, int stars) Calculate(
            int gridWidth, int gridHeight,
            double elapsedTime, int hintsUsed, int totalHintsAvailable, int viaductsUsed)
        {
            if (Config == null)
            {
                throw new DataValidationException("EconomyConfigAsset not injected! ScoreCalculator requires DI container initialization.");
            }

            var config = Config;
            double cellCount = gridWidth * gridHeight;
            double baseScore = cellCount * config.BaseScorePerCell;
            double idealTime = cellCount * config.IdealTimeFactor;
            double timeMultiplier = elapsedTime <= idealTime
                ? 1.0
                : System.Math.Max(config.MinTimeMultiplier, idealTime / elapsedTime);

            double hintMultiplier = 1.0 - (hintsUsed * config.HintPenaltyPerUse);
            if (hintMultiplier < 0.0) hintMultiplier = 0.0;

            double viaductPenalty = viaductsUsed * config.ViaductPenaltyPerUse;
            if (viaductPenalty > 1.0) viaductPenalty = 1.0;

            double finalScore = baseScore * timeMultiplier * hintMultiplier * (1.0 - viaductPenalty);
            int roundedScore = (int)(finalScore + 0.5);

            int stars = config.CalculateStars(viaductsUsed);

            return (roundedScore, stars);
        }
    }
}
