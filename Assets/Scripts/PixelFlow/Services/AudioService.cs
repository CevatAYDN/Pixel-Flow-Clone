using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Nexus.Core;
using PixelFlow.Models;

namespace PixelFlow.Services
{
    public enum SfxType
    {
        UIClick, PathDraw, VehicleEngine, Crash, Horn,
        CoinCollect, LevelComplete, ViaductPlace,
        AmbientHub, AmbientPuzzle, AmbientOverclock, MainTheme
    }

    public interface IAudioService
    {
        void PlaySfx(SfxType type);
        void StopSfx(SfxType type);
        void SetMasterVolume(float volume);
        void SetSfxVolume(float volume);
        void SetMusicVolume(float volume);
    }

    public class AudioService : IAudioService, INexusService
    {
        [Inject] public ISettingsModel SettingsModel { get; set; }

        private readonly Dictionary<SfxType, AudioSource> _sources = new Dictionary<SfxType, AudioSource>();
        private GameObject _audioRoot;

        public ValueTask InitializeAsync(CancellationToken ct)
        {
            _audioRoot = new GameObject("[AudioService]");
            _audioRoot.hideFlags = HideFlags.DontSave;
            UnityEngine.Object.DontDestroyOnLoad(_audioRoot);

            foreach (SfxType type in Enum.GetValues(typeof(SfxType)))
            {
                var child = new GameObject(type.ToString());
                child.transform.SetParent(_audioRoot.transform);
                var source = child.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.loop = (type == SfxType.VehicleEngine || type == SfxType.AmbientHub || 
                               type == SfxType.AmbientPuzzle || type == SfxType.AmbientOverclock || 
                               type == SfxType.MainTheme);
                _sources[type] = source;
            }

            return default;
        }

        public void OnDispose()
        {
            if (_audioRoot != null)
                UnityEngine.Object.Destroy(_audioRoot);
            _sources.Clear();
        }

        public void PlaySfx(SfxType type)
        {
            if (_sources.TryGetValue(type, out var source))
            {
                float vol = type == SfxType.MainTheme ? SettingsModel.MusicVolume : SettingsModel.SfxVolume;
                source.volume = vol * SettingsModel.MasterVolume;
                if (!source.isPlaying)
                    source.Play();
            }
        }

        public void StopSfx(SfxType type)
        {
            if (_sources.TryGetValue(type, out var source))
                source.Stop();
        }

        public void SetMasterVolume(float volume)
        {
            foreach (var kvp in _sources)
                kvp.Value.volume = volume;
        }

        public void SetSfxVolume(float volume) { }
        public void SetMusicVolume(float volume) { }
    }
}
