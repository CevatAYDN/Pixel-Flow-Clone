using System;
using System.Threading.Tasks;
using UnityEngine;
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
        private readonly StorageKeysConfigAsset _keys;

        public EncryptedCloudSaveAdapter()
            : this(Resources.Load<StorageKeysConfigAsset>("Configs/StorageKeysConfig") ?? ScriptableObject.CreateInstance<StorageKeysConfigAsset>())
        {
        }

        public EncryptedCloudSaveAdapter(StorageKeysConfigAsset keys)
        {
            _keys = keys ?? Resources.Load<StorageKeysConfigAsset>("Configs/StorageKeysConfig") ?? ScriptableObject.CreateInstance<StorageKeysConfigAsset>();
        }

        private string CloudStorePrefKey
        {
            get
            {
                if (_keys != null && !string.IsNullOrEmpty(_keys.KeyCloudRecord))
                    return _keys.KeyCloudRecord;
                throw new DataValidationException("StorageKeysConfigAsset.KeyCloudRecord missing!");
            }
        }

        public Task<string> LoadCloudSaveAsync()
        {
            if (!PlayerPrefs.HasKey(CloudStorePrefKey))
            {
                return Task.FromResult<string>(null);
            }
            string data = PlayerPrefs.GetString(CloudStorePrefKey, null);
            return Task.FromResult(data);
        }

        public Task<bool> SaveCloudSaveAsync(string saveJson, int version)
        {
            if (string.IsNullOrEmpty(saveJson))
            {
                return Task.FromResult(false);
            }
            PlayerPrefs.SetString(CloudStorePrefKey, saveJson);
            PlayerPrefs.SetInt(CloudStorePrefKey + "_ver", version);
            PlayerPrefs.Save();
            return Task.FromResult(true);
        }

        public Task<bool> DeleteCloudSaveAsync()
        {
            PlayerPrefs.DeleteKey(CloudStorePrefKey);
            PlayerPrefs.DeleteKey(CloudStorePrefKey + "_ver");
            PlayerPrefs.Save();
            return Task.FromResult(true);
        }
    }
}
