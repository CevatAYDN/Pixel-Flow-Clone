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
        public readonly string SkinId;

        public SkinUnlockedSignal(string skinId)
        {
            SkinId = skinId;
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
