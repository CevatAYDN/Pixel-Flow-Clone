using UnityEngine;
using System.Collections.Generic;

namespace PixelFlow.Data
{
    /// <summary>
/// GDD §3.2: 5 renk paleti — her renk bir şekille eşleşir (renk körlüğü erişilebilirlik).
/// Blue=Circle, Red=Triangle, Yellow=Square, Green=Diamond, Purple=Star
/// </summary>
public enum ColorType { None, Red, Green, Blue, Yellow, Purple }

    /// <summary>GDD §3.2: Renk körü güvenli çift kodlama için şekil tipleri.</summary>
    public enum ShapeType { Circle, Triangle, Square, Diamond, Star }

    /// <summary>GDD §3.1: Düğüm fonksiyonel tipleri.</summary>
    public enum NodeType { Home, Office, Hospital, School, Park, Mall }

    /// <summary>GDD §3.1: Orthogonal yönler.</summary>
    public enum Direction { Up, Down, Left, Right }

    /// <summary>GDD §3.5: Engel tipleri.</summary>
    public enum ObstacleType { None, Construction, Lake, Park, OneWay, Ferry, NarrowPass }

    /// <summary>GDD §5.1: Kazanma koşulu tipleri.</summary>
    public enum WinConditionType { FlowScoreThreshold, FullGridCoverage }

    /// <summary>GDD §2.4: Kaza çözüm stratejileri.</summary>
    public enum ViaductResolution { Reroute, OneWay, Viaduct }

    /// <summary>GDD §2.4: Kriz paneli seçenekleri.</summary>
    public enum CrisisChoice { Undo, Viaduct }

    /// <summary>GDD §8: Tutorial olay tipleri.</summary>
    public enum TutorialEvent { None, CrashIntro, ViaductIntro, OneWayIntro, ObstacleIntro }

    /// <summary>
    /// Yıldız kriterleri (editörde yazar tarafından görüntülenen string ifadeler).
    /// NOT: Runtime yıldız hesabı EconomyConfig.CalculateStars(viaductsUsed) ile yapılır
    /// (bkz. ScoreCalculator); bu alan sahne/editör dokümantasyonu içindir.
    /// </summary>
    [System.Serializable]
    public struct StarCriteria
    {
        public string OneStar;
        public string TwoStars;
        public string ThreeStars;

        public static StarCriteria Default => new StarCriteria
        {
            OneStar = "complete",
            TwoStars = "viaducts_used <= 2",
            ThreeStars = "viaducts_used == 0"
        };
    }

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
        /// <summary>GDD §3.2: Renk körü güvenli çift kodlama için şekil.</summary>
        public ShapeType shape;
        /// <summary>GDD §3.1: Düğüm tipi (Home/Office/Hospital/School/Park/Mall).</summary>
        public NodeType type;
        /// <summary>GDD §3.4: true = kaynak (ev), false = hedef.</summary>
        public bool isSource;
        /// <summary>GDD §3.4: Aynı renkteki çiftin indeksi (0-based).</summary>
        public int pairIndex;
    }

    [System.Serializable]
    public struct PathSolution
    {
        public ColorType color;
        public List<Vector2Int> pathPositions;
    }

    /// <summary>game_plan.md §2.1.A: Seviye bazlı 3D Toy Temaları.</summary>
    public enum ToyThemeType { Default, PastelToy, NeonCity, CandyPark, Woodland }

    /// <summary>game_plan.md §2.1.A: Zıplayan Araç (Bouncy Physics) parametreleri.</summary>
    [System.Serializable]
    public struct BouncyPhysicsConfig
    {
        [Tooltip("Zıplama kuvveti (g-force/impulse)")] public float BounceForce;
        [Tooltip("Zayıflama / sönümleme katsayısı")] public float BounceDamping;
        [Tooltip("Esneklik / ezilme-büzülme şiddeti")] public float SquishFactor;

        public static BouncyPhysicsConfig Default => new BouncyPhysicsConfig
        {
            BounceForce = 4.5f,
            BounceDamping = 0.75f,
            SquishFactor = 0.35f
        };
    }

    [CreateAssetMenu(fileName = "LevelData", menuName = "PixelFlow/LevelData")]
    public class LevelData : ScriptableObject
    {
        public int levelIndex;

        [Min(3)] public int width = 5;
        [Min(3)] public int height = 5;

        [Header("3D Toy Theme (game_plan.md §2.1.A)")]
        public ToyThemeType toyTheme = ToyThemeType.PastelToy;

        [Header("Zıplayan Araç Physics (game_plan.md §2.1.A)")]
        public BouncyPhysicsConfig bouncyPhysics = BouncyPhysicsConfig.Default;

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

        [Header("Procedural Difficulty (GDD §9)")]
        [Tooltip("GDD §9 formülüyle hesaplanan zorluk skoru: (Colors×10)+(Intersections×5)+(Obstacles×3)-(ViaductLimit×4)")]
        public int difficultyScore;

        [Header("Yıldız Kriterleri (GDD §3.5)")]
        public StarCriteria stars = StarCriteria.Default;

        [Header("Tutorial (GDD §8)")]
        public TutorialEvent tutorialEvent = TutorialEvent.None;

        [Header("Engeller (GDD §9.4)")]
        public List<ObstacleData> obstacles = new List<ObstacleData>();

        [Header("OneWay (Seviye 20+, GDD §2.7 — Viyadüğe alternatif taktik)")]
        public List<OneWayCell> oneWayCells = new List<OneWayCell>();
    }
}
