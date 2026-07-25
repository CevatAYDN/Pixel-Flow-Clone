using UnityEngine;

namespace PixelFlow.Data
{
    /// <summary>
    /// game_plan.md §2.2: Zero-Hardcode Policy — Tüm PlayerPrefs / EncryptedStorage anahtarları
    /// bu ScriptableObject'te tutulur. Kod içinde `const string` ile saklama adresi tanımlanmaz;
    /// IPlayerPrefsService üzerinden erişen tüm modeller GameContextLifecycle'tan inject edilen
    /// bu asset'ten anahtarı okur.
    /// </summary>
    [CreateAssetMenu(fileName = "StorageKeysConfig", menuName = "PixelFlow/Storage/StorageKeysConfig")]
    public class StorageKeysConfigAsset : ScriptableObject
    {
        [Header("Settings")]
        public string KeyTheme;
        public string KeyColorBlind;
        public string KeyVehicleStyle;
        public string KeyMasterVol;
        public string KeySfxVol;
        public string KeyMusicVol;
        public string KeyHaptics;

        [Header("Progression")]
        public string KeyUnlockedLevels;

        [Header("Hint / Game Session")]
        public string KeyHintCount;

        [Header("Sound (model-level debug toggles)")]
        public string KeySoundMuted;

        [Header("Tutorial")]
        public string KeyTutorialStep;

        [Header("Cloud Save")]
        public string KeyCloudPlayerId;
        public string KeyCloudRecord;

        [Header("LiveOps — Daily Login Streak")]
        public string KeyDailyLogin_LastLogin;
        public string KeyDailyLogin_Streak;
        public string KeyDailyLogin_VipSkinGranted;
        public string DailyLoginVipSkinId;

        [Header("LiveOps — Rush Hour Event")]
        public string KeyRushHour_Active;
        public string KeyRushHour_EndTime;
        public string KeyRushHour_Cooldown;

        [Header("Economy Currency Identifiers")]
        public string CurrencyIdCoin;
        public string CurrencyIdGem;
        public string CurrencyIdTicket;

        [Header("Editor / Dev Utilities (PixelFlowSetupWindow)")]
        public string EditorKeyUnlockedLevelsAllOverride;
    }
}
