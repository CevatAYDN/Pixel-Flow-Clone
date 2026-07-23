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

            var hudRect = hudObj.GetComponent<RectTransform>();
            if (hudRect != null)
            {
                hudRect.anchorMin = Vector2.zero;
                hudRect.anchorMax = Vector2.one;
                hudRect.sizeDelta = Vector2.zero;
            }

            // ═══════════════════════════════════════════════════
            // 1. TOP HUD BAR (Level Badge, Coin Counter, Pause Button)
            // ═══════════════════════════════════════════════════
            var topBarObj = FindOrCreateChild(hudObj.transform, "TopHUDBar");
            var topBgImg = topBarObj.GetComponent<UnityEngine.UI.Image>();
            if (topBgImg == null) topBgImg = topBarObj.AddComponent<UnityEngine.UI.Image>();
            topBgImg.color = new Color(0.08f, 0.10f, 0.16f, 0.85f); // Header background bar

            var topRect = topBarObj.GetComponent<RectTransform>();
            if (topRect != null)
            {
                topRect.anchorMin = new Vector2(0.04f, 0.88f);
                topRect.anchorMax = new Vector2(0.96f, 0.97f);
                topRect.sizeDelta = Vector2.zero;
            }

            // Level Title Badge
            var levelBadgeObj = FindOrCreateChild(topBarObj.transform, "LevelBadge");
            var levelBgImg = levelBadgeObj.GetComponent<UnityEngine.UI.Image>();
            if (levelBgImg == null) levelBgImg = levelBadgeObj.AddComponent<UnityEngine.UI.Image>();
            levelBgImg.color = new Color(0.24f, 0.28f, 0.42f, 0.95f);
            var levelBadgeRect = levelBadgeObj.GetComponent<RectTransform>();
            if (levelBadgeRect != null)
            {
                levelBadgeRect.anchorMin = new Vector2(0.03f, 0.15f);
                levelBadgeRect.anchorMax = new Vector2(0.38f, 0.85f);
                levelBadgeRect.sizeDelta = Vector2.zero;
            }

            var levelTitleText = EnsureTMPText(levelBadgeObj, "LevelTitleText");
            levelTitleText.text = "SEVİYE 1";
            levelTitleText.fontSize = 24;
            levelTitleText.fontStyle = TMPro.FontStyles.Bold;
            levelTitleText.color = Color.white;
            levelTitleText.alignment = TMPro.TextAlignmentOptions.Center;
            var lTextRect = levelTitleText.GetComponent<RectTransform>();
            if (lTextRect != null)
            {
                lTextRect.anchorMin = Vector2.zero;
                lTextRect.anchorMax = Vector2.one;
                lTextRect.sizeDelta = Vector2.zero;
            }

            // Coin / Score Counter
            var coinCounterObj = FindOrCreateChild(topBarObj.transform, "CoinCounter");
            var coinBgImg = coinCounterObj.GetComponent<UnityEngine.UI.Image>();
            if (coinBgImg == null) coinBgImg = coinCounterObj.AddComponent<UnityEngine.UI.Image>();
            coinBgImg.color = new Color(0.24f, 0.28f, 0.42f, 0.95f);
            var coinCounterRect = coinCounterObj.GetComponent<RectTransform>();
            if (coinCounterRect != null)
            {
                coinCounterRect.anchorMin = new Vector2(0.42f, 0.15f);
                coinCounterRect.anchorMax = new Vector2(0.78f, 0.85f);
                coinCounterRect.sizeDelta = Vector2.zero;
            }

            var scoreText = EnsureTMPText(coinCounterObj, "ScoreText");
            scoreText.text = "SKOR: 1,450";
            scoreText.fontSize = 22;
            scoreText.fontStyle = TMPro.FontStyles.Bold;
            scoreText.color = new Color(1f, 0.85f, 0.2f);
            scoreText.alignment = TMPro.TextAlignmentOptions.Center;
            var sTextRect = scoreText.GetComponent<RectTransform>();
            if (sTextRect != null)
            {
                sTextRect.anchorMin = Vector2.zero;
                sTextRect.anchorMax = Vector2.one;
                sTextRect.sizeDelta = Vector2.zero;
            }

            // Pause Button
            var pauseBtn = EnsureButton(topBarObj, "PauseButton");
            var pauseImg = pauseBtn.GetComponent<UnityEngine.UI.Image>();
            if (pauseImg != null) pauseImg.color = new Color(0.35f, 0.40f, 0.55f, 0.95f);
            var pauseRect = pauseBtn.GetComponent<RectTransform>();
            if (pauseRect != null)
            {
                pauseRect.anchorMin = new Vector2(0.82f, 0.15f);
                pauseRect.anchorMax = new Vector2(0.97f, 0.85f);
                pauseRect.sizeDelta = Vector2.zero;
            }

            var pauseText = EnsureTMPText(pauseBtn.gameObject, "Text");
            pauseText.text = "II";
            pauseText.fontSize = 22;
            pauseText.fontStyle = TMPro.FontStyles.Bold;
            pauseText.color = Color.white;
            pauseText.alignment = TMPro.TextAlignmentOptions.Center;
            var pTextRect = pauseText.GetComponent<RectTransform>();
            if (pTextRect != null)
            {
                pTextRect.anchorMin = Vector2.zero;
                pTextRect.anchorMax = Vector2.one;
                pTextRect.sizeDelta = Vector2.zero;
            }

            var timerText = EnsureTMPText(topBarObj, "TimerText");
            timerText.gameObject.SetActive(false);

            // ═══════════════════════════════════════════════════
            // 2. BOTTOM POWER-UP DOCK BAR (Viaduct, Clear/Hint, Rainbow)
            // ═══════════════════════════════════════════════════
            var bottomDockObj = FindOrCreateChild(hudObj.transform, "PowerUpBar");
            var dockImg = bottomDockObj.GetComponent<UnityEngine.UI.Image>();
            if (dockImg == null) dockImg = bottomDockObj.AddComponent<UnityEngine.UI.Image>();
            dockImg.color = new Color(0.08f, 0.10f, 0.16f, 0.92f); // Floating Dark Dock
            var dockRect = bottomDockObj.GetComponent<RectTransform>();
            if (dockRect != null)
            {
                dockRect.anchorMin = new Vector2(0.04f, 0.02f);
                dockRect.anchorMax = new Vector2(0.96f, 0.10f);
                dockRect.sizeDelta = Vector2.zero;
            }

            // Undo / Viaduct Button
            var undoBtn = EnsureButton(bottomDockObj, "UndoButton");
            var uImg = undoBtn.GetComponent<UnityEngine.UI.Image>();
            if (uImg != null) uImg.color = new Color(0.54f, 0.36f, 0.96f); // Purple Viaduct
            var uRect = undoBtn.GetComponent<RectTransform>();
            if (uRect != null)
            {
                uRect.anchorMin = new Vector2(0.05f, 0.10f);
                uRect.anchorMax = new Vector2(0.32f, 0.90f);
                uRect.sizeDelta = Vector2.zero;
            }
            var uText = EnsureTMPText(undoBtn.gameObject, "Text");
            uText.text = "VİYADÜK";
            uText.fontSize = 18;
            uText.fontStyle = TMPro.FontStyles.Bold;
            uText.color = Color.white;
            uText.alignment = TMPro.TextAlignmentOptions.Center;
            var uTextRect = uText.GetComponent<RectTransform>();
            if (uTextRect != null)
            {
                uTextRect.anchorMin = Vector2.zero;
                uTextRect.anchorMax = Vector2.one;
                uTextRect.sizeDelta = Vector2.zero;
            }

            // Hint / Clear Button
            var hintBtn = EnsureButton(bottomDockObj, "HintButton");
            var hImg = hintBtn.GetComponent<UnityEngine.UI.Image>();
            if (hImg != null) hImg.color = new Color(0.22f, 0.74f, 0.97f); // Sky Blue
            var hRect = hintBtn.GetComponent<RectTransform>();
            if (hRect != null)
            {
                hRect.anchorMin = new Vector2(0.36f, 0.10f);
                hRect.anchorMax = new Vector2(0.64f, 0.90f);
                hRect.sizeDelta = Vector2.zero;
            }
            var hintCountText = EnsureTMPText(hintBtn.gameObject, "HintCountText");
            hintCountText.text = "TEMİZLE";
            hintCountText.fontSize = 18;
            hintCountText.fontStyle = TMPro.FontStyles.Bold;
            hintCountText.color = Color.white;
            hintCountText.alignment = TMPro.TextAlignmentOptions.Center;
            var hTextRect = hintCountText.GetComponent<RectTransform>();
            if (hTextRect != null)
            {
                hTextRect.anchorMin = Vector2.zero;
                hTextRect.anchorMax = Vector2.one;
                hTextRect.sizeDelta = Vector2.zero;
            }

            // Redo / Rainbow Button
            var redoBtn = EnsureButton(bottomDockObj, "RedoButton");
            var rImg = redoBtn.GetComponent<UnityEngine.UI.Image>();
            if (rImg != null) rImg.color = new Color(0.98f, 0.35f, 0.45f); // Coral Red
            var rRect = redoBtn.GetComponent<RectTransform>();
            if (rRect != null)
            {
                rRect.anchorMin = new Vector2(0.68f, 0.10f);
                rRect.anchorMax = new Vector2(0.95f, 0.90f);
                rRect.sizeDelta = Vector2.zero;
            }
            var rText = EnsureTMPText(redoBtn.gameObject, "Text");
            rText.text = "GÖKKUŞAĞI";
            rText.fontSize = 18;
            rText.fontStyle = TMPro.FontStyles.Bold;
            rText.color = Color.white;
            rText.alignment = TMPro.TextAlignmentOptions.Center;
            var rTextRect = rText.GetComponent<RectTransform>();
            if (rTextRect != null)
            {
                rTextRect.anchorMin = Vector2.zero;
                rTextRect.anchorMax = Vector2.one;
                rTextRect.sizeDelta = Vector2.zero;
            }

            // ═══════════════════════════════════════════════════
            // 3. COMPLETION PANEL (Level Victory Modal)
            // ═══════════════════════════════════════════════════
            var compPanelObj = FindOrCreateChild(hudObj.transform, "CompletionPanel");
            var compImg = compPanelObj.GetComponent<UnityEngine.UI.Image>();
            if (compImg == null) compImg = compPanelObj.AddComponent<UnityEngine.UI.Image>();
            compImg.color = new Color(0.08f, 0.10f, 0.16f, 0.96f); // Sleek Dark Glass Overlay
            var compRect = compPanelObj.GetComponent<RectTransform>();
            if (compRect != null)
            {
                compRect.anchorMin = Vector2.zero;
                compRect.anchorMax = Vector2.one;
                compRect.sizeDelta = Vector2.zero;
            }

            var compText = EnsureTMPText(compPanelObj, "CompletionTitleText");
            compText.text = "SEVİYE TAMAMLANDI! 🎉";
            compText.fontSize = 40;
            compText.fontStyle = TMPro.FontStyles.Bold;
            compText.color = new Color(1f, 0.85f, 0.2f);
            compText.alignment = TMPro.TextAlignmentOptions.Center;
            var cTitleRect = compText.GetComponent<RectTransform>();
            if (cTitleRect != null)
            {
                cTitleRect.anchorMin = new Vector2(0.1f, 0.75f);
                cTitleRect.anchorMax = new Vector2(0.9f, 0.88f);
                cTitleRect.sizeDelta = Vector2.zero;
            }

            var starsContainer = FindOrCreateChild(compPanelObj.transform, "StarsContainer");
            var starsRect = starsContainer.GetComponent<RectTransform>();
            if (starsRect != null)
            {
                starsRect.anchorMin = new Vector2(0.2f, 0.60f);
                starsRect.anchorMax = new Vector2(0.8f, 0.72f);
                starsRect.sizeDelta = Vector2.zero;
            }
            var star1 = FindOrCreateChild(starsContainer.transform, "Star1");
            var star2 = FindOrCreateChild(starsContainer.transform, "Star2");
            var star3 = FindOrCreateChild(starsContainer.transform, "Star3");

            var compScoreText = EnsureTMPText(compPanelObj, "CompletionScoreText");
            compScoreText.text = "Skor: 1,500";
            compScoreText.fontSize = 28;
            compScoreText.color = Color.white;
            compScoreText.alignment = TMPro.TextAlignmentOptions.Center;
            var cScoreRect = compScoreText.GetComponent<RectTransform>();
            if (cScoreRect != null)
            {
                cScoreRect.anchorMin = new Vector2(0.1f, 0.48f);
                cScoreRect.anchorMax = new Vector2(0.9f, 0.58f);
                cScoreRect.sizeDelta = Vector2.zero;
            }

            var compStarsText = EnsureTMPText(compPanelObj, "CompletionStarsText");
            compStarsText.gameObject.SetActive(false);

            var nextBtn = EnsureButton(compPanelObj, "NextLevelButton");
            var nextImg = nextBtn.GetComponent<UnityEngine.UI.Image>();
            if (nextImg != null) nextImg.color = new Color(0.12f, 0.82f, 0.38f); // Emerald Green
            var nextRect = nextBtn.GetComponent<RectTransform>();
            if (nextRect != null)
            {
                nextRect.anchorMin = new Vector2(0.12f, 0.20f);
                nextRect.anchorMax = new Vector2(0.88f, 0.34f);
                nextRect.sizeDelta = Vector2.zero;
            }
            var nextText = EnsureTMPText(nextBtn.gameObject, "Text");
            nextText.text = "SONRAKİ SEVİYE ➔";
            nextText.fontSize = 32;
            nextText.fontStyle = TMPro.FontStyles.Bold;
            nextText.color = Color.white;
            nextText.alignment = TMPro.TextAlignmentOptions.Center;
            var nTextRect = nextText.GetComponent<RectTransform>();
            if (nTextRect != null)
            {
                nTextRect.anchorMin = Vector2.zero;
                nTextRect.anchorMax = Vector2.one;
                nTextRect.sizeDelta = Vector2.zero;
            }

            var continueBtn = EnsureButton(compPanelObj, "ContinueButton");
            continueBtn.gameObject.SetActive(false);

            // ═══════════════════════════════════════════════════
            // 4. LEVEL FAILED PANEL
            // ═══════════════════════════════════════════════════
            var failPanelObj = FindOrCreateChild(hudObj.transform, "LevelFailedPanel");
            var failImg = failPanelObj.GetComponent<UnityEngine.UI.Image>();
            if (failImg == null) failImg = failPanelObj.AddComponent<UnityEngine.UI.Image>();
            failImg.color = new Color(0.18f, 0.06f, 0.08f, 0.96f);
            var failRect = failPanelObj.GetComponent<RectTransform>();
            if (failRect != null)
            {
                failRect.anchorMin = Vector2.zero;
                failRect.anchorMax = Vector2.one;
                failRect.sizeDelta = Vector2.zero;
            }

            var failText = EnsureTMPText(failPanelObj, "LevelFailedText");
            failText.text = "SEVİYE BAŞARISIZ! 🚨";
            failText.fontSize = 38;
            failText.fontStyle = TMPro.FontStyles.Bold;
            failText.color = new Color(1f, 0.3f, 0.35f);
            failText.alignment = TMPro.TextAlignmentOptions.Center;
            var fTextRect = failText.GetComponent<RectTransform>();
            if (fTextRect != null)
            {
                fTextRect.anchorMin = new Vector2(0.1f, 0.65f);
                fTextRect.anchorMax = new Vector2(0.9f, 0.80f);
                fTextRect.sizeDelta = Vector2.zero;
            }

            var retryBtn = EnsureButton(failPanelObj, "RetryButton");
            var rtryImg = retryBtn.GetComponent<UnityEngine.UI.Image>();
            if (rtryImg != null) rtryImg.color = new Color(0.92f, 0.25f, 0.3f);
            var retryRect = retryBtn.GetComponent<RectTransform>();
            if (retryRect != null)
            {
                retryRect.anchorMin = new Vector2(0.12f, 0.25f);
                retryRect.anchorMax = new Vector2(0.88f, 0.38f);
                retryRect.sizeDelta = Vector2.zero;
            }
            var retryText = EnsureTMPText(retryBtn.gameObject, "Text");
            retryText.text = "TEKRAR DENE 🔄";
            retryText.fontSize = 30;
            retryText.fontStyle = TMPro.FontStyles.Bold;
            retryText.color = Color.white;
            retryText.alignment = TMPro.TextAlignmentOptions.Center;
            var rtryTextRect = retryText.GetComponent<RectTransform>();
            if (rtryTextRect != null)
            {
                rtryTextRect.anchorMin = Vector2.zero;
                rtryTextRect.anchorMax = Vector2.one;
                rtryTextRect.sizeDelta = Vector2.zero;
            }

            var failContBtn = EnsureButton(failPanelObj, "LevelFailedContinueButton");
            failContBtn.gameObject.SetActive(false);

            compPanelObj.SetActive(false);
            failPanelObj.SetActive(false);

            // Assign serialized properties via SerializedObject
            var so = new SerializedObject(hudView);
            SetProp(so, "_scoreText", scoreText);
            SetProp(so, "_timerText", timerText);
            SetProp(so, "_hintCountText", hintCountText);
            SetProp(so, "_levelTitleText", levelTitleText);
            SetProp(so, "_hintButton", hintBtn);
            SetProp(so, "_undoButton", undoBtn);
            SetProp(so, "_redoButton", redoBtn);
            SetProp(so, "_pauseButton", pauseBtn);

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

        private TMPro.TMP_Text EnsureTMPText(GameObject parent, string name)
        {
            var obj = FindOrCreateChild(parent.transform, name);
            var tmp = obj.GetComponent<TMPro.TMP_Text>();
            if (tmp == null) tmp = obj.AddComponent<TMPro.TextMeshProUGUI>();
            tmp.enableWordWrapping = false;
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

            // Background canvas & styling
            var bgImg = menuObj.GetComponent<UnityEngine.UI.Image>();
            if (bgImg == null) bgImg = menuObj.AddComponent<UnityEngine.UI.Image>();
            bgImg.color = new Color(0.08f, 0.09f, 0.14f, 0.98f); // Sleek Dark Glass UI Background

            var menuRect = menuObj.GetComponent<RectTransform>();
            if (menuRect != null)
            {
                menuRect.anchorMin = Vector2.zero;
                menuRect.anchorMax = Vector2.one;
                menuRect.sizeDelta = Vector2.zero;
            }

            // 1. TitleText & CoinText
            var titleTextObj = FindOrCreateChild(menuObj.transform, "TitleText");
            var titleText = titleTextObj.GetComponent<TMPro.TMP_Text>();
            if (titleText == null) titleText = titleTextObj.AddComponent<TMPro.TextMeshProUGUI>();
            titleText.text = "COLOR JAM 3D";
            titleText.fontSize = 48;
            titleText.fontStyle = TMPro.FontStyles.Bold;
            titleText.color = new Color(1f, 0.85f, 0.2f); // Gold Title
            titleText.alignment = TMPro.TextAlignmentOptions.Center;
            var titleRect = titleTextObj.GetComponent<RectTransform>();
            if (titleRect != null)
            {
                titleRect.anchorMin = new Vector2(0.1f, 0.82f);
                titleRect.anchorMax = new Vector2(0.9f, 0.94f);
                titleRect.sizeDelta = Vector2.zero;
            }

            var coinTextObj = FindOrCreateChild(menuObj.transform, "CoinText");
            var coinText = coinTextObj.GetComponent<TMPro.TMP_Text>();
            if (coinText == null) coinText = coinTextObj.AddComponent<TMPro.TextMeshProUGUI>();
            coinText.text = "💰 1,450";
            coinText.fontSize = 28;
            coinText.fontStyle = TMPro.FontStyles.Bold;
            coinText.color = Color.white;
            coinText.alignment = TMPro.TextAlignmentOptions.Right;
            var coinRect = coinTextObj.GetComponent<RectTransform>();
            if (coinRect != null)
            {
                coinRect.anchorMin = new Vector2(0.6f, 0.92f);
                coinRect.anchorMax = new Vector2(0.95f, 0.98f);
                coinRect.sizeDelta = Vector2.zero;
            }

            // 2. GarageCard + VehicleNameText + VehicleTypeText + OpenGarageButton
            var garageCardObj = FindOrCreateChild(menuObj.transform, "GarageCard");
            var cardImg = garageCardObj.GetComponent<UnityEngine.UI.Image>();
            if (cardImg == null) cardImg = garageCardObj.AddComponent<UnityEngine.UI.Image>();
            cardImg.color = new Color(0.14f, 0.17f, 0.25f, 0.92f);
            var cardRect = garageCardObj.GetComponent<RectTransform>();
            if (cardRect != null)
            {
                cardRect.anchorMin = new Vector2(0.08f, 0.30f);
                cardRect.anchorMax = new Vector2(0.92f, 0.78f);
                cardRect.sizeDelta = Vector2.zero;
            }

            var vehicleNameObj = FindOrCreateChild(garageCardObj.transform, "VehicleNameText");
            var vehicleNameText = vehicleNameObj.GetComponent<TMPro.TMP_Text>();
            if (vehicleNameText == null) vehicleNameText = vehicleNameObj.AddComponent<TMPro.TextMeshProUGUI>();
            vehicleNameText.text = "Dondurma Arabası";
            vehicleNameText.fontSize = 32;
            vehicleNameText.fontStyle = TMPro.FontStyles.Bold;
            vehicleNameText.color = Color.white;
            vehicleNameText.alignment = TMPro.TextAlignmentOptions.Center;
            var vehNameRect = vehicleNameObj.GetComponent<RectTransform>();
            if (vehNameRect != null)
            {
                vehNameRect.anchorMin = new Vector2(0.1f, 0.72f);
                vehNameRect.anchorMax = new Vector2(0.9f, 0.90f);
                vehNameRect.sizeDelta = Vector2.zero;
            }

            var vehicleTypeObj = FindOrCreateChild(garageCardObj.transform, "VehicleTypeText");
            var vehicleTypeText = vehicleTypeObj.GetComponent<TMPro.TMP_Text>();
            if (vehicleTypeText == null) vehicleTypeText = vehicleTypeObj.AddComponent<TMPro.TextMeshProUGUI>();
            vehicleTypeText.text = "Kuşanılan Sarı Araç";
            vehicleTypeText.fontSize = 22;
            vehicleTypeText.color = new Color(0.4f, 0.8f, 1f);
            vehicleTypeText.alignment = TMPro.TextAlignmentOptions.Center;
            var vehTypeRect = vehicleTypeObj.GetComponent<RectTransform>();
            if (vehTypeRect != null)
            {
                vehTypeRect.anchorMin = new Vector2(0.1f, 0.55f);
                vehTypeRect.anchorMax = new Vector2(0.9f, 0.70f);
                vehTypeRect.sizeDelta = Vector2.zero;
            }

            var garageBtnObj = FindOrCreateChild(garageCardObj.transform, "GarageButton");
            var gImg = garageBtnObj.GetComponent<UnityEngine.UI.Image>();
            if (gImg == null) gImg = garageBtnObj.AddComponent<UnityEngine.UI.Image>();
            gImg.color = new Color(0.2f, 0.45f, 0.95f);
            var garageBtn = garageBtnObj.GetComponent<UnityEngine.UI.Button>();
            if (garageBtn == null) garageBtn = garageBtnObj.AddComponent<UnityEngine.UI.Button>();
            var gBtnRect = garageBtnObj.GetComponent<RectTransform>();
            if (gBtnRect != null)
            {
                gBtnRect.anchorMin = new Vector2(0.2f, 0.10f);
                gBtnRect.anchorMax = new Vector2(0.8f, 0.35f);
                gBtnRect.sizeDelta = Vector2.zero;
            }

            var gTextObj = FindOrCreateChild(garageBtnObj.transform, "Text");
            var gText = gTextObj.GetComponent<TMPro.TMP_Text>();
            if (gText == null) gText = gTextObj.AddComponent<TMPro.TextMeshProUGUI>();
            gText.text = "GARAJI AÇ 🚗";
            gText.fontSize = 26;
            gText.fontStyle = TMPro.FontStyles.Bold;
            gText.color = Color.white;
            gText.alignment = TMPro.TextAlignmentOptions.Center;
            var gTextRect = gTextObj.GetComponent<RectTransform>();
            if (gTextRect != null)
            {
                gTextRect.anchorMin = Vector2.zero;
                gTextRect.anchorMax = Vector2.one;
                gTextRect.sizeDelta = Vector2.zero;
            }

            // 3. PlayButton + PlayButtonText
            var playBtnObj = FindOrCreateChild(menuObj.transform, "PlayButton");
            var pImg = playBtnObj.GetComponent<UnityEngine.UI.Image>();
            if (pImg == null) pImg = playBtnObj.AddComponent<UnityEngine.UI.Image>();
            pImg.color = new Color(0.12f, 0.82f, 0.38f); // Vibrant Emerald Green Play Button
            var playBtn = playBtnObj.GetComponent<UnityEngine.UI.Button>();
            if (playBtn == null) playBtn = playBtnObj.AddComponent<UnityEngine.UI.Button>();
            var playRect = playBtnObj.GetComponent<RectTransform>();
            if (playRect != null)
            {
                playRect.anchorMin = new Vector2(0.08f, 0.08f);
                playRect.anchorMax = new Vector2(0.92f, 0.22f);
                playRect.sizeDelta = Vector2.zero;
            }

            var playTextObj = FindOrCreateChild(playBtnObj.transform, "PlayButtonText");
            var playButtonText = playTextObj.GetComponent<TMPro.TMP_Text>();
            if (playButtonText == null) playButtonText = playTextObj.AddComponent<TMPro.TextMeshProUGUI>();
            playButtonText.text = "OYUNA BAŞLA (SEVİYE 1)";
            playButtonText.fontSize = 34;
            playButtonText.fontStyle = TMPro.FontStyles.Bold;
            playButtonText.color = Color.white;
            playButtonText.alignment = TMPro.TextAlignmentOptions.Center;
            var pTextRect = playTextObj.GetComponent<RectTransform>();
            if (pTextRect != null)
            {
                pTextRect.anchorMin = Vector2.zero;
                pTextRect.anchorMax = Vector2.one;
                pTextRect.sizeDelta = Vector2.zero;
            }

            // 4. SettingsButton
            var settingsBtnObj = FindOrCreateChild(menuObj.transform, "SettingsButton");
            var sImg = settingsBtnObj.GetComponent<UnityEngine.UI.Image>();
            if (sImg == null) sImg = settingsBtnObj.AddComponent<UnityEngine.UI.Image>();
            sImg.color = new Color(0.3f, 0.35f, 0.45f);
            var settingsBtn = settingsBtnObj.GetComponent<UnityEngine.UI.Button>();
            if (settingsBtn == null) settingsBtn = settingsBtnObj.AddComponent<UnityEngine.UI.Button>();
            var sRect = settingsBtnObj.GetComponent<RectTransform>();
            if (sRect != null)
            {
                sRect.anchorMin = new Vector2(0.05f, 0.92f);
                sRect.anchorMax = new Vector2(0.22f, 0.98f);
                sRect.sizeDelta = Vector2.zero;
            }

            var sTextObj = FindOrCreateChild(settingsBtnObj.transform, "Text");
            var sText = sTextObj.GetComponent<TMPro.TMP_Text>();
            if (sText == null) sText = sTextObj.AddComponent<TMPro.TextMeshProUGUI>();
            sText.text = "⚙️ AYARLAR";
            sText.fontSize = 20;
            sText.color = Color.white;
            sText.alignment = TMPro.TextAlignmentOptions.Center;
            var sTextRect = sTextObj.GetComponent<RectTransform>();
            if (sTextRect != null)
            {
                sTextRect.anchorMin = Vector2.zero;
                sTextRect.anchorMax = Vector2.one;
                sTextRect.sizeDelta = Vector2.zero;
            }

            // Assign serialized properties
            var so = new SerializedObject(menuView);
            if (so.FindProperty("_titleText") != null) so.FindProperty("_titleText").objectReferenceValue = titleText;
            if (so.FindProperty("_coinText") != null) so.FindProperty("_coinText").objectReferenceValue = coinText;
            if (so.FindProperty("_garageCard") != null) so.FindProperty("_garageCard").objectReferenceValue = garageCardObj;
            if (so.FindProperty("_equippedVehicleNameText") != null) so.FindProperty("_equippedVehicleNameText").objectReferenceValue = vehicleNameText;
            if (so.FindProperty("_equippedVehicleTypeText") != null) so.FindProperty("_equippedVehicleTypeText").objectReferenceValue = vehicleTypeText;
            if (so.FindProperty("_openGarageButton") != null) so.FindProperty("_openGarageButton").objectReferenceValue = garageBtn;
            if (so.FindProperty("_playButton") != null) so.FindProperty("_playButton").objectReferenceValue = playBtn;
            if (so.FindProperty("_playButtonText") != null) so.FindProperty("_playButtonText").objectReferenceValue = playButtonText;
            if (so.FindProperty("_settingsButton") != null) so.FindProperty("_settingsButton").objectReferenceValue = settingsBtn;

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(menuView);
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
            EnsureViewUnderCanvas<BloomFlashView>(canvas, "BloomFlashOverlay", addImage: true);
            EnsureViewUnderCanvas<ConfettiView>(canvas, "ConfettiView");
            EnsureViewUnderCanvas<SettingsView>(canvas, "SettingsView");
            var settingsView = Object.FindAnyObjectByType<SettingsView>(FindObjectsInactive.Include);
            if (settingsView != null)
            {
                EnsureSettingsBindings(settingsView);
            }
            EnsureViewUnderCanvas<DailyCrisisView>(canvas, "DailyCrisisView");
            EnsureViewUnderCanvas<TutorialView>(canvas, "TutorialView");
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
            }

            // Dim Overlay Background
            var bgImg = settingsObj.GetComponent<UnityEngine.UI.Image>();
            if (bgImg == null) bgImg = settingsObj.AddComponent<UnityEngine.UI.Image>();
            bgImg.color = new Color(0.05f, 0.07f, 0.12f, 0.90f);

            // Modal Card Container
            var cardObj = FindOrCreateChild(settingsObj.transform, "SettingsCard");
            var cardImg = cardObj.GetComponent<UnityEngine.UI.Image>();
            if (cardImg == null) cardImg = cardObj.AddComponent<UnityEngine.UI.Image>();
            cardImg.color = new Color(0.12f, 0.15f, 0.22f, 0.98f);
            var cardRect = cardObj.GetComponent<RectTransform>();
            if (cardRect != null)
            {
                cardRect.anchorMin = new Vector2(0.10f, 0.25f);
                cardRect.anchorMax = new Vector2(0.90f, 0.75f);
                cardRect.sizeDelta = Vector2.zero;
            }

            // Title Text
            var titleObj = FindOrCreateChild(cardObj.transform, "TitleText");
            var titleText = EnsureTMPText(titleObj, "Text");
            titleText.text = "AYARLAR / PAUSE";
            titleText.fontSize = 36;
            titleText.fontStyle = TMPro.FontStyles.Bold;
            titleText.color = new Color(1f, 0.85f, 0.2f);
            titleText.alignment = TMPro.TextAlignmentOptions.Center;
            var titleRect = titleObj.GetComponent<RectTransform>();
            if (titleRect != null)
            {
                titleRect.anchorMin = new Vector2(0.1f, 0.80f);
                titleRect.anchorMax = new Vector2(0.9f, 0.95f);
                titleRect.sizeDelta = Vector2.zero;
            }

            // Close / Continue Button
            var closeBtnObj = FindOrCreateChild(cardObj.transform, "CloseButton");
            var closeImg = closeBtnObj.GetComponent<UnityEngine.UI.Image>();
            if (closeImg == null) closeImg = closeBtnObj.AddComponent<UnityEngine.UI.Image>();
            closeImg.color = new Color(0.12f, 0.82f, 0.38f); // Emerald Green Continue Button
            var closeBtn = closeBtnObj.GetComponent<UnityEngine.UI.Button>();
            if (closeBtn == null) closeBtn = closeBtnObj.AddComponent<UnityEngine.UI.Button>();
            var closeRect = closeBtnObj.GetComponent<RectTransform>();
            if (closeRect != null)
            {
                closeRect.anchorMin = new Vector2(0.15f, 0.10f);
                closeRect.anchorMax = new Vector2(0.85f, 0.28f);
                closeRect.sizeDelta = Vector2.zero;
            }

            var closeText = EnsureTMPText(closeBtnObj, "Text");
            closeText.text = "DEVAM ET ➔";
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
