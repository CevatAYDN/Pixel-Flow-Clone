using NUnit.Framework;
using Nexus.Core;
using PixelFlow.Services;
using UnityEngine;

namespace PixelFlow.Editor.Tests
{
    [TestFixture]
    public class AudioServiceTests
    {
        [Test]
        public void AudioService_VolumeControls_SetWithoutExceptions()
        {
            var service = new AudioService();
            Assert.DoesNotThrow(() => service.SetMasterVolume(0.5f));
            Assert.DoesNotThrow(() => service.SetSfxVolume(0.8f));
            Assert.DoesNotThrow(() => service.SetMusicVolume(0.3f));
        }

        [Test]
        public void AudioClipProvider_Load_ReturnsValidOrSilentClip()
        {
            var clip = AudioClipProvider.Load(SfxType.UIClick);
            Assert.IsNotNull(clip);
            Assert.IsTrue(clip.length > 0f);
        }

        [Test]
        public void AudioService_PlayAndStop_DoesNotCrash()
        {
            var service = new AudioService();
            Assert.DoesNotThrow(() => service.PlaySfx(SfxType.UIClick));
            Assert.DoesNotThrow(() => service.StopSfx(SfxType.UIClick));
        }
    }
}
