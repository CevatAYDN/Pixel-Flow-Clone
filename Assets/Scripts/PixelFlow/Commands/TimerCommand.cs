using Nexus.Core;
using PixelFlow.Models;
using PixelFlow.Signals;
using UnityEngine;

namespace PixelFlow.Commands
{
    public class TimerCommand : ICommand<TimerTickSignal>, IResettable
    {
        [Inject] public IGameSessionModel GameSessionModel { get; set; }
        [Inject] public IGameStateModel GameStateModel { get; set; }

        public void Execute(TimerTickSignal signal)
        {
            if (GameStateModel.CurrentState != GameState.Playing) return;
            GameSessionModel.UpdateTime(Time.deltaTime);
        }

        public void Reset() { }
    }
}
