using UnityEngine;

namespace PixelFlow.Services
{
    /// <summary>
    /// GDD §16.2 + Ek A: 12 SFX tipi için procedural AudioClip üretici.
    /// Gerçek ses dosyaları üretilmediği için (no asset deps), synth ile
    /// runtime'da üretilir. Bu sınıf statik; cache eder.
    /// </summary>
    public static class ProceduralAudioFactory
    {
        private const int SampleRate = 44100;

        public static AudioClip CreateCrash()
        {
            int sampleCount = (int)(0.4f * SampleRate);
            float[] data = new float[sampleCount];
            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / SampleRate;
                float env = Mathf.Exp(-t * 8f);
                float noise = Random.Range(-1f, 1f);
                float low = Mathf.Sin(2f * Mathf.PI * 80f * t);
                data[i] = (noise * 0.7f + low * 0.3f) * env;
            }
            return MakeClip("SFX_Crash", data);
        }

        public static AudioClip CreateHorn()
        {
            int sampleCount = (int)(0.25f * SampleRate);
            float[] data = new float[sampleCount];
            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / SampleRate;
                float env = Mathf.Min(1f, t * 20f) * Mathf.Exp(-t * 4f);
                data[i] = (Mathf.Sin(2f * Mathf.PI * 440f * t) + Mathf.Sin(2f * Mathf.PI * 660f * t) * 0.5f) * env * 0.4f;
            }
            return MakeClip("SFX_Horn", data);
        }

        public static AudioClip CreateViaductPlace()
        {
            int sampleCount = (int)(0.3f * SampleRate);
            float[] data = new float[sampleCount];
            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / SampleRate;
                float env = Mathf.Exp(-t * 12f);
                float sweep = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(200f, 800f, t * 4f) * t);
                data[i] = sweep * env * 0.5f;
            }
            return MakeClip("SFX_Viaduct", data);
        }

        public static AudioClip CreateLevelComplete()
        {
            int sampleCount = (int)(1.5f * SampleRate);
            float[] data = new float[sampleCount];
            float[] freqs = { 523f, 659f, 784f, 1047f };
            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / SampleRate;
                int note = Mathf.Min((int)(t * 4f), freqs.Length - 1);
                float env = Mathf.Min(1f, t * 5f) * Mathf.Exp(-(t - note * 0.25f) * 2f);
                env = Mathf.Max(0f, env);
                data[i] = Mathf.Sin(2f * Mathf.PI * freqs[note] * t) * env * 0.3f;
            }
            return MakeClip("SFX_LevelComplete", data);
        }

        public static AudioClip CreateCoinCollect()
        {
            int sampleCount = (int)(0.15f * SampleRate);
            float[] data = new float[sampleCount];
            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / SampleRate;
                float env = Mathf.Exp(-t * 25f);
                float freq = Mathf.Lerp(880f, 1320f, t * 4f);
                data[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * env * 0.3f;
            }
            return MakeClip("SFX_CoinCollect", data);
        }

        public static AudioClip CreateUIClick()
        {
            int sampleCount = (int)(0.05f * SampleRate);
            float[] data = new float[sampleCount];
            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / SampleRate;
                float env = Mathf.Exp(-t * 80f);
                data[i] = Mathf.Sin(2f * Mathf.PI * 1200f * t) * env * 0.25f;
            }
            return MakeClip("SFX_UIClick", data);
        }

        public static AudioClip CreateAmbientHub()
        {
            int sampleCount = (int)(4f * SampleRate);
            float[] data = new float[sampleCount];
            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / SampleRate;
                float w1 = Mathf.Sin(2f * Mathf.PI * 110f * t);
                float w2 = Mathf.Sin(2f * Mathf.PI * 165f * t);
                float w3 = Mathf.Sin(2f * Mathf.PI * 220f * t);
                float wind = (Mathf.PerlinNoise(t * 0.5f, 0) - 0.5f) * 0.3f;
                data[i] = (w1 * 0.3f + w2 * 0.2f + w3 * 0.1f + wind) * 0.15f;
            }
            return MakeClip("AMB_Hub", data);
        }

        public static AudioClip CreateAmbientPuzzle()
        {
            int sampleCount = (int)(4f * SampleRate);
            float[] data = new float[sampleCount];
            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / SampleRate;
                float pulse = Mathf.Sin(2f * Mathf.PI * 0.3f * t);
                float hum = Mathf.Sin(2f * Mathf.PI * 220f * t) * (0.5f + 0.5f * pulse);
                data[i] = hum * 0.1f;
            }
            return MakeClip("AMB_Puzzle", data);
        }

        public static AudioClip CreateAmbientOverclock()
        {
            int sampleCount = (int)(4f * SampleRate);
            float[] data = new float[sampleCount];
            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / SampleRate;
                float beat = Mathf.Sin(2f * Mathf.PI * 2f * t);
                float bass = Mathf.Sin(2f * Mathf.PI * 100f * t) * (0.5f + 0.5f * beat);
                float lead = Mathf.Sin(2f * Mathf.PI * 660f * t) * 0.3f;
                data[i] = (bass + lead) * 0.2f;
            }
            return MakeClip("AMB_Overclock", data);
        }

        public static AudioClip CreateMainTheme()
        {
            int sampleCount = (int)(8f * SampleRate);
            float[] data = new float[sampleCount];
            float[] scale = { 261.63f, 293.66f, 329.63f, 349.23f, 392.00f, 440.00f, 493.88f, 523.25f };
            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / SampleRate;
                float beat = (t * 2f) % 1f;
                int idx = (int)(t * 1.5f) % scale.Length;
                float env = beat < 0.1f ? 1f : Mathf.Max(0f, 1f - (beat - 0.1f) * 2f);
                float note = Mathf.Sin(2f * Mathf.PI * scale[idx] * t);
                float bass = Mathf.Sin(2f * Mathf.PI * scale[idx] * 0.5f * t) * 0.3f;
                data[i] = (note + bass) * env * 0.15f;
            }
            return MakeClip("MUSIC_Main", data);
        }

        public static AudioClip CreateVehicleEngine()
        {
            int sampleCount = (int)(0.5f * SampleRate);
            float[] data = new float[sampleCount];
            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / SampleRate;
                float env = 0.4f + 0.6f * Mathf.Sin(2f * Mathf.PI * 8f * t);
                data[i] = (Mathf.Sin(2f * Mathf.PI * 120f * t) + Mathf.PerlinNoise(t * 20f, 0) * 0.3f) * env * 0.15f;
            }
            return MakeClip("SFX_Engine", data);
        }

        public static AudioClip CreatePathDraw()
        {
            int sampleCount = (int)(0.08f * SampleRate);
            float[] data = new float[sampleCount];
            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / SampleRate;
                float env = Mathf.Exp(-t * 35f);
                data[i] = Mathf.Sin(2f * Mathf.PI * 600f * t) * env * 0.2f;
            }
            return MakeClip("SFX_PathDraw", data);
        }

        private static AudioClip MakeClip(string name, float[] data)
        {
            var clip = AudioClip.Create(name, data.Length, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
