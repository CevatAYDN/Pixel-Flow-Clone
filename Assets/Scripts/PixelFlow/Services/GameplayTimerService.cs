using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Nexus.Core;
using PixelFlow.Models;
using PixelFlow.Signals;

namespace PixelFlow.Services
{
    public interface IGameplayTimerService
    {
        void ResetIdleTimer();
        void RequestGraceSkip();
        bool CanGraceSkip { get; }
    }

    public class GameplayTimerService : IGameplayTimerService, INexusService
    {
        [Inject] public IGameStateModel GameStateModel { get; set; }
        [Inject] public ISignalBus SignalBus { get; set; }

        private float _idleTimer;
        private int _graceSkipCount;
        private const float IdleReminderSeconds = 300f;
        private const int MaxGraceSkips = 3;
        private SimulationUpdater _updater;

        public bool CanGraceSkip => _graceSkipCount < MaxGraceSkips;

        public ValueTask InitializeAsync(CancellationToken ct)
        {
#if UNITY_EDITOR
            if (!UnityEditor.EditorApplication.isPlaying)
            {
                // EditMode test: SimulationUpdater GameObject'i yaratma.
            }
            else
#endif
            {
                GameObject updaterObj = new GameObject("[GameplayTimerUpdater]");
                updaterObj.hideFlags = HideFlags.DontSave;
                _updater = updaterObj.AddComponent<SimulationUpdater>();
                _updater.OnUpdate = Update;
            }
            return default;
        }

        public void OnDispose()
        {
            if (_updater != null)
                Object.Destroy(_updater.gameObject);
        }

        public void ResetIdleTimer()
        {
            _idleTimer = 0f;
        }

        public void RequestGraceSkip()
        {
            if (!CanGraceSkip) return;
            if (GameStateModel.CurrentState != GameState.Playing) return;

            _graceSkipCount++;
            _idleTimer = 0f;

            GameStateModel.SetState(GameState.LevelCompleted);
            SignalBus.Fire(new LevelCompletedSignal());
        }

        public void Update()
        {
            if (GameStateModel.CurrentState != GameState.Playing) return;

            _idleTimer += Time.deltaTime;
            if (_idleTimer >= IdleReminderSeconds)
            {
                _idleTimer = 0f;
                Debug.Log("[GameplayTimer] Mola vermek ister misiniz?");
            }
        }
    }
}
