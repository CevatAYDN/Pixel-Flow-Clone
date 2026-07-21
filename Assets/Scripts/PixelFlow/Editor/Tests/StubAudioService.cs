using UnityEngine;
using Nexus.Core.Services;

namespace PixelFlow.Editor.Tests
{
    /// <summary>
    /// Minimal no-op stub for <see cref="Nexus.Core.Services.IAudioService"/>.
    /// Satisfies DI injection in <see cref="FeedbackService"/> without requiring
    /// the full Nexus AudioService setup (which needs PlayerPrefsService, AudioRoot, etc.).
    /// All methods are no-ops — tests verify gameplay logic, not audio playback.
    /// </summary>
    public sealed class StubAudioService : IAudioService
    {
        public float MasterVolume { get; set; }
        public float BgmVolume { get; set; }
        public float SfxVolume { get; set; }
        public bool IsMuted { get; set; }
        public float BgmStateMultiplier { get; set; }

        public void PlayBgm(AudioClip clip, bool loop = true, float fadeDuration = 0.5f) { }
        public void StopBgm(float fadeDuration = 0.5f) { }
        public void PlaySfx(AudioClip clip, float volume = 1f, float pitchMin = 1f, float pitchMax = 1f) { }
        public void PlaySfxAtPosition(AudioClip clip, Vector3 position, float volume = 1f) { }
    }
}
