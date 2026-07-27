using System.Collections.Generic;
using UnityEngine;
using PixelFlow.Data;
using PixelFlow.Models;

namespace PixelFlow.Views
{
    /// <summary>
    /// Config-driven procedural araç/tren görsel üretimi.
    /// Tüm boyutlar, renkler ve parametreler VehicleVisualConfigAsset'ten okunur.
    /// Sıfır hardcode — her şey data-driven.
    /// </summary>
    public static class VehicleVisualFactory
    {
        private static VehicleMaterialConfigAsset _materialConfig;
        private static VehicleVisualConfigAsset _visualConfig;

        /// <summary>
        /// Bootstrap'ta GameContextLifecycle tarafından çağrılır.
        /// </summary>
        public static void Initialize(VehicleMaterialConfigAsset materialConfig, VehicleVisualConfigAsset visualConfig)
        {
            _materialConfig = materialConfig;
            _visualConfig = visualConfig;

            // Force re-creation with new config colors
            _sharedSpriteMat = null;
            _sharedMetalMat = null;
            _sharedWindowMat = null;
            _sharedHeadlightMat = null;
            _sharedWhiteMat = null;
            _sharedTailMat = null;

            // Ghost alpha global'ini full opak olarak başlat — ilk frame'de 0 görünmezlik olmasın
            Shader.SetGlobalFloat("_PixelFlow_GhostAlpha", 1f);
        }

        // Shared materials for vehicle visuals — prevents new Material per-primitive
        private static Material _sharedSpriteMat;
        private static Material _sharedMetalMat;
        private static Material _sharedWindowMat;
        private static Material _sharedHeadlightMat;
        private static Material _sharedWhiteMat;
        private static Material _sharedTailMat;

        private static void EnsureAllSharedMaterialsCreated()
        {
            if (_sharedSpriteMat != null) return;
            var cfg = _materialConfig;
            var shader = Shader.Find("Hidden/PixelFlow/VehicleGhost") ?? Shader.Find("Sprites/Default");

            _sharedSpriteMat = CreateSharedMat(shader, cfg != null ? cfg.SpriteColor : Color.white);
            _sharedMetalMat = CreateSharedMat(shader, cfg != null ? cfg.MetalColor : new Color(0.15f, 0.15f, 0.18f, 1f));
            _sharedWindowMat = CreateSharedMat(shader, cfg != null ? cfg.WindowColor : new Color(0.2f, 0.9f, 1f, 0.9f));
            _sharedHeadlightMat = CreateSharedMat(shader, cfg != null ? cfg.HeadlightColor : new Color(1f, 0.95f, 0.5f, 1f));
            _sharedWhiteMat = CreateSharedMat(shader, cfg != null ? cfg.WhiteAccentColor : Color.white);
            _sharedTailMat = CreateSharedMat(shader, cfg != null ? cfg.TaillightColor : new Color(1f, 0.15f, 0.15f, 1f));
        }

        private static Material CreateSharedMat(Shader shader, Color color)
        {
            var mat = new Material(shader) { color = color, hideFlags = HideFlags.DontSave };
            mat.name = $"Shared_{color}";
            return mat;
        }

        /// <summary>Mevcut görseli renklendirmek için MaterialPropertyBlock kullan.</summary>
        public static void ApplyColorToRenderers(ColorType color, Renderer[] renderers, MaterialPropertyBlock mpb, float alpha = 1f)
        {
            if (renderers == null || mpb == null) return;
            Color vehicleColor = CellView.GetColor(color);
            mpb.SetColor("_Color", new Color(vehicleColor.r, vehicleColor.g, vehicleColor.b, alpha));
            for (int ri = 0; ri < renderers.Length; ri++)
            {
                if (renderers[ri] != null)
                    renderers[ri].SetPropertyBlock(mpb);
            }
        }

