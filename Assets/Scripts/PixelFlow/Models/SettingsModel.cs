using System;
using UnityEngine;

namespace PixelFlow.Models
{
    public enum AppTheme { Dark, Light, Neon }

    public interface ISettingsModel
    {
        AppTheme CurrentTheme { get; }
        event Action<AppTheme> OnThemeChanged;
        void SetTheme(AppTheme theme);
    }

    public class SettingsModel : ISettingsModel
    {
        public AppTheme CurrentTheme { get; private set; }
        public event Action<AppTheme> OnThemeChanged;

        public SettingsModel()
        {
            CurrentTheme = (AppTheme)PlayerPrefs.GetInt("AppTheme", (int)AppTheme.Dark);
        }

        public void SetTheme(AppTheme theme)
        {
            if (CurrentTheme != theme)
            {
                CurrentTheme = theme;
                PlayerPrefs.SetInt("AppTheme", (int)theme);
                PlayerPrefs.Save();
                OnThemeChanged?.Invoke(CurrentTheme);
            }
        }
    }
}
