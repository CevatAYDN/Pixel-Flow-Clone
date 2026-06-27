using System;
using UnityEngine;

namespace PixelFlow.Models
{
    public interface IHintModel
    {
        int HintsRemaining { get; }
        event Action<int> OnHintCountChanged;
        void UseHint();
        void AddHints(int amount);
    }

    public class HintModel : IHintModel
    {
        public int HintsRemaining { get; private set; }
        public event Action<int> OnHintCountChanged;

        public HintModel()
        {
            HintsRemaining = PlayerPrefs.GetInt("HintCount", 3);
        }

        public void UseHint()
        {
            if (HintsRemaining > 0)
            {
                HintsRemaining--;
                PlayerPrefs.SetInt("HintCount", HintsRemaining);
                PlayerPrefs.Save();
                OnHintCountChanged?.Invoke(HintsRemaining);
            }
        }

        public void AddHints(int amount)
        {
            HintsRemaining += amount;
            PlayerPrefs.SetInt("HintCount", HintsRemaining);
            PlayerPrefs.Save();
            OnHintCountChanged?.Invoke(HintsRemaining);
        }
    }
}
