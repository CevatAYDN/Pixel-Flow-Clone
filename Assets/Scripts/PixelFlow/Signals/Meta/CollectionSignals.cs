using PixelFlow.Data;

namespace PixelFlow.Signals
{
    public struct CoinsEarnedSignal
    {
        public readonly int Amount;
        public readonly string Source;

        public CoinsEarnedSignal(int amount, string source = "gameplay")
        {
            Amount = amount;
            Source = source;
        }
    }

    public struct SkinUnlockedSignal
    {
        public string SkinId;
        public bool IsPurchase;

        public SkinUnlockedSignal(string skinId, bool isPurchase = false)
        {
            SkinId = skinId;
            IsPurchase = isPurchase;
        }
    }

    public struct EquipSkinSignal
    {
        public readonly ColorType ColorFamily;
        public readonly string SkinId;

        public EquipSkinSignal(ColorType colorFamily, string skinId)
        {
            ColorFamily = colorFamily;
            SkinId = skinId;
        }
    }
}
