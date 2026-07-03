using Nexus.Core;
using PixelFlow.Models;
using PixelFlow.Signals;
using PixelFlow.Services;
using System.Collections.Generic;
using UnityEngine;
using PixelFlow.Data;

namespace PixelFlow.Commands
{
    public class UseHintCommand : ICommand<RequestHintSignal>, IResettable
    {
        [Inject] public IHintModel HintModel { get; set; }
        [Inject] public IGridModel GridModel { get; set; }
        [Inject] public ILevelModel LevelModel { get; set; }
        [Inject] public IGameSessionModel GameSessionModel { get; set; }
        [Inject] public ISignalBus SignalBus { get; set; }
        [Inject] public IPathService PathService { get; set; }
        [Inject] public IHintService HintService { get; set; }
        [Inject] public IGameHistoryService HistoryService { get; set; }
        [Inject] public ISaveThrottler SaveThrottler { get; set; }
        [Inject] public IHapticService HapticService { get; set; }

        public void Execute(RequestHintSignal signal)
        {
            if (HintModel.HintsRemaining <= 0)
            {
                Debug.LogWarning("[UseHintCommand] Abort: no hints remaining.");
                return;
            }

            var level = LevelModel.CurrentLevel;
            if (level == null)
            {
                Debug.LogWarning("[UseHintCommand] Abort: LevelModel.CurrentLevel is null.");
                return;
            }

            var hintPath = HintService.GetNextUnsolvedHint(level, GridModel, steps: 3);
            if (hintPath == null || hintPath.Count == 0)
            {
                Debug.LogWarning($"[UseHintCommand] Abort: GetNextUnsolvedHint returned {(hintPath == null ? "null" : "empty")}. Grid has {GridModel.Paths.Count} paths.");
                return;
            }

            Debug.Log($"[UseHintCommand] Hint path: {hintPath.Count} cells. Applying...");

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
            SaveThrottler?.TryRequestSave(GridModel, GameSessionModel, LevelModel);
            HapticService?.Vibrate(HapticType.Light);
        }

        private ColorType ResolveHintColor(LevelData level, List<Vector2Int> hintPath)
        {
            if (hintPath == null || hintPath.Count == 0) return ColorType.None;

            // Check if any hint position is adjacent to an existing path's endpoint
            foreach (var kvp in GridModel.Paths)
            {
                if (kvp.Value.Count == 0) continue;
                var lastPos = kvp.Value[kvp.Value.Count - 1];
                foreach (var pos in hintPath)
                {
                    int dist = Mathf.Abs(pos.x - lastPos.x) + Mathf.Abs(pos.y - lastPos.y);
                    if (dist == 1)
                        return kvp.Key;
                }
            }

            // Check if first hint position is adjacent to an unsolved start node
            var firstHint = hintPath[0];
            foreach (var node in level.initialNodes)
            {
                if (node.color == ColorType.None) continue;
                if (GridModel.Paths.ContainsKey(node.color)) continue;
                int dist = Mathf.Abs(firstHint.x - node.position.x) + Mathf.Abs(firstHint.y - node.position.y);
                if (dist == 1)
                    return node.color;
            }

            // Fallback: check if any hint position matches a node position
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
