using System.Collections.Generic;
using UnityEngine;
using PixelFlow.Data;
using PixelFlow.Models;

namespace PixelFlow.Views
{
    /// <summary>
    /// Procedural araç/tren görsel üretiminden sorumlu static fabrika.
    /// Shared material'lar ve MaterialPropertyBlock ile GC alloc minimize edilir.
    /// VehicleSimulator'dan ayrıştırıldı (1247 satır → ~450 satır).
    /// </summary>
    public static class VehicleVisualFactory
    {
        // Shared materials for vehicle visuals — prevents new Material per-primitive (saved ~20+ allocs/vehicle)
        private static Material _sharedSpriteMat;
        private static Material _sharedMetalMat;
        private static Material _sharedWindowMat;
        private static Material _sharedHeadlightMat;
        private static Material _sharedWhiteMat;
        private static Material _sharedTailMat;

        private static void EnsureAllSharedMaterialsCreated()
        {
            if (_sharedSpriteMat != null) return;
            var shader = Shader.Find("Sprites/Default") ?? Shader.Find("Standard");
            _sharedSpriteMat = CreateSharedMat(shader, Color.white);
            _sharedMetalMat = CreateSharedMat(shader, new Color(0.15f, 0.15f, 0.18f, 1f));
            _sharedWindowMat = CreateSharedMat(shader, new Color(0.2f, 0.9f, 1f, 0.9f));
            _sharedHeadlightMat = CreateSharedMat(shader, new Color(1f, 0.95f, 0.5f, 1f));
            _sharedWhiteMat = CreateSharedMat(shader, Color.white);
            _sharedTailMat = CreateSharedMat(shader, new Color(1f, 0.15f, 0.15f, 1f));
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
        /// Tren görselini procedural olarak oluşturur: Loco + Coupler1 + Wagon1 + Coupler2 + Wagon2.
        /// Her parça ayrı Transform olarak döndürülür (VehicleSimulator.UpdateMovement'te kullanılır).
        /// </summary>
        public static List<Renderer> CreateTrain3D(GameObject root, ColorType color,
            out Transform loco, out Transform wagon1, out Transform wagon2,
            out Transform coupler1, out Transform coupler2)
        {
            var renderers = new List<Renderer>();
            loco = null; wagon1 = null; wagon2 = null; coupler1 = null; coupler2 = null;
            if (!Application.isPlaying) return renderers;
            EnsureAllSharedMaterialsCreated();

            // 1. LOCOMOTIVE ENGINE HEAD
            var locoObj = new GameObject("Locomotive");
            locoObj.transform.SetParent(root.transform, false);
            loco = locoObj.transform;

            var locoBody = GameObject.CreatePrimitive(PrimitiveType.Cube);
            locoBody.name = "EngineBody";
            locoBody.transform.SetParent(loco, false);
            locoBody.transform.localScale = new Vector3(0.38f, 0.22f, 0.18f);
            var rLoco = locoBody.GetComponent<Renderer>();
            if (rLoco != null) { rLoco.material = _sharedSpriteMat; rLoco.sortingOrder = 10; renderers.Add(rLoco); }

            var locoCab = GameObject.CreatePrimitive(PrimitiveType.Cube);
            locoCab.name = "EngineCabin";
            locoCab.transform.SetParent(loco, false);
            locoCab.transform.localScale = new Vector3(0.18f, 0.20f, 0.16f);
            locoCab.transform.localPosition = new Vector3(-0.06f, 0f, -0.10f);
            var rCab = locoCab.GetComponent<Renderer>();
            if (rCab != null) { rCab.material = _sharedSpriteMat; rCab.sortingOrder = 10; renderers.Add(rCab); }

            var windshield = GameObject.CreatePrimitive(PrimitiveType.Cube);
            windshield.name = "Windshield";
            windshield.transform.SetParent(loco, false);
            windshield.transform.localScale = new Vector3(0.04f, 0.18f, 0.08f);
            windshield.transform.localPosition = new Vector3(0.19f, 0f, -0.06f);
            var rWin = windshield.GetComponent<Renderer>();
            if (rWin != null) { rWin.material = _sharedWindowMat; rWin.sortingOrder = 10; renderers.Add(rWin); }

            var headlight = GameObject.CreatePrimitive(PrimitiveType.Cube);
            headlight.name = "TrainHeadlight";
            headlight.transform.SetParent(loco, false);
            headlight.transform.localScale = new Vector3(0.05f, 0.08f, 0.06f);
            headlight.transform.localPosition = new Vector3(0.20f, 0f, 0.02f);
            var rHead = headlight.GetComponent<Renderer>();
            if (rHead != null) { rHead.material = _sharedHeadlightMat; rHead.sortingOrder = 10; renderers.Add(rHead); }

            var stripe = GameObject.CreatePrimitive(PrimitiveType.Cube);
            stripe.name = "RoofStripe";
            stripe.transform.SetParent(loco, false);
            stripe.transform.localScale = new Vector3(0.36f, 0.06f, 0.04f);
            stripe.transform.localPosition = new Vector3(0f, 0f, -0.19f);
            var rStripe = stripe.GetComponent<Renderer>();
            if (rStripe != null) { rStripe.material = _sharedWhiteMat; rStripe.sortingOrder = 10; renderers.Add(rStripe); }

            float[] locoWheelX = { 0.10f, -0.10f };
            foreach (float x in locoWheelX)
            {
                for (int side = -1; side <= 1; side += 2)
                {
                    var wheel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    wheel.name = "Wheel";
                    wheel.transform.SetParent(loco, false);
                    wheel.transform.localScale = new Vector3(0.07f, 0.02f, 0.07f);
                    wheel.transform.localPosition = new Vector3(x, side * 0.09f, 0.05f);
                    wheel.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                    var rWheel = wheel.GetComponent<Renderer>();
                    if (rWheel != null) { rWheel.material = _sharedMetalMat; rWheel.sortingOrder = 10; renderers.Add(rWheel); }
                }
            }

            // 2. COUPLER 1
            var c1Obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            c1Obj.name = "Coupler1";
            c1Obj.transform.SetParent(root.transform, false);
            c1Obj.transform.localScale = new Vector3(0.10f, 0.06f, 0.06f);
            coupler1 = c1Obj.transform;
            var rC1 = c1Obj.GetComponent<Renderer>();
            if (rC1 != null) { rC1.material = _sharedMetalMat; rC1.sortingOrder = 10; renderers.Add(rC1); }

            // 3. WAGON 1
            var w1Obj = new GameObject("Wagon1");
            w1Obj.transform.SetParent(root.transform, false);
            wagon1 = w1Obj.transform;

            var w1Body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            w1Body.name = "WagonBody";
            w1Body.transform.SetParent(wagon1, false);
            w1Body.transform.localScale = new Vector3(0.34f, 0.20f, 0.16f);
            var rW1 = w1Body.GetComponent<Renderer>();
            if (rW1 != null) { rW1.material = _sharedSpriteMat; rW1.sortingOrder = 10; renderers.Add(rW1); }

            for (int side = -1; side <= 1; side += 2)
            {
                var wWin = GameObject.CreatePrimitive(PrimitiveType.Cube);
                wWin.name = "WagonWindows";
                wWin.transform.SetParent(wagon1, false);
                wWin.transform.localScale = new Vector3(0.24f, 0.02f, 0.06f);
                wWin.transform.localPosition = new Vector3(0f, side * 0.10f, -0.03f);
                var rWWin = wWin.GetComponent<Renderer>();
                if (rWWin != null) { rWWin.material = _sharedWindowMat; rWWin.sortingOrder = 10; renderers.Add(rWWin); }
            }

            foreach (float x in locoWheelX)
            {
                for (int side = -1; side <= 1; side += 2)
                {
                    var wheel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    wheel.name = "Wheel";
                    wheel.transform.SetParent(wagon1, false);
                    wheel.transform.localScale = new Vector3(0.07f, 0.02f, 0.07f);
                    wheel.transform.localPosition = new Vector3(x, side * 0.09f, 0.05f);
                    wheel.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                    var rWheel = wheel.GetComponent<Renderer>();
                    if (rWheel != null) { rWheel.material = _sharedMetalMat; rWheel.sortingOrder = 10; renderers.Add(rWheel); }
                }
            }

            // 4. COUPLER 2
            var c2Obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            c2Obj.name = "Coupler2";
            c2Obj.transform.SetParent(root.transform, false);
            c2Obj.transform.localScale = new Vector3(0.10f, 0.06f, 0.06f);
            coupler2 = c2Obj.transform;
            var rC2 = c2Obj.GetComponent<Renderer>();
            if (rC2 != null) { rC2.material = _sharedMetalMat; rC2.sortingOrder = 10; renderers.Add(rC2); }

            // 5. WAGON 2
            var w2Obj = new GameObject("Wagon2");
            w2Obj.transform.SetParent(root.transform, false);
            wagon2 = w2Obj.transform;

            var w2Body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            w2Body.name = "WagonBody";
            w2Body.transform.SetParent(wagon2, false);
            w2Body.transform.localScale = new Vector3(0.32f, 0.20f, 0.16f);
            var rW2 = w2Body.GetComponent<Renderer>();
            if (rW2 != null) { rW2.material = _sharedSpriteMat; rW2.sortingOrder = 10; renderers.Add(rW2); }

            foreach (float x in locoWheelX)
            {
                for (int side = -1; side <= 1; side += 2)
                {
                    var wheel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    wheel.name = "Wheel";
                    wheel.transform.SetParent(wagon2, false);
                    wheel.transform.localScale = new Vector3(0.07f, 0.02f, 0.07f);
                    wheel.transform.localPosition = new Vector3(x, side * 0.09f, 0.05f);
                    wheel.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                    var rWheel = wheel.GetComponent<Renderer>();
                    if (rWheel != null) { rWheel.material = _sharedMetalMat; rWheel.sortingOrder = 10; renderers.Add(rWheel); }
                }
            }

            // GDD §16.2 / Hissiyat: Add glowing neon trail to train
            var trail = root.AddComponent<TrailRenderer>();
            trail.time = 0.55f;
            trail.startWidth = 0.22f;
            trail.endWidth = 0f;
            trail.numCornerVertices = 4;
            trail.material = _sharedSpriteMat;
            Color cVal = CellView.GetColor(color);
            trail.startColor = new Color(cVal.r, cVal.g, cVal.b, 0.45f);
            trail.endColor = new Color(cVal.r, cVal.g, cVal.b, 0f);

            return renderers;
        }

        /// <summary>
        /// Normal araç görselini procedural olarak oluşturur: Chassis + Cabin + Headlights + Taillights + Wheels.
        /// </summary>
        public static List<Renderer> CreateCar3D(GameObject root, ColorType color)
        {
            var renderers = new List<Renderer>();
            if (!Application.isPlaying) return renderers;
            EnsureAllSharedMaterialsCreated();

            // 1. Main Chassis / Body
            var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name = "Chassis";
            body.transform.SetParent(root.transform, false);
            body.transform.localScale = new Vector3(0.44f, 0.26f, 0.16f);
            var rBody = body.GetComponent<Renderer>();
            if (rBody != null)
            {
                rBody.material = _sharedSpriteMat;
                rBody.sortingOrder = 10;
                renderers.Add(rBody);
            }

            // 2. Cabin / Windshield
            var cabin = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cabin.name = "Cabin";
            cabin.transform.SetParent(root.transform, false);
            cabin.transform.localScale = new Vector3(0.24f, 0.20f, 0.12f);
            cabin.transform.localPosition = new Vector3(-0.03f, 0f, -0.12f);
            var rCabin = cabin.GetComponent<Renderer>();
            if (rCabin != null)
            {
                rCabin.material = _sharedWindowMat;
                rCabin.sortingOrder = 10;
                renderers.Add(rCabin);
            }

            // 3. Headlights (Bright Cyan/White at front bumper +X)
            var headL = GameObject.CreatePrimitive(PrimitiveType.Cube);
            headL.name = "Headlights";
            headL.transform.SetParent(root.transform, false);
            headL.transform.localScale = new Vector3(0.04f, 0.20f, 0.06f);
            headL.transform.localPosition = new Vector3(0.22f, 0f, -0.02f);
            var rHead = headL.GetComponent<Renderer>();
            if (rHead != null)
            {
                rHead.material = _sharedHeadlightMat;
                rHead.sortingOrder = 10;
                renderers.Add(rHead);
            }

            // 4. Taillights (Red at rear bumper -X)
            var tailL = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tailL.name = "Taillights";
            tailL.transform.SetParent(root.transform, false);
            tailL.transform.localScale = new Vector3(0.04f, 0.20f, 0.05f);
            tailL.transform.localPosition = new Vector3(-0.22f, 0f, -0.02f);
            var rTail = tailL.GetComponent<Renderer>();
            if (rTail != null)
            {
                rTail.material = _sharedTailMat;
                rTail.sortingOrder = 10;
                renderers.Add(rTail);
            }

            // 5. 4 Wheels (Dark Cylinders)
            float[] wx = { -0.14f, 0.14f };
            float[] wy = { -0.12f, 0.12f };
            foreach (float x in wx)
            {
                foreach (float y in wy)
                {
                    var wheel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    wheel.name = "Wheel";
                    wheel.transform.SetParent(root.transform, false);
                    wheel.transform.localScale = new Vector3(0.09f, 0.02f, 0.09f);
                    wheel.transform.localPosition = new Vector3(x, y, 0.06f);
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

            // GDD §16.2 / Hissiyat: Add glowing neon trail to vehicle
            var trail = root.AddComponent<TrailRenderer>();
            trail.time = 0.45f;
            trail.startWidth = 0.18f;
            trail.endWidth = 0f;
            trail.numCornerVertices = 4;
            trail.material = _sharedSpriteMat;
            Color cVal = CellView.GetColor(color);
            trail.startColor = new Color(cVal.r, cVal.g, cVal.b, 0.45f);
            trail.endColor = new Color(cVal.r, cVal.g, cVal.b, 0f);

            return renderers;
        }
    }
}
