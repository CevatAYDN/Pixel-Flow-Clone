using Nexus.Core;
using Nexus.Core.Services;
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
        [Inject] public ILoggerService LoggerService { get; set; }
        [Inject] public IHintModel HintModel { get; set; }
        [Inject] public ILevelModel LevelModel { get; set; }
        [Inject] public ISettingsModel SettingsModel { get; set; }
        [Inject] public IGameStateModel GameStateModel { get; set; }
        [Inject] public IGameSessionModel GameSessionModel { get; set; }
        [Inject] public IGameHistoryService HistoryService { get; set; }
        [Inject] public ILevelProgressionService ProgressionService { get; set; }
        [Inject] public ILocalizationService LocalizationService { get; set; }

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
            View.OnContinueClicked += HandleContinueClicked;
            View.OnUndoClicked += HandleUndoClicked;
            View.OnRedoClicked += HandleRedoClicked;
            View.OnThemeDarkClicked += _themeDarkHandler;
            View.OnThemeLightClicked += _themeLightHandler;
            View.OnThemeNeonClicked += _themeNeonHandler;
            View.OnSimulateDebugPressed += HandleSimulateDebugPressed;
            View.OnCrisisViaductClicked += HandleCrisisViaductClicked;
            View.OnCrisisUndoClicked += HandleCrisisUndoClicked;
            View.OnPauseClicked += HandlePauseClicked;
            View.OnRetryClicked += HandleRetryClicked;
            View.OnLevelFailedContinueClicked += HandleLevelFailedContinueClicked;

            HintModel.OnHintCountChanged += HandleHintCountChanged;
            GameSessionModel.OnScoreChanged += HandleScoreChanged;
            GameSessionModel.OnTimeChanged += HandleTimeChanged;
            GameSessionModel.OnStarsChanged += HandleStarsChanged;
            GameSessionModel.OnSimulationTimerChanged += HandleSimulationTimerChanged;
            GameSessionModel.OnViaductsChanged += HandleViaductsChanged;

            View.HideCompletion();
            UpdateHintCountText(HintModel.HintsRemaining);
            UpdateScoreText(GameSessionModel.Score);
            View.UpdateTimer(GameSessionModel.ElapsedTime);
            View.UpdateStars(GameSessionModel.StarsEarned);
            View.HighlightActiveTheme(SettingsModel.CurrentTheme);
            UpdateLevelTitleText();

            Subscribe<LevelCompletedSignal>(HandleLevelCompleted);
            Subscribe<LoadLevelSignal>(OnLoadLevelSignalReceived);
            Subscribe<ThemeChangedSignal>(HandleThemeChanged);
            Subscribe<GridUpdatedSignal>(HandleGridUpdated);
            Subscribe<CrashDetectedSignal>(HandleCrashDetected);
            Subscribe<PathIntersectionWarningSignal>(HandleIntersectionWarning);
            Subscribe<ViaductExhaustedSignal>(HandleViaductExhausted);
            Subscribe<CrisisRetryExhaustedSignal>(HandleCrisisRetryExhausted);
            Subscribe<LevelFailedSignal>(HandleLevelFailed);

            GameStateModel.OnStateChanged += HandleStateChanged;
            UpdateVisibility();

            RefreshUndoRedoButtons();
        }

        protected override void OnUnbind()
        {
            View.OnHintClicked -= HandleHintClicked;
            View.OnNextLevelClicked -= HandleNextLevelClicked;
            View.OnContinueClicked -= HandleContinueClicked;
            View.OnUndoClicked -= HandleUndoClicked;
            View.OnRedoClicked -= HandleRedoClicked;
            View.OnSimulateDebugPressed -= HandleSimulateDebugPressed;
            View.OnCrisisViaductClicked -= HandleCrisisViaductClicked;
            View.OnCrisisUndoClicked -= HandleCrisisUndoClicked;
            View.OnPauseClicked -= HandlePauseClicked;
            View.OnRetryClicked -= HandleRetryClicked;
            View.OnLevelFailedContinueClicked -= HandleLevelFailedContinueClicked;

            if (_themeDarkHandler != null) View.OnThemeDarkClicked -= _themeDarkHandler;
            if (_themeLightHandler != null) View.OnThemeLightClicked -= _themeLightHandler;
            if (_themeNeonHandler != null) View.OnThemeNeonClicked -= _themeNeonHandler;
            _themeDarkHandler = null;
            _themeLightHandler = null;
            _themeNeonHandler = null;

            if (_continueCoroutine != null) View.StopCoroutine(_continueCoroutine);
            _continueCoroutine = null;
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
            View.HideLevelFailed();
        }

        private void HandleHintClicked()
        {
            if (GameStateModel.CurrentState != GameState.Playing)
            {
                LoggerService?.Log("[HUDMediator] Hint ignored: game is not in Playing state.");
                return;
            }
            SignalBus.Fire(new RequestHintSignal());
        }

        private void HandleUndoClicked()
        {
            LoggerService?.Log("[PixelFlow.HUDMediator] 'Undo' button clicked.");
            var state = GameStateModel.CurrentState;
            if (state != GameState.Playing && state != GameState.Paused) return;
            SignalBus.Fire(new UndoSignal());
        }

        private void HandleRedoClicked()
        {
            LoggerService?.Log("[PixelFlow.HUDMediator] 'Redo' button clicked.");
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
            
            string title = LocalizationService?.GetString("crisis_title") ?? "TRAFİK KRİZİ! 🚨";
            string desc = LocalizationService?.GetString("crisis_desc") ?? "Çarpışmayı çözmek için viyadük yerleştirin!";
            string format = LocalizationService?.GetString("crisis_viaducts_format") ?? "Kalan Viyadük: {0}";
            string viaductBtn = LocalizationService?.GetString("crisis_viaduct_btn") ?? "Viyadük Kullan";
            string undoBtn = LocalizationService?.GetString("crisis_undo_btn") ?? "Geri Al / Vazgeç";

            View.ShowCrisis(GameSessionModel.AvailableViaducts, title, desc, format, viaductBtn, undoBtn);
        }

        private void HandleIntersectionWarning(PathIntersectionWarningSignal signal)
        {
            LoggerService?.Log($"[HUDMediator] Intersection warning at {signal.Position} — viaduct may be needed.");
        }

        private void HandleSimulationTimerChanged(float remaining)
        {
            string format = LocalizationService?.GetString("hud_simulation_timer_format") ?? "Simülasyon: {0:F1}s";
            View.UpdateSimulationTimer(remaining, format);
        }

        private void HandleViaductsChanged(int count)
        {
            if (count <= 0)
            {
                string msg = LocalizationService?.GetString("crisis_exhausted_msg") ?? "Viyadük hakkınız bitti!";
                View.ShowViaductLimitReached(msg);
            }
        }

        private void HandleCrisisViaductClicked()
        {
            LoggerService?.Log($"[HUDMediator] Crisis Viaduct Clicked. Placing viaduct at {_lastCrashPosition}");
            SignalBus.Fire(new PlaceViaductSignal { Position = _lastCrashPosition });
        }

        private void HandleCrisisUndoClicked()
        {
            LoggerService?.Log("[HUDMediator] Crisis Undo Clicked. Reverting path.");
            GameSessionModel?.MarkCrisisUndoUsed();
            SignalBus.Fire(new UndoSignal());
            View?.HideCrisis();
            // UndoCommand may have already restored state to Playing (UndoCommand.cs:37).
            // If still Paused, resume directly — PauseSimulationSignal is a toggle
            // and would re-pause if Undo already set Playing.
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
            LoggerService?.Log($"[HUDMediator] HandleNextLevelClicked() called. Current State: {GameStateModel.CurrentState}");
            if (GameStateModel.CurrentState != GameState.LevelCompleted)
            {
                LoggerService?.LogWarning($"[HUDMediator] Next level ignored: state={GameStateModel.CurrentState}");
                return;
            }

            var current = LevelModel.CurrentLevel;
            if (current == null)
            {
                LoggerService?.LogWarning("[HUDMediator] No current level loaded; cannot determine next.");
                return;
            }

            int nextLevelIndex = current.levelIndex + 1;
            LoggerService?.Log($"[HUDMediator] Current Level Index: {current.levelIndex}, Next Target Index: {nextLevelIndex}");

            LevelData nextLevel = ProgressionService.GetOrGenerateLevel(nextLevelIndex);

            if (nextLevel != null)
            {
                LoggerService?.Log($"[HUDMediator] Firing LoadLevelSignal for level index: {nextLevel.levelIndex} ({nextLevel.name})");
                View.HideCompletion();
                SignalBus.Fire(new LoadLevelSignal { LevelToLoad = nextLevel });
            }
            else
            {
                LoggerService?.LogError("[HUDMediator] Failed to load or generate next level! nextLevel is null.");
            }
        }

        private void HandleHintCountChanged(int count)
        {
            UpdateHintCountText(count);
        }

        private void HandleScoreChanged(int score)
        {
            UpdateScoreText(score);
        }

        private void UpdateHintCountText(int count)
        {
            string format = LocalizationService?.GetString("hud_hint_count_format");
            if (string.IsNullOrEmpty(format) || !format.Contains("{0}") || format == "hud_hint_count_format")
            {
                format = "TEMİZLE ({0})";
            }
            View.UpdateHintCount(count, format);
        }

        private void UpdateScoreText(int score)
        {
            string format = LocalizationService?.GetString("hud_score_format");
            if (string.IsNullOrEmpty(format) || !format.Contains("{0}") || format == "hud_score_format")
            {
                format = "💰 {0:N0}";
            }
            View.UpdateScore(score, format);
        }

        private void HandleTimeChanged(float time)
        {
            View.UpdateTimer(time);
        }

        private void HandleStarsChanged(int stars)
        {
            View.UpdateStars(stars);
        }

        private Coroutine _continueCoroutine;

        private void HandleLevelCompleted(LevelCompletedSignal signal)
        {
            if (!Application.isPlaying) return;
            if (View == null || GameSessionModel == null) return;

            string title = LocalizationService?.GetString("level_completed_title") ?? "Tebrikler! Seviye Tamamlandı!";
            string scoreFormat = LocalizationService?.GetString("level_completed_score_format") ?? "Skor: {0}";
            string starsLabel = LocalizationService?.GetString("level_completed_stars_label") ?? "Yıldız";

            View.ShowCompletion(GameSessionModel.Score, GameSessionModel.StarsEarned, title, scoreFormat, starsLabel);

            if (View.isActiveAndEnabled)
                _continueCoroutine = View.StartCoroutine(AutoContinueToNextRoutine());
        }

        private System.Collections.IEnumerator AutoContinueToNextRoutine()
        {
            yield return new WaitForSeconds(3f);
            if (View != null && GameStateModel != null && SignalBus != null
                && GameStateModel.CurrentState == GameState.LevelCompleted)
            {
                HandleNextLevelClicked();
            }
        }

        private void HandleContinueClicked()
        {
            if (GameStateModel.CurrentState != GameState.LevelCompleted) return;
            HandleNextLevelClicked();
        }

        private void OnLoadLevelSignalReceived(LoadLevelSignal signal)
        {
            View?.HideCompletion();
            View?.HideLevelFailed();
            View?.HideCrisis();
            UpdateLevelTitleText();
        }

        private void UpdateLevelTitleText()
        {
            var currentLevel = LevelModel?.CurrentLevel;
            int levelNumber = currentLevel != null ? currentLevel.levelIndex + 1 : 1;
            string format = LocalizationService?.GetString("hud_level_title_format");
            if (string.IsNullOrEmpty(format) || !format.Contains("{0}") || format == "hud_level_title_format")
            {
                format = "SEVİYE {0}";
            }
            View?.UpdateLevelTitle(levelNumber, format);
        }

        private void HandleThemeChanged(ThemeChangedSignal signal)
        {
            View.HighlightActiveTheme(SettingsModel.CurrentTheme);
        }

        private void HandleViaductExhausted(ViaductExhaustedSignal signal)
        {
            LoggerService?.Log("[HUDMediator] Viaducts exhausted! Showing crisis prompt.");
            View.ShowViaductLimitReached($"Viaducts exhausted! ({GameSessionModel.AvailableViaducts} remaining)");
        }

        private void HandleCrisisRetryExhausted(CrisisRetryExhaustedSignal signal)
        {
            LoggerService?.Log($"[HUDMediator] Crisis retries exhausted ({signal.RetryCount}). Requesting ad/skip.");
            View.ShowCrisisRetryExhausted(signal.RetryCount);
        }

        private void HandlePauseClicked()
        {
            LoggerService?.Log("[PixelFlow.HUDMediator] 'Pause' button clicked.");
            SignalBus.Fire(new PauseSimulationSignal());
        }

        private void HandleRetryClicked()
        {
            LoggerService?.Log("[PixelFlow.HUDMediator] 'Retry' button clicked.");
            if (GameStateModel.CurrentState != GameState.LevelFailed) return;
            var currentLevel = LevelModel.CurrentLevel;
            if (currentLevel != null)
            {
                SignalBus.Fire(new LoadLevelSignal { LevelToLoad = currentLevel });
            }
            View.HideLevelFailed();
        }

        private void HandleLevelFailed(LevelFailedSignal signal)
        {
            if (!Application.isPlaying) return;
            if (View == null) return;

            LoggerService?.Log($"[PixelFlow.HUDMediator] Level failed popup displayed! Reason: {signal.Reason}");

            string title = LocalizationService?.GetString("level_failed_title") ?? "Seviye Başarısız!";
            string retryLabel = LocalizationService?.GetString("level_failed_retry") ?? "Tekrar Dene";
            string hubLabel = LocalizationService?.GetString("level_failed_hub") ?? "Hub'a Dön";
            string scoreFormat = LocalizationService?.GetString("level_failed_score_format") ?? "Retry: {0}/3";

            View.ShowLevelFailed($"{title} ({signal.Reason})", scoreFormat, retryLabel, hubLabel);
        }

        private void HandleLevelFailedContinueClicked()
        {
            LoggerService?.Log("[PixelFlow.HUDMediator] 'Level Failed Continue' clicked.");
            if (GameStateModel.CurrentState != GameState.LevelFailed) return;
            View.HideLevelFailed();
        }

        private void HandleStateChanged(GameState state)
        {
            LoggerService?.Log($"[PixelFlow.HUDMediator] HandleStateChanged: State -> {state}");
            UpdateVisibility();
        }

        private void UpdateVisibility()
        {
            var state = GameStateModel.CurrentState;
            bool isGameplay = state == GameState.Playing || state == GameState.Simulating || state == GameState.Paused || state == GameState.LevelCompleted || state == GameState.LevelFailed;
            
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
            // Debug-only: bypasses command flow for rapid testing
            // Bu direkt SetState çağrısı sadece Editor/PlayMode debug için kullanılır.
            // MVCS kuralı: normal oynanışta Signal → Command → SetState akışı kullanılır.
#if UNITY_EDITOR
            var state = GameStateModel.CurrentState;
            if (state == GameState.Playing)
            {
                LoggerService?.Log("[HUDMediator] Debug: Manually starting simulation phase (Simulating).");
                GameStateModel.SetState(GameState.Simulating);
            }
            else if (state == GameState.Simulating)
            {
                LoggerService?.Log("[HUDMediator] Debug: Manually stopping simulation phase (Playing).");
                GameStateModel.SetState(GameState.Playing);
            }
#endif
        }
    }
}