        /// <summary>
        /// Recycles all vehicle part primitives under the given visual root back to VehiclePartPool,
        /// then destroys the root GameObject itself.
        /// </summary>
        public static void RecycleVehicle(GameObject visualRoot)
        {
            if (visualRoot == null) return;
            VehiclePartPool.RecycleVehicle(visualRoot.transform);
            if (Application.isPlaying)
                Object.Destroy(visualRoot);
            else
                Object.DestroyImmediate(visualRoot);
        }

        /// <summary>
        /// Tren görselini config-driven procedural olarak oluşturur: Loco + Coupler1 + Wagon1 + Coupler2 + Wagon2.
        /// Tüm boyutlar VehicleVisualConfigAsset'ten okunur.
        /// </summary>
        public static List<Renderer> CreateTrain3D(GameObject root, ColorType color,
            out Transform loco, out Transform wagon1, out Transform wagon2,
            out Transform coupler1, out Transform coupler2)
        {
            var renderers = new List<Renderer>();
            loco = null; wagon1 = null; wagon2 = null; coupler1 = null; coupler2 = null;
            if (!Application.isPlaying) return renderers;
            EnsureAllSharedMaterialsCreated();

            if (_visualConfig == null)
            {
                throw new DataValidationException("VehicleVisualConfigAsset is null! VehicleVisualFactory requires VehicleVisualConfigAsset to be loaded.");
            }

            var vc = new VehicleVisualConfig.TrainConfig
            {
                BodySize = _visualConfig.TrainBodySize,
                CabinSize = _visualConfig.TrainCabinSize,
                CabinOffset = _visualConfig.TrainCabinOffset,
                WindshieldSize = _visualConfig.TrainWindshieldSize,
                WindshieldOffset = _visualConfig.TrainWindshieldOffset,
                HeadlightSize = _visualConfig.TrainHeadlightSize,
                HeadlightOffset = _visualConfig.TrainHeadlightOffset,
                StripeSize = _visualConfig.TrainStripeSize,
                StripeOffset = _visualConfig.TrainStripeOffset,
                WheelSize = _visualConfig.TrainWheelSize,
                WheelYOffset = _visualConfig.TrainWheelYOffset,
                WheelZOffset = _visualConfig.TrainWheelZOffset,
                LocoWheelPositions = _visualConfig.TrainLocoWheelPositions,
                CouplerSize = _visualConfig.TrainCouplerSize,
                WagonBodySize = _visualConfig.TrainWagon1BodySize,
                WagonWindowSize = _visualConfig.TrainWagon1WindowSize,
                WagonWindowOffsets = _visualConfig.TrainWagon1WindowOffsets,
                WagonWheelPositions = _visualConfig.TrainWagon1WheelPositions,
                Wagon2BodySize = _visualConfig.TrainWagon2BodySize,
                Wagon2WheelPositions = _visualConfig.TrainWagon2WheelPositions,
                TrailTime = _visualConfig.TrainTrailTime,
                TrailStartWidth = _visualConfig.TrainTrailStartWidth
            };

            // 1. LOCOMOTIVE ENGINE HEAD
            var locoObj = new GameObject("Locomotive");
            locoObj.transform.SetParent(root.transform, false);
            loco = locoObj.transform;

            var locoBody = VehiclePartPool.GetCube(loco);
            locoBody.name = "EngineBody";
            locoBody.transform.localScale = vc.BodySize;
            var rLoco = locoBody.GetComponent<Renderer>();
            if (rLoco != null) { rLoco.material = _sharedSpriteMat; rLoco.sortingOrder = 10; renderers.Add(rLoco); }

            var locoCab = VehiclePartPool.GetCube(loco);
            locoCab.name = "EngineCabin";
            locoCab.transform.localScale = vc.CabinSize;
            locoCab.transform.localPosition = vc.CabinOffset;
            var rCab = locoCab.GetComponent<Renderer>();
            if (rCab != null) { rCab.material = _sharedSpriteMat; rCab.sortingOrder = 10; renderers.Add(rCab); }

            var windshield = VehiclePartPool.GetCube(loco);
            windshield.name = "Windshield";
            windshield.transform.localScale = vc.WindshieldSize;
            windshield.transform.localPosition = vc.WindshieldOffset;
            var rWin = windshield.GetComponent<Renderer>();
            if (rWin != null) { rWin.material = _sharedWindowMat; rWin.sortingOrder = 10; renderers.Add(rWin); }

            var headlight = VehiclePartPool.GetCube(loco);
            headlight.name = "TrainHeadlight";
            headlight.transform.localScale = vc.HeadlightSize;
            headlight.transform.localPosition = vc.HeadlightOffset;
            var rHead = headlight.GetComponent<Renderer>();
            if (rHead != null) { rHead.material = _sharedHeadlightMat; rHead.sortingOrder = 10; renderers.Add(rHead); }

            var stripe = VehiclePartPool.GetCube(loco);
            stripe.name = "RoofStripe";
            stripe.transform.localScale = vc.StripeSize;
            stripe.transform.localPosition = vc.StripeOffset;
            var rStripe = stripe.GetComponent<Renderer>();
            if (rStripe != null) { rStripe.material = _sharedWhiteMat; rStripe.sortingOrder = 10; renderers.Add(rStripe); }

            foreach (var wheelPos in vc.LocoWheelPositions)
            {
                foreach (float side in new float[] { -1f, 1f })
                {
                    var wheel = VehiclePartPool.GetCylinder(loco);
                    wheel.name = "Wheel";
                    wheel.transform.localScale = vc.WheelSize;
                    wheel.transform.localPosition = new Vector3(wheelPos.x, side * vc.WheelYOffset, vc.WheelZOffset);
                    wheel.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                    var rWheel = wheel.GetComponent<Renderer>();
                    if (rWheel != null) { rWheel.material = _sharedMetalMat; rWheel.sortingOrder = 10; renderers.Add(rWheel); }
                }
            }

            // 2. COUPLER 1
            var c1Obj = VehiclePartPool.GetCube(root.transform);
            c1Obj.name = "Coupler1";
            c1Obj.transform.localScale = vc.CouplerSize;
            coupler1 = c1Obj.transform;
            var rC1 = c1Obj.GetComponent<Renderer>();
            if (rC1 != null) { rC1.material = _sharedMetalMat; rC1.sortingOrder = 10; renderers.Add(rC1); }

            // 3. WAGON 1
            var w1Obj = new GameObject("Wagon1");
            w1Obj.transform.SetParent(root.transform, false);
            wagon1 = w1Obj.transform;

            var w1Body = VehiclePartPool.GetCube(wagon1);
            w1Body.name = "WagonBody";
            w1Body.transform.localScale = vc.WagonBodySize;
            var rW1 = w1Body.GetComponent<Renderer>();
            if (rW1 != null) { rW1.material = _sharedSpriteMat; rW1.sortingOrder = 10; renderers.Add(rW1); }

            foreach (var winPos in vc.WagonWindowOffsets)
            {
                for (int side = -1; side <= 1; side += 2)
                {
                    var wWin = VehiclePartPool.GetCube(wagon1);
                    wWin.name = "WagonWindows";
                    wWin.transform.localScale = vc.WagonWindowSize;
                    wWin.transform.localPosition = new Vector3(winPos.x, side * winPos.y, winPos.z);
                    var rWWin = wWin.GetComponent<Renderer>();
                    if (rWWin != null) { rWWin.material = _sharedWindowMat; rWWin.sortingOrder = 10; renderers.Add(rWWin); }
                }
            }

            foreach (var wheelPos in vc.WagonWheelPositions)
            {
                foreach (float side in new float[] { -1f, 1f })
                {
                    var wheel = VehiclePartPool.GetCylinder(wagon1);
                    wheel.name = "Wheel";
                    wheel.transform.localScale = vc.WheelSize;
                    wheel.transform.localPosition = new Vector3(wheelPos.x, side * vc.WheelYOffset, vc.WheelZOffset);
                    wheel.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                    var rWheel = wheel.GetComponent<Renderer>();
                    if (rWheel != null) { rWheel.material = _sharedMetalMat; rWheel.sortingOrder = 10; renderers.Add(rWheel); }
                }
            }

            // 4. COUPLER 2
            var c2Obj = VehiclePartPool.GetCube(root.transform);
            c2Obj.name = "Coupler2";
            c2Obj.transform.localScale = vc.CouplerSize;
            coupler2 = c2Obj.transform;
            var rC2 = c2Obj.GetComponent<Renderer>();
            if (rC2 != null) { rC2.material = _sharedMetalMat; rC2.sortingOrder = 10; renderers.Add(rC2); }

            // 5. WAGON 2
            var w2Obj = new GameObject("Wagon2");
            w2Obj.transform.SetParent(root.transform, false);
            wagon2 = w2Obj.transform;

            var w2Body = VehiclePartPool.GetCube(wagon2);
            w2Body.name = "WagonBody";
            w2Body.transform.localScale = vc.Wagon2BodySize;
            var rW2 = w2Body.GetComponent<Renderer>();
            if (rW2 != null) { rW2.material = _sharedSpriteMat; rW2.sortingOrder = 10; renderers.Add(rW2); }

            foreach (var wheelPos in vc.Wagon2WheelPositions)
            {
                foreach (float side in new float[] { -1f, 1f })
                {
                    var wheel = VehiclePartPool.GetCylinder(wagon2);
                    wheel.name = "Wheel";
                    wheel.transform.localScale = vc.WheelSize;
                    wheel.transform.localPosition = new Vector3(wheelPos.x, side * vc.WheelYOffset, vc.WheelZOffset);
                    wheel.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                    var rWheel = wheel.GetComponent<Renderer>();
                    if (rWheel != null) { rWheel.material = _sharedMetalMat; rWheel.sortingOrder = 10; renderers.Add(rWheel); }
                }
            }

            // Trail renderer (neon glow)
            AddTrailRenderer(root, color, vc.TrailTime, vc.TrailStartWidth);

            return renderers;
        }

