using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Nexus.Core;
using PixelFlow.Models;
using UnityEngine;

namespace PixelFlow.Services
{
    public class LocalizationService : ILocalizationService, INexusService
    {
        [Inject] public IPlayerPrefsService PlayerPrefsService { get; set; }

        public string CurrentLanguage { get; private set; } = "en";
        public event Action<string> OnLanguageChanged;
        public bool IsRTL => CurrentLanguage == "ar";

        private readonly Dictionary<string, Dictionary<string, string>> _localizedTable = new Dictionary<string, Dictionary<string, string>>();

        public ValueTask InitializeAsync(CancellationToken ct)
        {
            LoadSavedLanguage();
            BuildLocalizationDictionary();
            return default;
        }

        public void OnDispose()
        {
            _localizedTable.Clear();
        }

        private void LoadSavedLanguage()
        {
            if (PlayerPrefsService != null)
            {
                CurrentLanguage = PlayerPrefsService.GetString("NT_Language", "en");
            }
        }

        public void SetLanguage(string langCode)
        {
            if (string.IsNullOrEmpty(langCode) || CurrentLanguage == langCode) return;
            CurrentLanguage = langCode.ToLower();
            if (PlayerPrefsService != null)
            {
                PlayerPrefsService.SetString("NT_Language", CurrentLanguage);
                PlayerPrefsService.Save();
            }
            OnLanguageChanged?.Invoke(CurrentLanguage);
        }

        public string GetString(string key, string fallback = "")
        {
            if (string.IsNullOrEmpty(key)) return fallback;

            if (_localizedTable.TryGetValue(CurrentLanguage, out var dict) && dict.TryGetValue(key, out var val))
            {
                return FormatRTLIfNeeded(val);
            }
            if (_localizedTable.TryGetValue("en", out var enDict) && enDict.TryGetValue(key, out var enVal))
            {
                return FormatRTLIfNeeded(enVal);
            }
            return FormatRTLIfNeeded(!string.IsNullOrEmpty(fallback) ? fallback : key);
        }

        public string FormatRTLIfNeeded(string text)
        {
            if (string.IsNullOrEmpty(text) || !IsRTL) return text;
            // Simple RTL character reversal helper for standard UI rendering in Arabic
            char[] chars = text.ToCharArray();
            Array.Reverse(chars);
            return new string(chars);
        }

        private void BuildLocalizationDictionary()
        {
            // GDD §15 Target Languages: EN, TR, DE, FR, ES, JP, KR, AR, CN, PT, RU
            var en = new Dictionary<string, string>
            {
                { "app_name", "Neon Transit" },
                { "btn_play", "Play" },
                { "btn_undo", "Undo" },
                { "btn_redo", "Redo" },
                { "btn_viaduct", "Viaduct" },
                { "btn_hint", "Hint" },
                { "btn_hub", "Return to Hub" },
                { "crisis_title", "Traffic Crisis! 🚨" },
                { "win_title", "Level Completed! 🎉" },
                { "daily_contracts", "Daily Contracts" },
                { "overclock_title", "Rush Hour (Overclock)" },
                { "welcome_offline", "Welcome Back!" },
                { "collect_tax", "Collect Tax" }
            };

            var tr = new Dictionary<string, string>
            {
                { "app_name", "Neon Transit" },
                { "btn_play", "Oyna" },
                { "btn_undo", "Geri Al" },
                { "btn_redo", "İleri Al" },
                { "btn_viaduct", "Viyadük" },
                { "btn_hint", "İpucu" },
                { "btn_hub", "Şehre Dön" },
                { "crisis_title", "Trafik Krizi! 🚨" },
                { "win_title", "Bölüm Tamamlandı! 🎉" },
                { "daily_contracts", "Günlük Kontratlar" },
                { "overclock_title", "Yoğun Saat (Overclock)" },
                { "welcome_offline", "Tekrar Hoş Geldin!" },
                { "collect_tax", "Vergi Topla" }
            };

            _localizedTable["en"] = en;
            _localizedTable["tr"] = tr;
            // Additional languages default to English fallback if key is requested
        }
    }
}
