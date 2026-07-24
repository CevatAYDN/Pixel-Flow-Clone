using UnityEngine;
using Nexus.Core;
using PixelFlow.Models;
using PixelFlow.Services;

namespace PixelFlow.Data
{
    /// <summary>
    /// Merkezi oyun konfigürasyonu. Tüm sihirli sayılar (magic numbers)
    /// bu ScriptableObject'te toplanır. Resources/GameConfig.asset olarak
    /// saklanır, runtime'da GameContext üzerinden erişilir.
    ///
    /// Kullanım: [Inject] public GameConfig Config { get; set; }
    /// </summary>
    [CreateAssetMenu(
        fileName = "GameConfig",
        menuName = "PixelFlow/Game Configuration")]
    public class GameConfig : ScriptableObject
    {
        [Header("=== Vehicle Simulation ===")]
        [Tooltip("Araç hızı (grid birimi/saniye)")]
        public float VehicleSpeed = 3f;

        [Tooltip("Araç spawn aralığı (saniye)")]
        public float SpawnInterval = 1.2f;

        [Tooltip("Frame başına maksimum ilerleme miktarı")]
        public float MaxProgressPerFrame = 0.25f;

        [Tooltip("Simülasyon güvenlik zaman aşımı (saniye) — darboğazda kilitlenmeyi önler")]
        public float MaxSimulationSafetyDuration = 45f;

        [Tooltip("Simülasyon hız çarpanı (1x = normal, 2x = iki kat hızlı)")]
        [SerializeField, Range(0.5f, 5f)] private float _simulationSpeedMultiplier = 1f;
        public float SimulationSpeedMultiplier => _simulationSpeedMultiplier;

        [Header("=== Obstacles ===")]
        [Tooltip("Ferry engelinin yön değiştirme periyodu (saniye)")]
        public float FerryPeriod = 10f;

        [Header("=== Undo/Redo ===")]
        [Tooltip("Undo yığını maksimum derinliği")]
        public int HistoryMaxDepth = 200;

        [Header("=== Camera ===")]
        [Tooltip("Hub (şehir) görünümü orthographic boyutu")]
        public float HubCameraSize = 7f;

        [Tooltip("Minimum zoom seviyesi (orthographic size)")]
        public float MinZoom = 8f;

        [Tooltip("Maximum zoom seviyesi (orthographic size)")]
        public float MaxZoom = 12f;

        [Header("=== Hints ===")]
        [Tooltip("Level başına varsayılan ipucu sayısı")]
        public int DefaultHintCount = 3;

        [Header("=== Crisis / Ad Service (game_plan.md §2.1.B2) ===")]
        [Tooltip("Interstitial reklam öncesi maksimum kriz deneme sayısı")]
        public int MaxRetriesBeforeInterstitial = 3;

        [Tooltip("Interstitial reklamın gösterileceği minimum level (1-indexed)")]
        public int MinLevelForInterstitial = 5;

        [Tooltip("Kaç seviyede bir Interstitial reklam gösterileceği (baraj)")]
        public int InterstitialLevelInterval = 3;

        [Tooltip("Rewarded Ad izlenince kazanılan altın miktarı")]
        public int RewardedAdCoinReward = 100;

        [Tooltip("Rewarded Ad izlenince kazanılan ipucu sayısı")]
        public int RewardedAdHintReward = 2;

        [Tooltip("Seviye sonu 2x Para çarpanı")]
        public float DoubleCoinMultiplier = 2.0f;

        [Tooltip("Interstitial Reklam Placement ID")]
        public string InterstitialPlacementId = "interstitial_level_end";

        [Tooltip("Rewarded Reklam Placement ID")]
        public string RewardedPlacementId = "rewarded_double_coins";

        [Tooltip("Banner Reklam Placement ID")]
        public string BannerPlacementId = "banner_bottom";

        [Header("=== Gameplay Timer ===")]
        [Tooltip("Boşta kalma hatırlatma süresi (saniye)")]
        public float IdleReminderSeconds = 300f;

        [Tooltip("Maksimum grace skip sayısı")]
        public int MaxGraceSkips = 3;

        [Header("=== Path Solver ===")]
        [Tooltip("Recursive solver maksimum iterasyon sayısı")]
        public int PathSolverMaxIterations = 200000;

        [Header("=== Procedural Audio ===")]
        [Tooltip("Prosedürel ses örnekleme hızı")]
        public int AudioSampleRate = 44100;

        [Header("=== Progress ===")]
        [Tooltip("İlk açılışta unlock edilen seviye sayısı")]
        public int DefaultUnlockedLevels = 1;

        [Header("=== Power-Ups (IPowerUpService) ===")]
        [Tooltip("Rainbow Road her aktivasyonda verilen segment sayısı")]
        public int RainbowRoadSegmentsPerActivation = 3;

        [Tooltip("Her level'da verilen Clear Jam kullanım hakkı")]
        public int ClearJamUsesPerLevel = 1;

        [Header("=== Save System ===")]
        [Tooltip("Kayıt formatı versiyonu — yapı değiştiğinde artırılır")]
        public int SaveFormatVersion = 2;

        [Tooltip("Kayıt PlayerPrefs anahtarı")]
        public string SaveVersionKey = "PF_SaveFormat_Version";

