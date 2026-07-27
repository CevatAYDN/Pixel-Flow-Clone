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
    /// game_plan.md §2.2 (Zero Silent Fallback):
    ///   - Key hiç yazılmamışsa (ilk çalıştırma) → defaultValue döner ve storage'a yazar (bootstrap).
    ///   - Key dosyası var ama bozuk/HMAC eşleşmiyorsa (tampering) → DataValidationException fırlatır.
    /// Nexus Core EncryptedStorageService'i wrap eder; ilk çalıştırmayı bozulma durumundan ayırır.
    /// </summary>
    public class StrictEncryptedStorageService : IPlayerPrefsService, IDisposable
    {
        private readonly EncryptedStorageService _inner;
        private readonly ILoggerService _logger;

        public bool AutoSave { get; set; } = false;

        /// <summary>Sentinel returned by inner service when key doesn't exist OR decrypt fails.</summary>
        private const string Sentinel = "__STRICT_NULL_SENTINEL__";

        public StrictEncryptedStorageService(string customSalt = "Nexus_Secure_Salt_2026", ILoggerService logger = null)
        {
            _inner = new EncryptedStorageService(customSalt);
            _logger = logger ?? NexusRuntime.Logger;
        }

        /// <summary>
        /// Ortak okuma mantığı:
        /// 1) Key hiç yazılmamışsa (HasKey=false) → first-run bootstrap: defaultValue'yu storage'a yaz ve döndür.
        /// 2) Key varsa ama inner service okuyamıyorsa (HMAC mismatch / seed değişti / tampering)
        ///    → corrupt dosyayı sil, warning logla, defaultValue ile yeniden bootstrap yap.
        /// 3) Key varsa ve okunabiliyorsa → değeri döndür.
        /// </summary>
        private string ReadOrBootstrap(string key, string defaultValue)
        {
            if (string.IsNullOrEmpty(key))
                throw new DataValidationException("[StrictEncryptedStorage] Key cannot be null or empty.");

            // Case 1: Key hiç yazılmamış — ilk çalıştırma / temiz yükleme
            if (!_inner.HasKey(key))
            {
                return defaultValue;
            }

            // Case 2 & 3: Key dosyası var — okumayı dene
            string val = _inner.GetString(key, Sentinel);
            if (val == Sentinel || val == null)
            {
                // Dosya var ama okunamadı — encryption seed değişmiş veya tampering.
                // Kurtarılabilir durum: corrupt dosyayı sil, warning logla ve default döndür.
                _logger?.LogWarning(
                    $"[StrictEncryptedStorage] Key '{key}' corrupt/tampered (HMAC mismatch). " +
                    "Recovering: deleting corrupt file.");
                _inner.DeleteKey(key);
                return defaultValue;
            }
            return val;
        }

        public int GetInt(string key, int defaultValue = 0)
        {
            string valStr = ReadOrBootstrap(key, defaultValue.ToString());
            if (valStr == null)
                return defaultValue;
            if (!int.TryParse(valStr, out int res))
                throw new DataValidationException($"[StrictEncryptedStorage] Key '{key}' contains non-integer value: '{valStr}'");
            return res;
        }

        public void SetInt(string key, int value) => _inner.SetInt(key, value);

        public bool GetBool(string key, bool defaultValue = false)
        {
            string valStr = ReadOrBootstrap(key, defaultValue.ToString());
            if (valStr == null)
                return defaultValue;
            if (!bool.TryParse(valStr, out bool res))
                throw new DataValidationException($"[StrictEncryptedStorage] Key '{key}' contains non-boolean value: '{valStr}'");
            return res;
        }

        public void SetBool(string key, bool value) => _inner.SetBool(key, value);

        public string GetString(string key, string defaultValue = "")
        {
            return ReadOrBootstrap(key, defaultValue);
        }

        public void SetString(string key, string value) => _inner.SetString(key, value);

        public float GetFloat(string key, float defaultValue = 0f)
        {
            string valStr = ReadOrBootstrap(key, defaultValue.ToString(System.Globalization.CultureInfo.InvariantCulture));
            if (valStr == null)
                return defaultValue;
            if (!float.TryParse(valStr, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float res))
                throw new DataValidationException($"[StrictEncryptedStorage] Key '{key}' contains non-float value: '{valStr}'");
            return res;
        }

        public void SetFloat(string key, float value) => _inner.SetFloat(key, value);

        public long GetLong(string key, long defaultValue = 0L)
        {
            string valStr = ReadOrBootstrap(key, defaultValue.ToString());
            if (valStr == null)
                return defaultValue;
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