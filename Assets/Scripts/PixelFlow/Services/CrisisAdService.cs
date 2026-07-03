using System.Threading;
using System.Threading.Tasks;
using Nexus.Core;
using PixelFlow.Models;
using PixelFlow.Signals;
using UnityEngine;

namespace PixelFlow.Services
{
    public interface ICrisisAdService
    {
        void RecordCrisisAttempt();
        int RetryCount { get; }
        void ResetRetryCount();
        bool IsViaductExhausted { get; }
    }

    /// <summary>
    /// GDD §2.4: 3 başarısız kriz denemesi sonrası interstitial reklam
    /// tetiklenir; ilk 5 seviyede asla gösterilmez.
    /// GDD §6.1: Viyadük hakkı bittiğinde UI "Acil Durum Viyadüğü" reklamı sunar.
    /// Bu sınıf sadece event'leri ateşler; gerçek SDK entegrasyonu adapter
    /// tarafından yapılacak.
    /// </summary>
    public class CrisisAdService : ICrisisAdService, INexusService
    {
        [Inject] public IGameSessionModel GameSessionModel { get; set; }
        [Inject] public ISignalBus SignalBus { get; set; }
        [Inject] public ILevelModel LevelModel { get; set; }

        private const int MaxRetriesBeforeInterstitial = 3;
        private const int MinLevelForInterstitial = 5; // 1-indexed: ilk 5 seviyede yok

        public int RetryCount => GameSessionModel?.RetryCount ?? 0;
        public bool IsViaductExhausted => GameSessionModel != null && GameSessionModel.AvailableViaducts <= 0;

        public ValueTask InitializeAsync(CancellationToken ct) => default;
        public void OnDispose() { }

        public void RecordCrisisAttempt()
        {
            if (GameSessionModel == null) return;
            GameSessionModel.IncrementRetryCount();

            int level = LevelModel?.CurrentLevel?.levelIndex ?? 0;
            if (level + 1 < MinLevelForInterstitial) return;
            if (RetryCount > 0 && RetryCount % MaxRetriesBeforeInterstitial == 0)
            {
                SignalBus?.Fire(new RequestInterstitialAdSignal());
                SignalBus?.Fire(new CrisisRetryExhaustedSignal { RetryCount = RetryCount });
            }

            if (IsViaductExhausted)
            {
                SignalBus?.Fire(new ViaductExhaustedSignal());
            }
        }

        public void ResetRetryCount()
        {
            if (GameSessionModel == null) return;
            for (int i = 0; i < GameSessionModel.RetryCount; i++)
            {
                GameSessionModel.IncrementRetryCount();
            }
        }
    }
}
