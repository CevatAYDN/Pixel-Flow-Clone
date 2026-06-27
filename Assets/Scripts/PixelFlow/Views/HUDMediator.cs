using Nexus.Core;
using PixelFlow.Commands;
using PixelFlow.Models;
using PixelFlow.Signals;
using PixelFlow.Data;
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

        // Level pack opsiyonel: inspector'dan atanabilir veya Resources'tan yüklenebilir.
        // Hiçbiri yoksa "next level" sessizce yok sayılır.
        [SerializeField] private LevelPack _fallbackLevelPack;

        // Closure'lar field olarak saklanır; OnBind'de eklenip OnUnbind'de
        // aynı instance çıkarılır. Aksi halde her bind'da yeni closure oluşur
        // ve -= eski referansı bulamaz → event sızıntısı.
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
            View.OnThemeDarkClicked += _themeDarkHandler;
            View.OnThemeLightClicked += _themeLightHandler;
            View.OnThemeNeonClicked += _themeNeonHandler;

            HintModel.OnHintCountChanged += HandleHintCountChanged;
            View.HideCompletion();
            View.UpdateHintCount(HintModel.HintsRemaining);
            View.HighlightActiveTheme(SettingsModel.CurrentTheme);

            Subscribe<LevelCompletedSignal>(HandleLevelCompleted);
            Subscribe<LoadLevelSignal>(HandleLoadLevel);
            Subscribe<ThemeChangedSignal>(HandleThemeChanged);
        }

        protected override void OnUnbind()
        {
            View.OnHintClicked -= HandleHintClicked;
            View.OnNextLevelClicked -= HandleNextLevelClicked;

            if (_themeDarkHandler != null) View.OnThemeDarkClicked -= _themeDarkHandler;
            if (_themeLightHandler != null) View.OnThemeLightClicked -= _themeLightHandler;
            if (_themeNeonHandler != null) View.OnThemeNeonClicked -= _themeNeonHandler;
            _themeDarkHandler = null;
            _themeLightHandler = null;
            _themeNeonHandler = null;

            HintModel.OnHintCountChanged -= HandleHintCountChanged;
            // Subscribe<T> ile alınanlar Mediator.Unbind'de otomatik dispose edilir.
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

        private void HandleNextLevelClicked()
        {
            // Yalnızca oyun tamamlandıktan sonra next level mantıklı.
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
            // Bulunamadıysa (-1) ilk level'a dön; son level'daysa başa sar.
            int nextIndex = currentIndex < 0 ? 0 : (currentIndex + 1) % pack.levels.Count;
            SignalBus.Fire(new LoadLevelSignal { LevelToLoad = pack.levels[nextIndex] });
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

        private void HandleLevelCompleted(LevelCompletedSignal signal)
        {
            View.ShowCompletion();
        }

        private void HandleThemeChanged(ThemeChangedSignal signal)
        {
            View.HighlightActiveTheme(SettingsModel.CurrentTheme);
        }
    }
}