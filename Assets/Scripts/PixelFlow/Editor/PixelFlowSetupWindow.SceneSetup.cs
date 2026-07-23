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
            var config = LoadOrCreateConfig();
            var rootObj = FindOrCreateRootObject();
            var root = rootObj.GetComponent<Root>();
            if (root == null)
            {
                root = rootObj.AddComponent<Root>();
            }
            AssignContextData(root, config);

            var canvasObj = FindOrCreateChild(rootObj.transform, "Canvas");
            var canvas = canvasObj.GetComponent<Canvas>();
            if (canvas == null)
            {
                canvas = canvasObj.AddComponent<Canvas>();
            }
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            EnsureComponent<CanvasScaler>(canvasObj, scaler => scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize);
            EnsureComponent<GraphicRaycaster>(canvasObj);

            EnsureEventSystem(rootObj.transform);

            var gridObj = FindOrCreateChild(rootObj.transform, "Grid");
            var gridView = gridObj.GetComponent<GridView>();
            if (gridView == null)
            {
                gridView = gridObj.AddComponent<GridView>();
            }
            EnsureGridBindings(gridView);

            var camObj = FindOrCreateChild(rootObj.transform, "Main Camera");
            camObj.tag = "MainCamera";
            var cam = camObj.GetComponent<Camera>();
            if (cam == null)
            {
                cam = camObj.AddComponent<Camera>();
            }
            cam.orthographic = true;
            cam.orthographicSize = 5f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            EnsureComponent<CameraController>(camObj);

            var hudObj = FindOrCreateChild(canvasObj.transform, "HUD");
            EnsureComponent<HUDView>(hudObj);
            EnsureComponent<CanvasGroup>(hudObj);
            var hudRect = hudObj.GetComponent<RectTransform>();
            if (hudRect == null)
            {
                hudRect = hudObj.AddComponent<RectTransform>();
            }
            hudRect.anchorMin = Vector2.zero;
            hudRect.anchorMax = Vector2.one;
            hudRect.sizeDelta = Vector2.zero;

            var soundObj = FindOrCreateChild(rootObj.transform, "SoundHandler");
            EnsureComponent<SoundHandlerView>(soundObj);

            var themeObj = FindOrCreateChild(rootObj.transform, "ThemeHandler");
            EnsureComponent<ThemeHandlerView>(themeObj);

            var bootObj = FindOrCreateChild(rootObj.transform, "GameBootstrapper");
            var boot = bootObj.GetComponent<GameBootstrapper>();
            if (boot == null)
            {
                boot = bootObj.AddComponent<GameBootstrapper>();
            }
            boot.nexusRoot = root;

            EnsureExtendedViews(canvasObj.transform);

            Selection.activeGameObject = rootObj;
            EditorGUIUtility.PingObject(rootObj);
            Debug.Log("[PixelFlow] Scene setup complete.");
        }

        private GameConfig LoadOrCreateConfig()
        {
            var config = AssetDatabase.LoadAssetAtPath<GameConfig>("Assets/Resources/Configs/GameConfig.asset");
            if (config != null) return config;

            if (!Directory.Exists("Assets/Resources/Configs"))
            {
                Directory.CreateDirectory("Assets/Resources/Configs");
            }

            config = ScriptableObject.CreateInstance<GameConfig>();
            AssetDatabase.CreateAsset(config, "Assets/Resources/Configs/GameConfig.asset");
            AssetDatabase.SaveAssets();
            return config;
        }

        private GameObject FindOrCreateRootObject()
        {
            var existing = Object.FindAnyObjectByType<Root>(FindObjectsInactive.Include);
            if (existing != null)
            {
                return existing.gameObject;
            }

            var byName = GameObject.Find("[PixelFlow]");
            if (byName != null)
            {
                return byName;
            }

            var rootObj = new GameObject("[PixelFlow]");
            Undo.RegisterCreatedObjectUndo(rootObj, "Setup PixelFlow Scene");
            return rootObj;
        }

        private GameObject FindOrCreateChild(Transform parent, string name)
        {
            // Rekürsif arama: tüm alt nesnelerde isim kontrolü
            var allChildren = parent.GetComponentsInChildren<Transform>(true);
            foreach (var child in allChildren)
            {
                if (child != parent && child.name == name)
                {
                    return child.gameObject;
                }
            }

            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            Undo.RegisterCreatedObjectUndo(obj, "Setup PixelFlow Scene Child");
            return obj;
        }

        private void EnsureComponent<T>(GameObject obj) where T : Component
        {
            if (obj.GetComponent<T>() == null)
            {
                obj.AddComponent<T>();
            }
        }

        private void EnsureComponent<T>(GameObject obj, System.Action<T> configure) where T : Component
        {
            var component = obj.GetComponent<T>();
            if (component == null)
            {
                component = obj.AddComponent<T>();
            }
            configure?.Invoke(component);
        }

        private void AssignContextData(Root root, GameConfig config)
        {
            if (root == null || config == null) return;
            var serialized = new SerializedObject(root);
            var property = serialized.FindProperty("contextData");
            if (property != null)
            {
                property.objectReferenceValue = config;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private void EnsureEventSystem(Transform parent)
        {
            var eventSystem = Object.FindAnyObjectByType<EventSystem>(FindObjectsInactive.Include);
            if (eventSystem != null)
            {
                if (eventSystem.transform.parent == null && parent != null)
                {
                    eventSystem.transform.SetParent(parent, false);
                }
                return;
            }

            var esObj = FindOrCreateChild(parent, "EventSystem");
            EnsureComponent<EventSystem>(esObj);
            EnsureComponent<InputSystemUIInputModule>(esObj);
        }

        private void EnsureGridBindings(GridView gridView)
        {
            if (gridView == null) return;
            var gridContainer = gridView.transform.Find("GridContainer") ?? new GameObject("GridContainer").transform;
            gridContainer.SetParent(gridView.transform, false);
            var cellPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/CellView.prefab");
            var serialized = new SerializedObject(gridView);
            var containerProp = serialized.FindProperty("_gridContainer");
            if (containerProp != null)
            {
                containerProp.objectReferenceValue = gridContainer;
            }
            var prefabProp = serialized.FindProperty("_cellPrefab");
            if (prefabProp != null)
            {
                prefabProp.objectReferenceValue = cellPrefab != null ? cellPrefab.GetComponent<CellView>() : null;
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private void EnsureExtendedViews(Transform canvas)
        {
            EnsureViewUnderCanvas<SplashView>(canvas, "SplashView");
            EnsureViewUnderCanvas<BloomFlashView>(canvas, "BloomFlashOverlay", addImage: true);
            EnsureViewUnderCanvas<ConfettiView>(canvas, "ConfettiView");
            EnsureViewUnderCanvas<SettingsView>(canvas, "SettingsView");
            EnsureViewUnderCanvas<DailyCrisisView>(canvas, "DailyCrisisView");
            EnsureViewUnderCanvas<TutorialView>(canvas, "TutorialView");
        }

        private void EnsureViewUnderCanvas<T>(Transform canvas, string name, bool addImage = false) where T : Component
        {
            var existing = Object.FindAnyObjectByType<T>(FindObjectsInactive.Include);
            if (existing != null)
            {
                if (existing.transform.parent != canvas)
                {
                    existing.transform.SetParent(canvas, false);
                }
                return;
            }

            var obj = FindOrCreateChild(canvas, name);
            EnsureComponent<T>(obj);
            if (addImage && obj.GetComponent<Image>() == null)
            {
                var image = obj.AddComponent<Image>();
                image.color = new Color(0, 0, 0, 0);
            }
        }

        private void SetupGlobalVolume()
        {
            var hasVolume = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include)
                .Any(go => go.name.Contains("Volume"));
            if (hasVolume) return;

            var volObj = new GameObject("Global Volume (add Volume component)");
            Debug.Log("[PixelFlow] Global Volume object stub created. Attach a Volume component via Inspector.");
        }

        private void SetupCameraController()
        {
            var camera = Object.FindAnyObjectByType<Camera>(FindObjectsInactive.Include);
            if (camera == null)
            {
                SetupScene();
                camera = Object.FindAnyObjectByType<Camera>(FindObjectsInactive.Include);
            }

            if (camera == null) return;
            if (camera.GetComponent<CameraController>() != null) return;
            camera.gameObject.AddComponent<CameraController>();
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
