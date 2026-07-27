using System;
using System.Threading;
using System.Threading.Tasks;
using Nexus.Core;
using Nexus.Core.Services;
using PixelFlow.Models;
using PixelFlow.Signals;
using PixelFlow.Data;
using UnityEngine;

namespace PixelFlow.Services
{
    /// <summary>
    /// Rush Hour Event servisi - game_plan.md §6 ve §15.5'e göre.
    /// 24 saatlik Çift Para etkinliği.
    /// </summary>
    public class RushHourEventService : IRushHourEventService, INexusService
    {
        [Inject] public IEconomyService EconomyService { get; set; }
        [Inject] public ILoggerService LoggerService { get; set; }
        [Inject] public IPlayerPrefsService PlayerPrefsService { get; set; }
        [Inject] public ISignalBus SignalBus { get; set; }
        [Inject] public RushHourConfigAsset RushHourConfig { get; set; }
        [Inject, OptionalInject] public GameConfig Config { get; set; }
        [Inject, OptionalInject] public PixelFlow.Data.StorageKeysConfigAsset Keys { get; set; }

        private string EventActiveKey => Keys?.KeyRushHour_Active;
        private string EventEndTimeKey => Keys?.KeyRushHour_EndTime;
        private string EventCooldownKey => Keys?.KeyRushHour_Cooldown;

        private bool _isEventActive = false;
        private DateTime _eventEndTime;

        public bool IsEventActive => _isEventActive && DateTime.UtcNow < _eventEndTime;
        public TimeSpan TimeRemaining => IsEventActive ? _eventEndTime - DateTime.UtcNow : TimeSpan.Zero;
        public float CoinMultiplier => IsEventActive ? RushHourConfig?.CoinMultiplier ?? 2.0f : 1.0f;

        public event Action<bool> OnEventStateChanged;

        public ValueTask InitializeAsync(CancellationToken ct)
        {
            if (RushHourConfig == null)
                throw new DataValidationException("RushHourEventService: RushHourConfigAsset not injected. Must be bound in GameContextLifecycle.");

            LoadEventState();
            CheckAndTriggerEvent();
            return default;
        }

        public void OnDispose() { }

        private void LoadEventState()
        {
            if (PlayerPrefsService == null) return;
            if (string.IsNullOrEmpty(EventActiveKey) || string.IsNullOrEmpty(EventEndTimeKey) || string.IsNullOrEmpty(EventCooldownKey)) throw new DataValidationException("RushHourEventService requires configured storage keys.");

            bool wasActive = PlayerPrefsService.GetBool(EventActiveKey, false);
            if (wasActive)
            {
                string endTimeStr = PlayerPrefsService.GetString(EventEndTimeKey, "");
                if (DateTime.TryParse(endTimeStr, out DateTime endTime))
                {
                    if (DateTime.UtcNow < endTime)
                    {
                        _isEventActive = true;
                        _eventEndTime = endTime;
                        LoggerService?.Log($"[PixelFlow.RushHourEventService] Loaded active event ending at {endTime}");
                    }
                    else
                    {
                        // Event expired
                        PlayerPrefsService.SetBool(EventActiveKey, false);
                        PlayerPrefsService.Save();
                    }
                }
            }
        }

        private void CheckAndTriggerEvent()
        {
            if (PlayerPrefsService == null) return;
            if (string.IsNullOrEmpty(EventActiveKey) || string.IsNullOrEmpty(EventEndTimeKey) || string.IsNullOrEmpty(EventCooldownKey)) throw new DataValidationException("RushHourEventService requires configured storage keys.");

            // Check cooldown
            string cooldownStr = PlayerPrefsService.GetString(EventCooldownKey, "");
            DateTime cooldownEnd;
            if (DateTime.TryParse(cooldownStr, out cooldownEnd))
            {
                if (DateTime.UtcNow < cooldownEnd)
                {
                    LoggerService?.Log($"[PixelFlow.RushHourEventService] Event on cooldown until {cooldownEnd}");
                    return;
                }
            }

            // Check if should trigger (e.g., every 48 hours, or based on RemoteConfig)
            // For now, trigger if no event in last 48 hours
            if (IsEventActive) return;

            // Check minimum level requirement
            if (RushHourConfig.MinLevel > 0)
            {
                LoggerService?.Log($"[PixelFlow.RushHourEventService] Rush Hour MinLevel requirement: {RushHourConfig.MinLevel}");
            }

            // Check time since last session
            string lastLoginStr = PlayerPrefsService.GetString(Keys?.KeyDailyLogin_LastLogin, "");
            if (DateTime.TryParse(lastLoginStr, out DateTime lastLogin))
            {
                TimeSpan sinceLastLogin = DateTime.UtcNow - lastLogin;
                if (sinceLastLogin.TotalHours < RushHourConfig.TriggerAfterHours)
                {
                    LoggerService?.Log($"[PixelFlow.RushHourEventService] Not enough time since last login ({sinceLastLogin.TotalHours}h < {RushHourConfig.TriggerAfterHours}h). Event not triggered.");
                    return;
                }
            }

            // Trigger event with configured duration
            TriggerEvent(TimeSpan.FromSeconds(RushHourConfig.DurationSeconds));
        }

        public void TriggerEvent(TimeSpan duration)
        {
            if (IsEventActive) return;

            _isEventActive = true;
            _eventEndTime = DateTime.UtcNow + duration;

            if (PlayerPrefsService != null)
            {
                PlayerPrefsService.SetBool(EventActiveKey, true);
                PlayerPrefsService.SetString(EventEndTimeKey, _eventEndTime.ToString("O"));
                PlayerPrefsService.Save();
            }

            float multiplier = RushHourConfig?.CoinMultiplier ?? 2.0f;
            LoggerService?.Log($"[PixelFlow.RushHourEventService] 🚀 Rush Hour Event STARTED! Duration: {duration.TotalHours}h, Multiplier: {multiplier}x");
            OnEventStateChanged?.Invoke(true);

            // Fire signal for UI
            SignalBus?.Fire(new RushHourStartedSignal 
            { 
                DurationSeconds = (int)duration.TotalSeconds, 
                Multiplier = multiplier
            });
        }

        public void EndEvent()
        {
            if (!IsEventActive) return;

            _isEventActive = false;

            if (PlayerPrefsService != null)
            {
                PlayerPrefsService.SetBool(EventActiveKey, false);
                PlayerPrefsService.SetString(EventEndTimeKey, "");
                // Set cooldown from config
                int cooldownHours = RushHourConfig?.CooldownHours ?? 48;
                PlayerPrefsService.SetString(EventCooldownKey, DateTime.UtcNow.AddHours(cooldownHours).ToString("O"));
                PlayerPrefsService.Save();
            }

            LoggerService?.Log($"[PixelFlow.RushHourEventService] 🛑 Rush Hour Event ENDED. Next event in {RushHourConfig?.CooldownHours ?? 48}h.");
            OnEventStateChanged?.Invoke(false);

            SignalBus?.Fire(new RushHourEndedSignal());
        }

        public void Update(float deltaTime)
        {
            if (IsEventActive && DateTime.UtcNow >= _eventEndTime)
            {
                EndEvent();
            }
        }
    }
}
