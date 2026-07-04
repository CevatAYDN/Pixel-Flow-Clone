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

            return default;
        }

        private static AudioClip CreateClipForType(SfxType type)
        {
            switch (type)
            {
                case SfxType.Crash: return ProceduralAudioFactory.CreateCrash();
                case SfxType.Horn: return ProceduralAudioFactory.CreateHorn();
                case SfxType.ViaductPlace: return ProceduralAudioFactory.CreateViaductPlace();
                case SfxType.LevelComplete: return ProceduralAudioFactory.CreateLevelComplete();
                case SfxType.CoinCollect: return ProceduralAudioFactory.CreateCoinCollect();
                case SfxType.UIClick: return ProceduralAudioFactory.CreateUIClick();
                case SfxType.AmbientHub: return ProceduralAudioFactory.CreateAmbientHub();
                case SfxType.AmbientPuzzle: return ProceduralAudioFactory.CreateAmbientPuzzle();
                case SfxType.AmbientOverclock: return ProceduralAudioFactory.CreateAmbientOverclock();
                case SfxType.MainTheme: return ProceduralAudioFactory.CreateMainTheme();
                case SfxType.VehicleEngine: return ProceduralAudioFactory.CreateVehicleEngine();
                case SfxType.PathDraw: return ProceduralAudioFactory.CreatePathDraw();
                default: return null;
            }
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
