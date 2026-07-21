namespace PixelFlow.Signals
{
    /// <summary>GDD §2.4: 3 ardışık kaza denemesi sonrası level başarısız.</summary>
    public enum FailReason { CrashLimitReached, Timeout, Quit }

    /// <summary>GDD §2.4: Level başarısız olduğunda ateşlenir.</summary>
    public struct LevelFailedSignal
    {
        public FailReason Reason;
        public int RetryCount;
    }
}
