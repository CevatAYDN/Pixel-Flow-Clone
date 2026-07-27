using NUnit.Framework;
using Nexus.Core;
using Nexus.Core.Services;
using PixelFlow.Signals;
using PixelFlow.Services.GlobalRelease;
using PixelFlow.Data;
using UnityEngine;
using System.Threading;

namespace PixelFlow.Editor.Tests
{
    [TestFixture]
    public class GlobalReleaseServicesTests
    {
        [Test]
        public void PrivacyComplianceService_RequestConsent_GathersConsent()
        {
            var service = new PrivacyComplianceService();
            Assert.IsFalse(service.IsConsentGathered);

            service.RequestPrivacyConsent();

            Assert.IsTrue(service.IsConsentGathered);
        }

        [Test]
        public void PrivacyComplianceService_InitializeAsync_GathersConsentOnStartup()
        {
            var service = new PrivacyComplianceService();
            service.InitializeAsync(CancellationToken.None);

            Assert.IsTrue(service.IsConsentGathered);
        }

        [Test]
        public void SilentCrashDiagnosticsService_Lifecycle_SubscribesAndDisposesWithoutException()
        {
            var service = new SilentCrashDiagnosticsService();
            Assert.DoesNotThrowAsync(async () => await service.InitializeAsync(CancellationToken.None));
            Assert.DoesNotThrow(() => service.OnDispose());
        }

        [Test]
        public void InAppReviewService_OnLevelCompleted_TriggersOnThresholds()
        {
            using var ctx = GameTestContext.CreateGameContext();
            var prefs = ctx.GetModel<IPlayerPrefsService>();

            var service = new InAppReviewService
            {
                SignalBus = ctx.Context.SignalBus,
                Prefs = prefs
            };
            service.InitializeAsync(CancellationToken.None);

            // Simulate 9 completed levels
            prefs.SetInt("CompletedLevelsCount", 9);
            ctx.Dispatch(new LevelCompletedSignal());

            Assert.AreEqual(10, prefs.GetInt("CompletedLevelsCount"));

            // Level 15 completed
            prefs.SetInt("CompletedLevelsCount", 14);
            ctx.Dispatch(new LevelCompletedSignal());

            Assert.AreEqual(15, prefs.GetInt("CompletedLevelsCount"));
        }

        [Test]
        public void LocalNotificationService_InitializeAsync_SchedulesNotifications()
        {
            var service = new LocalNotificationService
            {
                LocalizationService = new PixelFlow.Services.LocalizationService(),
                Config = ScriptableObject.CreateInstance<GameConfig>()
            };
            service.Config.AllowNotificationFallbackText = true;
            Assert.DoesNotThrowAsync(async () => await service.InitializeAsync(CancellationToken.None));
            Assert.DoesNotThrow(() => service.ScheduleRetentionNotifications());
            Assert.DoesNotThrow(() => service.OnDispose());
        }
    }
}
