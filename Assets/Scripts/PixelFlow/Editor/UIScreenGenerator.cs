using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using PixelFlow.Views;

namespace PixelFlow.Editor
{
    /// <summary>
    /// Hiyerarşik düzeni mükemmel olan Color Jam 3D UI Screen Generator.
    /// 
    /// Unity Editor içinde tek tıkla Color Jam 3D v6.0.0 UI ekranlarını oluşturur:
    /// Canvas (ScreenSpaceOverlay)
    /// ├── HUDView (Gold Counter, Power-up Bar, Bouncy Toast)
    /// ├── GarageView (3D Vehicle Showcase & Skin Select)
    /// └── EventSystem (InputSystemUIInputModule)
    /// 
    /// Kullanım: Tools → Pixel Flow → Color Jam 3D UI Suite Generator
    /// </summary>
    public class UIScreenGenerator : EditorWindow
    {
        private string _screenName = "Garage";
        private string _title = "Garaj ve Skinler";

        [MenuItem("Tools/Pixel Flow/UI Screen Generator", false, 100)]
        private static void ShowWindow()
        {
            var window = GetWindow<UIScreenGenerator>("Color Jam 3D UI Generator");
            window.minSize = new Vector2(350, 400);
            window.Show();
        }

        [MenuItem("Tools/Pixel Flow/Generate Color Jam 3D UI Suite", false, 101)]
        public static void GenerateColorJamUISuite()
        {
            var uiParent = GameObject.Find("_UI");
            if (uiParent == null)
            {
                var root = GameObject.Find("[PixelFlow]");
                if (root == null) root = new GameObject("[PixelFlow]");
                uiParent = new GameObject("_UI");
                uiParent.transform.SetParent(root.transform);
            }

            var canvas = EnsureCanvas(uiParent.transform);
            EnsureEventSystem(uiParent.transform);

            // 1. Create GarageView if missing
            var garageView = Object.FindAnyObjectByType<GarageView>();
            if (garageView == null)
            {
                var garageGo = CreateUIChild("GarageView", canvas.gameObject, Vector2.zero, Vector2.one);
                garageView = garageGo.AddComponent<GarageView>();
                garageView.gameObject.SetActive(false);
                Debug.Log("[UIScreenGenerator] GarageView Unity C# UI ekranı başarıyla oluşturuldu.");
            }

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Color Jam 3D UI Screen Generator (v6.0.0)", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Unity UI hiyerarşisinde C# View bileşenlerini otomatik oluşturur.", EditorStyles.miniLabel);
            EditorGUILayout.Space(10);

            _screenName = EditorGUILayout.TextField("Ekran İsmi", _screenName);
            _title = EditorGUILayout.TextField("Başlık", _title);

            EditorGUILayout.Space(15);
            if (GUILayout.Button($"Color Jam 3D '{_screenName}' UI Suite ({_title}) Oluştur", GUILayout.Height(36)))
            {
                GenerateColorJamUISuite();
            }
        }

        private static Canvas EnsureCanvas(Transform uiParent)
        {
            var existing = Object.FindAnyObjectByType<Canvas>();
            if (existing != null)
            {
                var scaler = existing.GetComponent<CanvasScaler>();
                if (scaler != null)
                {
                    scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                    scaler.referenceResolution = new Vector2(1080, 1920);
                }
                return existing;
            }

            var go = new GameObject("Canvas", typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(go, "Create Canvas");
            go.transform.SetParent(uiParent);

            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scalerComp = go.AddComponent<CanvasScaler>();
            scalerComp.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scalerComp.referenceResolution = new Vector2(1080, 1920);
            scalerComp.matchWidthOrHeight = 0.5f;

            go.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        private static void EnsureEventSystem(Transform uiParent)
        {
            var existing = Object.FindAnyObjectByType<EventSystem>();
            if (existing != null)
            {
                return;
            }

            var go = new GameObject("EventSystem");
            Undo.RegisterCreatedObjectUndo(go, "Create EventSystem");
            go.transform.SetParent(uiParent);
            go.AddComponent<EventSystem>();
            go.AddComponent<InputSystemUIInputModule>();
        }

        private static GameObject CreateUIChild(string name, GameObject parent, Vector2 anchorMin, Vector2 anchorMax)
        {
            var go = new GameObject(name, typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(go, $"Create {name}");
            go.transform.SetParent(parent.transform, false);

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            return go;
        }
    }
}
