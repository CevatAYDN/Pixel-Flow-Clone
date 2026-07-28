using System.Collections.Generic;
using UnityEngine;
using PixelFlow.Data;

namespace PixelFlow.Models
{
    /// <summary>
    /// Bir aracın runtime state'ini tutar.
    /// Eskiden VehicleSimulator içinde inner class'tı; refactor sonucu ayrıldı.
    /// </summary>
    public class VehicleInstance
    {
        public ColorType Color;
        public VehicleStyle Style;
        public List<Vector2Int> Path;
        public int SegmentIndex;
        public float Progress;
        public float TotalDistance;
        public GameObject Visual;
        public Vector3 CurrentPosition;
        public float Speed;
        public Renderer[] CachedRenderers;

        public Transform LocoTransform;
        public Transform Wagon1Transform;
        public Transform Wagon2Transform;
        public Transform Coupler1Transform;
        public Transform Coupler2Transform;

        public readonly MaterialPropertyBlock Mpb = new MaterialPropertyBlock();

        /// <summary>
        /// Bobbing ve ghost alpha animasyonları için per-vehicle offset.
        /// Spawn'da Random.Range ile doldurulur. GetHashCode() yerine kullanılır
        /// çünkü GetHashCode() GC compaction sonrası değişebilir → stutter.
        /// </summary>
        public float AnimationOffset;

        /// <summary>
        /// Ghost alpha MPB güncelleme throttle sayacı.
        /// VehicleMovementService her frame 1 artırır, 2 olunca MPB günceller ve sıfırlar.
        /// </summary>
        public int GhostAlphaCounter;
    }
}
