using System.Collections.Generic;
using PixelFlow.Data;
using UnityEngine;

namespace PixelFlow.Services
{
    /// <summary>
    /// Bir level'ın node'larına göre path çözümü üreten servis.
    /// Editor auto-solver'ın runtime versiyonu.
    /// </summary>
    public interface IPathSolver
    {
        /// <summary>
        /// Verilen level için tüm renklerin path'lerini çözer.
        /// </summary>
        bool Solve(LevelData level, out Dictionary<ColorType, List<Vector2Int>> solutions);

        /// <summary>
        /// Tek bir renk için kısmi çözüm döndürür (partial hint).
        /// Steps sayısı kadar adım ilerletir.
        /// </summary>
        bool SolvePartial(LevelData level, ColorType color, int steps, out List<Vector2Int> partialPath);
    }
}
