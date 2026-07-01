using System.Collections.Generic;
using PixelFlow.Data;
using UnityEngine;

namespace PixelFlow.Services
{
    public static class BridgeValidationUtility
    {
        public const int MaxPathsPerBridge = 2;

        public static Vector2Int GetCrossingDirection(IList<Vector2Int> path, Vector2Int bridgePos)
        {
            int idx = path.IndexOf(bridgePos);
            if (idx <= 0 || idx >= path.Count - 1)
                return Vector2Int.zero;

            var prev = path[idx - 1];
            var next = path[idx + 1];

            if (prev.x == next.x)
                return new Vector2Int(0, next.y - prev.y);
            if (prev.y == next.y)
                return new Vector2Int(next.x - prev.x, 0);

            return Vector2Int.zero;
        }

        public static bool ArePerpendicular(Vector2Int dirA, Vector2Int dirB)
        {
            return dirA.x * dirB.x + dirA.y * dirB.y == 0
                && dirA != Vector2Int.zero && dirB != Vector2Int.zero;
        }

        public static bool IsValidBridgeCrossing(
            IList<Vector2Int> existingPath, IList<Vector2Int> newPath,
            Vector2Int bridgePos, Vector2Int newEntryDir)
        {
            var existingDir = GetCrossingDirection(existingPath, bridgePos);
            if (existingDir == Vector2Int.zero) return false;

            return ArePerpendicular(existingDir, newEntryDir);
        }

        public static int CountBridgeUsers(
            Dictionary<ColorType, List<Vector2Int>> paths, Vector2Int bridgePos)
        {
            int count = 0;
            foreach (var path in paths.Values)
            {
                if (path.Contains(bridgePos))
                    count++;
            }
            return count;
        }
    }
}
