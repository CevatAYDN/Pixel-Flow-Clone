using Nexus.Core;
using PixelFlow.Data;
using PixelFlow.Models;
using PixelFlow.Signals;
using System.Collections.Generic;
using UnityEngine;

namespace PixelFlow.Views
{
    public class GridMediator : Mediator<GridView>
    {
        [Inject] public IGridModel GridModel { get; set; }
        [Inject] public ISettingsModel SettingsModel { get; set; }

        private CellState[,] _previousCellStates;
        private ColorType[,] _previousCellColors;
        private byte[,] _previousPathColorMasks;
        private readonly HashSet<Vector2Int> _changedCells = new HashSet<Vector2Int>();

        protected override void OnBind()
        {
            Subscribe<GridUpdatedSignal>(HandleGridUpdated);
            Subscribe<ThemeChangedSignal>(HandleThemeChanged);

            View.OnGlobalPointerDown += HandleGlobalPointerDown;
            View.OnGlobalPointerDrag += HandleGlobalPointerDrag;
            View.OnGlobalPointerUp += HandleGlobalPointerUp;

            CacheCellState();

            if (GridModel.Width > 0 && GridModel.Height > 0)
            {
                InitializeAndCenter();
            }
        }

        protected override void OnUnbind()
        {
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

        private void HandleGridUpdated(GridUpdatedSignal signal)
        {
            if (!View.IsInitialized && GridModel.Width > 0 && GridModel.Height > 0)
            {
                InitializeAndCenter();
                return;
            }

            if (GridModel.Width <= 0 || GridModel.Height <= 0) return;

            Vector2Int crashPos = GridModel.LastCrashPosition.Value;
            ComputeChangedCells();
            if (_changedCells.Count > 0)
            {
                View.UpdateDifferential(GridModel.Grid, SettingsModel.CurrentTheme, _changedCells, crashPos);
            }

            View.UpdatePathVisuals(GridModel.Paths, GridModel.Grid, crashPos, GridModel.CrashColorA.Value, GridModel.CrashColorB.Value);
            CacheCellState();

            GridModel.LastCrashPosition.Value = new Vector2Int(-1, -1);
        }

        private void ComputeChangedCells()
        {
            _changedCells.Clear();
            int w = GridModel.Width;
            int h = GridModel.Height;

            if (_previousCellStates == null || _previousCellStates.GetLength(0) != w || _previousCellStates.GetLength(1) != h)
            {
                for (int x = 0; x < w; x++)
                    for (int y = 0; y < h; y++)
                        _changedCells.Add(new Vector2Int(x, y));
                return;
            }

            for (int x = 0; x < w; x++)
            {
                for (int y = 0; y < h; y++)
                {
                    var current = GridModel.Grid[x, y];
                    bool stateChanged = _previousCellStates[x, y] != current.State;
                    bool colorChanged = _previousCellColors[x, y] != current.Color;
                    bool pathColorsChanged = _previousPathColorMasks != null
                        && _previousPathColorMasks[x, y] != current.PathColorsMask;
                    if (stateChanged || colorChanged || pathColorsChanged)
                    {
                        _changedCells.Add(new Vector2Int(x, y));
                    }
                }
            }
        }

        private void CacheCellState()
        {
            int w = GridModel.Width;
            int h = GridModel.Height;
            if (w <= 0 || h <= 0) return;

            if (_previousCellStates == null || _previousCellStates.GetLength(0) != w || _previousCellStates.GetLength(1) != h)
            {
                _previousCellStates = new CellState[w, h];
                _previousCellColors = new ColorType[w, h];
                _previousPathColorMasks = new byte[w, h];
            }

            for (int x = 0; x < w; x++)
            {
                for (int y = 0; y < h; y++)
                {
                    _previousCellStates[x, y] = GridModel.Grid[x, y].State;
                    _previousCellColors[x, y] = GridModel.Grid[x, y].Color;
                    _previousPathColorMasks[x, y] = GridModel.Grid[x, y].PathColorsMask;
                }
            }
        }

        private void InitializeAndCenter()
        {
            CacheCellState();
            View.InitializeGrid(GridModel.Width, GridModel.Height);
            View.UpdateGridVisuals(GridModel.Grid, GridModel.Width, GridModel.Height, SettingsModel.CurrentTheme, GridModel.Paths, GridModel.LastCrashPosition.Value);
            View.CenterCamera(GridModel.Width, GridModel.Height);

            var cam = View.GetCachedCamera();
            if (cam != null)
            {
                var camCtrl = cam.GetComponent<PixelFlow.Services.CameraController>();
                if (camCtrl != null)
                {
                    float cx = (GridModel.Width - 1) * 0.5f;
                    float cy = (GridModel.Height - 1) * 0.5f;
                    float size = cam.orthographicSize;
                    camCtrl.SetPuzzleView(cx, cy, size);
                    camCtrl.TransitionToPuzzle();
                }
            }
        }
    }
}