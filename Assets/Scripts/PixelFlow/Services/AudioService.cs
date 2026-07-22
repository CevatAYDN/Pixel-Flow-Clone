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
        private readonly Dictionary<SfxType, AudioClip> _clips = new Dictionary<SfxType, AudioClip>();
        private GameObject _audioRoot;
        private float _masterVolume = 1f;
        private float _sfxVolume = 1f;
        private float _musicVolume = 0.7f;

        // SfxType'ları SFX mi Music/Ambient mi diye ayıran yardımcı
        private static bool IsMusicOrAmbient(SfxType type)
        {
            return type == SfxType.AmbientHub || type == SfxType.AmbientPuzzle ||
                   type == SfxType.AmbientOverclock || type == SfxType.MainTheme ||
                   type == SfxType.VehicleEngine;
        }

        public ValueTask InitializeAsync(CancellationToken ct)
        {
            if (!Application.isPlaying) return default;
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
                _clips[type] = CreateClipForType(type);
                if (_clips[type] != null) source.clip = _clips[type];
            }

            // Başlangıç volume ayarlarını SettingsModel'den al
            if (SettingsModel != null)
            {
                _masterVolume = SettingsModel.MasterVolume;
                _sfxVolume = SettingsModel.SfxVolume;
                _musicVolume = SettingsModel.MusicVolume;
            }
            ApplyAllVolumes();

            return default;
        }

        private void ApplyAllVolumes()
        {
            foreach (var kvp in _sources)
            {
                float typeVol = IsMusicOrAmbient(kvp.Key) ? _musicVolume : _sfxVolume;
                kvp.Value.volume = typeVol * _masterVolume;
            }
        }

        private static AudioClip CreateClipForType(SfxType type)
        {
            return AudioClipProvider.Load(type);
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
                float typeVol = IsMusicOrAmbient(type) ? _musicVolume : _sfxVolume;
                source.volume = typeVol * _masterVolume;
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
            _masterVolume = Mathf.Clamp01(volume);
            ApplyAllVolumes();
        }

        public void SetSfxVolume(float volume)
        {
            _sfxVolume = Mathf.Clamp01(volume);
            foreach (var kvp in _sources)
            {
                if (!IsMusicOrAmbient(kvp.Key))
                {
                    kvp.Value.volume = _sfxVolume * _masterVolume;
                }
            }
        }

        public void SetMusicVolume(float volume)
        {
            _musicVolume = Mathf.Clamp01(volume);
            foreach (var kvp in _sources)
            {
                if (IsMusicOrAmbient(kvp.Key))
                {
                    kvp.Value.volume = _musicVolume * _masterVolume;
                }
            }
        }
    }
}
