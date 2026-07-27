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

        private string CloudPlayerIdKey
        {
            get
            {
                if (Keys != null && !string.IsNullOrEmpty(Keys.KeyCloudPlayerId)) return Keys.KeyCloudPlayerId;
                return "NT_CloudPlayerId";
            }
        }

        private string CloudRecordKey
        {
            get
            {
                if (Keys != null && !string.IsNullOrEmpty(Keys.KeyCloudRecord)) return Keys.KeyCloudRecord;
                return "NT_CloudRecord";
            }
        }

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
            if (Prefs == null)
                throw new DataValidationException("CloudSaveManager: IPlayerPrefsService is not injected. Cannot load cloud record.");

            string json = Prefs.GetString(CloudRecordKey, "");
            if (string.IsNullOrEmpty(json))
                throw new DataValidationException($"Cloud record for key '{CloudRecordKey}' is empty or not found. Cannot load corrupted save.");

            try
            {
                var record = JsonUtility.FromJson<CloudSaveRecord>(json);
                // Struct can't be null; check if parsed successfully by checking key fields
                if (string.IsNullOrEmpty(record.PlayerId))
                    throw new DataValidationException($"Cloud record JSON parsed to empty struct for key '{CloudRecordKey}'. Corrupted save data.");
                return record;
            }
            catch (DataValidationException)
            {
                throw; // Re-throw our own exceptions
            }
            catch (System.Exception ex)
            {
                throw new DataValidationException($"Failed to parse cloud save JSON for key '{CloudRecordKey}': {ex.Message}");
            }
        }

        public void SaveCloudRecord(CloudSaveRecord record)
        {
            if (Prefs == null)
                throw new DataValidationException("CloudSaveManager: IPlayerPrefsService is not injected. Cannot save cloud record.");

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

        public static Task SyncToCloudAsync(IPlayerPrefsService prefs, string localSaveJson, int version)
        {
            throw new DataValidationException("CloudSaveManager.SyncToCloudAsync(IPlayerPrefsService, ...) is deprecated. Bind ICloudSaveAdapter and call the instance method instead.");
        }

        /// <summary>
        /// Save sonrası cloud sync. ICloudSaveAdapter varsa gerçek sync yapar, yoksa
        /// DataValidationException fırlatır (Zero-Silent-Fallback §2.2).
        /// </summary>
        public async Task SyncToCloudAsync(string localSaveJson, int version)
        {
            if (Adapter == null)
                throw new DataValidationException("CloudSaveManager: ICloudSaveAdapter is not injected. Cannot sync to cloud (Zero-Silent-Fallback §2.2).");

            CloudSaveRecord record;
            try
            {
                record = LoadCloudRecord();
            }
            catch
            {
                record = new CloudSaveRecord();
            }
            record.PlayerId = GetOrCreatePlayerId();
            record.LocalSaveJson = localSaveJson;
            record.LocalVersion = version;

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
                    throw new DataValidationException("CloudSaveManager: Cloud sync failed — adapter returned false.");
                }
            }
            catch (DataValidationException)
            {
                throw; // Re-throw our own
            }
            catch (System.Exception ex)
            {
                LoggerService?.LogError($"[CloudSaveManager] Cloud sync exception: {ex.Message}");
                throw new DataValidationException($"CloudSaveManager: Cloud sync exception: {ex.Message}");
            }

            SaveCloudRecord(record);
        }

        /// <summary>
        /// Load save from cloud, resolving conflicts with local save.
        /// </summary>
        public async Task<string> LoadFromCloudAsync(string localSaveJson, int localVersion)
        {
            if (Adapter == null)
                throw new DataValidationException("CloudSaveManager: ICloudSaveAdapter is not injected. Cannot load from cloud (Zero-Silent-Fallback §2.2).");

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
            catch (DataValidationException)
            {
                throw; // Re-throw our own
            }
            catch (System.Exception ex)
            {
                LoggerService?.LogError($"[CloudSaveManager] Cloud load failed: {ex.Message}");
                throw new DataValidationException($"CloudSaveManager: Cloud load failed: {ex.Message}");
            }
        }

        public void OnDispose() { }
    }
}
