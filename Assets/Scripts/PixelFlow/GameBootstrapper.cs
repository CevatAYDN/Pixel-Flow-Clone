using UnityEngine;
using Nexus.Core;
using Nexus.Core.Services;
using PixelFlow.Data;
using PixelFlow.Models;
using PixelFlow.Signals;
using PixelFlow.Services;
using System.Collections;

namespace PixelFlow
{
    /// <summary>
    /// GDD §5.1: Boot → Splash → Hub (MainMenu) veya Restore → Playing.
    /// İlk çalıştırmada EnterHubSignal ateşlenir; save varsa restore edilir.
    ///
    /// Tüm DI bağımlılıkları Root başlatıldıktan sonra container'dan tek seferde
    /// çözülür ve önbelleğe alınır. Resources.Load yerine ILevelProgressionService
    /// kullanılır. initialLevel ve nexusRoot public field'ları Editor uyumluluğu
    /// için korunur.
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

            _loggerService?.Log("[PixelFlow.GameBootstrapper] Checking for saved game states to restore...");
            if (TryRestoreSavedGame())
            {
                _loggerService?.Log("[PixelFlow.GameBootstrapper] Saved game state restored successfully. Startup complete.");
                yield break;
            }

            // İlk çalıştırma veya save bozuk → doğrudan Playing state'e geç, ilk level'ı yükle.
            _loggerService?.Log("[PixelFlow.GameBootstrapper] No valid save file found — loading initial level directly.");
            EnterPlaying();
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

                // Trigger lazy init for services that need to be alive at boot
                container.Resolve<IVehicleSimulator>();
                container.Resolve<IObstacleService>();

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
            if (!GridStateSerializer.HasSavedGame(_prefs))
                return false;

            _loggerService?.Log("[PixelFlow.GameBootstrapper] Saved game detected in PlayerPrefs. Checking save validity...");
            var saved = GridStateSerializer.Load(_prefs);
            if (saved == null)
            {
                _loggerService?.LogWarning("[PixelFlow.GameBootstrapper] Failed to load saved game snapshot.");
                return false;
            }

            var cloud = Models.CloudSaveManager.LoadCloudRecord(_prefs);
            string localJson = _prefs.GetString("NT_PuzzleSave_", "");
            var local = new Models.CloudSaveRecord
            {
                PlayerId = Models.CloudSaveManager.GetOrCreatePlayerId(_prefs),
                LocalSaveJson = localJson,
                TimestampUnix = (long)(System.DateTime.UtcNow - new System.DateTime(1970, 1, 1, 0, 0, 0, System.DateTimeKind.Utc)).TotalSeconds
            };
            string resolvedJson = Models.CloudSaveManager.ResolveConflict(local, cloud);
            if (!string.IsNullOrEmpty(resolvedJson) && resolvedJson != localJson)
            {
                _loggerService?.Log("[PixelFlow.GameBootstrapper] Cloud conflict resolved. Updating local save json.");
                _prefs.SetString("NT_PuzzleSave_", resolvedJson);
                saved = GridStateSerializer.Load(_prefs);
            }

            if (saved == null || saved.cells == null || saved.cells.Count == 0)
            {
                _loggerService?.LogWarning("[PixelFlow.GameBootstrapper] Saved game cells collection is empty or null.");
                return false;
            }

            if (saved.paths == null || saved.paths.Count == 0)
            {
                _loggerService?.LogWarning("[PixelFlow.GameBootstrapper] Saved game snapshot had 0 nodes/paths. Clearing empty save file.");
                GridStateSerializer.ClearSave(_prefs);
                return false;
            }

            var level = ResolveLevelByIndex(saved.levelIndex);
            if (level == null)
            {
                _loggerService?.LogWarning($"[PixelFlow.GameBootstrapper] Could not resolve LevelData asset for index {saved.levelIndex}. Falling back to Hub.");
                return false;
            }

            if (!GridStateSerializer.IsSaveDataValidForLevel(saved, level))
            {
                _loggerService?.LogWarning($"[PixelFlow.GameBootstrapper] Outdated or invalid saved game layout detected for Level {saved.levelIndex + 1}. Discarding save...");
                GridStateSerializer.ClearSave(_prefs);
                return false;
            }

            _loggerService?.Log($"[PixelFlow.GameBootstrapper] Restoring valid saved game: Level {saved.levelIndex + 1} ({level.name}, Grid: {saved.width}x{saved.height}, Cells: {saved.cells.Count}, Paths: {saved.paths.Count})");
            _levelModel.SetLevel(level);
            GridStateSerializer.ApplyToGrid(saved, _gridModel);
            GridStateSerializer.EnsureInitialNodesOnGrid(level, _gridModel);
            _sessionModel.ApplySave(saved.availableViaducts, saved.maxViaducts,
                saved.elapsedTime, saved.score, saved.stars, saved.levelIndex);

