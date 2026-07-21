using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Nexus.Core;
using UnityEngine;

namespace PixelFlow.Models
{
    public enum GameState { Boot, Loading, MainMenu, Playing, Simulating, Paused, LevelCompleted, LevelFailed }

    public interface IGameStateModel
    {
        GameState CurrentState { get; }
        GameState PreviousState { get; }
        event Action<GameState> OnStateChanged;
        void SetState(GameState state);
    }

    public class GameStateModel : IGameStateModel, IReactiveModel
    {
        public GameState CurrentState { get; private set; }
        public GameState PreviousState { get; private set; }
        public event Action<GameState> OnStateChanged;

        private static readonly HashSet<(GameState from, GameState to)> AllowedTransitions = new HashSet<(GameState, GameState)>
        {
            // Same-state (no-op)
            (GameState.Boot, GameState.Boot),
            (GameState.Loading, GameState.Loading),
            (GameState.MainMenu, GameState.MainMenu),
            (GameState.Playing, GameState.Playing),
            (GameState.Simulating, GameState.Simulating),
            (GameState.Paused, GameState.Paused),
            (GameState.LevelCompleted, GameState.LevelCompleted),
            (GameState.LevelFailed, GameState.LevelFailed),
            // Boot → Loading → MainMenu
            (GameState.Boot, GameState.Loading),
            (GameState.Loading, GameState.MainMenu),
            // Direct Boot → Playing (saved game restore) and Boot → MainMenu (fresh start)
            (GameState.Boot, GameState.MainMenu),
            (GameState.Boot, GameState.Playing),
            // Hub → Gameplay
            (GameState.MainMenu, GameState.Playing),
            // Playing ↔ Paused
            (GameState.Playing, GameState.Paused),
            (GameState.Paused, GameState.Playing),
            // Hub exit / escape transitions
            (GameState.Playing, GameState.MainMenu),
            (GameState.Simulating, GameState.MainMenu),
            (GameState.Paused, GameState.MainMenu),
            // Playing → Simulating
            (GameState.Playing, GameState.Simulating),
            // Playing → LevelCompleted (grace skip, debug shortcuts)
            (GameState.Playing, GameState.LevelCompleted),
            // Simulating transitions
            (GameState.Simulating, GameState.Playing),
            (GameState.Simulating, GameState.Paused),
            (GameState.Simulating, GameState.LevelCompleted),
            // Paused → Simulating (crisis viaduct resolution restores sim)
            (GameState.Paused, GameState.Simulating),
            // LevelCompleted → Hub or next level
            (GameState.LevelCompleted, GameState.MainMenu),
            (GameState.LevelCompleted, GameState.Playing),
            // LevelFailed transitions (GDD §2.4)
            (GameState.Playing, GameState.LevelFailed),
            (GameState.Paused, GameState.LevelFailed),
            (GameState.Simulating, GameState.LevelFailed),
            (GameState.LevelFailed, GameState.MainMenu),
            (GameState.LevelFailed, GameState.Playing),
        };

        public void SetState(GameState state)
        {
            if (CurrentState == state) return;

            if (!AllowedTransitions.Contains((CurrentState, state)))
            {
                Debug.LogError($"[GameStateModel] Illegal transition: {CurrentState} → {state}. Blocked.");
                return;
            }

            PreviousState = CurrentState;
            CurrentState = state;
            OnStateChanged?.Invoke(CurrentState);
        }

        public ValueTask OnBind(CancellationToken ct) => default;
    }
}
