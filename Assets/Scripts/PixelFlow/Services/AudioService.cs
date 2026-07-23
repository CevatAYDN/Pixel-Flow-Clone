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
        PowerUpActivate, PowerUpClear,
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

        private const int PoolSize = 3;

        private static readonly HashSet<SfxType> CriticalTypes = new HashSet<SfxType>
        {
            SfxType.Crash,
            SfxType.UIClick,
            SfxType.PathDraw,
            SfxType.LevelComplete
        };

        private readonly Dictionary<SfxType, List<AudioSource>> _sources = new Dictionary<SfxType, List<AudioSource>>();
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

        private static bool IsCritical(SfxType type) => CriticalTypes.Contains(type);

        public ValueTask InitializeAsync(CancellationToken ct)
        {
            if (!Application.isPlaying) return default;
            _audioRoot = new GameObject("[AudioService]");
            _audioRoot.hideFlags = HideFlags.DontSave;
            UnityEngine.Object.DontDestroyOnLoad(_audioRoot);

            foreach (SfxType type in Enum.GetValues(typeof(SfxType)))
            {
                int count = IsCritical(type) ? PoolSize : 1;
                var list = new List<AudioSource>(count);

                for (int i = 0; i < count; i++)
                {
                    var child = new GameObject($"{type}_{i}");
                    child.transform.SetParent(_audioRoot.transform);
                    var source = child.AddComponent<AudioSource>();
                    source.playOnAwake = false;
                    source.loop = (type == SfxType.VehicleEngine || type == SfxType.AmbientHub ||
                                   type == SfxType.AmbientPuzzle || type == SfxType.AmbientOverclock ||
                                   type == SfxType.MainTheme);
                    list.Add(source);
                }

                _sources[type] = list;
                _clips[type] = CreateClipForType(type);
                if (_clips[type] != null)
                {
                    foreach (var source in list)
                        source.clip = _clips[type];
                }
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
                foreach (var source in kvp.Value)
                    source.volume = typeVol * _masterVolume;
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
            if (_sources.TryGetValue(type, out var list))
            {
                float typeVol = IsMusicOrAmbient(type) ? _musicVolume : _sfxVolume;
                float vol = typeVol * _masterVolume;

                // Find first idle source, or fall back to the first (oldest) source
                AudioSource target = null;
                foreach (var source in list)
                {
                    if (!source.isPlaying)
                    {
                        target = source;
                        break;
                    }
                }

                target ??= list[0]; // all busy — reuse the first/oldest

                target.volume = vol;
                target.Play();
            }
        }

        public void StopSfx(SfxType type)
        {
            if (_sources.TryGetValue(type, out var list))
            {
                foreach (var source in list)
                    source.Stop();
            }
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
                    float vol = _sfxVolume * _masterVolume;
                    foreach (var source in kvp.Value)
                        source.volume = vol;
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
                    float vol = _musicVolume * _masterVolume;
                    foreach (var source in kvp.Value)
                        source.volume = vol;
                }
            }
        }
    }
}
