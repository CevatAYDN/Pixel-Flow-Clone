#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Nexus.Core;
using PixelFlow.Views;
using PixelFlow.Data;
using System.IO;

namespace PixelFlow.Editor
{
    public class PixelFlowSetupWindow : EditorWindow
    {
        [MenuItem("Pixel Flow/Setup Helper")]
        public static void ShowWindow()
        {
            GetWindow<PixelFlowSetupWindow>("Pixel Flow Setup");
        }

        private void OnGUI()
        {
            GUILayout.Label("Pixel Flow Scene Setup", EditorStyles.boldLabel);

            if (GUILayout.Button("1. Generate Base Prefabs"))
            {
                GeneratePrefabs();
            }

            if (GUILayout.Button("2. Setup Scene (Context, Grid, UI)"))
            {
                SetupScene();
            }
            
            GUILayout.Space(20);
            GUILayout.Label("Level Management", EditorStyles.boldLabel);

            if (GUILayout.Button("Create Empty LevelData"))
            {
                CreateLevelData();
            }

            GUILayout.Space(10);
            if (GUILayout.Button("Create Sample 5x5 Level"))
            {
                CreateSampleLevel();
            }

            GUILayout.Space(10);
            if (GUILayout.Button("Generate 3-Level Pack and LevelPack"))
            {
                CreateThreeLevelPack();
            }
        }



        private void GeneratePrefabs()
        {
            Debug.Log("[PixelFlowSetupWindow] Generating Base Prefabs...");
            if (!Directory.Exists("Assets/Prefabs"))
            {
                Directory.CreateDirectory("Assets/Prefabs");
                Debug.Log("[PixelFlowSetupWindow] Created Assets/Prefabs directory.");
            }

            string cellPrefabPath = "Assets/Prefabs/CellView.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(cellPrefabPath) == null)
            {
                GameObject cellObj = new GameObject("CellView");
                var cellView = cellObj.AddComponent<CellView>();
                cellObj.AddComponent<BoxCollider2D>(); 
                Debug.Log("[PixelFlowSetupWindow] Created CellView GameObject and added BoxCollider2D.");
                
                GameObject bgObj = new GameObject("Background");
                bgObj.transform.SetParent(cellObj.transform);
                var bgRenderer = bgObj.AddComponent<SpriteRenderer>();
                Debug.Log("[PixelFlowSetupWindow] Created Background child and added SpriteRenderer.");

                GameObject dotObj = new GameObject("Dot");
                dotObj.transform.SetParent(cellObj.transform);
                var dotRenderer = dotObj.AddComponent<SpriteRenderer>();
                Debug.Log("[PixelFlowSetupWindow] Created Dot child and added SpriteRenderer.");

                GameObject bridgeObj = new GameObject("Bridge");
                bridgeObj.transform.SetParent(cellObj.transform);
                var bridgeRenderer = bridgeObj.AddComponent<SpriteRenderer>();
                Debug.Log("[PixelFlowSetupWindow] Created Bridge child and added SpriteRenderer.");

                SerializedObject so = new SerializedObject(cellView);
                so.FindProperty("_bgRenderer").objectReferenceValue = bgRenderer;
                so.FindProperty("_dotRenderer").objectReferenceValue = dotRenderer;
                so.FindProperty("_bridgeRenderer").objectReferenceValue = bridgeRenderer;
                so.ApplyModifiedProperties();
                Debug.Log("[PixelFlowSetupWindow] Assigned CellView renderer references in SerializedObject.");

                PrefabUtility.SaveAsPrefabAsset(cellObj, cellPrefabPath);
                DestroyImmediate(cellObj);
                Debug.Log("[PixelFlowSetupWindow] CellView prefab created at: " + cellPrefabPath);
            }
            else
            {
                Debug.Log("[PixelFlowSetupWindow] CellView prefab already exists.");
            }
        }

