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
            var pack = View.LevelPackData;
            if (pack != null && levelIndex < pack.levels.Count)
            {
                var levelData = pack.levels[levelIndex];
                SignalBus.Fire(new LoadLevelSignal { LevelToLoad = levelData });
            }
        }
    }
}