        /// <summary>
        /// Normal araç görselini config-driven procedural olarak oluşturur: Chassis + Cabin + Headlights + Taillights + Wheels.
        /// </summary>
        public static List<Renderer> CreateCar3D(GameObject root, ColorType color)
        {
            var renderers = new List<Renderer>();

            if (_visualConfig == null)
            {
                throw new DataValidationException("VehicleVisualConfigAsset is null! VehicleVisualFactory requires VehicleVisualConfigAsset to be loaded.");
            }

            if (!Application.isPlaying) return renderers;
            EnsureAllSharedMaterialsCreated();

            var vc = new VehicleVisualConfig.CarConfig
            {
                BodySize = _visualConfig.CarBodySize,
                CabinSize = _visualConfig.CarCabinSize,
                CabinOffset = _visualConfig.CarCabinOffset,
                HeadlightSize = _visualConfig.CarHeadlightSize,
                HeadlightOffset = _visualConfig.CarHeadlightOffset,
                TaillightSize = _visualConfig.CarTaillightSize,
                TaillightOffset = _visualConfig.CarTaillightOffset,
                WheelSize = _visualConfig.CarWheelSize,
                WheelXPositions = _visualConfig.CarWheelXPositions,
                WheelYPositions = _visualConfig.CarWheelYPositions,
                WheelZOffset = _visualConfig.CarWheelZOffset,
                TrailTime = _visualConfig.CarTrailTime,
                TrailStartWidth = _visualConfig.CarTrailStartWidth
            };

            bool is2DMode = _visualConfig.VisualMode == VehicleVisualMode.Mode2D_FlatSprite;

            // 1. Main Chassis / Body
            var body = VehiclePartPool.GetCube(root.transform);
            body.name = "Chassis";
            body.transform.localScale = is2DMode ? new Vector3(vc.BodySize.x, 0.02f, vc.BodySize.z) : vc.BodySize;
            var rBody = body.GetComponent<Renderer>();
            if (rBody != null)
            {
                rBody.material = _sharedSpriteMat;
                rBody.sortingOrder = 10;
                renderers.Add(rBody);
            }

            // 2. Cabin / Windshield
            var cabin = VehiclePartPool.GetCube(root.transform);
            cabin.name = "Cabin";
            cabin.transform.localScale = vc.CabinSize;
            cabin.transform.localPosition = vc.CabinOffset;
            var rCabin = cabin.GetComponent<Renderer>();
            if (rCabin != null)
            {
                rCabin.material = _sharedWindowMat;
                rCabin.sortingOrder = 10;
                renderers.Add(rCabin);
            }

            // 3. Headlights (Brighter at front bumper +X)
            var headL = VehiclePartPool.GetCube(root.transform);
            headL.name = "Headlights";
            headL.transform.localScale = vc.HeadlightSize;
            headL.transform.localPosition = vc.HeadlightOffset;
            var rHead = headL.GetComponent<Renderer>();
            if (rHead != null)
            {
                rHead.material = _sharedHeadlightMat;
                rHead.sortingOrder = 10;
                renderers.Add(rHead);
            }

            // 4. Taillights (Red at rear bumper -X)
            var tailL = VehiclePartPool.GetCube(root.transform);
            tailL.name = "Taillights";
            tailL.transform.localScale = vc.TaillightSize;
            tailL.transform.localPosition = vc.TaillightOffset;
            var rTail = tailL.GetComponent<Renderer>();
            if (rTail != null)
            {
                rTail.material = _sharedTailMat;
                rTail.sortingOrder = 10;
                renderers.Add(rTail);
            }

            // 5. 4 Wheels (Dark Cylinders)
            foreach (var wx in vc.WheelXPositions)
            {
                foreach (var wy in vc.WheelYPositions)
                {
                    var wheel = VehiclePartPool.GetCylinder(root.transform);
                    wheel.name = "Wheel";
                    wheel.transform.localScale = vc.WheelSize;
                    wheel.transform.localPosition = new Vector3(wx, wy, vc.WheelZOffset);
                    wheel.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                    var rWheel = wheel.GetComponent<Renderer>();
                    if (rWheel != null)
                    {
                        rWheel.material = _sharedMetalMat;
                        rWheel.sortingOrder = 10;
                        renderers.Add(rWheel);
                    }
                }
            }

            // Trail renderer (neon glow)
            AddTrailRenderer(root, color, vc.TrailTime, vc.TrailStartWidth);

            return renderers;
        }

