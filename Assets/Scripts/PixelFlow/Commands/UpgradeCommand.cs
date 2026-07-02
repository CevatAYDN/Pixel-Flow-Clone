using Nexus.Core;
using PixelFlow.Models;
using PixelFlow.Signals;
using UnityEngine;

namespace PixelFlow.Commands
{
    /// <summary>
    /// Geliştirme satın alma işlemini yöneten komut.
    /// HubHUDMediator doğrudan Model çağırmak yerine bu komutu tetikler.
    /// </summary>
    public class UpgradeCommand : ICommand<UpgradeSignal>, IResettable
    {
        [Inject] public ICityEconomyModel CityEconomyModel { get; set; }

        public void Execute(UpgradeSignal signal)
        {
            int cost = CityEconomyModel.GetUpgradeCost(signal.Type);

            if (CityEconomyModel.Coins < cost)
            {
                Debug.LogWarning($"[UpgradeCommand] Insufficient coins for {signal.Type}. Need: {cost}, Have: {CityEconomyModel.Coins}");
                return;
            }

            CityEconomyModel.PurchaseUpgrade(signal.Type);
            Debug.Log($"[UpgradeCommand] Purchased upgrade: {signal.Type} for {cost} coins.");
        }

        public void Reset()
        {
        }
    }
}
