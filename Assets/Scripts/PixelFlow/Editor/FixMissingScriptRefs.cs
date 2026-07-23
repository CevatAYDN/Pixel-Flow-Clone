using UnityEditor;
using UnityEngine;
using System.Linq;

namespace PixelFlow.Editor
{
    /// <summary>
    /// Cleans missing MonoBehaviour script references from LevelData assets.
    /// Run via: Tools → PixelFlow → Fix Missing Script References
    /// </summary>
    public static class FixMissingScriptRefs
    {
        [MenuItem("Tools/PixelFlow/Fix Missing Script References")]
        private static void FixMissingRefs()
        {
            var allLevelAssets = AssetDatabase.FindAssets("t:ScriptableObject", new[] { "Assets/Resources/Levels" })
                .Select(guid => AssetDatabase.LoadAssetAtPath<ScriptableObject>(AssetDatabase.GUIDToAssetPath(guid)))
                .Where(so => so != null)
                .ToArray();

            int fixedCount = 0;

            foreach (var asset in allLevelAssets)
            {
                string path = AssetDatabase.GetAssetPath(asset);
                if (string.IsNullOrEmpty(path)) continue;

                // Use SerializedObject to iterate and clean missing refs
                var so = new SerializedObject(asset);
                var prop = so.GetIterator();

                bool modified = false;
                while (prop.NextVisible(true))
                {
                    if (prop.propertyType == SerializedPropertyType.ObjectReference &&
                        prop.objectReferenceValue == null)
                    {
                        // Check if this was a previously assigned reference (non-default entity ID)
                        var entityId = prop.objectReferenceEntityIdValue;
                        if (!entityId.Equals(default(GlobalObjectId)))
                        {
                            // This is a missing script reference (non-null instance ID but null reference)
                            prop.objectReferenceValue = null;
                            modified = true;
                            fixedCount++;
                        }
                    }
                }

                if (modified)
                {
                    so.ApplyModifiedPropertiesWithoutUndo();
                    EditorUtility.SetDirty(asset);
                    Debug.Log($"[PixelFlow.FixMissingScriptRefs] Cleaned missing refs in: {path}");
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[PixelFlow.FixMissingScriptRefs] ✅ Complete. Cleaned {fixedCount} missing references across {allLevelAssets.Length} assets.");
        }
    }
}