            var obstacleService = nexusRoot.Context.Container.Resolve<IObstacleService>();
            obstacleService?.InitializeFromLevel(level);

            var tutorialDriver = nexusRoot.Context.Container.Resolve<ITutorialDriver>();
            tutorialDriver?.OnLevelLoaded(level.levelIndex);

            // Kayıtlı oyunda viyadüksüz kesişimler varsa kriz panelini göster
            var crashCell = FindFirstCrashCell(_gridModel);
            if (crashCell.HasValue)
            {
                _loggerService?.Log($"[PixelFlow.GameBootstrapper] Restored game has unresolved intersection at {crashCell.Value}. Showing crisis panel.");
                var cell = _gridModel.GetCell(crashCell.Value);
                var colors = new System.Collections.Generic.List<ColorType>();
                foreach (var pc in cell.GetPathColors())
                    colors.Add(pc);

                _gridModel.LastCrashPosition.Value = crashCell.Value;
                if (colors.Count >= 2)
                {
                    _gridModel.CrashColorA.Value = colors[0];
                    _gridModel.CrashColorB.Value = colors[1];
                }

                _signalBus.Fire(new CrashDetectedSignal
                {
                    Position = crashCell.Value,
                    ColorA = colors.Count >= 1 ? colors[0] : ColorType.None,
                    ColorB = colors.Count >= 2 ? colors[1] : ColorType.None
                });
                _signalBus.Fire(new GridUpdatedSignal());
                _stateModel.SetState(GameState.Playing);
                _stateModel.SetState(GameState.Paused);
                _loggerService?.Log($"[PixelFlow.GameBootstrapper] Game state transitioned to Paused for crisis resolution at {crashCell.Value}.");
            }
            else
            {
                _signalBus.Fire(new GridUpdatedSignal());
                _stateModel.SetState(GameState.Playing);
                _loggerService?.Log($"[PixelFlow] Game state transitioned to Playing. Level {level.levelIndex + 1} restored.");
            }

            return true;
        }

        private void OnApplicationPause(bool pause)
        {
            if (pause) SaveGameState();
        }

        private void OnApplicationQuit()
        {
            SaveGameState();
        }

        private void SaveGameState()
        {
            if (_gridModel == null || _sessionModel == null || _levelModel == null || _stateModel == null) return;
            if (_levelModel.CurrentLevel == null) return;
            if (_stateModel.CurrentState == GameState.MainMenu) return;
            if (_prefs == null) return;

            try
            {
                GridStateSerializer.Save(_gridModel, _sessionModel, _levelModel, _prefs);
                Models.CloudSaveManager.SyncToCloud(
                    _prefs,
                    _prefs.GetString("NT_PuzzleSave_", ""),
                    _sessionModel.Score);
            }
            catch (System.Exception ex)
            {
                (_loggerService ?? FallbackLogger)?.LogWarning($"[GameBootstrapper] Failed to save game state: {ex.Message}");
            }
        }

        private void EnterPlaying()
        {
            LevelData targetLevel = null;

            // Try progression service first (handles Resources + procedural fallback)
            if (_progressionService != null)
            {
                int targetIndex = -1;
                try
                {
                    var progressModel = nexusRoot.Context.Container.Resolve<IProgressModel>();
                    if (progressModel != null)
                    {
                        targetIndex = progressModel.UnlockedLevels - 1;
                        targetLevel = _progressionService.GetOrGenerateLevel(targetIndex);
                        if (targetLevel != null)
                            _loggerService?.Log($"[PixelFlow] Progression indicates Level {progressModel.UnlockedLevels} is unlocked. Resolved via LevelProgressionService: {targetLevel.name}");
                    }
                }
                catch (System.Exception ex)
                {
                    _loggerService?.LogWarning($"[PixelFlow] Failed to resolve level from progression model: {ex.Message}");
                }
            }

            // Fallback to initialLevel public field (Editor-assigned) or Level 1 via Resources
            if (targetLevel == null)
            {
                targetLevel = initialLevel != null ? initialLevel : ResolveLevelByIndex(0);
            }

            if (targetLevel != null)
            {
                _signalBus.Fire(new LoadLevelSignal { LevelToLoad = targetLevel });
            }
            else
            {
                _stateModel.SetState(GameState.Playing);
            }
            _signalBus.Fire(new LoadedInitialLevelSignal());
        }

        private IEnumerator WaitForRoot()
        {
            int retries = _rootSearchRetries;
            while (_cachedRoot == null && retries > 0)
            {
                _cachedRoot = FindAnyObjectByType<Root>();
                if (_cachedRoot == null)
                {
                    retries--;
                    yield return new WaitForSeconds(_rootSearchInterval);
                }
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
