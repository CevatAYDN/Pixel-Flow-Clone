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

            // CellView component
            var cellView = cellObj.AddComponent<CellView>();

            // Z-sıralaması: Background(0) > Bridge(-0.2f) > DotNode(-0.4f) > Warning(-0.5f)
            var bgObj = new GameObject("Background");
            bgObj.transform.SetParent(cellObj.transform, false);
            var bgRenderer = bgObj.AddComponent<SpriteRenderer>();
            bgRenderer.sortingOrder = 0;

            var bridgeObj = new GameObject("Bridge");
            bridgeObj.transform.SetParent(cellObj.transform, false);
            bridgeObj.transform.localPosition = new Vector3(0, 0, -0.2f);
            var bridgeRenderer = bridgeObj.AddComponent<SpriteRenderer>();
            bridgeRenderer.sortingOrder = 1;

            var dotObj = new GameObject("DotNode");
            dotObj.transform.SetParent(cellObj.transform, false);
            dotObj.transform.localPosition = new Vector3(0, 0, -0.4f);
            var dotRenderer = dotObj.AddComponent<SpriteRenderer>();
            dotRenderer.sortingOrder = 2;

            var warnObj = new GameObject("Warning");
            warnObj.transform.SetParent(cellObj.transform, false);
            warnObj.transform.localPosition = new Vector3(0, 0, -0.5f);
            var warnRenderer = warnObj.AddComponent<SpriteRenderer>();
            warnRenderer.sortingOrder = 3;

            var arrowObj = new GameObject("OneWayArrow");
            arrowObj.transform.SetParent(cellObj.transform, false);
            arrowObj.transform.localPosition = new Vector3(0, 0, -0.25f);
            var arrowRenderer = arrowObj.AddComponent<SpriteRenderer>();
            arrowRenderer.sortingOrder = 2;

            // SerializedField referanslarını SerializedObject ile ata
            var so = new SerializedObject(cellView);
            so.FindProperty("_bgRenderer").objectReferenceValue = bgRenderer;
            so.FindProperty("_dotRenderer").objectReferenceValue = dotRenderer;
            so.FindProperty("_bridgeRenderer").objectReferenceValue = bridgeRenderer;
            so.FindProperty("_warningRenderer").objectReferenceValue = warnRenderer;
            so.FindProperty("_oneWayArrow").objectReferenceValue = arrowRenderer;
            so.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(cellObj, "Assets/Prefabs/CellView.prefab");
            DestroyImmediate(cellObj);
            AssetDatabase.Refresh();
            Debug.Log("[PixelFlow] CellView.prefab created with full renderer hierarchy.");
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

            // ✅ GameContextLifecycle'i Root'a ekle — yoksa Nexus DI servis kaydı yapılmaz
            EnsureComponent<GameContextLifecycle>(rootObj);

            var canvasObj = FindOrCreateChild(rootObj.transform, "Canvas");
            var canvas = canvasObj.GetComponent<Canvas>();
            if (canvas == null)
            {
                canvas = canvasObj.AddComponent<Canvas>();
            }
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            EnsureComponent<CanvasScaler>(canvasObj, scaler =>
            {
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1080, 1920); // Portrait 9:16 per GDD
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight = 0.5f;
            });
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
            var hudView = hudObj.GetComponent<HUDView>();
            if (hudView == null) hudView = hudObj.AddComponent<HUDView>();
            EnsureComponent<CanvasGroup>(hudObj);
            EnsureComponent<SafeArea>(hudObj);
            var hudRect = hudObj.GetComponent<RectTransform>();
            if (hudRect == null)
            {
                hudRect = hudObj.AddComponent<RectTransform>();
            }
            hudRect.anchorMin = Vector2.zero;
            hudRect.anchorMax = Vector2.one;
            hudRect.sizeDelta = Vector2.zero;
            EnsureHUDBindings(hudView);

            var menuObj = FindOrCreateChild(canvasObj.transform, "MainMenuView");
            var menuView = menuObj.GetComponent<MainMenuView>();
            if (menuView == null) menuView = menuObj.AddComponent<MainMenuView>();
            EnsureComponent<CanvasGroup>(menuObj);
            EnsureComponent<SafeArea>(menuObj);
            EnsureMainMenuBindings(menuView);

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
            if (boot.initialLevel == null)
            {
                RefreshLevelsCache();
                if (_cachedLevels.Count == 0)
                {
                    CreatePhase1And2HandCraftedPack();
                    RefreshLevelsCache();
                }
                if (_cachedLevels.Count > 0)
                {
                    Undo.RecordObject(boot, "Başlangıç Seviyesi Ata");
                    boot.initialLevel = _cachedLevels[0];
                    EditorUtility.SetDirty(boot);
                }
            }

            EnsureExtendedViews(canvasObj.transform);

            Selection.activeGameObject = rootObj;
            EditorGUIUtility.PingObject(rootObj);
            Debug.Log("[PixelFlow] Scene setup complete.");
        }

        private void EnsureHUDBindings(HUDView hudView)
        {
            if (hudView == null) return;
            var hudObj = hudView.gameObject;

            // Purge old children
            for (int i = hudObj.transform.childCount - 1; i >= 0; i--)
            {
                Object.DestroyImmediate(hudObj.transform.GetChild(i).gameObject);
            }

            var hudRect = hudObj.GetComponent<RectTransform>();
            if (hudRect != null)
            {
                hudRect.anchorMin = Vector2.zero;
                hudRect.anchorMax = Vector2.one;
                hudRect.sizeDelta = Vector2.zero;
                hudRect.anchoredPosition = Vector2.zero;
            }

            // Transparent background for gameplay HUD
            var bgImg = hudObj.GetComponent<UnityEngine.UI.Image>();
            if (bgImg == null) bgImg = hudObj.AddComponent<UnityEngine.UI.Image>();
            bgImg.color = new Color(0, 0, 0, 0);

            // 1. TOP HUD BAR (Clean Pill Badges per gameplay-hud.html)
            var topBarObj = FindOrCreateChild(hudObj.transform, "TopHUDBar");
            var topBarRect = topBarObj.GetComponent<RectTransform>();
            if (topBarRect != null)
            {
                topBarRect.anchorMin = new Vector2(0.04f, 0.90f);
                topBarRect.anchorMax = new Vector2(0.96f, 0.97f);
                topBarRect.sizeDelta = Vector2.zero;
                topBarRect.anchoredPosition = Vector2.zero;
            }

            // Level Badge Pill (White rounded pill, indigo text)
            var levelBadgeObj = FindOrCreateChild(topBarObj.transform, "LevelBadge");
            var lbImg = levelBadgeObj.GetComponent<UnityEngine.UI.Image>();
            if (lbImg == null) lbImg = levelBadgeObj.AddComponent<UnityEngine.UI.Image>();
            lbImg.color = Color.white;
            var lbRect = levelBadgeObj.GetComponent<RectTransform>();
            if (lbRect != null)
            {
                lbRect.anchorMin = new Vector2(0f, 0.15f);
                lbRect.anchorMax = new Vector2(0.32f, 0.85f);
                lbRect.sizeDelta = Vector2.zero;
                lbRect.anchoredPosition = Vector2.zero;
            }

            var levelTitleText = EnsureTMPText(levelBadgeObj, "LevelTitleText");
            levelTitleText.text = "SEVİYE 1";
            levelTitleText.fontSize = 22;
            levelTitleText.fontStyle = TMPro.FontStyles.Bold;
            levelTitleText.color = new Color(0.39f, 0.40f, 0.95f); // #6366F1 Indigo
            levelTitleText.alignment = TMPro.TextAlignmentOptions.Center;
            var ltRect = levelTitleText.GetComponent<RectTransform>();
            if (ltRect != null) { ltRect.anchorMin = Vector2.zero; ltRect.anchorMax = Vector2.one; ltRect.sizeDelta = Vector2.zero; }

            // Coin Counter Pill (Soft Gold Pill)
            var coinPillObj = FindOrCreateChild(topBarObj.transform, "CoinCounterPill");
            var cpImg = coinPillObj.GetComponent<UnityEngine.UI.Image>();
            if (cpImg == null) cpImg = coinPillObj.AddComponent<UnityEngine.UI.Image>();
            cpImg.color = new Color(0.99f, 0.95f, 0.78f); // #FEF3C7 Soft Gold Pill
            var cpRect = coinPillObj.GetComponent<RectTransform>();
            if (cpRect != null)
            {
                cpRect.anchorMin = new Vector2(0.36f, 0.15f);
                cpRect.anchorMax = new Vector2(0.78f, 0.85f);
                cpRect.sizeDelta = Vector2.zero;
                cpRect.anchoredPosition = Vector2.zero;
            }

            var scoreText = EnsureTMPText(coinPillObj, "ScoreText");
            scoreText.text = "SKOR: 1,450";
            scoreText.fontSize = 22;
            scoreText.fontStyle = TMPro.FontStyles.Bold;
            scoreText.color = new Color(0.70f, 0.35f, 0.05f); // #B45309 Gold Text
            scoreText.alignment = TMPro.TextAlignmentOptions.Center;
            var stRect = scoreText.GetComponent<RectTransform>();
            if (stRect != null) { stRect.anchorMin = Vector2.zero; stRect.anchorMax = Vector2.one; stRect.sizeDelta = Vector2.zero; }

            // Pause Button (White rounded square, dark slate text)
            var pauseBtn = EnsureButton(topBarObj, "PauseButton");
            var pImg = pauseBtn.GetComponent<UnityEngine.UI.Image>();
            if (pImg != null) pImg.color = Color.white;
            var pRect = pauseBtn.GetComponent<RectTransform>();
            if (pRect != null)
            {
                pRect.anchorMin = new Vector2(0.82f, 0.10f);
                pRect.anchorMax = new Vector2(1.0f, 0.90f);
                pRect.sizeDelta = Vector2.zero;
                pRect.anchoredPosition = Vector2.zero;
            }

            var pauseText = EnsureTMPText(pauseBtn.gameObject, "Text");
            pauseText.text = "II";
            pauseText.fontSize = 24;
            pauseText.fontStyle = TMPro.FontStyles.Bold;
            pauseText.color = new Color(0.39f, 0.45f, 0.55f); // #64748B Slate
            pauseText.alignment = TMPro.TextAlignmentOptions.Center;
            var pTextRect = pauseText.GetComponent<RectTransform>();
            if (pTextRect != null) { pTextRect.anchorMin = Vector2.zero; pTextRect.anchorMax = Vector2.one; pTextRect.sizeDelta = Vector2.zero; }

            // 2. POWER-UP BOTTOM BAR (Floating White Dock per gameplay-hud.html)
            var powerUpDockObj = FindOrCreateChild(hudObj.transform, "PowerUpBar");
            var dockImg = powerUpDockObj.GetComponent<UnityEngine.UI.Image>();
            if (dockImg == null) dockImg = powerUpDockObj.AddComponent<UnityEngine.UI.Image>();
            dockImg.color = Color.white; // Floating White Dock
            var dockRect = powerUpDockObj.GetComponent<RectTransform>();
            if (dockRect != null)
            {
                dockRect.anchorMin = new Vector2(0.04f, 0.02f);
                dockRect.anchorMax = new Vector2(0.96f, 0.10f);
                dockRect.sizeDelta = Vector2.zero;
                dockRect.anchoredPosition = Vector2.zero;
            }

            // Undo Button (Purple Pill)
            var undoBtn = EnsureButton(powerUpDockObj, "UndoButton");
            var uImg = undoBtn.GetComponent<UnityEngine.UI.Image>();
            if (uImg != null) uImg.color = new Color(0.55f, 0.36f, 0.96f); // #8B5CF6 Purple
            var uRect = undoBtn.GetComponent<RectTransform>();
            if (uRect != null)
            {
                uRect.anchorMin = new Vector2(0.04f, 0.15f);
                uRect.anchorMax = new Vector2(0.32f, 0.85f);
                uRect.sizeDelta = Vector2.zero;
                uRect.anchoredPosition = Vector2.zero;
            }
            var uText = EnsureTMPText(undoBtn.gameObject, "Text");
            uText.text = "VİYADÜK";
            uText.fontSize = 20;
            uText.fontStyle = TMPro.FontStyles.Bold;
            uText.color = Color.white;
            uText.alignment = TMPro.TextAlignmentOptions.Center;
            var uTextRect = uText.GetComponent<RectTransform>();
            if (uTextRect != null) { uTextRect.anchorMin = Vector2.zero; uTextRect.anchorMax = Vector2.one; uTextRect.sizeDelta = Vector2.zero; }

            // Hint Button (Sky Blue Pill)
            var hintBtn = EnsureButton(powerUpDockObj, "HintButton");
            var hImg = hintBtn.GetComponent<UnityEngine.UI.Image>();
            if (hImg != null) hImg.color = new Color(0.22f, 0.74f, 0.97f); // #38BDF8 Sky Blue
            var hRect = hintBtn.GetComponent<RectTransform>();
            if (hRect != null)
            {
                hRect.anchorMin = new Vector2(0.36f, 0.15f);
                hRect.anchorMax = new Vector2(0.64f, 0.85f);
                hRect.sizeDelta = Vector2.zero;
                hRect.anchoredPosition = Vector2.zero;
            }
            var hText = EnsureTMPText(hintBtn.gameObject, "Text");
            hText.text = "TEMİZLE";
            hText.fontSize = 20;
            hText.fontStyle = TMPro.FontStyles.Bold;
            hText.color = Color.white;
            hText.alignment = TMPro.TextAlignmentOptions.Center;
            var hTextRect = hText.GetComponent<RectTransform>();
            if (hTextRect != null) { hTextRect.anchorMin = Vector2.zero; hTextRect.anchorMax = Vector2.one; hTextRect.sizeDelta = Vector2.zero; }

            // Redo Button (Rose Red Pill)
            var redoBtn = EnsureButton(powerUpDockObj, "RedoButton");
            var rImg = redoBtn.GetComponent<UnityEngine.UI.Image>();
            if (rImg != null) rImg.color = new Color(0.96f, 0.25f, 0.37f); // #F43F5E Rose Red
            var rRect = redoBtn.GetComponent<RectTransform>();
            if (rRect != null)
            {
                rRect.anchorMin = new Vector2(0.68f, 0.15f);
                rRect.anchorMax = new Vector2(0.96f, 0.85f);
                rRect.sizeDelta = Vector2.zero;
                rRect.anchoredPosition = Vector2.zero;
            }
            var rText = EnsureTMPText(redoBtn.gameObject, "Text");
            rText.text = "GÖKKUŞAĞI";
            rText.fontSize = 20;
            rText.fontStyle = TMPro.FontStyles.Bold;
            rText.color = Color.white;
            rText.alignment = TMPro.TextAlignmentOptions.Center;
            var rTextRect = rText.GetComponent<RectTransform>();
            if (rTextRect != null) { rTextRect.anchorMin = Vector2.zero; rTextRect.anchorMax = Vector2.one; rTextRect.sizeDelta = Vector2.zero; }

            // 3. COMPLETION PANEL (Victory Modal)
            var compPanelObj = FindOrCreateChild(hudObj.transform, "CompletionPanel");
            var compImg = compPanelObj.GetComponent<UnityEngine.UI.Image>();
            if (compImg == null) compImg = compPanelObj.AddComponent<UnityEngine.UI.Image>();
            compImg.color = new Color(0.06f, 0.09f, 0.16f, 0.88f);
            var compRect = compPanelObj.GetComponent<RectTransform>();
            if (compRect != null)
            {
                compRect.anchorMin = Vector2.zero;
                compRect.anchorMax = Vector2.one;
                compRect.sizeDelta = Vector2.zero;
                compRect.anchoredPosition = Vector2.zero;
            }

            var compCardObj = FindOrCreateChild(compPanelObj.transform, "CompletionCard");
            var ccImg = compCardObj.GetComponent<UnityEngine.UI.Image>();
            if (ccImg == null) ccImg = compCardObj.AddComponent<UnityEngine.UI.Image>();
            ccImg.color = Color.white;
            var ccRect = compCardObj.GetComponent<RectTransform>();
            if (ccRect != null)
            {
                ccRect.anchorMin = new Vector2(0.08f, 0.25f);
                ccRect.anchorMax = new Vector2(0.92f, 0.75f);
                ccRect.sizeDelta = Vector2.zero;
                ccRect.anchoredPosition = Vector2.zero;
            }

            var compText = EnsureTMPText(compCardObj, "CompletionText");
            compText.text = "SEVİYE TAMAMLANDI!";
            compText.fontSize = 32;
            compText.fontStyle = TMPro.FontStyles.Bold;
            compText.color = new Color(0.12f, 0.23f, 0.54f);
            compText.alignment = TMPro.TextAlignmentOptions.Center;
            var ctRect = compText.GetComponent<RectTransform>();
            if (ctRect != null) { ctRect.anchorMin = new Vector2(0.05f, 0.80f); ctRect.anchorMax = new Vector2(0.95f, 0.94f); ctRect.sizeDelta = Vector2.zero; }

            var nextBtn = EnsureButton(compCardObj, "NextLevelButton");
            var nImg = nextBtn.GetComponent<UnityEngine.UI.Image>();
            if (nImg != null) nImg.color = new Color(0.06f, 0.72f, 0.51f);
            var nRect = nextBtn.GetComponent<RectTransform>();
            if (nRect != null)
            {
                nRect.anchorMin = new Vector2(0.10f, 0.08f);
                nRect.anchorMax = new Vector2(0.90f, 0.24f);
                nRect.sizeDelta = Vector2.zero;
                nRect.anchoredPosition = Vector2.zero;
            }

            var nextText = EnsureTMPText(nextBtn.gameObject, "Text");
            nextText.text = "SONRAKİ SEVİYE";
            nextText.fontSize = 26;
            nextText.fontStyle = TMPro.FontStyles.Bold;
            nextText.color = Color.white;
            nextText.alignment = TMPro.TextAlignmentOptions.Center;
            var nTextRect = nextText.GetComponent<RectTransform>();
            if (nTextRect != null) { nTextRect.anchorMin = Vector2.zero; nTextRect.anchorMax = Vector2.one; nTextRect.sizeDelta = Vector2.zero; }

            // 4. LEVEL FAILED PANEL
            var failPanelObj = FindOrCreateChild(hudObj.transform, "LevelFailedPanel");
            var failImg = failPanelObj.GetComponent<UnityEngine.UI.Image>();
            if (failImg == null) failImg = failPanelObj.AddComponent<UnityEngine.UI.Image>();
            failImg.color = new Color(0.06f, 0.09f, 0.16f, 0.88f);
            var failRect = failPanelObj.GetComponent<RectTransform>();
            if (failRect != null)
            {
                failRect.anchorMin = Vector2.zero;
                failRect.anchorMax = Vector2.one;
                failRect.sizeDelta = Vector2.zero;
                failRect.anchoredPosition = Vector2.zero;
            }

            var failCardObj = FindOrCreateChild(failPanelObj.transform, "FailCard");
            var fcImg = failCardObj.GetComponent<UnityEngine.UI.Image>();
            if (fcImg == null) fcImg = failCardObj.AddComponent<UnityEngine.UI.Image>();
            fcImg.color = Color.white;
            var fcRect = failCardObj.GetComponent<RectTransform>();
            if (fcRect != null)
            {
                fcRect.anchorMin = new Vector2(0.08f, 0.25f);
                fcRect.anchorMax = new Vector2(0.92f, 0.75f);
                fcRect.sizeDelta = Vector2.zero;
                fcRect.anchoredPosition = Vector2.zero;
            }

            var failText = EnsureTMPText(failCardObj, "LevelFailedText");
            failText.text = "SEVİYE BAŞARISIZ!";
            failText.fontSize = 32;
            failText.fontStyle = TMPro.FontStyles.Bold;
            failText.color = new Color(0.85f, 0.2f, 0.2f);
            failText.alignment = TMPro.TextAlignmentOptions.Center;
            var fTextRect = failText.GetComponent<RectTransform>();
            if (fTextRect != null) { fTextRect.anchorMin = new Vector2(0.05f, 0.80f); fTextRect.anchorMax = new Vector2(0.95f, 0.94f); fTextRect.sizeDelta = Vector2.zero; }

            var retryBtn = EnsureButton(failCardObj, "RetryButton");
            var rtryImg = retryBtn.GetComponent<UnityEngine.UI.Image>();
            if (rtryImg != null) rtryImg.color = new Color(0.9f, 0.25f, 0.25f);
            var retryRect = retryBtn.GetComponent<RectTransform>();
            if (retryRect != null)
            {
                retryRect.anchorMin = new Vector2(0.10f, 0.08f);
                retryRect.anchorMax = new Vector2(0.90f, 0.24f);
                retryRect.sizeDelta = Vector2.zero;
                retryRect.anchoredPosition = Vector2.zero;
            }

            var retryText = EnsureTMPText(retryBtn.gameObject, "Text");
            retryText.text = "TEKRAR DENE";
            retryText.fontSize = 26;
            retryText.fontStyle = TMPro.FontStyles.Bold;
            retryText.color = Color.white;
            retryText.alignment = TMPro.TextAlignmentOptions.Center;
            var rtryTextRect = retryText.GetComponent<RectTransform>();
            if (rtryTextRect != null) { rtryTextRect.anchorMin = Vector2.zero; rtryTextRect.anchorMax = Vector2.one; rtryTextRect.sizeDelta = Vector2.zero; }

            compPanelObj.SetActive(false);
            failPanelObj.SetActive(false);

            // Assign serialized properties via SerializedObject
            var so = new SerializedObject(hudView);
            SetProp(so, "_scoreText", scoreText);
            SetProp(so, "_levelTitleText", levelTitleText);
            SetProp(so, "_pauseButton", pauseBtn);
            SetProp(so, "_undoButton", undoBtn);
            SetProp(so, "_hintButton", hintBtn);
            SetProp(so, "_redoButton", redoBtn);

            SetProp(so, "_completionPanel", compPanelObj);
            SetProp(so, "_completionText", compText);
            SetProp(so, "_completionScoreText", compScoreText);
            SetProp(so, "_completionStarsText", compStarsText);
            SetProp(so, "_nextLevelButton", nextBtn);
            SetProp(so, "_continueButton", continueBtn);
            SetProp(so, "_starsContainer", starsContainer);
            SetProp(so, "_star1", star1);
            SetProp(so, "_star2", star2);
            SetProp(so, "_star3", star3);

            SetProp(so, "_levelFailedPanel", failPanelObj);
            SetProp(so, "_levelFailedText", failText);
            SetProp(so, "_retryButton", retryBtn);
            SetProp(so, "_levelFailedContinueButton", failContBtn);

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(hudView);
        }

        private GameObject FindOrCreateChild(Transform parent, string childName)
        {
            var child = parent.Find(childName);
            GameObject go;
            if (child != null)
            {
                go = child.gameObject;
            }
            else
            {
                go = new GameObject(childName);
                go.transform.SetParent(parent, false);
            }

            if (parent.GetComponentInParent<Canvas>() != null && go.GetComponent<RectTransform>() == null)
            {
                go.AddComponent<RectTransform>();
            }
            return go;
        }

        private TMPro.TMP_Text EnsureTMPText(GameObject parent, string name)
        {
            var obj = FindOrCreateChild(parent.transform, name);
            var tmp = obj.GetComponent<TMPro.TMP_Text>();
            if (tmp == null) tmp = obj.AddComponent<TMPro.TextMeshProUGUI>();
            tmp.textWrappingMode = TMPro.TextWrappingModes.NoWrap;
            tmp.overflowMode = TMPro.TextOverflowModes.Overflow;
            return tmp;
        }

        private UnityEngine.UI.Button EnsureButton(GameObject parent, string name)
        {
            var obj = FindOrCreateChild(parent.transform, name);
            EnsureComponent<UnityEngine.UI.Image>(obj);
            var btn = obj.GetComponent<UnityEngine.UI.Button>();
            if (btn == null) btn = obj.AddComponent<UnityEngine.UI.Button>();
            return btn;
        }

        private void SetProp(SerializedObject so, string propName, Object val)
        {
            var prop = so.FindProperty(propName);
            if (prop != null) prop.objectReferenceValue = val;
        }

        private void EnsureMainMenuBindings(MainMenuView menuView)
        {
            if (menuView == null) return;
            var menuObj = menuView.gameObject;

            // Purge all old child objects under MainMenuView to force clean rebuild
            for (int i = menuObj.transform.childCount - 1; i >= 0; i--)
            {
                Object.DestroyImmediate(menuObj.transform.GetChild(i).gameObject);
            }

            var cg = menuObj.GetComponent<CanvasGroup>();
            if (cg == null) cg = menuObj.AddComponent<CanvasGroup>();

            var menuRect = menuObj.GetComponent<RectTransform>();
            if (menuRect != null)
            {
                menuRect.anchorMin = Vector2.zero;
                menuRect.anchorMax = Vector2.one;
                menuRect.sizeDelta = Vector2.zero;
                menuRect.anchoredPosition = Vector2.zero;
            }

            // Light Sky-Blue background per index.html
            var bgImg = menuObj.GetComponent<UnityEngine.UI.Image>();
            if (bgImg == null) bgImg = menuObj.AddComponent<UnityEngine.UI.Image>();
            bgImg.color = new Color(0.94f, 0.96f, 0.98f, 1f); // #EFF6FF

            // 1. Header Container & Controls (Title + Coin Pill + Settings Button)
            var titleText = EnsureTMPText(menuObj, "TitleText");
            titleText.text = "Color Jam 3D";
            titleText.fontSize = 38;
            titleText.fontStyle = TMPro.FontStyles.Bold;
            titleText.color = new Color(0.12f, 0.23f, 0.54f); // #1E3A8A Dark Indigo
            titleText.alignment = TMPro.TextAlignmentOptions.Left;
            var titleRect = titleText.GetComponent<RectTransform>();
            if (titleRect != null)
            {
                titleRect.anchorMin = new Vector2(0.06f, 0.88f);
                titleRect.anchorMax = new Vector2(0.60f, 0.96f);
                titleRect.sizeDelta = Vector2.zero;
            }

            // Coin Pill Container
            var coinPillObj = FindOrCreateChild(menuObj.transform, "CoinPill");
            var cpImg = coinPillObj.GetComponent<UnityEngine.UI.Image>();
            if (cpImg == null) cpImg = coinPillObj.AddComponent<UnityEngine.UI.Image>();
            cpImg.color = new Color(0.99f, 0.95f, 0.78f); // #FEF3C7 Soft Gold Pill
            var cpRect = coinPillObj.GetComponent<RectTransform>();
            if (cpRect != null)
            {
                cpRect.anchorMin = new Vector2(0.65f, 0.90f);
                cpRect.anchorMax = new Vector2(0.94f, 0.95f);
                cpRect.sizeDelta = Vector2.zero;
            }

            var coinText = EnsureTMPText(coinPillObj, "CoinText");
            coinText.text = "1,450 GOLD";
            coinText.fontSize = 20;
            coinText.fontStyle = TMPro.FontStyles.Bold;
            coinText.color = new Color(0.7f, 0.35f, 0.05f); // #B45309 Gold Text
            coinText.alignment = TMPro.TextAlignmentOptions.Center;
            var coinRect = coinText.GetComponent<RectTransform>();
            if (coinRect != null)
            {
                coinRect.anchorMin = Vector2.zero;
                coinRect.anchorMax = Vector2.one;
                coinRect.sizeDelta = Vector2.zero;
            }

            // 2. Vehicle Garage Showcase Card (White Rounded Card)
            var garageCardObj = FindOrCreateChild(menuObj.transform, "GarageCard");
            var cardImg = garageCardObj.GetComponent<UnityEngine.UI.Image>();
            if (cardImg == null) cardImg = garageCardObj.AddComponent<UnityEngine.UI.Image>();
            cardImg.color = Color.white; // Clean White Card per index.html
            var cardRect = garageCardObj.GetComponent<RectTransform>();
            if (cardRect != null)
            {
                cardRect.anchorMin = new Vector2(0.06f, 0.28f);
                cardRect.anchorMax = new Vector2(0.94f, 0.78f);
                cardRect.sizeDelta = Vector2.zero;
            }

            // Vehicle Preview Box inside card
            var previewObj = FindOrCreateChild(garageCardObj.transform, "VehiclePreviewBox");
            var prevImg = previewObj.GetComponent<UnityEngine.UI.Image>();
            if (prevImg == null) prevImg = previewObj.AddComponent<UnityEngine.UI.Image>();
            prevImg.color = new Color(0.88f, 0.95f, 0.99f); // #E0F2FE Sky Blue Preview
            var prevRect = previewObj.GetComponent<RectTransform>();
            if (prevRect != null)
            {
                prevRect.anchorMin = new Vector2(0.08f, 0.45f);
                prevRect.anchorMax = new Vector2(0.92f, 0.92f);
                prevRect.sizeDelta = Vector2.zero;
            }

            var prevText = EnsureTMPText(previewObj, "Text");
            prevText.text = "SEÇİLİ ARAÇ";
            prevText.fontSize = 24;
            prevText.fontStyle = TMPro.FontStyles.Bold;
            prevText.color = new Color(0.1f, 0.4f, 0.7f);
            prevText.alignment = TMPro.TextAlignmentOptions.Center;
            var ptRect = prevText.GetComponent<RectTransform>();
            if (ptRect != null) { ptRect.anchorMin = Vector2.zero; ptRect.anchorMax = Vector2.one; ptRect.sizeDelta = Vector2.zero; }

            var vehicleNameText = EnsureTMPText(garageCardObj, "VehicleNameText");
            vehicleNameText.text = "Dondurma Arabası";
            vehicleNameText.fontSize = 28;
            vehicleNameText.fontStyle = TMPro.FontStyles.Bold;
            vehicleNameText.color = new Color(0.06f, 0.09f, 0.16f); // #0F172A Dark Slate
            vehicleNameText.alignment = TMPro.TextAlignmentOptions.Center;
            var vehNameRect = vehicleNameText.GetComponent<RectTransform>();
            if (vehNameRect != null)
            {
                vehNameRect.anchorMin = new Vector2(0.05f, 0.32f);
                vehNameRect.anchorMax = new Vector2(0.95f, 0.42f);
                vehNameRect.sizeDelta = Vector2.zero;
            }

            var vehicleTypeText = EnsureTMPText(garageCardObj, "VehicleTypeText");
            vehicleTypeText.text = "KUŞANILAN SARI ARAÇ";
            vehicleTypeText.fontSize = 18;
            vehicleTypeText.fontStyle = TMPro.FontStyles.Bold;
            vehicleTypeText.color = new Color(0.39f, 0.45f, 0.55f); // #64748B Slate
            vehicleTypeText.alignment = TMPro.TextAlignmentOptions.Center;
            var vehTypeRect = vehicleTypeText.GetComponent<RectTransform>();
            if (vehTypeRect != null)
            {
                vehTypeRect.anchorMin = new Vector2(0.05f, 0.22f);
                vehTypeRect.anchorMax = new Vector2(0.95f, 0.30f);
                vehTypeRect.sizeDelta = Vector2.zero;
            }

            var garageBtn = EnsureButton(garageCardObj, "GarageButton");
            var gImg = garageBtn.GetComponent<UnityEngine.UI.Image>();
            if (gImg != null) gImg.color = new Color(0.23f, 0.51f, 0.96f); // #3B82F6 Blue
            var gBtnRect = garageBtn.GetComponent<RectTransform>();
            if (gBtnRect != null)
            {
                gBtnRect.anchorMin = new Vector2(0.08f, 0.05f);
                gBtnRect.anchorMax = new Vector2(0.92f, 0.18f);
                gBtnRect.sizeDelta = Vector2.zero;
            }

            var gText = EnsureTMPText(garageBtn.gameObject, "Text");
            gText.text = "GARAJI AÇ (12/24 SKIN)";
            gText.fontSize = 22;
            gText.fontStyle = TMPro.FontStyles.Bold;
            gText.color = Color.white;
            gText.alignment = TMPro.TextAlignmentOptions.Center;
            var gTextRect = gText.GetComponent<RectTransform>();
            if (gTextRect != null) { gTextRect.anchorMin = Vector2.zero; gTextRect.anchorMax = Vector2.one; gTextRect.sizeDelta = Vector2.zero; }

            // 3. PlayButton (Vibrant Emerald Green per index.html)
            var playBtn = EnsureButton(menuObj, "PlayButton");
            var pImg = playBtn.GetComponent<UnityEngine.UI.Image>();
            if (pImg != null) pImg.color = new Color(0.06f, 0.72f, 0.51f); // #10B981 Emerald
            var playRect = playBtn.GetComponent<RectTransform>();
            if (playRect != null)
            {
                playRect.anchorMin = new Vector2(0.06f, 0.14f);
                playRect.anchorMax = new Vector2(0.94f, 0.24f);
                playRect.sizeDelta = Vector2.zero;
            }

            var playButtonText = EnsureTMPText(playBtn.gameObject, "PlayButtonText");
            playButtonText.text = "OYUNA BAŞLA (SEVİYE 1)";
            playButtonText.fontSize = 30;
            playButtonText.fontStyle = TMPro.FontStyles.Bold;
            playButtonText.color = Color.white;
            playButtonText.alignment = TMPro.TextAlignmentOptions.Center;
            var pTextRect = playButtonText.GetComponent<RectTransform>();
            if (pTextRect != null) { pTextRect.anchorMin = Vector2.zero; pTextRect.anchorMax = Vector2.one; pTextRect.sizeDelta = Vector2.zero; }

            // 4. Settings Button
            var settingsBtn = EnsureButton(menuObj, "SettingsButton");
            var sImg = settingsBtn.GetComponent<UnityEngine.UI.Image>();
            if (sImg != null) sImg.color = new Color(0.20f, 0.25f, 0.33f); // #334155 Slate Button
            var sRect = settingsBtn.GetComponent<RectTransform>();
            if (sRect != null)
            {
                sRect.anchorMin = new Vector2(0.06f, 0.05f);
                sRect.anchorMax = new Vector2(0.94f, 0.12f);
                sRect.sizeDelta = Vector2.zero;
            }

            var sText = EnsureTMPText(settingsBtn.gameObject, "Text");
            sText.text = "AYARLAR";
            sText.fontSize = 22;
            sText.fontStyle = TMPro.FontStyles.Bold;
            sText.color = Color.white;
            sText.alignment = TMPro.TextAlignmentOptions.Center;
            var sTextRect = sText.GetComponent<RectTransform>();
            if (sTextRect != null) { sTextRect.anchorMin = Vector2.zero; sTextRect.anchorMax = Vector2.one; sTextRect.sizeDelta = Vector2.zero; }

            // Assign serialized properties
            var so = new SerializedObject(menuView);
            SetProp(so, "_titleText", titleText);
            SetProp(so, "_coinText", coinText);
            SetProp(so, "_garageCard", garageCardObj);
            SetProp(so, "_equippedVehicleNameText", vehicleNameText);
            SetProp(so, "_equippedVehicleTypeText", vehicleTypeText);
            SetProp(so, "_openGarageButton", garageBtn);
            SetProp(so, "_playButton", playBtn);
            SetProp(so, "_playButtonText", playButtonText);
            SetProp(so, "_settingsButton", settingsBtn);

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(menuView);
        }

        private void EnsureHUDBindings(HUDView hudView)
        {
            if (hudView == null) return;
            var hudObj = hudView.gameObject;

            // Purge all old child objects under HUDView to force clean rebuild
            for (int i = hudObj.transform.childCount - 1; i >= 0; i--)
            {
                Object.DestroyImmediate(hudObj.transform.GetChild(i).gameObject);
            }

            var cg = hudObj.GetComponent<CanvasGroup>();
            if (cg == null) cg = hudObj.AddComponent<CanvasGroup>();

            var hudRect = hudObj.GetComponent<RectTransform>();
            if (hudRect != null)
            {
                hudRect.anchorMin = Vector2.zero;
                hudRect.anchorMax = Vector2.one;
                hudRect.sizeDelta = Vector2.zero;
                hudRect.anchoredPosition = Vector2.zero;
            }

            // Transparent background for gameplay HUD
            var bgImg = hudObj.GetComponent<UnityEngine.UI.Image>();
            if (bgImg == null) bgImg = hudObj.AddComponent<UnityEngine.UI.Image>();
            bgImg.color = new Color(0, 0, 0, 0);

            // 1. TOP HUD BAR (Clean Pill Badges per gameplay-hud.html)
            var topBarObj = FindOrCreateChild(hudObj.transform, "TopHUDBar");
            var topBarRect = topBarObj.GetComponent<RectTransform>();
            if (topBarRect != null)
            {
                topBarRect.anchorMin = new Vector2(0.04f, 0.90f);
                topBarRect.anchorMax = new Vector2(0.96f, 0.97f);
                topBarRect.sizeDelta = Vector2.zero;
            }

            // Level Badge Pill (White rounded pill, indigo text)
            var levelBadgeObj = FindOrCreateChild(topBarObj.transform, "LevelBadge");
            var lbImg = levelBadgeObj.GetComponent<UnityEngine.UI.Image>();
            if (lbImg == null) lbImg = levelBadgeObj.AddComponent<UnityEngine.UI.Image>();
            lbImg.color = Color.white;
            var lbRect = levelBadgeObj.GetComponent<RectTransform>();
            if (lbRect != null)
            {
                lbRect.anchorMin = new Vector2(0f, 0.15f);
                lbRect.anchorMax = new Vector2(0.32f, 0.85f);
                lbRect.sizeDelta = Vector2.zero;
            }

            var levelTitleText = EnsureTMPText(levelBadgeObj, "LevelTitleText");
            levelTitleText.text = "SEVİYE 1";
            levelTitleText.fontSize = 22;
            levelTitleText.fontStyle = TMPro.FontStyles.Bold;
            levelTitleText.color = new Color(0.39f, 0.40f, 0.95f); // #6366F1 Indigo
            levelTitleText.alignment = TMPro.TextAlignmentOptions.Center;
            var ltRect = levelTitleText.GetComponent<RectTransform>();
            if (ltRect != null) { ltRect.anchorMin = Vector2.zero; ltRect.anchorMax = Vector2.one; ltRect.sizeDelta = Vector2.zero; }

            // Coin Counter Pill (Soft Gold Pill)
            var coinPillObj = FindOrCreateChild(topBarObj.transform, "CoinCounterPill");
            var cpImg = coinPillObj.GetComponent<UnityEngine.UI.Image>();
            if (cpImg == null) cpImg = coinPillObj.AddComponent<UnityEngine.UI.Image>();
            cpImg.color = new Color(0.99f, 0.95f, 0.78f); // #FEF3C7 Soft Gold Pill
            var cpRect = coinPillObj.GetComponent<RectTransform>();
            if (cpRect != null)
            {
                cpRect.anchorMin = new Vector2(0.36f, 0.15f);
                cpRect.anchorMax = new Vector2(0.78f, 0.85f);
                cpRect.sizeDelta = Vector2.zero;
            }

            var scoreText = EnsureTMPText(coinPillObj, "ScoreText");
            scoreText.text = "SKOR: 1,450";
            scoreText.fontSize = 22;
            scoreText.fontStyle = TMPro.FontStyles.Bold;
            scoreText.color = new Color(0.70f, 0.35f, 0.05f); // #B45309 Gold Text
            scoreText.alignment = TMPro.TextAlignmentOptions.Center;
            var stRect = scoreText.GetComponent<RectTransform>();
            if (stRect != null) { stRect.anchorMin = Vector2.zero; stRect.anchorMax = Vector2.one; stRect.sizeDelta = Vector2.zero; }

            // Pause Button (White rounded square, dark slate text)
            var pauseBtn = EnsureButton(topBarObj, "PauseButton");
            var pImg = pauseBtn.GetComponent<UnityEngine.UI.Image>();
            if (pImg != null) pImg.color = Color.white;
            var pRect = pauseBtn.GetComponent<RectTransform>();
            if (pRect != null)
            {
                pRect.anchorMin = new Vector2(0.82f, 0.10f);
                pRect.anchorMax = new Vector2(1.0f, 0.90f);
                pRect.sizeDelta = Vector2.zero;
            }

            var pauseText = EnsureTMPText(pauseBtn.gameObject, "Text");
            pauseText.text = "II";
            pauseText.fontSize = 24;
            pauseText.fontStyle = TMPro.FontStyles.Bold;
            pauseText.color = new Color(0.39f, 0.45f, 0.55f); // #64748B Slate
            pauseText.alignment = TMPro.TextAlignmentOptions.Center;
            var pTextRect = pauseText.GetComponent<RectTransform>();
            if (pTextRect != null) { pTextRect.anchorMin = Vector2.zero; pTextRect.anchorMax = Vector2.one; pTextRect.sizeDelta = Vector2.zero; }

            // 2. POWER-UP BOTTOM BAR (Floating White Dock per gameplay-hud.html)
            var powerUpDockObj = FindOrCreateChild(hudObj.transform, "PowerUpBar");
            var dockImg = powerUpDockObj.GetComponent<UnityEngine.UI.Image>();
            if (dockImg == null) dockImg = powerUpDockObj.AddComponent<UnityEngine.UI.Image>();
            dockImg.color = Color.white; // Floating White Dock
            var dockRect = powerUpDockObj.GetComponent<RectTransform>();
            if (dockRect != null)
            {
                dockRect.anchorMin = new Vector2(0.04f, 0.02f);
                dockRect.anchorMax = new Vector2(0.96f, 0.10f);
                dockRect.sizeDelta = Vector2.zero;
            }

            // Undo Button (Purple Pill)
            var undoBtn = EnsureButton(powerUpDockObj, "UndoButton");
            var uImg = undoBtn.GetComponent<UnityEngine.UI.Image>();
            if (uImg != null) uImg.color = new Color(0.55f, 0.36f, 0.96f); // #8B5CF6 Purple
            var uRect = undoBtn.GetComponent<RectTransform>();
            if (uRect != null)
            {
                uRect.anchorMin = new Vector2(0.04f, 0.15f);
                uRect.anchorMax = new Vector2(0.32f, 0.85f);
                uRect.sizeDelta = Vector2.zero;
            }
            var uText = EnsureTMPText(undoBtn.gameObject, "Text");
            uText.text = "VİYADÜK";
            uText.fontSize = 20;
            uText.fontStyle = TMPro.FontStyles.Bold;
            uText.color = Color.white;
            uText.alignment = TMPro.TextAlignmentOptions.Center;
            var uTextRect = uText.GetComponent<RectTransform>();
            if (uTextRect != null) { uTextRect.anchorMin = Vector2.zero; uTextRect.anchorMax = Vector2.one; uTextRect.sizeDelta = Vector2.zero; }

            // Hint Button (Sky Blue Pill)
            var hintBtn = EnsureButton(powerUpDockObj, "HintButton");
            var hImg = hintBtn.GetComponent<UnityEngine.UI.Image>();
            if (hImg != null) hImg.color = new Color(0.22f, 0.74f, 0.97f); // #38BDF8 Sky Blue
            var hRect = hintBtn.GetComponent<RectTransform>();
            if (hRect != null)
            {
                hRect.anchorMin = new Vector2(0.36f, 0.15f);
                hRect.anchorMax = new Vector2(0.64f, 0.85f);
                hRect.sizeDelta = Vector2.zero;
            }
            var hText = EnsureTMPText(hintBtn.gameObject, "Text");
            hText.text = "TEMİZLE";
            hText.fontSize = 20;
            hText.fontStyle = TMPro.FontStyles.Bold;
            hText.color = Color.white;
            hText.alignment = TMPro.TextAlignmentOptions.Center;
            var hTextRect = hText.GetComponent<RectTransform>();
            if (hTextRect != null) { hTextRect.anchorMin = Vector2.zero; hTextRect.anchorMax = Vector2.one; hTextRect.sizeDelta = Vector2.zero; }

            // Redo Button (Rose Red Pill)
            var redoBtn = EnsureButton(powerUpDockObj, "RedoButton");
            var rImg = redoBtn.GetComponent<UnityEngine.UI.Image>();
            if (rImg != null) rImg.color = new Color(0.96f, 0.25f, 0.37f); // #F43F5E Rose Red
            var rRect = redoBtn.GetComponent<RectTransform>();
            if (rRect != null)
            {
                rRect.anchorMin = new Vector2(0.68f, 0.15f);
                rRect.anchorMax = new Vector2(0.96f, 0.85f);
                rRect.sizeDelta = Vector2.zero;
            }
            var rText = EnsureTMPText(redoBtn.gameObject, "Text");
            rText.text = "GÖKKUŞAĞI";
            rText.fontSize = 20;
            rText.fontStyle = TMPro.FontStyles.Bold;
            rText.color = Color.white;
            rText.alignment = TMPro.TextAlignmentOptions.Center;
            var rTextRect = rText.GetComponent<RectTransform>();
            if (rTextRect != null) { rTextRect.anchorMin = Vector2.zero; rTextRect.anchorMax = Vector2.one; rTextRect.sizeDelta = Vector2.zero; }

            // 3. COMPLETION PANEL (Victory Modal)
            var compPanelObj = FindOrCreateChild(hudObj.transform, "CompletionPanel");
            var compImg = compPanelObj.GetComponent<UnityEngine.UI.Image>();
            if (compImg == null) compImg = compPanelObj.AddComponent<UnityEngine.UI.Image>();
            compImg.color = new Color(0.06f, 0.09f, 0.16f, 0.88f);
            var compRect = compPanelObj.GetComponent<RectTransform>();
            if (compRect != null)
            {
                compRect.anchorMin = Vector2.zero;
                compRect.anchorMax = Vector2.one;
                compRect.sizeDelta = Vector2.zero;
            }

            var compCardObj = FindOrCreateChild(compPanelObj.transform, "CompletionCard");
            var ccImg = compCardObj.GetComponent<UnityEngine.UI.Image>();
            if (ccImg == null) ccImg = compCardObj.AddComponent<UnityEngine.UI.Image>();
            ccImg.color = Color.white;
            var ccRect = compCardObj.GetComponent<RectTransform>();
            if (ccRect != null)
            {
                ccRect.anchorMin = new Vector2(0.08f, 0.25f);
                ccRect.anchorMax = new Vector2(0.92f, 0.75f);
                ccRect.sizeDelta = Vector2.zero;
            }

            var compText = EnsureTMPText(compCardObj, "CompletionText");
            compText.text = "SEVİYE TAMAMLANDI!";
            compText.fontSize = 32;
            compText.fontStyle = TMPro.FontStyles.Bold;
            compText.color = new Color(0.12f, 0.23f, 0.54f);
            compText.alignment = TMPro.TextAlignmentOptions.Center;
            var ctRect = compText.GetComponent<RectTransform>();
            if (ctRect != null) { ctRect.anchorMin = new Vector2(0.05f, 0.80f); ctRect.anchorMax = new Vector2(0.95f, 0.94f); ctRect.sizeDelta = Vector2.zero; }

            var nextBtn = EnsureButton(compCardObj, "NextLevelButton");
            var nImg = nextBtn.GetComponent<UnityEngine.UI.Image>();
            if (nImg != null) nImg.color = new Color(0.06f, 0.72f, 0.51f);
            var nRect = nextBtn.GetComponent<RectTransform>();
            if (nRect != null)
            {
                nRect.anchorMin = new Vector2(0.10f, 0.08f);
                nRect.anchorMax = new Vector2(0.90f, 0.24f);
                nRect.sizeDelta = Vector2.zero;
            }

            var nextText = EnsureTMPText(nextBtn.gameObject, "Text");
            nextText.text = "SONRAKİ SEVİYE";
            nextText.fontSize = 26;
            nextText.fontStyle = TMPro.FontStyles.Bold;
            nextText.color = Color.white;
            nextText.alignment = TMPro.TextAlignmentOptions.Center;
            var nTextRect = nextText.GetComponent<RectTransform>();
            if (nTextRect != null) { nTextRect.anchorMin = Vector2.zero; nTextRect.anchorMax = Vector2.one; nTextRect.sizeDelta = Vector2.zero; }

            // 4. LEVEL FAILED PANEL
            var failPanelObj = FindOrCreateChild(hudObj.transform, "LevelFailedPanel");
            var failImg = failPanelObj.GetComponent<UnityEngine.UI.Image>();
            if (failImg == null) failImg = failPanelObj.AddComponent<UnityEngine.UI.Image>();
            failImg.color = new Color(0.06f, 0.09f, 0.16f, 0.88f);
            var failRect = failPanelObj.GetComponent<RectTransform>();
            if (failRect != null)
            {
                failRect.anchorMin = Vector2.zero;
                failRect.anchorMax = Vector2.one;
                failRect.sizeDelta = Vector2.zero;
            }

            var failCardObj = FindOrCreateChild(failPanelObj.transform, "FailCard");
            var fcImg = failCardObj.GetComponent<UnityEngine.UI.Image>();
            if (fcImg == null) fcImg = failCardObj.AddComponent<UnityEngine.UI.Image>();
            fcImg.color = Color.white;
            var fcRect = failCardObj.GetComponent<RectTransform>();
            if (fcRect != null)
            {
                fcRect.anchorMin = new Vector2(0.08f, 0.25f);
                fcRect.anchorMax = new Vector2(0.92f, 0.75f);
                fcRect.sizeDelta = Vector2.zero;
            }

            var failText = EnsureTMPText(failCardObj, "LevelFailedText");
            failText.text = "SEVİYE BAŞARISIZ!";
            failText.fontSize = 32;
            failText.fontStyle = TMPro.FontStyles.Bold;
            failText.color = new Color(0.85f, 0.2f, 0.2f);
            failText.alignment = TMPro.TextAlignmentOptions.Center;
            var fTextRect = failText.GetComponent<RectTransform>();
            if (fTextRect != null) { fTextRect.anchorMin = new Vector2(0.05f, 0.80f); fTextRect.anchorMax = new Vector2(0.95f, 0.94f); fTextRect.sizeDelta = Vector2.zero; }

            var retryBtn = EnsureButton(failCardObj, "RetryButton");
            var rtryImg = retryBtn.GetComponent<UnityEngine.UI.Image>();
            if (rtryImg != null) rtryImg.color = new Color(0.9f, 0.25f, 0.25f);
            var retryRect = retryBtn.GetComponent<RectTransform>();
            if (retryRect != null)
            {
                retryRect.anchorMin = new Vector2(0.10f, 0.08f);
                retryRect.anchorMax = new Vector2(0.90f, 0.24f);
                retryRect.sizeDelta = Vector2.zero;
            }

            var retryText = EnsureTMPText(retryBtn.gameObject, "Text");
            retryText.text = "TEKRAR DENE";
            retryText.fontSize = 26;
            retryText.fontStyle = TMPro.FontStyles.Bold;
            retryText.color = Color.white;
            retryText.alignment = TMPro.TextAlignmentOptions.Center;
            var rtryTextRect = retryText.GetComponent<RectTransform>();
            if (rtryTextRect != null) { rtryTextRect.anchorMin = Vector2.zero; rtryTextRect.anchorMax = Vector2.one; rtryTextRect.sizeDelta = Vector2.zero; }

            compPanelObj.SetActive(false);
            failPanelObj.SetActive(false);

            // Assign serialized properties via SerializedObject
            var so = new SerializedObject(hudView);
            SetProp(so, "_scoreText", scoreText);
            SetProp(so, "_levelTitleText", levelTitleText);
            SetProp(so, "_pauseButton", pauseBtn);
            SetProp(so, "_undoButton", undoBtn);
            SetProp(so, "_hintButton", hintBtn);
            SetProp(so, "_redoButton", redoBtn);

            SetProp(so, "_completionPanel", compPanelObj);
            SetProp(so, "_completionText", compText);
            SetProp(so, "_nextLevelButton", nextBtn);

            SetProp(so, "_levelFailedPanel", failPanelObj);
            SetProp(so, "_levelFailedText", failText);
            SetProp(so, "_retryButton", retryBtn);

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(hudView);
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
            if (root == null) return;
            var serialized = new SerializedObject(root);
            var property = serialized.FindProperty("contextData");
            if (property != null)
            {
                var contextDataAsset = AssetDatabase.LoadAssetAtPath<ContextData>("Assets/Settings/PixelFlowContextData.asset");
                if (contextDataAsset == null)
                {
                    contextDataAsset = AssetDatabase.LoadAssetAtPath<ContextData>("Assets/NewContextData.asset");
                }
                if (contextDataAsset == null)
                {
                    if (!Directory.Exists("Assets/Settings")) Directory.CreateDirectory("Assets/Settings");
                    contextDataAsset = ScriptableObject.CreateInstance<ContextData>();
                    AssetDatabase.CreateAsset(contextDataAsset, "Assets/Settings/PixelFlowContextData.asset");
                    AssetDatabase.SaveAssets();
                }
                property.objectReferenceValue = contextDataAsset;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(root);
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
            var splashView = Object.FindAnyObjectByType<SplashView>(FindObjectsInactive.Include);
            if (splashView != null) EnsureSplashBindings(splashView);

            EnsureViewUnderCanvas<BloomFlashView>(canvas, "BloomFlashOverlay", addImage: true);
            EnsureViewUnderCanvas<ConfettiView>(canvas, "ConfettiView");

            EnsureViewUnderCanvas<SettingsView>(canvas, "SettingsView");
            var settingsView = Object.FindAnyObjectByType<SettingsView>(FindObjectsInactive.Include);
            if (settingsView != null) EnsureSettingsBindings(settingsView);

            EnsureViewUnderCanvas<GarageView>(canvas, "GarageView");
            var garageView = Object.FindAnyObjectByType<GarageView>(FindObjectsInactive.Include);
            if (garageView != null) EnsureGarageBindings(garageView);

            EnsureViewUnderCanvas<DailyCrisisView>(canvas, "DailyCrisisView");
            var crisisView = Object.FindAnyObjectByType<DailyCrisisView>(FindObjectsInactive.Include);
            if (crisisView != null) EnsureDailyCrisisBindings(crisisView);

            EnsureViewUnderCanvas<TutorialView>(canvas, "TutorialView");
        }

        private void EnsureSplashBindings(SplashView splashView)
        {
            if (splashView == null) return;
            var splashObj = splashView.gameObject;

            var cg = splashObj.GetComponent<CanvasGroup>();
            if (cg == null) cg = splashObj.AddComponent<CanvasGroup>();

            var splashRect = splashObj.GetComponent<RectTransform>();
            if (splashRect != null)
            {
                splashRect.anchorMin = Vector2.zero;
                splashRect.anchorMax = Vector2.one;
                splashRect.sizeDelta = Vector2.zero;
                splashRect.anchoredPosition = Vector2.zero;
            }

            var bgImg = splashObj.GetComponent<UnityEngine.UI.Image>();
            if (bgImg == null) bgImg = splashObj.AddComponent<UnityEngine.UI.Image>();
            bgImg.color = new Color(0.23f, 0.51f, 0.96f, 1f);

            var titleText = EnsureTMPText(splashObj, "SplashTitleText");
            titleText.text = "Color Jam 3D";
            titleText.fontSize = 54;
            titleText.fontStyle = TMPro.FontStyles.Bold;
            titleText.color = Color.white;
            titleText.alignment = TMPro.TextAlignmentOptions.Center;
            var tRect = titleText.GetComponent<RectTransform>();
            if (tRect != null)
            {
                tRect.anchorMin = new Vector2(0.05f, 0.58f);
                tRect.anchorMax = new Vector2(0.95f, 0.72f);
                tRect.sizeDelta = Vector2.zero;
                tRect.anchoredPosition = Vector2.zero;
            }

            var subText = EnsureTMPText(splashObj, "SplashSubtitleText");
            subText.text = "TRAFFIC FLOW & COLLECTION";
            subText.fontSize = 22;
            subText.fontStyle = TMPro.FontStyles.Bold;
            subText.color = new Color(1f, 1f, 1f, 0.85f);
            subText.alignment = TMPro.TextAlignmentOptions.Center;
            var sRect = subText.GetComponent<RectTransform>();
            if (sRect != null)
            {
                sRect.anchorMin = new Vector2(0.05f, 0.38f);
                sRect.anchorMax = new Vector2(0.95f, 0.48f);
                sRect.sizeDelta = Vector2.zero;
            }

            splashView.SetVisible(false);
        }

        private void EnsureGarageBindings(GarageView garageView)
        {
            if (garageView == null) return;
            var garageObj = garageView.gameObject;

            var cg = garageObj.GetComponent<CanvasGroup>();
            if (cg == null) cg = garageObj.AddComponent<CanvasGroup>();

            var gRect = garageObj.GetComponent<RectTransform>();
            if (gRect != null)
            {
                gRect.anchorMin = Vector2.zero;
                gRect.anchorMax = Vector2.one;
                gRect.sizeDelta = Vector2.zero;
                gRect.anchoredPosition = Vector2.zero;
            }

            var bgImg = garageObj.GetComponent<UnityEngine.UI.Image>();
            if (bgImg == null) bgImg = garageObj.AddComponent<UnityEngine.UI.Image>();
            bgImg.color = new Color(0.05f, 0.07f, 0.12f, 0.85f);

            var cardObj = FindOrCreateChild(garageObj.transform, "GarageCard");
            var cardImg = cardObj.GetComponent<UnityEngine.UI.Image>();
            if (cardImg == null) cardImg = cardObj.AddComponent<UnityEngine.UI.Image>();
            cardImg.color = new Color(0.96f, 0.98f, 1f, 0.98f);
            var cardRect = cardObj.GetComponent<RectTransform>();
            if (cardRect != null)
            {
                cardRect.anchorMin = new Vector2(0.05f, 0.08f);
                cardRect.anchorMax = new Vector2(0.95f, 0.92f);
                cardRect.sizeDelta = Vector2.zero;
                cardRect.anchoredPosition = Vector2.zero;
            }

            // Purge old children under cardObj for a clean layout
            for (int i = cardObj.transform.childCount - 1; i >= 0; i--)
            {
                Object.DestroyImmediate(cardObj.transform.GetChild(i).gameObject);
            }

            var titleText = EnsureTMPText(cardObj, "GarageTitleText");
            titleText.text = "GARAJ & ARAÇ SKİNLERİ";
            titleText.fontSize = 32;
            titleText.fontStyle = TMPro.FontStyles.Bold;
            titleText.color = new Color(0.06f, 0.23f, 0.54f);
            titleText.alignment = TMPro.TextAlignmentOptions.Center;
            var tRect = titleText.GetComponent<RectTransform>();
            if (tRect != null)
            {
                tRect.anchorMin = new Vector2(0.05f, 0.88f);
                tRect.anchorMax = new Vector2(0.95f, 0.96f);
                tRect.sizeDelta = Vector2.zero;
                tRect.anchoredPosition = Vector2.zero;
            }

            var coinsText = EnsureTMPText(cardObj, "CoinsText");
            coinsText.text = "1,450 GOLD";
            coinsText.fontSize = 24;
            coinsText.fontStyle = TMPro.FontStyles.Bold;
            coinsText.color = new Color(0.7f, 0.35f, 0.05f);
            coinsText.alignment = TMPro.TextAlignmentOptions.Center;
            var cRect = coinsText.GetComponent<RectTransform>();
            if (cRect != null)
            {
                cRect.anchorMin = new Vector2(0.1f, 0.80f);
                cRect.anchorMax = new Vector2(0.9f, 0.87f);
                cRect.sizeDelta = Vector2.zero;
                cRect.anchoredPosition = Vector2.zero;
            }

            // Skin Container (Scroll/Grid Area)
            var skinContainerObj = FindOrCreateChild(cardObj.transform, "Container");
            var scRect = skinContainerObj.GetComponent<RectTransform>();
            if (scRect != null)
            {
                scRect.anchorMin = new Vector2(0.05f, 0.20f);
                scRect.anchorMax = new Vector2(0.95f, 0.78f);
                scRect.sizeDelta = Vector2.zero;
                scRect.anchoredPosition = Vector2.zero;
            }

            var closeBtn = EnsureButton(cardObj, "CloseButton");
            var closeImg = closeBtn.GetComponent<UnityEngine.UI.Image>();
            if (closeImg != null) closeImg.color = new Color(0.2f, 0.25f, 0.35f);
            var closeRect = closeBtn.GetComponent<RectTransform>();
            if (closeRect != null)
            {
                closeRect.anchorMin = new Vector2(0.15f, 0.05f);
                closeRect.anchorMax = new Vector2(0.85f, 0.16f);
                closeRect.sizeDelta = Vector2.zero;
                closeRect.anchoredPosition = Vector2.zero;
            }

            var closeText = EnsureTMPText(closeBtn.gameObject, "Text");
            closeText.text = "KAPAT";
            closeText.fontSize = 26;
            closeText.fontStyle = TMPro.FontStyles.Bold;
            closeText.color = Color.white;
            closeText.alignment = TMPro.TextAlignmentOptions.Center;
            var cTextRect = closeText.GetComponent<RectTransform>();
            if (cTextRect != null)
            {
                cTextRect.anchorMin = Vector2.zero;
                cTextRect.anchorMax = Vector2.one;
                cTextRect.sizeDelta = Vector2.zero;
            }

            var so = new SerializedObject(garageView);
            SetProp(so, "_panel", garageObj);
            SetProp(so, "_closeButton", closeBtn);
            SetProp(so, "_coinsText", coinsText);
            SetProp(so, "_skinContainer", skinContainerObj.transform);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(garageView);

            garageView.SetActive(false);
        }

        private void EnsureDailyCrisisBindings(DailyCrisisView crisisView)
        {
            if (crisisView == null) return;
            var crisisObj = crisisView.gameObject;

            var cg = crisisObj.GetComponent<CanvasGroup>();
            if (cg == null) cg = crisisObj.AddComponent<CanvasGroup>();

            var cRect = crisisObj.GetComponent<RectTransform>();
            if (cRect != null)
            {
                cRect.anchorMin = Vector2.zero;
                cRect.anchorMax = Vector2.one;
                cRect.sizeDelta = Vector2.zero;
                cRect.anchoredPosition = Vector2.zero;
            }

            var bgImg = crisisObj.GetComponent<UnityEngine.UI.Image>();
            if (bgImg == null) bgImg = crisisObj.AddComponent<UnityEngine.UI.Image>();
            bgImg.color = new Color(0.05f, 0.07f, 0.12f, 0.85f);

            var cardObj = FindOrCreateChild(crisisObj.transform, "CrisisCard");
            var cardImg = cardObj.GetComponent<UnityEngine.UI.Image>();
            if (cardImg == null) cardImg = cardObj.AddComponent<UnityEngine.UI.Image>();
            cardImg.color = new Color(0.98f, 0.98f, 1f, 0.98f);
            var cardRect = cardObj.GetComponent<RectTransform>();
            if (cardRect != null)
            {
                cardRect.anchorMin = new Vector2(0.06f, 0.10f);
                cardRect.anchorMax = new Vector2(0.94f, 0.90f);
                cardRect.sizeDelta = Vector2.zero;
                cardRect.anchoredPosition = Vector2.zero;
            }

            // Purge old children
            for (int i = cardObj.transform.childCount - 1; i >= 0; i--)
            {
                Object.DestroyImmediate(cardObj.transform.GetChild(i).gameObject);
            }

            var titleText = EnsureTMPText(cardObj, "CrisisTitleText");
            titleText.text = "GÜNLÜK KRİZ GÖREVLERİ";
            titleText.fontSize = 30;
            titleText.fontStyle = TMPro.FontStyles.Bold;
            titleText.color = new Color(0.85f, 0.2f, 0.2f);
            titleText.alignment = TMPro.TextAlignmentOptions.Center;
            var tRect = titleText.GetComponent<RectTransform>();
            if (tRect != null)
            {
                tRect.anchorMin = new Vector2(0.05f, 0.88f);
                tRect.anchorMax = new Vector2(0.95f, 0.96f);
                tRect.sizeDelta = Vector2.zero;
                tRect.anchoredPosition = Vector2.zero;
            }

            var streakText = EnsureTMPText(cardObj, "StreakText");
            streakText.text = "Galibiyet Serisi: 3 Gün";
            streakText.fontSize = 22;
            streakText.fontStyle = TMPro.FontStyles.Bold;
            streakText.color = new Color(0.85f, 0.45f, 0.05f);
            streakText.alignment = TMPro.TextAlignmentOptions.Center;
            var stRect = streakText.GetComponent<RectTransform>();
            if (stRect != null)
            {
                stRect.anchorMin = new Vector2(0.05f, 0.80f);
                stRect.anchorMax = new Vector2(0.95f, 0.87f);
                stRect.sizeDelta = Vector2.zero;
                stRect.anchoredPosition = Vector2.zero;
            }

            // Easy Crisis Button
            var easyBtn = EnsureButton(cardObj, "EasyButton");
            var eImg = easyBtn.GetComponent<UnityEngine.UI.Image>();
            if (eImg != null) eImg.color = new Color(0.12f, 0.82f, 0.38f);
            var eRect = easyBtn.GetComponent<RectTransform>();
            if (eRect != null)
            {
                eRect.anchorMin = new Vector2(0.10f, 0.58f);
                eRect.anchorMax = new Vector2(0.90f, 0.72f);
                eRect.sizeDelta = Vector2.zero;
                eRect.anchoredPosition = Vector2.zero;
            }
            var eText = EnsureTMPText(easyBtn.gameObject, "Text");
            eText.text = "KOLAY KRİZ (100 COIN)";
            eText.fontSize = 22;
            eText.fontStyle = TMPro.FontStyles.Bold;
            eText.color = Color.white;
            eText.alignment = TMPro.TextAlignmentOptions.Center;
            var eTForm = eText.GetComponent<RectTransform>();
            if (eTForm != null) { eTForm.anchorMin = Vector2.zero; eTForm.anchorMax = Vector2.one; eTForm.sizeDelta = Vector2.zero; }

            // Medium Crisis Button
            var medBtn = EnsureButton(cardObj, "MediumButton");
            var mImg = medBtn.GetComponent<UnityEngine.UI.Image>();
            if (mImg != null) mImg.color = new Color(0.95f, 0.6f, 0.1f);
            var mRect = medBtn.GetComponent<RectTransform>();
            if (mRect != null)
            {
                mRect.anchorMin = new Vector2(0.10f, 0.40f);
                mRect.anchorMax = new Vector2(0.90f, 0.54f);
                mRect.sizeDelta = Vector2.zero;
                mRect.anchoredPosition = Vector2.zero;
            }
            var mText = EnsureTMPText(medBtn.gameObject, "Text");
            mText.text = "ORTA KRİZ (250 COIN)";
            mText.fontSize = 22;
            mText.fontStyle = TMPro.FontStyles.Bold;
            mText.color = Color.white;
            mText.alignment = TMPro.TextAlignmentOptions.Center;
            var mTForm = mText.GetComponent<RectTransform>();
            if (mTForm != null) { mTForm.anchorMin = Vector2.zero; mTForm.anchorMax = Vector2.one; mTForm.sizeDelta = Vector2.zero; }

            // Hard Crisis Button
            var hardBtn = EnsureButton(cardObj, "HardButton");
            var hImg = hardBtn.GetComponent<UnityEngine.UI.Image>();
            if (hImg != null) hImg.color = new Color(0.9f, 0.25f, 0.25f);
            var hRect = hardBtn.GetComponent<RectTransform>();
            if (hRect != null)
            {
                hRect.anchorMin = new Vector2(0.10f, 0.22f);
                hRect.anchorMax = new Vector2(0.90f, 0.36f);
                hRect.sizeDelta = Vector2.zero;
                hRect.anchoredPosition = Vector2.zero;
            }
            var hText = EnsureTMPText(hardBtn.gameObject, "Text");
            hText.text = "ZOR KRİZ (500 COIN)";
            hText.fontSize = 22;
            hText.fontStyle = TMPro.FontStyles.Bold;
            hText.color = Color.white;
            hText.alignment = TMPro.TextAlignmentOptions.Center;
            var hTForm = hText.GetComponent<RectTransform>();
            if (hTForm != null) { hTForm.anchorMin = Vector2.zero; hTForm.anchorMax = Vector2.one; hTForm.sizeDelta = Vector2.zero; }

            var closeBtn = EnsureButton(cardObj, "CloseButton");
            var closeImg = closeBtn.GetComponent<UnityEngine.UI.Image>();
            if (closeImg != null) closeImg.color = new Color(0.2f, 0.25f, 0.35f);
            var closeRect = closeBtn.GetComponent<RectTransform>();
            if (closeRect != null)
            {
                closeRect.anchorMin = new Vector2(0.15f, 0.05f);
                closeRect.anchorMax = new Vector2(0.85f, 0.16f);
                closeRect.sizeDelta = Vector2.zero;
                closeRect.anchoredPosition = Vector2.zero;
            }

            var closeText = EnsureTMPText(closeBtn.gameObject, "Text");
            closeText.text = "KAPAT";
            closeText.fontSize = 26;
            closeText.fontStyle = TMPro.FontStyles.Bold;
            closeText.color = Color.white;
            closeText.alignment = TMPro.TextAlignmentOptions.Center;
            var cTextRect = closeText.GetComponent<RectTransform>();
            if (cTextRect != null) { cTextRect.anchorMin = Vector2.zero; cTextRect.anchorMax = Vector2.one; cTextRect.sizeDelta = Vector2.zero; }

            var so = new SerializedObject(crisisView);
            SetProp(so, "_panelContainer", crisisObj);
            SetProp(so, "_titleText", titleText);
            SetProp(so, "_streakText", streakText);
            SetProp(so, "_easyButton", easyBtn);
            SetProp(so, "_mediumButton", medBtn);
            SetProp(so, "_hardButton", hardBtn);
            SetProp(so, "_closeButton", closeBtn);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(crisisView);

            crisisView.Hide();
        }

        private void EnsureSettingsBindings(SettingsView settingsView)
        {
            if (settingsView == null) return;
            var settingsObj = settingsView.gameObject;

            var cg = settingsObj.GetComponent<CanvasGroup>();
            if (cg == null) cg = settingsObj.AddComponent<CanvasGroup>();

            var settingsRect = settingsObj.GetComponent<RectTransform>();
            if (settingsRect != null)
            {
                settingsRect.anchorMin = Vector2.zero;
                settingsRect.anchorMax = Vector2.one;
                settingsRect.sizeDelta = Vector2.zero;
                settingsRect.anchoredPosition = Vector2.zero;
            }

            var bgImg = settingsObj.GetComponent<UnityEngine.UI.Image>();
            if (bgImg == null) bgImg = settingsObj.AddComponent<UnityEngine.UI.Image>();
            bgImg.color = new Color(0.05f, 0.07f, 0.12f, 0.85f);

            var cardObj = FindOrCreateChild(settingsObj.transform, "SettingsCard");
            var cardImg = cardObj.GetComponent<UnityEngine.UI.Image>();
            if (cardImg == null) cardImg = cardObj.AddComponent<UnityEngine.UI.Image>();
            cardImg.color = new Color(0.96f, 0.98f, 1f, 0.98f);
            var cardRect = cardObj.GetComponent<RectTransform>();
            if (cardRect != null)
            {
                cardRect.anchorMin = new Vector2(0.05f, 0.08f);
                cardRect.anchorMax = new Vector2(0.95f, 0.92f);
                cardRect.sizeDelta = Vector2.zero;
                cardRect.anchoredPosition = Vector2.zero;
            }

            // Purge old children
            for (int i = cardObj.transform.childCount - 1; i >= 0; i--)
            {
                Object.DestroyImmediate(cardObj.transform.GetChild(i).gameObject);
            }

            var titleText = EnsureTMPText(cardObj, "TitleText");
            titleText.text = "AYARLAR & SEVİYE SEÇİMİ";
            titleText.fontSize = 30;
            titleText.fontStyle = TMPro.FontStyles.Bold;
            titleText.color = new Color(0.06f, 0.15f, 0.35f);
            titleText.alignment = TMPro.TextAlignmentOptions.Center;
            var titleRect = titleText.GetComponent<RectTransform>();
            if (titleRect != null)
            {
                titleRect.anchorMin = new Vector2(0.05f, 0.88f);
                titleRect.anchorMax = new Vector2(0.95f, 0.96f);
                titleRect.sizeDelta = Vector2.zero;
                titleRect.anchoredPosition = Vector2.zero;
            }

            var closeBtn = EnsureButton(cardObj, "CloseButton");
            var closeImg = closeBtn.GetComponent<UnityEngine.UI.Image>();
            if (closeImg != null) closeImg.color = new Color(0.12f, 0.82f, 0.38f);
            var closeRect = closeBtn.GetComponent<RectTransform>();
            if (closeRect != null)
            {
                closeRect.anchorMin = new Vector2(0.15f, 0.05f);
                closeRect.anchorMax = new Vector2(0.85f, 0.16f);
                closeRect.sizeDelta = Vector2.zero;
                closeRect.anchoredPosition = Vector2.zero;
            }

            var closeText = EnsureTMPText(closeBtn.gameObject, "Text");
            closeText.text = "DEVAM ET";
            closeText.fontSize = 30;
            closeText.fontStyle = TMPro.FontStyles.Bold;
            closeText.color = Color.white;
            closeText.alignment = TMPro.TextAlignmentOptions.Center;
            var cTextRect = closeText.GetComponent<RectTransform>();
            if (cTextRect != null)
            {
                cTextRect.anchorMin = Vector2.zero;
                cTextRect.anchorMax = Vector2.one;
                cTextRect.sizeDelta = Vector2.zero;
            }

            var so = new SerializedObject(settingsView);
            SetProp(so, "_settingsCanvas", settingsObj);
            SetProp(so, "_closeButton", closeBtn);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(settingsView);

            settingsView.SetVisible(false);
        }

        private void EnsureViewUnderCanvas<T>(Transform canvas, string name, bool addImage = false) where T : Component
        {
            var existing = Object.FindAnyObjectByType<T>(FindObjectsInactive.Include);
            GameObject obj;
            if (existing != null)
            {
                obj = existing.gameObject;
                if (obj.transform.parent != canvas)
                {
                    obj.transform.SetParent(canvas, false);
                }
            }
            else
            {
                obj = FindOrCreateChild(canvas, name);
                EnsureComponent<T>(obj);
            }

            var rt = obj.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.sizeDelta = Vector2.zero;
                rt.anchoredPosition = Vector2.zero;
            }

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
