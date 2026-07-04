using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using PixelFlow.Services;
using Nexus.Core;

namespace PixelFlow.Models
{
    public enum UpgradeType { Storage, Rate, Viaduct, Offline, District }

    public interface ICityEconomyModel
    {
        int Coins { get; }
        int CityLevel { get; }
        int CompletedLevelsCount { get; }

        int StorageUpgradeLevel { get; }
        int RateUpgradeLevel { get; }
        int ViaductUpgradeLevel { get; }
        int OfflineUpgradeLevel { get; }
        int DistrictUnlockLevel { get; }

        int MaxStorage { get; }
        float TaxRatePerSecond { get; }
        float MaxOfflineSeconds { get; }
        int ViaductBonus { get; }

        event Action<int> OnCoinsChanged;
        event Action OnEconomyUpdated;

        void AddCoins(int amount);
        bool TrySpendCoins(int amount);
        void CollectTaxes();
        void PurchaseUpgrade(UpgradeType type);
        int GetUpgradeCost(UpgradeType type);
        float GetAccumulatedTaxes();
        void SetAccumulatedTaxes(float value);
    }

    public class CityEconomyModel : ICityEconomyModel, IReactiveModel
    {
        [Inject] public IPlayerPrefsService PlayerPrefsService { get; set; }
        [Inject] public ILevelProgressionService ProgressionService { get; set; }

        public int Coins { get; private set; }
        public int CityLevel => 1 + StorageUpgradeLevel + RateUpgradeLevel + ViaductUpgradeLevel + OfflineUpgradeLevel + DistrictUnlockLevel;
        public int CompletedLevelsCount => PlayerPrefsService != null ? PlayerPrefsService.GetInt("PF_CompletedLevelsCount", 0) : 0;

        public int StorageUpgradeLevel { get; private set; }
        public int RateUpgradeLevel { get; private set; }
        public int ViaductUpgradeLevel { get; private set; }
        public int OfflineUpgradeLevel { get; private set; }
        public int DistrictUnlockLevel { get; private set; }

        private DateTime _lastCollectionTime;
        private float _accumulatedTaxes = 0f;

        public event Action<int> OnCoinsChanged;
        public event Action OnEconomyUpdated;

        private const string PrefKeyCoins = "NT_Coins";
        private const string PrefKeyStorageLvl = "NT_StorageLvl";
        private const string PrefKeyRateLvl = "NT_RateLvl";
        private const string PrefKeyViaductLvl = "NT_ViaductLvl";
        private const string PrefKeyOfflineLvl = "NT_OfflineLvl";
        private const string PrefKeyDistrictLvl = "NT_DistrictLvl";
        private const string PrefKeyLastCollect = "NT_LastCollect";
        private const string PrefKeyAccumulated = "NT_Accumulated";

        public int MaxStorage
        {
            get
            {
                // Level 0: 1K, Level 10: 500K, logarithmic scale
                int[] capacityTable = { 1000, 2500, 5000, 10000, 25000, 50000, 100000, 200000, 500000, 1000000, 5000000 };
                int idx = Mathf.Clamp(StorageUpgradeLevel, 0, capacityTable.Length - 1);
                return capacityTable[idx];
            }
        }

        public bool IsOverclockActive => OverclockRemainingSeconds > 0f;
        public float OverclockRemainingSeconds { get; private set; } = 0f;

        public float TaxRatePerSecond
        {
            get
            {
                // Tax/second = Base_Rate * (1 + Infrastructure_Bonus) * City_Level_Multiplier * (IsOverclockActive ? 2.0f : 1.0f)
                float baseRate = 10f;
                // Rate upgrade adds +5 base rate per level
                baseRate += RateUpgradeLevel * 5f;

                float infraBonus = CompletedLevelsCount * 0.15f;
                float cityMultiplier = 1f + (CityLevel * 0.1f);
                float overclockMultiplier = IsOverclockActive ? 2.0f : 1.0f;

                return baseRate * (1f + infraBonus) * cityMultiplier * overclockMultiplier;
            }
        }

