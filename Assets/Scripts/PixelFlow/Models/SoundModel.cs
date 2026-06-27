using System;
using UnityEngine;

namespace PixelFlow.Models
{
    public interface ISoundModel
    {
        bool IsMuted { get; }
        event Action<bool> OnMuteChanged;
        event Action<int> OnPlayDrawSound;

        void ToggleMute();
        void PlayDrawSound(int currentPathLength);
    }

    public class SoundModel : ISoundModel
    {
        public bool IsMuted { get; private set; }
        public event Action<bool> OnMuteChanged;
        public event Action<int> OnPlayDrawSound;

        public SoundModel()
        {
            IsMuted = PlayerPrefs.GetInt("IsMuted", 0) == 1;
        }

        public void ToggleMute()
        {
            IsMuted = !IsMuted;
            PlayerPrefs.SetInt("IsMuted", IsMuted ? 1 : 0);
            PlayerPrefs.Save();
            OnMuteChanged?.Invoke(IsMuted);
        }

        public void PlayDrawSound(int currentPathLength)
        {
            if (IsMuted) return;
            OnPlayDrawSound?.Invoke(currentPathLength);
        }
    }
}
