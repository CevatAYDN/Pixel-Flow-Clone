using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PixelFlow.Data;
using UnityEngine;
using Nexus.Core;

namespace PixelFlow.Services
{
    /// <summary>
    /// Editor auto-solver algoritmasının runtime versiyonu.
    /// Iterative backtracking (DFS) ile tüm olası path'leri dener.
    /// Explicit Stack kullanır — recursive versiyonun StackOverflow riskini ortadan kaldırır.
    /// Bridge crossing kurallarını destekler.
    ///
    /// Mimari: 
    /// - Solve(): giriş noktası, grid/renk/engel verilerini hazırlar
    /// - SolveRecursive(): renk-seviyesi recursive outer loop (max 5 renk → güvenli derinlik)
    /// - FindPathIterative(): hücre-seviyesi iterative inner loop (Stack ile, 400+ hücre güvenli)
    /// - SolvePartial(): ipucu sistemi için — recursive kalır çünkü maxSteps ile sınırlı
    /// </summary>
    public sealed class RuntimePathSolver : IPathSolver
    {
        [Inject, OptionalInject] public GameConfig Config { get; set; }

        // Editor/test context'inde DI olmadığında Config'i elle set etmek için.
        // game_plan.md §15.9 KURAL 6: new ile oluşturma → AMA editor aracı olduğu için exception fırlatmak yerine
        // Resources.Load ile fallback yap (DataValidationException sadece runtime build'de).
        internal void SetEditorConfig(GameConfig config) => Config = config;

        private GameConfig ResolvedConfig
        {
            get
            {
                if (Config != null) return Config;
                var loaded = Resources.Load<GameConfig>("Configs/GameConfig");
                if (loaded != null) { Config = loaded; return Config; }
                Config = ScriptableObject.CreateInstance<GameConfig>();
                return Config;
            }
        }

        private int MinIterations => ResolvedConfig.PathSolverMaxIterations;
        private int MaxIterationsCap => ResolvedConfig.PathSolverMaxIterationsCap;

        private int _perSolveMaxIterations;
        private CancellationToken _cancellationToken;

        // ─── Iterative Path Search Data Structures ────────────────────────
        // FindPathIterative için explicit stack frame.
        // Her frame bir path adımını temsil eder: pozisyon + sıradaki yön.
        private struct PathFrame
        {
            public Vector2Int Position;
            public int DirectionIndex;  // 0-3: GetSortedDirections'da hangi yöne bakıyor
            public int GridChangesStart;  // Bu frame başlamadan önce kaç grid change vardı
            public int PathLengthBefore;  // Bu frame başlamadan önce path.Count
        }

        // Grid üzerinde yapılan bir değişikliği kaydeder (backtracking için).
        private struct GridChange
        {
            public int X;
            public int Y;
            public ColorType OldColor;
        }

        private bool _requireFullCoverage;

        // Iterative path search için reusable değişkenler (GC alloc azaltma)
        private readonly Stack<PathFrame> _pathStack = new Stack<PathFrame>();
        private readonly Stack<GridChange> _gridChanges = new Stack<GridChange>();
        private readonly List<Vector2Int> _currentPath = new List<Vector2Int>();
        private readonly HashSet<Vector2Int> _pathSet = new HashSet<Vector2Int>();  // O(1) Contains için

