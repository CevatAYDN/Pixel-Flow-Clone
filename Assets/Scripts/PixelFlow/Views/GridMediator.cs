using Nexus.Core;
using Nexus.Core.Services;
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
        [Inject] public ILoggerService Logger { get; set; }
        [Inject] public PixelFlow.Services.IAudioService AudioService { get; set; }

        private CellState[,] _previousCellStates;
        private ColorType[,] _previousCellColors;
        private byte[,] _previousPathColorMasks;
        private readonly HashSet<Vector2Int> _changedCells = new HashSet<Vector2Int>();
        private readonly HashSet<Vector2Int> _stateChangedCells = new HashSet<Vector2Int>();
        private readonly HashSet<ColorType> _completedColors = new HashSet<ColorType>();

        protected override void OnBind()
        {
            Subscribe<GridUpdatedSignal>(HandleGridUpdated);
            Subscribe<ThemeChangedSignal>(HandleThemeChanged);
            Subscribe<ThirdColorRejectionSignal>(HandleThirdColorRejection);

            View.OnGlobalPointerDown += HandleGlobalPointerDown;
            View.OnGlobalPointerDrag += HandleGlobalPointerDrag;
            View.OnGlobalPointerUp += HandleGlobalPointerUp;

            CacheCellState();

            if (GridModel.Width > 0 && GridModel.Height > 0)
            {
                Logger?.Log($"[PixelFlow.GridMediator] 🎯 OnBind: Grid model active ({GridModel.Width}x{GridModel.Height}). Initializing & centering camera.");
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
                View.UpdateDifferential(GridModel.Grid, SettingsModel.CurrentTheme, _changedCells, crashPos, _stateChangedCells);
            }

            View.UpdatePathVisuals(GridModel.Paths, GridModel.Grid, crashPos, GridModel.CrashColorA.Value, GridModel.CrashColorB.Value);

            // Path connection feedback (juice and chimes)
            var currentCompletedColors = new HashSet<ColorType>();
            foreach (var kvp in GridModel.Paths)
            {
                var color = kvp.Key;
                var path = kvp.Value;
                if (path == null || path.Count < 2) continue;

                // Validate if both start and end cells are nodes of the correct color
                var startCell = GridModel.Grid[path[0].x, path[0].y];
                var endCell = GridModel.Grid[path[path.Count - 1].x, path[path.Count - 1].y];

                if (startCell.State == CellState.Node && startCell.Color == color &&
                    endCell.State == CellState.Node && endCell.Color == color)
                {
                    currentCompletedColors.Add(color);
                }
            }

            foreach (var color in currentCompletedColors)
            {
                if (!_completedColors.Contains(color))
                {
                    // Newly completed path connection: play ascending chime and springy bounce both nodes
                    AudioService?.PlaySfx(PixelFlow.Services.SfxType.CoinCollect);
                    var path = GridModel.Paths[color];
                    View.TriggerJuicyBounce(path[0], 1.25f, 0.4f);
                    View.TriggerJuicyBounce(path[path.Count - 1], 1.25f, 0.4f);
                }
            }

            _completedColors.Clear();
            foreach (var color in currentCompletedColors)
            {
                _completedColors.Add(color);
            }

            CacheCellState();

            GridModel.LastCrashPosition.Value = new Vector2Int(-1, -1);
        }

        private void ComputeChangedCells()
        {
            _changedCells.Clear();
            _stateChangedCells.Clear();
            int w = GridModel.Width;
            int h = GridModel.Height;

            if (_previousCellStates == null || _previousCellStates.GetLength(0) != w || _previousCellStates.GetLength(1) != h)
            {
                for (int x = 0; x < w; x++)
                {
                    for (int y = 0; y < h; y++)
                    {
                        _changedCells.Add(new Vector2Int(x, y));
                        _stateChangedCells.Add(new Vector2Int(x, y));
                    }
                }
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
                        if (stateChanged)
                        {
                            _stateChangedCells.Add(new Vector2Int(x, y));
                        }
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
            float cx = (GridModel.Width - 1) * 0.5f;
            float cy = (GridModel.Height - 1) * 0.5f;
            if (cam != null)
            {
                var camCtrl = cam.GetComponent<PixelFlow.Services.CameraController>();
                if (camCtrl != null)
                {
                    float size = cam.orthographicSize;
                    camCtrl.SetPuzzleView(cx, cy, size);
                    camCtrl.TransitionToPuzzle();
                }
            }
            Logger?.Log($"[PixelFlow.GridMediator] 📷 Grid initialized ({GridModel.Width}x{GridModel.Height}) and camera centered at ({cx}, {cy}).");
        }

        private void HandleThirdColorRejection(ThirdColorRejectionSignal signal)
        {
            if (!View.IsInitialized) return;
            View.TriggerThirdColorRejectionPulse(signal.Position);
        }
    }
}