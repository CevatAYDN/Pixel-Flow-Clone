using System;
using PixelFlow.Services;
using System.Threading;
using System.Threading.Tasks;
using Nexus.Core;
using Nexus.Core.Services;

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

    /// <summary>
    /// Ses tercihlerini IPlayerPrefsService üzerinden kalıcı saklar.
    /// PlayDrawSound çağrısı mute durumunda event fırlatmaz.
    /// </summary>
    public class SoundModel : ISoundModel, IReactiveModel
    {
        private const string MuteKey = "IsMuted";

        private readonly IPlayerPrefsService _prefs;

        public bool IsMuted { get; private set; }
        public event Action<bool> OnMuteChanged;
        public event Action<int> OnPlayDrawSound;

        public SoundModel(IPlayerPrefsService prefs)
        {
            _prefs = prefs ?? throw new System.ArgumentNullException(nameof(prefs));
            IsMuted = _prefs.GetBool(MuteKey, false);
        }

        public void ToggleMute()
        {
            IsMuted = !IsMuted;
            _prefs.SetBool(MuteKey, IsMuted);
            OnMuteChanged?.Invoke(IsMuted);
        }

        public void PlayDrawSound(int currentPathLength)
        {
            if (IsMuted) return;
            OnPlayDrawSound?.Invoke(currentPathLength);
        }

        public ValueTask OnBind(CancellationToken ct) => default;
    }
}