using UnityEditor;
using UnityEngine;
using System;
using System.Reflection;
using System.Collections.Generic;
using Object = UnityEngine.Object;

namespace PixelFlow.Editor
{
    /// <summary>
    /// Auto-reference Editor aracı — View alt sınıflarındaki tüm [SerializeField]
    /// GameObject, Component, RectTransform, Transform alanlarını child GameObject'lerde
    /// bularak otomatik doldurur. Hem Reflection hem SerializedObject ile çalışır.
    /// 
    /// Kullanım:
    ///   - Hierarchy'de View GameObject'ine sağ tıklayın → "Pixel Flow / Auto-Reference This View"
    ///   - Tüm sahne için: "Pixel Flow / Auto-Reference All Views In Scene"
    /// </summary>
    public static class AutoReferenceEditor
    {
        private const BindingFlags FieldFlags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

        [MenuItem("GameObject/Pixel Flow/Auto-Reference This View", false, 10)]
        private static void AutoReferenceSelected()
        {
            var selected = Selection.activeGameObject;
            if (selected == null)
            {
                Debug.LogWarning("[AutoRef] Please select a GameObject.");
                return;
            }

            Undo.RegisterFullObjectHierarchyUndo(selected, "Auto-Reference");
            int totalFixed = ProcessGameObject(selected, true);
            if (totalFixed > 0)
                Debug.Log($"[AutoRef] {selected.name}: {totalFixed} references filled.", selected);
            else
                Debug.Log($"[AutoRef] {selected.name}: No empty references found.", selected);
        }

        [MenuItem("GameObject/Pixel Flow/Auto-Reference This View", true)]
        private static bool AutoReferenceSelectedValidate()
        {
            return Selection.activeGameObject != null;
        }

        [MenuItem("GameObject/Pixel Flow/Auto-Reference All Views In Scene", false, 11)]
        private static void AutoReferenceAllViews()
        {
            var allViews = Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include);
            int totalFixed = 0;
            int viewCount = 0;
            var processed = new HashSet<GameObject>();

            foreach (var mb in allViews)
            {
                if (mb == null || processed.Contains(mb.gameObject)) continue;
                processed.Add(mb.gameObject);
                int fixedCount = AutoReferenceComponent(mb, mb.gameObject);
                if (fixedCount > 0)
                {
                    totalFixed += fixedCount;
                    viewCount++;
                }
            }

            Debug.Log($"[AutoRef] {viewCount} View components with {totalFixed} total references filled across scene.");
        }

        /// <summary>
        /// Programmatic entry point for SetupScene.
        /// </summary>
        public static void AutoReferenceAllViewsInScene()
        {
            AutoReferenceAllViews();
        }

        private static int ProcessGameObject(GameObject go, bool markDirty)
        {
            int totalFixed = 0;
            var monos = go.GetComponents<MonoBehaviour>();
            foreach (var mb in monos)
            {
                if (mb == null) continue;
                totalFixed += AutoReferenceComponent(mb, go);
            }
            if (totalFixed > 0 && markDirty)
                EditorUtility.SetDirty(go);
            return totalFixed;
        }

        private static int AutoReferenceComponent(MonoBehaviour mb, GameObject root)
        {
            int fixedCount = 0;
            var type = mb.GetType();
            var fields = type.GetFields(FieldFlags);
            var serializableFields = new List<FieldInfo>();

            foreach (var field in fields)
            {
                if (Attribute.IsDefined(field, typeof(SerializeField)) ||
                    (field.IsPublic && !field.IsInitOnly && !field.IsStatic))
                {
                    serializableFields.Add(field);
                }
            }

            // Check each serialized field for empty references
            foreach (var field in serializableFields)
            {
                var currentValue = field.GetValue(mb);
                if (currentValue is Object unityObj && unityObj != null)
                    continue; // Already assigned

                var fieldType = field.FieldType;
                string fieldName = field.Name;

                Object resolved = ResolveReference(root.transform, fieldType, fieldName);
                if (resolved != null)
                {
                    field.SetValue(mb, resolved);
                    fixedCount++;
                }
            }

            // Also scan via SerializedObject for fields Reflection might have missed
            // (e.g. base class [SerializeField] fields)
            using (var so = new SerializedObject(mb))
            {
                var prop = so.GetIterator();
                if (prop.NextVisible(true))
                {
                    do
                    {
                        if (prop.propertyType != SerializedPropertyType.ObjectReference) continue;
                        if (prop.objectReferenceValue != null) continue;
                        if (prop.name.StartsWith("m_") || prop.name == "script") continue;

                        string cleanName = prop.name;
                        Object resolved = ResolveReference(root.transform, null, cleanName);
                        if (resolved != null)
                        {
                            prop.objectReferenceValue = resolved;
                            fixedCount++;
                        }
                    }
                    while (prop.NextVisible(false));
                }
                if (fixedCount > 0) so.ApplyModifiedPropertiesWithoutUndo();
            }

            return fixedCount;
        }

        private static Object ResolveReference(Transform root, Type fieldType, string fieldName)
        {
            string cleanName = fieldName.TrimStart('_');

            // GameObject fields
            if (fieldType == typeof(GameObject))
            {
                var go = FindGameObjectStrict(root, cleanName);
                if (go != null) return go;
            }

            // RectTransform (special case)
            if (fieldType == typeof(RectTransform) || fieldType == typeof(Transform))
            {
                var found = FindGameObjectStrict(root, cleanName);
                if (found != null)
                {
                    var comp = found.GetComponent(fieldType);
                    if (comp != null) return comp;
                }
            }

            // Component fields: try GetComponentInChildren
            if (fieldType != null && typeof(Component).IsAssignableFrom(fieldType))
            {
                var comp = root.GetComponentInChildren(fieldType, true);
                if (comp != null) return comp;
            }

            // If we don't know the type (SerializedObject path), try common types by name
            if (fieldType == null)
            {
                var go = FindGameObjectStrict(root, cleanName);
                if (go != null)
                {
                    var rect = go.GetComponent<RectTransform>();
                    if (rect != null) return rect;
                    return go;
                }

                var allComps = root.GetComponentsInChildren<Component>(true);
                foreach (var c in allComps)
                {
                    if (c == null) continue;
                    string cName = c.name;
                    string cClean = cName.TrimStart('_');
                    if (cClean.Equals(cleanName, StringComparison.OrdinalIgnoreCase) ||
                        Normalize(cClean).Equals(Normalize(cleanName), StringComparison.OrdinalIgnoreCase))
                    {
                        return c;
                    }
                }
            }

            return null;
        }

        private static GameObject FindGameObjectStrict(Transform parent, string fieldName)
        {
            string cleanName = fieldName.TrimStart('_');

            // Direct children first (exact match)
            foreach (Transform child in parent)
            {
                if (child.name.Equals(fieldName, StringComparison.OrdinalIgnoreCase) ||
                    child.name.Equals(cleanName, StringComparison.OrdinalIgnoreCase))
                    return child.gameObject;
            }

            // Direct children (normalized match)
            foreach (Transform child in parent)
            {
                string childNorm = Normalize(child.name);
                string fieldNorm = Normalize(cleanName);
                if (childNorm.Equals(fieldNorm, StringComparison.OrdinalIgnoreCase))
                    return child.gameObject;
            }

            // Recursive deep search
            foreach (Transform child in parent)
            {
                var found = FindGameObjectStrict(child, fieldName);
                if (found != null) return found;
            }

            return null;
        }

        private static string Normalize(string s)
        {
            return s.Replace(" ", "").Replace("_", "").Replace("-", "");
        }
    }
}
