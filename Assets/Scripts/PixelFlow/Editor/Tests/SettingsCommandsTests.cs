using NUnit.Framework;
using Nexus.Core;
using PixelFlow.Commands;
using PixelFlow.Models;
using PixelFlow.Signals;

namespace PixelFlow.Editor.Tests
{
    [TestFixture]
    public class SettingsCommandsTests
    {
        [Test]
        public void ChangeAudioVolumeCommand_UpdatesSettingsModelVolume()
        {
            using var ctx = GameTestContext.CreateGameContext();
            var settings = ctx.GetModel<ISettingsModel>();

            ctx.Dispatch(new ChangeAudioVolumeSignal { Channel = AudioChannel.Master, Value = 0.5f });
            Assert.AreEqual(0.5f, settings.MasterVolume, 0.01f);

            ctx.Dispatch(new ChangeAudioVolumeSignal { Channel = AudioChannel.Sfx, Value = 0.8f });
            Assert.AreEqual(0.8f, settings.SfxVolume, 0.01f);

            ctx.Dispatch(new ChangeAudioVolumeSignal { Channel = AudioChannel.Music, Value = 0.2f });
            Assert.AreEqual(0.2f, settings.MusicVolume, 0.01f);
        }

        [Test]
        public void ChangeColorBlindModeCommand_UpdatesSettingsModelMode()
        {
            using var ctx = GameTestContext.CreateGameContext();
            var settings = ctx.GetModel<ISettingsModel>();

            ctx.Dispatch(new ChangeColorBlindModeSignal { Mode = ColorBlindMode.Protanopia });
            Assert.AreEqual(ColorBlindMode.Protanopia, settings.CurrentColorBlindMode);
        }

        [Test]
        public void ToggleHapticsCommand_UpdatesSettingsModelHapticsFlag()
        {
            using var ctx = GameTestContext.CreateGameContext();
            var settings = ctx.GetModel<ISettingsModel>();

            ctx.Dispatch(new ToggleHapticsSignal { Disabled = true });
            Assert.IsTrue(settings.HapticsDisabled);

            ctx.Dispatch(new ToggleHapticsSignal { Disabled = false });
            Assert.IsFalse(settings.HapticsDisabled);
        }
    }
}
