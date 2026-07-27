namespace PixelFlow.Signals
{
    public struct StopSkinUnlockedSignal
    {
        public string SkinId;
        public bool IsPurchase;

        public StopSkinUnlockedSignal(string skinId, bool isPurchase = false)
        {
            SkinId = skinId;
            IsPurchase = isPurchase;
        }
    }
}