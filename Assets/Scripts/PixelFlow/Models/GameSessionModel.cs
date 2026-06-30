using System;
using PixelFlow.Services;

namespace PixelFlow.Models
{
    public interface IGameSessionModel
    {
        int Score { get; }
        float ElapsedTime { get; }
        int StarsEarned { get; }
        bool IsSessionActive { get; }

        event Action<int> OnScoreChanged;
        event Action<float> OnTimeChanged;
        event Action<int> OnStarsChanged;

        void StartSession();
        void UpdateTime(float deltaTime);
        void AddScore(int points);
        void SetStars(int stars);
        void ResetSession();
    }

    public class GameSessionModel : IGameSessionModel
    {
        public int Score { get; private set; }
        public float ElapsedTime { get; private set; }
        public int StarsEarned { get; private set; }
        public bool IsSessionActive { get; private set; }

        public event Action<int> OnScoreChanged;
        public event Action<float> OnTimeChanged;
        public event Action<int> OnStarsChanged;

        public void StartSession()
        {
            Score = 0;
            ElapsedTime = 0f;
            StarsEarned = 0;
            IsSessionActive = true;
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
            StarsEarned = Math.Max(0, Math.Min(stars, 3));
            OnStarsChanged?.Invoke(StarsEarned);
        }

        public void ResetSession()
        {
            Score = 0;
            ElapsedTime = 0f;
            StarsEarned = 0;
            IsSessionActive = false;
        }
    }
}
