#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using Nexus.Core;
using PixelFlow.Views;
using PixelFlow.Data;
using PixelFlow.Services;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace PixelFlow.Editor
{
    partial class PixelFlowSetupWindow
    {
        // ═══════════════════════════════════════════════════
        // SAHNE KURULUM METODLARI
        // ═══════════════════════════════════════════════════

        private void GeneratePrefabs()
        {
            if (!Directory.Exists("Assets/Prefabs")) Directory.CreateDirectory("Assets/Prefabs");

            var existingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/CellView.prefab");
            if (existingPrefab != null) return;

            var cellObj = new GameObject("CellView");
            cellObj.AddComponent<CellView>();
            PrefabUtility.SaveAsPrefabAsset(cellObj, "Assets/Prefabs/CellView.prefab");
            DestroyImmediate(cellObj);
            AssetDatabase.Refresh();
            Debug.Log("[PixelFlow] CellView.prefab created.");
        }

        private void FixCellViewWarningIcon()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/CellView.prefab");
            if (prefab == null || prefab.GetComponent<CellView>() == null) return;

            var cellView = prefab.GetComponent<CellView>();
            var so = new SerializedObject(cellView);
            var warnProp = so.FindProperty("_warningRenderer");
            if (warnProp == null || warnProp.objectReferenceValue != null) return;

            var childRenderers = prefab.GetComponentsInChildren<SpriteRenderer>(true);
            var warningRenderer = childRenderers.FirstOrDefault(r => r.gameObject.name.ToLower().Contains("warning"));
            if (warningRenderer != null)
            {
                warnProp.objectReferenceValue = warningRenderer;
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(cellView);
                Debug.Log("[PixelFlow] CellView warning icon renderer fixed.");
            }
        }

        private void SetupScene()
        {
            var rootObj = new GameObject("[PixelFlow]");
            Undo.RegisterCreatedObjectUndo(rootObj, "Setup PixelFlow Scene");

            // Root + Context
            var root = rootObj.AddComponent<Root>();
            var rootData = AssetDatabase.LoadAssetAtPath<PixelFlow.Data.GameConfig>("Assets/Resources/Configs/GameConfig.asset");
            if (rootData != null)
            {
                var rootSo = new SerializedObject(root);
                rootSo.FindProperty("contextData").objectReferenceValue = rootData;
                rootSo.ApplyModifiedProperties();
            }

            // Canvas
            var canvasObj = new GameObject("Canvas"); canvasObj.transform.SetParent(rootObj.transform);
            canvasObj.AddComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasObj.AddComponent<GraphicRaycaster>();

            // EventSystem
            var esObj = new GameObject("EventSystem"); esObj.transform.SetParent(rootObj.transform);
            var es = esObj.AddComponent<EventSystem>();
            esObj.AddComponent<InputSystemUIInputModule>();

            // GridView
            var gridObj = new GameObject("Grid"); gridObj.transform.SetParent(rootObj.transform);
            var gridView = gridObj.AddComponent<GridView>();
            var gridContainer = new GameObject("GridContainer").transform;
            gridContainer.SetParent(gridObj.transform);
            var cellPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/CellView.prefab");
            var gridSo = new SerializedObject(gridView);
            gridSo.FindProperty("_gridContainer").objectReferenceValue = gridContainer;
            gridSo.FindProperty("_cellPrefab").objectReferenceValue = cellPrefab?.GetComponent<CellView>();
            gridSo.ApplyModifiedProperties();

            // Camera
            var camObj = new GameObject("Main Camera"); camObj.transform.SetParent(rootObj.transform);
            var cam = camObj.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 5f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            camObj.tag = "MainCamera";
            camObj.AddComponent<CameraController>();

            // HUD
            var hudObj = new GameObject("HUD"); hudObj.transform.SetParent(canvasObj.transform);
            hudObj.AddComponent<HUDView>();
            hudObj.AddComponent<CanvasGroup>();
            var hudRect = hudObj.AddComponent<RectTransform>();
            hudRect.anchorMin = Vector2.zero; hudRect.anchorMax = Vector2.one;
            hudRect.sizeDelta = Vector2.zero;

            // Sound & Theme
            var soundObj = new GameObject("SoundHandler"); soundObj.transform.SetParent(rootObj.transform);
            soundObj.AddComponent<SoundHandlerView>();
            var themeObj = new GameObject("ThemeHandler"); themeObj.transform.SetParent(rootObj.transform);
            themeObj.AddComponent<ThemeHandlerView>();

            // GameBootstrapper
            var bootObj = new GameObject("GameBootstrapper"); bootObj.transform.SetParent(rootObj.transform);
            var boot = bootObj.AddComponent<GameBootstrapper>();
            boot.nexusRoot = root;

            // Gerekli View'lar
            var splashObj = new GameObject("SplashView"); splashObj.transform.SetParent(canvasObj.transform);
            splashObj.AddComponent<SplashView>();

            // Bloom flash
            var bloomObj = new GameObject("BloomFlashOverlay"); bloomObj.transform.SetParent(canvasObj.transform);
            bloomObj.AddComponent<BloomFlashView>();
            var bloomImg = bloomObj.AddComponent<Image>();
            bloomImg.color = new Color(0, 0, 0, 0);

            // Confetti
            var confettiObj = new GameObject("ConfettiView"); confettiObj.transform.SetParent(canvasObj.transform);
            confettiObj.AddComponent<ConfettiView>();

            // Settings
            var settingsObj = new GameObject("SettingsView"); settingsObj.transform.SetParent(canvasObj.transform);
            settingsObj.AddComponent<SettingsView>();

            // DailyCrisis
            var crisisObj = new GameObject("DailyCrisisView"); crisisObj.transform.SetParent(canvasObj.transform);
            crisisObj.AddComponent<DailyCrisisView>();

            // Tutorial
            var tutorialObj = new GameObject("TutorialView"); tutorialObj.transform.SetParent(canvasObj.transform);
            tutorialObj.AddComponent<TutorialView>();

            Undo.RegisterFullObjectHierarchyUndo(rootObj, "Setup Scene Objects");
            AssetDatabase.SaveAssets();
            Debug.Log("[PixelFlow] Scene setup complete.");
        }

        private void SetupExtendedViews()
        {
            var canvas = Object.FindAnyObjectByType<Canvas>();
            if (canvas == null) { SetupScene(); return; }

            if (Object.FindAnyObjectByType<DailyCrisisView>(FindObjectsInactive.Include) == null)
                new GameObject("DailyCrisisView").AddComponent<DailyCrisisView>().transform.SetParent(canvas.transform);
            if (Object.FindAnyObjectByType<ConfettiView>(FindObjectsInactive.Include) == null)
                new GameObject("ConfettiView").AddComponent<ConfettiView>().transform.SetParent(canvas.transform);
            if (Object.FindAnyObjectByType<BloomFlashView>(FindObjectsInactive.Include) == null)
                new GameObject("BloomFlashOverlay").AddComponent<BloomFlashView>().transform.SetParent(canvas.transform);
            if (Object.FindAnyObjectByType<TutorialView>(FindObjectsInactive.Include) == null)
                new GameObject("TutorialView").AddComponent<TutorialView>().transform.SetParent(canvas.transform);
            if (Object.FindAnyObjectByType<SettingsView>(FindObjectsInactive.Include) == null)
                new GameObject("SettingsView").AddComponent<SettingsView>().transform.SetParent(canvas.transform);

            Debug.Log("[PixelFlow] Extended views setup complete.");
        }

        private void SetupGlobalVolume()
        {
            // Check by name whether a Global Volume object already exists
            bool hasVolume = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include)
                .Any(go => go.name.Contains("Volume"));
            if (hasVolume) return;

            var volObj = new GameObject("Global Volume (add Volume component)");
            Debug.Log("[PixelFlow] Global Volume object stub created. Attach a Volume component via Inspector.");
        }

        private void SetupCameraController()
        {
            var cam = Camera.main ?? Object.FindAnyObjectByType<Camera>();
            if (cam == null) return;
            if (cam.GetComponent<CameraController>() != null) return;
            cam.gameObject.AddComponent<CameraController>();
            Debug.Log("[PixelFlow] CameraController added.");
        }

        // ═══════════════════════════════════════════════════
        // SEVİYE OLUŞTURMA METODLARI
        // ═══════════════════════════════════════════════════

        private void CreateCustomLevel(int index, int width, int height)
        {
            var level = ScriptableObject.CreateInstance<LevelData>();
            level.levelIndex = index;
            level.width = width;
            level.height = height;
            level.name = $"Level_{index}_{width}x{height}";

            if (!Directory.Exists("Assets/Resources/Levels")) Directory.CreateDirectory("Assets/Resources/Levels");
            string path = $"Assets/Resources/Levels/Level{index}.asset";
            AssetDatabase.CreateAsset(level, path);
            AssetDatabase.SaveAssets();
            Debug.Log($"[PixelFlow] Level {index} ({width}x{height}) created at {path}");
        }

        private void CreateThreeLevelPack()
        {
            CreatePhase1And2HandCraftedPack(); // Varsayılan olarak 3 seviyeli mini paket
        }

        private void CreatePhase1And2HandCraftedPack()
        {
            if (!Directory.Exists("Assets/Resources/Levels")) Directory.CreateDirectory("Assets/Resources/Levels");

            var levelConfigs = new (int index, int w, int h)[]
            {
                (0, 4, 4), (1, 4, 4), (2, 5, 5), (3, 5, 5), (4, 5, 5),
                (5, 6, 6), (6, 6, 6), (7, 7, 7), (8, 7, 7), (9, 8, 8),
                (10, 8, 8), (11, 9, 9)
            };

            foreach (var (index, w, h) in levelConfigs)
            {
                string path = $"Assets/Resources/Levels/Level{index + 1}.asset";
                if (File.Exists(path)) continue;

                var level = ScriptableObject.CreateInstance<LevelData>();
                level.levelIndex = index;
                level.width = w;
                level.height = h;
                level.name = $"Level{index + 1}";
                AssetDatabase.CreateAsset(level, path);
            }

            AssetDatabase.SaveAssets();
            Debug.Log("[PixelFlow] Phase 1+2 hand-crafted level pack created (12 levels).");
        }

        private void GenerateProceduralLevel(int difficultyIndex, int? seed, int levelIndex)
        {
            var param = new DifficultyParams(
                width: 5 + difficultyIndex * 2,
                height: 5 + difficultyIndex * 2,
                colors: 2 + difficultyIndex,
                bridges: 1 + difficultyIndex,
                fullCoverage: difficultyIndex >= 2,
                obstacles: difficultyIndex >= 1,
                ferry: difficultyIndex >= 2,
                narrow: difficultyIndex >= 3);

            var generator = new ProceduralLevelGenerator(new RuntimePathSolver());
            var level = generator.Generate(param);
            if (level == null) { Debug.LogError("[PixelFlow] Procedural generation failed."); return; }

            level.levelIndex = levelIndex;
            if (!Directory.Exists("Assets/Resources/Levels")) Directory.CreateDirectory("Assets/Resources/Levels");
            string path = $"Assets/Resources/Levels/Level{levelIndex + 1}.asset";
            AssetDatabase.CreateAsset(level, path);
            AssetDatabase.SaveAssets();
            Debug.Log($"[PixelFlow] Procedural Level {levelIndex + 1} generated.");
        }

        private void GenerateProceduralBatch(int difficultyIndex, int? seed, int startIndex, int count)
        {
            for (int i = 0; i < count; i++)
                GenerateProceduralLevel(difficultyIndex, seed, startIndex + i);
            AssetDatabase.SaveAssets();
            Debug.Log($"[PixelFlow] Batch generation complete: {count} levels.");
        }

        // ═══════════════════════════════════════════════════
        // SEVİYE KOPYALAMA / DUPLİKASYON
        // ═══════════════════════════════════════════════════

        /// <summary>
        /// Var olan bir LevelData'yı derin kopyalayıp yeni index'e kaydeder.
        /// </summary>
        private void DuplicateLevel(int sourceIndex, int targetIndex)
        {
            // Kaynak seviyeyi bul
            var source = _cachedLevels.FirstOrDefault(l => l != null && l.levelIndex == sourceIndex);
            if (source == null)
            {
                Debug.LogWarning($"[PixelFlow] Duplicate: Source level {sourceIndex} bulunamadı!");
                return;
            }

            // Hedef path çakışma kontrolü
            string targetPath = $"Assets/Resources/Levels/Level{targetIndex}.asset";
            if (File.Exists(targetPath))
            {
                if (!EditorUtility.DisplayDialog("Seviye Zaten Var",
                    $"Level{targetIndex}.asset zaten mevcut. Üzerine yazılsın mı?",
                    "Evet", "Hayır"))
                    return;
                AssetDatabase.DeleteAsset(targetPath);
            }

            // JSON ile derin kopyalama (ScriptableObject.Copy İÇERİĞİ kopyalamaz, referans kopyalar)
            string json = EditorJsonUtility.ToJson(source);
            var duplicate = ScriptableObject.CreateInstance<LevelData>();
            EditorJsonUtility.FromJsonOverwrite(json, duplicate);

            // Yeni identity ata
            duplicate.levelIndex = targetIndex;
            duplicate.name = $"Level{targetIndex}";

            if (!Directory.Exists("Assets/Resources/Levels"))
                Directory.CreateDirectory("Assets/Resources/Levels");

            AssetDatabase.CreateAsset(duplicate, targetPath);
            AssetDatabase.SaveAssets();
            Debug.Log($"[PixelFlow] Level {sourceIndex} → {targetIndex} kopyalandı: {targetPath}");
        }

        /// <summary>
        /// Bir kaynaktan batch halinde seviye kopyalama.
        /// </summary>
        private void DuplicateLevelBatch(int sourceIndex, int startTargetIndex, int count)
        {
            for (int i = 0; i < count; i++)
            {
                DuplicateLevel(sourceIndex, startTargetIndex + i);
            }
            AssetDatabase.SaveAssets();
            RefreshData();
            Debug.Log($"[PixelFlow] Batch duplicate: Level {sourceIndex} → {startTargetIndex}-{startTargetIndex + count - 1} ({count} adet)");
        }
    }
}
#endif
