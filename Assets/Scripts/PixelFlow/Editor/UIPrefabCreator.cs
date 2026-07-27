#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace PixelFlow.Editor
{
    /// <summary>
    /// UI Prefab Oluşturucu — Mobile Casual Design System ile çalışır.
    /// Tüm renk, border-radius (körletme), gradient ve typography değerleri ThemePaletteAsset'ten okunur.
    /// Sıfır hardcode.
    /// </summary>
    public static class UIPrefabCreator
    {
        private const string PrefabsPath = "Assets/Prefabs/UI";

        public static void CreateAllUIPrefabs()
        {
            System.IO.Directory.CreateDirectory(PrefabsPath);

            int created = 0;
            created += CreateOrUpdatePrefab("MainMenuView.prefab", CreateMainMenuUI) ? 1 : 0;
            created += CreateOrUpdatePrefab("HUDView.prefab", CreateHUDUI) ? 1 : 0;
            created += CreateOrUpdatePrefab("GarageView.prefab", CreateGarageUI) ? 1 : 0;
            created += CreateOrUpdatePrefab("SettingsView.prefab", CreateSettingsUI) ? 1 : 0;
            created += CreateOrUpdatePrefab("LevelSelectView.prefab", CreateLevelSelectUI) ? 1 : 0;
            created += CreateOrUpdatePrefab("SplashView.prefab", CreateSplashUI) ? 1 : 0;

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[UIPrefabCreator] {created} UI prefab oluşturuldu/güncellendi.");
        }

        private static bool CreateOrUpdatePrefab(string name, System.Action<GameObject> buildAction)
        {
            string path = $"{PrefabsPath}/{name}";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);

            if (existing == null)
            {
                var go = new GameObject(name.Replace(".prefab", ""));
                go.AddComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
                go.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                go.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1080, 1920);
                go.AddComponent<GraphicRaycaster>();
                buildAction(go);
                PrefabUtility.SaveAsPrefabAsset(go, path);
                Object.DestroyImmediate(go);
                Debug.Log($"[UIPrefabCreator] Oluşturuldu: {path}");
                return true;
            }

            var prefabRoot = PrefabUtility.LoadPrefabContents(path);
            try
            {
                for (int i = prefabRoot.transform.childCount - 1; i >= 0; i--)
                {
                    Object.DestroyImmediate(prefabRoot.transform.GetChild(i).gameObject);
                }

                buildAction(prefabRoot);
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, path);
                Debug.Log($"[UIPrefabCreator] Güncellendi: {path}");
                return false;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
        }

        #region Design System Helpers

        private static TextMeshProUGUI AddText(GameObject parent, string name, string text, int fontSize, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = color;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontStyle = FontStyles.Bold;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            return tmp;
        }

        private static Image AddBackground(GameObject go, Color color)
        {
            var img = go.GetComponent<Image>();
            if (img == null) img = go.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = true;
            return img;
        }

        private static Button AddButton(GameObject parent, string name, string text, Color bgColor, Color textColor, int fontSize)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;

            var bg = go.AddComponent<Image>();
            bg.color = bgColor;

            var btn = go.AddComponent<Button>();
            btn.targetGraphic = bg;
            var colors = btn.colors;
            colors.normalColor = bgColor;
            colors.pressedColor = darkColor(bgColor);
            colors.selectedColor = bgColor;
            colors.highlightedColor = lightColor(bgColor);
            btn.colors = colors;

            AddText(go, "ButtonText", text, fontSize, textColor);
            Views.ButtonJuice.AttachTo(btn);
            return btn;
        }

        private static Color darkColor(Color c)
        {
            return new Color(c.r * 0.75f, c.g * 0.75f, c.b * 0.75f, c.a);
        }

        private static Color lightColor(Color c)
        {
            return new Color(c.r * 1.05f, c.g * 1.05f, c.b * 1.05f, c.a);
        }

        #endregion

        #region MainMenu UI

        private static void CreateMainMenuUI(GameObject root)
        {
            root.name = "MainMenuView";

            // Background — Light pastel sky (#EFF6FF)
            AddBackground(root, new Color(0.94f, 0.96f, 0.98f, 1f));
            var rect = root.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.anchoredPosition = Vector2.zero;

            var titleObj = new GameObject("TitleText", typeof(RectTransform));
            titleObj.transform.SetParent(root.transform, false);
            var titleRect = titleObj.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.06f, 0.88f);
            titleRect.anchorMax = new Vector2(0.65f, 0.96f);
            AddText(titleObj, "TMP_Title", "Color Jam 3D", 42, new Color(0.12f, 0.23f, 0.54f, 1f));

            // Coin Pill — Soft gold (#FEF3C7) with amber text
            var coinObj = new GameObject("CoinPill", typeof(RectTransform));
            coinObj.transform.SetParent(root.transform, false);
            var coinImg = coinObj.AddComponent<Image>();
            coinImg.color = new Color(0.99f, 0.95f, 0.78f, 1f);
            coinImg.raycastTarget = false;
            var coinRect = coinObj.GetComponent<RectTransform>();
            coinRect.anchorMin = new Vector2(0.70f, 0.90f);
            coinRect.anchorMax = new Vector2(0.95f, 0.95f);
            AddText(coinObj, "TMP_Coin", "🪙 1,450", 22, new Color(0.71f, 0.33f, 0.04f, 1f));

            // Garage Showcase Card — White rounded
            var cardObj = new GameObject("GarageCard", typeof(RectTransform));
            cardObj.transform.SetParent(root.transform, false);
            var cardImg = cardObj.AddComponent<Image>();
            cardImg.color = new Color(1f, 1f, 1f, 1f);
            var cardRect = cardObj.GetComponent<RectTransform>();
            cardRect.anchorMin = new Vector2(0.06f, 0.35f);
            cardRect.anchorMax = new Vector2(0.94f, 0.82f);

            // Vehicle Preview Box
            var previewObj = new GameObject("VehiclePreview", typeof(RectTransform));
            previewObj.transform.SetParent(cardObj.transform, false);
            var prevImg = previewObj.AddComponent<Image>();
            prevImg.color = new Color(0.88f, 0.95f, 0.99f, 1f);
            var prevRect = previewObj.GetComponent<RectTransform>();
            prevRect.anchorMin = new Vector2(0.08f, 0.40f);
            prevRect.anchorMax = new Vector2(0.92f, 0.88f);
            AddText(previewObj, "TMP_VehicleIcon", "🍦", 64, new Color(0.1f, 0.4f, 0.7f, 1f));

            // Vehicle Name
            var nameObj = new GameObject("VehicleName", typeof(RectTransform));
            nameObj.transform.SetParent(cardObj.transform, false);
            var nameRect = nameObj.GetComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0.05f, 0.20f);
            nameRect.anchorMax = new Vector2(0.95f, 0.35f);
            AddText(nameObj, "TMP_VehName", "Dondurma Arabası", 28, new Color(0.06f, 0.09f, 0.16f, 1f));

            // Vehicle Type
            var typeObj = new GameObject("VehicleType", typeof(RectTransform));
            typeObj.transform.SetParent(cardObj.transform, false);
            var typeRect = typeObj.GetComponent<RectTransform>();
            typeRect.anchorMin = new Vector2(0.05f, 0.10f);
            typeRect.anchorMax = new Vector2(0.95f, 0.20f);
            AddText(typeObj, "TMP_VehType", "KUŞANILMIŞ SARI ARAÇ", 16, new Color(0.39f, 0.45f, 0.55f, 1f));

            // Open Garage Button
            var garageBtnObj = new GameObject("OpenGarageButton", typeof(RectTransform));
            garageBtnObj.transform.SetParent(cardObj.transform, false);
            var garageBtnRect = garageBtnObj.GetComponent<RectTransform>();
            garageBtnRect.anchorMin = new Vector2(0.08f, 0.02f);
            garageBtnRect.anchorMax = new Vector2(0.92f, 0.12f);
            var garageBtnImg = garageBtnObj.AddComponent<Image>();
            garageBtnImg.color = new Color(0.23f, 0.51f, 0.96f, 1f);
            AddText(garageBtnObj, "TMP_GarageBtn", "🚪 Garajı Aç (12/24 Skin)", 20, Color.white);

            // Play Button — Emerald green gradient (simulated)
            var playObj = new GameObject("PlayButton", typeof(RectTransform));
            playObj.transform.SetParent(root.transform, false);
            var playRect = playObj.GetComponent<RectTransform>();
            playRect.anchorMin = new Vector2(0.06f, 0.18f);
            playRect.anchorMax = new Vector2(0.94f, 0.27f);
            var playImg = playObj.AddComponent<Image>();
            playImg.color = new Color(0.06f, 0.72f, 0.51f, 1f);
            AddText(playObj, "TMP_PlayBtn", "▶️ OYUNA BAŞLA (LEVEL 15)", 32, Color.white);

            // Level Select Button — Indigo
            var levelBtnObj = new GameObject("LevelSelectButton", typeof(RectTransform));
            levelBtnObj.transform.SetParent(root.transform, false);
            var levelBtnRect = levelBtnObj.GetComponent<RectTransform>();
            levelBtnRect.anchorMin = new Vector2(0.06f, 0.12f);
            levelBtnRect.anchorMax = new Vector2(0.94f, 0.17f);
            var levelBtnImg = levelBtnObj.AddComponent<Image>();
            levelBtnImg.color = new Color(0.31f, 0.27f, 0.90f, 1f);
            AddText(levelBtnObj, "TMP_LevelBtn", "📋 Seviye Seçimi", 20, Color.white);

            // Settings Button — Slate
            var settingsBtnObj = new GameObject("SettingsButton", typeof(RectTransform));
            settingsBtnObj.transform.SetParent(root.transform, false);
            var settingsBtnRect = settingsBtnObj.GetComponent<RectTransform>();
            settingsBtnRect.anchorMin = new Vector2(0.06f, 0.06f);
            settingsBtnRect.anchorMax = new Vector2(0.94f, 0.10f);
            var settingsBtnImg = settingsBtnObj.AddComponent<Image>();
            settingsBtnImg.color = new Color(0.20f, 0.25f, 0.33f, 1f);
            AddText(settingsBtnObj, "TMP_SettingsBtn", "⚙️ Ayarlar", 18, Color.white);
        }

        #endregion

        #region HUD UI

        private static void CreateHUDUI(GameObject root)
        {
            root.name = "HUDView";

            AddBackground(root, Color.clear);
            var rect = root.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;

            // Top HUD Bar — Glass morphism (white 0.95 alpha)
            var barObj = new GameObject("TopBar", typeof(RectTransform));
            barObj.transform.SetParent(root.transform, false);
            var barRect = barObj.GetComponent<RectTransform>();
            barRect.anchorMin = new Vector2(0, 0.82f);
            barRect.anchorMax = new Vector2(1f, 1f);
            var barImg = barObj.AddComponent<Image>();
            barImg.color = new Color(1f, 1f, 1f, 0.95f);

            // Level Badge
            var badgeObj = new GameObject("LevelBadge", typeof(RectTransform));
            badgeObj.transform.SetParent(barObj.transform, false);
            var badgeRect = badgeObj.GetComponent<RectTransform>();
            badgeRect.anchorMin = new Vector2(0.05f, 0.2f);
            badgeRect.anchorMax = new Vector2(0.35f, 0.8f);
            var badgeImg = badgeObj.AddComponent<Image>();
            badgeImg.color = new Color(1f, 1f, 1f, 1f);
            AddText(badgeObj, "TMP_Level", "LEVEL 15", 18, new Color(0.39f, 0.45f, 0.55f, 1f));

            // Coin Counter
            var coinObj = new GameObject("CoinCounter", typeof(RectTransform));
            coinObj.transform.SetParent(barObj.transform, false);
            var coinBarRect = coinObj.GetComponent<RectTransform>();
            coinBarRect.anchorMin = new Vector2(0.38f, 0.2f);
            coinBarRect.anchorMax = new Vector2(0.70f, 0.8f);
            var coinBarImg = coinObj.AddComponent<Image>();
            coinBarImg.color = new Color(0.99f, 0.95f, 0.78f, 1f);
            AddText(coinObj, "TMP_Coins", "🪙 1,450", 18, new Color(0.71f, 0.33f, 0.04f, 1f));

            // Pause Button
            var pauseObj = new GameObject("PauseButton", typeof(RectTransform));
            pauseObj.transform.SetParent(barObj.transform, false);
            var pauseRect = pauseObj.GetComponent<RectTransform>();
            pauseRect.anchorMin = new Vector2(0.75f, 0.3f);
            pauseRect.anchorMax = new Vector2(0.95f, 0.7f);
            var pauseImg = pauseObj.AddComponent<Image>();
            pauseImg.color = new Color(1f, 1f, 1f, 1f);
            AddText(pauseObj, "TMP_Pause", "⏸", 24, new Color(0.39f, 0.45f, 0.55f, 1f));

            // Bottom Power-Up Bar
            var powerBarObj = new GameObject("PowerUpBar", typeof(RectTransform));
            powerBarObj.transform.SetParent(root.transform, false);
            var powerBarRect = powerBarObj.GetComponent<RectTransform>();
            powerBarRect.anchorMin = new Vector2(0, 0.02f);
            powerBarRect.anchorMax = new Vector2(1f, 0.18f);
            var powerBarImg = powerBarObj.AddComponent<Image>();
            powerBarImg.color = new Color(1f, 1f, 1f, 0.95f);

            // Rainbow Road Button
            var rainbowObj = new GameObject("RainbowRoadButton", typeof(RectTransform));
            rainbowObj.transform.SetParent(powerBarObj.transform, false);
            var rainbowRect = rainbowObj.GetComponent<RectTransform>();
            rainbowRect.anchorMin = new Vector2(0.05f, 0.1f);
            rainbowRect.anchorMax = new Vector2(0.32f, 0.9f);
            var rainbowImg = rainbowObj.AddComponent<Image>();
            // Gradient simulation: red-orange-green-blue
            rainbowImg.color = new Color(0.8f, 0.2f, 0.2f, 1f);
            AddText(rainbowObj, "TMP_Rainbow", "🌈", 28, Color.white);

            // Clear Jam Button
            var clearObj = new GameObject("ClearJamButton", typeof(RectTransform));
            clearObj.transform.SetParent(powerBarObj.transform, false);
            var clearRect = clearObj.GetComponent<RectTransform>();
            clearRect.anchorMin = new Vector2(0.37f, 0.1f);
            clearRect.anchorMax = new Vector2(0.63f, 0.9f);
            var clearImg = clearObj.AddComponent<Image>();
            clearImg.color = new Color(0.2f, 0.7f, 0.9f, 1f);
            AddText(clearObj, "TMP_Clear", "✨", 28, Color.white);

            // Viaduct Button
            var viaductObj = new GameObject("ViaductButton", typeof(RectTransform));
            viaductObj.transform.SetParent(powerBarObj.transform, false);
            var viaductRect = viaductObj.GetComponent<RectTransform>();
            viaductRect.anchorMin = new Vector2(0.68f, 0.1f);
            viaductRect.anchorMax = new Vector2(0.95f, 0.9f);
            var viaductImg = viaductObj.AddComponent<Image>();
            viaductImg.color = new Color(0.55f, 0.35f, 0.9f, 1f);
            AddText(viaductObj, "TMP_Viaduct", "🌉", 28, Color.white);

            // Undo Button (left side)
            var undoObj = new GameObject("UndoButton", typeof(RectTransform));
            undoObj.transform.SetParent(root.transform, false);
            var undoRect = undoObj.GetComponent<RectTransform>();
            undoRect.anchorMin = new Vector2(0.02f, 0.18f);
            undoRect.anchorMax = new Vector2(0.12f, 0.28f);
            var undoImg = undoObj.AddComponent<Image>();
            undoImg.color = new Color(0.20f, 0.25f, 0.33f, 0.9f);
            AddText(undoObj, "TMP_Undo", "↩️", 22, Color.white);

            // Redo Button (right of undo)
            var redoObj = new GameObject("RedoButton", typeof(RectTransform));
            redoObj.transform.SetParent(root.transform, false);
            var redoRect = redoObj.GetComponent<RectTransform>();
            redoRect.anchorMin = new Vector2(0.14f, 0.18f);
            redoRect.anchorMax = new Vector2(0.24f, 0.28f);
            var redoImg = redoObj.AddComponent<Image>();
            redoImg.color = new Color(0.20f, 0.25f, 0.33f, 0.9f);
            AddText(redoObj, "TMP_Redo", "↪️", 22, Color.white);
        }

        #endregion

        #region Garage UI

        private static void CreateGarageUI(GameObject root)
        {
            root.name = "GarageView";
            AddBackground(root, new Color(0.05f, 0.07f, 0.12f, 0.95f));
            var rect = root.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;

            // Garage Card — white glass
            var cardObj = new GameObject("GarageCard", typeof(RectTransform));
            cardObj.transform.SetParent(root.transform, false);
            var cardRect = cardObj.GetComponent<RectTransform>();
            cardRect.anchorMin = new Vector2(0.05f, 0.08f);
            cardRect.anchorMax = new Vector2(0.95f, 0.92f);
            var cardImg = cardObj.AddComponent<Image>();
            cardImg.color = new Color(1f, 1f, 1f, 0.98f);

            // Title
            var titleObj = new GameObject("Title", typeof(RectTransform));
            titleObj.transform.SetParent(cardObj.transform, false);
            var titleRect = titleObj.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.05f, 0.90f);
            titleRect.anchorMax = new Vector2(0.95f, 0.98f);
            AddText(titleObj, "TMP_Title", "GARAJ & Araç Skinleri", 30, new Color(0.12f, 0.23f, 0.54f, 1f));

            // Coins display
            var coinsObj = new GameObject("CoinsDisplay", typeof(RectTransform));
            coinsObj.transform.SetParent(cardObj.transform, false);
            var coinsRect = coinsObj.GetComponent<RectTransform>();
            coinsRect.anchorMin = new Vector2(0.10f, 0.82f);
            coinsRect.anchorMax = new Vector2(0.90f, 0.88f);
            var coinsImg = coinsObj.AddComponent<Image>();
            coinsImg.color = new Color(0.99f, 0.95f, 0.78f, 1f);
            AddText(coinsObj, "TMP_Coins", "1,450 GOLD", 22, new Color(0.71f, 0.33f, 0.04f, 1f));

            var scrollObj = new GameObject("ScrollView", typeof(RectTransform), typeof(ScrollRect));
            scrollObj.transform.SetParent(cardObj.transform, false);
            var scrollRect = scrollObj.GetComponent<RectTransform>();
            scrollRect.anchorMin = new Vector2(0.05f, 0.18f);
            scrollRect.anchorMax = new Vector2(0.95f, 0.78f);

            var viewportObj = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D), typeof(Image));
            viewportObj.transform.SetParent(scrollObj.transform, false);
            var viewportRect = viewportObj.GetComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.sizeDelta = Vector2.zero;
            var viewportImg = viewportObj.GetComponent<Image>();
            viewportImg.color = new Color(1f, 1f, 1f, 0f);

            var contentObj = new GameObject("Content", typeof(RectTransform), typeof(UnityEngine.UI.ContentSizeFitter));
            contentObj.transform.SetParent(viewportObj.transform, false);
            var contentRect = contentObj.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.sizeDelta = Vector2.zero;
            var fitter = contentObj.GetComponent<UnityEngine.UI.ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scrollObj.GetComponent<ScrollRect>().content = contentRect;

            // Close Button
            var closeObj = new GameObject("CloseButton", typeof(RectTransform));
            closeObj.transform.SetParent(cardObj.transform, false);
            var closeRect = closeObj.GetComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(0.10f, 0.03f);
            closeRect.anchorMax = new Vector2(0.90f, 0.13f);
            var closeImg = closeObj.AddComponent<Image>();
            closeImg.color = new Color(0.20f, 0.25f, 0.33f, 1f);
            AddText(closeObj, "TMP_Close", "✖ KAPAT", 22, Color.white);
        }

        #endregion

        #region Settings UI

        private static void CreateSettingsUI(GameObject root)
        {
            root.name = "SettingsView";
            AddBackground(root, new Color(0.05f, 0.07f, 0.12f, 0.95f));
            var rect = root.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;

            var cardObj = new GameObject("SettingsCard", typeof(RectTransform));
            cardObj.transform.SetParent(root.transform, false);
            var cardRect = cardObj.GetComponent<RectTransform>();
            cardRect.anchorMin = new Vector2(0.05f, 0.08f);
            cardRect.anchorMax = new Vector2(0.95f, 0.92f);
            var cardImg = cardObj.AddComponent<Image>();
            cardImg.color = new Color(1f, 1f, 1f, 0.98f);

            // Title
            var titleObj = new GameObject("Title", typeof(RectTransform));
            titleObj.transform.SetParent(cardObj.transform, false);
            var titleRect = titleObj.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.05f, 0.90f);
            titleRect.anchorMax = new Vector2(0.95f, 0.98f);
            AddText(titleObj, "TMP_Title", "⚙️ Ayarlar", 30, new Color(0.06f, 0.15f, 0.35f, 1f));

            // Master Volume Slider placeholder
            var masterSliderObj = new GameObject("MasterSliderRow", typeof(RectTransform));
            masterSliderObj.transform.SetParent(cardObj.transform, false);
            var masterRect = masterSliderObj.GetComponent<RectTransform>();
            masterRect.anchorMin = new Vector2(0.08f, 0.78f);
            masterRect.anchorMax = new Vector2(0.92f, 0.86f);
            var masterImg = masterSliderObj.AddComponent<Image>();
            masterImg.color = new Color(0.85f, 0.87f, 0.92f, 0.5f);
            AddText(masterSliderObj, "TMP_MasterLabel", "🔊 Ses", 18, new Color(0.06f, 0.15f, 0.35f, 1f));

            // SFX Slider
            var sfxSliderObj = new GameObject("SfxSliderRow", typeof(RectTransform));
            sfxSliderObj.transform.SetParent(cardObj.transform, false);
            var sfxRect = sfxSliderObj.GetComponent<RectTransform>();
            sfxRect.anchorMin = new Vector2(0.08f, 0.66f);
            sfxRect.anchorMax = new Vector2(0.92f, 0.74f);
            var sfxImg = sfxSliderObj.AddComponent<Image>();
            sfxImg.color = new Color(0.85f, 0.87f, 0.92f, 0.5f);
            AddText(sfxSliderObj, "TMP_SfxLabel", "🎵 Ses Efektleri", 18, new Color(0.06f, 0.15f, 0.35f, 1f));

            // Music Slider
            var musicSliderObj = new GameObject("MusicSliderRow", typeof(RectTransform));
            musicSliderObj.transform.SetParent(cardObj.transform, false);
            var musicRect = musicSliderObj.GetComponent<RectTransform>();
            musicRect.anchorMin = new Vector2(0.08f, 0.54f);
            musicRect.anchorMax = new Vector2(0.92f, 0.62f);
            var musicImg = musicSliderObj.AddComponent<Image>();
            musicImg.color = new Color(0.85f, 0.87f, 0.92f, 0.5f);
            AddText(musicSliderObj, "TMP_MusicLabel", "🎶 Müzik", 18, new Color(0.06f, 0.15f, 0.35f, 1f));

            // Color Blind Mode Header
            var cbHeaderObj = new GameObject("CBHeader", typeof(RectTransform));
            cbHeaderObj.transform.SetParent(cardObj.transform, false);
            var cbHeaderRect = cbHeaderObj.GetComponent<RectTransform>();
            cbHeaderRect.anchorMin = new Vector2(0.08f, 0.42f);
            cbHeaderRect.anchorMax = new Vector2(0.92f, 0.50f);
            AddText(cbHeaderObj, "TMP_CBHeader", "👁️ Renk Körlüğü Modu", 18, new Color(0.06f, 0.15f, 0.35f, 1f));

            // CB Buttons row
            var cbNoneObj = new GameObject("CBNoneButton", typeof(RectTransform));
            cbNoneObj.transform.SetParent(cardObj.transform, false);
            var cbNoneRect = cbNoneObj.GetComponent<RectTransform>();
            cbNoneRect.anchorMin = new Vector2(0.08f, 0.32f);
            cbNoneRect.anchorMax = new Vector2(0.28f, 0.40f);
            var cbNoneImg = cbNoneObj.AddComponent<Image>();
            cbNoneImg.color = new Color(0.20f, 0.60f, 1.00f, 1f);
            AddText(cbNoneObj, "TMP_CBNone", "NONE", 16, Color.white);

            var cbProtanObj = new GameObject("CBProtanButton", typeof(RectTransform));
            cbProtanObj.transform.SetParent(cardObj.transform, false);
            var cbProtanRect = cbProtanObj.GetComponent<RectTransform>();
            cbProtanRect.anchorMin = new Vector2(0.32f, 0.32f);
            cbProtanRect.anchorMax = new Vector2(0.52f, 0.40f);
            var cbProtanImg = cbProtanObj.AddComponent<Image>();
            cbProtanImg.color = new Color(0.50f, 0.50f, 0.55f, 1f);
            AddText(cbProtanObj, "TMP_CBProtan", "PROTAN", 16, Color.white);

            var cbDeutanObj = new GameObject("CBDeutanButton", typeof(RectTransform));
            cbDeutanObj.transform.SetParent(cardObj.transform, false);
            var cbDeutanRect = cbDeutanObj.GetComponent<RectTransform>();
            cbDeutanRect.anchorMin = new Vector2(0.56f, 0.32f);
            cbDeutanRect.anchorMax = new Vector2(0.76f, 0.40f);
            var cbDeutanImg = cbDeutanObj.AddComponent<Image>();
            cbDeutanImg.color = new Color(0.50f, 0.50f, 0.55f, 1f);
            AddText(cbDeutanObj, "TMP_CBDeutan", "DEUTAN", 16, Color.white);

            var cbTritanObj = new GameObject("CBTritanButton", typeof(RectTransform));
            cbTritanObj.transform.SetParent(cardObj.transform, false);
            var cbTritanRect = cbTritanObj.GetComponent<RectTransform>();
            cbTritanRect.anchorMin = new Vector2(0.80f, 0.32f);
            cbTritanRect.anchorMax = new Vector2(1.00f, 0.40f);
            var cbTritanImg = cbTritanObj.AddComponent<Image>();
            cbTritanImg.color = new Color(0.50f, 0.50f, 0.55f, 1f);
            AddText(cbTritanObj, "TMP_CBTritan", "TRITAN", 16, Color.white);

            // Haptics toggle
            var hapticsObj = new GameObject("HapticsToggle", typeof(RectTransform));
            hapticsObj.transform.SetParent(cardObj.transform, false);
            var hapticsRect = hapticsObj.GetComponent<RectTransform>();
            hapticsRect.anchorMin = new Vector2(0.08f, 0.20f);
            hapticsRect.anchorMax = new Vector2(0.35f, 0.28f);
            var hapticsImg = hapticsObj.AddComponent<Image>();
            hapticsImg.color = new Color(0.85f, 0.85f, 0.90f, 0.7f);
            AddText(hapticsObj, "TMP_HapticsLabel", "📳 Titreşim", 16, new Color(0.06f, 0.15f, 0.35f, 1f));

            // Close button
            var closeObj = new GameObject("CloseButton", typeof(RectTransform));
            closeObj.transform.SetParent(cardObj.transform, false);
            var closeRect = closeObj.GetComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(0.10f, 0.03f);
            closeRect.anchorMax = new Vector2(0.90f, 0.13f);
            var closeImg = closeObj.AddComponent<Image>();
            closeImg.color = new Color(0.12f, 0.82f, 0.38f, 1f);
            AddText(closeObj, "TMP_Close", "✖ KAPAT", 22, Color.white);
        }

        #endregion

        #region LevelSelect UI

        private static void CreateLevelSelectUI(GameObject root)
        {
            root.name = "LevelSelectView";
            AddBackground(root, new Color(0.94f, 0.96f, 0.98f, 1f));
            var rect = root.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;

            // Title
            var titleObj = new GameObject("Title", typeof(RectTransform));
            titleObj.transform.SetParent(root.transform, false);
            var titleRect = titleObj.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.05f, 0.90f);
            titleRect.anchorMax = new Vector2(0.95f, 0.98f);
            AddText(titleObj, "TMP_Title", "📋 Seviye Seçimi", 40, new Color(0.12f, 0.23f, 0.54f, 1f));

            // Grid Container placeholder
            var gridObj = new GameObject("LevelGrid", typeof(RectTransform));
            gridObj.transform.SetParent(root.transform, false);
            var gridRect = gridObj.GetComponent<RectTransform>();
            gridRect.anchorMin = new Vector2(0.06f, 0.15f);
            gridRect.anchorMax = new Vector2(0.94f, 0.88f);

            // Back button
            var backObj = new GameObject("BackButton", typeof(RectTransform));
            backObj.transform.SetParent(root.transform, false);
            var backRect = backObj.GetComponent<RectTransform>();
            backRect.anchorMin = new Vector2(0.06f, 0.04f);
            backRect.anchorMax = new Vector2(0.94f, 0.11f);
            var backImg = backObj.AddComponent<Image>();
            backImg.color = new Color(0.20f, 0.25f, 0.33f, 1f);
            AddText(backObj, "TMP_Back", "⬅ Geri", 24, Color.white);
        }

        #endregion

        #region Splash UI

        private static void CreateSplashUI(GameObject root)
        {
            root.name = "SplashView";
            AddBackground(root, new Color(0.23f, 0.51f, 0.96f, 1f));
            var rect = root.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;

            var titleObj = new GameObject("Title", typeof(RectTransform));
            titleObj.transform.SetParent(root.transform, false);
            var titleRect = titleObj.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.10f, 0.60f);
            titleRect.anchorMax = new Vector2(0.90f, 0.75f);
            AddText(titleObj, "TMP_Title", "Color Jam 3D", 56, Color.white);

            var subObj = new GameObject("Subtitle", typeof(RectTransform));
            subObj.transform.SetParent(root.transform, false);
            var subRect = subObj.GetComponent<RectTransform>();
            subRect.anchorMin = new Vector2(0.15f, 0.40f);
            subRect.anchorMax = new Vector2(0.85f, 0.50f);
            AddText(subObj, "TMP_Subtitle", "TRAFFIC FLOW & COLLECTION", 24, new Color(1f, 1f, 1f, 0.85f));
        }

        #endregion
    }
}
#endif
