using NUnit.Framework;
using Nexus.Core;
using PixelFlow.Models;
using PixelFlow.Services;
using PixelFlow.Signals;
using PixelFlow.Commands;

namespace PixelFlow.Editor.Tests
{
    [TestFixture]
    public class ThemeAndSettingsTests
    {
        private NexusTestContext _ctx;
        private ISettingsModel _settings;
        private ISoundModel _sound;

        [SetUp]
        public void SetUp()
        {
            _ctx = NexusTestHarness.CreateContext(builder =>
            {
                builder.Bind<IPlayerPrefsService, InMemoryPlayerPrefsService>();
                builder.BindReactiveModel<ISettingsModel, SettingsModel>();
                builder.BindReactiveModel<ISoundModel, SoundModel>();
                
                builder.BindCommand<ChangeThemeSignal, ChangeThemeCommand>();
            });

            _settings = _ctx.GetModel<ISettingsModel>();
            _sound = _ctx.GetModel<ISoundModel>();
        }

        [TearDown]
        public void TearDown()
        {
            _ctx?.Dispose();
            _ctx = null;
        }

        [Test]
        public void SettingsModel_InitialTheme_IsDark()
        {
            // Assuming default theme is Dark
            Assert.AreEqual(AppTheme.Dark, _settings.CurrentTheme);
        }

        [Test]
        public void ChangeThemeCommand_Fired_ChangesTheme()
        {
            var signalBus = _ctx.Context.Container.Resolve<ISignalBus>();

            Assert.AreEqual(AppTheme.Dark, _settings.CurrentTheme);

            signalBus.Fire(new ChangeThemeSignal { Theme = AppTheme.Light });

            Assert.AreEqual(AppTheme.Light, _settings.CurrentTheme);

            signalBus.Fire(new ChangeThemeSignal { Theme = AppTheme.Dark });

            Assert.AreEqual(AppTheme.Dark, _settings.CurrentTheme);
        }

        [Test]
        public void SoundModel_InitialState_IsNotMuted()
        {
            Assert.IsFalse(_sound.IsMuted);
        }

        [Test]
        public void SoundModel_SetMuted_TogglesMuteState()
        {
            bool initialMute = _sound.IsMuted;
            _sound.ToggleMute();
            Assert.AreNotEqual(initialMute, _sound.IsMuted);

            _sound.ToggleMute();
            Assert.AreEqual(initialMute, _sound.IsMuted);
        }

        [Test]
        public void SettingsModel_ColorBlindMode_ChangesState()
        {
            Assert.AreEqual(ColorBlindMode.None, _settings.CurrentColorBlindMode);

            _settings.SetColorBlindMode(ColorBlindMode.Protanopia);
            Assert.AreEqual(ColorBlindMode.Protanopia, _settings.CurrentColorBlindMode);

            _settings.SetColorBlindMode(ColorBlindMode.Tritanopia);
            Assert.AreEqual(ColorBlindMode.Tritanopia, _settings.CurrentColorBlindMode);
        }
    }
}