        public bool Solve(LevelData level, out Dictionary<ColorType, List<Vector2Int>> solutions)
        {
            solutions = null;
            _requireFullCoverage = level.requireFullGridCoverage;

            var colorNodes = CollectColorNodes(level);
            if (colorNodes == null || colorNodes.Count == 0) return false;
            _perSolveMaxIterations = CalculateMaxIterations(level.width, level.height, colorNodes.Count);

            var bridges = new HashSet<Vector2Int>(level.bridgePositions ?? new List<Vector2Int>());
            var blockedCells = new HashSet<Vector2Int>();
            var oneWayDict = new Dictionary<Vector2Int, Vector2Int>();

            if (level.obstacles != null)
            {
                foreach (var obs in level.obstacles)
                {
                    if (obs.type == ObstacleType.Construction || obs.type == ObstacleType.Lake ||
                        obs.type == ObstacleType.Park || obs.type == ObstacleType.Ferry ||
                        obs.type == ObstacleType.NarrowPass)
                    {
                        if (obs.position.x >= 0 && obs.position.x < level.width &&
                            obs.position.y >= 0 && obs.position.y < level.height)
                        {
                            blockedCells.Add(obs.position);
                        }
                    }
                }
            }

            if (level.oneWayCells != null)
            {
                foreach (var ow in level.oneWayCells)
                {
                    oneWayDict[ow.position] = ow.allowedDirection;
                }
            }

            var grid = new ColorType[level.width, level.height];

            foreach (var node in level.initialNodes)
            {
                if (node.position.x >= 0 && node.position.x < level.width &&
                    node.position.y >= 0 && node.position.y < level.height)
                {
                    grid[node.position.x, node.position.y] = node.color;
                }
            }

            foreach (var blocked in blockedCells)
            {
                grid[blocked.x, blocked.y] = ColorType.Red; // Occupy grid so path cannot enter
            }

            var colors = new List<ColorType>(colorNodes.Keys);
            var result = new Dictionary<ColorType, List<Vector2Int>>();
            foreach (var c in colors) result[c] = new List<Vector2Int>();
            int iterationCount = 0;

            if (SolveRecursive(0, colors, colorNodes, result, grid, bridges, blockedCells, oneWayDict, level.width, level.height, ref iterationCount))
            {
                solutions = result;
                return true;
            }

            return false;
        }

        public async Task<bool> SolveAsync(LevelData level, Dictionary<ColorType, List<Vector2Int>> solutions, CancellationToken cancellationToken = default)
        {
            _cancellationToken = cancellationToken;
            Dictionary<ColorType, List<Vector2Int>> result = null;
            bool success = await Task.Run(() => Solve(level, out result), cancellationToken);
            if (success && result != null)
            {
                foreach (var kvp in result)
                    solutions[kvp.Key] = kvp.Value;
                return true;
            }
            return false;
        }

        public bool SolvePartial(LevelData level, ColorType color, int steps, out List<Vector2Int> partialPath)
        {
            partialPath = null;

            var colorNodes = CollectColorNodes(level);
            if (colorNodes == null || !colorNodes.ContainsKey(color) || colorNodes[color].Count != 2)
                return false;

            if (steps <= 0) return false;

            _perSolveMaxIterations = MinIterations;

            var bridges = level.bridgePositions != null ? new HashSet<Vector2Int>(level.bridgePositions) : new HashSet<Vector2Int>();
            var grid = new ColorType[level.width, level.height];

            foreach (var node in level.initialNodes)
            {
                if (node.position.x >= 0 && node.position.x < level.width &&
                    node.position.y >= 0 && node.position.y < level.height)
                {
                    grid[node.position.x, node.position.y] = node.color;
                }
            }

            var start = colorNodes[color][0];
            var end = colorNodes[color][1];
            var currentPath = new List<Vector2Int> { start };

            int iterationCount = 0;
            var resultPath = FindPartialPath(start, end, color, currentPath, grid, bridges, level.width, level.height, steps, ref iterationCount);
            if (resultPath != null)
            {
                partialPath = resultPath;
                return true;
            }

            return false;
        }

