using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace PixelFlow.Editor
{
    /// <summary>
    /// Hiyerarşik düzeni mükemmel olan UI Screen Generator.
    /// 
    /// Tek tıkla eksiksiz bir puzzle oyun UI ekranı oluşturur:
    /// [PixelFlow]
    ///   _UI
    ///     Canvas (ScreenSpaceOverlay)
    ///       [ScreenName]View (RectTransform, full-screen)
    ///         _Header
    ///           _BackButton
    ///             _Icon
    ///           _TitleText
    ///         _Content
    ///           ...
    ///         _Footer
    ///           ...
    ///     EventSystem
    /// 
    /// Kullanım: Tools → Pixel Flow → UI Screen Generator
    /// </summary>
    public class UIScreenGenerator : EditorWindow
    {
        private string _screenName = "NewScreen";
        private bool _includeHeader = true;
        private bool _includeFooter;
        private bool _includeBackButton = true;
        private string _title = "Screen Title";
        private int _contentColumns = 1;
        private bool _addToExistingCanvas = true;

        [MenuItem("Tools/Pixel Flow/UI Screen Generator", false, 100)]
        private static void ShowWindow()
        {
            var window = GetWindow<UIScreenGenerator>("UI Screen Generator");
            window.minSize = new Vector2(350, 400);
            window.Show();
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("UI Screen Generator", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Mükemmel hiyerarşi ile UI ekranı oluşturur.", EditorStyles.miniLabel);
            EditorGUILayout.Space(10);

            // ─── Screen Settings ───
            EditorGUILayout.LabelField("Screen Settings", EditorStyles.boldLabel);
            _screenName = EditorGUILayout.TextField("Screen Name", _screenName);
            _title = EditorGUILayout.TextField("Title Text", _title);
            _addToExistingCanvas = EditorGUILayout.Toggle("Add to Existing Canvas", _addToExistingCanvas);

            EditorGUILayout.Space(10);

            // ─── Layout Options ───
            EditorGUILayout.LabelField("Layout Options", EditorStyles.boldLabel);
            _includeHeader = EditorGUILayout.Toggle("Include Header", _includeHeader);
            if (_includeHeader)
            {
                EditorGUI.indentLevel++;
                _includeBackButton = EditorGUILayout.Toggle("Include Back Button", _includeBackButton);
                EditorGUI.indentLevel--;
            }
            _includeFooter = EditorGUILayout.Toggle("Include Footer", _includeFooter);
            _contentColumns = EditorGUILayout.IntSlider("Content Columns", _contentColumns, 1, 4);

            EditorGUILayout.Space(15);

            // ─── Generate Button ───
            GUI.color = new Color(0.2f, 0.6f, 0.9f);
            if (GUILayout.Button($"Generate '{_screenName}' UI Screen", GUILayout.Height(40)))
            {
                GenerateScreen();
            }
            GUI.color = Color.white;

            EditorGUILayout.Space(5);
            if (GUILayout.Button("Generate Full Game UI Suite (All Screens)", GUILayout.Height(30)))
            {
                GenerateFullSuite();
            }
        }

        private void GenerateScreen()
        {
            string screenName = _screenName;
            if (string.IsNullOrWhiteSpace(screenName))
            {
                Debug.LogError("[UIScreenGenerator] Screen name cannot be empty.");
                return;
            }

            Undo.IncrementCurrentGroup();
            int groupIndex = Undo.GetCurrentGroup();

            // ─── Root [PixelFlow] ───
            var root = EnsureRoot();

            // ─── _UI ───
            var uiParent = EnsureChild(root.transform, "_UI");

            // ─── Canvas ───
            var canvas = EnsureCanvas(uiParent);

            // ─── Screen View ───
            var screenObj = new GameObject($"{screenName}View", typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(screenObj, $"Create {screenName}View");
            screenObj.transform.SetParent(canvas.transform, false);

            var screenRect = screenObj.GetComponent<RectTransform>();
            screenRect.anchorMin = Vector2.zero;
            screenRect.anchorMax = Vector2.one;
            screenRect.offsetMin = Vector2.zero;
            screenRect.offsetMax = Vector2.zero;

            var canvasGroup = screenObj.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;

            // Image background
            var bgImg = screenObj.AddComponent<Image>();
            bgImg.color = new Color(0.043f, 0.059f, 0.098f, 1f);
            bgImg.raycastTarget = true;

            // ─── _Header ───
            if (_includeHeader)
            {
                var header = CreateUIChild("_Header", screenObj, new Vector2(0, 0.85f), new Vector2(1, 1), new Vector2(0.5f, 1f));
                var headerImg = header.AddComponent<Image>();
                headerImg.color = new Color(0.06f, 0.08f, 0.14f, 1f);

                if (_includeBackButton)
                {
                    var backBtn = CreateUIChild("_BackButton", header, new Vector2(0, 0), new Vector2(0, 1), new Vector2(0, 0.5f),
                        new Vector2(60, 0), new Vector2(60, 0));
                    var backBtnImg = backBtn.AddComponent<Image>();
                    backBtnImg.color = new Color(0.15f, 0.15f, 0.2f, 1f);
                    backBtn.AddComponent<Button>();

                    var backIcon = CreateUIChild("_Icon", backBtn, new Vector2(0.2f, 0.2f), new Vector2(0.8f, 0.8f));
                    var iconImg = backIcon.AddComponent<Image>();
                    iconImg.color = new Color(0.5f, 0.5f, 0.6f, 1f);
                }

                var title = CreateUIChild("_TitleText", header, new Vector2(0, 0), new Vector2(1, 1), new Vector2(0.5f, 0.5f),
                    new Vector2(70, 0), new Vector2(70, 0));
                var titleText = title.AddComponent<Text>();
                titleText.text = _title;
                titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
                titleText.fontSize = 28;
                titleText.alignment = TextAnchor.MiddleCenter;
                titleText.color = Color.white;
                titleText.fontStyle = FontStyle.Bold;
            }

            // ─── _Content ───
            float headerSpace = _includeHeader ? 0.15f : 0f;
            float footerSpace = _includeFooter ? 0.12f : 0f;
            var content = CreateUIChild("_Content", screenObj,
                new Vector2(0, footerSpace), new Vector2(1, 1f - headerSpace));
            var contentImg = content.AddComponent<Image>();
            contentImg.color = new Color(0.05f, 0.065f, 0.11f, 1f);

            if (_contentColumns > 1)
            {
                var gridLayout = content.AddComponent<GridLayoutGroup>();
                gridLayout.cellSize = new Vector2(140, 140);
                gridLayout.spacing = new Vector2(12, 12);
                gridLayout.padding = new RectOffset(16, 16, 16, 16);
                gridLayout.childAlignment = TextAnchor.UpperCenter;
                gridLayout.startAxis = GridLayoutGroup.Axis.Horizontal;
                gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                gridLayout.constraintCount = _contentColumns;
            }

            // ─── _Footer ───
            if (_includeFooter)
            {
                var footer = CreateUIChild("_Footer", screenObj, new Vector2(0, 0), new Vector2(1, 0.12f));
                var footerImg = footer.AddComponent<Image>();
                footerImg.color = new Color(0.06f, 0.08f, 0.14f, 1f);

                // Primary action button in footer
                var actionBtn = CreateUIChild("_ActionButton", footer,
                    new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0.5f),
                    new Vector2(-90, 15), new Vector2(90, -15));
                var btnImg = actionBtn.AddComponent<Image>();
                btnImg.color = new Color(0.2f, 0.6f, 0.3f, 1f);
                actionBtn.AddComponent<Button>();

                var btnText = CreateUIChild("_Text", actionBtn, Vector2.zero, Vector2.one);
                var txt = btnText.AddComponent<Text>();
                txt.text = "ACTION";
                txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
                txt.fontSize = 18;
                txt.alignment = TextAnchor.MiddleCenter;
                txt.color = Color.white;
                txt.fontStyle = FontStyle.Bold;
            }

            // ─── EventSystem ───
            EnsureEventSystem(uiParent);

            Undo.CollapseUndoOperations(groupIndex);
            EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            Debug.Log($"[UIScreenGenerator] '{screenName}View' created with perfect hierarchy under [PixelFlow]/_UI/Canvas/");
        }

        private void GenerateFullSuite()
        {
            // Save current name
            string savedName = _screenName;

            // Generate all game screens
            string[] screens = { "Splash", "HUD", "Settings", "Tutorial", "Pause" };
            foreach (var s in screens)
            {
                _screenName = s;
                _title = s;
                _includeHeader = s != "HUD";
                _includeFooter = s == "Settings" || s == "Tutorial";
                _includeBackButton = s != "HUD" && s != "Splash";
                _contentColumns = s == "Settings" ? 2 : 1;
                GenerateScreen();
            }

            _screenName = savedName;
            Debug.Log("[UIScreenGenerator] Full UI Suite generated: Splash, HUD, Settings, Tutorial, Pause");
        }

        // ─── Helpers ───

        private static Transform EnsureRoot()
        {
            var go = GameObject.Find("[PixelFlow]");
            if (go != null) return go.transform;
            go = new GameObject("[PixelFlow]");
            Undo.RegisterCreatedObjectUndo(go, "Create [PixelFlow] Root");
            return go.transform;
        }

        private static Transform EnsureChild(Transform parent, string name)
        {
            var existing = parent.Find(name);
            if (existing != null) return existing;
            var go = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(go, $"Create {name}");
            go.transform.SetParent(parent);
            return go.transform;
        }

        private static Canvas EnsureCanvas(Transform uiParent)
        {
            var existing = Object.FindAnyObjectByType<Canvas>();
            if (existing != null)
            {
                existing.transform.SetParent(uiParent);
                return existing;
            }

            var go = new GameObject("Canvas", typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(go, "Create Canvas");
            go.transform.SetParent(uiParent);

            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            go.AddComponent<CanvasScaler>();
            go.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        private static void EnsureEventSystem(Transform uiParent)
        {
            var existing = Object.FindAnyObjectByType<EventSystem>();
            if (existing != null)
            {
                existing.transform.SetParent(uiParent);
                if (existing.GetComponent<InputSystemUIInputModule>() == null)
                    existing.gameObject.AddComponent<InputSystemUIInputModule>();
                return;
            }

            var go = new GameObject("EventSystem");
            Undo.RegisterCreatedObjectUndo(go, "Create EventSystem");
            go.transform.SetParent(uiParent);
            go.AddComponent<EventSystem>();
            go.AddComponent<InputSystemUIInputModule>();
        }

        private static GameObject CreateUIChild(string name, GameObject parent,
            Vector2 anchorMin, Vector2 anchorMax,
            Vector2? pivot = null,
            Vector2? offsetMin = null, Vector2? offsetMax = null)
        {
            var go = new GameObject(name, typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(go, $"Create {name}");
            go.transform.SetParent(parent.transform, false);

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot ?? new Vector2(0.5f, 0.5f);
            rt.offsetMin = offsetMin ?? Vector2.zero;
            rt.offsetMax = offsetMax ?? Vector2.zero;

            return go;
        }
    }
}
