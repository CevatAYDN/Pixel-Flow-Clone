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

    public class GameplayTimerService : IGameplayTimerService, ITickable, INexusService
    {
        [Inject] public IGameStateModel GameStateModel { get; set; }
        [Inject] public ISignalBus SignalBus { get; set; }
        [Inject] public ILoggerService LoggerService { get; set; }
        [Inject, OptionalInject] public Data.GameConfig Config { get; set; }
        [Inject, OptionalInject] public ITickService TickService { get; set; }

        private float _idleTimer;
        private int _graceSkipCount;

        private float ConfigIdleReminderSeconds => Config != null ? Config.IdleReminderSeconds : throw new Data.DataValidationException("GameConfig.IdleReminderSeconds erişilemedi!");
        private int ConfigMaxGraceSkips => Config != null ? Config.MaxGraceSkips : throw new Data.DataValidationException("GameConfig.MaxGraceSkips erişilemedi!");

        public bool CanGraceSkip => _graceSkipCount < ConfigMaxGraceSkips;

        public ValueTask InitializeAsync(CancellationToken ct)
        {
            if (Application.isPlaying)
            {
                TickService?.RegisterTickable(this);
            }
            return default;
        }

        public void OnDispose()
        {
            TickService?.UnregisterTickable(this);
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

            // HATA FIX: Win condition validasyonu için direkt state değiştirme
            // CheckWinConditionSignal fırlat — Command içinde skor/star hesaplaması yapılır
            SignalBus?.Fire(new CheckWinConditionSignal());
        }

        public void Tick(float deltaTime)
        {
            if (GameStateModel.CurrentState != GameState.Playing) return;

            _idleTimer += deltaTime;
            if (_idleTimer >= ConfigIdleReminderSeconds)
            {
                _idleTimer = 0f;
                LoggerService?.Log("[GameplayTimer] Mola vermek ister misiniz?");
            }
        }
    }
}
