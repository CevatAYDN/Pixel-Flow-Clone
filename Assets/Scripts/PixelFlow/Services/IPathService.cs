using UnityEngine;
using PixelFlow.Data;

namespace PixelFlow.Services
{
    public interface IPathService
    {
        void ClearPath(ColorType color);
        void BacktrackPath(ColorType color, Vector2Int toPos);
        void BreakPath(ColorType color, Vector2Int breakPos);
    }
}
