using UnityEngine;

namespace PixelFlow.Models
{
    public interface IProgressModel
    {
        int UnlockedLevels { get; }
        void UnlockNextLevel();
    }

    public class ProgressModel : IProgressModel
    {
        public int UnlockedLevels { get; private set; }

        public ProgressModel()
        {
            UnlockedLevels = PlayerPrefs.GetInt("UnlockedLevels", 1);
        }

        public void UnlockNextLevel()
        {
            UnlockedLevels++;
            PlayerPrefs.SetInt("UnlockedLevels", UnlockedLevels);
            PlayerPrefs.Save();
        }
    }
}
