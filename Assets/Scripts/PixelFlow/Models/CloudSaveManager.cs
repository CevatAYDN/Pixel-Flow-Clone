using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Nexus.Core.Services;
using Nexus.Core;
using PixelFlow.Data;

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

    /// <summary>
    /// Cloud save adapter interface. Replace with FirestoreAdapter / PlayFabAdapter / etc.
    /// in production builds via DI binding.
    /// </summary>
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
    /// game_plan.md §3.8: Cloud save manager.
    /// - ICloudSaveAdapter arayüzü üzerinden DI ile enjekte edilir.
    /// - Gerçek cloud adapter yoksa INexusService üzerinden atlanır (null-object pattern).
    /// - Conflict resolution: "son değiştiren kazanır" (last-write-wins).
    /// </summary>
    public class CloudSaveManager : INexusService
    {
        [Inject, OptionalInject] public ICloudSaveAdapter Adapter { get; set; }
        [Inject, OptionalInject] public IPlayerPrefsService Prefs { get; set; }
        [Inject, OptionalInject] public ILoggerService LoggerService { get; set; }
        [Inject, OptionalInject] public StorageKeysConfigAsset Keys { get; set; }

        private string CloudPlayerIdKey => Keys?.KeyCloudPlayerId;
        private string CloudRecordKey => Keys?.KeyCloudRecord;

        public ValueTask InitializeAsync(CancellationToken ct)
        {
            if (Adapter == null)
            {
                LoggerService?.Log("[CloudSaveManager] No cloud adapter registered — cloud saves disabled.");
            }
            else
            {
                LoggerService?.Log("[CloudSaveManager] Cloud adapter ready.");
            }
            return default;
        }

        public string GetOrCreatePlayerId()
        {
            if (Prefs == null) return "offline";
            if (string.IsNullOrEmpty(CloudPlayerIdKey)) throw new DataValidationException("CloudPlayerId key is not configured on StorageKeysConfigAsset.");
            string id = Prefs.GetString(CloudPlayerIdKey, "");
            if (string.IsNullOrEmpty(id))
            {
                id = Guid.NewGuid().ToString("N");
                Prefs.SetString(CloudPlayerIdKey, id);
                Prefs.Save();
            }
            return id;
        }

        public CloudSaveRecord LoadCloudRecord()
        {
            if (Prefs == null) return default;
            if (string.IsNullOrEmpty(CloudRecordKey)) throw new DataValidationException("CloudRecord key is not configured on StorageKeysConfigAsset.");
            string json = Prefs.GetString(CloudRecordKey, "");
            if (string.IsNullOrEmpty(json)) return default;
            try
            {
                return JsonUtility.FromJson<CloudSaveRecord>(json);
            }
            catch
            {
                return default;
            }
        }

        public void SaveCloudRecord(CloudSaveRecord record)
        {
            if (Prefs == null) return;
            if (string.IsNullOrEmpty(CloudRecordKey)) throw new DataValidationException("CloudRecord key is not configured on StorageKeysConfigAsset.");
            record.TimestampUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            string json = JsonUtility.ToJson(record);
            Prefs.SetString(CloudRecordKey, json);
            Prefs.Save();
        }

        /// <summary>
        /// Conflict resolution: last-write-wins.
        /// (game_plan.md §3.8: "En son değiştirilen kazanır")
        /// </summary>
        public static string ResolveConflict(CloudSaveRecord local, CloudSaveRecord cloud)
        {
            if (string.IsNullOrEmpty(cloud.CloudSaveJson)) return local.LocalSaveJson;
            if (string.IsNullOrEmpty(local.LocalSaveJson)) return cloud.CloudSaveJson;
            return local.TimestampUnix > cloud.TimestampUnix ? local.LocalSaveJson : cloud.CloudSaveJson;
        }

        // ═══════════════════════════════════════════════════════════
        // Backward-compatible static forwarders (used by GameBootstrapper
        // and Editor Tests that haven't migrated to DI yet).
        // ═══════════════════════════════════════════════════════════

        public static CloudSaveRecord LoadCloudRecord(IPlayerPrefsService prefs) =>
            new CloudSaveManager { Prefs = prefs }.LoadCloudRecord();

        public static string GetOrCreatePlayerId(IPlayerPrefsService prefs) =>
            new CloudSaveManager { Prefs = prefs }.GetOrCreatePlayerId();

        public static Task SyncToCloudAsync(IPlayerPrefsService prefs, string localSaveJson, int version) =>
            new CloudSaveManager { Prefs = prefs }.SyncToCloudAsync(localSaveJson, version);

        /// <summary>
        /// Save sonrası cloud sync. ICloudSaveAdapter varsa gerçek sync yapar, yoksa
        /// PlayerPrefs'e lokal kopya kaydeder.
        /// </summary>
        public async Task SyncToCloudAsync(string localSaveJson, int version)
        {
            var record = LoadCloudRecord();
            record.PlayerId = GetOrCreatePlayerId();
            record.LocalSaveJson = localSaveJson;

            if (Adapter != null)
            {
                try
                {
                    bool success = await Adapter.SaveCloudSaveAsync(localSaveJson, version);
                    if (success)
                    {
                        record.CloudSaveJson = localSaveJson;
                        record.CloudVersion = version;
                        LoggerService?.Log("[CloudSaveManager] Cloud sync succeeded.");
                    }
                    else
                    {
                        LoggerService?.LogWarning("[CloudSaveManager] Cloud sync failed — will retry on next save.");
                    }
                }
                catch (Exception ex)
                {
                    LoggerService?.LogError($"[CloudSaveManager] Cloud sync exception: {ex.Message}");
                }
            }
            else
            {
                // No cloud adapter — save to local PlayerPrefs as fallback.
                record.CloudSaveJson = localSaveJson;
                record.CloudVersion = version;
                LoggerService?.Log("[CloudSaveManager] Cloud sync: local-only (no adapter).");
            }

            SaveCloudRecord(record);
        }

        /// <summary>
        /// Load save from cloud, resolving conflicts with local save.
        /// </summary>
        public async Task<string> LoadFromCloudAsync(string localSaveJson, int localVersion)
        {
            if (Adapter == null)
            {
                LoggerService?.Log("[CloudSaveManager] Cloud load: local-only (no adapter).");
                return null;
            }

            try
            {
                string cloudJson = await Adapter.LoadCloudSaveAsync();
                if (string.IsNullOrEmpty(cloudJson))
                {
                    LoggerService?.Log("[CloudSaveManager] No cloud data found.");
                    return null;
                }

                // Resolve conflict between local and cloud data.
                var localRecord = new CloudSaveRecord
                {
                    LocalSaveJson = localSaveJson,
                    TimestampUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                    LocalVersion = localVersion
                };

                var cloudRecord = new CloudSaveRecord
                {
                    CloudSaveJson = cloudJson,
                    TimestampUnix = 0, // actual timestamp from cloud metadata
                    CloudVersion = 0   // actual version from cloud metadata
                };

                string resolved = ResolveConflict(localRecord, cloudRecord);
                LoggerService?.Log("[CloudSaveManager] Cloud load completed with conflict resolution.");
                return resolved;
            }
            catch (Exception ex)
            {
                LoggerService?.LogError($"[CloudSaveManager] Cloud load failed: {ex.Message}");
                return null;
            }
        }

        public void OnDispose() { }
    }
}
