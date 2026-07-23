using NUnit.Framework;
using Nexus.Core;
using PixelFlow.Models;
using PixelFlow.Services;
using static PixelFlow.Editor.Tests.GameTestContext;

namespace PixelFlow.Editor.Tests
{
    [TestFixture]
    public class PowerUpServiceTests
    {
        private NexusTestContext _ctx;
        private IPowerUpService _powerUp;
        private int _lastEventValue;
        private int _eventFireCount;

        [SetUp]
        public void SetUp()
        {
            _ctx = CreateGameContext();
            _powerUp = _ctx.Context.Container.Resolve<IPowerUpService>();
            _lastEventValue = -999;
            _eventFireCount = 0;
        }

        [TearDown]
        public void TearDown()
        {
            _ctx?.Dispose();
        }

        // ── Rainbow Road Tests ──────────────────────────────

        [Test]
        public void RainbowRoad_StartsWithZeroUses()
        {
            Assert.AreEqual(0, _powerUp.RainbowRoadUses);
            Assert.IsFalse(_powerUp.HasActiveRainbowRoad);
        }

        [Test]
        public void ActivateRainbowRoad_SetsThreeUses()
        {
            _powerUp.ActivateRainbowRoad();
            Assert.AreEqual(3, _powerUp.RainbowRoadUses);
            Assert.IsTrue(_powerUp.HasActiveRainbowRoad);
        }

        [Test]
        public void ActivateRainbowRoad_FiresEvent()
        {
            _powerUp.OnRainbowRoadUsesChanged += (v) => { _lastEventValue = v; _eventFireCount++; };

            _powerUp.ActivateRainbowRoad();
            Assert.AreEqual(3, _lastEventValue);
            Assert.AreEqual(1, _eventFireCount);
        }

        [Test]
        public void ActivateRainbowRoad_WhenAlreadyActive_RefreshesUses()
        {
            _powerUp.ActivateRainbowRoad();
            _powerUp.TryConsumeRainbowRoadSegment(); // 3 → 2
            _powerUp.TryConsumeRainbowRoadSegment(); // 2 → 1

            _powerUp.ActivateRainbowRoad(); // Reset to 3
            Assert.AreEqual(3, _powerUp.RainbowRoadUses);
        }

        [Test]
        public void TryConsumeRainbowRoadSegment_DecrementsUses()
        {
            _powerUp.ActivateRainbowRoad();
            Assert.IsTrue(_powerUp.TryConsumeRainbowRoadSegment());
            Assert.AreEqual(2, _powerUp.RainbowRoadUses);
            Assert.IsTrue(_powerUp.HasActiveRainbowRoad);
        }

        [Test]
        public void TryConsumeRainbowRoadSegment_ConsumesAllThree()
        {
            _powerUp.ActivateRainbowRoad();
            Assert.IsTrue(_powerUp.TryConsumeRainbowRoadSegment()); // 3→2
            Assert.IsTrue(_powerUp.TryConsumeRainbowRoadSegment()); // 2→1
            Assert.IsTrue(_powerUp.TryConsumeRainbowRoadSegment()); // 1→0
            Assert.IsFalse(_powerUp.HasActiveRainbowRoad);
            Assert.AreEqual(0, _powerUp.RainbowRoadUses);
        }

        [Test]
        public void TryConsumeRainbowRoadSegment_WhenExhausted_ReturnsFalse()
        {
            _powerUp.ActivateRainbowRoad();
            for (int i = 0; i < 3; i++) _powerUp.TryConsumeRainbowRoadSegment();
            Assert.IsFalse(_powerUp.TryConsumeRainbowRoadSegment());
            Assert.AreEqual(0, _powerUp.RainbowRoadUses);
        }

        [Test]
        public void TryConsumeRainbowRoadSegment_WithoutActivation_ReturnsFalse()
        {
            Assert.IsFalse(_powerUp.TryConsumeRainbowRoadSegment());
        }

        [Test]
        public void TryConsumeRainbowRoadSegment_FiresEvent()
        {
            _powerUp.OnRainbowRoadUsesChanged += (v) => { _lastEventValue = v; _eventFireCount++; };
            _powerUp.ActivateRainbowRoad(); // fires: 3
            _powerUp.TryConsumeRainbowRoadSegment(); // fires: 2

            Assert.AreEqual(2, _lastEventValue);
            Assert.AreEqual(2, _eventFireCount);
        }

