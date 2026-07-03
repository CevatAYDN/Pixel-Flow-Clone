using System;
using System.Threading;
using System.Threading.Tasks;
using Nexus.Core;

namespace PixelFlow.Models
{
    public interface IGameSessionModel
    {
        int Score { get; }
        float ElapsedTime { get; }
        int StarsEarned { get; }
        bool IsSessionActive { get; }
        int AvailableViaducts { get; }
        int MaxViaducts { get; }
        float SimulationTimeRemaining { get; set; }

        event Action<int> OnScoreChanged;
        event Action<float> OnTimeChanged;
        event Action<int> OnStarsChanged;
        event Action<int> OnViaductsChanged;
        event Action<float> OnSimulationTimerChanged;

        void StartSession();
        void StartSession(int maxViaducts);
        void UpdateTime(float deltaTime);
        void AddScore(int points);
        void SetStars(int stars);
        void ResetSession();
        bool TryUseViaduct();
        void RefundViaduct();
        void SetSimulationTimer(float remaining);
    }

    public class GameSessionModel : IGameSessionModel, IReactiveModel
    {
        public int Score { get; private set; }
        public float ElapsedTime { get; private set; }
        public int StarsEarned { get; private set; }
        public bool IsSessionActive { get; private set; }
        public int AvailableViaducts { get; private set; }
        public int MaxViaducts { get; private set; }
        public float SimulationTimeRemaining { get; set; }

        public event Action<int> OnScoreChanged;
        public event Action<float> OnTimeChanged;
        public event Action<int> OnStarsChanged;
        public event Action<int> OnViaductsChanged;
        public event Action<float> OnSimulationTimerChanged;

        public void StartSession()
        {
            StartSession(3);
        }

        public void StartSession(int maxViaducts)
        {
            Score = 0;
            ElapsedTime = 0f;
            StarsEarned = 0;
            MaxViaducts = maxViaducts;
            AvailableViaducts = maxViaducts;
            IsSessionActive = true;
            OnViaductsChanged?.Invoke(AvailableViaducts);
        }

        public void UpdateTime(float deltaTime)
        {
            if (!IsSessionActive) return;
            ElapsedTime += deltaTime;
            OnTimeChanged?.Invoke(ElapsedTime);
        }

        public void AddScore(int points)
        {
            if (!IsSessionActive || points <= 0) return;
            Score += points;
            OnScoreChanged?.Invoke(Score);
        }

        public void SetStars(int stars)
        {
            if (stars == StarsEarned) return;
            StarsEarned = System.Math.Max(0, System.Math.Min(stars, 3));
            OnStarsChanged?.Invoke(StarsEarned);
        }

        public void ResetSession()
        {
            Score = 0;
            ElapsedTime = 0f;
            StarsEarned = 0;
            AvailableViaducts = 0;
            MaxViaducts = 0;
            IsSessionActive = false;
        }

        public bool TryUseViaduct()
        {
            if (AvailableViaducts > 0)
            {
                AvailableViaducts--;
                OnViaductsChanged?.Invoke(AvailableViaducts);
                return true;
            }
            return false;
        }

        public void RefundViaduct()
        {
            if (AvailableViaducts < MaxViaducts)
            {
                AvailableViaducts++;
                OnViaductsChanged?.Invoke(AvailableViaducts);
            }
        }

        public ValueTask OnBind(CancellationToken ct) => default;

        public void SetSimulationTimer(float remaining)
        {
            SimulationTimeRemaining = remaining;
            OnSimulationTimerChanged?.Invoke(remaining);
        }
    }
}