        public void TriggerOverclock(float durationSeconds = 14400f)
        {
            OverclockRemainingSeconds = Mathf.Max(OverclockRemainingSeconds, durationSeconds);
            OnEconomyUpdated?.Invoke();
        }

        public void TickOverclock(float deltaTime)
        {
            if (OverclockRemainingSeconds > 0f)
            {
                OverclockRemainingSeconds = Mathf.Max(0f, OverclockRemainingSeconds - deltaTime);
            }
        }

        public float MaxOfflineSeconds
        {
            get
            {
                // Default 8h, up to 24h
                // 8 hours = 28800 seconds
                // Each level adds 4 hours (14400 seconds)
                return 28800f + (OfflineUpgradeLevel * 14400f);
            }
        }

        public int ViaductBonus
        {
            get
            {
                // Upgrades can add +1 viaduct limit
                return ViaductUpgradeLevel;
            }
        }

        public ValueTask OnBind(CancellationToken ct)
        {
            LoadState();
            CalculateOfflineEarnings();
            return default;
        }

        [Inject] public IGameStateModel GameStateModel { get; set; }

        /// <summary>
        /// Birikmiş vergi miktarını döndürür (TaxCollectionService tarafından kullanılır).
        /// </summary>
        public float GetAccumulatedTaxes() => _accumulatedTaxes;

        /// <summary>
        /// Birikmiş vergi miktarını ayarlar (TaxCollectionService tarafından kullanılır).
        /// MaxStorage limitini aşamaz.
        /// </summary>
        public void SetAccumulatedTaxes(float value)
        {
            _accumulatedTaxes = Mathf.Min(value, MaxStorage);
            SaveAccumulated();
            OnEconomyUpdated?.Invoke();
        }

        private void LoadState()
        {
            if (PlayerPrefsService == null) return;
            Coins = PlayerPrefsService.GetInt(PrefKeyCoins, 0);
            StorageUpgradeLevel = PlayerPrefsService.GetInt(PrefKeyStorageLvl, 0);
            RateUpgradeLevel = PlayerPrefsService.GetInt(PrefKeyRateLvl, 0);
            ViaductUpgradeLevel = PlayerPrefsService.GetInt(PrefKeyViaductLvl, 0);
            OfflineUpgradeLevel = PlayerPrefsService.GetInt(PrefKeyOfflineLvl, 0);
            DistrictUnlockLevel = PlayerPrefsService.GetInt(PrefKeyDistrictLvl, 0);
            _accumulatedTaxes = PlayerPrefsService.GetInt(PrefKeyAccumulated, 0) / 100f;

            int lastCollectUnix = PlayerPrefsService.GetInt(PrefKeyLastCollect, 0);
            _lastCollectionTime = GetDateTime(lastCollectUnix);
        }

        private void SaveState()
        {
            if (PlayerPrefsService == null) return;
            PlayerPrefsService.SetInt(PrefKeyCoins, Coins);
            PlayerPrefsService.SetInt(PrefKeyStorageLvl, StorageUpgradeLevel);
            PlayerPrefsService.SetInt(PrefKeyRateLvl, RateUpgradeLevel);
            PlayerPrefsService.SetInt(PrefKeyViaductLvl, ViaductUpgradeLevel);
            PlayerPrefsService.SetInt(PrefKeyOfflineLvl, OfflineUpgradeLevel);
            PlayerPrefsService.SetInt(PrefKeyDistrictLvl, DistrictUnlockLevel);
            PlayerPrefsService.SetInt(PrefKeyLastCollect, GetUnixTime(_lastCollectionTime));
            PlayerPrefsService.SetInt(PrefKeyAccumulated, Mathf.RoundToInt(_accumulatedTaxes * 100f));
            PlayerPrefsService.Save();
        }

        private void SaveAccumulated()
        {
            if (PlayerPrefsService == null) return;
            PlayerPrefsService.SetInt(PrefKeyAccumulated, Mathf.RoundToInt(_accumulatedTaxes * 100f));
        }

