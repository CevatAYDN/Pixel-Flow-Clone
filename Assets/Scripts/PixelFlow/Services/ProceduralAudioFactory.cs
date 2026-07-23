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

        /// <summary>Eksik WAV dosyaları için prosedürel synth ses dalgası üretir.</summary>
        private static AudioClip CreateSilentClip(string name)
        {
            int sampleRate = 44100;
            float duration = 0.15f;
            float freq = 440f; // Default A4 note

            if (name.Contains("UIClick")) { duration = 0.05f; freq = 800f; }
            else if (name.Contains("CoinCollect")) { duration = 0.2f; freq = 1200f; }
            else if (name.Contains("LevelComplete")) { duration = 0.4f; freq = 600f; }
            else if (name.Contains("Horn")) { duration = 0.25f; freq = 350f; }
            else if (name.Contains("Crash")) { duration = 0.2f; freq = 180f; }
            else if (name.Contains("VehicleEngine")) { duration = 0.3f; freq = 220f; }

            int sampleCount = Mathf.RoundToInt(sampleRate * duration);
            var clip = AudioClip.Create(name, sampleCount, 1, sampleRate, false);
            var data = new float[sampleCount];

            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleRate;
                float env = 1f - (t / duration); // Decay envelope
                data[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * env * 0.15f;
            }

            clip.SetData(data, 0);
            return clip;
        }
    }
}
