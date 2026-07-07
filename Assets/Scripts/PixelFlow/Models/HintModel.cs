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
    /// Testlerde InMemoryPlayerPrefsService ile değiştirilebilir.
    /// </summary>
    public class HintModel : IHintModel, IReactiveModel
    {
        private const string Key = "HintCount";
        private const int DefaultHints = 3;

        [Inject] public IPlayerPrefsService _prefs { get; set; }
        private int _hintsRemaining;
        private int _totalHintsUsed;

        public int HintsRemaining => _hintsRemaining;
        public int TotalHintsUsed => _totalHintsUsed;
        public event Action<int> OnHintCountChanged;

        public ValueTask OnBind(CancellationToken ct)
        {
            _hintsRemaining = _prefs != null ? _prefs.GetInt(Key, DefaultHints) : DefaultHints;
            return default;
        }

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
            // 3 star = +1 hint, 2 star = +0.5 hint (rounded), 1 star = no hint
            if (stars >= 3)
            {
                AddHint();
            }
            else if (stars == 2)
            {
                // 50% chance to get a hint for 2 stars
                if (UnityEngine.Random.value < 0.5f)
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