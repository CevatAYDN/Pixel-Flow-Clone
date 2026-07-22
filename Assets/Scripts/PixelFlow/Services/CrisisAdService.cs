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
    ///
    /// RecordCrisisAttempt: sadece retry/interstitial logic'i.
    /// Viaduct exhaustion ayrı bir çağrı ile (CheckViaductExhaustion) yönetilir.
    /// </summary>
    public class CrisisAdService : ICrisisAdService, INexusService
    {
        [Inject] public IGameSessionModel GameSessionModel { get; set; }
        [Inject] public ISignalBus SignalBus { get; set; }
        [Inject] public ILevelModel LevelModel { get; set; }
        [Inject, OptionalInject] public Data.GameConfig Config { get; set; }

        private int ConfigMaxRetries => Config != null ? Config.MaxRetriesBeforeInterstitial : 3;
        private int ConfigMinLevel => Config != null ? Config.MinLevelForInterstitial : 5;

        public int RetryCount => GameSessionModel?.RetryCount ?? 0;
        public bool IsViaductExhausted => GameSessionModel != null && GameSessionModel.AvailableViaducts <= 0;

        public ValueTask InitializeAsync(CancellationToken ct) => default;
        public void OnDispose() { }

        /// <summary>
        /// Kriz denemesini kaydeder. 3 denemede:
        /// 1. Interstitial reklam tetikler
        /// 2. CrisisRetryExhaustedSignal fırlatır
        /// 3. GDD §2.4: LevelFailedSignal fırlatır (3 başarısız deneme = level failed)
        /// Viaduct exhaustion kontrolü içermez — CheckViaductExhaustion() ayrı çağrılır.
        /// </summary>
        public void RecordCrisisAttempt()
        {
            if (GameSessionModel == null) return;
            GameSessionModel.IncrementRetryCount();

            int level = LevelModel?.CurrentLevel?.levelIndex ?? 0;
            if (level + 1 < ConfigMinLevel) return;
            if (RetryCount > 0 && RetryCount % ConfigMaxRetries == 0)
            {
                SignalBus?.Fire(new RequestInterstitialAdSignal());
                SignalBus?.Fire(new CrisisRetryExhaustedSignal { RetryCount = RetryCount });

                // GDD §2.4: 3 ardışık kaza denemesi → LevelFailedSignal
                SignalBus?.Fire(new LevelFailedSignal
                {
                    Reason = FailReason.CrashLimitReached,
                    RetryCount = RetryCount
                });
            }
        }

        /// <summary>
        /// Viyadük hakkı bittiğinde ayrı olarak çağrılır.
        /// RecordCrisisAttempt'tan ayrıştırıldı — H5 fix.
        /// </summary>
        public void CheckViaductExhaustion()
        {
            if (IsViaductExhausted)
            {
                SignalBus?.Fire(new ViaductExhaustedSignal());
            }
        }

        public void ResetRetryCount()
        {
            if (GameSessionModel == null) return;
            GameSessionModel.ResetRetryCount();
        }
    }
}
