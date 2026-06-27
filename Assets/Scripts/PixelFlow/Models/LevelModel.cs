using PixelFlow.Data;

namespace PixelFlow.Models
{
    public interface ILevelModel
    {
        LevelData CurrentLevel { get; }
        void SetLevel(LevelData level);
    }

    public class LevelModel : ILevelModel
    {
        public LevelData CurrentLevel { get; private set; }

        public void SetLevel(LevelData level)
        {
            CurrentLevel = level;
        }
    }
}
