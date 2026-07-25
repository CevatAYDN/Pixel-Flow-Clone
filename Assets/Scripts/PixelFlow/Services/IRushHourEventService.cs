using System;
using System.Threading;
using System.Threading.Tasks;
using Nexus.Core;

namespace PixelFlow.Services
{
    public interface IRushHourEventService : INexusService
    {
        bool IsEventActive { get; }
        TimeSpan TimeRemaining { get; }
        float CoinMultiplier { get; }
        event Action<bool> OnEventStateChanged;
        void TriggerEvent(TimeSpan duration);
        void EndEvent();
        void Update(float deltaTime);
    }
}