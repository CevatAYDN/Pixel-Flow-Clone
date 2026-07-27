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
            LoadLocalizationTable(_currentLanguage);
            return default;
        }

        public void OnDispose() { }

        public void SetLanguage(string langCode)
        {
            if (string.IsNullOrEmpty(langCode)) return;
            _currentLanguage = langCode.ToLowerInvariant();
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

            return !string.IsNullOrEmpty(fallback) ? fallback : key;
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