        private void SetupScene()
        {
            Debug.Log("[PixelFlowSetupWindow] Starting scene setup...");

            // Check for duplicates
            var roots = Object.FindObjectsByType<Root>();
            if (roots.Length > 1)
            {
                Debug.LogWarning($"[PixelFlowSetupWindow] WARNING: Multiple Root components found in scene ({roots.Length})!");
            }
            var gridViews = Object.FindObjectsByType<GridView>();
            if (gridViews.Length > 1)
            {
                Debug.LogWarning($"[PixelFlowSetupWindow] WARNING: Multiple GridView components found in scene ({gridViews.Length})!");
            }
            var canvases = Object.FindObjectsByType<Canvas>();
            if (canvases.Length > 1)
            {
                Debug.LogWarning($"[PixelFlowSetupWindow] WARNING: Multiple Canvas components found in scene ({canvases.Length})!");
            }

            // 1. Context setup
            Root context = Object.FindAnyObjectByType<Root>();
            if (context == null)
            {
                GameObject contextObj = new GameObject("PixelFlow_Context");
                context = contextObj.AddComponent<Root>();
                contextObj.AddComponent<GameContextLifecycle>();
                Undo.RegisterCreatedObjectUndo(contextObj, "Create Context");
                Debug.Log("[PixelFlowSetupWindow] Created PixelFlow_Context GameObject with Root and GameContextLifecycle components.");
            }
            else
            {
                Debug.Log("[PixelFlowSetupWindow] Found existing PixelFlow_Context in the scene.");
            }

            if (context != null && context.ContextData == null)
            {
                string settingsFolder = "Assets/Settings";
                if (!AssetDatabase.IsValidFolder(settingsFolder))
                {
                    AssetDatabase.CreateFolder("Assets", "Settings");
                }

                string assetPath = "Assets/Settings/PixelFlowContextData.asset";
                ContextData contextDataAsset = AssetDatabase.LoadAssetAtPath<ContextData>(assetPath);
                if (contextDataAsset == null)
                {
                    contextDataAsset = ScriptableObject.CreateInstance<ContextData>();
                    AssetDatabase.CreateAsset(contextDataAsset, assetPath);
                    AssetDatabase.SaveAssets();
                    Debug.Log($"[PixelFlowSetupWindow] Created new ContextData asset at {assetPath}");
                }

                SerializedObject serializedContext = new SerializedObject(context);
                SerializedProperty contextDataProp = serializedContext.FindProperty("contextData");
                if (contextDataProp != null)
                {
                    serializedContext.Update();
                    contextDataProp.objectReferenceValue = contextDataAsset;
                    serializedContext.ApplyModifiedProperties();
                    EditorUtility.SetDirty(context);
                    Debug.Log("[PixelFlowSetupWindow] Assigned PixelFlowContextData configuration to the Root component.");
                }
            }

            // 2. GridView setup
            GridView gridView = Object.FindAnyObjectByType<GridView>();
            GameObject gridObj;
            if (gridView == null)
            {
                gridObj = new GameObject("GridView");
                gridView = gridObj.AddComponent<GridView>();
                Undo.RegisterCreatedObjectUndo(gridObj, "Create GridView");
                Debug.Log("[PixelFlowSetupWindow] Created GridView GameObject.");
            }
            else
            {
                gridObj = gridView.gameObject;
                Debug.Log("[PixelFlowSetupWindow] Found existing GridView GameObject.");
            }

            // GridView children setup
            Transform container = gridObj.transform.Find("CellsContainer");
            if (container == null)
            {
                GameObject containerObj = new GameObject("CellsContainer");
                container = containerObj.transform;
                container.SetParent(gridObj.transform);
                Debug.Log("[PixelFlowSetupWindow] Created CellsContainer child under GridView.");
            }
            else
            {
                Debug.Log("[PixelFlowSetupWindow] Found existing CellsContainer child under GridView.");
            }

            // Load and assign CellView prefab
            CellView cellPrefab = AssetDatabase.LoadAssetAtPath<CellView>("Assets/Prefabs/CellView.prefab");
            if (cellPrefab == null)
            {
                Debug.LogError("[PixelFlowSetupWindow] CellView prefab not found at Assets/Prefabs/CellView.prefab! Generate prefabs first.");
            }
            else
            {
                Debug.Log("[PixelFlowSetupWindow] Successfully loaded CellView prefab.");
            }

            SerializedObject gridSo = new SerializedObject(gridView);
            gridSo.FindProperty("_gridContainer").objectReferenceValue = container;
            gridSo.FindProperty("_cellPrefab").objectReferenceValue = cellPrefab;
            gridSo.ApplyModifiedProperties();
            EditorUtility.SetDirty(gridView);
            Debug.Log($"[PixelFlowSetupWindow] Assigned GridView references. _gridContainer: {container.name}, _cellPrefab: {(cellPrefab != null ? cellPrefab.name : "null")}");

            // 3. UI setup (Canvas & HUDView)
            Canvas canvas = Object.FindAnyObjectByType<Canvas>();
            GameObject canvasObj;
            if (canvas == null)
            {
                canvasObj = new GameObject("Canvas");
                canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasObj.AddComponent<CanvasScaler>();
                canvasObj.AddComponent<GraphicRaycaster>();
                Undo.RegisterCreatedObjectUndo(canvasObj, "Create Canvas");
                Debug.Log("[PixelFlowSetupWindow] Created Canvas GameObject with Canvas, CanvasScaler, and GraphicRaycaster components.");
            }
            else
            {
                canvasObj = canvas.gameObject;
                Debug.Log("[PixelFlowSetupWindow] Found existing Canvas GameObject.");
            }

            var eventSystem = Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>();
            if (eventSystem == null)
            {
                GameObject eventSystemObj = new GameObject("EventSystem");
                eventSystemObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
                eventSystemObj.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
                Undo.RegisterCreatedObjectUndo(eventSystemObj, "Create EventSystem");
                Debug.Log("[PixelFlowSetupWindow] Created EventSystem GameObject with InputSystemUIInputModule.");
            }
            else
            {
                var standaloneModule = eventSystem.GetComponent<UnityEngine.EventSystems.StandaloneInputModule>();
                if (standaloneModule != null)
                {
                    Undo.DestroyObjectImmediate(standaloneModule);
                    eventSystem.gameObject.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
                    Debug.Log("[PixelFlowSetupWindow] Replaced StandaloneInputModule with InputSystemUIInputModule on EventSystem.");
                }
            }

            HUDView hudView = Object.FindAnyObjectByType<HUDView>();
            GameObject hudObj;
            if (hudView == null)
            {
                hudObj = new GameObject("HUDView", typeof(RectTransform));
                hudObj.transform.SetParent(canvasObj.transform, false);
                hudView = hudObj.AddComponent<HUDView>();
                RectTransform hudRect = hudObj.GetComponent<RectTransform>();
                hudRect.anchorMin = Vector2.zero;
                hudRect.anchorMax = Vector2.one;
                hudRect.sizeDelta = Vector2.zero;
                Debug.Log("[PixelFlowSetupWindow] Created HUDView GameObject under Canvas.");
            }
            else
            {
                hudObj = hudView.gameObject;
                hudObj.transform.SetParent(canvasObj.transform, false);
                Debug.Log("[PixelFlowSetupWindow] Found existing HUDView GameObject.");
            }

            // Hint Button setup
            Transform hintBtnTransform = hudObj.transform.Find("HintButton");
            GameObject hintBtnObj;
            if (hintBtnTransform == null)
            {
                hintBtnObj = new GameObject("HintButton", typeof(RectTransform));
                hintBtnObj.transform.SetParent(hudObj.transform, false);
                Debug.Log("[PixelFlowSetupWindow] Created HintButton GameObject under HUDView.");
            }
            else
            {
                hintBtnObj = hintBtnTransform.gameObject;
                Debug.Log("[PixelFlowSetupWindow] Found existing HintButton GameObject under HUDView.");
            }

            Image hintImg = hintBtnObj.GetComponent<Image>();
            if (hintImg == null) hintImg = hintBtnObj.AddComponent<Image>();
            hintImg.color = new Color(0.15f, 0.15f, 0.18f, 1f);

            Button hintBtn = hintBtnObj.GetComponent<Button>();
            if (hintBtn == null) hintBtn = hintBtnObj.AddComponent<Button>();

            RectTransform hintRect = hintBtnObj.GetComponent<RectTransform>();
            hintRect.anchorMin = new Vector2(0.5f, 0f);
            hintRect.anchorMax = new Vector2(0.5f, 0f);
            hintRect.pivot = new Vector2(0.5f, 0f);
            hintRect.anchoredPosition = new Vector2(0f, 60f);
            hintRect.sizeDelta = new Vector2(160f, 50f);

            // Hint Count Text setup
            Transform hintTextTransform = hintBtnObj.transform.Find("HintCountText");
            GameObject hintTextObj;
            if (hintTextTransform == null)
            {
                hintTextObj = new GameObject("HintCountText", typeof(RectTransform));
                hintTextObj.transform.SetParent(hintBtnObj.transform, false);
                Debug.Log("[PixelFlowSetupWindow] Created HintCountText GameObject under HintButton.");
            }
            else
            {
                hintTextObj = hintTextTransform.gameObject;
                Debug.Log("[PixelFlowSetupWindow] Found existing HintCountText GameObject.");
            }

            Text hintText = hintTextObj.GetComponent<Text>();
            if (hintText == null) hintText = hintTextObj.AddComponent<Text>();
            hintText.text = "HINT (3)";
            hintText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            hintText.fontSize = 18;
            hintText.alignment = TextAnchor.MiddleCenter;
            hintText.color = Color.white;

            RectTransform textRect = hintTextObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;

            // Completion Panel setup
            Transform compPanelTransform = hudObj.transform.Find("CompletionPanel");
            GameObject completionPanel;
            if (compPanelTransform == null)
            {
                completionPanel = new GameObject("CompletionPanel", typeof(RectTransform));
                completionPanel.transform.SetParent(hudObj.transform, false);
                Debug.Log("[PixelFlowSetupWindow] Created CompletionPanel GameObject under HUDView.");
            }
            else
            {
                completionPanel = compPanelTransform.gameObject;
                Debug.Log("[PixelFlowSetupWindow] Found existing CompletionPanel GameObject.");
            }

            Image panelImg = completionPanel.GetComponent<Image>();
            if (panelImg == null) panelImg = completionPanel.AddComponent<Image>();
            panelImg.color = new Color(0.08f, 0.08f, 0.1f, 0.85f);

            RectTransform panelRect = completionPanel.GetComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.sizeDelta = Vector2.zero;
            completionPanel.SetActive(false);

            // Completion Text setup
            Transform compTextTransform = completionPanel.transform.Find("CompletionText");
            GameObject completionTextObj;
            if (compTextTransform == null)
            {
                completionTextObj = new GameObject("CompletionText", typeof(RectTransform));
                completionTextObj.transform.SetParent(completionPanel.transform, false);
                Debug.Log("[PixelFlowSetupWindow] Created CompletionText GameObject under CompletionPanel.");
            }
            else
            {
                completionTextObj = compTextTransform.gameObject;
                Debug.Log("[PixelFlowSetupWindow] Found existing CompletionText GameObject.");
            }

            Text compText = completionTextObj.GetComponent<Text>();
            if (compText == null) compText = completionTextObj.AddComponent<Text>();
            compText.text = "Tebrikler!";
            compText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            compText.fontSize = 32;
            compText.color = new Color(0.2f, 0.85f, 0.3f);
            compText.alignment = TextAnchor.MiddleCenter;

            RectTransform compTextRect = completionTextObj.GetComponent<RectTransform>();
            compTextRect.anchorMin = new Vector2(0f, 0.6f);
            compTextRect.anchorMax = new Vector2(1f, 0.9f);
            compTextRect.sizeDelta = Vector2.zero;

            // Next Level Button setup
            Transform nextLvlBtnTransform = completionPanel.transform.Find("NextLevelButton");
            GameObject nextLvlBtnObj;
            if (nextLvlBtnTransform == null)
            {
                nextLvlBtnObj = new GameObject("NextLevelButton", typeof(RectTransform));
                nextLvlBtnObj.transform.SetParent(completionPanel.transform, false);
                Debug.Log("[PixelFlowSetupWindow] Created NextLevelButton GameObject under CompletionPanel.");
            }
            else
            {
                nextLvlBtnObj = nextLvlBtnTransform.gameObject;
                Debug.Log("[PixelFlowSetupWindow] Found existing NextLevelButton GameObject under CompletionPanel.");
            }

            Image nextLvlImg = nextLvlBtnObj.GetComponent<Image>();
            if (nextLvlImg == null) nextLvlImg = nextLvlBtnObj.AddComponent<Image>();
            nextLvlImg.color = new Color(0.15f, 0.6f, 0.25f, 1f); // Nice green button

            Button nextLvlBtn = nextLvlBtnObj.GetComponent<Button>();
            if (nextLvlBtn == null) nextLvlBtn = nextLvlBtnObj.AddComponent<Button>();

            RectTransform nextLvlRect = nextLvlBtnObj.GetComponent<RectTransform>();
            nextLvlRect.anchorMin = new Vector2(0.5f, 0.4f);
            nextLvlRect.anchorMax = new Vector2(0.5f, 0.4f);
            nextLvlRect.pivot = new Vector2(0.5f, 0.5f);
            nextLvlRect.anchoredPosition = new Vector2(0f, 0f);
            nextLvlRect.sizeDelta = new Vector2(180f, 50f);

            // Next Level Button Text
            Transform nextLvlTextTransform = nextLvlBtnObj.transform.Find("Text");
            GameObject nextLvlTextObj;
            if (nextLvlTextTransform == null)
            {
                nextLvlTextObj = new GameObject("Text", typeof(RectTransform));
                nextLvlTextObj.transform.SetParent(nextLvlBtnObj.transform, false);
            }
            else
            {
                nextLvlTextObj = nextLvlTextTransform.gameObject;
            }

            Text nextLvlText = nextLvlTextObj.GetComponent<Text>();
            if (nextLvlText == null) nextLvlText = nextLvlTextObj.AddComponent<Text>();
            nextLvlText.text = "SONRAKİ SEVİYE";
            nextLvlText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            nextLvlText.fontSize = 18;
            nextLvlText.alignment = TextAnchor.MiddleCenter;
            nextLvlText.color = Color.white;

            RectTransform nextLvlTextRect = nextLvlTextObj.GetComponent<RectTransform>();
            nextLvlTextRect.anchorMin = Vector2.zero;
            nextLvlTextRect.anchorMax = Vector2.one;
            nextLvlTextRect.sizeDelta = Vector2.zero;

            SerializedObject hudSo = new SerializedObject(hudView);
            hudSo.FindProperty("_hintButton").objectReferenceValue = hintBtn;
            hudSo.FindProperty("_hintCountText").objectReferenceValue = hintText;
            hudSo.FindProperty("_completionPanel").objectReferenceValue = completionPanel;
            hudSo.FindProperty("_completionText").objectReferenceValue = compText;
            hudSo.FindProperty("_nextLevelButton").objectReferenceValue = nextLvlBtn;
            hudSo.ApplyModifiedProperties();
            EditorUtility.SetDirty(hudView);
            Debug.Log("[PixelFlowSetupWindow] Assigned HUDView references to SerializedObject and marked HUDView dirty.");

            // 4. SoundHandlerView setup
            SoundHandlerView soundView = Object.FindAnyObjectByType<SoundHandlerView>();
            if (soundView == null)
            {
                GameObject soundObj = new GameObject("SoundHandlerView");
                soundView = soundObj.AddComponent<SoundHandlerView>();
                Undo.RegisterCreatedObjectUndo(soundObj, "Create SoundHandlerView");
                Debug.Log("[PixelFlowSetupWindow] Created SoundHandlerView GameObject.");
            }
            else
            {
                Debug.Log("[PixelFlowSetupWindow] Found existing SoundHandlerView in the scene.");
            }

            // 5. ThemeHandlerView setup
            ThemeHandlerView themeView = Object.FindAnyObjectByType<ThemeHandlerView>();
            if (themeView == null)
            {
                GameObject themeObj = new GameObject("ThemeHandlerView");
                themeView = themeObj.AddComponent<ThemeHandlerView>();
                Undo.RegisterCreatedObjectUndo(themeObj, "Create ThemeHandlerView");
                Debug.Log("[PixelFlowSetupWindow] Created ThemeHandlerView GameObject.");
            }
            else
            {
                Debug.Log("[PixelFlowSetupWindow] Found existing ThemeHandlerView in the scene.");
            }

            // 6. GameBootstrapper setup
            GameBootstrapper bootstrapper = Object.FindAnyObjectByType<GameBootstrapper>();
            if (bootstrapper == null)
            {
                GameObject bootObj = new GameObject("GameBootstrapper");
                bootstrapper = bootObj.AddComponent<GameBootstrapper>();
                Undo.RegisterCreatedObjectUndo(bootObj, "Create GameBootstrapper");
                Debug.Log("[PixelFlowSetupWindow] Created GameBootstrapper GameObject.");
            }
            else
            {
                Debug.Log("[PixelFlowSetupWindow] Found existing GameBootstrapper in the scene.");
            }

            // Assign level if missing
            if (bootstrapper.initialLevel == null)
            {
                string[] guids = AssetDatabase.FindAssets("t:LevelData");
                if (guids.Length > 0)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                    bootstrapper.initialLevel = AssetDatabase.LoadAssetAtPath<LevelData>(path);
                    Debug.Log($"[PixelFlowSetupWindow] Assigned initialLevel to GameBootstrapper: {bootstrapper.initialLevel.name} from {path}");
                    
                    // Auto-populate Level1 if empty
                    if (bootstrapper.initialLevel.initialNodes == null || bootstrapper.initialLevel.initialNodes.Count == 0)
                    {
                        CreateSampleLevel(bootstrapper.initialLevel, path);
                    }
                }
                else
                {
                    Debug.LogWarning("[PixelFlowSetupWindow] No LevelData found in project resources to assign to GameBootstrapper.");
                }
            }
            
