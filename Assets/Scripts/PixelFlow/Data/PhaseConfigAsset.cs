using UnityEngine;
using System.Collections.Generic;

namespace PixelFlow.Data
{
    /// <summary>
    /// Tüm fazların bir arada tutulduğu konteyner ScriptableObject.
    /// GameContext'te referans edilir; LevelProgressionService bunu okuyup
    /// seviye progression'ını yönetir.
    /// </summary>
    [CreateAssetMenu(
        fileName = "PhaseConfig.asset",
        menuName = "PixelFlow/Phase Configuration")]
    public class PhaseConfigAsset : ScriptableObject
    {
        [Tooltip("Faz 1 (Seviye 1-12)")]
        public PhaseDefinitionAsset Phase1;

        [Tooltip("Faz 2 (Seviye 13-28)")]
        public PhaseDefinitionAsset Phase2;

        [Tooltip("Faz 3 (Seviye 29-45)")]
        public PhaseDefinitionAsset Phase3;

        [Tooltip("Faz 4 (Seviye 46-60+)")]
        public PhaseDefinitionAsset Phase4;

        public IEnumerable<PhaseDefinitionAsset> AllPhases
        {
            get
            {
                if (Phase1 != null) yield return Phase1;
                if (Phase2 != null) yield return Phase2;
                if (Phase3 != null) yield return Phase3;
                if (Phase4 != null) yield return Phase4;
            }
        }

        /// <summary>
        /// Seviye indeksine göre hangi fazda olduğumuzu bulur.
        /// </summary>
        public PhaseDefinitionAsset GetPhaseForLevel(int levelIndex)
        {
            foreach (var phase in AllPhases)
            {
                if (phase.ContainsLevel(levelIndex))
                    return phase;
            }
            return Phase4; // Fallback: 60+ seviyeler Faz 4
        }

        public PhaseDefinition[] ToStructArray()
        {
            var list = new List<PhaseDefinition>();
            foreach (var asset in AllPhases)
            {
                if (asset != null)
                    list.Add(asset.ToStruct());
            }
            return list.ToArray();
        }
    }
}
