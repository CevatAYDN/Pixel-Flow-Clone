using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using Nexus.Core;
using Nexus.Core.Services;
using PixelFlow.Data;
using UnityEngine;

namespace PixelFlow.Services
{
    /// <summary>
    /// AES-256 Encrypted & Device-Bound Storage Service — STRICT MODE.
    /// game_plan.md §2.2 (Zero Silent Fallback): Eksik/bozuk veri durumunda DataValidationException fırlatır.
    /// Nexus Core EncryptedStorageService'i wrap eder ama defaultValue yerine hata fırlatır.
    /// </summary>
    public class StrictEncryptedStorageService : IPlayerPrefsService, IDisposable
    {
        private readonly EncryptedStorageService _inner;
        private readonly ILoggerService _logger;

        public bool AutoSave { get; set; } = false;

        public StrictEncryptedStorageService(string customSalt = "Nexus_Secure_Salt_2026", ILoggerService logger = null)
        {
            _inner = new EncryptedStorageService(customSalt);
            _logger = logger ?? NexusRuntime.Logger;
        }

        public int GetInt(string key, int defaultValue = 0)
        {
            string valStr = GetString(key, null);
            if (valStr == null)
                throw new DataValidationException($"[StrictEncryptedStorage] Key '{key}' not found or corrupt. Zero-Silent-Fallback policy (§2.2) forbids returning default.");
            if (!int.TryParse(valStr, out int res))
                throw new DataValidationException($"[StrictEncryptedStorage] Key '{key}' contains non-integer value: '{valStr}'");
            return res;
        }

        public void SetInt(string key, int value) => _inner.SetInt(key, value);

        public bool GetBool(string key, bool defaultValue = false)
        {
            string valStr = GetString(key, null);
            if (valStr == null)
                throw new DataValidationException($"[StrictEncryptedStorage] Key '{key}' not found or corrupt. Zero-Silent-Fallback policy (§2.2) forbids returning default.");
            if (!bool.TryParse(valStr, out bool res))
                throw new DataValidationException($"[StrictEncryptedStorage] Key '{key}' contains non-boolean value: '{valStr}'");
            return res;
        }

        public void SetBool(string key, bool value) => _inner.SetBool(key, value);

        public string GetString(string key, string defaultValue = "")
        {
            if (string.IsNullOrEmpty(key))
                throw new DataValidationException("[StrictEncryptedStorage] Key cannot be null or empty.");

            string val = _inner.GetString(key, null);
            if (val == null)
                throw new DataValidationException($"[StrictEncryptedStorage] Key '{key}' not found, tampered, or corrupt. Zero-Silent-Fallback policy (§2.2) forbids returning default.");
            return val;
        }

        public void SetString(string key, string value) => _inner.SetString(key, value);

        public float GetFloat(string key, float defaultValue = 0f)
        {
            string valStr = GetString(key, null);
            if (valStr == null)
                throw new DataValidationException($"[StrictEncryptedStorage] Key '{key}' not found or corrupt. Zero-Silent-Fallback policy (§2.2) forbids returning default.");
            if (!float.TryParse(valStr, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float res))
                throw new DataValidationException($"[StrictEncryptedStorage] Key '{key}' contains non-float value: '{valStr}'");
            return res;
        }

        public void SetFloat(string key, float value) => _inner.SetFloat(key, value);

        public long GetLong(string key, long defaultValue = 0L)
        {
            string valStr = GetString(key, null);
            if (valStr == null)
                throw new DataValidationException($"[StrictEncryptedStorage] Key '{key}' not found or corrupt. Zero-Silent-Fallback policy (§2.2) forbids returning default.");
            if (!long.TryParse(valStr, out long res))
                throw new DataValidationException($"[StrictEncryptedStorage] Key '{key}' contains non-long value: '{valStr}'");
            return res;
        }

        public void SetLong(string key, long value) => _inner.SetLong(key, value);

        public bool HasKey(string key) => _inner.HasKey(key);

        public void DeleteKey(string key) => _inner.DeleteKey(key);

        public void Save() => _inner.Save();

        public void Dispose() => _inner.Dispose();
    }
}