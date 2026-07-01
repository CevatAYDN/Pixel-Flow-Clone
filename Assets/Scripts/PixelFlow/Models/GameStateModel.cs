using System;
using System.Threading;
using System.Threading.Tasks;
using Nexus.Core;

namespace PixelFlow.Models
{
    public enum GameState { MainMenu, Playing, Paused, LevelCompleted }

    public interface IGameStateModel
    {
        GameState CurrentState { get; }
        event Action<GameState> OnStateChanged;
        void SetState(GameState state);
    }

    public class GameStateModel : IGameStateModel, IReactiveModel
    {
        public GameState CurrentState { get; private set; }
        public event Action<GameState> OnStateChanged;

        public void SetState(GameState state)
        {
            if (CurrentState != state)
            {
                CurrentState = state;
                OnStateChanged?.Invoke(CurrentState);
            }
        }

        public ValueTask OnBind(CancellationToken ct) => default;
    }
}
