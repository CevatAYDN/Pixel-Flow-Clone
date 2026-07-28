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
        public string KeyTheme = "NT_Theme";
        public string KeyColorBlind = "NT_ColorBlind";
        public string KeyVehicleStyle = "NT_VehicleStyle";
        public string KeyMasterVol = "NT_MasterVol";
        public string KeySfxVol = "NT_SfxVol";
        public string KeyMusicVol = "NT_MusicVol";
        public string KeyHaptics = "NT_Haptics";
        public string KeySelectedLanguage = "NT_SelectedLanguage";

        [Header("Progression")]
        public string KeyUnlockedLevels = "NT_UnlockedLevels";

        [Header("Hint / Game Session")]
        public string KeyHintCount = "NT_HintCount";

        [Header("Sound (model-level debug toggles)")]
        public string KeySoundMuted = "NT_SoundMuted";

        [Header("Tutorial")]
        public string KeyTutorialStep = "NT_TutorialStep";

        [Header("Cloud Save")]
        public string KeyCloudPlayerId = "NT_CloudPlayerId";
        public string KeyCloudRecord = "NT_CloudRecord";

        [Header("LiveOps — Daily Login Streak")]
        public string KeyDailyLogin_LastLogin = "NT_DailyLogin_LastLogin";
        public string KeyDailyLogin_Streak = "NT_DailyLogin_Streak";
        public string KeyDailyLogin_VipSkinGranted = "NT_DailyLogin_VipSkinGranted";
        public string DailyLoginVipSkinId = "skin_goldenbus";

        [Header("LiveOps — Rush Hour Event")]
        public string KeyRushHour_Active = "NT_RushHour_Active";
        public string KeyRushHour_EndTime = "NT_RushHour_EndTime";
        public string KeyRushHour_Cooldown = "NT_RushHour_Cooldown";

        [Header("Economy Currency Identifiers")]
        public string CurrencyIdCoin = "coin";
        public string CurrencyIdGem = "gem";
        public string CurrencyIdTicket = "ticket";

        [Header("LiveOps — Daily Crisis")]
        public string KeyCrisisStreak = "NT_CrisisStreak";
        public string KeyCrisisBadges = "NT_CrisisBadges";
        public string KeyCrisisSeed = "NT_CrisisLastSeed";
        public string KeyCrisisFlags = "NT_CrisisFlags";

        [Header("Grid Save")]
        public string KeyPuzzleSavePrefix = "NT_PuzzleSave_";

        [Header("Editor / Dev Utilities (PixelFlowSetupWindow)")]
        public string EditorKeyUnlockedLevelsAllOverride = "NT_UnlockedLevelsOverride";
    }
}
