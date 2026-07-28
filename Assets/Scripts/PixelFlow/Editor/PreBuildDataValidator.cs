using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using PixelFlow.Data;
using UnityEditor;
using UnityEngine;

namespace PixelFlow.Editor
{
    /// <summary>
    /// Pre-build data validator. Runs validation checks against all
    /// ScriptableObject configs and critical data before a build is produced.
    /// Failures produce a descriptive error message listing which configs
    /// are missing or invalid.
    /// </summary>
    public static class PreBuildDataValidator
    {
        /// <summary>
        /// Runs all data validation checks. Returns true if all required
        /// configs are present and valid; false (with a non-null error message)
        /// if any required config is missing or invalid.
        /// </summary>
        public static bool ValidateAllData(out string errorMessage)
        {
            var errors = new List<string>();

            // ---- Check GameConfig ----
            var gameConfigPaths = AssetDatabase.FindAssets("t:GameConfig");
            if (gameConfigPaths.Length == 0)
            {
                errors.Add("GameConfig asset not found. Create one via Assets > Create > PixelFlow > Game Configuration.");
            }
            else if (gameConfigPaths.Length > 1)
            {
                errors.Add($"Multiple GameConfig assets found ({gameConfigPaths.Length}). Only one is allowed.");
            }

            // ---- Check PhaseConfigAsset ----
            var phaseConfigPaths = AssetDatabase.FindAssets("t:PhaseConfigAsset");
            if (phaseConfigPaths.Length == 0)
            {
                errors.Add("PhaseConfigAsset not found. Create one via Assets > Create > PixelFlow > Phase Configuration.");
            }

            // ---- Check LevelCatalogAsset ----
            var levelCatalogPaths = AssetDatabase.FindAssets("t:LevelCatalogAsset");
            if (levelCatalogPaths.Length == 0)
            {
                errors.Add("LevelCatalogAsset not found. Create one via Assets > Create > PixelFlow > Level Catalog.");
            }

            // ---- Check StorageKeysConfigAsset ----
            var storageKeysPaths = AssetDatabase.FindAssets("t:StorageKeysConfigAsset");
            if (storageKeysPaths.Length == 0)
            {
                errors.Add("StorageKeysConfigAsset not found. Create one via Assets > Create > PixelFlow > Storage Keys Configuration.");
            }

            // ---- Check EconomyConfigAsset ----
            var economyPaths = AssetDatabase.FindAssets("t:EconomyConfigAsset");
            if (economyPaths.Length == 0)
            {
                errors.Add("EconomyConfigAsset not found. Create one via Assets > Create > PixelFlow > Economy Configuration.");
            }

            // ---- Check ThemePaletteAsset ----
            var themePaths = AssetDatabase.FindAssets("t:ThemePaletteAsset");
            if (themePaths.Length == 0)
            {
                errors.Add("ThemePaletteAsset not found. Create one via Assets > Create > PixelFlow > Theme Palette.");
            }

            // ---- Check DifficultyFormulaConfigAsset ----
            var difficultyPaths = AssetDatabase.FindAssets("t:DifficultyFormulaConfigAsset");
            if (difficultyPaths.Length == 0)
            {
                errors.Add("DifficultyFormulaConfigAsset not found. Create one via Assets > Create > PixelFlow > Difficulty Formula.");
            }

            // ---- Check StarCriteriaConfigAsset ----
            var starPaths = AssetDatabase.FindAssets("t:StarCriteriaConfigAsset");
            if (starPaths.Length == 0)
            {
                errors.Add("StarCriteriaConfigAsset not found. Create one via Assets > Create > PixelFlow > Star Criteria.");
            }

            if (errors.Count > 0)
            {
                errorMessage = "Pre-build data validation failed:\n" +
                               string.Join("\n", errors.Select((e, i) => $"  {i + 1}. {e}"));
                return false;
            }

            errorMessage = null;
            return true;
        }

        /// <summary>
        /// Validates a GameConfig instance for strict global-release validation.
        /// In EditMode tests, the config has EditorFlags defaulting to strict.
        /// </summary>
        public static bool ValidateGameConfig(GameConfig config, out string errorMessage)
        {
            if (config == null)
            {
                errorMessage = "GameConfig is null.";
                return false;
            }

            var errors = new List<string>();

            if (config.VehicleSpeed <= 0f)
                errors.Add("VehicleSpeed must be positive.");

            if (config.SpawnInterval <= 0f)
                errors.Add("SpawnInterval must be positive.");

            if (config.MaxSimulationSafetyDuration <= 0f)
                errors.Add("MaxSimulationSafetyDuration must be positive.");

            if (config.PathSolverMaxIterations <= 0)
                errors.Add("PathSolverMaxIterations must be positive.");

            if (config.AudioPoolSize <= 0)
                errors.Add("AudioPoolSize must be positive.");

            if (errors.Count > 0)
            {
                errorMessage = "GameConfig validation failed:\n" +
                               string.Join("\n", errors.Select((e, i) => $"  {i + 1}. {e}"));
                return false;
            }

            errorMessage = null;
            return true;
        }
    }
}