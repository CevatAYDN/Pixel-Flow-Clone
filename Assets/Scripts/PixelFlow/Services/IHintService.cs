using System.Collections.Generic;
using System.Linq;
using PixelFlow.Data;
using PixelFlow.Models;
using UnityEngine;

namespace PixelFlow.Services
{

    public interface IHintService
    {
        /// <summary>
        /// Belirtilen renk için sonraki adım(lar)ı döndürür.
        /// steps=1: sadece 1 hücre, steps=-1: tam çözüm.
        /// </summary>
        List<Vector2Int> GetHint(LevelData level, ColorType color, int steps = 1);

        /// <summary>
        /// Çözülmemiş ilk rengi bulur ve onun için hint döndürür.
        /// </summary>
        List<Vector2Int> GetNextUnsolvedHint(LevelData level, IGridModel grid, int steps = 1);
    }
}