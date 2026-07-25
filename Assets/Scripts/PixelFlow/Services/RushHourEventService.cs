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
        [Inject, OptionalInject] public GameConfig Config { get; set; }

        private const string EventActiveKey = "NT_RushHour_Active";
        private const string EventEndTimeKey = "NT_RushHour_EndTime";
        private const string EventCooldownKey = "NT_RushHour_Cooldown";

        private bool _isEventActive = false;
        private DateTime _eventEndTime;

        public bool IsEventActive => _isEventActive && DateTime.UtcNow < _eventEndTime;
        public TimeSpan TimeRemaining => IsEventActive ? _eventEndTime - DateTime.UtcNow : TimeSpan.Zero;
        public float CoinMultiplier => IsEventActive ? 2.0f : 1.0f;

        public event Action<bool> OnEventStateChanged;

        public ValueTask InitializeAsync(CancellationToken ct)
        {
            LoadEventState();
            CheckAndTriggerEvent();
            return default;
        }

        public void OnDispose() { }

        private void LoadEventState()
        {
            if (PlayerPrefsService == null) return;

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

            // Trigger event with some probability or based on schedule
            // In production, this would be driven by RemoteConfig or server
            TriggerEvent(TimeSpan.FromHours(24));
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

            LoggerService?.Log($"[PixelFlow.RushHourEventService] 🚀 Rush Hour Event STARTED! Duration: {duration.TotalHours}h, Multiplier: 2x");
            OnEventStateChanged?.Invoke(true);

            // Fire signal for UI
            SignalBus?.Fire(new RushHourStartedSignal 
            { 
                DurationSeconds = (int)duration.TotalSeconds, 
                Multiplier = 2.0f 
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
                // Set cooldown for 48 hours
                PlayerPrefsService.SetString(EventCooldownKey, DateTime.UtcNow.AddHours(48).ToString("O"));
                PlayerPrefsService.Save();
            }

            LoggerService?.Log($"[PixelFlow.RushHourEventService] 🛑 Rush Hour Event ENDED. Next event in 48h.");
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