using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Nexus.Core;
using Nexus.Core.Services;
using PixelFlow.Data;

namespace PixelFlow.Views
{
    /// <summary>
    /// Tek bir seviye kutusunun görüntü verisi (LevelSelectView tarafından runtime'da üretilir).
    /// </summary>
    public struct LevelButtonInfo
    {
        public int LevelIndex;     // 0-tabanlı iç index
        public int DisplayNumber;  // Kullanıcıya gösterilen 1-tabanlı numara
        public bool Unlocked;      // Kilit durumu
        public int Stars;          // 0-3 kazanılan yıldız
    }

    /// <summary>
    /// DesignSystem/Mockups/settings-levels.html "SEVİYE SEÇİMİ" ekranına sadık görünüm.
    /// Seviye kutuları GridLayoutGroup altında runtime'da üretilir; tamamlanan seviyeler
    /// yeşil + yıldız, oynanabilir seviyeler mavi, kilitli seviyeler gri 🔒 gösterilir.
    /// </summary>
    [Mediator(typeof(LevelSelectMediator))]
    public class LevelSelectView : View
    {
        [SerializeField] private TMP_Text _titleText;
        [SerializeField] private Button _backButton;
        [SerializeField] private Transform _gridContainer;

        public event Action OnBackClicked;
        public event Action<int> OnLevelSelected;

        [Inject] public ILoggerService LoggerService { get; set; }
        [Inject] public Data.ThemePaletteAsset ThemePalette { get; set; }

        protected override void OnBind(IContext context)
        {
            base.OnBind(context);
            AutoWireUIReferences();
            if (_backButton != null)
            {
                ButtonJuice.AttachTo(_backButton);
                _backButton.onClick.AddListener(() => OnBackClicked?.Invoke());
            }

            LoggerService?.Log($"[PixelFlow.LevelSelectView] AutoWire: title={(bool)_titleText}, " +
                $"backButton={(bool)_backButton}, gridContainer={(bool)_gridContainer}");
        }

        protected override void OnUnbind()
        {
            base.OnUnbind();
            if (_backButton != null) _backButton.onClick.RemoveAllListeners();
            ClearGrid();
        }

        public void AutoWireUIReferences()
        {
            if (_gridContainer == null)
            {
                var gc = transform.Find("LevelGrid");
                _gridContainer = gc != null ? gc : transform;
            }

            if (_titleText == null)
            {
                var texts = GetComponentsInChildren<TMP_Text>(true);
                foreach (var t in texts)
                {
                    if (t.gameObject.name.ToLower().Contains("title")) { _titleText = t; break; }
                }
            }

            if (_backButton == null)
            {
                var buttons = GetComponentsInChildren<Button>(true);
                foreach (var b in buttons)
                {
                    if (b.gameObject.name.ToLower().Contains("back")) { _backButton = b; break; }
                }
            }
        }

        public void SetVisible(bool visible)
        {
            var cg = GetComponent<CanvasGroup>();
            if (cg == null) cg = gameObject.AddComponent<CanvasGroup>();
            cg.alpha = visible ? 1f : 0f;
            cg.blocksRaycasts = visible;
            cg.interactable = visible;

            var canvas = GetComponent<Canvas>();
            if (canvas != null) canvas.enabled = visible;
        }

        private void EnsureGridContainerLayout()
        {
            if (_gridContainer == null) return;
            var grid = _gridContainer.GetComponent<GridLayoutGroup>();
            if (grid == null)
            {
                grid = _gridContainer.gameObject.AddComponent<GridLayoutGroup>();
                grid.cellSize = new Vector2(76, 76);
                grid.spacing = new Vector2(10, 10);
                grid.padding = new RectOffset(10, 10, 10, 10);
                grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                grid.constraintCount = 4;
            }
        }

        /// <summary>Grid'i temizleyip verilen seviye listesine göre kutuları yeniden üretir.</summary>
        public void PopulateLevels(IReadOnlyList<LevelButtonInfo> levels)
        {
            if (_gridContainer == null)
            {
                LoggerService?.LogError("[PixelFlow.LevelSelectView] gridContainer null — seviye kutuları üretilemiyor.");
                return;
            }

            EnsureGridContainerLayout();
            ClearGrid();
            if (levels == null) return;

            for (int i = 0; i < levels.Count; i++)
            {
                CreateLevelButton(levels[i]);
            }
        }

        private void ClearGrid()
        {
            if (_gridContainer == null) return;
            for (int i = _gridContainer.childCount - 1; i >= 0; i--)
            {
                var child = _gridContainer.GetChild(i).gameObject;
                if (Application.isPlaying) Destroy(child);
                else DestroyImmediate(child);
            }
        }

        private void CreateLevelButton(LevelButtonInfo info)
        {
            var go = new GameObject($"LevelBox_{info.DisplayNumber}",
                typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(_gridContainer, false);

            var img = go.GetComponent<Image>();
            bool completed = info.Unlocked && info.Stars > 0;

            if (ThemePalette == null)
                throw new DataValidationException("[LevelSelectView] ThemePaletteAsset is not injected. Bind ThemePaletteAsset in GameContextLifecycle.");

            img.color = !info.Unlocked ? ThemePalette.LevelSelectLockedBox
                : (completed ? ThemePalette.LevelSelectCompletedBox : ThemePalette.LevelSelectUnlockedBox);

            var btn = go.GetComponent<Button>();
            btn.interactable = info.Unlocked;
            if (info.Unlocked)
            {
                ButtonJuice.AttachTo(btn);
                int idx = info.LevelIndex; // closure için sabitle
                btn.onClick.AddListener(() => OnLevelSelected?.Invoke(idx));
            }

            // Seviye numarası / kilit ikonu
            var numText = CreateChildText(go.transform, "Number",
                info.DisplayNumber.ToString());
            numText.fontSize = 30;
            numText.fontStyle = FontStyles.Bold;
            numText.color = !info.Unlocked ? ThemePalette.LevelSelectLockedText
                : (completed ? ThemePalette.LevelSelectCompletedText : ThemePalette.LevelSelectUnlockedText);
            numText.alignment = TextAlignmentOptions.Center;
            var numRect = numText.GetComponent<RectTransform>();
            numRect.anchorMin = new Vector2(0f, 0.32f);
            numRect.anchorMax = new Vector2(1f, 1f);
            numRect.sizeDelta = Vector2.zero;

            // Yıldız satırı (yalnızca tamamlanmış seviyelerde)
            if (completed)
            {
                var starsText = CreateChildText(go.transform, "Stars", BuildStarString(info.Stars));
                starsText.fontSize = 18;
                starsText.color = ThemePalette.LevelSelectStarColor;
                starsText.alignment = TextAlignmentOptions.Center;
                var starRect = starsText.GetComponent<RectTransform>();
                starRect.anchorMin = new Vector2(0f, 0.04f);
                starRect.anchorMax = new Vector2(1f, 0.34f);
                starRect.sizeDelta = Vector2.zero;
            }
        }

        private static string BuildStarString(int stars)
        {
            if (stars <= 0) return "";
            if (stars > 3) stars = 3;
            return new string('*', stars);
        }

        private static TMP_Text CreateChildText(Transform parent, string name, string text)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.raycastTarget = false;
            return tmp;
        }
    }
}
