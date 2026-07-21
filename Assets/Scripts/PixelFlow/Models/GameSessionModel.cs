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
        int ViaductsUsed { get; }
        float SimulationTimeRemaining { get; set; }
        int RetryCount { get; }
        int CrisisAttemptCount { get; }
        int CurrentLevelId { get; }
        bool HasUsedCrisisUndo { get; }
        int CurrentFlowScore { get; }
        int TargetFlowScore { get; }

        event Action<int> OnScoreChanged;
        event Action<float> OnTimeChanged;
        event Action<int> OnStarsChanged;
        event Action<int> OnViaductsChanged;
        event Action<float> OnSimulationTimerChanged;
        event Action<int> OnRetryCountChanged;
        event Action<int, int> OnFlowScoreChanged;

        void StartSession();
        void StartSession(int maxViaducts);
        void StartSession(int maxViaducts, int targetFlowScore);
        void StartSession(int levelId, int maxViaducts, int targetFlowScore, bool hasLevelId);
        void UpdateTime(float deltaTime);
        void AddScore(int points);
        void SetStars(int stars);
        void ResetSession();
        bool TryUseViaduct();
        void RefundViaduct();
        void SetSimulationTimer(float remaining);
        void IncrementRetryCount();
        void ResetRetryCount();
        void IncrementCrisisAttempt();
        void ResetCrisisAttempt();
        void MarkCrisisUndoUsed();
        void AddBonusViaduct(int amount);
        void SetLevelId(int levelId);
        void IncrementFlowScore();
        void SetTargetFlowScore(int target);
        void ApplySave(int availableViaducts, int maxViaducts, float elapsedTime, int score, int stars, int levelId);
    }

    public class GameSessionModel : IGameSessionModel, IReactiveModel
    {
        public int Score { get; private set; }
        public float ElapsedTime { get; private set; }
        public int StarsEarned { get; private set; }
        public bool IsSessionActive { get; private set; }
        public int AvailableViaducts { get; private set; }
        public int MaxViaducts { get; private set; }
        public int ViaductsUsed => MaxViaducts - AvailableViaducts;
        public float SimulationTimeRemaining { get; set; }
        public int RetryCount { get; private set; }
        public int CrisisAttemptCount { get; private set; }
        public int CurrentLevelId { get; private set; }
        public bool HasUsedCrisisUndo { get; private set; }
        public int CurrentFlowScore { get; private set; }
        public int TargetFlowScore { get; private set; }

        public event Action<int> OnScoreChanged;
        public event Action<float> OnTimeChanged;
        public event Action<int> OnStarsChanged;
        public event Action<int> OnViaductsChanged;
        public event Action<float> OnSimulationTimerChanged;
        public event Action<int> OnRetryCountChanged;
        public event Action<int> OnCrisisAttemptCountChanged;
        public event Action<int, int> OnFlowScoreChanged;

        public void StartSession()
        {
            StartSession(3, 5);
        }

        public void StartSession(int maxViaducts)
        {
            StartSession(maxViaducts, 5);
        }

        public void StartSession(int maxViaducts, int targetFlowScore)
        {
            CurrentLevelId = 0;
            Score = 0;
            ElapsedTime = 0f;
            StarsEarned = 0;
            MaxViaducts = maxViaducts;
            AvailableViaducts = maxViaducts;
            IsSessionActive = true;
            RetryCount = 0;
            CrisisAttemptCount = 0;
            HasUsedCrisisUndo = false;
            CurrentFlowScore = 0;
            TargetFlowScore = targetFlowScore;
            OnViaductsChanged?.Invoke(AvailableViaducts);
            OnRetryCountChanged?.Invoke(RetryCount);
            OnFlowScoreChanged?.Invoke(CurrentFlowScore, TargetFlowScore);
        }

        public void StartSession(int levelId, int maxViaducts, int targetFlowScore, bool hasLevelId)
        {
            CurrentLevelId = hasLevelId ? levelId : 0;
            Score = 0;
            ElapsedTime = 0f;
            StarsEarned = 0;
            MaxViaducts = maxViaducts;
            AvailableViaducts = maxViaducts;
            IsSessionActive = true;
            RetryCount = 0;
            CrisisAttemptCount = 0;
            HasUsedCrisisUndo = false;
            CurrentFlowScore = 0;
            TargetFlowScore = targetFlowScore;
            OnViaductsChanged?.Invoke(AvailableViaducts);
            OnRetryCountChanged?.Invoke(RetryCount);
            OnFlowScoreChanged?.Invoke(CurrentFlowScore, TargetFlowScore);
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
            CrisisAttemptCount = 0;
            CurrentLevelId = 0;
            HasUsedCrisisUndo = false;
            CurrentFlowScore = 0;
            TargetFlowScore = 0;
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

        public void ResetRetryCount()
        {
            RetryCount = 0;
            OnRetryCountChanged?.Invoke(RetryCount);
        }

        /// <summary>
        /// GDD §4.3: Kriz anı sayacı. 3 ardışık kaza denemesi → LevelFailed.
        /// </summary>
        public void IncrementCrisisAttempt()
        {
            CrisisAttemptCount++;
            OnCrisisAttemptCountChanged?.Invoke(CrisisAttemptCount);
        }

        public void ResetCrisisAttempt()
        {
            CrisisAttemptCount = 0;
            OnCrisisAttemptCountChanged?.Invoke(CrisisAttemptCount);
        }

        public void SetLevelId(int levelId)
        {
            CurrentLevelId = levelId;
        }

        public void IncrementFlowScore()
        {
            if (!IsSessionActive) return;
            CurrentFlowScore++;
            OnFlowScoreChanged?.Invoke(CurrentFlowScore, TargetFlowScore);
        }

        public void SetTargetFlowScore(int target)
        {
            if (!IsSessionActive) return;
            TargetFlowScore = target;
            OnFlowScoreChanged?.Invoke(CurrentFlowScore, TargetFlowScore);
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
        public void ApplySave(int availableViaducts, int maxViaducts, float elapsedTime, int score, int stars, int levelId)
        {
            CurrentLevelId = levelId;
            MaxViaducts = System.Math.Max(0, maxViaducts);
            AvailableViaducts = System.Math.Clamp(availableViaducts, 0, MaxViaducts);
            ElapsedTime = System.Math.Max(0f, elapsedTime);
            Score = System.Math.Max(0, score);
            StarsEarned = System.Math.Clamp(stars, 0, 3);
            IsSessionActive = true;
            RetryCount = 0;
            CrisisAttemptCount = 0;
            HasUsedCrisisUndo = false;
            CurrentFlowScore = 0;
            TargetFlowScore = 5; // Default fallback
            OnViaductsChanged?.Invoke(AvailableViaducts);
            OnTimeChanged?.Invoke(ElapsedTime);
            OnScoreChanged?.Invoke(Score);
            OnStarsChanged?.Invoke(StarsEarned);
            OnRetryCountChanged?.Invoke(RetryCount);
            OnFlowScoreChanged?.Invoke(CurrentFlowScore, TargetFlowScore);
        }

        public ValueTask OnBind(CancellationToken ct) => default;

        public void SetSimulationTimer(float remaining)
        {
            SimulationTimeRemaining = remaining;
            OnSimulationTimerChanged?.Invoke(remaining);
        }
    }
}
