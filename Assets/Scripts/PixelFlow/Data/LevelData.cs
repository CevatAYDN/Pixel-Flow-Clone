using UnityEngine;
using System.Collections.Generic;

namespace PixelFlow.Data
{
    public enum ColorType { None, Red, Green, Blue, Yellow, Orange, Purple, Cyan, Magenta }

    public enum ObstacleType { None, Construction, Lake, Park, OneWay, Ferry, NarrowPass }

    /// <summary>
    /// GDD §9.4: Tamamen bloklanmış, çizilemez hücreler.
    /// Örn. İnşaat alanı, gölet, park.
    /// </summary>
    [System.Serializable]
    public struct ObstacleData
    {
        public Vector2Int position;
        public ObstacleType type;
    }

    /// <summary>
    /// GDD §2.7 + §9.4: OneWay (Tek Yön) hücresi. Çizilebilir ama sadece tek yönde.
    /// Viyadüğe alternatif taktik mekaniği (Seviye 20+).
    /// </summary>
    [System.Serializable]
    public struct OneWayCell
    {
        public Vector2Int position;
        /// <summary>İzin verilen hareket yönü. (1,0)=sağ, (-1,0)=sol, (0,1)=yukarı, (0,-1)=aşağı.</summary>
        public Vector2Int allowedDirection;
    }

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

        [Min(3)] public int width = 5;
        [Min(3)] public int height = 5;

        [Header("Nodes (2 per color)")]
        public List<GridNode> initialNodes = new List<GridNode>();

        [Header("Authored Solutions / Path Data")]
        public List<PathSolution> solutions = new List<PathSolution>();

        [Header("Bridges / Viyadük Hakkı")]
        public List<Vector2Int> bridgePositions = new List<Vector2Int>();
        public int viaductLimit = 3;

        [Header("Kazanma Koşulu (GDD §2.8)")]
        [Tooltip("Seviye 29+ için tüm hücreler yol tarafından kaplanmalı; erken seviyeler için esnek bağlama yeterli.")]
        public bool requireFullGridCoverage = false;
        [Tooltip("Simülasyon başına Flow Score eşik değeri. İlk çalıştırmada bu değere ulaşılınca kazanılır. " +
                 "Faz 1: 3-5, Faz 2: 6-10, Faz 3: 12-18, Faz 4: 18-30.")]
        public int flowScoreThreshold = 5;

        [Header("Engeller (GDD §9.4)")]
        public List<ObstacleData> obstacles = new List<ObstacleData>();

        [Header("OneWay (Seviye 20+, GDD §2.7 — Viyadüğe alternatif taktik)")]
        public List<OneWayCell> oneWayCells = new List<OneWayCell>();
    }
}