        private static void AddTrailRenderer(GameObject root, ColorType color, float trailTime, float trailWidth)
        {
            var trail = root.AddComponent<TrailRenderer>();
            trail.time = trailTime;
            trail.startWidth = trailWidth;
            trail.endWidth = 0f;
            trail.numCornerVertices = 4;
            trail.material = _sharedSpriteMat;
            Color cVal = CellView.GetColor(color);
            trail.startColor = new Color(cVal.r, cVal.g, cVal.b, 0.45f);
            trail.endColor = new Color(cVal.r, cVal.g, cVal.b, 0f);
        }

        private static VehicleVisualConfig.TrainConfig CreateDefaultTrainConfig()
        {
            return new VehicleVisualConfig.TrainConfig
            {
                BodySize = new Vector3(0.38f, 0.22f, 0.18f),
                CabinSize = new Vector3(0.18f, 0.20f, 0.16f),
                CabinOffset = new Vector3(-0.06f, 0f, -0.10f),
                WindshieldSize = new Vector3(0.04f, 0.18f, 0.08f),
                WindshieldOffset = new Vector3(0.19f, 0f, -0.06f),
                HeadlightSize = new Vector3(0.05f, 0.08f, 0.06f),
                HeadlightOffset = new Vector3(0.20f, 0f, 0.02f),
                StripeSize = new Vector3(0.36f, 0.06f, 0.04f),
                StripeOffset = new Vector3(0f, 0f, -0.19f),
                WheelSize = new Vector3(0.07f, 0.02f, 0.07f),
                WheelYOffset = 0.09f,
                WheelZOffset = 0.05f,
                LocoWheelPositions = new List<Vector2Int> { new Vector2Int(10, 0), new Vector2Int(-10, 0) },
                CouplerSize = new Vector3(0.10f, 0.06f, 0.06f),
                WagonBodySize = new Vector3(0.34f, 0.20f, 0.16f),
                WagonWindowSize = new Vector3(0.24f, 0.02f, 0.06f),
                WagonWindowOffsets = new List<Vector3Int> { new Vector3Int(0, 10, -3) },
                WagonWheelPositions = new List<Vector2Int> { new Vector2Int(10, 0), new Vector2Int(-10, 0) },
                Wagon2BodySize = new Vector3(0.32f, 0.20f, 0.16f),
                Wagon2WheelPositions = new List<Vector2Int> { new Vector2Int(10, 0), new Vector2Int(-10, 0) },
                TrailTime = 0.55f,
                TrailStartWidth = 0.22f
            };
        }

