using Nexus.Core;
using PixelFlow.Commands;
using PixelFlow.Models;
using PixelFlow.Signals;
using PixelFlow.Data;
using PixelFlow.Services;
using UnityEngine;
using System;

namespace PixelFlow.Views
{
    public class HUDMediator : Mediator<HUDView>
    {
        [Inject] public IHintModel HintModel { get; set; }
        [Inject] public ILevelModel LevelModel { get; set; }
        [Inject] public ISettingsModel SettingsModel { get; set; }
        [Inject] public IGameStateModel GameStateModel { get; set; }
        [Inject] public IGameSessionModel GameSessionModel { get; set; }
        [Inject] public IGameHistoryService HistoryService { get; set; }
        [Inject] public ILevelProgressionService ProgressionService { get; set; }

        [SerializeField] private LevelPack _fallbackLevelPack;

        private Action _themeDarkHandler;
        private Action _themeLightHandler;
        private Action _themeNeonHandler;
        private Vector2Int _lastCrashPosition;

        protected override void OnBind()
        {
            _themeDarkHandler = () => FireTheme(PixelFlow.Models.AppTheme.Dark);
            _themeLightHandler = () => FireTheme(PixelFlow.Models.AppTheme.Light);
            _themeNeonHandler = () => FireTheme(PixelFlow.Models.AppTheme.Neon);

            View.OnHintClicked += HandleHintClicked;
            View.OnNextLevelClicked += HandleNextLevelClicked;
            View.OnReturnToHubClicked += HandleReturnToHubClicked;
            View.OnUndoClicked += HandleUndoClicked;
            View.OnRedoClicked += HandleRedoClicked;
            View.OnThemeDarkClicked += _themeDarkHandler;
            View.OnThemeLightClicked += _themeLightHandler;
            View.OnThemeNeonClicked += _themeNeonHandler;
            View.OnSimulateDebugPressed += HandleSimulateDebugPressed;
            View.OnCrisisViaductClicked += HandleCrisisViaductClicked;
            View.OnCrisisUndoClicked += HandleCrisisUndoClicked;

            HintModel.OnHintCountChanged += HandleHintCountChanged;
            GameSessionModel.OnScoreChanged += HandleScoreChanged;
            GameSessionModel.OnTimeChanged += HandleTimeChanged;
            GameSessionModel.OnStarsChanged += HandleStarsChanged;
            GameSessionModel.OnSimulationTimerChanged += HandleSimulationTimerChanged;
            GameSessionModel.OnViaductsChanged += HandleViaductsChanged;

            View.HideCompletion();
            View.UpdateHintCount(HintModel.HintsRemaining);
            View.UpdateScore(GameSessionModel.Score);
            View.UpdateTimer(GameSessionModel.ElapsedTime);
            View.UpdateStars(GameSessionModel.StarsEarned);
            View.HighlightActiveTheme(SettingsModel.CurrentTheme);

            Subscribe<LevelCompletedSignal>(HandleLevelCompleted);
            Subscribe<LoadLevelSignal>(HandleLoadLevel);
            Subscribe<ThemeChangedSignal>(HandleThemeChanged);
            Subscribe<GridUpdatedSignal>(HandleGridUpdated);
            Subscribe<CrashDetectedSignal>(HandleCrashDetected);
            Subscribe<PathIntersectionWarningSignal>(HandleIntersectionWarning);
            Subscribe<ViaductExhaustedSignal>(HandleViaductExhausted);
            Subscribe<CrisisRetryExhaustedSignal>(HandleCrisisRetryExhausted);

            GameStateModel.OnStateChanged += HandleStateChanged;
            UpdateVisibility();

            RefreshUndoRedoButtons();
        }

        protected override void OnUnbind()
        {
            View.OnHintClicked -= HandleHintClicked;
            View.OnNextLevelClicked -= HandleNextLevelClicked;
            View.OnReturnToHubClicked -= HandleReturnToHubClicked;
            View.OnUndoClicked -= HandleUndoClicked;
            View.OnRedoClicked -= HandleRedoClicked;
            View.OnSimulateDebugPressed -= HandleSimulateDebugPressed;
            View.OnCrisisViaductClicked -= HandleCrisisViaductClicked;
            View.OnCrisisUndoClicked -= HandleCrisisUndoClicked;

            if (_themeDarkHandler != null) View.OnThemeDarkClicked -= _themeDarkHandler;
            if (_themeLightHandler != null) View.OnThemeLightClicked -= _themeLightHandler;
            if (_themeNeonHandler != null) View.OnThemeNeonClicked -= _themeNeonHandler;
            _themeDarkHandler = null;
            _themeLightHandler = null;
            _themeNeonHandler = null;

            if (_returnToHubCoroutine != null) View.StopCoroutine(_returnToHubCoroutine);
            HintModel.OnHintCountChanged -= HandleHintCountChanged;
            GameSessionModel.OnScoreChanged -= HandleScoreChanged;
            GameSessionModel.OnTimeChanged -= HandleTimeChanged;
            GameSessionModel.OnStarsChanged -= HandleStarsChanged;
            GameSessionModel.OnSimulationTimerChanged -= HandleSimulationTimerChanged;
            GameSessionModel.OnViaductsChanged -= HandleViaductsChanged;
            GameStateModel.OnStateChanged -= HandleStateChanged;
        }

