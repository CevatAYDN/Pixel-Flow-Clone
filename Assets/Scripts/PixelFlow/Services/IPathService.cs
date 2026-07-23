using System.Collections.Generic;
using UnityEngine;
using PixelFlow.Data;

namespace PixelFlow.Services
{
    public interface IPathService
    {
        bool CanDrawPath(ColorType color, Vector2Int from, Vector2Int to);
        void DrawPath(ColorType color, Vector2Int from, Vector2Int to);
        void ClearPath(ColorType color);
        void BacktrackPath(ColorType color, Vector2Int toPos);
        void BreakPath(ColorType color, Vector2Int breakPos);
        List<Vector2Int> GetPathCells(ColorType color);
        /// <summary>
        /// Tüm renklere ait yolları temizler. Hücreleri başlangıç durumuna döndürür
        /// (node/obstacle state'leri korunur, Empty hücreler sıfırlanır).
        /// Viyadükler iade edilmez — Clear Jam power-up kasıtlı olarak viadük korur.
        /// </summary>
        void ClearAllPaths();
    }
}
