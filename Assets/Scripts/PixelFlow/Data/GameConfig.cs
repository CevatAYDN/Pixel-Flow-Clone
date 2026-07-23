using UnityEngine;
using Nexus.Core;

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
    }
}
