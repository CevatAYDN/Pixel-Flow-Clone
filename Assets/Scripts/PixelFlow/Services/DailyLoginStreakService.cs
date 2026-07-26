using System;
using System.Threading;
using System.Threading.Tasks;
using Nexus.Core;
using Nexus.Core.Services;
using PixelFlow.Models;
using PixelFlow.Data;
using UnityEngine;

namespace PixelFlow.Services
{
    /// <summary>
    /// Daily Login Streak servisi - game_plan.md §6 ve §15.5'e göre.
    /// 7. günde VIP Skin ödülü verir.
    /// </summary>
    public class DailyLoginStreakService : IDailyLoginStreakService, INexusService
    {
        [Inject] public IInventoryModel InventoryModel { get; set; }
        [Inject] public IEconomyService EconomyService { get; set; }
        [Inject] public ILoggerService LoggerService { get; set; }
        [Inject] public IPlayerPrefsService PlayerPrefsService { get; set; }
        [Inject, OptionalInject] public GameConfig Config { get; set; }
        [Inject, OptionalInject] public StorageKeysConfigAsset Keys { get; set; }
        [Inject, OptionalInject] public ISkinCatalogService SkinCatalog { get; set; }

        private string CoinCurrencyId => Keys?.CurrencyIdCoin;

        private string LastLoginKey => Keys?.KeyDailyLogin_LastLogin;
        private string StreakKey => Keys?.KeyDailyLogin_Streak;
        private string VipSkinGrantedKey => Keys?.KeyDailyLogin_VipSkinGranted;
        private string VipSkinId => Keys?.DailyLoginVipSkinId;

        public ValueTask InitializeAsync(CancellationToken ct)
        {
            return default;
        }

        public void OnDispose() { }

        /// <summary>
        /// Günlük giriş kontrolü ve ödül verme.
        /// </summary>
        public void CheckDailyLogin()
        {
            if (PlayerPrefsService == null) 
                throw new DataValidationException("PlayerPrefsService is null in DailyLoginStreakService!");

            if (Config == null)
                throw new DataValidationException("GameConfig is null in DailyLoginStreakService!");

            if (string.IsNullOrEmpty(LastLoginKey) || string.IsNullOrEmpty(StreakKey) || string.IsNullOrEmpty(VipSkinGrantedKey) || string.IsNullOrEmpty(VipSkinId)) 
                throw new DataValidationException("DailyLoginStreakService requires configured storage keys.");

            var economyConfig = Resources.Load<EconomyConfigAsset>("Configs/EconomyConfig");
            float rollHours = economyConfig != null ? economyConfig.DailyLoginRollHours : 20f;

            string lastLoginStr = PlayerPrefsService.GetString(LastLoginKey, "");
            DateTime lastLogin;
            bool isNewDay = false;

            if (DateTime.TryParse(lastLoginStr, out lastLogin))
            {
                TimeSpan diff = DateTime.UtcNow - lastLogin;
                if (diff.TotalHours >= rollHours)
                    isNewDay = true;
            }
            else
            {
                isNewDay = true;
            }

            if (!isNewDay)
            {
                LoggerService?.Log("[PixelFlow.DailyLoginStreakService] Already claimed today.");
                return;
            }

            int currentStreak = PlayerPrefsService.GetInt(StreakKey, 0) + 1;
            PlayerPrefsService.SetInt(StreakKey, currentStreak);
            PlayerPrefsService.SetString(LastLoginKey, DateTime.UtcNow.ToString("O"));
            PlayerPrefsService.Save();

            LoggerService?.Log($"[PixelFlow.DailyLoginStreakService] Daily login streak: Day {currentStreak}");

            GrantDailyReward(currentStreak);

            if (currentStreak >= 7 && !PlayerPrefsService.GetBool(VipSkinGrantedKey, false))
            {
                GrantVipSkin();
                PlayerPrefsService.SetBool(VipSkinGrantedKey, true);
                PlayerPrefsService.Save();
            }
        }

        private void GrantDailyReward(int streakDay)
        {
            if (Config == null)
                throw new DataValidationException("GameConfig is null in DailyLoginStreakService!");

            var economyConfig = Resources.Load<EconomyConfigAsset>("Configs/EconomyConfig");
            int bonusPerDay = economyConfig != null ? economyConfig.DailyLoginBonusPerDay : 20;
            int maxBonus = economyConfig != null ? economyConfig.DailyLoginMaxBonus : 500;

            int baseCoins = Config.DailyChestCoins;
            int streakBonus = Mathf.Min(streakDay * bonusPerDay, maxBonus);
            int totalCoins = baseCoins + streakBonus;

            if (string.IsNullOrEmpty(CoinCurrencyId)) throw new DataValidationException("DailyLoginStreakService requires configured currency identifiers.");

            if (EconomyService != null)
            {
                EconomyService.Earn(CoinCurrencyId, totalCoins, $"daily_login:streak_{streakDay}");
            }
            else if (InventoryModel != null)
            {
                InventoryModel.AddCoins(totalCoins);
            }

            LoggerService?.Log($"[PixelFlow.DailyLoginStreakService] Granted {totalCoins} coins (base: {baseCoins}, streak bonus: {streakBonus}) for streak day {streakDay}");
        }

        private void GrantVipSkin()
        {
            string vipSkinId = VipSkinId;
            VehicleSkinConfig vipSkin = SkinCatalog != null
                ? SkinCatalog.GetVehicleSkinById(vipSkinId)
                : null;

            if (vipSkin != null)
            {
                InventoryModel?.UnlockSkin(vipSkin.SkinId);
                LoggerService?.Log($"[PixelFlow.DailyLoginStreakService] 🎉 VIP Skin granted: {vipSkin.DisplayName} ({vipSkinId})");
            }
            else
            {
                LoggerService?.LogWarning($"[PixelFlow.DailyLoginStreakService] VIP Skin config not found: {vipSkinId}");
            }
        }

        public int GetCurrentStreak()
        {
            if (PlayerPrefsService == null)
                throw new DataValidationException("PlayerPrefsService is null in DailyLoginStreakService!");
            return PlayerPrefsService.GetInt(StreakKey, 0);
        }

        public DateTime? GetLastLoginTime()
        {
            if (PlayerPrefsService == null)
                throw new DataValidationException("PlayerPrefsService is null in DailyLoginStreakService!");
            string lastLoginStr = PlayerPrefsService.GetString(LastLoginKey, "");
            if (DateTime.TryParse(lastLoginStr, out DateTime dt))
                return dt;
            return null;
        }

        public void ResetStreak()
        {
            if (PlayerPrefsService != null)
            {
                PlayerPrefsService.SetInt(StreakKey, 0);
                PlayerPrefsService.SetString(LastLoginKey, "");
                PlayerPrefsService.SetBool(VipSkinGrantedKey, false);
                PlayerPrefsService.Save();
            }
        }
    }
}
