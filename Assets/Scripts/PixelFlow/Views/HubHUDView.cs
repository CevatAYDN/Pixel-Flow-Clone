using System;
using UnityEngine;
using UnityEngine.UI;
using PixelFlow.Models;
using Nexus.Core;

namespace PixelFlow.Views
{
    [Mediator(typeof(HubHUDMediator))]
    public class HubHUDView : View
    {
        [SerializeField] private GameObject _hubCanvas;
        [SerializeField] private Text _coinText;
        [SerializeField] private Text _taxRateText;
        [SerializeField] private Text _storageBarText;
        [SerializeField] private Image _storageFillImage;
        [SerializeField] private Button _collectTaxesButton;
        [SerializeField] private Button _playLevelButton;
        [SerializeField] private Button _toggleUpgradesButton;

        // Upgrade Panel & Buttons
        [SerializeField] private GameObject _upgradesPanel;
        [SerializeField] private Button _upgradeStorageButton;
        [SerializeField] private Button _upgradeRateButton;
        [SerializeField] private Button _upgradeViaductButton;
        [SerializeField] private Button _upgradeOfflineButton;
        [SerializeField] private Button _upgradeDistrictButton;

        [SerializeField] private Text _upgradeStorageText;
        [SerializeField] private Text _upgradeRateText;
        [SerializeField] private Text _upgradeViaductText;
        [SerializeField] private Text _upgradeOfflineText;
        [SerializeField] private Text _upgradeDistrictText;

        public event Action OnCollectTaxesClicked;
        public event Action OnPlayLevelClicked;
        public event Action<UpgradeType> OnUpgradeClicked;

        private void Start()
        {
            if (_hubCanvas == null)
            {
                // Create a basic Canvas structure programmatically if not present in the scene
                CreateDefaultHubUI();
            }

            if (_collectTaxesButton != null)
                _collectTaxesButton.onClick.AddListener(() => OnCollectTaxesClicked?.Invoke());
            if (_playLevelButton != null)
                _playLevelButton.onClick.AddListener(() => OnPlayLevelClicked?.Invoke());
            
            if (_toggleUpgradesButton != null)
                _toggleUpgradesButton.onClick.AddListener(() => {
                    if (_upgradesPanel != null) _upgradesPanel.SetActive(!_upgradesPanel.activeSelf);
                });

            if (_upgradeStorageButton != null)
                _upgradeStorageButton.onClick.AddListener(() => OnUpgradeClicked?.Invoke(UpgradeType.Storage));
            if (_upgradeRateButton != null)
                _upgradeRateButton.onClick.AddListener(() => OnUpgradeClicked?.Invoke(UpgradeType.Rate));
            if (_upgradeViaductButton != null)
                _upgradeViaductButton.onClick.AddListener(() => OnUpgradeClicked?.Invoke(UpgradeType.Viaduct));
            if (_upgradeOfflineButton != null)
                _upgradeOfflineButton.onClick.AddListener(() => OnUpgradeClicked?.Invoke(UpgradeType.Offline));
            if (_upgradeDistrictButton != null)
                _upgradeDistrictButton.onClick.AddListener(() => OnUpgradeClicked?.Invoke(UpgradeType.District));
        }

        public void SetVisible(bool visible)
        {
            if (_hubCanvas != null)
                _hubCanvas.SetActive(visible);
        }

        public void UpdateUI(int coins, float taxRate, int maxStorage, int currentTaxes,
            int storageLvl, int rateLvl, int viaductLvl, int offlineLvl, int districtLvl,
            int storageCost, int rateCost, int viaductCost, int offlineCost, int districtCost)
        {
            if (_coinText != null)
                _coinText.text = $"JETON: {coins}";
            if (_taxRateText != null)
                _taxRateText.text = $"H\u0131z: {taxRate:F1} jeton/sn";
            
            float fillPct = maxStorage > 0 ? (float)currentTaxes / maxStorage : 0f;
            if (_storageFillImage != null)
                _storageFillImage.fillAmount = fillPct;
            if (_storageBarText != null)
                _storageBarText.text = $"Kasa: {currentTaxes} / {maxStorage} ({(fillPct * 100f):F0}%)";

            if (_upgradeStorageText != null)
                _upgradeStorageText.text = $"Depo Kapasitesi (Lvl {storageLvl})\nMaliyet: {storageCost} Jeton";
            if (_upgradeRateText != null)
                _upgradeRateText.text = $"Vergi H\u0131z\u0131 (Lvl {rateLvl})\nMaliyet: {rateCost} Jeton";
            if (_upgradeViaductText != null)
                _upgradeViaductText.text = $"Viyad\u00fck S\u0131n\u0131r\u0131 (Lvl {viaductLvl})\nMaliyet: {viaductCost} Jeton";
            if (_upgradeOfflineText != null)
                _upgradeOfflineText.text = $"\u00c7evrimd\u0131\u015f\u0131 S\u00fcre (Lvl {offlineLvl})\nMaliyet: {offlineCost} Jeton";
            if (_upgradeDistrictText != null)
                _upgradeDistrictText.text = $"B\u00f6lge Kilidi (Lvl {districtLvl})\nMaliyet: {districtCost} Jeton";
        }

