using Nexus.Core;
using PixelFlow.Models;

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
                View.InitializeGrid(GridModel.Width, GridModel.Height);
                View.UpdateGridVisuals(GridModel.Grid, GridModel.Width, GridModel.Height);
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
                View.InitializeGrid(GridModel.Width, GridModel.Height);
            }
            View.UpdateGridVisuals(GridModel.Grid, GridModel.Width, GridModel.Height);
        }
    }
}
