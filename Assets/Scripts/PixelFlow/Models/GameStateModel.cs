using System;

namespace PixelFlow.Models
{
    public enum GameState { MainMenu, Playing, Paused, LevelCompleted }

    public interface IGameStateModel
    {
        GameState CurrentState { get; }
        event Action<GameState> OnStateChanged;
        void SetState(GameState state);
    }

    public class GameStateModel : IGameStateModel
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
    }
}
