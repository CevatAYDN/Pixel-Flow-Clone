using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Nexus.Core;
using Nexus.Core.Services;
using PixelFlow.Signals;
using UnityEngine;

namespace PixelFlow.Services
{
    /// <summary>
    /// game_plan.md §13 Analitik Event Map.
    /// Listens to SignalBus and tracks canonical game events via Nexus IAnalyticsService:
    /// - level_start, level_complete, level_fail
    /// - undo_used, skin_unlocked, ad_impression, ad_rewarded, iap_purchase
    /// - session_start, daily_claim, event_join
    /// </summary>
    public class PixelFlowAnalyticsTracker : INexusService
    {
        [Inject] public ISignalBus SignalBus { get; set; }
        [Inject, OptionalInject] public IAnalyticsService AnalyticsService { get; set; }
        [Inject, OptionalInject] public ILoggerService LoggerService { get; set; }

        public ValueTask InitializeAsync(CancellationToken ct)
        {
            if (SignalBus != null)
            {
                SignalBus.Subscribe<LoadLevelSignal>(OnLoadLevel);
                SignalBus.Subscribe<LevelCompletedSignal>(OnLevelCompleted);
                SignalBus.Subscribe<LevelFailedSignal>(OnLevelFailed);
                SignalBus.Subscribe<UndoSignal>(OnUndo);
                SignalBus.Subscribe<SkinUnlockedSignal>(OnSkinUnlocked);
                SignalBus.Subscribe<RushHourStartedSignal>(OnEventJoin);
            }

            TrackEvent("session_start", new Dictionary<string, object> { { "timestamp", System.DateTime.UtcNow.Ticks } });
            return default;
        }

        public void OnDispose() { }

        private void OnLoadLevel(LoadLevelSignal sig)
        {
            int index = sig.LevelToLoad != null ? sig.LevelToLoad.levelIndex : 0;
            TrackEvent("level_start", new Dictionary<string, object> { { "level_index", index } });
        }

        private void OnLevelCompleted(LevelCompletedSignal sig)
        {
            TrackEvent("level_complete", new Dictionary<string, object> { { "timestamp", System.DateTime.UtcNow.Ticks } });
        }

        private void OnLevelFailed(LevelFailedSignal sig)
        {
            TrackEvent("level_fail", new Dictionary<string, object> { { "reason", sig.Reason.ToString() }, { "retry_count", sig.RetryCount } });
        }

        private void OnUndo(UndoSignal sig)
        {
            TrackEvent("undo_used", new Dictionary<string, object> { { "timestamp", System.DateTime.UtcNow.Ticks } });
        }

        private void OnSkinUnlocked(SkinUnlockedSignal sig)
        {
            TrackEvent("skin_unlocked", new Dictionary<string, object> { { "skin_id", sig.SkinId } });
        }

        private void OnEventJoin(RushHourStartedSignal sig)
        {
            TrackEvent("event_join", new Dictionary<string, object> { { "event_name", "RushHour" } });
        }

        public void TrackEvent(string eventName, Dictionary<string, object> parameters = null)
        {
            LoggerService?.Log($"[AnalyticsTracker] Event '{eventName}' tracked.");
            AnalyticsService?.LogEvent(eventName, parameters);
        }
    }
}
