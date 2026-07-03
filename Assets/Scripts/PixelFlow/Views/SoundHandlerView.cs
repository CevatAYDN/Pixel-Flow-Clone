using Nexus.Core;
using PixelFlow.Models;
using UnityEngine;

namespace PixelFlow.Views
{
    [Mediator(typeof(SoundHandlerMediator))]
    public class SoundHandlerView : View
    {
    }

    public class SoundHandlerMediator : Mediator<SoundHandlerView>
    {
        [Inject] public ISoundModel SoundModel { get; set; }

        private AudioSource _audioSource;

        protected override void OnBind()
        {
            _audioSource = View.gameObject.GetComponent<AudioSource>();
            if (_audioSource == null)
                _audioSource = View.gameObject.AddComponent<AudioSource>();

            _audioSource.playOnAwake = false;

            if (_audioSource.clip == null)
            {
                _audioSource.clip = CreateProceduralChimeClip();
            }

            SoundModel.OnPlayDrawSound += HandlePlayDrawSound;
        }

        protected override void OnUnbind()
        {
            SoundModel.OnPlayDrawSound -= HandlePlayDrawSound;
        }

        private static AudioClip s_proceduralClip;

        private static AudioClip CreateProceduralChimeClip()
        {
            if (s_proceduralClip != null) return s_proceduralClip;
            int sampleRate = 44100;
            float duration = 0.08f;
            int sampleCount = Mathf.RoundToInt(sampleRate * duration);
            float[] samples = new float[sampleCount];
            float freq = 523.25f; // C5 note

            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleRate;
                float env = Mathf.Exp(-t * 40f); // Fast decay envelope
                samples[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * env * 0.5f;
            }

            s_proceduralClip = AudioClip.Create("ProceduralChime", sampleCount, 1, sampleRate, false);
            s_proceduralClip.SetData(samples, 0);
            return s_proceduralClip;
        }

        // Pentatonic major scale pitch multipliers
        private static readonly float[] PentatonicScale = new float[]
        {
            1.0000f, // C5
            1.1225f, // D5
            1.2599f, // E5
            1.4983f, // G5
            1.6818f, // A5
            2.0000f, // C6
            2.2449f, // D6
            2.5198f, // E6
            2.9966f, // G6
            3.3636f  // A6
        };

        private void HandlePlayDrawSound(int pathLength)
        {
            if (SoundModel.IsMuted) return;
            if (_audioSource == null) return;
            if (_audioSource.clip == null) _audioSource.clip = CreateProceduralChimeClip();

            int noteIndex = Mathf.Clamp(pathLength - 1, 0, PentatonicScale.Length - 1);
            _audioSource.pitch = PentatonicScale[noteIndex];
            _audioSource.PlayOneShot(_audioSource.clip, 0.45f);
        }
    }
}
