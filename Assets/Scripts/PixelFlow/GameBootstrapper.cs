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
    /// </summary>
    public class GameBootstrapper : MonoBehaviour
    {
        public LevelData initialLevel;
        public Root nexusRoot;

        private const int RootSearchRetries = 10;
        private const float RootSearchInterval = 0.1f;
        private Root _cachedRoot;

        private ISignalBus _signalBus;
        private IGameStateModel _stateModel;
        private IGridModel _gridModel;
        private IGameSessionModel _sessionModel;
        private ILevelModel _levelModel;
        private ILoggerService _loggerService;

        private IEnumerator Start()
        {
            yield return WaitForRoot();
            if (_cachedRoot == null)
            {
                Debug.LogError("[PixelFlow] ERROR: Nexus Root not found after retries. Game cannot start.");
                yield break;
            }
            nexusRoot = _cachedRoot;

            while (!nexusRoot.IsInitialized)
                yield return null;

            // Container'dan kritik servisleri çözümle.
            try
            {
                var container = nexusRoot.Context.Container;
                _loggerService = container.Resolve<ILoggerService>();
                _loggerService?.Log("[PixelFlow] Nexus Root initialized successfully. Resolving services...");

                _signalBus = container.Resolve<ISignalBus>();
                _stateModel = container.Resolve<IGameStateModel>();
                _gridModel = container.Resolve<IGridModel>();
                _sessionModel = container.Resolve<IGameSessionModel>();
                _levelModel = container.Resolve<ILevelModel>();
                
                // Unity runtime update'leri ve kaza kurtarma stratejilerini tetiklemek için resolve et
                container.Resolve<IVehicleSimulator>();
                container.Resolve<IObstacleService>();

                _loggerService?.Log("[PixelFlow] DI Services resolved successfully.");
            }
            catch (System.Exception ex)
            {
                if (_loggerService != null)
                    _loggerService.LogError($"[PixelFlow] ERROR: DI resolve failed: {ex.Message}");
                else
                    Debug.LogError($"[PixelFlow] ERROR: DI resolve failed: {ex.Message}");
                yield break;
            }

            var splash = FindAnyObjectByType<Views.SplashView>();
            if (splash != null)
            {
                _loggerService?.Log("[PixelFlow] Waiting for Splash screen completion...");
                bool splashDone = false;
                splash.OnSplashComplete += () => splashDone = true;
                yield return new WaitUntil(() => splashDone);
                _loggerService?.Log("[PixelFlow] Splash screen complete.");
            }

            var prefs = nexusRoot.Context.Container.Resolve<IPlayerPrefsService>();
            if (GridStateSerializer.HasSavedGame(prefs))
            {
                _loggerService?.Log("[PixelFlow] Saved game detected in PlayerPrefs. Checking save validity...");
                var saved = GridStateSerializer.Load(prefs);
                if (saved != null)
                {
                    var cloud = Models.CloudSaveManager.LoadCloudRecord(prefs);
                    string localJson = prefs.GetString("NT_PuzzleSave_", "");
                    var local = new Models.CloudSaveRecord
                    {
                        PlayerId = Models.CloudSaveManager.GetOrCreatePlayerId(prefs),
                        LocalSaveJson = localJson,
                        TimestampUnix = (long)(System.DateTime.UtcNow - new System.DateTime(1970, 1, 1, 0, 0, 0, System.DateTimeKind.Utc)).TotalSeconds
                    };
                    string resolvedJson = Models.CloudSaveManager.ResolveConflict(local, cloud);
                    if (!string.IsNullOrEmpty(resolvedJson) && resolvedJson != localJson)
                    {
                        prefs.SetString("NT_PuzzleSave_", resolvedJson);
                        saved = GridStateSerializer.Load(prefs);
                    }

                    if (saved != null && saved.cells != null && saved.cells.Count > 0)
                    {
                        bool hasNodesOrPaths = saved.cells.Exists(c => c.state == (int)Models.CellState.Node) || (saved.paths != null && saved.paths.Count > 0);
                        if (hasNodesOrPaths)
                        {
                            var level = ResolveLevelByIndex(saved.levelIndex);
                            if (level != null)
                            {
                                _loggerService?.Log($"[PixelFlow] Restoring valid saved game: Level {saved.levelIndex + 1} ({level.name}, Grid: {saved.width}x{saved.height}, Cells: {saved.cells.Count}, Paths: {saved.paths.Count})");
                                _levelModel.SetLevel(level);
                                GridStateSerializer.ApplyToGrid(saved, _gridModel);
                                GridStateSerializer.EnsureInitialNodesOnGrid(level, _gridModel);
                                _sessionModel.ApplySave(saved.availableViaducts, saved.maxViaducts,
                                    saved.elapsedTime, saved.score, saved.stars, saved.levelIndex);

                                var obstacleService = nexusRoot.Context.Container.Resolve<IObstacleService>();
                                obstacleService?.InitializeFromLevel(level);

                                var tutorialDriver = nexusRoot.Context.Container.Resolve<ITutorialDriver>();
                                tutorialDriver?.OnLevelLoaded(level.levelIndex);

                                _signalBus.Fire(new GridUpdatedSignal());
                                _stateModel.SetState(GameState.Playing);
                                _loggerService?.Log($"[PixelFlow] Game state transitioned to Playing. Level {level.levelIndex + 1} restored.");
                                yield break;
                            }
                            else
                            {
                                _loggerService?.LogWarning($"[PixelFlow] Could not resolve LevelData asset for index {saved.levelIndex}. Falling back to Hub.");
                            }
                        }
                        else
                        {
                            _loggerService?.LogWarning("[PixelFlow] Saved game snapshot had 0 nodes/paths. Clearing empty save file.");
                            GridStateSerializer.ClearSave(prefs);
                        }
                    }
                }
            }

            // İlk çalıştırma veya save bozuk → doğrudan Playing state'e geç, ilk level'ı yükle.
            _loggerService?.Log("[PixelFlow] No valid save file found — loading initial level directly.");
            EnterPlaying();
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
            
            // Eğer Hub ekranındaysak, Grid arka plan için boş olarak yaratıldığından bunu save etmemeliyiz.
            if (_stateModel.CurrentState == GameState.MainMenu) return;

            try
            {
                var prefs = nexusRoot.Context.Container.Resolve<IPlayerPrefsService>();
                GridStateSerializer.Save(_gridModel, _sessionModel, _levelModel, prefs);
                Models.CloudSaveManager.SyncToCloud(
                    prefs,
                    prefs.GetString("NT_PuzzleSave_", ""),
                    _sessionModel.Score);
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[GameBootstrapper] Failed to save game state: {ex.Message}");
            }
        }

        private void EnterPlaying()
        {
            if (initialLevel == null) initialLevel = ResolveInitialLevel();
            if (initialLevel != null)
            {
                _levelModel.SetLevel(initialLevel);
                GridStateSerializer.ApplyToGrid(BuildFreshGridForLevel(initialLevel), _gridModel);
            }
            _stateModel.SetState(GameState.Playing);
            _signalBus.Fire(new LoadedInitialLevelSignal());
        }

        private GridStateSerializer.GridSaveData BuildFreshGridForLevel(LevelData level)
        {
            // Boş bir save snapshot'ı üret; sadece level index ve boyut var.
            return new GridStateSerializer.GridSaveData
            {
                levelIndex = level.levelIndex,
                width = level.width,
                height = level.height,
            };
        }

        private IEnumerator WaitForRoot()
        {
            int retries = RootSearchRetries;
            while (_cachedRoot == null && retries > 0)
            {
                _cachedRoot = FindAnyObjectByType<Root>();
                if (_cachedRoot == null)
                {
                    retries--;
                    yield return new WaitForSeconds(RootSearchInterval);
                }
            }
        }

        private LevelData ResolveLevelByIndex(int index)
        {
            // Önce Resources/Levels/LevelN'i dene
            var byName = Resources.Load<LevelData>($"Levels/Level{index + 1}");
            if (byName != null) return byName;
            // Sonra diğer asset isimlerini
            var all = Resources.LoadAll<LevelData>("Levels");
            if (all != null && all.Length > 0)
            {
                System.Array.Sort(all, (a, b) => a.levelIndex.CompareTo(b.levelIndex));
                foreach (var lvl in all)
                {
                    if (lvl != null && lvl.levelIndex == index) return lvl;
                }
            }
            return ResolveInitialLevel();
        }

        private LevelData ResolveInitialLevel()
        {
            if (initialLevel != null) return initialLevel;

            var byName = Resources.Load<LevelData>("Levels/Level1");
            if (byName != null) return byName;

            var any = Resources.LoadAll<LevelData>("Levels");
            if (any != null && any.Length > 0)
            {
                System.Array.Sort(any, (a, b) => a.levelIndex.CompareTo(b.levelIndex));
                Debug.LogWarning($"[PixelFlow] Levels/Level1 not found; using lowest levelIndex asset: {any[0].name} (Index: {any[0].levelIndex})");
                return any[0];
            }

            return null;
        }
    }
}