        private void FireTheme(PixelFlow.Models.AppTheme theme)
        {
            if (SettingsModel.CurrentTheme == theme) return;
            SignalBus.Fire(new ChangeThemeSignal { Theme = theme });
        }

        private void HandleLoadLevel(LoadLevelSignal signal)
        {
            View.HideCompletion();
            View.HideCrisis();
        }

        private void HandleHintClicked()
        {
            if (GameStateModel.CurrentState != GameState.Playing)
            {
                Debug.Log("[HUDMediator] Hint ignored: game is not in Playing state.");
                return;
            }
            SignalBus.Fire(new RequestHintSignal());
        }

        private void HandleUndoClicked()
        {
            var state = GameStateModel.CurrentState;
            if (state != GameState.Playing && state != GameState.Paused) return;
            SignalBus.Fire(new UndoSignal());
        }

        private void HandleRedoClicked()
        {
            var state = GameStateModel.CurrentState;
            if (state != GameState.Playing && state != GameState.Paused) return;
            SignalBus.Fire(new RedoSignal());
        }

        private void HandleGridUpdated(GridUpdatedSignal signal)
        {
            RefreshUndoRedoButtons();
            var state = GameStateModel.CurrentState;
            if (state == GameState.Playing || state == GameState.Simulating)
            {
                View.HideCrisis();
            }
        }

        private void HandleCrashDetected(CrashDetectedSignal signal)
        {
            _lastCrashPosition = signal.Position;
            View.ShowCrisis(GameSessionModel.AvailableViaducts);
        }

        private void HandleIntersectionWarning(PathIntersectionWarningSignal signal)
        {
            Debug.Log($"[HUDMediator] Intersection warning at {signal.Position} — viaduct may be needed.");
        }

        private void HandleSimulationTimerChanged(float remaining)
        {
            View.UpdateSimulationTimer(remaining);
        }

        private void HandleViaductsChanged(int count)
        {
            if (count <= 0)
            {
                View.ShowViaductLimitReached();
            }
        }

        private void HandleCrisisViaductClicked()
        {
            Debug.Log($"[HUDMediator] Crisis Viaduct Clicked. Placing viaduct at {_lastCrashPosition}");
            SignalBus.Fire(new PlaceViaductSignal { Position = _lastCrashPosition });
        }

        private void HandleCrisisUndoClicked()
        {
            Debug.Log("[HUDMediator] Crisis Undo Clicked. Reverting path.");
            GameSessionModel?.MarkCrisisUndoUsed();
            SignalBus.Fire(new UndoSignal());
            View?.HideCrisis();
            if (GameStateModel != null && GameStateModel.CurrentState == GameState.Paused)
            {
                GameStateModel.SetState(GameState.Playing);
            }
        }

        private void RefreshUndoRedoButtons()
        {
            if (View == null) return;
            if (HistoryService != null)
            {
                View.SetUndoInteractable(HistoryService.CanUndo);
                View.SetRedoInteractable(HistoryService.CanRedo);
            }
            else
            {
                View.SetUndoInteractable(false);
                View.SetRedoInteractable(false);
            }
        }

        private void HandleNextLevelClicked()
        {
            Debug.Log($"[HUDMediator] HandleNextLevelClicked() called. Current State: {GameStateModel.CurrentState}");
            if (GameStateModel.CurrentState != GameState.LevelCompleted)
            {
                Debug.LogWarning($"[HUDMediator] Next level ignored: state={GameStateModel.CurrentState}");
                return;
            }

            var pack = ResolveLevelPack();
            if (pack == null)
            {
                Debug.LogWarning("[HUDMediator] ResolveLevelPack returned null.");
            }
            else
            {
                Debug.Log($"[HUDMediator] LevelPack found. Total levels: {pack.levels?.Count ?? 0}");
            }

            var current = LevelModel.CurrentLevel;
            if (current == null)
            {
                Debug.LogWarning("[HUDMediator] No current level loaded; cannot determine next.");
                return;
            }

            int currentIndex = pack != null && pack.levels != null ? pack.levels.FindIndex(l => l != null && l.levelIndex == current.levelIndex) : -1;
            int nextLevelIndex = current.levelIndex + 1;
            Debug.Log($"[HUDMediator] Current Level Index: {current.levelIndex}, Position in Pack: {currentIndex}, Next Target Index: {nextLevelIndex}");

            LevelData nextLevel = null;
            if (pack != null && pack.levels != null && currentIndex >= 0 && currentIndex + 1 < pack.levels.Count)
            {
                nextLevel = pack.levels[currentIndex + 1];
                if (nextLevel != null)
                {
                    Debug.Log($"[HUDMediator] Found next level in LevelPack: {nextLevel.name}");
                }
            }

            if (nextLevel == null)
            {
                Debug.Log($"[HUDMediator] Next level not in pack. Generating procedurally for index {nextLevelIndex}...");
                nextLevel = ProgressionService.GetOrGenerateLevel(nextLevelIndex);
            }

            if (nextLevel != null)
            {
                Debug.Log($"[HUDMediator] Firing LoadLevelSignal for level index: {nextLevel.levelIndex} ({nextLevel.name})");
                View.HideCompletion();
                SignalBus.Fire(new LoadLevelSignal { LevelToLoad = nextLevel });
            }
            else
            {
                Debug.LogError("[HUDMediator] Failed to load or generate next level! nextLevel is null.");
            }
        }

