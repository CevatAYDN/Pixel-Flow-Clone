using NUnit.Framework;
using Nexus.Core;
using Nexus.Core.Services;
using PixelFlow.Commands;
using PixelFlow.Data;
using PixelFlow.Models;
using PixelFlow.Signals;
using UnityEngine;

namespace PixelFlow.Editor.Tests
{
    [TestFixture]
    public class SaveAndAdCommandsTests
    {
        [Test]
        public void SaveProgressCommand_UnlocksNextLevelAndAwardsCoins()
        {
            using var ctx = GameTestContext.CreateGameContext();
            var progress = ctx.GetModel<IProgressModel>();
            var levelModel = ctx.GetModel<ILevelModel>();
            var session = ctx.GetModel<IGameSessionModel>();
            var economy = ctx.GetModel<IEconomyService>();

            var level = GameTestContext.CreateTestLevel(1);
            levelModel.SetLevel(level);
            session.StartSession(3);
            progress.RecordStars(1, 3);

            ctx.Dispatch(new LevelCompletedSignal());

            Assert.IsTrue(progress.UnlockedLevels >= 1);
            Assert.AreEqual(3, progress.GetStars(1));
            Assert.IsTrue(economy.GetBalance("coins") >= 0);
        }

        [Test]
        public void RewardedAdCommand_ExecutesWithoutException()
        {
            using var ctx = GameTestContext.CreateGameContext();
            var cmd = new RewardedAdCommand
            {
                Logger = ctx.GetModel<ILoggerService>(),
                FeedbackService = ctx.GetModel<IFeedbackService>()
            };

            Assert.DoesNotThrow(() => cmd.Execute());
            Assert.DoesNotThrow(() => cmd.Reset());
        }

        [Test]
        public void InterstitialAdCommand_FiresAndExecutes()
        {
            using var ctx = GameTestContext.CreateGameContext();
            Assert.DoesNotThrow(() => ctx.Dispatch(new RequestInterstitialAdSignal()));
        }
    }
}
