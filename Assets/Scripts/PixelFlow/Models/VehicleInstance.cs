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

        // Not: Bobbing animasyonu için v.GetHashCode() direkt kullanılır
        // (GetCachedHash eklenmişti ama runtime'da kullanılmıyor — temizlendi)
    }
}
