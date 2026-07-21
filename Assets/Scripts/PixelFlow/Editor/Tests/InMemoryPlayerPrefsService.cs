using System.Collections.Generic;
using Nexus.Core;
using Nexus.Core.Services;
using PixelFlow.Services;

namespace PixelFlow.Editor.Tests
{
    /// <summary>
    /// In-memory fake for IPlayerPrefsService.
    /// Replaces UnityPlayerPrefs in EditMode tests so models can be constructed
    /// without a running Unity runtime environment.
    /// </summary>
    public sealed class InMemoryPlayerPrefsService : IPlayerPrefsService
    {
        private readonly Dictionary<string, int> _store = new Dictionary<string, int>();
        private readonly Dictionary<string, string> _strings = new Dictionary<string, string>();

        public int GetInt(string key, int defaultValue = 0)
        {
            return _store.TryGetValue(key, out var val) ? val : defaultValue;
        }

        public void SetInt(string key, int value)
        {
            _store[key] = value;
        }

        public bool GetBool(string key, bool defaultValue = false)
        {
            return GetInt(key, defaultValue ? 1 : 0) == 1;
        }

        public void SetBool(string key, bool value)
        {
            SetInt(key, value ? 1 : 0);
        }

        public string GetString(string key, string defaultValue = "")
        {
            return _strings.TryGetValue(key, out var val) ? val : defaultValue;
        }

        public void SetString(string key, string value)
        {
            _strings[key] = value;
        }

        private readonly Dictionary<string, float> _floats = new Dictionary<string, float>();

        public float GetFloat(string key, float defaultValue = 0f)
        {
            return _floats.TryGetValue(key, out var val) ? val : defaultValue;
        }

        public void SetFloat(string key, float value)
        {
            _floats[key] = value;
        }

        private readonly Dictionary<string, long> _longs = new Dictionary<string, long>();

        public long GetLong(string key, long defaultValue = 0L)
        {
            return _longs.TryGetValue(key, out var val) ? val : defaultValue;
        }

        public void SetLong(string key, long value)
        {
            _longs[key] = value;
        }

        public bool HasKey(string key)
        {
            return _store.ContainsKey(key) || _strings.ContainsKey(key) || _floats.ContainsKey(key) || _longs.ContainsKey(key);
        }

        public void DeleteKey(string key)
        {
            _store.Remove(key);
            _strings.Remove(key);
            _floats.Remove(key);
            _longs.Remove(key);
        }

        public void Save() { }
    }
}
