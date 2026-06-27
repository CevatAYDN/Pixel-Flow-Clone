using UnityEngine;
using System.Collections.Generic;

namespace PixelFlow.Data
{
    [CreateAssetMenu(fileName = "LevelPack", menuName = "PixelFlow/LevelPack")]
    public class LevelPack : ScriptableObject
    {
        public string packName;
        public List<LevelData> levels = new List<LevelData>();
    }
}
