#if UNITY_EDITOR
using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace PixelFlow.Editor
{
    public static class AudioClipGenerator
    {
        public static void GenerateAllAudioClips()
        {
            string[] audioPaths = new string[]
            {
                "Assets/Resources/Audio/SFX/Crash.wav",
                "Assets/Resources/Audio/SFX/Horn.wav",
                "Assets/Resources/Audio/SFX/ViaductPlace.wav",
                "Assets/Resources/Audio/SFX/LevelComplete.wav",
                "Assets/Resources/Audio/SFX/CoinCollect.wav",
                "Assets/Resources/Audio/SFX/UIClick.wav",
                "Assets/Resources/Audio/SFX/PathDraw.wav",
                "Assets/Resources/Audio/SFX/VehicleEngine.wav",
                "Assets/Resources/Audio/SFX/PowerUpActivate.wav",
                "Assets/Resources/Audio/SFX/PowerUpClear.wav",
                "Assets/Resources/Audio/AMB/AmbientHub.wav",
                "Assets/Resources/Audio/AMB/AmbientPuzzle.wav",
                "Assets/Resources/Audio/AMB/AmbientOverclock.wav",
                "Assets/Resources/Audio/MUSIC/MainTheme.wav"
            };

            int generatedCount = 0;
            foreach (string path in audioPaths)
            {
                if (File.Exists(path)) continue;

                string dir = Path.GetDirectoryName(path);
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

                byte[] wavBytes = CreateMonoWavBytes(44100, 0.1f);
                File.WriteAllBytes(path, wavBytes);
                generatedCount++;
            }

            if (generatedCount > 0)
            {
                AssetDatabase.Refresh();
                Debug.Log($"[AudioClipGenerator] Successfully generated {generatedCount} missing PCM WAV audio clips in Resources/Audio/.");
            }
        }

        private static byte[] CreateMonoWavBytes(int sampleRate, float durationSeconds)
        {
            int numSamples = Mathf.Max(100, (int)(sampleRate * durationSeconds));
            int dataSize = numSamples * 2;
            int fileSize = 36 + dataSize;

            byte[] bytes = new byte[44 + dataSize];

            // RIFF header
            Encoding.ASCII.GetBytes("RIFF").CopyTo(bytes, 0);
            BitConverter.GetBytes(fileSize).CopyTo(bytes, 4);
            Encoding.ASCII.GetBytes("WAVE").CopyTo(bytes, 8);

            // fmt chunk
            Encoding.ASCII.GetBytes("fmt ").CopyTo(bytes, 12);
            BitConverter.GetBytes(16).CopyTo(bytes, 16);
            BitConverter.GetBytes((short)1).CopyTo(bytes, 20); // PCM
            BitConverter.GetBytes((short)1).CopyTo(bytes, 22); // Mono
            BitConverter.GetBytes(sampleRate).CopyTo(bytes, 24);
            BitConverter.GetBytes(sampleRate * 2).CopyTo(bytes, 28);
            BitConverter.GetBytes((short)2).CopyTo(bytes, 32);
            BitConverter.GetBytes((short)16).CopyTo(bytes, 34);

            // data chunk
            Encoding.ASCII.GetBytes("data").CopyTo(bytes, 36);
            BitConverter.GetBytes(dataSize).CopyTo(bytes, 40);

            // PCM sine wave samples for audible feedback
            for (int i = 0; i < numSamples; i++)
            {
                float t = (float)i / sampleRate;
                short sample = (short)(Mathf.Sin(2f * Mathf.PI * 440f * t) * 8000f);
                BitConverter.GetBytes(sample).CopyTo(bytes, 44 + (i * 2));
            }

            return bytes;
        }
    }
}
#endif
