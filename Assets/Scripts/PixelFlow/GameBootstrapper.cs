using UnityEngine;
using Nexus.Core;
using PixelFlow.Data;
using PixelFlow.Signals;
using System.Collections;

namespace PixelFlow
{
    /// <summary>
    /// Oyunun başlangıç noktası. Nexus Root'un tamamen hazır olmasını bekler,
    /// sonra ilk level'ı yükler. Çift fallback stratejisi:
    ///   1) Inspector'dan atanmış initialLevel (tercih edilen)
    ///   2) Resources/Levels/Level1 (kurulum sonrası garanti)
    ///   3) Resources'taki ilk LevelData (kullanıcı farklı isimle kaydettiyse)
    /// Hiçbiri yoksa net bir hata loglar ve çıkar.
    /// </summary>
    public class GameBootstrapper : MonoBehaviour
    {
        public LevelData initialLevel;
        public Root nexusRoot;

        private const int RootSearchRetries = 10;
        private const float RootSearchInterval = 0.1f;

        private IEnumerator Start()
        {
            yield return WaitForRoot();
            if (nexusRoot == null)
            {
                Debug.LogError("[PixelFlow] Nexus Root not found after retries. Game cannot start.");
                yield break;
            }

            // Root tüm lifecycle aşamalarını bitirene kadar bekle; aksi halde
            // SignalBus henüz komutları kaydetmemiş olabilir.
            while (!nexusRoot.IsInitialized)
            {
                yield return null;
            }

            var level = ResolveInitialLevel();
            if (level == null)
            {
                Debug.LogError("[PixelFlow] No level available to load. Create a LevelData asset or assign initialLevel.");
                yield break;
            }

            Debug.Log($"[PixelFlow] Bootstrap loading level: {level.name} ({level.width}x{level.height})");
            var signalBus = nexusRoot.Context.Container.Resolve<ISignalBus>();
            signalBus.Fire(new LoadLevelSignal { LevelToLoad = level });
        }

        private IEnumerator WaitForRoot()
        {
            int retries = RootSearchRetries;
            while (nexusRoot == null && retries > 0)
            {
                nexusRoot = FindAnyObjectByType<Root>();
                if (nexusRoot == null)
                {
                    retries--;
                    yield return new WaitForSeconds(RootSearchInterval);
                }
            }
        }

        private LevelData ResolveInitialLevel()
        {
            if (initialLevel != null) return initialLevel;

            // Yaygın isim
            var byName = Resources.Load<LevelData>("Levels/Level1");
            if (byName != null) return byName;

            // Resources/Levels altındaki ilk LevelData
            var any = Resources.LoadAll<LevelData>("Levels");
            if (any != null && any.Length > 0)
            {
                Debug.LogWarning($"[PixelFlow] Levels/Level1 bulunamadı; ilk bulunan LevelData kullanılıyor: {any[0].name}");
                return any[0];
            }

            return null;
        }
    }
}