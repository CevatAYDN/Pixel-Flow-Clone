using PixelFlow.Data;
using System;
using PixelFlow.Services;
using System.Threading;
using System.Threading.Tasks;
using Nexus.Core;
using Nexus.Core.Services;

namespace PixelFlow.Models
{
    public interface IHintModel
    {
        int HintsRemaining { get; }
        int TotalHintsUsed { get; }
        event Action<int> OnHintCountChanged;
        void UseHint();
        void AddHint();
        void AddHints(int amount);
        void ResetSessionHints();
        void AwardHintForStar(int stars);
    }

    /// <summary>
    /// İpucu sayısını IPlayerPrefsService üzerinden kalıcı saklar.
    /// Constructor injection ile alır — diğer modellerle tutarlı ve test edilebilir.
    /// Testlerde InMemoryPlayerPrefsService ile değiştirilebilir.
    /// </summary>
    public class HintModel : IHintModel, IReactiveModel
    {
        private const string Key = "HintCount";

        private readonly IPlayerPrefsService _prefs;
        private readonly float _twoStarHintChance;
        private int _hintsRemaining;
        private int _totalHintsUsed;

        public int HintsRemaining => _hintsRemaining;
        public int TotalHintsUsed => _totalHintsUsed;
        public event Action<int> OnHintCountChanged;

        [Inject]
        public HintModel(IPlayerPrefsService prefs, GameConfig config)
        {
            _prefs = prefs ?? throw new System.ArgumentNullException(nameof(prefs));
            if (config == null) throw new DataValidationException("GameConfig erişilemedi! HintModel başlatılamıyor.");
            _twoStarHintChance = config.TwoStarHintChance;
            _hintsRemaining = _prefs.GetInt(Key, config.DefaultHintCount);
        }

        // Test amaçlı constructor (config olmadan) — SO varsayılanını yansıtır
        internal HintModel(IPlayerPrefsService prefs, int defaultHints)
        {
            _prefs = prefs ?? throw new System.ArgumentNullException(nameof(prefs));
            _twoStarHintChance = 0.5f;
            _hintsRemaining = _prefs.GetInt(Key, defaultHints);
        }

        public ValueTask OnBind(CancellationToken ct) => default;

        public void UseHint()
        {
            if (_hintsRemaining > 0)
            {
                _hintsRemaining--;
                _totalHintsUsed++;
                _prefs.SetInt(Key, _hintsRemaining);
                OnHintCountChanged?.Invoke(_hintsRemaining);
            }
        }

        public void AddHints(int amount)
        {
            if (amount <= 0) return;
            _hintsRemaining += amount;
            _prefs.SetInt(Key, _hintsRemaining);
            OnHintCountChanged?.Invoke(_hintsRemaining);
        }

        public void AddHint()
        {
            AddHints(1);
        }

        public void AwardHintForStar(int stars)
        {
            if (stars >= 3)
            {
                AddHint();
            }
            else if (stars == 2)
            {
                if (UnityEngine.Random.value < _twoStarHintChance)
                {
                    AddHint();
                }
            }
        }

        public void ResetSessionHints()
        {
            _totalHintsUsed = 0;
        }
    }
}