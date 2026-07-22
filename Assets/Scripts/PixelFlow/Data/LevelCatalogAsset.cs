using System.Collections.Generic;
using PixelFlow.Services;
using UnityEngine;

namespace PixelFlow.Data
{
    /// <summary>
    /// Merkezi level kataloğu ScriptableObject'i.
    /// Hangi level'ın hangi kaynaktan yükleneceğini tanımlar:
    /// - Doğrudan LevelData referansı (authored/handcrafted levels)
    /// - Procedural generation (DifficultyParams ile)
    /// - Katalogda bulunamazsa eski Resources.Load fallback
    /// 
    /// GameContextLifecycle içinde Resources'tan yüklenir
    /// ve ILevelProgressionService'e enjekte edilir.
    /// </summary>
    [CreateAssetMenu(
        fileName = "LevelCatalog",
        menuName = "PixelFlow/Level Catalog")]
    public class LevelCatalogAsset : ScriptableObject
    {
        [System.Serializable]
        public class LevelCatalogEntry
        {
            [Tooltip("Level index (0-based)")]
            public int LevelIndex;

            [Tooltip("Doğrudan LevelData referansı (handcrafted level)")]
            public LevelData AuthoredLevel;

            [Tooltip("True = procedural üretim, False = authored level kullan")]
            public bool UseProceduralFallback;

            [Tooltip("Procedural üretim parametreleri (UseProceduralFallback=true ise)")]
            public DifficultyParams ProceduralDifficulty;
        }

        [Header("=== Level Kataloğu ===")]
        [Tooltip("Katalogdaki level tanımları. Sıralama önemli değil — LevelIndex'e göre lookup yapılır.")]
        public List<LevelCatalogEntry> Levels = new List<LevelCatalogEntry>();

        // Runtime lookup cache
        private Dictionary<int, LevelCatalogEntry> _lookup;

        private void BuildLookup()
        {
            if (_lookup != null) return;
            _lookup = new Dictionary<int, LevelCatalogEntry>();
            if (Levels == null) return;
            foreach (var entry in Levels)
            {
                if (entry != null && !_lookup.ContainsKey(entry.LevelIndex))
                {
                    _lookup[entry.LevelIndex] = entry;
                }
            }
        }

        /// <summary>
        /// Level index'e göre katalog girişini bulur.
        /// </summary>
        public bool TryGetEntry(int levelIndex, out LevelCatalogEntry entry)
        {
            BuildLookup();
            return _lookup.TryGetValue(levelIndex, out entry);
        }

        /// <summary>
        /// Level index'e göre authored LevelData'yı döndürür.
        /// Katalogda yoksa veya procedural ise null döner.
        /// </summary>
        public LevelData GetAuthoredLevel(int levelIndex)
        {
            if (TryGetEntry(levelIndex, out var entry))
            {
                if (!entry.UseProceduralFallback && entry.AuthoredLevel != null)
                    return entry.AuthoredLevel;
            }
            return null;
        }

        /// <summary>
        /// Level index için procedural generation gerekip gerekmediğini döndürür.
        /// </summary>
        public bool TryGetProceduralParams(int levelIndex, out DifficultyParams param)
        {
            param = default;
            if (TryGetEntry(levelIndex, out var entry))
            {
                if (entry.UseProceduralFallback)
                {
                    param = entry.ProceduralDifficulty;
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Katalogda kaç authored level olduğunu döndürür.
        /// </summary>
        public int AuthoredLevelCount
        {
            get
            {
                BuildLookup();
                int count = 0;
                foreach (var entry in _lookup.Values)
                {
                    if (!entry.UseProceduralFallback && entry.AuthoredLevel != null)
                        count++;
                }
                return count;
            }
        }
    }
}