        private List<Vector2Int> FindPartialPath(
            Vector2Int current, Vector2Int end, ColorType color,
            List<Vector2Int> path, ColorType[,] grid, HashSet<Vector2Int> bridges,
            int w, int h, int maxSteps, ref int iterationCount)
        {
            if (iterationCount > _perSolveMaxIterations) return null;
            if (_cancellationToken.IsCancellationRequested) return null;
            iterationCount++;

            if (path.Count - 1 >= maxSteps || current == end)
                return new List<Vector2Int>(path);

            var dirs = GetSortedDirections(current, end);

            foreach (var dir in dirs)
            {
                var next = current + dir;

                if (next.x < 0 || next.x >= w || next.y < 0 || next.y >= h) continue;
                if (path.Contains(next)) continue;

                bool isBridge = bridges.Contains(next);
                bool canMove = false;

                if (next == end)
                {
                    canMove = true;
                }
                else if (isBridge)
                {
                    var exit = next + dir;
                    if (exit.x >= 0 && exit.x < w && exit.y >= 0 && exit.y < h && !path.Contains(exit) &&
                        (grid[exit.x, exit.y] == ColorType.None || exit == end))
                    {
                        canMove = true;
                    }
                }
                else if (grid[next.x, next.y] == ColorType.None)
                {
                    canMove = true;
                }

                if (canMove)
                {
                    if (isBridge)
                    {
                        var exit = next + dir;
                        var oldNextColor = grid[next.x, next.y];
                        var oldExitColor = grid[exit.x, exit.y];
                        grid[next.x, next.y] = color;
                        grid[exit.x, exit.y] = color;
                        path.Add(next);
                        path.Add(exit);

                        var sub = FindPartialPath(exit, end, color, path, grid, bridges, w, h, maxSteps, ref iterationCount);
                        if (sub != null) return sub;

                        path.RemoveAt(path.Count - 1);
                        path.RemoveAt(path.Count - 1);
                        grid[next.x, next.y] = oldNextColor;
                        grid[exit.x, exit.y] = oldExitColor;
                    }
                    else
                    {
                        var oldColor = grid[next.x, next.y];
                        grid[next.x, next.y] = color;
                        path.Add(next);

                        var sub = FindPartialPath(next, end, color, path, grid, bridges, w, h, maxSteps, ref iterationCount);
                        if (sub != null) return sub;

                        path.RemoveAt(path.Count - 1);
                        grid[next.x, next.y] = oldColor;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Renk-seviyesi recursive outer loop. Max 5 renk → güvenli derinlik.
        /// Her renk için FindPathIterative() çağırır (hücre-seviyesi iterative).
        ///
        /// KRİTİK: FindPathIterative grid'i path ile işaretler. Sonraki rengin
        /// arama yapabilmesi için bu işaretler gerekir. Ancak sonraki renk
        /// başarısız olursa, alternatif bir path bulmak için grid'in ESKİ
        /// haline dönmesi gerekir. Bunun için grid snapshot'ı alınır.
        /// </summary>
        private bool SolveRecursive(
            int colorIndex, List<ColorType> colors,
            Dictionary<ColorType, List<Vector2Int>> colorNodes,
            Dictionary<ColorType, List<Vector2Int>> solutions,
            ColorType[,] grid, HashSet<Vector2Int> bridges,
            HashSet<Vector2Int> blockedCells,
            Dictionary<Vector2Int, Vector2Int> oneWayDict,
            int w, int h, ref int iterationCount)
        {
            if (colorIndex >= colors.Count)
            {
                if (!_requireFullCoverage)
                    return true;
                for (int x = 0; x < w; x++)
                    for (int y = 0; y < h; y++)
                        if (grid[x, y] == ColorType.None && !bridges.Contains(new Vector2Int(x, y)) && !blockedCells.Contains(new Vector2Int(x, y)))
                            return false;
                return true;
            }

            if (iterationCount > _perSolveMaxIterations) return false;
            if (_cancellationToken.IsCancellationRequested) return false;
            iterationCount++;

            var color = colors[colorIndex];
            var start = colorNodes[color][0];
            var end = colorNodes[color][1];

            // Grid snapshot: FindPathIterative grid'i path ile işaretler.
            var gridSnapshot = (ColorType[,])grid.Clone();

            var path = FindPathIterative(start, end, color, colorIndex, colors, colorNodes, solutions, grid, bridges, blockedCells, oneWayDict, w, h, ref iterationCount);
            if (path != null)
            {
                solutions[color] = path;
                if (SolveRecursive(colorIndex + 1, colors, colorNodes, solutions, grid, bridges, blockedCells, oneWayDict, w, h, ref iterationCount))
                    return true;
                solutions[color].Clear();

                // Backtrack: grid'i eski haline döndür
                for (int x = 0; x < w; x++)
                    for (int y = 0; y < h; y++)
                        grid[x, y] = gridSnapshot[x, y];
            }

            return false;
        }

        private List<Vector2Int> FindPathIterative(
            Vector2Int start, Vector2Int end, ColorType color,
            int colorIndex,
            List<ColorType> colors,
            Dictionary<ColorType, List<Vector2Int>> colorNodes,
            Dictionary<ColorType, List<Vector2Int>> solutions,
            ColorType[,] grid, HashSet<Vector2Int> bridges,
            HashSet<Vector2Int> blockedCells,
            Dictionary<Vector2Int, Vector2Int> oneWayDict,
            int w, int h, ref int iterationCount)
        {
            // Quick check: if end node is completely surrounded by occupied cells, return null immediately
            int openNeighbors = 0;
            if (end.x > 0 && (grid[end.x - 1, end.y] == ColorType.None || grid[end.x - 1, end.y] == color || bridges.Contains(new Vector2Int(end.x - 1, end.y)))) openNeighbors++;
            if (end.x < w - 1 && (grid[end.x + 1, end.y] == ColorType.None || grid[end.x + 1, end.y] == color || bridges.Contains(new Vector2Int(end.x + 1, end.y)))) openNeighbors++;
            if (end.y > 0 && (grid[end.x, end.y - 1] == ColorType.None || grid[end.x, end.y - 1] == color || bridges.Contains(new Vector2Int(end.x, end.y - 1)))) openNeighbors++;
            if (end.y < h - 1 && (grid[end.x, end.y + 1] == ColorType.None || grid[end.x, end.y + 1] == color || bridges.Contains(new Vector2Int(end.x, end.y + 1)))) openNeighbors++;
            if (openNeighbors == 0 && start != end) return null;

            _pathStack.Clear();
            _gridChanges.Clear();
            _currentPath.Clear();
            _pathSet.Clear();
            _currentPath.Add(start);
            _pathSet.Add(start);

            _pathStack.Push(new PathFrame
            {
                Position = start,
                DirectionIndex = 0,
                GridChangesStart = 0,
                PathLengthBefore = 1
            });

            while (_pathStack.Count > 0)
            {
                if (iterationCount > _perSolveMaxIterations) return null;
                if (_cancellationToken.IsCancellationRequested) return null;
                iterationCount++;

                var frame = _pathStack.Peek();

                // Hedefe ulaştık mı?
                if (frame.Position == end)
                {
                    return new List<Vector2Int>(_currentPath);
                }

                // Sıradaki yönü dene
                var dirs = GetSortedDirections(frame.Position, end);
                bool foundMove = false;

                while (frame.DirectionIndex < 4 && !foundMove)
                {
                    var dir = dirs[frame.DirectionIndex];
                    frame.DirectionIndex++;
                    var next = frame.Position + dir;

                    // Sınır ve path kontrolü (HashSet ile O(1))
                    if (next.x < 0 || next.x >= w || next.y < 0 || next.y >= h) continue;
                    if (_pathSet.Contains(next)) continue;

                    // Obstacle check
                    if (blockedCells != null && blockedCells.Contains(next)) continue;

                    // OneWay cell direction check
                    if (oneWayDict != null && oneWayDict.Count > 0)
                    {
                        if (oneWayDict.TryGetValue(frame.Position, out var allowedFrom) && allowedFrom != Vector2Int.zero && dir != allowedFrom)
                            continue;
                        if (oneWayDict.TryGetValue(next, out var allowedTo) && allowedTo != Vector2Int.zero && dir != allowedTo)
                            continue;
                    }

                    bool isBridge = bridges.Contains(next);
                    bool canMove = false;

                    if (next == end)
                    {
                        canMove = true;
                    }
                    else if (isBridge)
                    {
                        var exit = next + dir;
                        if (exit.x >= 0 && exit.x < w && exit.y >= 0 && exit.y < h && !_pathSet.Contains(exit))
                        {
                            if (grid[exit.x, exit.y] == ColorType.None || exit == end)
                            {
                                int otherUse = 0;
                                ColorType otherColor = ColorType.None;
                                for (int i = 0; i < colorIndex; i++)
                                {
                                    if (solutions[colors[i]].Contains(next))
                                    {
                                        otherUse++;
                                        otherColor = colors[i];
                                    }
                                }

                                if (otherUse == 0)
                                    canMove = true;
                                else if (otherUse == 1)
                                {
                                    if (BridgeValidationUtility.IsValidBridgeCrossing(
                                        solutions[otherColor], _currentPath, next, dir))
                                        canMove = true;
                                }
                            }
                        }
                    }
                    else if (grid[next.x, next.y] == ColorType.None)
                    {
                        canMove = true;
                    }

                    if (canMove)
                    {
                        foundMove = true;

                        // Update current frame in stack with its new DirectionIndex before pushing next
                        _pathStack.Pop();
                        _pathStack.Push(frame);

                        if (isBridge)
                        {
                            var exit = next + dir;

                            // Grid değişikliklerini kaydet (bridge = 2 hücre)
                            _gridChanges.Push(new GridChange { X = next.x, Y = next.y, OldColor = grid[next.x, next.y] });
                            grid[next.x, next.y] = color;
                            _gridChanges.Push(new GridChange { X = exit.x, Y = exit.y, OldColor = grid[exit.x, exit.y] });
                            grid[exit.x, exit.y] = color;

                            int pathLenBefore = _currentPath.Count;
                            _currentPath.Add(next);
                            _pathSet.Add(next);
                            _currentPath.Add(exit);
                            _pathSet.Add(exit);

                            _pathStack.Push(new PathFrame
                            {
                                Position = exit,
                                DirectionIndex = 0,
                                GridChangesStart = _gridChanges.Count,
                                PathLengthBefore = pathLenBefore
                            });
                        }
                        else
                        {
                            // Grid değişikliğini kaydet (normal = 1 hücre)
                            _gridChanges.Push(new GridChange { X = next.x, Y = next.y, OldColor = grid[next.x, next.y] });
                            grid[next.x, next.y] = color;

                            int pathLenBefore = _currentPath.Count;
                            _currentPath.Add(next);
                            _pathSet.Add(next);

                            _pathStack.Push(new PathFrame
                            {
                                Position = next,
                                DirectionIndex = 0,
                                GridChangesStart = _gridChanges.Count,
                                PathLengthBefore = pathLenBefore
                            });
                        }
                    }
                }

                if (!foundMove)
                {
                    // Çıkmaz sokak → backtrack
                    _pathStack.Pop();

                    // Grid değişikliklerini geri al
                    while (_gridChanges.Count > frame.GridChangesStart)
                    {
                        var change = _gridChanges.Pop();
                        grid[change.X, change.Y] = change.OldColor;
                    }

                    // Path'i geri al (HashSet'ten de sil)
                    while (_currentPath.Count > frame.PathLengthBefore)
                    {
                        _pathSet.Remove(_currentPath[_currentPath.Count - 1]);
                        _currentPath.RemoveAt(_currentPath.Count - 1);
                    }
                }
            }

            return null;
        }

        private int CalculateMaxIterations(int width, int height, int colorCount)
        {
            int maxCap = MaxIterationsCap > 0 ? MaxIterationsCap : 10000;
            int baseFactor = ResolvedConfig != null && ResolvedConfig.PathSolverMaxIterations > 0 ? ResolvedConfig.PathSolverMaxIterations : 1000;
            long raw = width * height * colorCount * (long)baseFactor;
            int clamped = (int)Math.Min(maxCap, Math.Max(200, raw));
            return clamped;
        }

        private static Dictionary<ColorType, List<Vector2Int>> CollectColorNodes(LevelData level)
        {
            var colorNodes = new Dictionary<ColorType, List<Vector2Int>>();
            foreach (var node in level.initialNodes)
            {
                if (node.color == ColorType.None) continue;
                if (!colorNodes.ContainsKey(node.color))
                    colorNodes[node.color] = new List<Vector2Int>();
                colorNodes[node.color].Add(node.position);
            }

            foreach (var kvp in colorNodes)
            {
                if (kvp.Value.Count != 2) return null;
            }

            return colorNodes;
        }

        private static Vector2Int[] GetSortedDirections(Vector2Int current, Vector2Int end)
        {
            var dirs = new[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
            Array.Sort(dirs, (a, b) =>
            {
                int da = Mathf.Abs((current.x + a.x) - end.x) + Mathf.Abs((current.y + a.y) - end.y);
                int db = Mathf.Abs((current.x + b.x) - end.x) + Mathf.Abs((current.y + b.y) - end.y);
                return da.CompareTo(db);
            });
            return dirs;
        }
    }
}
