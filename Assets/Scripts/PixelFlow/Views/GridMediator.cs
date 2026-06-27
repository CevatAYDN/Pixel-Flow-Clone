using Nexus.Core;
using PixelFlow.Models;
using PixelFlow.Signals;
using UnityEngine;

namespace PixelFlow.Views
{
    public class GridMediator : Mediator<GridView>
    {
        [Inject] public IGridModel GridModel { get; set; }
        [Inject] public ISettingsModel SettingsModel { get; set; }

        protected override void OnBind()
        {
            UnityEngine.Debug.Log($"[GridMediator] OnBind called.");
            GridModel.OnGridUpdated += HandleGridUpdated;
            Subscribe<ThemeChangedSignal>(HandleThemeChanged);
            if (GridModel.Width > 0 && GridModel.Height > 0)
            {
                InitializeAndCenter();
            }
        }

        protected override void OnUnbind()
        {
            GridModel.OnGridUpdated -= HandleGridUpdated;
        }

        private void HandleThemeChanged(ThemeChangedSignal signal)
        {
            if (GridModel.Width > 0 && GridModel.Height > 0)
            {
                View.UpdateGridVisuals(GridModel.Grid, GridModel.Width, GridModel.Height, SettingsModel.CurrentTheme, GridModel.Paths);
            }
        }

        private void HandleGridUpdated()
        {
            UnityEngine.Debug.Log($"[GridMediator] HandleGridUpdated called. View initialized: {View.IsInitialized}");
            if (!View.IsInitialized && GridModel.Width > 0 && GridModel.Height > 0)
            {
                InitializeAndCenter();
            }
            View.UpdateGridVisuals(GridModel.Grid, GridModel.Width, GridModel.Height, SettingsModel.CurrentTheme, GridModel.Paths);
        }

        private void InitializeAndCenter()
        {
            UnityEngine.Debug.Log($"[GridMediator] InitializeAndCenter called with Width:{GridModel.Width}, Height:{GridModel.Height}");
            View.InitializeGrid(GridModel.Width, GridModel.Height);
            View.UpdateGridVisuals(GridModel.Grid, GridModel.Width, GridModel.Height, SettingsModel.CurrentTheme, GridModel.Paths);

            Camera cam = Camera.main;
            if (cam != null)
            {
                float cx = (GridModel.Width - 1) * 0.5f;
                float cy = (GridModel.Height - 1) * 0.5f;
                cam.transform.position = new Vector3(cx, cy, -10f);
                cam.orthographic = true;
                cam.orthographicSize = Mathf.Max(GridModel.Width, GridModel.Height) * 0.6f;
            }
        }
    }
}
