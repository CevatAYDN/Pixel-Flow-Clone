using System;
using UnityEngine;
using UnityEngine.UI;
using PixelFlow.Models;
using PixelFlow.Signals;
using Nexus.Core;

namespace PixelFlow.Views
{
    [Mediator(typeof(UpgradeTreeMediator))]
    public class UpgradeTreeView : View
    {
        [SerializeField] private GameObject _panel;
        [SerializeField] private Text _title;
        [SerializeField] private Button _closeButton;
        [SerializeField] private Text _infoText;
        [SerializeField] private Text _storageLevel;
        [SerializeField] private Text _rateLevel;
        [SerializeField] private Text _viaductLevel;
        [SerializeField] private Text _offlineLevel;
        [SerializeField] private Text _districtLevel;
        [SerializeField] private RectTransform _connectionContainer;
        [SerializeField] private Color _connectionColor = new Color(0.3f, 0.7f, 1f, 0.6f);
        [SerializeField] private Color _connectionMaxedColor = new Color(0.2f, 0.85f, 0.3f, 0.8f);

        private readonly System.Collections.Generic.List<Image> _connectionLines = new System.Collections.Generic.List<Image>();

        public event Action OnCloseClicked;

        public void SetVisible(bool visible)
        {
            if (_panel != null) _panel.SetActive(visible);
        }

        public void UpdateInfo(int storageLvl, int rateLvl, int viaductLvl, int offlineLvl, int districtLvl,
            int storageCost, int rateCost, int viaductCost, int offlineCost, int districtCost)
        {
            if (_storageLevel != null) _storageLevel.text = $"Depo Lvl {storageLvl} → {storageCost}";
            if (_rateLevel != null) _rateLevel.text = $"Vergi Hızı Lvl {rateLvl} → {rateCost}";
            if (_viaductLevel != null) _viaductLevel.text = $"Viyadük Lvl {viaductLvl} → {viaductCost}";
            if (_offlineLevel != null) _offlineLevel.text = $"Çevrimdışı Lvl {offlineLvl} → {offlineCost}";
            if (_districtLevel != null) _districtLevel.text = $"Bölge Lvl {districtLvl} → {districtCost}";
            UpdateConnectionLines(storageLvl, rateLvl, viaductLvl, offlineLvl, districtLvl);
        }

        private void UpdateConnectionLines(int storageLvl, int rateLvl, int viaductLvl, int offlineLvl, int districtLvl)
        {
            if (_connectionContainer == null) return;
            ClearConnectionLines();

            Vector2 storagePos = GetNodePosition(_storageLevel);
            Vector2 ratePos = GetNodePosition(_rateLevel);
            Vector2 viaductPos = GetNodePosition(_viaductLevel);
            Vector2 offlinePos = GetNodePosition(_offlineLevel);
            Vector2 districtPos = GetNodePosition(_districtLevel);

            DrawConnection(storagePos, ratePos, storageLvl > 0);
            DrawConnection(storagePos, viaductPos, storageLvl >= 2);
            DrawConnection(ratePos, offlinePos, rateLvl >= 2);
            DrawConnection(viaductPos, districtPos, viaductLvl >= 3);
            DrawConnection(ratePos, districtPos, rateLvl >= 4);
        }

        private Vector2 GetNodePosition(Text text)
        {
            if (text == null) return Vector2.zero;
            var rt = text.rectTransform;
            Vector2 worldPos = rt.TransformPoint(rt.rect.center);
            return _connectionContainer.InverseTransformPoint(worldPos);
        }

        private void DrawConnection(Vector2 from, Vector2 to, bool isUnlocked)
        {
            if (_connectionContainer == null) return;
            var lineObj = new GameObject("UpgradeConnection");
            lineObj.transform.SetParent(_connectionContainer, false);
            var img = lineObj.AddComponent<Image>();
            img.color = isUnlocked ? _connectionMaxedColor : _connectionColor;
            img.raycastTarget = false;

            var rt = lineObj.GetComponent<RectTransform>();
            Vector2 diff = to - from;
            float dist = diff.magnitude;
            rt.sizeDelta = new Vector2(dist, 2f);
            rt.anchoredPosition = from;
            rt.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg);
            rt.pivot = new Vector2(0f, 0.5f);
            _connectionLines.Add(img);
        }

        private void ClearConnectionLines()
        {
            for (int i = 0; i < _connectionLines.Count; i++)
            {
                if (_connectionLines[i] != null) Destroy(_connectionLines[i].gameObject);
            }
            _connectionLines.Clear();
        }

        protected override void OnBind(IContext context)
        {
            base.OnBind(context);
            if (_closeButton != null) _closeButton.onClick.AddListener(() => OnCloseClicked?.Invoke());
        }

        protected override void OnUnbind()
        {
            base.OnUnbind();
            if (_closeButton != null) _closeButton.onClick.RemoveAllListeners();
            ClearConnectionLines();
        }
    }

    public class UpgradeTreeMediator : Mediator<UpgradeTreeView>
    {
        [Inject] public ICityEconomyModel CityEconomyModel { get; set; }

        protected override void OnBind()
        {
            View.OnCloseClicked += HandleClose;
            View.SetVisible(false);
            CityEconomyModel.OnEconomyUpdated += HandleEconomy;
            Update();
        }

        protected override void OnUnbind()
        {
            View.OnCloseClicked -= HandleClose;
            CityEconomyModel.OnEconomyUpdated -= HandleEconomy;
        }

        private void HandleEconomy() => Update();

        private void Update()
        {
            View.UpdateInfo(
                CityEconomyModel.StorageUpgradeLevel,
                CityEconomyModel.RateUpgradeLevel,
                CityEconomyModel.ViaductUpgradeLevel,
                CityEconomyModel.OfflineUpgradeLevel,
                CityEconomyModel.DistrictUnlockLevel,
                CityEconomyModel.GetUpgradeCost(UpgradeType.Storage),
                CityEconomyModel.GetUpgradeCost(UpgradeType.Rate),
                CityEconomyModel.GetUpgradeCost(UpgradeType.Viaduct),
                CityEconomyModel.GetUpgradeCost(UpgradeType.Offline),
                CityEconomyModel.GetUpgradeCost(UpgradeType.District));
        }

        private void HandleClose() => View.SetVisible(false);
    }
}
