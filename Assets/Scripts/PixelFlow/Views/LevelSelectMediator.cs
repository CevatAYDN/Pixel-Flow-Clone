using System.Collections.Generic;
using UnityEngine;
using Nexus.Core;
using Nexus.Core.Services;
using PixelFlow.Models;
using PixelFlow.Signals;
using PixelFlow.Services;

namespace PixelFlow.Views
{
    /// <summary>
    /// settings-levels.html "SEVİYE SEÇİMİ" ekranının Mediator'ı.
    /// GameState.LevelSelect aktifken görünür olur; seviye ızgarasını ProgressModel'in
    /// açılan seviye + yıldız verisiyle doldurur. Bir seviye seçilince LoadLevelSignal
    /// tetikler ve GameState.Playing'e geçer. Geri butonu Hub'a döner.
    /// </summary>
    public class LevelSelectMediator : Mediator<LevelSelectView>
    {
        [Inject] public IGameStateModel GameStateModel { get; set; }
        [Inject] public IProgressModel ProgressModel { get; set; }
        [Inject] public ILevelProgressionService ProgressionService { get; set; }
        [Inject] public ILoggerService LoggerService { get; set; }

        // Kilitli seviyelerden kaç tanesinin önden gösterileceği ve minimum ızgara boyutu.
        private const int LockedPreviewCount = 4;
        private const int MinLevelsShown = 12;

        protected override void OnBind()
        {
            if (View == null) return;

            View.OnBackClicked += HandleBackClicked;
            View.OnLevelSelected += HandleLevelSelected;

            if (GameStateModel != null)
                GameStateModel.OnStateChanged += HandleStateChanged;

            UpdateVisibility();
        }

        protected override void OnUnbind()
        {
            if (View != null)
            {
                View.OnBackClicked -= HandleBackClicked;
                View.OnLevelSelected -= HandleLevelSelected;
            }

            if (GameStateModel != null)
                GameStateModel.OnStateChanged -= HandleStateChanged;
        }

        private void HandleStateChanged(GameState state)
        {
            UpdateVisibility();
            if (state == GameState.LevelSelect)
                RefreshLevels();
        }

        private void UpdateVisibility()
        {
            if (GameStateModel == null || View == null) return;
            bool isLevelSelect = GameStateModel.CurrentState == GameState.LevelSelect;
            View.SetVisible(isLevelSelect);
            if (isLevelSelect)
                RefreshLevels();
        }

        private void RefreshLevels()
        {
            if (View == null) return;

            int unlocked = ProgressModel != null ? ProgressModel.UnlockedLevels : 1;
            if (unlocked < 1) unlocked = 1;

            // Açılan seviyeler + birkaç kilitli önizleme; düzgün ızgara için 4'ün katına yuvarla.
            int total = Mathf.Max(MinLevelsShown, unlocked + LockedPreviewCount);
            int remainder = total % 4;
            if (remainder != 0) total += (4 - remainder);

            var levels = new List<LevelButtonInfo>(total);
            for (int i = 0; i < total; i++)
            {
                levels.Add(new LevelButtonInfo
                {
                    LevelIndex = i,
                    DisplayNumber = i + 1,
                    Unlocked = i < unlocked,
                    Stars = ProgressModel != null ? ProgressModel.GetStars(i) : 0
                });
            }

            View.PopulateLevels(levels);
            LoggerService?.Log($"[PixelFlow.LevelSelectMediator] Populated {total} levels (unlocked={unlocked}).");
        }

        private void HandleBackClicked()
        {
            LoggerService?.Log("[PixelFlow.LevelSelectMediator] Back clicked -> MainMenu.");
            GameStateModel?.SetState(GameState.MainMenu);
        }

        private void HandleLevelSelected(int levelIndex)
        {
            var level = ProgressionService?.GetOrGenerateLevel(levelIndex);
            if (level == null)
            {
                LoggerService?.LogError($"[PixelFlow.LevelSelectMediator] Level index {levelIndex} çözümlenemedi.");
                return;
            }

            LoggerService?.Log($"[PixelFlow.LevelSelectMediator] Level {levelIndex + 1} ({level.name}) selected. Firing LoadLevelSignal -> Playing.");
            SignalBus.Fire(new LoadLevelSignal { LevelToLoad = level });
            GameStateModel?.SetState(GameState.Playing);
        }
    }
}
