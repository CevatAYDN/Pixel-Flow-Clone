using Nexus.Core;
using Nexus.Core.Services;
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
        [Inject] public ILoggerService LoggerService { get; set; }
        [Inject] public IPlayerPrefsService PlayerPrefsService { get; set; }

        public void Execute(RequestHintSignal signal)
        {
            LoggerService?.Log($"[PixelFlow.UseHintCommand] RequestHintSignal received. Hints remaining: {HintModel.HintsRemaining}");
            if (HintModel.HintsRemaining <= 0)
            {
                LoggerService?.LogWarning("[PixelFlow.UseHintCommand] Abort: no hints remaining.");
                return;
            }

            var level = LevelModel.CurrentLevel;
            if (level == null)
            {
                LoggerService?.LogWarning("[PixelFlow.UseHintCommand] Abort: LevelModel.CurrentLevel is null.");
                return;
            }

            LoggerService?.Log("[PixelFlow.UseHintCommand] Generating hint path via HintService...");
            var hintPath = HintService.GetNextUnsolvedHint(level, GridModel, steps: 3);
            if (hintPath == null || hintPath.Count == 0)
            {
                LoggerService?.LogWarning($"[PixelFlow.UseHintCommand] Abort: GetNextUnsolvedHint returned {(hintPath == null ? "null" : "empty")}. Grid has {GridModel.Paths.Count} paths.");
                return;
            }

            LoggerService?.Log($"[PixelFlow.UseHintCommand] Hint path generated: {hintPath.Count} cells. Analyzing connection color...");

            HistoryService.Record(GridModel, GameSessionModel);

            var colorToHint = ResolveHintColor(level, hintPath);
            LoggerService?.Log($"[PixelFlow.UseHintCommand] Resolved color for hint: {colorToHint}");
            if (colorToHint == ColorType.None)
            {
                LoggerService?.LogWarning("[PixelFlow.UseHintCommand] Abort: Could not resolve target color for hint path.");
                return;
            }

            if (!GridModel.Paths.ContainsKey(colorToHint))
                GridModel.Paths[colorToHint] = new List<Vector2Int>();

            LoggerService?.Log($"[PixelFlow.UseHintCommand] Applying hint cells for color {colorToHint} onto grid...");
            foreach (var pos in hintPath)
            {
                if (pos.x < 0 || pos.x >= GridModel.Width || pos.y < 0 || pos.y >= GridModel.Height)
                {
                    LoggerService?.LogWarning($"[PixelFlow.UseHintCommand] Skipping out-of-bounds hint position: {pos}");
                    continue;
                }

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
                    LoggerService?.Log($"[PixelFlow.UseHintCommand] Breaking conflicting path for color {cell.Color} at {pos}.");
                    PathService.BreakPath(cell.Color, pos);
                    cell.State = CellState.Path;
                    cell.Color = colorToHint;
                }
            }

            HintModel.UseHint();
            LoggerService?.Log($"[PixelFlow.UseHintCommand] Hint applied successfully. Remaining: {HintModel.HintsRemaining}");
            SignalBus.Fire(new GridUpdatedSignal());
            SignalBus.Fire(new CheckWinConditionSignal());
            SaveHelper.TrySave(SaveThrottler, GridModel, GameSessionModel, LevelModel, PlayerPrefsService);
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
