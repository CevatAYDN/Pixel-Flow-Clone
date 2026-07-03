using System;
using PixelFlow.Services;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Nexus.Core;

namespace PixelFlow.Models
{
    public enum AppTheme { Dark, Light, Neon }
    public enum ColorBlindMode { None, Protanopia, Deuteranopia, Tritanopia }

    public interface ISettingsModel
    {
        AppTheme CurrentTheme { get; }
        ColorBlindMode CurrentColorBlindMode { get; }
        float MasterVolume { get; }
        float SfxVolume { get; }
        float MusicVolume { get; }
        event Action<AppTheme> OnThemeChanged;
        event Action<ColorBlindMode> OnColorBlindModeChanged;
        void SetTheme(AppTheme theme);
        void SetColorBlindMode(ColorBlindMode mode);
        void SetMasterVolume(float volume);
        void SetSfxVolume(float volume);
        void SetMusicVolume(float volume);
    }

    /// <summary>
    /// Tema tercihini IPlayerPrefsService üzerinden kalıcı saklar.
    /// Geçersiz kayıtlı değer gelirse varsayılan (Dark) kullanılır.
    /// </summary>
    public class SettingsModel : ISettingsModel, IReactiveModel
    {
        private const string KeyTheme = "AppTheme";
        private const string KeyColorBlind = "ColorBlindMode";
        private const string KeyMasterVol = "MasterVolume";
        private const string KeySfxVol = "SfxVolume";
        private const string KeyMusicVol = "MusicVolume";
        private const AppTheme DefaultTheme = AppTheme.Dark;

        private readonly IPlayerPrefsService _prefs;

        public AppTheme CurrentTheme { get; private set; }
        public ColorBlindMode CurrentColorBlindMode { get; private set; }
        public float MasterVolume { get; private set; } = 1f;
        public float SfxVolume { get; private set; } = 1f;
        public float MusicVolume { get; private set; } = 0.7f;

        public event Action<AppTheme> OnThemeChanged;
        public event Action<ColorBlindMode> OnColorBlindModeChanged;

        public SettingsModel(IPlayerPrefsService prefs)
        {
            _prefs = prefs ?? throw new System.ArgumentNullException(nameof(prefs));
            int raw = _prefs.GetInt(KeyTheme, (int)DefaultTheme);
            CurrentTheme = IsValidTheme(raw) ? (AppTheme)raw : DefaultTheme;

            int cbRaw = _prefs.GetInt(KeyColorBlind, 0);
            CurrentColorBlindMode = cbRaw >= 0 && cbRaw <= 3 ? (ColorBlindMode)cbRaw : ColorBlindMode.None;

            MasterVolume = _prefs.GetInt(KeyMasterVol, 100) / 100f;
            SfxVolume = _prefs.GetInt(KeySfxVol, 100) / 100f;
            MusicVolume = _prefs.GetInt(KeyMusicVol, 70) / 100f;
        }

        public void SetTheme(AppTheme theme)
        {
            if (CurrentTheme == theme) return;
            CurrentTheme = theme;
            _prefs.SetInt(KeyTheme, (int)theme);
            OnThemeChanged?.Invoke(CurrentTheme);
        }

        public void SetColorBlindMode(ColorBlindMode mode)
        {
            if (CurrentColorBlindMode == mode) return;
            CurrentColorBlindMode = mode;
            _prefs.SetInt(KeyColorBlind, (int)mode);
            OnColorBlindModeChanged?.Invoke(mode);
        }

        public void SetMasterVolume(float volume) { MasterVolume = Mathf.Clamp01(volume); _prefs.SetInt(KeyMasterVol, Mathf.RoundToInt(MasterVolume * 100f)); }
        public void SetSfxVolume(float volume) { SfxVolume = Mathf.Clamp01(volume); _prefs.SetInt(KeySfxVol, Mathf.RoundToInt(SfxVolume * 100f)); }
        public void SetMusicVolume(float volume) { MusicVolume = Mathf.Clamp01(volume); _prefs.SetInt(KeyMusicVol, Mathf.RoundToInt(MusicVolume * 100f)); }

        private static bool IsValidTheme(int value)
        {
            return value >= (int)AppTheme.Dark && value <= (int)AppTheme.Neon;
        }

        public ValueTask OnBind(CancellationToken ct) => default;
    }
}