using UnityEngine;
using Nexus.Core;
using Nexus.Core.Services;
using PixelFlow.Data;
using PixelFlow.Models;
using PixelFlow.Signals;
using PixelFlow.Services;
using System.Collections;
using System.Collections.Generic;

namespace PixelFlow
{
    /// <summary>
    /// GDD §5.1: Boot → Splash → Hub (MainMenu) veya Restore → Playing.
    /// İlk çalıştırmada EnterHubSignal ateşlenir; save varsa restore edilir.
    ///
    /// Tüm DI bağımlılıkları Root başlatıldıktan sonra container'dan tek seferde
    /// çözülür ve önbelleğe alınır. FindAnyObjectByType yerine Root static registry'si kullanılır (§15.9 Kural 8).
    /// initialLevel ve nexusRoot public field'ları Editor uyumluluğu için korunur.
    /// </summary>
    public class GameBootstrapper : MonoBehaviour
    {
        public LevelData initialLevel;
        public Root nexusRoot;

        [Header("Root Search (Boot)")]
        [SerializeField] [Tooltip("Nexus Root aranırken maksimum deneme sayısı")]
        private int _rootSearchRetries = 10;
        [SerializeField] [Tooltip("Root arama denemeleri arasındaki saniye cinsinden bekleme")]
        private float _rootSearchInterval = 0.1f;

        private ILoggerService FallbackLogger => NexusRuntime.Logger;
        private Root _cachedRoot;

        // Cached DI references — resolved once after Root init, reused throughout
        private ISignalBus _signalBus;
        private IGameStateModel _stateModel;
        private IGridModel _gridModel;
        private IGameSessionModel _sessionModel;
        private ILevelModel _levelModel;
        private ILoggerService _loggerService;
        private IPlayerPrefsService _prefs;
        private ILevelProgressionService _progressionService;
        private GridStateSerializer _gridStateSerializer;

        // Save format version — GameConfig ScriptableObject'ten okunur (§2.2 Zero-Silent-Fallback).
        // _gameConfig, ResolveServices'te container.Resolve ile çözülür; null ise orada fail-loud edilir.
        private int SaveFormatVersion => _gameConfig.SaveFormatVersion;
        private string SaveVersionKey => _gameConfig.SaveVersionKey;
        private Data.GameConfig _gameConfig;

        private IEnumerator Start()
        {
            FallbackLogger?.Log("[PixelFlow.GameBootstrapper] Bootstrapper starting up. Waiting for Nexus Root...");
            yield return WaitForRoot();
            if (_cachedRoot == null)
            {
                FallbackLogger?.LogError("[PixelFlow.GameBootstrapper] ERROR: Nexus Root not found after retries. Game cannot start.");
                yield break;
            }
            nexusRoot = _cachedRoot;
            FallbackLogger?.Log("[PixelFlow.GameBootstrapper] Nexus Root reference cached. Waiting for context initialization...");

            while (!nexusRoot.IsInitialized)
                yield return null;

            FallbackLogger?.Log("[PixelFlow.GameBootstrapper] Nexus Root context initialized. Resolving services...");
            // Container'dan tüm bağımlılıkları tek seferde çözümle.
            if (!ResolveServices()) yield break;

            _loggerService?.Log("[PixelFlow.GameBootstrapper] DI Services resolved successfully. Starting lifecycle check...");

#if !UNITY_EDITOR
            var splash = FindAnyObjectByType<Views.SplashView>(FindObjectsInactive.Include);
            if (splash != null && !splash.IsComplete && splash.gameObject.activeInHierarchy)
            {
                _loggerService?.Log("[PixelFlow.GameBootstrapper] Waiting for Splash screen completion...");
                bool splashDone = false;
                splash.OnSplashComplete += () => splashDone = true;
                yield return new WaitUntil(() => splashDone || splash.IsComplete);
                _loggerService?.Log("[PixelFlow.GameBootstrapper] Splash screen complete.");
            }
#endif

            _loggerService?.Log("[PixelFlow.GameBootstrapper] Checking for saved game states...");

            // Ensure save format version is stored so future boots can validate
            _prefs?.SetInt(SaveVersionKey, SaveFormatVersion);
            _prefs?.Save();

            // Her zaman Ana Menü'ye git — save varsa MainMenu'dan "Devam Et" ile restore edilir.
            // Otomatik restore kaldırıldı: oyuncu her açılışta MainMenu'yu görmeli.
            _loggerService?.Log("[PixelFlow.GameBootstrapper] Entering Main Menu / Hub (GameState.MainMenu).");
            EnterMainMenu();
        }