        [Header("=== Economy / Coins ===")]
        [Tooltip("Simülasyonda hedefe ulaşan her araç başına kazanılan coin")]
        public int CoinPerFlowScore = 5;

        [Tooltip("Seviye tamamlama bonus coin")]
        public int LevelCompleteCoinBonus = 50;

        [Header("=== Vehicle Simulation (Advanced) ===")]
        [Tooltip("Sabit zaman adımı (saniye) — fizik tutarlılığı için")]
        public float FixedTimeStep = 1f / 60f;

        [Tooltip("Spawn kontrolü frame atlama aralığı (performans optimizasyonu)")]
        public int SpawnCheckInterval = 10;

        [Tooltip("Araç hız rastgele varyasyon aralığı (+/-)")]
        public float SpeedVariationRange = 0.3f;

        [Tooltip("Çarpışma tespit mesafesi (grid birimi)")]
        public float CollisionDistance = 0.45f;

        [Tooltip("Viyadük Z-fark eşiği (çarpışma yok sayma)")]
        public float ViaductZDiffThreshold = 0.15f;

        [Tooltip("Viyadük üst yol Z offset")]
        public float ViaductOverZOffset = -0.4f;

        [Tooltip("Viyadük alt yol Z offset")]
        public float ViaductUnderZOffset = -0.1f;

        [Tooltip("Normal yol Z offset")]
        public float NormalZOffset = -0.2f;

        [Header("=== Camera (Advanced) ===")]
        [Tooltip("Kamera geçiş süresi (saniye)")]
        public float CameraTransitionDuration = 0.18f;

        [Header("=== Camera (Hub & Transitions) ===")]
        [Tooltip("Hub (şehir) kamera dünya pozisyonu — izometrik görünüm")]
        public Vector3 HubCameraPosition = new Vector3(8f, 12f, -8f);

        [Tooltip("Hub kamera Euler rotasyonu (derece)")]
        public Vector3 HubCameraEuler = new Vector3(45f, 45f, 0f);

        [Tooltip("Hub/Puzzle state geçiş süresi (saniye) — GDD §5.1: 0.8s ease-in-out")]
        public float StateTransitionDuration = 0.8f;

        [Tooltip("Puzzle görünümü güvenlik fallback orthographic boyutu")]
        public float PuzzleFallbackCameraSize = 5f;

        [Tooltip("Kaza anı kamera sarsıntı şiddeti")]
        public float CrashShakeIntensity = 0.35f;

        [Tooltip("Kaza anı kamera sarsıntı süresi (saniye)")]
        public float CrashShakeDuration = 0.45f;

        [Tooltip("Kaza anı kamera odak kaydırma mesafesi (birim)")]
        public float CrashFocusOffset = 0.4f;

        [Header("=== Audio ===")]
        [Tooltip("Ses havuzu önceden tahsis boyutu")]
        public int AudioPoolSize = 3;

        [Header("=== Path Solver (Advanced) ===")]
        [Tooltip("Path solver maksimum iterasyon üst sınırı")]
        public int PathSolverMaxIterationsCap = 1000000;

        [Header("=== Vehicle Visual Pool ===")]
        [Tooltip("Önceden tahsis edilen küp (cube) sayısı")]
        public int VehiclePartPoolCubes = 512;

        [Tooltip("Önceden tahsis edilen silindir (cylinder) sayısı")]
        public int VehiclePartPoolCylinders = 256;

        [Header("=== Visual Feedback ===")]
        [Tooltip("Üçüncü renk reddi nabız frekansı (Hz)")]
        public float RejectionPulseFrequency = 15f;

        [Header("=== Bridge Rules ===")]
        [Tooltip("Köprü başına maksimum yol sayısı")]
        public int MaxPathsPerBridge = 2;

        [Header("=== Settings Defaults ===")]
        [Tooltip("Varsayılan tema")]
        public AppTheme DefaultTheme = AppTheme.Dark;

        [Tooltip("Varsayılan master ses seviyesi")]
        [Range(0f, 1f)] public float DefaultMasterVolume = 1f;

        [Tooltip("Varsayılan SFX ses seviyesi")]
        [Range(0f, 1f)] public float DefaultSfxVolume = 1f;

        [Tooltip("Varsayılan müzik ses seviyesi")]
        [Range(0f, 1f)] public float DefaultMusicVolume = 0.7f;

        [Tooltip("Varsayılan haptik kapalı mı")]
        public bool DefaultHapticsDisabled = false;

        [Header("=== Daily Crisis Difficulty (game_plan.md §3.6) ===")]
        [Tooltip("Kolay günlük kriz (crisisIndex 0) zorluk parametreleri")]
        public DifficultyParams DailyCrisisEasy = new DifficultyParams(10, 10, 3, 2, false, true);

        [Tooltip("Orta günlük kriz (crisisIndex 1) zorluk parametreleri")]
        public DifficultyParams DailyCrisisMedium = new DifficultyParams(10, 10, 4, 3, false, true);

        [Tooltip("Zor günlük kriz (crisisIndex 2+) zorluk parametreleri")]
        public DifficultyParams DailyCrisisHard = new DifficultyParams(10, 10, 4, 4, true, true, true, true);
    }
}
