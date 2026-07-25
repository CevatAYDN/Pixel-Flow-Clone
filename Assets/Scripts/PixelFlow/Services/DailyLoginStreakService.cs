using System;
using System.Threading;
using System.Threading.Tasks;
using Nexus.Core;
using Nexus.Core.Services;
using PixelFlow.Models;
using PixelFlow.Signals;
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

        private const string LastLoginKey = "NT_DailyLogin_LastLogin";
        private const string StreakKey = "NT_DailyLogin_Streak";
        private const string VipSkinGrantedKey = "NT_DailyLogin_VipSkinGranted";

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
            if (PlayerPrefsService == null) return;

            string lastLoginStr = PlayerPrefsService.GetString(LastLoginKey, "");
            DateTime lastLogin;
            bool isNewDay = false;

            if (DateTime.TryParse(lastLoginStr, out lastLogin))
            {
                TimeSpan diff = DateTime.UtcNow - lastLogin;
                if (diff.TotalHours >= 20) // 20+ saat geçmişse yeni gün say
                    isNewDay = true;
            }
            else
            {
                isNewDay = true; // İlk giriş
            }

            if (!isNewDay)
            {
                LoggerService?.Log("[PixelFlow.DailyLoginStreakService] Already claimed today.");
                return;
            }

            // Streak artır
            int currentStreak = PlayerPrefsService.GetInt(StreakKey, 0) + 1;
            PlayerPrefsService.SetInt(StreakKey, currentStreak);
            PlayerPrefsService.SetString(LastLoginKey, DateTime.UtcNow.ToString("O"));
            PlayerPrefsService.Save();

            LoggerService?.Log($"[PixelFlow.DailyLoginStreakService] Daily login streak: Day {currentStreak}");

            // Ödüller
            GrantDailyReward(currentStreak);

            // 7. günde VIP Skin
            if (currentStreak >= 7 && !PlayerPrefsService.GetBool(VipSkinGrantedKey, false))
            {
                GrantVipSkin();
                PlayerPrefsService.SetBool(VipSkinGrantedKey, true);
                PlayerPrefsService.Save();
            }
        }

        private void GrantDailyReward(int streakDay)
        {
            int baseCoins = Config?.DailyChestCoins ?? 100;
            int streakBonus = Mathf.Min(streakDay * 20, 500); // Max 500 bonus
            int totalCoins = baseCoins + streakBonus;

            if (EconomyService != null)
            {
                EconomyService.Earn("coin", totalCoins, $"daily_login:streak_{streakDay}");
            }
            else if (InventoryModel != null)
            {
                InventoryModel.AddCoins(totalCoins);
            }

            LoggerService?.Log($"[PixelFlow.DailyLoginStreakService] Granted {totalCoins} coins (base: {baseCoins}, streak bonus: {streakBonus}) for streak day {streakDay}");
        }

        private void GrantVipSkin()
        {
            const string vipSkinId = "skin_vip_golden";
            
            // VIP Skin'i kilidi aç
            var allSkins = Resources.LoadAll<VehicleSkinConfig>("Configs/Skins");
            var vipSkin = Array.Find(allSkins, s => s.SkinId == vipSkinId);
            
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
            if (PlayerPrefsService == null) return 0;
            return PlayerPrefsService.GetInt(StreakKey, 0);
        }

        public DateTime? GetLastLoginTime()
        {
            if (PlayerPrefsService == null) return null;
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