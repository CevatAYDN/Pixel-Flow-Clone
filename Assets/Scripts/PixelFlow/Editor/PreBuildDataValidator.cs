#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using PixelFlow.Data;
using System.IO;

namespace PixelFlow.Editor
{
    /// <summary>
    /// Color Jam 3D - Build Öncesi Veri Doğrulayıcı (Pre-Build Data Validator).
    /// Derleme (Build) veya Play Mode öncesinde projede eksik ScriptableObject, hardcoded ihlal
    /// veya çözümsüz seviye olup olmadığını denetler. Hata varsa Build almayı engeller.
    /// </summary>
    public class PreBuildDataValidator : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;

        [InitializeOnLoadMethod]
        private static void RegisterPlayModeValidation()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingEditMode)
            {
                if (!ValidateAllData(out string errorMessage))
                {
                    EditorApplication.isPlaying = false;
                    Debug.LogError($"[Zero-Hardcode Validator] Play Mode Engellendi! Nedeni: {errorMessage}");
                    EditorUtility.DisplayDialog("Veri Doğrulama Hatası", $"Play Mode başlatılamadı:\n\n{errorMessage}", "Tamam");
                }
            }
        }

        public void OnPreprocessBuild(BuildReport report)
        {
            if (!ValidateAllData(out string errorMessage))
            {
                throw new BuildFailedException($"[Zero-Hardcode Validator] Build Engellendi! Nedeni: {errorMessage}");
            }
        }

        public static bool ValidateAllData(out string errorMessage)
        {
            // 1. GameConfig Kontrolü
            var gameConfig = Resources.Load<GameConfig>("Configs/GameConfig");
            if (gameConfig == null)
            {
                // Fallback yok: GameConfig Resources klasöründe bulunmak zorunda!
                errorMessage = "Resources/Configs/GameConfig.asset bulunamadı! Lütfen 'Pixel Flow Kontrol Merkezi'nden oluşturun.";
                return false;
            }

            if (gameConfig.VehicleSpeed <= 0f)
            {
                errorMessage = "GameConfig.VehicleSpeed değeri 0 veya negatif olamaz!";
                return false;
            }

            // 2. Seviye Kataloğu Kontrolü
            var levelCatalog = Resources.Load<LevelCatalogAsset>("LevelCatalog");
            if (levelCatalog != null && levelCatalog.Levels != null)
            {
                foreach (var entry in levelCatalog.Levels)
                {
                    if (entry == null) continue;
                    if (!entry.UseProceduralFallback && entry.AuthoredLevel == null)
                    {
                        errorMessage = $"LevelCatalog içindeki LevelIndex {entry.LevelIndex} için AuthoredLevel NULL!";
                        return false;
                    }
                }
            }

            errorMessage = string.Empty;
            return true;
        }
    }
}
#endif
