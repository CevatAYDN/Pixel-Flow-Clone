using Nexus.Core;
using PixelFlow.Data;
using PixelFlow.Models;
using PixelFlow.Signals;

namespace PixelFlow.Views
{
    public class LevelPackMediator : Mediator<LevelPackView>
    {
        [Inject] public IProgressModel ProgressModel { get; set; }
        [Inject] public LevelPack CurrentPack { get; set; }

        protected override void OnBind()
        {
            int unlockedCount = ProgressModel.UnlockedLevels;
            View.PopulateButtons(unlockedCount, OnLevelSelected);
        }

        private void OnLevelSelected(int levelIndex)
        {
            if (CurrentPack != null && levelIndex < CurrentPack.levels.Count)
            {
                var levelData = CurrentPack.levels[levelIndex];
                SignalBus.Fire(new LoadLevelSignal { LevelToLoad = levelData });
            }
        }
    }
}