        private LevelPack ResolveLevelPack()
        {
            if (_fallbackLevelPack != null) return _fallbackLevelPack;
            return Resources.Load<LevelPack>("Levels/MainLevelPack");
        }

        private void HandleHintCountChanged(int count)
        {
            View.UpdateHintCount(count);
        }

        private void HandleScoreChanged(int score)
        {
            View.UpdateScore(score);
        }

        private void HandleTimeChanged(float time)
        {
            View.UpdateTimer(time);
        }

        private void HandleStarsChanged(int stars)
        {
            View.UpdateStars(stars);
        }

        private Coroutine _returnToHubCoroutine;

        private void HandleLevelCompleted(LevelCompletedSignal signal)
        {
            if (!Application.isPlaying) return;
            if (View == null || GameSessionModel == null) return;
            View.ShowCompletion(GameSessionModel.Score, GameSessionModel.StarsEarned);

            if (View.isActiveAndEnabled)
                _returnToHubCoroutine = View.StartCoroutine(AutoReturnToHubRoutine());
        }

        private System.Collections.IEnumerator AutoReturnToHubRoutine()
        {
            yield return new WaitForSeconds(3f);
            if (View != null && GameStateModel != null && SignalBus != null
                && GameStateModel.CurrentState == GameState.LevelCompleted)
            {
                SignalBus.Fire(new RequestReturnToHubSignal());
            }
        }

        private void HandleReturnToHubClicked()
        {
            if (GameStateModel.CurrentState != GameState.LevelCompleted) return;
            SignalBus.Fire(new RequestReturnToHubSignal());
        }

        private void HandleThemeChanged(ThemeChangedSignal signal)
        {
            View.HighlightActiveTheme(SettingsModel.CurrentTheme);
        }

        private void HandleViaductExhausted(ViaductExhaustedSignal signal)
        {
            Debug.Log("[HUDMediator] Viaducts exhausted! Showing crisis prompt.");
        }

        private void HandleCrisisRetryExhausted(CrisisRetryExhaustedSignal signal)
        {
            Debug.Log($"[HUDMediator] Crisis retries exhausted ({signal.RetryCount}). Requesting ad/skip.");
        }

        private void HandleStateChanged(GameState state)
        {
            UpdateVisibility();
        }

        private void UpdateVisibility()
        {
            var state = GameStateModel.CurrentState;
            bool isGameplay = state == GameState.Playing || state == GameState.Simulating || state == GameState.Paused || state == GameState.LevelCompleted;
            
            // Do not disable the GameObject itself, as that unregisters the View and destroys the Mediator binding.
            // Instead, disable/enable the Canvas component, or control CanvasGroup alpha/interactivity.
            var canvas = View.GetComponent<Canvas>();
            if (canvas != null)
            {
                canvas.enabled = isGameplay;
            }
            else
            {
                var canvasGroup = View.GetComponent<CanvasGroup>();
                if (canvasGroup == null)
                {
                    canvasGroup = View.gameObject.AddComponent<CanvasGroup>();
                }
                canvasGroup.alpha = isGameplay ? 1f : 0f;
                canvasGroup.blocksRaycasts = isGameplay;
                canvasGroup.interactable = isGameplay;
            }
        }

        private void HandleSimulateDebugPressed()
        {
            var state = GameStateModel.CurrentState;
            if (state == GameState.Playing)
            {
                Debug.Log("[HUDMediator] Debug: Manually starting simulation phase (Simulating).");
                GameStateModel.SetState(GameState.Simulating);
            }
            else if (state == GameState.Simulating)
            {
                Debug.Log("[HUDMediator] Debug: Manually stopping simulation phase (Playing).");
                GameStateModel.SetState(GameState.Playing);
            }
        }
    }
}