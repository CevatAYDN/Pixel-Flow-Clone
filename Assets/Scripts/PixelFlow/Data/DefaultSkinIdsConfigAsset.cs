using UnityEngine;

namespace PixelFlow.Data
{
    /// <summary>
    /// game_plan.md §2.2: Centralized Default/Fallback Skin IDs config ScriptableObject.
    /// Replaces hardcoded string literals like "skin_default" across vehicle simulators and inventory models.
    /// </summary>
    [CreateAssetMenu(fileName = "DefaultSkinIdsConfig", menuName = "PixelFlow/Config/DefaultSkinIdsConfig")]
    public class DefaultSkinIdsConfigAsset : ScriptableObject
    {
        [Header("Default Vehicle & Stop Skins")]
        public string DefaultVehicleSkinId = "skin_default";
        public string DefaultStopSkinId = "stop_default";
    }
}
