using UnityEngine;
using System.Collections.Generic;

namespace PixelFlow.Data
{
    public enum ColorType { None, Red, Green, Blue, Yellow, Orange, Purple, Cyan, Magenta }

    [System.Serializable]
    public struct GridNode
    {
        public Vector2Int position;
        public ColorType color;
    }

    [System.Serializable]
    public struct PathSolution
    {
        public ColorType color;
        public List<Vector2Int> pathPositions;
    }

    [CreateAssetMenu(fileName = "LevelData", menuName = "PixelFlow/LevelData")]
    public class LevelData : ScriptableObject
    {
        public int levelIndex;
        public int width = 5;
        public int height = 5;
        public List<GridNode> initialNodes = new List<GridNode>();
        public List<PathSolution> solutions = new List<PathSolution>();
        public List<Vector2Int> bridgePositions = new List<Vector2Int>();
        public int viaductLimit = 3;
    }
}
