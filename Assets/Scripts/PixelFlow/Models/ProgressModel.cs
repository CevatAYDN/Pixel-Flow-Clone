using UnityEngine;

namespace PixelFlow.Models
{
    public interface IProgressModel
    {
        int UnlockedLevels { get; }
        void UnlockLevel(int levelIndex);
    }

    public class ProgressModel : IProgressModel
    {
        public int UnlockedLevels { get; private set; }

        public ProgressModel()
        {
            UnlockedLevels = PlayerPrefs.GetInt("UnlockedLevels", 1);
        }

        public void UnlockLevel(int levelIndex)
        {
            int requiredUnlocked = levelIndex + 2;
            if (requiredUnlocked > UnlockedLevels)
            {
                UnlockedLevels = requiredUnlocked;
                PlayerPrefs.SetInt("UnlockedLevels", UnlockedLevels);
                PlayerPrefs.Save();
            }
        }
    }
}
