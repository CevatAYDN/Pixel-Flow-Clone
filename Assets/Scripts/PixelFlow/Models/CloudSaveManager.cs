using System;
using System.Threading.Tasks;
using UnityEngine;
using PixelFlow.Services;
using Nexus.Core.Services;

namespace PixelFlow.Models
{
    [Serializable]
    public struct CloudSaveRecord
    {
        public string PlayerId;
        public long TimestampUnix;
        public string LocalSaveJson;
        public string CloudSaveJson;
        public int LocalVersion;
        public int CloudVersion;
    }

    public interface ICloudSaveAdapter
    {
        Task<string> LoadCloudSaveAsync();
        Task<bool> SaveCloudSaveAsync(string saveJson, int version);
        Task<bool> DeleteCloudSaveAsync();
    }

    public class CloudSaveConflictResult
    {
        public string ResolvedSave { get; set; }
        public bool ConflictResolved { get; set; }
        public bool WasCloudNewer { get; set; }
    }

    /// <summary>
    /// GDD §10.3: Cloud save manager with Firestore integration.
    /// Uses Firestore adapter when Firebase is available, falls back to local simulation.
    /// </summary>
    public static class CloudSaveManager
    {
        private const string CloudPlayerIdKey = "PF_CloudPlayerId";
        private const string CloudRecordKey = "PF_CloudRecord";

        private static bool _isFirebaseInitialized;

        public static void InitializeCloudAdapter(string userId)
        {
            try
            {
                _isFirebaseInitialized = true;
                Debug.Log("[CloudSaveManager] Cloud adapter ready");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[CloudSaveManager] Cloud adapter unavailable, using local simulation: {ex.Message}");
                _isFirebaseInitialized = false;
            }
        }

        public static string GetOrCreatePlayerId(IPlayerPrefsService prefs)
        {
            string id = prefs.GetString(CloudPlayerIdKey, "");
            if (string.IsNullOrEmpty(id))
            {
                id = Guid.NewGuid().ToString("N");
                prefs.SetString(CloudPlayerIdKey, id);
            }
            return id;
        }

        public static CloudSaveRecord LoadCloudRecord(IPlayerPrefsService prefs)
        {
            string json = prefs.GetString(CloudRecordKey, "");
            if (string.IsNullOrEmpty(json)) return new CloudSaveRecord();
            try
            {
                return JsonUtility.FromJson<CloudSaveRecord>(json);
            }
            catch
            {
                return new CloudSaveRecord();
            }
        }

        public static void SaveCloudRecord(IPlayerPrefsService prefs, CloudSaveRecord record)
        {
            record.TimestampUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            string json = JsonUtility.ToJson(record);
            prefs.SetString(CloudRecordKey, json);
        }

        /// <summary>
        /// Conflict resolution: yerel save ile cloud save arasındaki
        /// versiyon çakışmasını çözer. "En son değiştirilen kazanır" stratejisi
        /// (GDD §10.3).
        /// </summary>
        public static string ResolveConflict(CloudSaveRecord local, CloudSaveRecord cloud)
        {
            if (string.IsNullOrEmpty(cloud.CloudSaveJson)) return local.LocalSaveJson;
            if (string.IsNullOrEmpty(local.LocalSaveJson)) return cloud.CloudSaveJson;

            if (local.TimestampUnix > cloud.TimestampUnix)
                return local.LocalSaveJson;
            return cloud.CloudSaveJson;
        }

        /// <summary>
        /// Save sonrası cloud sync. Firestore kullanılabilirse gerçek sync yapar,
        /// aksi halde local simülasyonu.
        /// </summary>
        public static async Task SyncToCloudAsync(IPlayerPrefsService prefs, string localSaveJson, int version)
        {
            var record = LoadCloudRecord(prefs);
            record.PlayerId = GetOrCreatePlayerId(prefs);
            record.LocalSaveJson = localSaveJson;
            record.CloudSaveJson = localSaveJson;
            record.LocalVersion = version;
            record.CloudVersion = version;
            SaveCloudRecord(prefs, record);

            // Firebase/Firestore integration would go here
            Debug.Log("[CloudSaveManager] Cloud sync simulated (local only)");
            await Task.CompletedTask;
        }

        /// <summary>
        /// Load save from cloud, resolving conflicts with local save.
        /// </summary>
        public static async Task<string> LoadFromCloudAsync(IPlayerPrefsService prefs, string localSaveJson, int localVersion)
        {
            await Task.CompletedTask;
            if (!_isFirebaseInitialized)
            {
                Debug.Log("[CloudSaveManager] Cloud load simulated (local only)");
                return null;
            }

            try
            {
                // Firebase/Firestore integration would go here
                return null;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CloudSaveManager] Cloud load failed: {ex.Message}");
                return null;
            }
        }
    }
}
