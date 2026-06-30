using Nexus.Core;
using PixelFlow.Models;
using PixelFlow.Signals;
using PixelFlow.Services;
using System.Collections.Generic;
using UnityEngine;
using PixelFlow.Data;
using static PixelFlow.Services.HintService;

namespace PixelFlow.Commands
{
    public class UseHintCommand : ICommand<RequestHintSignal>, IResettable
    {
        [Inject] public IHintModel HintModel { get; set; }
        [Inject] public IGridModel GridModel { get; set; }
        [Inject] public ILevelModel LevelModel { get; set; }
        [Inject] public ISignalBus SignalBus { get; set; }
        [Inject] public IPathService PathService { get; set; }
        [Inject] public IHintService HintService { get; set; }
        [Inject] public IGameHistoryService HistoryService { get; set; }

        public void Execute(RequestHintSignal signal)
        {
            if (HintModel.HintsRemaining <= 0) return;

            var level = LevelModel.CurrentLevel;
            if (level == null) return;

            var hintPath = HintService.GetNextUnsolvedHint(level, GridModel, steps: 3);
            if (hintPath == null || hintPath.Count == 0) return;

            HistoryService.Record(GridModel);

            var colorToHint = ResolveHintColor(level, hintPath);
            if (colorToHint == ColorType.None) return;

            if (!GridModel.Paths.ContainsKey(colorToHint))
                GridModel.Paths[colorToHint] = new List<Vector2Int>();

            foreach (var pos in hintPath)
            {
                if (pos.x < 0 || pos.x >= GridModel.Width || pos.y < 0 || pos.y >= GridModel.Height)
                    continue;

                if (!GridModel.Paths[colorToHint].Contains(pos))
                {
                    GridModel.Paths[colorToHint].Add(pos);
                }

                var cell = GridModel.Grid[pos.x, pos.y];
                if (cell.State == CellState.Empty)
                {
                    cell.State = CellState.Path;
                    cell.Color = colorToHint;
                }
                else if (cell.Color != colorToHint && cell.State == CellState.Path)
                {
                    PathService.BreakPath(cell.Color, pos);
                    cell.State = CellState.Path;
                    cell.Color = colorToHint;
                }
            }

            HintModel.UseHint();
            SignalBus.Fire(new GridUpdatedSignal());
            SignalBus.Fire(new CheckWinConditionSignal());
        }

        private ColorType ResolveHintColor(LevelData level, List<Vector2Int> hintPath)
        {
            if (hintPath == null || hintPath.Count == 0) return ColorType.None;
            var firstPos = hintPath[0];

            foreach (var node in level.initialNodes)
            {
                if (node.position == firstPos)
                    return node.color;
            }

            foreach (var node in level.initialNodes)
            {
                foreach (var pos in hintPath)
                {
                    if (node.position == pos)
                        return node.color;
                }
            }

            return ColorType.None;
        }

        public void Reset() { }
    }
}
