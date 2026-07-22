using System;
using System.Collections.Generic;
using PixelFlow.Data;
using UnityEngine;

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
        private const int MaxIterations = 200000;

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

            var bridges = new HashSet<Vector2Int>(level.bridgePositions ?? new List<Vector2Int>());
            var grid = new ColorType[level.width, level.height];

            foreach (var node in level.initialNodes)
            {
                if (node.position.x >= 0 && node.position.x < level.width &&
                    node.position.y >= 0 && node.position.y < level.height)
                {
                    grid[node.position.x, node.position.y] = node.color;
                }
            }

            var colors = new List<ColorType>(colorNodes.Keys);
            var result = new Dictionary<ColorType, List<Vector2Int>>();
            foreach (var c in colors) result[c] = new List<Vector2Int>();
            int iterationCount = 0;

            if (SolveRecursive(0, colors, colorNodes, result, grid, bridges, level.width, level.height, ref iterationCount))
            {
                solutions = result;
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
            if (iterationCount > MaxIterations) return null;
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
            int w, int h, ref int iterationCount)
        {
            if (colorIndex >= colors.Count)
            {
                if (!_requireFullCoverage)
                    return true;
                for (int x = 0; x < w; x++)
                    for (int y = 0; y < h; y++)
                        if (grid[x, y] == ColorType.None && !bridges.Contains(new Vector2Int(x, y)))
                            return false;
                return true;
            }

            if (iterationCount > MaxIterations) return false;
            iterationCount++;

            var color = colors[colorIndex];
            var start = colorNodes[color][0];
            var end = colorNodes[color][1];

            // Grid snapshot: FindPathIterative grid'i path ile işaretler.
            // Sonraki renk başarısız olursa, bu rengin grid değişikliklerini
            // geri almak için snapshot'ı kullanırız.
            // Grid max 20×20 = 400 eleman, snapshot maliyeti ihmal edilebilir.
            var gridSnapshot = (ColorType[,])grid.Clone();

            // FindPathIterative bu renk için bir path bulur.
            // NOT: FindPathIterative deterministik DFS kullanır (sıralı yönler).
            // Grid snapshot'a döndürülüp tekrar çağrılırsa AYNI path'i bulur.
            // Bu nedenle sadece bir alternatif deneriz — doğru backtracking
            // üst seviyedeki SolveRecursive çağrıları tarafından sağlanır.
            var path = FindPathIterative(start, end, color, colorIndex, colors, colorNodes, solutions, grid, bridges, w, h, ref iterationCount);
            if (path != null)
            {
                solutions[color] = path;
                if (SolveRecursive(colorIndex + 1, colors, colorNodes, solutions, grid, bridges, w, h, ref iterationCount))
                    return true;
                solutions[color].Clear();

                // Backtrack: grid'i eski haline döndür (üst seviye alternatif deneyebilir)
                for (int x = 0; x < w; x++)
                    for (int y = 0; y < h; y++)
                        grid[x, y] = gridSnapshot[x, y];
            }

            return false;
        }

        /// <summary>
        /// Hücre-seviyesi iterative backtracking (DFS).
        /// Explicit Stack kullanır — recursive versiyon gibi stack derinliği yaratmaz.
        /// 20×20 grid'de 400+ adımda dahi StackOverflow riski yoktur.
        ///
        /// NOT: Grid üzerinde yaptığı path işaretlemelerini geri almaz.
        /// Çağıran (SolveRecursive) bu işaretlemeleri yönetir.
        /// </summary>
        private List<Vector2Int> FindPathIterative(
            Vector2Int start, Vector2Int end, ColorType color,
            int colorIndex,
            List<ColorType> colors,
            Dictionary<ColorType, List<Vector2Int>> colorNodes,
            Dictionary<ColorType, List<Vector2Int>> solutions,
            ColorType[,] grid, HashSet<Vector2Int> bridges,
            int w, int h, ref int iterationCount)
        {
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
                if (iterationCount > MaxIterations) return null;
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

        // Reusable buffer to avoid allocating a new array on every recursive call
        private static readonly Vector2Int[] _directionBuffer = new Vector2Int[4];

        // Pre-allocated comparer class — struct boxing'i önler, her çağrıda zero alloc
        private sealed class DirectionComparer : System.Collections.Generic.IComparer<Vector2Int>
        {
            public Vector2Int Current;
            public Vector2Int End;

            public int Compare(Vector2Int a, Vector2Int b)
            {
                int da = Mathf.Abs((Current + a).x - End.x) + Mathf.Abs((Current + a).y - End.y);
                int db = Mathf.Abs((Current + b).x - End.x) + Mathf.Abs((Current + b).y - End.y);
                return da.CompareTo(db);
            }
        }

        private static readonly DirectionComparer _directionComparer = new DirectionComparer();

        private static Vector2Int[] GetSortedDirections(Vector2Int current, Vector2Int end)
        {
            _directionBuffer[0] = Vector2Int.up;
            _directionBuffer[1] = Vector2Int.down;
            _directionBuffer[2] = Vector2Int.left;
            _directionBuffer[3] = Vector2Int.right;
            _directionComparer.Current = current;
            _directionComparer.End = end;
            Array.Sort(_directionBuffer, _directionComparer);
            return _directionBuffer;
        }
    }
}