            // Assign nexusRoot reference on bootstrapper
            if (bootstrapper.nexusRoot == null)
            {
                bootstrapper.nexusRoot = context;
                Debug.Log("[PixelFlowSetupWindow] Assigned nexusRoot reference to GameBootstrapper.");
            }
            EditorUtility.SetDirty(bootstrapper);

            // 7. Main Camera setup
            if (Camera.main != null)
            {
                Camera.main.orthographic = true;
                Camera.main.orthographicSize = 5;
                EditorUtility.SetDirty(Camera.main);
                Debug.Log("[PixelFlowSetupWindow] Main Camera set to orthographic with size 5.");
            }

            // 8. Mark the active scene dirty so changes are saved
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            Debug.Log("[PixelFlowSetupWindow] Scene marked as dirty. Setup completed successfully.");
        }

        private void CreateLevelData()
        {
            if (!Directory.Exists("Assets/Resources/Levels"))
            {
                Directory.CreateDirectory("Assets/Resources/Levels");
            }

            LevelData asset = ScriptableObject.CreateInstance<LevelData>();
            string path = AssetDatabase.GenerateUniqueAssetPath("Assets/Resources/Levels/NewLevelData.asset");
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();

            EditorUtility.FocusProjectWindow();
            Selection.activeObject = asset;
            Debug.Log("Created LevelData at " + path);
        }
        
