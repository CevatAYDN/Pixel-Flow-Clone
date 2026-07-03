using Nexus.Core;
using PixelFlow.Models;
using PixelFlow.Signals;
using UnityEngine;

namespace PixelFlow.Commands
{
    /// <summary>
    /// RequestReturnToHubSignal'i işler: state'i MainMenu'ye alır, ardından
    /// EnterHubSignal ateşler. CameraController ve Hub UI'ları bu sinyale tepki verir.
    /// </summary>
    public class ReturnToHubCommand : ICommand<RequestReturnToHubSignal>, IResettable
    {
        [Inject] public IGameStateModel GameStateModel { get; set; }
        [Inject] public ISignalBus SignalBus { get; set; }

        public void Execute(RequestReturnToHubSignal signal)
        {
            // Önce Playing/Simulating'den çıkıp MainMenu'ye geçiyoruz.
            // Bu, CameraController'ın OnStateChanged → TransitionToHub'ı tetikler.
            GameStateModel.SetState(GameState.MainMenu);

            // Hub'a özgü UI sinyallerini ateşle (CityHubView, HubHUDView dinler).
            SignalBus.Fire(new EnterHubSignal());
        }

        public void Reset() { }
    }

    /// <summary>
    /// RequestRewardedAdSignal için komut. Ad SDK'sı bağlanmadığı için
    /// sadece adım-1 davranışı sergilenir: mevcut state'e göre ödülü uygula.
    /// Gerçek SDK entegrasyonunda burası AdMob callback'lerine bağlanacak.
    /// </summary>
    public class RewardedAdCommand : ICommand<RequestRewardedAdSignal>, IResettable
    {
        [Inject] public IGameSessionModel GameSessionModel { get; set; }
        [Inject] public ICityEconomyModel CityEconomyModel { get; set; }
        [Inject] public ISignalBus SignalBus { get; set; }

        public void Execute(RequestRewardedAdSignal signal)
        {
            // Placeholder: gerçek reklam SDK'sı bağlanana kadar ödülü doğrudan ver.
            // Production'da bu metodun başında SDK.showRewardedAd() çağrısı olur.
            switch (signal.Type)
            {
                case RewardedAdType.Overclock:
                    // 4 saat ×2 vergi hızı — CityEconomyModel'e overclock bonus'u ekle.
                    Debug.Log("[RewardedAdCommand] Overclock ad rewarded (placeholder).");
                    break;
                case RewardedAdType.EmergencyViaduct:
                    // +1 viyadük hakkı — maxViaducts'ı geçici olarak 1 arttır.
                    GameSessionModel.AddBonusViaduct(1);
                    Debug.Log("[RewardedAdCommand] Emergency viaduct +1 granted.");
                    break;
                case RewardedAdType.OfflineTriple:
                    Debug.Log("[RewardedAdCommand] Offline x3 ad rewarded (placeholder).");
                    break;
                case RewardedAdType.ExtraHint:
                    // IHintModel'a ek hint ekle (3 adede kadar).
                    Debug.Log("[RewardedAdCommand] Extra hint rewarded (placeholder).");
                    break;
            }
        }

        public void Reset() { }
    }
}
