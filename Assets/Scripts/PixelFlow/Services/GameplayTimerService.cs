using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Nexus.Core;
using Nexus.Core.Services;
using PixelFlow.Data;
using PixelFlow.Models;
using PixelFlow.Signals;
using PixelFlow.Services;

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
        [Inject] public ILoggerService LoggerService { get; set; }
        [Inject] public Data.GameConfig Config { get; set; }

        private float _idleTimer;
        private int _graceSkipCount;
        private SimulationUpdater _updater;

        private float ConfigIdleReminderSeconds => Config != null ? Config.IdleReminderSeconds : 300f;
        private int ConfigMaxGraceSkips => Config != null ? Config.MaxGraceSkips : 3;

        public bool CanGraceSkip => _graceSkipCount < ConfigMaxGraceSkips;

        public ValueTask InitializeAsync(CancellationToken ct)
        {
            if (Application.isPlaying)
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
            if (_idleTimer >= ConfigIdleReminderSeconds)
            {
                _idleTimer = 0f;
                LoggerService?.Log("[GameplayTimer] Mola vermek ister misiniz?");
            }
        }
    }
}