        private void CreateSampleLevel()
        {
            LevelData level = null;
            string levelPath = "Assets/Resources/Levels/Level1.asset";
            
            if (File.Exists(levelPath))
            {
                level = AssetDatabase.LoadAssetAtPath<LevelData>(levelPath);
            }
            else
            {
                string[] guids = AssetDatabase.FindAssets("t:LevelData", new[] { "Assets/Resources/Levels" });
                if (guids.Length == 0)
                {
                    Debug.LogError("[PixelFlowSetupWindow] No LevelData found in Resources/Levels. Create one first.");
                    return;
                }
                levelPath = AssetDatabase.GUIDToAssetPath(guids[0]);
                level = AssetDatabase.LoadAssetAtPath<LevelData>(levelPath);
            }
            
            if (level != null)
            {
                PopulateSampleLevel(level);
                EditorUtility.SetDirty(level);
                AssetDatabase.SaveAssets();
                Debug.Log($"[PixelFlowSetupWindow] Sample level populated: {level.name} (5x5, 2 colors)");
            }
        }
        
        private void CreateSampleLevel(LevelData level, string assetPath)
        {
            PopulateSampleLevel(level);
            EditorUtility.SetDirty(level);
            AssetDatabase.SaveAssets();
            Debug.Log($"[PixelFlowSetupWindow] Auto-populated level: {level.name} ({assetPath})");
        }
        
