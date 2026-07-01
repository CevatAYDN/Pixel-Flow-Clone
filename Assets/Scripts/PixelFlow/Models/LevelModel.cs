using PixelFlow.Data;
using System.Threading;
using System.Threading.Tasks;
using Nexus.Core;

namespace PixelFlow.Models
{
    public interface ILevelModel
    {
        LevelData CurrentLevel { get; }
        void SetLevel(LevelData level);
    }

    public class LevelModel : ILevelModel, IReactiveModel
    {
        public LevelData CurrentLevel { get; private set; }

        public void SetLevel(LevelData level)
        {
            CurrentLevel = level;
        }

        public ValueTask OnBind(CancellationToken ct) => default;
    }
}
