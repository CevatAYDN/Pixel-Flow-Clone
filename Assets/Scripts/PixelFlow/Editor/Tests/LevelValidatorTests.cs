using System.Collections.Generic;
using NUnit.Framework;
using PixelFlow.Data;
using PixelFlow.Services;
using UnityEngine;

namespace PixelFlow.Editor.Tests
{
    [TestFixture]
    public class LevelValidatorTests
    {
        private LevelValidator _validator;

        [SetUp]
        public void SetUp()
        {
            _validator = new LevelValidator();
        }

        [Test]
        public void Validate_NullLevel_ReturnsErrorResult()
        {
            var result = _validator.Validate(null);
            Assert.IsFalse(result.IsValid);
            Assert.IsTrue(result.Issues.Exists(i => i.Message.Contains("null")));
        }

        [Test]
        public void Validate_ValidLevel_ReturnsValidResult()
        {
            var level = ScriptableObject.CreateInstance<LevelData>();
            level.width = 5;
            level.height = 5;
            level.viaductLimit = 2;
            level.initialNodes = new List<GridNode>
            {
                new GridNode { position = new Vector2Int(0, 0), color = ColorType.Red, isSource = true, pairIndex = 0 },
                new GridNode { position = new Vector2Int(4, 4), color = ColorType.Red, isSource = false, pairIndex = 1 }
            };

            var result = _validator.Validate(level);
            Assert.IsTrue(result.IsValid);
        }

        [Test]
        public void Validate_UnpairedNodes_ReturnsErrorResult()
        {
            var level = ScriptableObject.CreateInstance<LevelData>();
            level.width = 5;
            level.height = 5;
            level.initialNodes = new List<GridNode>
            {
                new GridNode { position = new Vector2Int(0, 0), color = ColorType.Red, isSource = true, pairIndex = 0 },
                new GridNode { position = new Vector2Int(2, 2), color = ColorType.Red, isSource = true, pairIndex = 0 } // Two sources, 0 target
            };

            var result = _validator.Validate(level);
            Assert.IsFalse(result.IsValid);
            Assert.IsTrue(result.Issues.Exists(i => i.Message.Contains("Source") || i.Message.Contains("Target")));
        }

        [Test]
        public void Validate_ObstacleNodeOverlap_ReturnsErrorResult()
        {
            var level = ScriptableObject.CreateInstance<LevelData>();
            level.width = 5;
            level.height = 5;
            level.initialNodes = new List<GridNode>
            {
                new GridNode { position = new Vector2Int(1, 1), color = ColorType.Blue, isSource = true }
            };
            level.obstacles = new List<ObstacleData>
            {
                new ObstacleData { position = new Vector2Int(1, 1), type = ObstacleType.Construction }
            };

            var result = _validator.Validate(level);
            Assert.IsFalse(result.IsValid);
            Assert.IsTrue(result.Issues.Exists(i => i.Message.Contains("overlaps")));
        }
    }
}
