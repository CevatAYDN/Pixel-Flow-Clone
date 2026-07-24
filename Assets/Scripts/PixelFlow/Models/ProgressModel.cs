using PixelFlow.Data;
using PixelFlow.Services;
using System.Threading;
using System.Threading.Tasks;
using Nexus.Core;
using Nexus.Core.Services;

namespace PixelFlow.Models
{
    public interface IProgressModel
    {
        int UnlockedLevels { get; }
        void UnlockLevel(int levelIndex);
        /// <summary>Belirtilen seviye için kaydedilmiş en yüksek yıldız sayısını döner (0-3). Kayıt yoksa 0.</summary>
        int GetStars(int levelIndex);
        /// <summary>Seviye için kazanılan yıldızı kalıcı saklar; sadece önceki kayıttan yüksekse günceller.</summary>
        void RecordStars(int levelIndex, int stars);
    }

    /// <summary>
    /// Açılan level'ları IPlayerPrefsService üzerinden kalıcı saklar.
    /// Constructor injection ile prefs servisi alır; testlerde fake ile değiştirilebilir.
    /// </summary>
    public class ProgressModel : IProgressModel, IReactiveModel
    {
        private const string Key = "UnlockedLevels";

        private readonly IPlayerPrefsService _prefs;

        public int UnlockedLevels { get; private set; }

        [Inject]
        public ProgressModel(IPlayerPrefsService prefs, GameConfig config)
        {
            _prefs = prefs ?? throw new System.ArgumentNullException(nameof(prefs));
            int defaultUnlocked = config != null ? config.DefaultUnlockedLevels : throw new DataValidationException("GameConfig.DefaultUnlockedLevels erişilemedi!");
            UnlockedLevels = _prefs.GetInt(Key, defaultUnlocked);
        }

        // Test amaçlı constructor (config olmadan)
        internal ProgressModel(IPlayerPrefsService prefs, int defaultUnlocked)
        {
            _prefs = prefs ?? throw new System.ArgumentNullException(nameof(prefs));
            UnlockedLevels = _prefs.GetInt(Key, defaultUnlocked);
        }

        public void UnlockLevel(int levelIndex)
        {
            // Mantık: tamamlanan level index'i + 2, çünkü henüz gösterilmeyen bir sonraki
            // level için de izin veriyoruz. Aşağı iniş yok.
            int requiredUnlocked = levelIndex + 2;
            if (requiredUnlocked > UnlockedLevels)
            {
                UnlockedLevels = requiredUnlocked;
                _prefs.SetInt(Key, UnlockedLevels);
            }
        }

        // Seviye başına yıldız kalıcılığı (settings-levels.html ⭐ göstergesi için).
        private static string StarsKey(int levelIndex) => $"LevelStars_{levelIndex}";

        public int GetStars(int levelIndex)
        {
            if (levelIndex < 0) return 0;
            return _prefs.GetInt(StarsKey(levelIndex), 0);
        }

        public void RecordStars(int levelIndex, int stars)
        {
            if (levelIndex < 0) return;
            // 0-3 aralığına sıkıştır
            if (stars < 0) stars = 0;
            if (stars > 3) stars = 3;
            int existing = _prefs.GetInt(StarsKey(levelIndex), 0);
            if (stars > existing)
            {
                _prefs.SetInt(StarsKey(levelIndex), stars);
                _prefs.Save();
            }
        }

        public ValueTask OnBind(CancellationToken ct) => default;
    }
}