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
        }

        private void GeneratePrefabs()
        {
            if (!Directory.Exists("Assets/Prefabs"))
            {
                Directory.CreateDirectory("Assets/Prefabs");
            }

            string cellPrefabPath = "Assets/Prefabs/CellView.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(cellPrefabPath) == null)
            {
                GameObject cellObj = new GameObject("CellView");
                var cellView = cellObj.AddComponent<CellView>();
                cellObj.AddComponent<BoxCollider2D>(); 
                
                GameObject bgObj = new GameObject("Background");
                bgObj.transform.SetParent(cellObj.transform);
                var bgRenderer = bgObj.AddComponent<SpriteRenderer>();

                GameObject dotObj = new GameObject("Dot");
                dotObj.transform.SetParent(cellObj.transform);
                var dotRenderer = dotObj.AddComponent<SpriteRenderer>();

                GameObject bridgeObj = new GameObject("Bridge");
                bridgeObj.transform.SetParent(cellObj.transform);
                var bridgeRenderer = bridgeObj.AddComponent<SpriteRenderer>();

                SerializedObject so = new SerializedObject(cellView);
                so.FindProperty("_bgRenderer").objectReferenceValue = bgRenderer;
                so.FindProperty("_dotRenderer").objectReferenceValue = dotRenderer;
                so.FindProperty("_bridgeRenderer").objectReferenceValue = bridgeRenderer;
                so.ApplyModifiedProperties();

                PrefabUtility.SaveAsPrefabAsset(cellObj, cellPrefabPath);
                DestroyImmediate(cellObj);
                Debug.Log("CellView prefab created at " + cellPrefabPath);
            }
            else
            {
                Debug.Log("CellView prefab already exists.");
            }
        }

        private void SetupScene()
        {
            if (FindObjectOfType<Context>() == null)
            {
                GameObject contextObj = new GameObject("PixelFlow_Context");
                var context = contextObj.AddComponent<Context>();
                contextObj.AddComponent<GameContextLifecycle>();
            }

            if (FindObjectOfType<GridView>() == null)
            {
                GameObject gridObj = new GameObject("GridView");
                var gridView = gridObj.AddComponent<GridView>();
                
                GameObject container = new GameObject("CellsContainer");
                container.transform.SetParent(gridObj.transform);
                
                SerializedObject so = new SerializedObject(gridView);
                so.FindProperty("_gridContainer").objectReferenceValue = container.transform;

                var cellPrefab = AssetDatabase.LoadAssetAtPath<CellView>("Assets/Prefabs/CellView.prefab");
                if (cellPrefab != null)
                {
                    so.FindProperty("_cellPrefab").objectReferenceValue = cellPrefab;
                }
                so.ApplyModifiedProperties();
            }

            if (FindObjectOfType<HUDView>() == null)
            {
                GameObject canvasObj = new GameObject("Canvas");
                var canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasObj.AddComponent<CanvasScaler>();
                canvasObj.AddComponent<GraphicRaycaster>();

                GameObject hudObj = new GameObject("HUDView");
                hudObj.transform.SetParent(canvasObj.transform, false);
                var hudView = hudObj.AddComponent<HUDView>();

                GameObject hintBtnObj = new GameObject("HintButton");
                hintBtnObj.transform.SetParent(hudObj.transform, false);
                hintBtnObj.AddComponent<Image>();
                var hintBtn = hintBtnObj.AddComponent<Button>();

                GameObject hintTextObj = new GameObject("HintCountText");
                hintTextObj.transform.SetParent(hintBtnObj.transform, false);
                var hintText = hintTextObj.AddComponent<Text>();
                hintText.text = "3";

                SerializedObject so = new SerializedObject(hudView);
                so.FindProperty("_hintButton").objectReferenceValue = hintBtn;
                so.FindProperty("_hintCountText").objectReferenceValue = hintText;
                so.ApplyModifiedProperties();
            }

            if (Camera.main != null)
            {
                Camera.main.orthographic = true;
                Camera.main.orthographicSize = 5;
            }

            Debug.Log("Scene setup complete.");
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
    }
}
#endif
