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

        protected override void OnBind()
        {
            _themeDarkHandler = () => FireTheme(PixelFlow.Models.AppTheme.Dark);
            _themeLightHandler = () => FireTheme(PixelFlow.Models.AppTheme.Light);
            _themeNeonHandler = () => FireTheme(PixelFlow.Models.AppTheme.Neon);

            View.OnHintClicked += HandleHintClicked;
            View.OnNextLevelClicked += HandleNextLevelClicked;
            View.OnUndoClicked += HandleUndoClicked;
            View.OnRedoClicked += HandleRedoClicked;
            View.OnThemeDarkClicked += _themeDarkHandler;
            View.OnThemeLightClicked += _themeLightHandler;
            View.OnThemeNeonClicked += _themeNeonHandler;

            HintModel.OnHintCountChanged += HandleHintCountChanged;
            GameSessionModel.OnScoreChanged += HandleScoreChanged;
            GameSessionModel.OnTimeChanged += HandleTimeChanged;
            GameSessionModel.OnStarsChanged += HandleStarsChanged;

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

            RefreshUndoRedoButtons();
        }

        protected override void OnUnbind()
        {
            View.OnHintClicked -= HandleHintClicked;
            View.OnNextLevelClicked -= HandleNextLevelClicked;
            View.OnUndoClicked -= HandleUndoClicked;
            View.OnRedoClicked -= HandleRedoClicked;

            if (_themeDarkHandler != null) View.OnThemeDarkClicked -= _themeDarkHandler;
            if (_themeLightHandler != null) View.OnThemeLightClicked -= _themeLightHandler;
            if (_themeNeonHandler != null) View.OnThemeNeonClicked -= _themeNeonHandler;
            _themeDarkHandler = null;
            _themeLightHandler = null;
            _themeNeonHandler = null;

            HintModel.OnHintCountChanged -= HandleHintCountChanged;
            GameSessionModel.OnScoreChanged -= HandleScoreChanged;
            GameSessionModel.OnTimeChanged -= HandleTimeChanged;
            GameSessionModel.OnStarsChanged -= HandleStarsChanged;
        }

        private void FireTheme(PixelFlow.Models.AppTheme theme)
        {
            if (SettingsModel.CurrentTheme == theme) return;
            SignalBus.Fire(new ChangeThemeSignal { Theme = theme });
        }

        private void HandleLoadLevel(LoadLevelSignal signal)
        {
            View.HideCompletion();
        }

        private void HandleHintClicked()
        {
            if (GameStateModel.CurrentState != GameState.Playing)
            {
                Debug.Log("[HUDMediator] Hint ignored: oyun Playing durumunda değil.");
                return;
            }
            SignalBus.Fire(new RequestHintSignal());
        }

        private void HandleUndoClicked()
        {
            if (GameStateModel.CurrentState != GameState.Playing) return;
            SignalBus.Fire(new UndoSignal());
        }

        private void HandleRedoClicked()
        {
            if (GameStateModel.CurrentState != GameState.Playing) return;
            SignalBus.Fire(new RedoSignal());
        }

        private void HandleGridUpdated(GridUpdatedSignal signal)
        {
            RefreshUndoRedoButtons();
        }

        private void RefreshUndoRedoButtons()
        {
            View.SetUndoInteractable(HistoryService.CanUndo);
            View.SetRedoInteractable(HistoryService.CanRedo);
        }

        private void HandleNextLevelClicked()
        {
            if (GameStateModel.CurrentState != GameState.LevelCompleted)
            {
                Debug.Log($"[HUDMediator] Next level ignored: state={GameStateModel.CurrentState}");
                return;
            }

            var pack = ResolveLevelPack();
            if (pack == null || pack.levels == null || pack.levels.Count == 0)
            {
                Debug.LogWarning("[HUDMediator] No level pack available; cannot load next level.");
                return;
            }

            var current = LevelModel.CurrentLevel;
            if (current == null)
            {
                Debug.LogWarning("[HUDMediator] No current level loaded; cannot determine next.");
                return;
            }

            int currentIndex = pack.levels.FindIndex(l => l.levelIndex == current.levelIndex);
            int nextLevelIndex = current.levelIndex + 1;

            LevelData nextLevel = null;
            if (currentIndex >= 0 && currentIndex + 1 < pack.levels.Count)
            {
                nextLevel = pack.levels[currentIndex + 1];
            }

            if (nextLevel == null)
            {
                nextLevel = ProgressionService.GetOrGenerateLevel(nextLevelIndex);
            }

            if (nextLevel != null)
                SignalBus.Fire(new LoadLevelSignal { LevelToLoad = nextLevel });
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

        private void HandleLevelCompleted(LevelCompletedSignal signal)
        {
            View.ShowCompletion(GameSessionModel.Score, GameSessionModel.StarsEarned);
        }

        private void HandleThemeChanged(ThemeChangedSignal signal)
        {
            View.HighlightActiveTheme(SettingsModel.CurrentTheme);
        }
    }
}