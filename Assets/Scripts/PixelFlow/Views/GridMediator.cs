using Nexus.Core;
using PixelFlow.Models;
using UnityEngine;

namespace PixelFlow.Views
{
    public class GridMediator : Mediator<GridView>
    {
        [Inject] public IGridModel GridModel { get; set; }

        protected override void OnBind()
        {
            GridModel.OnGridUpdated += HandleGridUpdated;
            if (GridModel.Width > 0 && GridModel.Height > 0)
            {
                InitializeAndCenter();
            }
        }

        protected override void OnUnbind()
        {
            GridModel.OnGridUpdated -= HandleGridUpdated;
        }

        private void HandleGridUpdated()
        {
            if (!View.IsInitialized && GridModel.Width > 0 && GridModel.Height > 0)
            {
                InitializeAndCenter();
            }
            View.UpdateGridVisuals(GridModel.Grid, GridModel.Width, GridModel.Height);
        }

        private void InitializeAndCenter()
        {
            View.InitializeGrid(GridModel.Width, GridModel.Height);
            View.UpdateGridVisuals(GridModel.Grid, GridModel.Width, GridModel.Height);

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
