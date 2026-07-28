using System.Collections.Generic;
using PixelFlow.Data;
using PixelFlow.Models;
using UnityEngine;
using Nexus.Core;

namespace PixelFlow.Services
{
    /// <summary>
    /// Single Responsibility Level & Grid State Validation Service.
    /// Evaluates LevelData assets and runtime GridModel states according to PixelFlow rules.
    /// </summary>
    public class LevelValidator : ILevelValidator
    {
        private readonly IPathSolver _pathSolver;

        [Inject] public DifficultyFormulaConfigAsset DifficultyFormulaConfig { get; set; }

        public LevelValidator(IPathSolver pathSolver = null)
        {
            _pathSolver = pathSolver ?? new RuntimePathSolver();
        }

        public ValidationResult Validate(LevelData level)
        {
            var result = new ValidationResult();

            if (level == null)
            {
                result.AddError("LevelData is null!");
                return result;
            }

            // 1. Dimension Checks
            if (level.width < 3 || level.height < 3)
            {
                result.AddError($"Grid dimensions must be at least 3x3. Current: {level.width}x{level.height}");
            }
            if (level.width > 12 || level.height > 12)
            {
                result.AddWarning($"Grid dimensions ({level.width}x{level.height}) are larger than typical mobile screen limits.");
            }

            // 2. Node Pair Validation
            if (level.initialNodes == null || level.initialNodes.Count == 0)
            {
                result.AddError("Level has no initial nodes!");
            }
            else
            {
                var colorCounts = new Dictionary<ColorType, (int sources, int targets)>();
                var occupiedPositions = new HashSet<Vector2Int>();

                foreach (var node in level.initialNodes)
                {
                    if (node.position.x < 0 || node.position.x >= level.width ||
                        node.position.y < 0 || node.position.y >= level.height)
                    {
                        result.AddError($"Node at ({node.position.x}, {node.position.y}) is outside grid bounds!", node.position);
                        continue;
                    }

                    if (node.color == ColorType.None)
                    {
                        result.AddError($"Node at ({node.position.x}, {node.position.y}) has no color assigned!", node.position);
                    }

                    if (occupiedPositions.Contains(node.position))
                    {
                        result.AddError($"Multiple nodes overlap at position ({node.position.x}, {node.position.y})!", node.position);
                    }
                    else
                    {
                        occupiedPositions.Add(node.position);
                    }

                    if (!colorCounts.ContainsKey(node.color))
                    {
                        colorCounts[node.color] = (0, 0);
                    }

                    var current = colorCounts[node.color];
                    if (node.isSource) current.sources++;
                    else current.targets++;
                    colorCounts[node.color] = current;
                }

                foreach (var kvp in colorCounts)
                {
                    if (kvp.Key == ColorType.None) continue;
                    var (sources, targets) = kvp.Value;
                    if (sources != 1 || targets != 1)
                    {
                        result.AddError($"Color '{kvp.Key}' must have exactly 1 Source and 1 Target! (Current: {sources} Source, {targets} Target)");
                    }
                }
            }

            // 3. Obstacles Validation
            if (level.obstacles != null && level.initialNodes != null)
            {
                var nodePosSet = new HashSet<Vector2Int>();
                foreach (var node in level.initialNodes) nodePosSet.Add(node.position);

                foreach (var obs in level.obstacles)
                {
                    if (obs.position.x < 0 || obs.position.x >= level.width ||
                        obs.position.y < 0 || obs.position.y >= level.height)
                    {
                        result.AddError($"Obstacle at ({obs.position.x}, {obs.position.y}) is outside grid bounds!", obs.position);
                    }
                    if (nodePosSet.Contains(obs.position))
                    {
                        result.AddError($"Obstacle overlaps with node at ({obs.position.x}, {obs.position.y})!", obs.position);
                    }
                }
            }

            // 4. OneWay Cell Validation
            if (level.oneWayCells != null)
            {
                foreach (var ow in level.oneWayCells)
                {
                    if (ow.position.x < 0 || ow.position.x >= level.width ||
                        ow.position.y < 0 || ow.position.y >= level.height)
                    {
                        result.AddError($"OneWay cell at ({ow.position.x}, {ow.position.y}) is outside grid bounds!", ow.position);
                    }
                    if (ow.allowedDirection == Vector2Int.zero)
                    {
                        result.AddError($"OneWay cell at ({ow.position.x}, {ow.position.y}) has zero direction vector!", ow.position);
                    }
                }
            }

            // 5. Complexity Score
            int colorCount = level.initialNodes != null ? level.initialNodes.Count / 2 : 0;
            int obstacleCount = level.obstacles != null ? level.obstacles.Count : 0;
            int bridgeCount = level.bridgePositions != null ? level.bridgePositions.Count : 0;
            
            if (DifficultyFormulaConfig == null)
            {
                throw new DataValidationException("DifficultyFormulaConfigAsset missing in LevelValidator! Ensure it's bound in GameContextLifecycle.");
            }
            int complexity = DifficultyFormulaConfig.CalculateDifficulty(colorCount, bridgeCount, obstacleCount, level.viaductLimit);
            result.ComplexityScore = Mathf.Max(0, complexity);

            // 6. Solvability Validation
            if (result.IsValid && _pathSolver != null)
            {
                bool isSolvable = _pathSolver.Solve(level, out var solutions);
                result.IsSolvable = isSolvable;
                if (!isSolvable)
                {
                    result.AddWarning("Auto-Solver could not find a valid solution path for this level layout!");
                }
                else
                {
                    result.AddInfo($"Solvable layout verified with {solutions.Count} color paths.");
                }
            }

            return result;
        }

        public ValidationResult ValidateRuntimeState(GridModel gridModel)
        {
            var result = new ValidationResult();
            if (gridModel == null)
            {
                result.AddError("GridModel is null!");
                return result;
            }

            if (gridModel.Width < 3 || gridModel.Height < 3)
            {
                result.AddError($"Runtime grid width/height invalid ({gridModel.Width}x{gridModel.Height})");
            }

            return result;
        }
    }
}
