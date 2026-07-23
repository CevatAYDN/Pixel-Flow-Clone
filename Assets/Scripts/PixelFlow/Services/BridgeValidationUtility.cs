using System.Collections.Generic;
using PixelFlow.Data;
using UnityEngine;

namespace PixelFlow.Services
{
    public static class BridgeValidationUtility
    {
        /// <summary>game_plan.md §2.2: GameConfig.MaxPathsPerBridge üzerinden erişilir.</summary>
        public static int GetMaxPathsPerBridge(Data.GameConfig config)
        {
            return config != null ? config.MaxPathsPerBridge : throw new Data.DataValidationException("GameConfig.MaxPathsPerBridge erişilemedi!");
        }

        public static Vector2Int GetCrossingDirection(IList<Vector2Int> path, Vector2Int bridgePos)
        {
            int idx = path.IndexOf(bridgePos);
            if (idx < 0)
                return Vector2Int.zero;

            // Start node of the path cannot be crossed
            if (idx == 0)
                return Vector2Int.zero;

            // If it is the end of the path (which is a regular path cell if the path is incomplete)
            if (idx == path.Count - 1)
            {
                if (path.Count < 2)
                    return Vector2Int.zero;
                return bridgePos - path[idx - 1];
            }

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

        public static bool HasReachedMaxPaths(HashSet<ColorType> existingColors, Vector2Int bridgePos, int maxPathsPerBridge)
        {
            return existingColors.Count >= maxPathsPerBridge;
        }

        public static string GetRejectionReason(
            IList<Vector2Int> existingPath, IList<Vector2Int> newPath,
            Vector2Int bridgePos, Vector2Int newEntryDir)
        {
            if (!newPath.Contains(bridgePos))
                return "Path doesn't cross this cell";

            var existingDir = GetCrossingDirection(existingPath, bridgePos);
            if (existingDir == Vector2Int.zero)
                return "Path doesn't cross this cell";

            if (newEntryDir == Vector2Int.zero)
                return "Invalid direction";

            if (!ArePerpendicular(existingDir, newEntryDir))
                return "Not perpendicular";

            return null;
        }
    }
}
