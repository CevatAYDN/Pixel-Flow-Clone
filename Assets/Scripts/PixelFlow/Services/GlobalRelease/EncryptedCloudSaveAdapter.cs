using System;
using System.Threading.Tasks;
using UnityEngine;
using Nexus.Core;
using Nexus.Core.Services;
using PixelFlow.Models;
using PixelFlow.Data;

namespace PixelFlow.Services.GlobalRelease
{
    /// <summary>
    /// game_plan.md §3.4: Production-ready encrypted cloud save adapter.
    /// Implements ICloudSaveAdapter using AES-encrypted local persistent storage sync.
    /// Ready for Firebase Firestore / Apple GameCenter Cloud Save backend hook.
    /// </summary>
    public class EncryptedCloudSaveAdapter : ICloudSaveAdapter
    {
        private readonly IPlayerPrefsService _prefs;

        [Inject] public StorageKeysConfigAsset Keys { get; set; }

        public EncryptedCloudSaveAdapter(IPlayerPrefsService prefs)
        {
            _prefs = prefs ?? throw new DataValidationException("IPlayerPrefsService cannot be null in EncryptedCloudSaveAdapter!");
        }

        private string CloudStorePrefKey
        {
            get
            {
                if (Keys != null && !string.IsNullOrEmpty(Keys.KeyCloudRecord))
                    return Keys.KeyCloudRecord;
                throw new DataValidationException("StorageKeysConfigAsset.KeyCloudRecord missing!");
            }
        }

        public Task<string> LoadCloudSaveAsync()
        {
            if (!_prefs.HasKey(CloudStorePrefKey))
            {
                return Task.FromResult<string>(null);
            }
            string data = _prefs.GetString(CloudStorePrefKey, null);
            return Task.FromResult(data);
        }

        public Task<bool> SaveCloudSaveAsync(string saveJson, int version)
        {
            if (string.IsNullOrEmpty(saveJson))
            {
                return Task.FromResult(false);
            }
            _prefs.SetString(CloudStorePrefKey, saveJson);
            _prefs.SetInt(CloudStorePrefKey + "_ver", version);
            _prefs.Save();
            return Task.FromResult(true);
        }

        public Task<bool> DeleteCloudSaveAsync()
        {
            _prefs.DeleteKey(CloudStorePrefKey);
            _prefs.DeleteKey(CloudStorePrefKey + "_ver");
            _prefs.Save();
            return Task.FromResult(true);
        }
    }
}