        private int GetUnixTime(DateTime dateTime)
        {
            return (int)(dateTime - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;
        }

        private DateTime GetDateTime(int unixTime)
        {
            if (unixTime <= 0) return DateTime.UtcNow;
            return new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(unixTime);
        }

        private void CalculateOfflineEarnings()
        {
            DateTime now = DateTime.UtcNow;
            double elapsedSeconds = (now - _lastCollectionTime).TotalSeconds;
            if (elapsedSeconds > 10) // Only count if away for more than 10 seconds
            {
                double offlineTime = Math.Min(elapsedSeconds, MaxOfflineSeconds);
                // Offline efficiency is 50%
                float offlineRate = TaxRatePerSecond * 0.5f;
                float earned = (float)(offlineTime * offlineRate);
                _accumulatedTaxes = Mathf.Min(_accumulatedTaxes + earned, MaxStorage);
            }
            _lastCollectionTime = now;
            SaveState();
        }

        public void AddCoins(int amount)
        {
            if (amount <= 0) return;
            Coins += amount;
            OnCoinsChanged?.Invoke(Coins);
            SaveState();
            OnEconomyUpdated?.Invoke();
        }

        public bool TrySpendCoins(int amount)
        {
            if (amount <= 0 || Coins < amount) return false;
            Coins -= amount;
            OnCoinsChanged?.Invoke(Coins);
            SaveState();
            OnEconomyUpdated?.Invoke();
            return true;
        }

        public void CollectTaxes()
        {
            int collected = Mathf.FloorToInt(_accumulatedTaxes);
            if (collected > 0)
            {
                AddCoins(collected);
                _accumulatedTaxes -= collected;
                _lastCollectionTime = DateTime.UtcNow;
                SaveState();
            }
        }

        public int GetUpgradeCost(UpgradeType type)
        {
            int currentLevel = 0;
            switch (type)
            {
                case UpgradeType.Storage: currentLevel = StorageUpgradeLevel; break;
                case UpgradeType.Rate: currentLevel = RateUpgradeLevel; break;
                case UpgradeType.Viaduct: currentLevel = ViaductUpgradeLevel; break;
                case UpgradeType.Offline: currentLevel = OfflineUpgradeLevel; break;
                case UpgradeType.District: currentLevel = DistrictUnlockLevel; break;
            }

            // Logarithmic upgrade cost: Cost = BaseCost * (CategoryMultiplier ^ Level)
            float baseCost = 200f;
            float categoryMultiplier = 1.35f;
            switch (type)
            {
                case UpgradeType.Storage: baseCost = 150f; categoryMultiplier = 1.5f; break;
                case UpgradeType.Rate: baseCost = 250f; categoryMultiplier = 1.4f; break;
                case UpgradeType.Viaduct: baseCost = 500f; categoryMultiplier = 1.6f; break;
                case UpgradeType.Offline: baseCost = 200f; categoryMultiplier = 2.0f; break;
                case UpgradeType.District: baseCost = 1000f; break; // Fixed increments
            }

            if (type == UpgradeType.District)
            {
                // District costs are fixed higher costs per level
                int[] districtCosts = { 1000, 5000, 15000, 50000, 150000, 500000 };
                int idx = Mathf.Clamp(currentLevel, 0, districtCosts.Length - 1);
                return districtCosts[idx];
            }

            return Mathf.RoundToInt(baseCost * Mathf.Pow(categoryMultiplier, currentLevel));
        }

        public void PurchaseUpgrade(UpgradeType type)
        {
            int cost = GetUpgradeCost(type);
            if (Coins >= cost)
            {
                if (TrySpendCoins(cost))
                {
                    switch (type)
                    {
                        case UpgradeType.Storage: StorageUpgradeLevel++; break;
                        case UpgradeType.Rate: RateUpgradeLevel++; break;
                        case UpgradeType.Viaduct: ViaductUpgradeLevel++; break;
                        case UpgradeType.Offline: OfflineUpgradeLevel++; break;
                        case UpgradeType.District: DistrictUnlockLevel++; break;
                    }
                    SaveState();
                    OnEconomyUpdated?.Invoke();
                }
            }
        }
    }
}
