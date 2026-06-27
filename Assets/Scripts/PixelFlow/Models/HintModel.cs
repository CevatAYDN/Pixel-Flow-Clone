using System;
using PixelFlow.Services;

namespace PixelFlow.Models
{
    public interface IHintModel
    {
        int HintsRemaining { get; }
        event Action<int> OnHintCountChanged;
        void UseHint();
        void AddHints(int amount);
    }

    /// <summary>
    /// İpucu sayısını IPlayerPrefsService üzerinden kalıcı saklar.
    /// Testlerde InMemoryPlayerPrefsService ile değiştirilebilir.
    /// </summary>
    public class HintModel : IHintModel
    {
        private const string Key = "HintCount";
        private const int DefaultHints = 3;

        private readonly IPlayerPrefsService _prefs;
        private int _hintsRemaining;

        public int HintsRemaining => _hintsRemaining;
        public event Action<int> OnHintCountChanged;

        public HintModel(IPlayerPrefsService prefs)
        {
            _prefs = prefs ?? throw new System.ArgumentNullException(nameof(prefs));
            _hintsRemaining = _prefs.GetInt(Key, DefaultHints);
        }

        public void UseHint()
        {
            if (_hintsRemaining > 0)
            {
                _hintsRemaining--;
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
    }
}