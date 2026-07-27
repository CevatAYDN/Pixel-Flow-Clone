using UnityEngine;
using System.Collections.Generic;

namespace PixelFlow.Data
{
    public enum VehicleVisualMode
    {
        Mode3D_ToyMesh = 0,
        Mode2D_FlatSprite = 1
    }

    /// <summary>
    /// Araç görsel parametrelerinin ScriptableObject konfigürasyonu.
    /// Tüm boyutlar, offset'ler ve tren/araca özel ayarlar burada tanımlanır.
    /// Sıfır hardcode — her şey buradan okunur.
    /// </summary>
    [CreateAssetMenu(fileName = "VehicleVisualConfig", menuName = "PixelFlow/Vehicle Visual Config")]
    public class VehicleVisualConfigAsset : ScriptableObject
    {
        [Header("=== Görsel Mod (2D / 3D) ===")]
        [Tooltip("Araç görsel modu: Mode3D_ToyMesh (3D Oyuncak Araç) veya Mode2D_FlatSprite (2D Düz Sprite)")]
        public VehicleVisualMode VisualMode = VehicleVisualMode.Mode3D_ToyMesh;

        [Header("=== Tren Parametreleri ===")]
        [Tooltip("Ana gövde boyutu")]
        public Vector3 TrainBodySize = new Vector3(0.38f, 0.22f, 0.18f);

        [Tooltip("Kabin boyutu")]
        public Vector3 TrainCabinSize = new Vector3(0.18f, 0.20f, 0.16f);

        [Tooltip("Kabin ofseti (loco'ya göre)")]
        public Vector3 TrainCabinOffset = new Vector3(-0.06f, 0f, -0.10f);

        [Tooltip("Cam boyutu")]
        public Vector3 TrainWindshieldSize = new Vector3(0.04f, 0.18f, 0.08f);

        [Tooltip("Cam ofseti")]
        public Vector3 TrainWindshieldOffset = new Vector3(0.19f, 0f, -0.06f);

        [Tooltip("Far boyutu")]
        public Vector3 TrainHeadlightSize = new Vector3(0.05f, 0.08f, 0.06f);

        [Tooltip("Far ofseti")]
        public Vector3 TrainHeadlightOffset = new Vector3(0.20f, 0f, 0.02f);

        [Tooltip("Çizgi boyutu")]
        public Vector3 TrainStripeSize = new Vector3(0.36f, 0.06f, 0.04f);

        [Tooltip("Çizgi ofseti")]
        public Vector3 TrainStripeOffset = new Vector3(0f, 0f, -0.19f);

        [Tooltip("Tekerlek boyutu")]
        public Vector3 TrainWheelSize = new Vector3(0.07f, 0.02f, 0.07f);

        [Tooltip("Tekerlek Y ofseti")]
        public float TrainWheelYOffset = 0.09f;

        [Tooltip("Tekerlek Z ofseti")]
        public float TrainWheelZOffset = 0.05f;

        [Tooltip("Loco tekerlek pozisyonları (x, z)")]
        public List<Vector2Int> TrainLocoWheelPositions = new List<Vector2Int>
        {
            new Vector2Int(10, 0),
            new Vector2Int(-10, 0)
        };

        [Tooltip("Coupler boyutu")]
        public Vector3 TrainCouplerSize = new Vector3(0.10f, 0.06f, 0.06f);

        [Tooltip("Vagon 1 gövde boyutu")]
        public Vector3 TrainWagon1BodySize = new Vector3(0.34f, 0.20f, 0.16f);

        [Tooltip("Vagon 1 cam boyutu")]
        public Vector3 TrainWagon1WindowSize = new Vector3(0.24f, 0.02f, 0.06f);

        [Tooltip("Vagon 1 cam ofsetleri (y çarpımı side için)")]
        public List<Vector3Int> TrainWagon1WindowOffsets = new List<Vector3Int>
        {
            new Vector3Int(0, 10, -3)
        };

        [Tooltip("Vagon 1 tekerlek pozisyonları")]
        public List<Vector2Int> TrainWagon1WheelPositions = new List<Vector2Int>
        {
            new Vector2Int(10, 0),
            new Vector2Int(-10, 0)
        };

        [Tooltip("Vagon 2 gövde boyutu")]
        public Vector3 TrainWagon2BodySize = new Vector3(0.32f, 0.20f, 0.16f);

        [Tooltip("Vagon 2 tekerlek pozisyonları")]
        public List<Vector2Int> TrainWagon2WheelPositions = new List<Vector2Int>
        {
            new Vector2Int(10, 0),
            new Vector2Int(-10, 0)
        };

        [Tooltip("Tren trail süresi")]
        public float TrainTrailTime = 0.55f;

        [Tooltip("Tren trail başlangıç genişliği")]
        public float TrainTrailStartWidth = 0.22f;

        [Header("=== Araç Parametreleri ===")]
        [Tooltip("Araç gövde boyutu")]
        public Vector3 CarBodySize = new Vector3(0.44f, 0.26f, 0.16f);

        [Tooltip("Araç kabin boyutu")]
        public Vector3 CarCabinSize = new Vector3(0.24f, 0.20f, 0.12f);

        [Tooltip("Araç kabin ofseti")]
        public Vector3 CarCabinOffset = new Vector3(-0.03f, 0f, -0.12f);

        [Tooltip("Araç far boyutu")]
        public Vector3 CarHeadlightSize = new Vector3(0.04f, 0.20f, 0.06f);

        [Tooltip("Araç far ofseti")]
        public Vector3 CarHeadlightOffset = new Vector3(0.22f, 0f, -0.02f);

        [Tooltip("Araç arka ışık boyutu")]
        public Vector3 CarTaillightSize = new Vector3(0.04f, 0.20f, 0.05f);

        [Tooltip("Araç arka ışık ofseti")]
        public Vector3 CarTaillightOffset = new Vector3(-0.22f, 0f, -0.02f);

        [Tooltip("Araç tekerlek boyutu")]
        public Vector3 CarWheelSize = new Vector3(0.09f, 0.02f, 0.09f);

        [Tooltip("Araç tekerlek X pozisyonları")]
        public List<float> CarWheelXPositions = new List<float> { -0.14f, 0.14f };

        [Tooltip("Araç tekerlek Y pozisyonları")]
        public List<float> CarWheelYPositions = new List<float> { -0.12f, 0.12f };

        [Tooltip("Araç tekerlek Z ofseti")]
        public float CarWheelZOffset = 0.06f;

        [Tooltip("Araç trail süresi")]
        public float CarTrailTime = 0.45f;

        [Tooltip("Araç trail başlangıç genişliği")]
        public float CarTrailStartWidth = 0.18f;
    }
}
