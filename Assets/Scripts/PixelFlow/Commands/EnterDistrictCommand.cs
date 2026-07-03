using System;
using Nexus.Core;
using PixelFlow.Models;
using PixelFlow.Services;
using PixelFlow.Signals;
using UnityEngine;

namespace PixelFlow.Commands
{
    public class EnterDistrictCommand : ICommand<EnterDistrictSignal>, IResettable
    {
        [Inject] public ILevelProgressionService ProgressionService { get; set; }
        [Inject] public IProgressModel ProgressModel { get; set; }
        [Inject] public ILevelModel LevelModel { get; set; }
        [Inject] public ISignalBus SignalBus { get; set; }
        [Inject] public ITutorialDriver TutorialDriver { get; set; }

        public void Execute(EnterDistrictSignal signal)
        {
            int levelIndex = DistrictToLevelIndex(signal.DistrictIndex);
            if (levelIndex < 0) return;

            var level = ProgressionService.GetOrGenerateLevel(levelIndex);
            if (level == null) return;

            LevelModel.SetLevel(level);
            TutorialDriver?.OnLevelLoaded(levelIndex);
            SignalBus.Fire(new LoadLevelSignal { LevelToLoad = level });
        }

        public static int DistrictToLevelIndex(int district)
        {
            switch (district)
            {
                case 0: return 0;
                case 1: return 9;
                case 2: return 19;
                case 3: return 29;
                case 4: return 41;
                case 5: return 54;
                default: return -1;
            }
        }

        public void Reset() { }
    }
}
