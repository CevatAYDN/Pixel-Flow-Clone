using Nexus.Core;
using Nexus.Core.Services;
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
        [Inject] public IPathService PathService { get; set; }
        [Inject] public IGridModel GridModel { get; set; }
        [Inject, OptionalInject] public IPowerUpService PowerUpService { get; set; }
        [Inject] public ILoggerService LoggerService { get; set; }
        [Inject] public IHintModel HintModel { get; set; }
        [Inject] public ILevelModel LevelModel { get; set; }
        [Inject] public ISettingsModel SettingsModel { get; set; }
        [Inject] public IGameStateModel GameStateModel { get; set; }
        [Inject] public IGameSessionModel GameSessionModel { get; set; }
        [Inject] public IGameHistoryService HistoryService { get; set; }
        [Inject] public ILevelProgressionService ProgressionService { get; set; }
        [Inject] public ILocalizationService LocalizationService { get; set; }

        protected override void OnBind()
        {
            View.OnHintClicked += HandleHintClicked;
            View.OnNextLevelClicked += HandleNextLevelClicked;
            View.OnContinueClicked += HandleContinueClicked;
            View.OnUndoClicked += HandleUndoClicked;
            View.OnRedoClicked += HandleRedoClicked;
            View.OnViaductClicked += HandleViaductClicked;
            View.OnRainbowRoadClicked += HandleRainbowRoadClicked;
            View.OnClearJamClicked += HandleClearJamClicked;
            View.OnPauseClicked += HandlePauseClicked;
            View.OnRetryClicked += HandleRetryClicked;
            View.OnLevelFailedContinueClicked += HandleLevelFailedContinueClicked;
            View.OnGarageClicked += HandleGarageClicked;

            if (LocalizationService == null)
                throw new DataValidationException("HUDMediator: ILocalizationService must be injected. All UI texts depend on it.");

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
            
            UpdateLevelTitleText();
            View.UpdateViaductCount(GameSessionModel.AvailableViaducts);

            Subscribe<LevelCompletedSignal>(HandleLevelCompleted);
            Subscribe<LoadLevelSignal>(OnLoadLevelSignalReceived);
            Subscribe<GridUpdatedSignal>(HandleGridUpdated);
            Subscribe<CrashDetectedSignal>(HandleCrashDetected);
            Subscribe<PathIntersectionWarningSignal>(HandleIntersectionWarning);
            Subscribe<ViaductExhaustedSignal>(HandleViaductExhausted);
            Subscribe<CrisisRetryExhaustedSignal>(HandleCrisisRetryExhausted);
            Subscribe<LevelFailedSignal>(HandleLevelFailed);
            Subscribe<CheckWinConditionSignal>(OnCheckWinConditionReceived);

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
            View.OnViaductClicked -= HandleViaductClicked;
            View.OnRainbowRoadClicked -= HandleRainbowRoadClicked;
            View.OnClearJamClicked -= HandleClearJamClicked;
            View.OnPauseClicked -= HandlePauseClicked;
            View.OnRetryClicked -= HandleRetryClicked;
            View.OnLevelFailedContinueClicked -= HandleLevelFailedContinueClicked;
            View.OnGarageClicked -= HandleGarageClicked;

            if (_continueCoroutine != null && View != null) View.StopCoroutine(_continueCoroutine);
            _continueCoroutine = null;
            HintModel.OnHintCountChanged -= HandleHintCountChanged;
            GameSessionModel.OnScoreChanged -= HandleScoreChanged;
            GameSessionModel.OnTimeChanged -= HandleTimeChanged;
            GameSessionModel.OnStarsChanged -= HandleStarsChanged;
            GameSessionModel.OnSimulationTimerChanged -= HandleSimulationTimerChanged;
            GameSessionModel.OnViaductsChanged -= HandleViaductsChanged;
            GameStateModel.OnStateChanged -= HandleStateChanged;
        }

        private void HandleLoadLevel(LoadLevelSignal signal)
        {
            View.HideCompletion();
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

        private void HandleViaductClicked()
        {
            LoggerService?.Log("[PixelFlow.HUDMediator] VİYADÜK button clicked.");
            var state = GameStateModel.CurrentState;
            if (state != GameState.Playing && state != GameState.Paused) return;

            Vector2Int? targetCell = null;
            var grid = GridModel?.Grid;

            if (grid != null && GridModel != null)
            {
                // Priority 1: Active crash position if valid and cell lacks a Viaduct
                var crashPos = GridModel.LastCrashPosition.Value;
                if (crashPos.x >= 0 && crashPos.y >= 0 && crashPos.x < GridModel.Width && crashPos.y < GridModel.Height)
                {
                    var crashCell = grid[crashPos.x, crashPos.y];
                    if (crashCell != null && crashCell.State != CellState.Node && !crashCell.HasViaduct)
                    {
                        targetCell = crashPos;
                    }
                }

                // Priority 2: Last touched/drawn path cell if valid and lacks a Viaduct
                if (!targetCell.HasValue)
                {
                    var lastPos = GridModel.LastPosition.Value;
                    if (lastPos.x >= 0 && lastPos.y >= 0 && lastPos.x < GridModel.Width && lastPos.y < GridModel.Height)
                    {
                        var lastCell = grid[lastPos.x, lastPos.y];
                        if (lastCell != null && lastCell.State != CellState.Node && !lastCell.HasViaduct && lastCell.PathColorCount > 0)
                        {
                            targetCell = lastPos;
                        }
                    }
                }

                // Priority 3: First drawn path cell on the grid that lacks a Viaduct
                if (!targetCell.HasValue)
                {
                    for (int x = 0; x < GridModel.Width; x++)
                    {
                        for (int y = 0; y < GridModel.Height; y++)
                        {
                            var cell = grid[x, y];
                            if (cell != null && cell.State != CellState.Node && !cell.HasViaduct && cell.PathColorCount > 0)
                            {
                                targetCell = new Vector2Int(x, y);
                                break;
                            }
                        }
                        if (targetCell.HasValue) break;
                    }
                }
            }

            if (targetCell.HasValue)
            {
                LoggerService?.Log($"[PixelFlow.HUDMediator] Placing Viaduct on target cell {targetCell.Value}.");
                SignalBus.Fire(new PlaceViaductSignal { Position = targetCell.Value });
            }
            else
            {
                LoggerService?.Log("[PixelFlow.HUDMediator] No path cell found for Viaduct. Showing toast info.");
                string msg = LocalizationService.GetString("intersection_warning_toast_msg") ?? "Kesişme! Viyadük gerekiyor!";
                View.ShowCrashToast(msg);
            }
        }

        private void HandleRainbowRoadClicked()
        {
            LoggerService?.Log("[PixelFlow.HUDMediator] GÖKKUŞAĞI button clicked.");
            var state = GameStateModel.CurrentState;
            if (state != GameState.Playing && state != GameState.Paused) return;
            PowerUpService?.ActivateRainbowRoad();
        }

        private void HandleClearJamClicked()
        {
            LoggerService?.Log("[PixelFlow.HUDMediator] TEMİZLE button clicked.");
            var state = GameStateModel.CurrentState;
            if (state != GameState.Playing && state != GameState.Paused) return;
            PathService?.ClearAllPaths();
            SignalBus.Fire(new GridUpdatedSignal());
        }

        private void HandleGridUpdated(GridUpdatedSignal signal)
        {
            RefreshUndoRedoButtons();
        }

        private void HandleCrashDetected(CrashDetectedSignal signal)
        {
            string msg = LocalizationService.GetString("crash_toast_msg");
            View.ShowCrashToast(msg);
        }

        private void HandleIntersectionWarning(PathIntersectionWarningSignal signal)
        {
            LoggerService?.Log($"[HUDMediator] Intersection warning at {signal.Position} — viaduct may be needed.");
            // b2: game_plan §15.4.2 Layer A — sürtünmesiz uyarı. Oyun durmaz, kısa toast gösterilir.
            string msg = LocalizationService.GetString("intersection_warning_toast_msg");
            View.ShowCrashToast(msg);
        }

        private void HandleSimulationTimerChanged(float remaining)
        {
            string format = LocalizationService.GetString("hud_simulation_timer_format");
            View.UpdateSimulationTimer(remaining, format);
        }

        private void HandleViaductsChanged(int count)
        {
            View.UpdateViaductCount(count);
            if (count <= 0)
            {
                string msg = LocalizationService.GetString("crisis_exhausted_msg");
                View.ShowCrashToast(msg);
            }
        }

        private void RefreshUndoRedoButtons()
        {
            if (View == null) return;
            if (HistoryService != null)
            {
                View.SetUndoInteractable(HistoryService.CanUndo);
            }
            else
            {
                View.SetUndoInteractable(false);
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

        // ⚠️ NOTE: Rainbow Road uses changed handler removed (not in game_plan.md)
        // private void HandleRainbowRoadUsesChanged(int remaining)
        // {
        //     LoggerService?.Log($"[PixelFlow.HUDMediator] Rainbow Road uses updated: {remaining}");
        //     View?.UpdateRainbowRoadCount(remaining);
        // }

        // ⚠️ NOTE: Clear Jam uses changed handler removed (not in game_plan.md)
        // private void HandleClearJamUsesChanged(int remaining)
        // {
        //     LoggerService?.Log($"[PixelFlow.HUDMediator] Clear Jam uses updated: {remaining}");
        //     View?.UpdateClearJamCount(remaining);
        // }

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
            string format = LocalizationService.GetString("hud_hint_count_format");
            View.UpdateHintCount(count, format);
        }

        private void UpdateScoreText(int score)
        {
            string format = LocalizationService.GetString("hud_score_format");
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

            if (_continueCoroutine != null)
            {
                View.StopCoroutine(_continueCoroutine);
                _continueCoroutine = null;
            }

            string title = LocalizationService.GetString("level_completed_title");
            string scoreFormat = LocalizationService.GetString("level_completed_score_format");
            string starsLabel = LocalizationService.GetString("level_completed_stars_label");

            View.ShowCompletion(GameSessionModel.Score, GameSessionModel.StarsEarned, title, scoreFormat, starsLabel);

            if (View.isActiveAndEnabled)
                _continueCoroutine = View.StartCoroutine(AutoContinueToNextRoutine());
        }

        private System.Collections.IEnumerator AutoContinueToNextRoutine()
        {
            yield return new WaitForSecondsRealtime(3f);
            if (View != null && GameStateModel != null && SignalBus != null
                && GameStateModel.CurrentState == GameState.LevelCompleted)
            {
                HandleNextLevelClicked();
            }

            _continueCoroutine = null;
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
            UpdateLevelTitleText();
        }

        private void OnCheckWinConditionReceived(CheckWinConditionSignal signal)
        {
            LoggerService?.Log("[HUDMediator] CheckWinConditionSignal received (from timer or completion).");
            // Win condition kontrolü CheckWinConditionCommand tarafından yapılır.
            // Burada sadece log tutulur — görsel güncelleme gerekirse eklenebilir.
        }

        private void UpdateLevelTitleText()
        {
            var currentLevel = LevelModel?.CurrentLevel;
            int levelNumber = currentLevel != null ? currentLevel.levelIndex + 1 : 1;
            string format = LocalizationService.GetString("hud_level_title_format");
            View?.UpdateLevelTitle(levelNumber, format);
        }

        private void HandleViaductExhausted(ViaductExhaustedSignal signal)
        {
            LoggerService?.Log("[HUDMediator] Viaducts exhausted! Showing crisis prompt.");
            string msg = LocalizationService.GetString("crisis_viaduct_exhausted_msg");
            View.ShowCrashToast(string.Format(msg, GameSessionModel.AvailableViaducts));
        }

        private void HandleCrisisRetryExhausted(CrisisRetryExhaustedSignal signal)
        {
            LoggerService?.Log($"[HUDMediator] Crisis retries exhausted ({signal.RetryCount}). Requesting ad/skip.");
            string msg = LocalizationService.GetString("crisis_retry_exhausted_msg");
            View.ShowCrashToast(string.Format(msg, signal.RetryCount));
        }

        private void HandleGarageClicked()
        {
            LoggerService?.Log("[PixelFlow.HUDMediator] 'Garage' button clicked from gameplay.");
            SignalBus.Fire(new PixelFlow.Signals.ShowGarageSignal());
        }

        private void HandlePauseClicked()
        {
            LoggerService?.Log("[PixelFlow.HUDMediator] 'Pause' button clicked.");
            SignalBus.Fire(new PauseSimulationSignal());
        }

        private void HandleRetryClicked()
        {
            LoggerService?.Log("[PixelFlow.HUDMediator] 'Retry' button clicked.");
            var state = GameStateModel?.CurrentState ?? GameState.Playing;
            if (state != GameState.Playing && state != GameState.Paused && state != GameState.Simulating && state != GameState.LevelFailed) return;
            
            var currentLevel = LevelModel?.CurrentLevel;
            if (currentLevel != null)
            {
                SignalBus.Fire(new LoadLevelSignal { LevelToLoad = currentLevel });
            }
            View.HideLevelFailed();
            View.HideCompletion();
            if (state == GameState.Paused)
            {
                GameStateModel?.SetState(GameState.Playing);
            }
        }

        private void HandleLevelFailed(LevelFailedSignal signal)
        {
            if (!Application.isPlaying) return;
            if (View == null) return;

            LoggerService?.Log($"[PixelFlow.HUDMediator] Level failed popup displayed! Reason: {signal.Reason}");

            string title = LocalizationService.GetString("level_failed_title");
            string retryLabel = LocalizationService.GetString("level_failed_retry");
            string hubLabel = LocalizationService.GetString("level_failed_hub");
            string scoreFormat = LocalizationService.GetString("level_failed_score_format");

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
                LoggerService?.Log($"[PixelFlow.HUDMediator] UpdateVisibility: state={state}, isGameplay={isGameplay}, canvas.enabled={canvas.enabled}");
            }
            else
            {
                var canvasGroup = View.GetComponent<CanvasGroup>();
                if (canvasGroup == null)
                {
                    canvasGroup = View.gameObject.AddComponent<CanvasGroup>();
                    LoggerService?.Log("[PixelFlow.HUDMediator] UpdateVisibility: Added CanvasGroup to HUDView (was null)");
                }
                canvasGroup.alpha = isGameplay ? 1f : 0f;
                canvasGroup.blocksRaycasts = isGameplay;
                canvasGroup.interactable = isGameplay;
                LoggerService?.Log($"[PixelFlow.HUDMediator] UpdateVisibility: state={state}, isGameplay={isGameplay}, " +
                    $"cg.alpha={canvasGroup.alpha:F2}, blocksRaycasts={canvasGroup.blocksRaycasts}, interactable={canvasGroup.interactable}");
            }

            var es = UnityEngine.EventSystems.EventSystem.current;
            LoggerService?.Log($"[PixelFlow.HUDMediator] EventSystem check: current={(bool)es}, " +
                $"inputModule={(es != null ? es.currentInputModule?.GetType().Name : "null")}, " +
                $"activeGO={(es != null && es.currentSelectedGameObject != null ? es.currentSelectedGameObject.name : "null")}");
        }
    }
}