        private void PopulateSampleLevel(LevelData level)
        {
            level.width = 5;
            level.height = 5;
            
            level.initialNodes = new System.Collections.Generic.List<PixelFlow.Data.GridNode>
            {
                new PixelFlow.Data.GridNode { position = new Vector2Int(0, 0), color = PixelFlow.Data.ColorType.Red },
                new PixelFlow.Data.GridNode { position = new Vector2Int(4, 0), color = PixelFlow.Data.ColorType.Red },
                new PixelFlow.Data.GridNode { position = new Vector2Int(0, 4), color = PixelFlow.Data.ColorType.Blue },
                new PixelFlow.Data.GridNode { position = new Vector2Int(4, 4), color = PixelFlow.Data.ColorType.Blue }
            };
            
            level.solutions = new System.Collections.Generic.List<PathSolution>
            {
                new PathSolution
                {
                    color = PixelFlow.Data.ColorType.Red,
                    pathPositions = new System.Collections.Generic.List<Vector2Int>
                    {
                        new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0), new Vector2Int(3, 0), new Vector2Int(4, 0)
                    }
                },
                new PathSolution
                {
                    color = PixelFlow.Data.ColorType.Blue,
                    pathPositions = new System.Collections.Generic.List<Vector2Int>
                    {
                        new Vector2Int(0, 4), new Vector2Int(1, 4), new Vector2Int(2, 4), new Vector2Int(3, 4), new Vector2Int(4, 4)
                    }
                }
            };
            level.bridgePositions = new System.Collections.Generic.List<Vector2Int>();
        }

        private void CreateThreeLevelPack()
        {
            if (!Directory.Exists("Assets/Resources/Levels"))
            {
                Directory.CreateDirectory("Assets/Resources/Levels");
            }

            // Level 1
            LevelData lvl1 = ScriptableObject.CreateInstance<LevelData>();
            lvl1.levelIndex = 0;
            lvl1.width = 5;
            lvl1.height = 5;
            lvl1.initialNodes = new System.Collections.Generic.List<GridNode>
            {
                new GridNode { position = new Vector2Int(0, 0), color = ColorType.Red },
                new GridNode { position = new Vector2Int(4, 0), color = ColorType.Red },
                new GridNode { position = new Vector2Int(0, 1), color = ColorType.Blue },
                new GridNode { position = new Vector2Int(0, 4), color = ColorType.Blue },
                new GridNode { position = new Vector2Int(1, 1), color = ColorType.Green },
                new GridNode { position = new Vector2Int(4, 1), color = ColorType.Green },
                new GridNode { position = new Vector2Int(1, 2), color = ColorType.Yellow },
                new GridNode { position = new Vector2Int(4, 2), color = ColorType.Yellow },
                new GridNode { position = new Vector2Int(1, 3), color = ColorType.Orange },
                new GridNode { position = new Vector2Int(4, 3), color = ColorType.Orange },
                new GridNode { position = new Vector2Int(1, 4), color = ColorType.Purple },
                new GridNode { position = new Vector2Int(4, 4), color = ColorType.Purple }
            };
            lvl1.solutions = new System.Collections.Generic.List<PathSolution>
            {
                new PathSolution { color = ColorType.Red, pathPositions = new System.Collections.Generic.List<Vector2Int> { new Vector2Int(0,0), new Vector2Int(1,0), new Vector2Int(2,0), new Vector2Int(3,0), new Vector2Int(4,0) } },
                new PathSolution { color = ColorType.Blue, pathPositions = new System.Collections.Generic.List<Vector2Int> { new Vector2Int(0,1), new Vector2Int(0,2), new Vector2Int(0,3), new Vector2Int(0,4) } },
                new PathSolution { color = ColorType.Green, pathPositions = new System.Collections.Generic.List<Vector2Int> { new Vector2Int(1,1), new Vector2Int(2,1), new Vector2Int(3,1), new Vector2Int(4,1) } },
                new PathSolution { color = ColorType.Yellow, pathPositions = new System.Collections.Generic.List<Vector2Int> { new Vector2Int(1,2), new Vector2Int(2,2), new Vector2Int(3,2), new Vector2Int(4,2) } },
                new PathSolution { color = ColorType.Orange, pathPositions = new System.Collections.Generic.List<Vector2Int> { new Vector2Int(1,3), new Vector2Int(2,3), new Vector2Int(3,3), new Vector2Int(4,3) } },
                new PathSolution { color = ColorType.Purple, pathPositions = new System.Collections.Generic.List<Vector2Int> { new Vector2Int(1,4), new Vector2Int(2,4), new Vector2Int(3,4), new Vector2Int(4,4) } }
            };
            AssetDatabase.CreateAsset(lvl1, "Assets/Resources/Levels/Level1.asset");

            // Level 2
            LevelData lvl2 = ScriptableObject.CreateInstance<LevelData>();
            lvl2.levelIndex = 1;
            lvl2.width = 5;
            lvl2.height = 5;
            lvl2.initialNodes = new System.Collections.Generic.List<GridNode>
            {
                new GridNode { position = new Vector2Int(0, 0), color = ColorType.Red },
                new GridNode { position = new Vector2Int(4, 0), color = ColorType.Red },
                new GridNode { position = new Vector2Int(0, 2), color = ColorType.Blue },
                new GridNode { position = new Vector2Int(4, 4), color = ColorType.Blue },
                new GridNode { position = new Vector2Int(1, 2), color = ColorType.Green },
                new GridNode { position = new Vector2Int(4, 1), color = ColorType.Green },
                new GridNode { position = new Vector2Int(3, 2), color = ColorType.Yellow },
                new GridNode { position = new Vector2Int(4, 2), color = ColorType.Yellow }
            };
            lvl2.solutions = new System.Collections.Generic.List<PathSolution>
            {
                new PathSolution { color = ColorType.Red, pathPositions = new System.Collections.Generic.List<Vector2Int> { new Vector2Int(0,0), new Vector2Int(0,1), new Vector2Int(1,1), new Vector2Int(1,0), new Vector2Int(2,0), new Vector2Int(3,0), new Vector2Int(4,0) } },
                new PathSolution { color = ColorType.Blue, pathPositions = new System.Collections.Generic.List<Vector2Int> { new Vector2Int(0,2), new Vector2Int(0,3), new Vector2Int(0,4), new Vector2Int(1,4), new Vector2Int(2,4), new Vector2Int(3,4), new Vector2Int(4,4) } },
                new PathSolution { color = ColorType.Green, pathPositions = new System.Collections.Generic.List<Vector2Int> { new Vector2Int(1,2), new Vector2Int(1,3), new Vector2Int(2,3), new Vector2Int(2,2), new Vector2Int(2,1), new Vector2Int(3,1), new Vector2Int(4,1) } },
                new PathSolution { color = ColorType.Yellow, pathPositions = new System.Collections.Generic.List<Vector2Int> { new Vector2Int(3,2), new Vector2Int(3,3), new Vector2Int(4,3), new Vector2Int(4,2) } }
            };
            AssetDatabase.CreateAsset(lvl2, "Assets/Resources/Levels/Level2.asset");

            // Level 3 (Bridge)
            LevelData lvl3 = ScriptableObject.CreateInstance<LevelData>();
            lvl3.levelIndex = 2;
            lvl3.width = 5;
            lvl3.height = 5;
            lvl3.bridgePositions = new System.Collections.Generic.List<Vector2Int> { new Vector2Int(2, 2) };
            lvl3.initialNodes = new System.Collections.Generic.List<GridNode>
            {
                new GridNode { position = new Vector2Int(0, 2), color = ColorType.Red },
                new GridNode { position = new Vector2Int(4, 2), color = ColorType.Red },
                new GridNode { position = new Vector2Int(2, 0), color = ColorType.Blue },
                new GridNode { position = new Vector2Int(2, 4), color = ColorType.Blue },
                new GridNode { position = new Vector2Int(0, 0), color = ColorType.Green },
                new GridNode { position = new Vector2Int(0, 1), color = ColorType.Green },
                new GridNode { position = new Vector2Int(3, 0), color = ColorType.Yellow },
                new GridNode { position = new Vector2Int(3, 1), color = ColorType.Yellow },
                new GridNode { position = new Vector2Int(0, 3), color = ColorType.Orange },
                new GridNode { position = new Vector2Int(0, 4), color = ColorType.Orange },
                new GridNode { position = new Vector2Int(3, 3), color = ColorType.Purple },
                new GridNode { position = new Vector2Int(3, 4), color = ColorType.Purple }
            };
            lvl3.solutions = new System.Collections.Generic.List<PathSolution>
            {
                new PathSolution { color = ColorType.Red, pathPositions = new System.Collections.Generic.List<Vector2Int> { new Vector2Int(0,2), new Vector2Int(1,2), new Vector2Int(2,2), new Vector2Int(3,2), new Vector2Int(4,2) } },
                new PathSolution { color = ColorType.Blue, pathPositions = new System.Collections.Generic.List<Vector2Int> { new Vector2Int(2,0), new Vector2Int(2,1), new Vector2Int(2,2), new Vector2Int(2,3), new Vector2Int(2,4) } },
                new PathSolution { color = ColorType.Green, pathPositions = new System.Collections.Generic.List<Vector2Int> { new Vector2Int(0,0), new Vector2Int(1,0), new Vector2Int(1,1), new Vector2Int(0,1) } },
                new PathSolution { color = ColorType.Yellow, pathPositions = new System.Collections.Generic.List<Vector2Int> { new Vector2Int(3,0), new Vector2Int(4,0), new Vector2Int(4,1), new Vector2Int(3,1) } },
                new PathSolution { color = ColorType.Orange, pathPositions = new System.Collections.Generic.List<Vector2Int> { new Vector2Int(0,3), new Vector2Int(1,3), new Vector2Int(1,4), new Vector2Int(0,4) } },
                new PathSolution { color = ColorType.Purple, pathPositions = new System.Collections.Generic.List<Vector2Int> { new Vector2Int(3,3), new Vector2Int(4,3), new Vector2Int(4,4), new Vector2Int(3,4) } }
            };
            AssetDatabase.CreateAsset(lvl3, "Assets/Resources/Levels/Level3.asset");

            // Level Pack
            LevelPack pack = ScriptableObject.CreateInstance<LevelPack>();
            pack.packName = "5x5 Beginner Pack";
            pack.levels = new System.Collections.Generic.List<LevelData> { lvl1, lvl2, lvl3 };
            AssetDatabase.CreateAsset(pack, "Assets/Resources/Levels/MainLevelPack.asset");

            AssetDatabase.SaveAssets();
            Debug.Log("[PixelFlowSetupWindow] Generated Level 1, Level 2, Level 3, and MainLevelPack.asset successfully.");
        }
    }
}
#endif
