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
        int RetryCount { get; }
        bool HasUsedCrisisUndo { get; }

        event Action<int> OnScoreChanged;
        event Action<float> OnTimeChanged;
        event Action<int> OnStarsChanged;
        event Action<int> OnViaductsChanged;
        event Action<float> OnSimulationTimerChanged;
        event Action<int> OnRetryCountChanged;

        void StartSession();
        void StartSession(int maxViaducts);
        void UpdateTime(float deltaTime);
        void AddScore(int points);
        void SetStars(int stars);
        void ResetSession();
        bool TryUseViaduct();
        void RefundViaduct();
        void SetSimulationTimer(float remaining);
        void IncrementRetryCount();
        void MarkCrisisUndoUsed();
        void AddBonusViaduct(int amount);
        void ApplySave(int availableViaducts, int maxViaducts, float elapsedTime, int score, int stars);
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
        public int RetryCount { get; private set; }
        public bool HasUsedCrisisUndo { get; private set; }

        public event Action<int> OnScoreChanged;
        public event Action<float> OnTimeChanged;
        public event Action<int> OnStarsChanged;
        public event Action<int> OnViaductsChanged;
        public event Action<float> OnSimulationTimerChanged;
        public event Action<int> OnRetryCountChanged;

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
            RetryCount = 0;
            HasUsedCrisisUndo = false;
            OnViaductsChanged?.Invoke(AvailableViaducts);
            OnRetryCountChanged?.Invoke(RetryCount);
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
            RetryCount = 0;
            HasUsedCrisisUndo = false;
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

        public void IncrementRetryCount()
        {
            RetryCount++;
            OnRetryCountChanged?.Invoke(RetryCount);
        }

        /// <summary>
        /// GDD §2.4: Kaza sonrası "Geri Al" seçildiğinde oyuncunun
        /// sonraki viyadük kullanma hakkı KAYBEDİLİR. Bu, MaxViaducts'ı
        /// kalıcı olarak 1 azaltır (rebind edilemez).
        /// </summary>
        public void MarkCrisisUndoUsed()
        {
            HasUsedCrisisUndo = true;
            if (MaxViaducts > 1)
            {
                MaxViaducts--;
            }
            if (AvailableViaducts > MaxViaducts)
            {
                AvailableViaducts = MaxViaducts;
            }
            OnViaductsChanged?.Invoke(AvailableViaducts);
        }

        /// <summary>
        /// GDD §6.1 "Acil Durum Viyadüğü" ödüllü reklam: +1 hak.
        /// Production'da RewardedAdCommand tarafından çağrılır.
        /// </summary>
        public void AddBonusViaduct(int amount)
        {
            if (amount <= 0) return;
            MaxViaducts += amount;
            AvailableViaducts += amount;
            OnViaductsChanged?.Invoke(AvailableViaducts);
        }

        /// <summary>
        /// Save dosyasından state'i geri yükle.
        /// </summary>
        public void ApplySave(int availableViaducts, int maxViaducts, float elapsedTime, int score, int stars)
        {
            MaxViaducts = System.Math.Max(0, maxViaducts);
            AvailableViaducts = System.Math.Clamp(availableViaducts, 0, MaxViaducts);
            ElapsedTime = System.Math.Max(0f, elapsedTime);
            Score = System.Math.Max(0, score);
            StarsEarned = System.Math.Clamp(stars, 0, 3);
            IsSessionActive = true;
            RetryCount = 0;
            HasUsedCrisisUndo = false;
            OnViaductsChanged?.Invoke(AvailableViaducts);
            OnTimeChanged?.Invoke(ElapsedTime);
            OnScoreChanged?.Invoke(Score);
            OnStarsChanged?.Invoke(StarsEarned);
            OnRetryCountChanged?.Invoke(RetryCount);
        }

        public ValueTask OnBind(CancellationToken ct) => default;

        public void SetSimulationTimer(float remaining)
        {
            SimulationTimeRemaining = remaining;
            OnSimulationTimerChanged?.Invoke(remaining);
        }
    }
}
