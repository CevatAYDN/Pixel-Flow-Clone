using System.Collections.Generic;
using UnityEngine;

namespace Nexus.Core.Services
{
    /// <summary>
    /// Loads external localization tables from Resources/Localization/ folder.
    /// Expected format per file: JSON array of { "langCode": "en", "key": "value" } entries.
    /// Falls back gracefully if no localization resources exist — built-in tables in
    /// LocalizationService handle default English/Turkish strings.
    /// </summary>
    public sealed class ResourceLocalizationTableProvider : ILocalizationTableProvider
    {
        private Dictionary<string, Dictionary<string, string>> _tables;
        private bool _initialized;

        public bool TryGetTable(string langCode, out IDictionary<string, string> table)
        {
            if (string.IsNullOrEmpty(langCode))
            {
                table = null;
                return false;
            }

            if (!_initialized)
                LoadTables();

            langCode = langCode.ToLower();

            if (_tables != null && _tables.TryGetValue(langCode, out var dict))
            {
                table = dict;
                return true;
            }

            table = null;
            return false;
        }

        private void LoadTables()
        {
            _initialized = true;

            try
            {
                var assets = Resources.LoadAll<TextAsset>("Localization");
                if (assets == null || assets.Length == 0)
                {
                    _tables = new Dictionary<string, Dictionary<string, string>>(0);
                    return;
                }

                _tables = new Dictionary<string, Dictionary<string, string>>();

                foreach (var asset in assets)
                {
                    if (asset == null || string.IsNullOrEmpty(asset.text))
                        continue;

                    // Support two JSON formats:
                    // 1. { "langCode": "en", "entries": { "key": "val" } }
                    // 2. [ { "langCode": "en", "key": "val" }, ... ]
                    ParseTableFile(asset.text);
                }
            }
            catch
            {
                // If Resources folder or files are missing, fall back gracefully
                _tables = new Dictionary<string, Dictionary<string, string>>(0);
            }
        }

        private void ParseTableFile(string json)
        {
            // Expected JSON: { "langCode": "en", "entries": [ { "key": "...", "value": "..." } ] }
            // Unity's JsonUtility does not support Dictionary deserialization, so we use a flat array.
            try
            {
                var wrapper = new LocalizationTableWrapper();
                JsonUtility.FromJsonOverwrite(json, wrapper);

                if (wrapper == null || string.IsNullOrEmpty(wrapper.langCode) || wrapper.entries == null)
                    return;

                var langCode = wrapper.langCode.ToLower();
                if (!_tables.ContainsKey(langCode))
                    _tables[langCode] = new Dictionary<string, string>();

                for (int i = 0; i < wrapper.entries.Length; i++)
                {
                    var entry = wrapper.entries[i];
                    if (entry == null || string.IsNullOrEmpty(entry.key))
                        continue;
                    _tables[langCode][entry.key] = entry.value ?? string.Empty;
                }
            }
            catch
            {
                // Silently ignore unparseable files
            }
        }

        [System.Serializable]
        private sealed class LocalizationTableWrapper
        {
            public string langCode;
            public LocalizationEntry[] entries;
        }

        [System.Serializable]
        private sealed class LocalizationEntry
        {
            public string key;
            public string value;
        }
    }
}
