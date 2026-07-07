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
        [SerializeField] private Text _levelProgressText;
        [SerializeField] private Button _playLevelButton;

        public event Action OnPlayLevelClicked;

        private void Awake()
        {
            EnsureUIInitialized();
        }

        private void EnsureUIInitialized()
        {
            if (_hubCanvas == null)
            {
                // Create a basic Canvas structure programmatically if not present in the scene
                CreateDefaultHubUI();
            }

            if (_playLevelButton != null)
            {
                _playLevelButton.onClick.RemoveAllListeners();
                _playLevelButton.onClick.AddListener(() => OnPlayLevelClicked?.Invoke());
            }
        }

        public void SetVisible(bool visible)
        {
            EnsureUIInitialized();
            if (_hubCanvas != null)
                _hubCanvas.SetActive(visible);
        }

        public void UpdateLevelProgress(int currentLevel, int nextLevel)
        {
            if (_levelProgressText != null)
                _levelProgressText.text = $"SEVİYE: {currentLevel + 1} → {nextLevel + 1}";
        }

        private void CreateDefaultHubUI()
        {
            // Simple UI for level selection
            _hubCanvas = new GameObject("HubCanvas");
            var canvas = _hubCanvas.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _hubCanvas.AddComponent<CanvasScaler>();
            _hubCanvas.AddComponent<GraphicRaycaster>();
            _hubCanvas.transform.SetParent(transform, false);

            // Create Top Panel
            GameObject topPanel = new GameObject("TopPanel");
            topPanel.transform.SetParent(_hubCanvas.transform, false);
            var rect = topPanel.AddComponent<Image>().rectTransform;
            rect.anchorMin = new Vector2(0f, 0.85f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            topPanel.GetComponent<Image>().color = new Color(0.08f, 0.08f, 0.12f, 0.85f);

            // Level Progress Text
            GameObject levelObj = new GameObject("LevelProgressText");
            levelObj.transform.SetParent(topPanel.transform, false);
            _levelProgressText = levelObj.AddComponent<Text>();
            _levelProgressText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _levelProgressText.fontSize = 24;
            _levelProgressText.color = Color.cyan;
            _levelProgressText.alignment = TextAnchor.MiddleCenter;
            var levelRect = _levelProgressText.rectTransform;
            levelRect.anchorMin = new Vector2(0.1f, 0.2f);
            levelRect.anchorMax = new Vector2(0.9f, 0.8f);
            levelRect.offsetMin = Vector2.zero;
            levelRect.offsetMax = Vector2.zero;

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
        }
    }
}
