#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using PixelFlow.Data;
using PixelFlow.Services;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace PixelFlow.Editor
{
    /// <summary>
    /// Color Jam 3D - Build Öncesi Veri Doğrulayıcı (Pre-Build Data Validator).
    /// Derleme (Build) veya Play Mode öncesinde projede eksik ScriptableObject, hardcoded ihlal
    /// veya çözümsüz seviye olup olmadığını denetler. Hata varsa Build almayı engeller (game_plan.md §2.1.B3 & §2.2).
    /// </summary>
    public class PreBuildDataValidator : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;

        [InitializeOnLoadMethod]
        private static void RegisterPlayModeValidation()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static bool IsRunningTests()
        {
            if (Application.isBatchMode) return true;
            string stackTrace = System.Environment.StackTrace;
            if (stackTrace.Contains("TestRunner") || stackTrace.Contains("NUnit") || stackTrace.Contains("UnitTestRunner"))
            {
                return true;
            }
            string cmd = System.Environment.CommandLine;
            if (cmd.Contains("-runTests") || cmd.Contains("-testPlatform"))
            {
                return true;
            }
            return false;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingEditMode)
            {
                if (!ValidateAllData(out string errorMessage))
                {
                    EditorApplication.isPlaying = false;
                    Debug.LogError($"[Zero-Hardcode Validator] Play Mode Engellendi! Nedeni: {errorMessage}");
                    if (!Application.isBatchMode && !IsRunningTests())
                    {
                        Debug.LogError($"[Zero-Hardcode Validator] Play Mode dialog skipped: {errorMessage}");
                    }
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
                errorMessage = "Resources/Configs/GameConfig.asset bulunamadı! Lütfen 'Pixel Flow Kontrol Merkezi'nden oluşturun.";
                return false;
            }

            if (gameConfig.VehicleSpeed <= 0f)
            {
                errorMessage = "GameConfig.VehicleSpeed değeri 0 veya negatif olamaz!";
                return false;
            }

            if (gameConfig.InterstitialLevelInterval <= 0)
            {
                errorMessage = "GameConfig.InterstitialLevelInterval değeri 0 veya negatif olamaz!";
                return false;
            }

            var storageKeys = Resources.Load<StorageKeysConfigAsset>("Configs/StorageKeysConfig");
            if (storageKeys == null)
            {
                errorMessage = "Resources/Configs/StorageKeysConfig.asset bulunamadı! Zero-hardcode policy için zorunludur.";
                return false;
            }

            foreach (var field in typeof(StorageKeysConfigAsset).GetFields(BindingFlags.Instance | BindingFlags.Public))
            {
                if (field.FieldType != typeof(string)) continue;
                var value = field.GetValue(storageKeys) as string;
                if (string.IsNullOrEmpty(value))
                {
                    errorMessage = $"StorageKeysConfigAsset alanı boş: {field.Name}. Zero-hardcode policy gereği tüm anahtarlar doldurulmalı.";
                    return false;
                }
            }

            // 2. PhaseConfig & EconomyConfig Kontrolü
            var phaseConfig = Resources.Load<PhaseConfigAsset>("Configs/PhaseConfig");
            if (phaseConfig == null)
            {
                errorMessage = "Resources/Configs/PhaseConfig.asset bulunamadı! Lütfen 'Pixel Flow Kontrol Merkezi'nden oluşturun.";
                return false;
            }

            var economyConfig = Resources.Load<EconomyConfigAsset>("Configs/EconomyConfig");
            if (economyConfig == null)
            {
                errorMessage = "Resources/Configs/EconomyConfig.asset bulunamadı!";
                return false;
            }

            economyConfig.ValidateIapProducts();

            if (gameConfig.InterstitialFrequency <= 0)
            {
                errorMessage = "GameConfig.InterstitialFrequency değeri 0 veya negatif olamaz!";
                return false;
            }

            if (gameConfig.RewardedUndoLimit <= 0)
            {
                errorMessage = "GameConfig.RewardedUndoLimit değeri 0 veya negatif olamaz!";
                return false;
            }

            if (string.IsNullOrWhiteSpace(gameConfig.InterstitialPlacementId) ||
                string.IsNullOrWhiteSpace(gameConfig.RewardedPlacementId) ||
                string.IsNullOrWhiteSpace(gameConfig.BannerPlacementId))
            {
                errorMessage = "GameConfig reklam placement ID alanları boş bırakılamaz!";
                return false;
            }

            var diffConfig = Resources.Load<DifficultyFormulaConfigAsset>("Configs/DifficultyFormulaConfig");
            if (diffConfig == null)
            {
                errorMessage = "Resources/Configs/DifficultyFormulaConfig.asset bulunamadı!";
                return false;
            }

            var defaultSkinConfig = Resources.Load<DefaultSkinIdsConfigAsset>("Configs/DefaultSkinIdsConfig");
            if (defaultSkinConfig == null)
            {
                errorMessage = "Resources/Configs/DefaultSkinIdsConfig.asset bulunamadı!";
                return false;
            }

            if (string.IsNullOrWhiteSpace(defaultSkinConfig.DefaultVehicleSkinId) ||
                string.IsNullOrWhiteSpace(defaultSkinConfig.DefaultStopSkinId))
            {
                errorMessage = "DefaultSkinIdsConfigAsset alanları boş bırakılamaz!";
                return false;
            }

            var bouncyConfig = Resources.Load<BouncyPhysicsConfigAsset>("Configs/BouncyPhysicsConfig");
            if (bouncyConfig == null)
            {
                errorMessage = "Resources/Configs/BouncyPhysicsConfig.asset bulunamadı!";
                return false;
            }

            if (bouncyConfig.BounceForce <= 0f || bouncyConfig.BounceDamping <= 0f || bouncyConfig.SquishFactor <= 0f)
            {
                errorMessage = "BouncyPhysicsConfigAsset alanları pozitif olmalıdır!";
                return false;
            }

            var starConfig = Resources.Load<StarCriteriaConfigAsset>("Configs/StarCriteriaConfig");
            if (starConfig == null)
            {
                errorMessage = "Resources/Configs/StarCriteriaConfig.asset bulunamadı!";
                return false;
            }

            if (starConfig.ThreeStarsMaxViaducts < 0 || starConfig.TwoStarsMaxViaducts < 0)
            {
                errorMessage = "StarCriteriaConfigAsset viyadük eşikleri negatif olamaz!";
                return false;
            }

            var rushHourConfig = Resources.Load<RushHourConfigAsset>("Configs/RushHourConfig");
            if (rushHourConfig == null)
            {
                errorMessage = "Resources/Configs/RushHourConfig.asset bulunamadı!";
                return false;
            }

            // 2b. Görsel & Tema Konfigürasyonları
            var themePalette = Resources.Load<ThemePaletteAsset>("Configs/ThemePalette");
            if (themePalette == null)
            {
                errorMessage = "Resources/Configs/ThemePalette.asset bulunamadı!";
                return false;
            }

            var colorBlindPalette = Resources.Load<ColorBlindPaletteAsset>("Configs/ColorBlindPalette");
            if (colorBlindPalette == null)
            {
                errorMessage = "Resources/Configs/ColorBlindPalette.asset bulunamadı!";
                return false;
            }

            var vehicleMaterialConfig = Resources.Load<VehicleMaterialConfigAsset>("Configs/VehicleMaterialConfig");
            if (vehicleMaterialConfig == null)
            {
                errorMessage = "Resources/Configs/VehicleMaterialConfig.asset bulunamadı!";
                return false;
            }

            var vehicleVisualConfig = Resources.Load<VehicleVisualConfigAsset>("Configs/VehicleVisualConfig");
            if (vehicleVisualConfig == null)
            {
                errorMessage = "Resources/Configs/VehicleVisualConfig.asset bulunamadı!";
                return false;
            }

            var levelValidator = new LevelValidator(new RuntimePathSolver());

            // 3. Seviye Kataloğu Kontrolü (asset Resources/Configs/LevelCatalog.asset konumunda)
            var levelCatalog = Resources.Load<LevelCatalogAsset>("Configs/LevelCatalog");
            if (levelCatalog != null && levelCatalog.Levels != null)
            {
                var seenIndices = new HashSet<int>();
                foreach (var entry in levelCatalog.Levels)
                {
                    if (entry == null) continue;

                    if (!seenIndices.Add(entry.LevelIndex))
                    {
                        errorMessage = $"LevelCatalog içinde tekrarlanan LevelIndex tespit edildi: {entry.LevelIndex}";
                        return false;
                    }

                    // 3a. AuthoredLevel null kontrolü ve Otomatik Tamir
                    if (!entry.UseProceduralFallback && entry.AuthoredLevel == null)
                    {
                        Debug.LogWarning($"[PreBuildDataValidator] LevelCatalog içindeki LevelIndex {entry.LevelIndex} için AuthoredLevel NULL! Otomatik olarak UseProceduralFallback=true yapıldı.");
                        entry.UseProceduralFallback = true;
                        entry.ProceduralDifficulty = DifficultyParams.Medium;
                        EditorUtility.SetDirty(levelCatalog);
                    }

                    // 3b. Prosedürel seviyeler için parametre doğrulama (game_plan.md §2.1.B3)
                    if (entry.UseProceduralFallback)
                    {
                        if (entry.ProceduralDifficulty.gridWidth < 3 || entry.ProceduralDifficulty.gridHeight < 3)
                        {
                            errorMessage = $"LevelCatalog LevelIndex {entry.LevelIndex} UseProceduralFallback=true " +
                                $"için grid boyutu ({entry.ProceduralDifficulty.gridWidth}x{entry.ProceduralDifficulty.gridHeight}) geçersiz! Minimum 3x3 olmalıdır.";
                            return false;
                        }

                        if (entry.ProceduralDifficulty.colorCount <= 0)
                        {
                            errorMessage = $"LevelCatalog LevelIndex {entry.LevelIndex} için procedural colorCount pozitif olmalıdır!";
                            return false;
                        }
                    }

                    if (entry.AuthoredLevel != null && entry.AuthoredLevel is PixelFlow.Data.LevelData levelData)
                    {
                        if (levelData.width < 3 || levelData.height < 3)
                        {
                            errorMessage = $"LevelCatalog LevelIndex {entry.LevelIndex} (AuthoredLevel) için grid boyutu " +
                                $"({levelData.width}x{levelData.height}) çok küçük! Minimum 3x3 olmalıdır.";
                            return false;
                        }
                        if (levelData.flowScoreThreshold <= 0)
                        {
                            errorMessage = $"LevelCatalog LevelIndex {entry.LevelIndex} (AuthoredLevel) için flowScoreThreshold " +
                                $"{levelData.flowScoreThreshold} — pozitif olmalıdır.";
                            return false;
                        }

                        var validation = levelValidator.Validate(levelData);
                        if (!validation.IsValid)
                        {
                            errorMessage = $"LevelCatalog LevelIndex {entry.LevelIndex} için LevelValidator hata verdi: {string.Join(" | ", validation.Issues.Where(i => i.Severity == ValidationSeverity.Error).Select(i => i.Message))}";
                            return false;
                        }

                        if (!validation.IsSolvable)
                        {
                            errorMessage = $"LevelCatalog LevelIndex {entry.LevelIndex} çözülebilir değil!";
                            return false;
                        }
                    }
                }
            }
            else if (levelCatalog == null)
            {
                errorMessage = "Resources/Configs/LevelCatalog.asset bulunamadı!";
                return false;
            }

            // 4. VehicleSkinConfig Kontrolü
            var skinGuids = AssetDatabase.FindAssets("t:VehicleSkinConfig");
            foreach (var guid in skinGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var skin = AssetDatabase.LoadAssetAtPath<VehicleSkinConfig>(path);
                if (skin != null)
                {
                    if (string.IsNullOrEmpty(skin.SkinId))
                    {
                        errorMessage = $"VehicleSkinConfig ({path}) için SkinId boş bırakılamaz!";
                        return false;
                    }
                    if (string.IsNullOrEmpty(skin.DisplayName))
                    {
                        errorMessage = $"VehicleSkinConfig ({path}) için DisplayName boş bırakılamaz!";
                        return false;
                    }
                }
            }

            var stopSkinGuids = AssetDatabase.FindAssets("t:StopSkinConfig");
            foreach (var guid in stopSkinGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var skin = AssetDatabase.LoadAssetAtPath<StopSkinConfig>(path);
                if (skin != null)
                {
                    if (string.IsNullOrEmpty(skin.SkinId))
                    {
                        errorMessage = $"StopSkinConfig ({path}) için SkinId boş bırakılamaz!";
                        return false;
                    }
                    if (string.IsNullOrEmpty(skin.DisplayName))
                    {
                        errorMessage = $"StopSkinConfig ({path}) için DisplayName boş bırakılamaz!";
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
