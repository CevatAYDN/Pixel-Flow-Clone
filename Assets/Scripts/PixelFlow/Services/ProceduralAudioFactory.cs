using UnityEngine;

namespace PixelFlow.Services
{
    /// <summary>
    /// AudioClip'leri Resources/Audio/ klasöründen yükler.
    /// Kullanıcı kendi ses dosyalarını aşağıdaki yollara koyunca
    /// otomatik olarak çalarlar.
    ///
    /// 📁 Resources/Audio/
    ///   ├── SFX/
    ///   │   ├── Crash.wav
    ///   │   ├── Horn.wav
    ///   │   ├── ViaductPlace.wav
    ///   │   ├── LevelComplete.wav
    ///   │   ├── CoinCollect.wav
    ///   │   ├── UIClick.wav
    ///   │   ├── PathDraw.wav
    ///   │   └── VehicleEngine.wav   (loop)
    ///   ├── AMB/
    ///   │   ├── AmbientHub.wav       (loop)
    ///   │   ├── AmbientPuzzle.wav    (loop)
    ///   │   └── AmbientOverclock.wav (loop)
    ///   └── MUSIC/
    ///       └── MainTheme.wav        (loop)
    ///
    /// Eksik clip'ler için Editor'de uyarı log'lanır, runtime'da
    /// sessiz bir fallback kullanılır (crash olmaz).
    /// </summary>
    public static class AudioClipProvider
    {
        /// <summary>Belirtilen SfxType için AudioClip yükler.</summary>
        public static AudioClip Load(SfxType type)
        {
            string path = GetResourcePath(type);
            var clip = Resources.Load<AudioClip>(path);
            if (clip != null)
                return clip;

#if UNITY_EDITOR
            Debug.LogWarning($"[AudioClipProvider] '{path}' bulunamadı! "
                + $"Resources/Audio/ klasörüne {type}.wav dosyasını koyun. "
                + $"Geçici sessiz clip kullanılıyor.");
#endif

            // Fallback: 0.01sn sessiz clip (null döndürmez, AudioSource hata vermez)
            return CreateSilentClip(type.ToString());
        }

        /// <summary>
        /// SfxType → Resources yolu dönüşümü.
        /// Örn: SfxType.Crash → "Audio/SFX/Crash"
        /// </summary>
        private static string GetResourcePath(SfxType type)
        {
            switch (type)
            {
                // ── SFX ──
                case SfxType.Crash:         return "Audio/SFX/Crash";
                case SfxType.Horn:          return "Audio/SFX/Horn";
                case SfxType.ViaductPlace:  return "Audio/SFX/ViaductPlace";
                case SfxType.LevelComplete: return "Audio/SFX/LevelComplete";
                case SfxType.CoinCollect:   return "Audio/SFX/CoinCollect";
                case SfxType.UIClick:       return "Audio/SFX/UIClick";
                case SfxType.PathDraw:      return "Audio/SFX/PathDraw";
                case SfxType.VehicleEngine: return "Audio/SFX/VehicleEngine";

                // ── Ambient ──
                case SfxType.AmbientHub:       return "Audio/AMB/AmbientHub";
                case SfxType.AmbientPuzzle:    return "Audio/AMB/AmbientPuzzle";
                case SfxType.AmbientOverclock: return "Audio/AMB/AmbientOverclock";

                // ── Music ──
                case SfxType.MainTheme: return "Audio/MUSIC/MainTheme";

                default: return null;
            }
        }

        /// <summary>Sessiz 0.01s clip — null reference hatası vermemek için</summary>
        private static AudioClip CreateSilentClip(string name)
        {
            int sampleCount = 441; // 0.01s × 44100
            var clip = AudioClip.Create(name, sampleCount, 1, 44100, false);
            var data = new float[sampleCount];
            clip.SetData(data, 0);
            return clip;
        }
    }
}
