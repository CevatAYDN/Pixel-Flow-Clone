using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Nexus.Core;
using Nexus.Core.Services;

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

        // settings-levels.html renk paleti (pastel kutu + renkli metin)
        private static readonly Color CompletedBoxColor = new Color(0.925f, 0.992f, 0.957f); // #ECFDF5 açık mint
        private static readonly Color UnlockedBoxColor  = new Color(1f, 1f, 1f);             // #FFFFFF beyaz
        private static readonly Color LockedBoxColor    = new Color(0.945f, 0.961f, 0.976f); // #F1F5F9 açık gri

        private static readonly Color CompletedTextColor = new Color(0.02f, 0.59f, 0.41f);   // #059669 yeşil
        private static readonly Color UnlockedTextColor  = new Color(0.20f, 0.25f, 0.33f);   // #334155 slate
        private static readonly Color LockedTextColor    = new Color(0.58f, 0.64f, 0.72f);   // #94A3B8 muted slate
        private static readonly Color StarColor          = new Color(0.96f, 0.62f, 0.04f);   // #F59E0B amber

        protected override void OnBind(IContext context)
        {
            base.OnBind(context);
            AutoWireUIReferences();
            if (_backButton != null)
                _backButton.onClick.AddListener(() => OnBackClicked?.Invoke());

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

        /// <summary>Grid'i temizleyip verilen seviye listesine göre kutuları yeniden üretir.</summary>
        public void PopulateLevels(IReadOnlyList<LevelButtonInfo> levels)
        {
            if (_gridContainer == null)
            {
                LoggerService?.LogError("[PixelFlow.LevelSelectView] gridContainer null — seviye kutuları üretilemiyor.");
                return;
            }

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
            img.color = !info.Unlocked ? LockedBoxColor : (completed ? CompletedBoxColor : UnlockedBoxColor);

            var btn = go.GetComponent<Button>();
            btn.interactable = info.Unlocked;
            if (info.Unlocked)
            {
                int idx = info.LevelIndex; // closure için sabitle
                btn.onClick.AddListener(() => OnLevelSelected?.Invoke(idx));
            }

            // Seviye numarası / kilit ikonu
            var numText = CreateChildText(go.transform, "Number",
                info.Unlocked ? info.DisplayNumber.ToString() : "\U0001F512");
            numText.fontSize = 30;
            numText.fontStyle = FontStyles.Bold;
            numText.color = !info.Unlocked ? LockedTextColor : (completed ? CompletedTextColor : UnlockedTextColor);
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
                starsText.color = StarColor; // #F59E0B amber
                starsText.alignment = TextAlignmentOptions.Center;
                var starRect = starsText.GetComponent<RectTransform>();
                starRect.anchorMin = new Vector2(0f, 0.04f);
                starRect.anchorMax = new Vector2(1f, 0.34f);
                starRect.sizeDelta = Vector2.zero;
            }
        }

        private static string BuildStarString(int stars)
        {
            if (stars < 0) stars = 0;
            if (stars > 3) stars = 3;
            // settings-levels.html yalnızca kazanılan dolu yıldızları gösterir (⭐⭐⭐ / ⭐⭐).
            return new string('\u2605', stars);
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
