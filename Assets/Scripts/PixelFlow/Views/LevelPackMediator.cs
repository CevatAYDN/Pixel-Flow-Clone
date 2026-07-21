using Nexus.Core;
using PixelFlow.Models;
using PixelFlow.Signals;

namespace PixelFlow.Views
{
    public class LevelPackMediator : Mediator<LevelPackView>
    {
        [Inject] public IProgressModel ProgressModel { get; set; }

        protected override void OnBind()
        {
            int unlockedCount = ProgressModel.UnlockedLevels;
            View.PopulateButtons(unlockedCount, OnLevelSelected);
        }

        private void OnLevelSelected(int levelIndex)
        {
            // M7 fix: verify level is actually unlocked before loading
            if (levelIndex > ProgressModel.UnlockedLevels - 1) return;

            var pack = View.LevelPackData;
            if (pack != null && levelIndex < pack.levels.Count)
            {
                var levelData = pack.levels[levelIndex];
                SignalBus.Fire(new LoadLevelSignal { LevelToLoad = levelData });
            }
        }
    }
}
