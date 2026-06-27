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
            SoundModel.OnPlayDrawSound += HandlePlayDrawSound;
        }

        protected override void OnUnbind()
        {
            SoundModel.OnPlayDrawSound -= HandlePlayDrawSound;
        }

        private void HandlePlayDrawSound(int pathLength)
        {
            if (SoundModel.IsMuted) return;

            _audioSource.pitch = Mathf.Clamp(0.8f + pathLength * 0.03f, 0.5f, 2.0f);
            _audioSource.Play();
            Debug.Log($"[SoundHandler] Played draw sound at pitch: {_audioSource.pitch:F2}");
        }
    }
}
