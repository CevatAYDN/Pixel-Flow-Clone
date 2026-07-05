using UnityEngine;

namespace PixelFlow.Signals
{
    public struct EnterHubSignal { }

    public struct RequestReturnToHubSignal { }

    public struct ReturnToPuzzleSignal { }

    public struct EnterDistrictSignal
    {
        public int DistrictIndex;
    }

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
