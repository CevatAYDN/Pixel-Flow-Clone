using UnityEngine;
using System.Collections.Generic;

namespace PixelFlow.Views
{
    /// <summary>Type of a pooled primitive part.</summary>
    public enum PartType { Cube, Cylinder }

    /// <summary>
    /// Marker component attached to each pooled vehicle part.
    /// Stores the part type so RecycleVehicle can correctly return
    /// cubes and cylinders to their respective stacks.
    /// </summary>
    public class VehiclePart : MonoBehaviour
    {
        public PartType Type;
    }

    /// <summary>
    /// Pre-allocated pool of cube and cylinder primitives for vehicle visuals.
    /// Eliminates runtime CreatePrimitive/Destroy GC pressure from vehicle spawning.
    /// 
    /// Usage:
    ///   var cube = VehiclePartPool.GetCube(parent);
    ///   cube.transform.localScale = ...;
    ///   cube.GetComponent<Renderer>().material = ...;
    ///   
    ///   VehiclePartPool.RecycleVehicle(vehicleRoot);
    /// </summary>
    public static class VehiclePartPool
    {
        private static readonly Stack<GameObject> _cubes = new Stack<GameObject>(512);
        private static readonly Stack<GameObject> _cylinders = new Stack<GameObject>(256);
        private static Transform _poolRoot;
        private static bool _initialized;
        private static readonly List<GameObject> _recycleList = new List<GameObject>(128);

        private const int PreAllocCubes = 512;
        private const int PreAllocCylinders = 256;

        /// <summary>
        /// Ensures the pool is initialized with pre-allocated primitives.
        /// Safe to call multiple times — only allocates once.
        /// </summary>
        public static void Initialize()
        {
            if (_initialized) return;
            _initialized = true;

            var rootObj = new GameObject("[VehiclePartPool]");
            rootObj.hideFlags = HideFlags.DontSave;
            _poolRoot = rootObj.transform;

            for (int i = 0; i < PreAllocCubes; i++)
                _cubes.Push(CreatePart(PrimitiveType.Cube));

            for (int i = 0; i < PreAllocCylinders; i++)
                _cylinders.Push(CreatePart(PrimitiveType.Cylinder));

            Nexus.Core.Services.NexusLog.Info("VehiclePartPool", "Initialize", "?", "Initialized pool with " + PreAllocCubes + " cubes and " + PreAllocCylinders + " cylinders.");
        }

        private static GameObject CreatePart(PrimitiveType type)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = type == PrimitiveType.Cylinder ? "Cylinder_pooled" : "Cube_pooled";
            go.transform.SetParent(_poolRoot, false);
            go.SetActive(false);

            // Remove collider — visual-only parts don't need physics
            var collider = go.GetComponent<Collider>();
            if (collider != null)
                Object.DestroyImmediate(collider);

            // Add marker component with type for reliable recycling
            var vp = go.AddComponent<VehiclePart>();
            vp.Type = type == PrimitiveType.Cylinder ? PartType.Cylinder : PartType.Cube;

            return go;
        }

        /// <summary>Returns a cube from the pool, parented to the specified transform and activated.</summary>
        public static GameObject GetCube(Transform parent)
        {
            if (!_initialized) Initialize();

            GameObject go;
            if (_cubes.Count > 0)
                go = _cubes.Pop();
            else
                go = CreatePart(PrimitiveType.Cube);

            go.transform.SetParent(parent, false);
            go.SetActive(true);
            return go;
        }

        /// <summary>Returns a cylinder from the pool, parented to the specified transform and activated.</summary>
        public static GameObject GetCylinder(Transform parent)
        {
            if (!_initialized) Initialize();

            GameObject go;
            if (_cylinders.Count > 0)
                go = _cylinders.Pop();
            else
                go = CreatePart(PrimitiveType.Cylinder);

            go.transform.SetParent(parent, false);
            go.SetActive(true);
            return go;
        }

        /// <summary>
        /// Recursively finds all VehiclePart components under the given root transform
        /// and returns them to the pool. The root GameObject itself is NOT destroyed here
        /// — the caller should destroy it after calling this method.
        /// </summary>
        public static void RecycleVehicle(Transform root)
        {
            if (root == null) return;
            if (!_initialized) Initialize();
            if (_poolRoot == null) return;

            _recycleList.Clear();
            CollectParts(root, _recycleList);

            int recycledCount = _recycleList.Count;
            for (int i = 0; i < recycledCount; i++)
            {
                var part = _recycleList[i];
                if (part == null) continue;

                part.transform.SetParent(_poolRoot, false);
                part.SetActive(false);

                var vp = part.GetComponent<VehiclePart>();
                if (vp != null && vp.Type == PartType.Cylinder)
                    _cylinders.Push(part);
                else
                    _cubes.Push(part);
            }
            Nexus.Core.Services.NexusLog.Info("VehiclePartPool", "RecycleVehicle", "?", "Recycled vehicle root " + root.name + " containing " + recycledCount + " parts.");
            _recycleList.Clear();
        }

        private static void CollectParts(Transform t, List<GameObject> results)
        {
            // Iterate in reverse to handle reparenting safely
            for (int i = t.childCount - 1; i >= 0; i--)
            {
                var child = t.GetChild(i);
                CollectParts(child, results);

                if (child.GetComponent<VehiclePart>() != null)
                    results.Add(child.gameObject);
            }
        }

        /// <summary>Clears the pool and destroys all cached primitives.</summary>
        public static void Dispose()
        {
            if (_poolRoot != null)
            {
                if (Application.isPlaying)
                    Object.Destroy(_poolRoot.gameObject);
                else
                    Object.DestroyImmediate(_poolRoot.gameObject);
                _poolRoot = null;
            }
            _cubes.Clear();
            _cylinders.Clear();
            _initialized = false;
        }
    }
}
