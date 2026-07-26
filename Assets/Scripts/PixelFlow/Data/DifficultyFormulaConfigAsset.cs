using UnityEngine;

namespace PixelFlow.Data
{
    /// <summary>
    /// game_plan.md §2.2: Centralized difficulty formula config ScriptableObject.
    /// Eliminates hardcoded magic weights across ProceduralLevelGenerator and LevelValidator.
    /// </summary>
    [CreateAssetMenu(fileName = "DifficultyFormulaConfig", menuName = "PixelFlow/Config/DifficultyFormulaConfig")]
    public class DifficultyFormulaConfigAsset : ScriptableObject
    {
        [Header("Difficulty Formula Weights")]
        [Tooltip("Weight multiplied by color count")]
        public int ColorWeight = 10;

        [Tooltip("Weight multiplied by intersection count")]
        public int IntersectionWeight = 5;

        [Tooltip("Weight multiplied by obstacle count")]
        public int ObstacleWeight = 3;

        [Tooltip("Weight subtracted for viaduct count")]
        public int ViaductWeight = 4;

        public int CalculateDifficulty(int colors, int intersections, int obstacles, int viaducts)
        {
            return (colors * ColorWeight) + (intersections * IntersectionWeight) + (obstacles * ObstacleWeight) - (viaducts * ViaductWeight);
        }
    }
}
