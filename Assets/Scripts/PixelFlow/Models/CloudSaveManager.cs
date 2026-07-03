using System;
using UnityEngine;
using PixelFlow.Services;

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
    /// GDD §10.3: Cloud save simülasyonu. Gerçek Firebase/Firestore entegrasyonu
    /// olmadan, local save ile "cloud save" arasında conflict-resolve mantığı
    /// hazırlar. Üretimde bu sınıf bir adapter (FirestoreClient) ile değiştirilir.
    /// </summary>
    public static class CloudSaveManager
    {
        private const string CloudPlayerIdKey = "PF_CloudPlayerId";
        private const string CloudRecordKey = "PF_CloudRecord";

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
        /// Save sonrası cloud sync simülasyonu. Gerçek ortamda bu metot
        /// FirestoreAdapter.UploadAsync(record) çağırır. Burada sadece local
        /// cache'e yazıp timestamp günceller.
        /// </summary>
        public static void SyncToCloud(IPlayerPrefsService prefs, string localSaveJson, int version)
        {
            var record = LoadCloudRecord(prefs);
            record.PlayerId = GetOrCreatePlayerId(prefs);
            record.LocalSaveJson = localSaveJson;
            record.CloudSaveJson = localSaveJson;
            record.LocalVersion = version;
            record.CloudVersion = version;
            SaveCloudRecord(prefs, record);
        }
    }
}
