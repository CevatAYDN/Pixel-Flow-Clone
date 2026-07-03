using System;
using System.Threading;
using System.Threading.Tasks;
using Nexus.Core;

namespace PixelFlow.Models
{
    public enum TutorialStep
    {
        None = 0,
        TouchAndDrag = 1,
        ColorMatch = 2,
        VehicleFlow = 3,
        LevelComplete = 4,
        ReturnToHub = 5,
        TaxCollect = 6,
        SecondColor = 9,
        CrashIntro = 13,
        ViaductIntro = 14,
        UndoIntro = 18,
        ObstacleIntro = 29,
        OneWayIntro = 35
    }

    public interface ITutorialModel
    {
        TutorialStep CurrentStep { get; }
        bool IsActive { get; }
        event Action<TutorialStep> OnStepStarted;
        event Action<TutorialStep> OnStepCompleted;
        bool IsCompleted(TutorialStep step);
        void StartStep(TutorialStep step);
        void CompleteStep(TutorialStep step);
    }

    public class TutorialModel : ITutorialModel, IReactiveModel
    {
        public TutorialStep CurrentStep { get; private set; }
        public bool IsActive { get; private set; }
        public event Action<TutorialStep> OnStepStarted;
        public event Action<TutorialStep> OnStepCompleted;

        public void StartStep(TutorialStep step)
        {
            CurrentStep = step;
            IsActive = true;
            OnStepStarted?.Invoke(step);
        }

        public void CompleteStep(TutorialStep step)
        {
            if (CurrentStep == step)
            {
                IsActive = false;
                CurrentStep = TutorialStep.None;
                OnStepCompleted?.Invoke(step);
            }
        }

        public bool IsCompleted(TutorialStep step) => !IsActive && CurrentStep == TutorialStep.None;

        public ValueTask OnBind(CancellationToken ct) => default;
    }
}
