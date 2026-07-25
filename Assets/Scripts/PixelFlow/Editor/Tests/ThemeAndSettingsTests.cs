using NUnit.Framework;
using Nexus.Core;
using Nexus.Core.Services;
using PixelFlow.Data;
using PixelFlow.Models;
using PixelFlow.Services;
using PixelFlow.Signals;
using PixelFlow.Commands;
using UnityEngine;

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

                // game_plan.md §2.2: GameConfig testlerde de mevcut olmalı (SettingsModel varsayılanları buradan gelir)
                builder.BindInstance(GameTestContext.CreateTestGameConfig());
                builder.BindInstance(GameTestContext.CreateTestStorageKeysConfig());

                builder.BindReactiveModel<ISettingsModel, SettingsModel>();
                builder.BindReactiveModel<ISoundModel, SoundModel>();
                var quietLogger = new LoggerService { IsEnabled = false };
                builder.BindInstance<ILoggerService>(quietLogger);
                builder.Bind<IFeedbackService, FeedbackService>();
                builder.Bind<Nexus.Core.Services.IAudioService, StubAudioService>();

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

            _settings.SetColorBlindMode(ColorBlindMode.Tritanopia);
            Assert.AreEqual(ColorBlindMode.Tritanopia, _settings.CurrentColorBlindMode);
        }

        [Test]
        public void ThemePaletteAsset_DistinctColorsForThemes()
        {
            var palette = ScriptableObject.CreateInstance<ThemePaletteAsset>();
            var darkBg = palette.GetCellBackground(AppTheme.Dark);
            var lightBg = palette.GetCellBackground(AppTheme.Light);
            var neonBg = palette.GetCellBackground(AppTheme.Neon);

            Assert.AreNotEqual(darkBg, lightBg, "Dark and Light cell backgrounds must differ");
            Assert.AreNotEqual(darkBg, neonBg, "Dark and Neon cell backgrounds must differ");
            Assert.AreNotEqual(lightBg, neonBg, "Light and Neon cell backgrounds must differ");

            Assert.IsNotNull(palette.Candy);
            Assert.IsNotNull(palette.Forest);
        }

        [Test]
        public void EconomyConfigAsset_EnsureCanonicalIapProducts_PopulatesNineProducts()
        {
            var economyConfig = ScriptableObject.CreateInstance<EconomyConfigAsset>();
            var products = economyConfig.EnsureCanonicalIapProducts();

            Assert.IsNotNull(products);
            Assert.AreEqual(9, products.Count, "Canonical IAP catalogue must contain exactly 9 products per game_plan.md §9.3");
            Assert.IsTrue(products.Exists(p => p.ProductId == "no_ads"));
            Assert.IsTrue(products.Exists(p => p.ProductId == "starter_pack"));
            Assert.IsTrue(products.Exists(p => p.ProductId == "vip_bundle"));
        }
    }
}
