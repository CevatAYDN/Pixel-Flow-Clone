using NUnit.Framework;
using Nexus.Core;
using PixelFlow.Data;
using PixelFlow.Models;
using PixelFlow.Services;

namespace PixelFlow.Editor.Tests
{
    [TestFixture]
    public class TutorialAndCrisisServiceTests
    {
        [Test]
        public void TutorialDriver_MapLevelToStep_ReturnsCorrectStep()
        {
            Assert.AreEqual(TutorialStep.TouchAndDrag, TutorialDriver.MapLevelToStep(0));
            Assert.AreEqual(TutorialStep.ColorMatch, TutorialDriver.MapLevelToStep(1));
            Assert.AreEqual(TutorialStep.SecondColor, TutorialDriver.MapLevelToStep(8));
            Assert.AreEqual(TutorialStep.None, TutorialDriver.MapLevelToStep(99));
        }

        [Test]
        public void TutorialDriver_StartAndCompleteStep_UpdatesModel()
        {
            using var ctx = GameTestContext.CreateGameContext();
            var driver = ctx.Context.Container.Resolve<ITutorialDriver>();
            var model = ctx.GetModel<ITutorialModel>();

            driver.StartStep(TutorialStep.TouchAndDrag);
            Assert.IsTrue(model.IsActive);
            Assert.AreEqual(TutorialStep.TouchAndDrag, model.CurrentStep);

            driver.CompleteCurrentStep();
            Assert.IsFalse(model.IsActive);
            Assert.IsTrue(model.IsCompleted(TutorialStep.TouchAndDrag));
        }

        [Test]
        public void CrisisAdService_RecordCrisisAttempt_IncrementsRetryCount()
        {
            using var ctx = GameTestContext.CreateGameContext();
            var service = ctx.Context.Container.Resolve<ICrisisAdService>();
            var session = ctx.GetModel<IGameSessionModel>();

            session.StartSession(3);
            Assert.AreEqual(0, service.RetryCount);

            service.RecordCrisisAttempt();
            Assert.AreEqual(1, service.RetryCount);

            service.ResetRetryCount();
            Assert.AreEqual(0, service.RetryCount);
        }
    }
}
