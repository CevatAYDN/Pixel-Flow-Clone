using UnityEngine;
using Nexus.Core;
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

        private ISignalBus _signalBus;
        private IGameStateModel _stateModel;
        private IGridModel _gridModel;
        private IGameSessionModel _sessionModel;
        private ILevelModel _levelModel;

        private IEnumerator Start()
        {
            yield return WaitForRoot();
            if (nexusRoot == null)
            {
                Debug.LogError("[PixelFlow] Nexus Root not found after retries. Game cannot start.");
                yield break;
            }

            while (!nexusRoot.IsInitialized)
                yield return null;

            // Container'dan kritik servisleri çözümle.
            try
            {
                var container = nexusRoot.Context.Container;
                _signalBus = container.Resolve<ISignalBus>();
                _stateModel = container.Resolve<IGameStateModel>();
                _gridModel = container.Resolve<IGridModel>();
                _sessionModel = container.Resolve<IGameSessionModel>();
                _levelModel = container.Resolve<ILevelModel>();
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[PixelFlow] DI resolve failed: {ex.Message}");
                yield break;
            }

            var splash = FindAnyObjectByType<Views.SplashView>();
            if (splash != null)
            {
                bool splashDone = false;
                splash.OnSplashComplete += () => splashDone = true;
                yield return new WaitUntil(() => splashDone);
            }

            if (GridStateSerializer.HasSavedGame())
            {
                var saved = GridStateSerializer.Load();
                if (saved != null)
                {
                    var prefs = nexusRoot.Context.Container.Resolve<IPlayerPrefsService>();
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
                        saved = GridStateSerializer.Load();
                    }

                    if (saved != null)
                    {
                        var level = ResolveLevelByIndex(saved.levelIndex);
                        if (level != null)
                        {
                            Debug.Log($"[PixelFlow] Restoring saved level {saved.levelIndex} ({saved.width}x{saved.height})");
                            _levelModel.SetLevel(level);
                            GridStateSerializer.ApplyToGrid(saved, _gridModel);
                            _sessionModel.ApplySave(saved.availableViaducts, saved.maxViaducts,
                                saved.elapsedTime, saved.score, saved.stars);
                            _signalBus.Fire(new GridUpdatedSignal());
                            _stateModel.SetState(GameState.Playing);
                            yield break;
                        }
                    }
                }
            }

            // İlk çalıştırma veya save bozuk → Hub'a gir.
            Debug.Log("[PixelFlow] No save — entering Hub.");
            EnterHub();
        }

        /// <summary>
        /// Statik erişim noktası. Save tüm UI/command'lardan tetiklenebilir.
        /// GameBootstrapper instance'ı yoksa null check ile no-op.
        /// </summary>
        public static void RequestSave(IGridModel grid, IGameSessionModel session, ILevelModel level)
        {
            var inst = FindAnyObjectByType<GameBootstrapper>();
            if (inst == null || inst._gridModel == null) return;
            try { GridStateSerializer.Save(grid, session, level); }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[GameBootstrapper.RequestSave] {ex.Message}");
            }
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
            if (_gridModel == null || _sessionModel == null || _levelModel == null) return;
            if (_levelModel.CurrentLevel == null) return;
            try
            {
                GridStateSerializer.Save(_gridModel, _sessionModel, _levelModel);
                var prefs = nexusRoot.Context.Container.Resolve<IPlayerPrefsService>();
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

        private void EnterHub()
        {
            if (initialLevel == null) initialLevel = ResolveInitialLevel();
            if (initialLevel != null)
            {
                _levelModel.SetLevel(initialLevel);
                GridStateSerializer.ApplyToGrid(BuildFreshGridForLevel(initialLevel), _gridModel);
            }
            _stateModel.SetState(GameState.MainMenu);
            _signalBus.Fire(new EnterHubSignal());
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
            while (nexusRoot == null && retries > 0)
            {
                nexusRoot = FindAnyObjectByType<Root>();
                if (nexusRoot == null)
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
            foreach (var lvl in all)
            {
                if (lvl != null && lvl.levelIndex == index) return lvl;
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
                Debug.LogWarning($"[PixelFlow] Levels/Level1 not found; using first available LevelData: {any[0].name}");
                return any[0];
            }

            return null;
        }
    }
}