        private void CreateDefaultHubUI()
        {
            // Programmatic Bento-Glass UI Creator
            _hubCanvas = new GameObject("HubCanvas");
            var canvas = _hubCanvas.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _hubCanvas.AddComponent<CanvasScaler>();
            _hubCanvas.AddComponent<GraphicRaycaster>();
            _hubCanvas.transform.SetParent(transform, false);

            // Create Top Panel (Bento style)
            GameObject topPanel = new GameObject("TopPanel");
            topPanel.transform.SetParent(_hubCanvas.transform, false);
            var rect = topPanel.AddComponent<Image>().rectTransform;
            rect.anchorMin = new Vector2(0f, 0.85f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            topPanel.GetComponent<Image>().color = new Color(0.08f, 0.08f, 0.12f, 0.85f);

            // Coin Text
            GameObject coinObj = new GameObject("CoinText");
            coinObj.transform.SetParent(topPanel.transform, false);
            _coinText = coinObj.AddComponent<Text>();
            _coinText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _coinText.fontSize = 24;
            _coinText.color = Color.yellow;
            _coinText.alignment = TextAnchor.MiddleLeft;
            var coinRect = _coinText.rectTransform;
            coinRect.anchorMin = new Vector2(0.05f, 0.1f);
            coinRect.anchorMax = new Vector2(0.4f, 0.9f);
            coinRect.offsetMin = Vector2.zero;
            coinRect.offsetMax = Vector2.zero;

            // Play Button
            GameObject playObj = new GameObject("PlayButton");
            playObj.transform.SetParent(_hubCanvas.transform, false);
            _playLevelButton = playObj.AddComponent<Button>();
            var playImage = playObj.AddComponent<Image>();
            playImage.color = new Color(0.12f, 0.83f, 0.46f, 1f); // Neon Green
            var playRect = playImage.rectTransform;
            playRect.anchorMin = new Vector2(0.35f, 0.05f);
            playRect.anchorMax = new Vector2(0.65f, 0.15f);
            playRect.offsetMin = Vector2.zero;
            playRect.offsetMax = Vector2.zero;

            GameObject playTextObj = new GameObject("Text");
            playTextObj.transform.SetParent(playObj.transform, false);
            var pText = playTextObj.AddComponent<Text>();
            pText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            pText.text = "BULMACA ODASI";
            pText.fontSize = 20;
            pText.color = Color.white;
            pText.alignment = TextAnchor.MiddleCenter;
            pText.rectTransform.anchorMin = Vector2.zero;
            pText.rectTransform.anchorMax = Vector2.one;
            pText.rectTransform.offsetMin = Vector2.zero;
            pText.rectTransform.offsetMax = Vector2.zero;

            // Collect Taxes Button
            GameObject collectObj = new GameObject("CollectTaxesButton");
            collectObj.transform.SetParent(_hubCanvas.transform, false);
            _collectTaxesButton = collectObj.AddComponent<Button>();
            var collectImg = collectObj.AddComponent<Image>();
            collectImg.color = new Color(1f, 0.85f, 0.24f, 1f); // Gold Yellow
            var collectRect = collectImg.rectTransform;
            collectRect.anchorMin = new Vector2(0.35f, 0.2f);
            collectRect.anchorMax = new Vector2(0.65f, 0.3f);
            collectRect.offsetMin = Vector2.zero;
            collectRect.offsetMax = Vector2.zero;

            GameObject collectTextObj = new GameObject("Text");
            collectTextObj.transform.SetParent(collectObj.transform, false);
            _storageBarText = collectTextObj.AddComponent<Text>();
            _storageBarText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _storageBarText.text = "VERG\u0130 TOPLA";
            _storageBarText.fontSize = 18;
            _storageBarText.color = Color.black;
            _storageBarText.alignment = TextAnchor.MiddleCenter;
            _storageBarText.rectTransform.anchorMin = Vector2.zero;
            _storageBarText.rectTransform.anchorMax = Vector2.one;
            _storageBarText.rectTransform.offsetMin = Vector2.zero;
            _storageBarText.rectTransform.offsetMax = Vector2.zero;

            // Toggle Upgrades Button
            GameObject toggleObj = new GameObject("ToggleUpgradesButton");
            toggleObj.transform.SetParent(_hubCanvas.transform, false);
            _toggleUpgradesButton = toggleObj.AddComponent<Button>();
            var toggleImg = toggleObj.AddComponent<Image>();
            toggleImg.color = new Color(0.2f, 0.2f, 0.25f, 0.9f);
            var toggleRect = toggleImg.rectTransform;
            toggleRect.anchorMin = new Vector2(0.7f, 0.85f);
            toggleRect.anchorMax = new Vector2(0.95f, 0.95f);
            toggleRect.offsetMin = Vector2.zero;
            toggleRect.offsetMax = Vector2.zero;

            GameObject toggleTextObj = new GameObject("Text");
            toggleTextObj.transform.SetParent(toggleObj.transform, false);
            var tText = toggleTextObj.AddComponent<Text>();
            tText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            tText.text = "GEL\u0130\u015eT\u0130RMELER";
            tText.fontSize = 16;
            tText.color = Color.white;
            tText.alignment = TextAnchor.MiddleCenter;
            tText.rectTransform.anchorMin = Vector2.zero;
            tText.rectTransform.anchorMax = Vector2.one;
            tText.rectTransform.offsetMin = Vector2.zero;
            tText.rectTransform.offsetMax = Vector2.zero;

            // Upgrades Panel
            _upgradesPanel = new GameObject("UpgradesPanel");
            _upgradesPanel.transform.SetParent(_hubCanvas.transform, false);
            var upgImg = _upgradesPanel.AddComponent<Image>();
            upgImg.color = new Color(0.05f, 0.05f, 0.08f, 0.95f);
            var upgRect = upgImg.rectTransform;
            upgRect.anchorMin = new Vector2(0.7f, 0.15f);
            upgRect.anchorMax = new Vector2(0.98f, 0.8f);
            upgRect.offsetMin = Vector2.zero;
            upgRect.offsetMax = Vector2.zero;
            _upgradesPanel.SetActive(false);

            // Add simple vertical layout upgrades buttons inside UpgradesPanel
            CreateUpgradeItem(_upgradesPanel.transform, UpgradeType.Storage, out _upgradeStorageButton, out _upgradeStorageText, 4);
            CreateUpgradeItem(_upgradesPanel.transform, UpgradeType.Rate, out _upgradeRateButton, out _upgradeRateText, 3);
            CreateUpgradeItem(_upgradesPanel.transform, UpgradeType.Viaduct, out _upgradeViaductButton, out _upgradeViaductText, 2);
            CreateUpgradeItem(_upgradesPanel.transform, UpgradeType.Offline, out _upgradeOfflineButton, out _upgradeOfflineText, 1);
            CreateUpgradeItem(_upgradesPanel.transform, UpgradeType.District, out _upgradeDistrictButton, out _upgradeDistrictText, 0);
        }

        private void CreateUpgradeItem(Transform parent, UpgradeType type, out Button button, out Text text, int index)
        {
            GameObject container = new GameObject($"UpgradeItem_{type}");
            container.transform.SetParent(parent, false);
            var containerRect = container.AddComponent<Image>().rectTransform;
            containerRect.anchorMin = new Vector2(0.05f, 0.05f + (index * 0.18f));
            containerRect.anchorMax = new Vector2(0.95f, 0.2f + (index * 0.18f));
            containerRect.offsetMin = Vector2.zero;
            containerRect.offsetMax = Vector2.zero;
            container.GetComponent<Image>().color = new Color(0.12f, 0.12f, 0.18f, 0.9f);

            GameObject btnObj = new GameObject("BuyButton");
            btnObj.transform.SetParent(container.transform, false);
            button = btnObj.AddComponent<Button>();
            var btnImg = btnObj.AddComponent<Image>();
            btnImg.color = new Color(0.35f, 0.7f, 0.45f, 1f); // Green buy button
            var btnRect = btnImg.rectTransform;
            btnRect.anchorMin = new Vector2(0.75f, 0.1f);
            btnRect.anchorMax = new Vector2(0.95f, 0.9f);
            btnRect.offsetMin = Vector2.zero;
            btnRect.offsetMax = Vector2.zero;

            GameObject buyTextObj = new GameObject("Text");
            buyTextObj.transform.SetParent(btnObj.transform, false);
            var buyText = buyTextObj.AddComponent<Text>();
            buyText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            buyText.text = "AL";
            buyText.fontSize = 14;
            buyText.color = Color.white;
            buyText.alignment = TextAnchor.MiddleCenter;
            buyText.rectTransform.anchorMin = Vector2.zero;
            buyText.rectTransform.anchorMax = Vector2.one;
            buyText.rectTransform.offsetMin = Vector2.zero;
            buyText.rectTransform.offsetMax = Vector2.zero;

            GameObject textObj = new GameObject("InfoText");
            textObj.transform.SetParent(container.transform, false);
            text = textObj.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 11;
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleLeft;
            var textRect = text.rectTransform;
            textRect.anchorMin = new Vector2(0.05f, 0.05f);
            textRect.anchorMax = new Vector2(0.7f, 0.95f);
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
        }
    }
}
