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
            level.viaductLimit = param.bridgeCount;

            var availableColors = new List<ColorType>(GddColorPalette.Standard);

            int colorCount = Mathf.Min(param.colorCount, availableColors.Count);

            var selectedColors = new List<ColorType>();
            var pool = new List<ColorType>(availableColors);
            for (int i = 0; i < colorCount; i++)
            {
                int idx = _rng.Next(pool.Count);
                selectedColors.Add(pool[idx]);
                pool.RemoveAt(idx);
            }

            var usedPositions = new HashSet<Vector2Int>();
            var nodes = new List<GridNode>();
            bool nodesOk = true;

            foreach (var color in selectedColors)
            {
                var positions = PickTwoPositions(param.gridWidth, param.gridHeight, usedPositions);
                if (positions == null) { nodesOk = false; break; }

                nodes.Add(new GridNode { position = positions.Value.pos1, color = color });
                nodes.Add(new GridNode { position = positions.Value.pos2, color = color });
                usedPositions.Add(positions.Value.pos1);
                usedPositions.Add(positions.Value.pos2);
            }
            if (!nodesOk) return null;
            level.initialNodes = nodes;
            _lastLevelNodes = nodes;

            if (!_solver.Solve(level, out var solutions))
            {
                return null;
            }

            if (!ValidateSolutions(solutions, nodes))
            {
                return null;
            }

            var bridges = new HashSet<Vector2Int>();
            if (param.bridgeCount > 0)
            {
                var crossings = FindPathCrossings(solutions);
                foreach (var cross in crossings)
                {
                    if (bridges.Count >= param.bridgeCount) break;
                    bridges.Add(cross);
                }
            }
            level.bridgePositions = bridges.ToList();

            if (param.obstaclesEnabled || param.ferryEnabled || param.narrowPassEnabled)
            {
                level.obstacles = GenerateObstacles(param, bridges);
            }
            else
            {
                level.obstacles = new List<ObstacleData>();
            }

            level.solutions = solutions.Select(kvp => new PathSolution
            {
                color = kvp.Key,
                pathPositions = new List<Vector2Int>(kvp.Value)
            }).ToList();

            return level;
        }

        private List<Vector2Int> FindPathCrossings(Dictionary<ColorType, List<Vector2Int>> solutions)
        {
            var crossings = new List<Vector2Int>();
            var seen = new HashSet<Vector2Int>();
            var pathLookup = new Dictionary<Vector2Int, List<ColorType>>();

            foreach (var kvp in solutions)
            {
                foreach (var pos in kvp.Value)
                {
                    if (!pathLookup.TryGetValue(pos, out var list))
                    {
                        list = new List<ColorType>();
                        pathLookup[pos] = list;
                    }
                    list.Add(kvp.Key);
                }
            }

            foreach (var kvp in pathLookup)
            {
                if (kvp.Value.Count >= 2 && !seen.Contains(kvp.Key))
                {
                    crossings.Add(kvp.Key);
                    seen.Add(kvp.Key);
                }
            }
            return crossings;
        }

        private List<ObstacleData> GenerateObstacles(DifficultyParams param, HashSet<Vector2Int> bridges)
        {
            var obstacles = new List<ObstacleData>();
            int target = Mathf.Min(2 + (param.gridWidth * param.gridHeight) / 25, 6);

            int safety = 200;
            while (obstacles.Count < target && safety-- > 0)
            {
                int ox = _rng.Next(1, param.gridWidth - 1);
                int oy = _rng.Next(1, param.gridHeight - 1);
                var pos = new Vector2Int(ox, oy);
                if (bridges.Contains(pos)) continue;
                bool already = obstacles.Exists(o => o.position == pos);
                if (already) continue;
                bool nearNode = false;
                foreach (var node in _lastLevelNodes)
                {
                    if (Vector2Int.Distance(node.position, pos) < 1.5f) { nearNode = true; break; }
                }
                if (nearNode) continue;

                ObstacleType type = ObstacleType.Construction;
                if (param.ferryEnabled && _rng.Next(100) < 30) type = ObstacleType.Ferry;
                else if (param.narrowPassEnabled && _rng.Next(100) < 20) type = ObstacleType.NarrowPass;
                else if (param.obstaclesEnabled)
                {
                    int r = _rng.Next(100);
                    if (r < 30) type = ObstacleType.Lake;
                    else if (r < 55) type = ObstacleType.Park;
                    else if (r < 80) type = ObstacleType.Construction;
                    else type = ObstacleType.OneWay;
                }

                obstacles.Add(new ObstacleData { position = pos, type = type });
            }
            return obstacles;
        }

        private List<GridNode> _lastLevelNodes = new List<GridNode>();

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
        public bool obstaclesEnabled;
        public bool ferryEnabled;
        public bool narrowPassEnabled;

        public DifficultyParams(int width, int height, int colors, int bridges, bool fullCoverage = false,
            bool obstacles = false, bool ferry = false, bool narrow = false)
        {
            gridWidth = width;
            gridHeight = height;
            colorCount = colors;
            bridgeCount = bridges;
            requireFullGridCoverage = fullCoverage;
            obstaclesEnabled = obstacles;
            ferryEnabled = ferry;
            narrowPassEnabled = narrow;
        }

        public static readonly DifficultyParams Easy = new DifficultyParams(5, 5, 1, 0, false);
        public static readonly DifficultyParams Medium = new DifficultyParams(6, 6, 2, 0, false);
        public static readonly DifficultyParams Hard = new DifficultyParams(7, 7, 3, 2, false, true);
        public static readonly DifficultyParams Expert = new DifficultyParams(8, 8, 4, 3, true, true);
        public static readonly DifficultyParams Master = new DifficultyParams(10, 10, 5, 4, true, true, true, true);
    }
}
