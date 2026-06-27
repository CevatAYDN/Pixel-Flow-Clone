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
            
            View.OnGlobalPointerDown += HandleGlobalPointerDown;
            View.OnGlobalPointerDrag += HandleGlobalPointerDrag;
            View.OnGlobalPointerUp += HandleGlobalPointerUp;

            if (GridModel.Width > 0 && GridModel.Height > 0)
            {
                InitializeAndCenter();
            }
        }

        protected override void OnUnbind()
        {
            GridModel.OnGridUpdated -= HandleGridUpdated;
            
            View.OnGlobalPointerDown -= HandleGlobalPointerDown;
            View.OnGlobalPointerDrag -= HandleGlobalPointerDrag;
            View.OnGlobalPointerUp -= HandleGlobalPointerUp;
        }

        private void HandleGlobalPointerDown(Vector2Int pos)
        {
            SignalBus.Fire(new InputInteractionSignal { Type = InputType.PointerDown, GridPosition = pos });
        }

        private void HandleGlobalPointerDrag(Vector2Int pos)
        {
            SignalBus.Fire(new InputInteractionSignal { Type = InputType.Drag, GridPosition = pos });
        }

        private void HandleGlobalPointerUp(Vector2Int pos)
        {
            SignalBus.Fire(new InputInteractionSignal { Type = InputType.PointerUp, GridPosition = pos });
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

                float aspect = cam.aspect;
                float padding = 1f; // Minimum padding around grid in units
                float hSize = (GridModel.Height + padding) * 0.5f;
                float wSize = (GridModel.Width + padding) * 0.5f / aspect;

                // In portrait mode, reserve extra vertical space to avoid HUD overlapping
                if (aspect < 1f)
                {
                    hSize += 1.5f;
                }

                cam.orthographicSize = Mathf.Max(hSize, wSize);
            }
        }
    }
}
