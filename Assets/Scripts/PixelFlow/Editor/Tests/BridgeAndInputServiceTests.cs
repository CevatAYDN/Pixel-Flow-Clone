using NUnit.Framework;
using PixelFlow.Services;
using System.Collections.Generic;
using UnityEngine;

namespace PixelFlow.Editor.Tests
{
    [TestFixture]
    public class BridgeAndInputServiceTests
    {
        [Test]
        public void BridgeValidationUtility_CrossingDirection_CalculatesCorrectVector()
        {
            var path = new List<Vector2Int>
            {
                new Vector2Int(1, 2),
                new Vector2Int(2, 2),
                new Vector2Int(3, 2)
            };

            var dir = BridgeValidationUtility.GetCrossingDirection(path, new Vector2Int(2, 2));
            Assert.AreEqual(new Vector2Int(2, 0), dir);
        }

        [Test]
        public void BridgeValidationUtility_ArePerpendicular_IdentifiesPerpendicularVectors()
        {
            var horizontal = new Vector2Int(1, 0);
            var vertical = new Vector2Int(0, 1);
            var parallel = new Vector2Int(2, 0);

            Assert.IsTrue(BridgeValidationUtility.ArePerpendicular(horizontal, vertical));
            Assert.IsFalse(BridgeValidationUtility.ArePerpendicular(horizontal, parallel));
        }

        [Test]
        public void BridgeValidationUtility_IsValidBridgeCrossing_EnforcesPerpendicularPath()
        {
            var existingPath = new List<Vector2Int>
            {
                new Vector2Int(1, 2),
                new Vector2Int(2, 2),
                new Vector2Int(3, 2)
            };

            var perpendicularEntry = new Vector2Int(0, 1);
            var parallelEntry = new Vector2Int(1, 0);

            Assert.IsTrue(BridgeValidationUtility.IsValidBridgeCrossing(existingPath, null, new Vector2Int(2, 2), perpendicularEntry));
            Assert.IsFalse(BridgeValidationUtility.IsValidBridgeCrossing(existingPath, null, new Vector2Int(2, 2), parallelEntry));
        }

        [Test]
        public void GridInputService_Reset_ResetsPointerState()
        {
            var inputService = new GridInputService();
            Assert.DoesNotThrow(() => inputService.Reset());
        }
    }
}