        private static VehicleVisualConfig.CarConfig CreateDefaultCarConfig()
        {
            return new VehicleVisualConfig.CarConfig
            {
                BodySize = new Vector3(0.44f, 0.26f, 0.16f),
                CabinSize = new Vector3(0.24f, 0.20f, 0.12f),
                CabinOffset = new Vector3(-0.03f, 0f, -0.12f),
                HeadlightSize = new Vector3(0.04f, 0.20f, 0.06f),
                HeadlightOffset = new Vector3(0.22f, 0f, -0.02f),
                TaillightSize = new Vector3(0.04f, 0.20f, 0.05f),
                TaillightOffset = new Vector3(-0.22f, 0f, -0.02f),
                WheelSize = new Vector3(0.09f, 0.02f, 0.09f),
                WheelXPositions = new List<float> { -0.14f, 0.14f },
                WheelYPositions = new List<float> { -0.12f, 0.12f },
                WheelZOffset = 0.06f,
                TrailTime = 0.45f,
                TrailStartWidth = 0.18f
            };
        }
    }

    /// <summary>
    /// Araç görsel parametreleri için data-driven config struct'ları.
    /// Sıfır hardcode — tüm boyutlar ve konfigürasyonlar buradan gelir.
    /// </summary>
    public static class VehicleVisualConfig
    {
        [System.Serializable]
        public struct TrainConfig
        {
            public Vector3 BodySize;
            public Vector3 CabinSize;
            public Vector3 CabinOffset;
            public Vector3 WindshieldSize;
            public Vector3 WindshieldOffset;
            public Vector3 HeadlightSize;
            public Vector3 HeadlightOffset;
            public Vector3 StripeSize;
            public Vector3 StripeOffset;
            public Vector3 WheelSize;
            public float WheelYOffset;
            public float WheelZOffset;
            public List<Vector2Int> LocoWheelPositions;
            public Vector3 CouplerSize;
            public Vector3 WagonBodySize;
            public Vector3 WagonWindowSize;
            public List<Vector3Int> WagonWindowOffsets;
            public List<Vector2Int> WagonWheelPositions;
            public Vector3 Wagon2BodySize;
            public List<Vector2Int> Wagon2WheelPositions;
            public float TrailTime;
            public float TrailStartWidth;
        }

        [System.Serializable]
        public struct CarConfig
        {
            public Vector3 BodySize;
            public Vector3 CabinSize;
            public Vector3 CabinOffset;
            public Vector3 HeadlightSize;
            public Vector3 HeadlightOffset;
            public Vector3 TaillightSize;
            public Vector3 TaillightOffset;
            public Vector3 WheelSize;
            public List<float> WheelXPositions;
            public List<float> WheelYPositions;
            public float WheelZOffset;
            public float TrailTime;
            public float TrailStartWidth;
        }
    }
}
