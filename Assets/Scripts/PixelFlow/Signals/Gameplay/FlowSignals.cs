using UnityEngine;

namespace PixelFlow.Signals
{
    /// <summary>
    /// Fired by GameBootstrapper after splash screen completes and initial level is loaded.
    /// Puzzle Mediators (GridMediator, HUDMediator) react to this to begin gameplay.
    /// </summary>
    public struct LoadedInitialLevelSignal { }

    public struct RequestRewardedAdSignal
    {
        public RewardedAdType Type;
    }

    public enum RewardedAdType
    {
        Overclock,
        EmergencyViaduct,
        OfflineTriple,
        ExtraHint,
    }

    public struct RequestInterstitialAdSignal { }

    public struct ViaductExhaustedSignal { }

    public struct CrisisRetryExhaustedSignal
    {
        public int RetryCount;
    }

    public struct CoinCollectionSignal
    {
        public int Amount;
        public Vector3 Origin;
    }

    public struct FlowScoreUpdatedSignal
    {
        public int CurrentScore;
        public int TargetScore;
    }
}
