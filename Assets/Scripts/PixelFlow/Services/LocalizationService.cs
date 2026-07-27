using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Nexus.Core;
using Nexus.Core.Services;
using PixelFlow.Data;

namespace PixelFlow.Services
{
    /// <summary>
    /// game_plan.md §6 / §13: 15-Language CSV Data-Driven Localization & RTL Support.
    /// Loads Resources/Localization/LocalizationTable.csv data and supports RTL text handling.
    /// </summary>
    public class LocalizationService : Nexus.Core.Services.ILocalizationService, INexusService
    {
        [Inject, OptionalInject] public ILoggerService LoggerService { get; set; }
        [Inject, OptionalInject] public PixelFlow.Data.GameConfig Config { get; set; }

        private string _currentLanguage = "en";
        private readonly Dictionary<string, string> _dictionary = new Dictionary<string, string>();

        public string CurrentLanguage => _currentLanguage;
        public bool IsRTL => RtlUtility.IsRtlLanguage(_currentLanguage);
        public event Action<string> OnLanguageChanged;

        public ValueTask InitializeAsync(CancellationToken ct)
        {
            string savedLang = PlayerPrefs.GetString("SelectedLanguage", "");
            if (string.IsNullOrEmpty(savedLang))
            {
                savedLang = Application.systemLanguage == SystemLanguage.Turkish ? "tr" : "en";
            }
            _currentLanguage = savedLang.ToLowerInvariant();
            LoadLocalizationTable(_currentLanguage);
            return default;
        }

        public void OnDispose() { }

        public void SetLanguage(string langCode)
        {
            if (string.IsNullOrEmpty(langCode)) return;
            _currentLanguage = langCode.ToLowerInvariant();
            PlayerPrefs.SetString("SelectedLanguage", _currentLanguage);
            PlayerPrefs.Save();
            LoadLocalizationTable(_currentLanguage);
            OnLanguageChanged?.Invoke(_currentLanguage);
        }

        public string GetString(string key, string fallback = "")
        {
            if (string.IsNullOrEmpty(key)) return fallback ?? string.Empty;

            if (_dictionary.TryGetValue(key, out var text))
            {
                return IsRTL ? FormatRTLIfNeeded(text) : text;
            }

            if (!string.IsNullOrEmpty(fallback)) return fallback;

            // Intelligent fallback for known UI format keys
            switch (key)
            {
                case "hub_coin_format": return "Coins: {0}";
                case "hub_play_level_format": return "LEVEL {0}";
                case "hud_level_title_format": return "LEVEL {0}";
                case "hud_score_format": return "Score: {0}";
                case "hud_hint_count_format": return "Hints: {0}";
                case "level_completed_title": return "LEVEL COMPLETED!";
                case "level_completed_score_format": return "Final Score: {0}";
                case "level_completed_stars_label": return "STARS: {0}";
                case "level_failed_title": return "LEVEL FAILED!";
                case "level_failed_retry": return "RETRY";
                case "level_failed_hub": return "MAIN MENU";
                case "garage_equip_label": return "EQUIP";
                case "garage_equipped_label": return "EQUIPPED";
                case "garage_cost_format": return "BUY ({0})";
                case "star_pass_tier_progress_format": return "Tier {0}/{1}";
                default: return key;
            }
        }

        public string GetText(string key, string fallback = null)
        {
            return GetString(key, fallback ?? string.Empty);
        }

        public string FormatRTLIfNeeded(string text)
        {
            return RtlUtility.ProcessRtlText(text);
        }

        public void RegisterLanguageTable(string langCode, IDictionary<string, string> dictionary)
        {
            if (dictionary == null) return;
            foreach (var kvp in dictionary)
            {
                _dictionary[kvp.Key] = kvp.Value;
            }
        }

        public void RegisterKey(string langCode, string key, string value)
        {
            if (!string.IsNullOrEmpty(key) && value != null)
            {
                _dictionary[key] = value;
            }
        }

        private void LoadLocalizationTable(string langCode)
        {
            _dictionary.Clear();
            var textAsset = Resources.Load<TextAsset>("Localization/LocalizationTable");
            if (textAsset == null)
            {
                if (Config != null && Config.AllowLocalizationFallbackDictionary)
                {
                    LoggerService?.LogWarning("[LocalizationService] Resources/Localization/LocalizationTable.csv not found. Using fallback dictionary because Config اجازت veriyor.");
                    PopulateDefaultFallback(langCode);
                    return;
                }

                throw new DataValidationException("Resources/Localization/LocalizationTable.csv bulunamadı ve fallback dictionary devre dışı.");
            }

            using (var reader = new StringReader(textAsset.text))
            {
                string headerLine = reader.ReadLine();
                if (string.IsNullOrEmpty(headerLine)) return;

                string[] headers = headerLine.Split(',');
                int targetCol = -1;
                for (int i = 1; i < headers.Length; i++)
                {
                    if (headers[i].Trim().Equals(langCode, StringComparison.OrdinalIgnoreCase))
                    {
                        targetCol = i;
                        break;
                    }
                }

                if (targetCol == -1) targetCol = 1; // Fallback to first language col (en)

                // Standard LiberationSans SDF font asset only contains Latin/ASCII glyphs.
                // Fallback non-Latin languages to English column to prevent TMPro missing glyph warnings.
                var supportedLatinLangs = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "en", "tr", "es", "de", "fr", "it", "pt", "id" };
                if (!supportedLatinLangs.Contains(langCode))
                {
                    targetCol = 1; // "en"
                }

                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    string[] cols = line.Split(',');
                    if (cols.Length > targetCol)
                    {
                        string key = cols[0].Trim();
                        string val = cols[targetCol].Trim();
                        _dictionary[key] = val;
                    }
                }
            }
        }

        private void PopulateDefaultFallback(string langCode)
        {
            if (Config == null || !Config.AllowLocalizationFallbackDictionary)
                throw new DataValidationException("Localization fallback dictionary disabled. CSV localization table is required.");

            _dictionary[Config.NotificationD1TitleKey] = "Daily Reward Ready!";
            _dictionary[Config.NotificationD1BodyKey] = "New vehicles and challenges are waiting for you!";
            _dictionary[Config.NotificationD2TitleKey] = "Rush Hour Event!";
            _dictionary[Config.NotificationD2BodyKey] = "Earn 2x coins now!";
        }
    }
}