        [Test]
        public void DeactivateRainbowRoad_ResetsToZero()
        {
            _powerUp.ActivateRainbowRoad();
            _powerUp.DeactivateRainbowRoad();
            Assert.AreEqual(0, _powerUp.RainbowRoadUses);
            Assert.IsFalse(_powerUp.HasActiveRainbowRoad);
        }

        [Test]
        public void DeactivateRainbowRoad_FiresEvent()
        {
            _powerUp.ActivateRainbowRoad();
            _powerUp.OnRainbowRoadUsesChanged += (v) => { _lastEventValue = v; };
            _powerUp.DeactivateRainbowRoad();
            Assert.AreEqual(0, _lastEventValue);
        }

        // ── Clear Jam Tests ─────────────────────────────────

        [Test]
        public void ClearJam_StartsWithZeroWithoutReset()
        {
            Assert.AreEqual(0, _powerUp.ClearJamUsesRemaining);
            Assert.IsFalse(_powerUp.CanUseClearJam);
        }

        [Test]
        public void AddClearJamUse_IncrementsUses()
        {
            _powerUp.AddClearJamUse(1);
            Assert.AreEqual(1, _powerUp.ClearJamUsesRemaining);
            Assert.IsTrue(_powerUp.CanUseClearJam);
        }

        [Test]
        public void AddClearJamUse_MultipleTimes()
        {
            _powerUp.AddClearJamUse(2);
            Assert.AreEqual(2, _powerUp.ClearJamUsesRemaining);
        }

        [Test]
        public void AddClearJamUse_FiresEvent()
        {
            _powerUp.OnClearJamUsesChanged += (v) => { _lastEventValue = v; _eventFireCount++; };
            _powerUp.AddClearJamUse(1);
            Assert.AreEqual(1, _lastEventValue);
            Assert.AreEqual(1, _eventFireCount);
        }

        [Test]
        public void TryUseClearJam_ConsumesOneUse()
        {
            _powerUp.AddClearJamUse(1);
            Assert.IsTrue(_powerUp.TryUseClearJam());
            Assert.AreEqual(0, _powerUp.ClearJamUsesRemaining);
            Assert.IsFalse(_powerUp.CanUseClearJam);
        }

        [Test]
        public void TryUseClearJam_WhenExhausted_ReturnsFalse()
        {
            _powerUp.AddClearJamUse(1);
            _powerUp.TryUseClearJam();
            Assert.IsFalse(_powerUp.TryUseClearJam());
        }

        [Test]
        public void TryUseClearJam_WithoutUses_ReturnsFalse()
        {
            Assert.IsFalse(_powerUp.TryUseClearJam());
        }

        [Test]
        public void TryUseClearJam_FiresEvent()
        {
            _powerUp.AddClearJamUse(1);
            _powerUp.OnClearJamUsesChanged += (v) => { _lastEventValue = v; _eventFireCount++; };
            _powerUp.TryUseClearJam();
            Assert.AreEqual(0, _lastEventValue);
            Assert.AreEqual(1, _eventFireCount);
        }

        // ── ResetForNewLevel Tests ──────────────────────────

        [Test]
        public void ResetForNewLevel_SetsOneClearJam()
        {
            _powerUp.ResetForNewLevel();
            Assert.AreEqual(1, _powerUp.ClearJamUsesRemaining);
            Assert.IsTrue(_powerUp.CanUseClearJam);
        }

        [Test]
        public void ResetForNewLevel_ResetsRainbowRoad()
        {
            _powerUp.ActivateRainbowRoad();
            _powerUp.ResetForNewLevel();
            Assert.AreEqual(0, _powerUp.RainbowRoadUses);
            Assert.IsFalse(_powerUp.HasActiveRainbowRoad);
        }

        [Test]
        public void ResetForNewLevel_RefreshesClearJam()
        {
            _powerUp.AddClearJamUse(1);
            _powerUp.TryUseClearJam(); // Used up
            _powerUp.ResetForNewLevel(); // Should give 1 again
            Assert.AreEqual(1, _powerUp.ClearJamUsesRemaining);
        }

        [Test]
        public void ResetForNewLevel_FiresBothEvents()
        {
            int rainbowFires = 0;
            int clearJamFires = 0;
            _powerUp.OnRainbowRoadUsesChanged += (v) => rainbowFires++;
            _powerUp.OnClearJamUsesChanged += (v) => clearJamFires++;

            _powerUp.ResetForNewLevel();
            Assert.AreEqual(1, rainbowFires);
            Assert.AreEqual(1, clearJamFires);
        }
    }
}