        private void EnterMainMenu()
        {
            _loggerService?.Log("[PixelFlow.GameBootstrapper] GameState changing -> MainMenu");
            _stateModel?.SetState(GameState.MainMenu);
        }

        private bool ResolveServices()
        {
            try
            {
                var container = nexusRoot.Context.Container;
                _loggerService = container.Resolve<ILoggerService>();
                _loggerService?.Log("[PixelFlow] Nexus Root initialized successfully. Resolving services...");

                _signalBus = container.Resolve<ISignalBus>();
                _signalBus?.Subscribe<LoadedInitialLevelSignal>(_ => _loggerService?.Log("[PixelFlow] Initial level loaded signal received."));
                _stateModel = container.Resolve<IGameStateModel>();
                _gridModel = container.Resolve<IGridModel>();
                _sessionModel = container.Resolve<IGameSessionModel>();
                _levelModel = container.Resolve<ILevelModel>();
                _prefs = container.Resolve<IPlayerPrefsService>();
                _progressionService = container.Resolve<ILevelProgressionService>();
                _gameConfig = container.Resolve<Data.GameConfig>();
                // game_plan.md §2.2: config zorunludur. Sessizce hardcode'a düşmek yerine fail-loud.
                if (_gameConfig == null)
                {
                    throw new Data.DataValidationException("GameConfig çözülemedi! Bootstrapper devam edemez.");
                }

                // Trigger lazy init for services that need to be alive at boot
                container.Resolve<IVehicleSimulator>();
                container.Resolve<IObstacleService>();

                _gridStateSerializer = container.Resolve<GridStateSerializer>();

                return true;
            }
            catch (System.Exception ex)
            {
                var logger = _loggerService ?? FallbackLogger;
                logger?.LogError($"[PixelFlow] ERROR: DI resolve failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Attempts to restore a saved game. Returns true if a valid save was restored,
        /// false if no save exists or the save was invalid/cleared.
        /// </summary>
        private bool TryRestoreSavedGame()
        {
            // User requirement: Every time a level starts, start clean from the beginning (do not continue mid-level paths)
            if (_prefs != null)
            {
                _gridStateSerializer?.ClearSave(_prefs);
            }
            return false;
        }

        private void OnApplicationPause(bool pause)
        {
            if (pause) _gridStateSerializer?.ClearSave(_prefs);
        }

        private void OnApplicationQuit()
        {
            _gridStateSerializer?.ClearSave(_prefs);
        }

        private void SaveGameState()
        {
            // User requirement: Level always starts clean from the beginning without restoring mid-level paths
            if (_prefs != null)
            {
                _gridStateSerializer?.ClearSave(_prefs);
            }
        }

        private IEnumerator WaitForRoot()
        {
            int retries = _rootSearchRetries;
            while (_cachedRoot == null && retries > 0)
            {
                // Use FindObjectsByType directly - Root.AllRoots is internal
                var roots = FindObjectsByType<Root>(FindObjectsInactive.Exclude);
                if (roots != null && roots.Length > 0)
                {
                    _cachedRoot = roots[0];
                    break;
                }

                retries--;
                if (retries > 0)
                    yield return new WaitForSeconds(_rootSearchInterval);
            }
        }

        /// <summary>
        /// Resolves a LevelData by index, preferring ILevelProgressionService
        /// (handles Resources → packs → procedural fallback).
        /// Falls back to initialLevel field for Editor compatibility.
        /// </summary>
        private LevelData ResolveLevelByIndex(int index)
        {
            if (_progressionService != null)
            {
                var level = _progressionService.GetOrGenerateLevel(index);
                if (level != null) return level;
            }

            if (index == 0 && initialLevel != null)
                return initialLevel;

            _loggerService?.LogWarning($"[PixelFlow] Could not resolve level index {index} via LevelProgressionService.");
            return null;
        }

        /// <summary>
        /// Grid'de viyadüksüz kesişim olan ilk hücreyi bulur.
        /// Kayıtlı oyun yüklenirken anında kazayı önlemek için kullanılır.
        /// </summary>
        private Vector2Int? FindFirstCrashCell(IGridModel grid)
        {
            for (int x = 0; x < grid.Width; x++)
            {
                for (int y = 0; y < grid.Height; y++)
                {
                    var cell = grid.GetCell(x, y);
                    if (cell.PathColorCount >= 2 && !cell.HasViaduct)
                    {
                        return new Vector2Int(x, y);
                    }
                }
            }
            return null;
        }
    }
}
