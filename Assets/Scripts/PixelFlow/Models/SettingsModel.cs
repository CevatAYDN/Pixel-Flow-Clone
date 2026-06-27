using System;
using PixelFlow.Services;

namespace PixelFlow.Models
{
    public enum AppTheme { Dark, Light, Neon }

    public interface ISettingsModel
    {
        AppTheme CurrentTheme { get; }
        event Action<AppTheme> OnThemeChanged;
        void SetTheme(AppTheme theme);
    }

    /// <summary>
    /// Tema tercihini IPlayerPrefsService üzerinden kalıcı saklar.
    /// Geçersiz kayıtlı değer gelirse varsayılan (Dark) kullanılır.
    /// </summary>
    public class SettingsModel : ISettingsModel
    {
        private const string Key = "AppTheme";
        private const AppTheme DefaultTheme = AppTheme.Dark;

        private readonly IPlayerPrefsService _prefs;

        public AppTheme CurrentTheme { get; private set; }
        public event Action<AppTheme> OnThemeChanged;

        public SettingsModel(IPlayerPrefsService prefs)
        {
            _prefs = prefs ?? throw new System.ArgumentNullException(nameof(prefs));
            int raw = _prefs.GetInt(Key, (int)DefaultTheme);
            CurrentTheme = IsValidTheme(raw) ? (AppTheme)raw : DefaultTheme;
        }

        public void SetTheme(AppTheme theme)
        {
            if (CurrentTheme == theme) return;
            CurrentTheme = theme;
            _prefs.SetInt(Key, (int)theme);
            OnThemeChanged?.Invoke(CurrentTheme);
        }

        private static bool IsValidTheme(int value)
        {
            return value >= (int)AppTheme.Dark && value <= (int)AppTheme.Neon;
        }
    }
}