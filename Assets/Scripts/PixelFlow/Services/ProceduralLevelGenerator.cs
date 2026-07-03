using System.Collections.Generic;
using System.Linq;
using PixelFlow.Data;
using UnityEngine;

namespace PixelFlow.Services
{
    /// <summary>
    /// Zorluk parametrelerine göre çözülebilir level üretir.
    /// Önce rastgele renk dağılımı + bridge yerleşimi yapar,
    /// sonra runtime solver ile çözümü doğrular.
    /// </summary>
    public sealed class ProceduralLevelGenerator
    {
        private readonly IPathSolver _solver;
        private readonly System.Random _rng;

        public ProceduralLevelGenerator(IPathSolver solver) : this(solver, new System.Random()) { }

        public ProceduralLevelGenerator(IPathSolver solver, int seed)
            : this(solver, new System.Random(seed)) { }

        private ProceduralLevelGenerator(IPathSolver solver, System.Random rng)
        {
            _solver = solver ?? new RuntimePathSolver();
            _rng = rng;
        }

        /// <summary>
        /// Zorluk seviyesi için çözülebilir bir LevelData üretir.
        /// maxAttempts sonunda çözüm bulunamazsa null döner.
        /// </summary>
        public LevelData Generate(DifficultyParams param, int maxAttempts = 50)
        {
            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                var level = TryGenerate(param);
                if (level != null)
                {
                    level.name = $"Procedural_{param.gridWidth}x{param.gridHeight}_{param.colorCount}c";
                    return level;
                }
            }
            Debug.LogWarning($"[ProceduralLevelGenerator] Failed to generate solvable level after {maxAttempts} attempts.");
            return null;
        }

        private LevelData TryGenerate(DifficultyParams param)
        {
            var level = ScriptableObject.CreateInstance<LevelData>();
            level.width = param.gridWidth;
            level.height = param.gridHeight;
            level.requireFullGridCoverage = param.requireFullGridCoverage;

            // Bridge pozisyonlarını seç
            var bridges = new HashSet<Vector2Int>();
            if (param.bridgeCount > 0)
            {
                int attempts = 0;
                while (bridges.Count < param.bridgeCount && attempts < param.gridWidth * param.gridHeight * 2)
                {
                    attempts++;
                    int bx = _rng.Next(1, param.gridWidth - 1);
                    int by = _rng.Next(1, param.gridHeight - 1);
                    var bp = new Vector2Int(bx, by);
                    if (bridges.Add(bp)) { }
                }
            }
            level.bridgePositions = bridges.ToList();

            // Kullanılabilir renkleri belirle
            var availableColors = new List<ColorType>
            {
                ColorType.Red, ColorType.Green, ColorType.Blue,
                ColorType.Yellow, ColorType.Orange, ColorType.Purple,
                ColorType.Cyan, ColorType.Magenta
            };

            int colorCount = Mathf.Min(param.colorCount, availableColors.Count);

            // Renkleri seç
            var selectedColors = new List<ColorType>();
            var pool = new List<ColorType>(availableColors);
            for (int i = 0; i < colorCount; i++)
            {
                int idx = _rng.Next(pool.Count);
                selectedColors.Add(pool[idx]);
                pool.RemoveAt(idx);
            }

            // Node pozisyonları: her renk için 2 node
            var usedPositions = new HashSet<Vector2Int>(bridges);
            var nodes = new List<GridNode>();

            foreach (var color in selectedColors)
            {
                var positions = PickTwoPositions(param.gridWidth, param.gridHeight, usedPositions);
                if (positions == null) return null;

                nodes.Add(new GridNode { position = positions.Value.pos1, color = color });
                nodes.Add(new GridNode { position = positions.Value.pos2, color = color });
                usedPositions.Add(positions.Value.pos1);
                usedPositions.Add(positions.Value.pos2);
            }
            level.initialNodes = nodes;

            // Solver ile çözümü doğrula
            if (_solver.Solve(level, out var solutions))
            {
                if (!ValidateSolutions(solutions, nodes))
                {
                    Debug.LogWarning("[ProceduralLevelGenerator] Solver produced invalid solution (path through node). Retrying...");
                    return null;
                }

                level.solutions = solutions.Select(kvp => new PathSolution
                {
                    color = kvp.Key,
                    pathPositions = new List<Vector2Int>(kvp.Value)
                }).ToList();
                return level;
            }

            return null;
        }

        private static bool ValidateSolutions(
            Dictionary<ColorType, List<Vector2Int>> solutions,
            List<GridNode> nodes)
        {
            var nodePositions = new HashSet<Vector2Int>();
            var nodeColor = new Dictionary<Vector2Int, ColorType>();
            foreach (var node in nodes)
            {
                nodePositions.Add(node.position);
                nodeColor[node.position] = node.color;
            }

            foreach (var kvp in solutions)
            {
                var pathColor = kvp.Key;
                foreach (var pos in kvp.Value)
                {
                    if (nodePositions.Contains(pos) && nodeColor[pos] != pathColor)
                        return false;
                }
            }

            return true;
        }

        private (Vector2Int pos1, Vector2Int pos2)? PickTwoPositions(int w, int h, HashSet<Vector2Int> occupied)
        {
            int maxAttempts = w * h * 4;
            int attempts = 0;

            while (attempts < maxAttempts)
            {
                attempts++;
                int x1 = _rng.Next(w);
                int y1 = _rng.Next(h);
                int x2 = _rng.Next(w);
                int y2 = _rng.Next(h);

                var p1 = new Vector2Int(x1, y1);
                var p2 = new Vector2Int(x2, y2);

                if (p1 == p2) continue;
                if (occupied.Contains(p1) || occupied.Contains(p2)) continue;

                // Minimum mesafe kontrolü
                int dist = Mathf.Abs(x1 - x2) + Mathf.Abs(y1 - y2);
                if (dist < System.Math.Max(w, h) / 2) continue;

                return (p1, p2);
            }

            return null;
        }
    }

    /// <summary>
    /// Zorluk parametreleri. Her bir zorluk seviyesi için sabit değerler tanımlanabilir.
    /// </summary>
    [System.Serializable]
    public struct DifficultyParams
    {
        public int gridWidth;
        public int gridHeight;
        public int colorCount;
        public int bridgeCount;
        public bool requireFullGridCoverage;

        public DifficultyParams(int width, int height, int colors, int bridges, bool fullCoverage = false)
        {
            gridWidth = width;
            gridHeight = height;
            colorCount = colors;
            bridgeCount = bridges;
            requireFullGridCoverage = fullCoverage;
        }

        public static readonly DifficultyParams Easy = new DifficultyParams(5, 5, 3, 0, false);
        public static readonly DifficultyParams Medium = new DifficultyParams(6, 6, 4, 1, false);
        public static readonly DifficultyParams Hard = new DifficultyParams(7, 7, 5, 2, true);
        public static readonly DifficultyParams Expert = new DifficultyParams(8, 8, 6, 3, true);
        public static readonly DifficultyParams Master = new DifficultyParams(10, 10, 8, 4, true);
    }
}
