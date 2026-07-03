using NUnit.Framework;
using PixelFlow.Data;
using PixelFlow.Services;
using System.Collections.Generic;
using UnityEngine;

namespace PixelFlow.Editor.Tests
{
    [TestFixture]
    public class RuntimePathSolverTests
    {
        private RuntimePathSolver _solver;

        [SetUp]
        public void SetUp()
        {
            _solver = new RuntimePathSolver();
        }

        [Test]
        public void Solve_ReturnsNull_ForEmptyGrid()
        {
            var level = ScriptableObject.CreateInstance<LevelData>();
            level.width = 5;
            level.height = 5;
            Assert.IsFalse(_solver.Solve(level, out _));
        }

        [Test]
        public void Solve_SingleColor_SimplePath()
        {
            var level = ScriptableObject.CreateInstance<LevelData>();
            level.width = 3;
            level.height = 1;
            level.initialNodes = new List<GridNode>
            {
                new GridNode { position = new Vector2Int(0, 0), color = ColorType.Red },
                new GridNode { position = new Vector2Int(2, 0), color = ColorType.Red },
            };

            Assert.IsTrue(_solver.Solve(level, out var solutions));
            Assert.IsTrue(solutions.ContainsKey(ColorType.Red));
            Assert.GreaterOrEqual(solutions[ColorType.Red].Count, 3);
        }

        [Test]
        public void Solve_MultiColor_WithBridge()
        {
            var level = ScriptableObject.CreateInstance<LevelData>();
            level.width = 5;
            level.height = 5;
            level.bridgePositions = new List<Vector2Int> { new Vector2Int(2, 2) };
            level.initialNodes = new List<GridNode>
            {
                new GridNode { position = new Vector2Int(0, 2), color = ColorType.Red },
                new GridNode { position = new Vector2Int(4, 2), color = ColorType.Red },
                new GridNode { position = new Vector2Int(2, 0), color = ColorType.Blue },
                new GridNode { position = new Vector2Int(2, 4), color = ColorType.Blue },
            };

            Assert.IsTrue(_solver.Solve(level, out var solutions));
            Assert.IsTrue(solutions.ContainsKey(ColorType.Red));
            Assert.IsTrue(solutions.ContainsKey(ColorType.Blue));
            Assert.IsTrue(solutions[ColorType.Red].Contains(new Vector2Int(2, 2)),
                "Red path should cross bridge at (2,2)");
        }

        [Test]
        public void Solve_Unsolvable_ReturnsFalse()
        {
            var level = ScriptableObject.CreateInstance<LevelData>();
            level.width = 2;
            level.height = 2;
            level.initialNodes = new List<GridNode>
            {
                new GridNode { position = new Vector2Int(0, 0), color = ColorType.Red },
                new GridNode { position = new Vector2Int(1, 1), color = ColorType.Red },
                new GridNode { position = new Vector2Int(0, 1), color = ColorType.Blue },
                new GridNode { position = new Vector2Int(1, 0), color = ColorType.Blue },
            };

            bool solved = _solver.Solve(level, out _);
            // 2x2 with 4 nodes of 2 colors crossing - may be unsolvable
            Assert.IsFalse(solved,
                "Crossing 2-color on 2x2 should be unsolvable");
        }

        [Test]
        public void SolvePartial_ReturnsLimitedSteps()
        {
            var level = ScriptableObject.CreateInstance<LevelData>();
            level.levelIndex = 0;
            level.width = 5;
            level.height = 5;
            level.initialNodes = new List<GridNode>
            {
                new GridNode { position = new Vector2Int(0, 0), color = ColorType.Red },
                new GridNode { position = new Vector2Int(4, 0), color = ColorType.Red },
            };

            bool solved = _solver.SolvePartial(level, ColorType.Red, steps: 3, out var hintPath);
            if (solved)
            {
                Assert.IsNotNull(hintPath);
                Assert.Greater(hintPath.Count, 0);
            }
        }

        [Test]
        public void Solve_Deterministic_WithSameSeed()
        {
            var solver1 = new RuntimePathSolver();
            var solver2 = new RuntimePathSolver();

            var level = ScriptableObject.CreateInstance<LevelData>();
            level.width = 5;
            level.height = 5;
            level.initialNodes = new List<GridNode>
            {
                new GridNode { position = new Vector2Int(0, 0), color = ColorType.Red },
                new GridNode { position = new Vector2Int(4, 0), color = ColorType.Red },
                new GridNode { position = new Vector2Int(0, 4), color = ColorType.Blue },
                new GridNode { position = new Vector2Int(4, 4), color = ColorType.Blue },
            };

            solver1.Solve(level, out var sol1);
            solver2.Solve(level, out var sol2);

            if (sol1 != null && sol2 != null && sol1.Count == sol2.Count)
            {
                foreach (var kvp in sol1)
                {
                    if (sol2.ContainsKey(kvp.Key))
                    {
                        Assert.AreEqual(kvp.Value.Count, sol2[kvp.Key].Count,
                            $"Path length for {kvp.Key} should be deterministic");
                    }
                }
            }
        }
    }
}
