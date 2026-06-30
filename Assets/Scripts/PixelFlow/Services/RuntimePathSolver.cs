using System;
using System.Collections.Generic;
using System.Linq;
using PixelFlow.Data;
using UnityEngine;

namespace PixelFlow.Services
{
    /// <summary>
    /// Editor auto-solver algoritmasının runtime versiyonu.
    /// Recursive backtracking ile tüm olası path'leri dener.
    /// Bridge crossing kurallarını destekler.
    /// </summary>
    public sealed class RuntimePathSolver : IPathSolver
    {
        private const int MaxIterations = 500000;

        public bool Solve(LevelData level, out Dictionary<ColorType, List<Vector2Int>> solutions)
        {
            solutions = null;

            var colorNodes = CollectColorNodes(level);
            if (colorNodes == null) return false;

            var bridges = new HashSet<Vector2Int>(level.bridgePositions ?? Enumerable.Empty<Vector2Int>());
            var grid = new ColorType[level.width, level.height];

            foreach (var node in level.initialNodes)
            {
                if (node.position.x >= 0 && node.position.x < level.width &&
                    node.position.y >= 0 && node.position.y < level.height)
                {
                    grid[node.position.x, node.position.y] = node.color;
                }
            }

            var colors = colorNodes.Keys.ToList();
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

            var bridges = new HashSet<Vector2Int>(level.bridgePositions ?? Enumerable.Empty<Vector2Int>());
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

            var resultPath = FindPartialPath(start, end, color, currentPath, grid, bridges, level.width, level.height, steps);
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
            int w, int h, int maxSteps)
        {
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

                        var sub = FindPartialPath(exit, end, color, path, grid, bridges, w, h, maxSteps);
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

                        var sub = FindPartialPath(next, end, color, path, grid, bridges, w, h, maxSteps);
                        if (sub != null) return sub;

                        path.RemoveAt(path.Count - 1);
                        grid[next.x, next.y] = oldColor;
                    }
                }
            }

            return null;
        }

        private bool SolveRecursive(
            int colorIndex, List<ColorType> colors,
            Dictionary<ColorType, List<Vector2Int>> colorNodes,
            Dictionary<ColorType, List<Vector2Int>> solutions,
            ColorType[,] grid, HashSet<Vector2Int> bridges,
            int w, int h, ref int iterationCount)
        {
            if (colorIndex >= colors.Count)
            {
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
            var path = new List<Vector2Int> { start };

            return FindFullPath(start, end, color, path, colorIndex, colors, colorNodes, solutions, grid, bridges, w, h, ref iterationCount);
        }

        private bool FindFullPath(
            Vector2Int current, Vector2Int end, ColorType color,
            List<Vector2Int> path, int colorIndex,
            List<ColorType> colors,
            Dictionary<ColorType, List<Vector2Int>> colorNodes,
            Dictionary<ColorType, List<Vector2Int>> solutions,
            ColorType[,] grid, HashSet<Vector2Int> bridges,
            int w, int h, ref int iterationCount)
        {
            if (current == end)
            {
                solutions[color] = new List<Vector2Int>(path);
                if (SolveRecursive(colorIndex + 1, colors, colorNodes, solutions, grid, bridges, w, h, ref iterationCount))
                    return true;
                solutions[color].Clear();
                return false;
            }

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
                    if (exit.x >= 0 && exit.x < w && exit.y >= 0 && exit.y < h && !path.Contains(exit))
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
                                var otherPath = solutions[otherColor];
                                int idx = otherPath.IndexOf(next);
                                if (idx > 0 && idx < otherPath.Count - 1)
                                {
                                    var inDir = next - otherPath[idx - 1];
                                    var outDir = otherPath[idx + 1] - next;
                                    if (inDir == outDir && Vector2.Dot(dir, inDir) == 0)
                                        canMove = true;
                                }
                            }
                        }
                    }
                }
                else if (grid[next.x, next.y] == ColorType.None)
                {
                    canMove = true;
                }

                if (canMove && isBridge)
                {
                    var exit = next + dir;
                    var oldNext = grid[next.x, next.y];
                    var oldExit = grid[exit.x, exit.y];

                    grid[next.x, next.y] = color;
                    grid[exit.x, exit.y] = color;
                    path.Add(next);
                    path.Add(exit);

                    if (FindFullPath(exit, end, color, path, colorIndex, colors, colorNodes, solutions, grid, bridges, w, h, ref iterationCount))
                        return true;

                    path.RemoveAt(path.Count - 1);
                    path.RemoveAt(path.Count - 1);
                    grid[next.x, next.y] = oldNext;
                    grid[exit.x, exit.y] = oldExit;
                }
                else if (canMove)
                {
                    var oldColor = grid[next.x, next.y];
                    grid[next.x, next.y] = color;
                    path.Add(next);

                    if (FindFullPath(next, end, color, path, colorIndex, colors, colorNodes, solutions, grid, bridges, w, h, ref iterationCount))
                        return true;

                    path.RemoveAt(path.Count - 1);
                    grid[next.x, next.y] = oldColor;
                }
            }

            return false;
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
                int da = Mathf.Abs((current + a).x - end.x) + Mathf.Abs((current + a).y - end.y);
                int db = Mathf.Abs((current + b).x - end.x) + Mathf.Abs((current + b).y - end.y);
                return da.CompareTo(db);
            });
            return dirs;
        }
    }
}
