using System;
using System.Threading;
using System.Threading.Tasks;
using Nexus.Core;
using Nexus.Core.Services;

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

    /// <summary>
    /// GDD §5.4: Tutorial durumunu PlayerPrefs üzerinden kalıcı saklar.
    /// İlk 5 seviyede forced tutorial; oyuncu her seferinde tutorial'ı görür.
    /// Constructor injection ile IPlayerPrefsService alır — test edilebilir.
    /// </summary>
    public class TutorialModel : ITutorialModel, IReactiveModel
    {
        private const string PrefKeyCompletedSteps = "NT_TutorialCompleted";
        private readonly IPlayerPrefsService _prefs;
        private int _completedStepsMask;

        public TutorialStep CurrentStep { get; private set; }
        public bool IsActive { get; private set; }
        public event Action<TutorialStep> OnStepStarted;
        public event Action<TutorialStep> OnStepCompleted;

        public TutorialModel(IPlayerPrefsService prefs)
        {
            _prefs = prefs ?? throw new ArgumentNullException(nameof(prefs));
            _completedStepsMask = _prefs.GetInt(PrefKeyCompletedSteps, 0);
        }

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

                // Mask bit'ini set et ve PlayerPrefs'e kaydet
                int bit = 1 << (int)step;
                _completedStepsMask |= bit;
                _prefs.SetInt(PrefKeyCompletedSteps, _completedStepsMask);

                OnStepCompleted?.Invoke(step);
            }
        }

        public bool IsCompleted(TutorialStep step)
        {
            int bit = 1 << (int)step;
            return (_completedStepsMask & bit) != 0;
        }

        public ValueTask OnBind(CancellationToken ct) => default;
    }
}
