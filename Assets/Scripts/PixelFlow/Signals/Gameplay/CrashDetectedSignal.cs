using UnityEngine;
using PixelFlow.Data;

namespace PixelFlow.Signals
{
    public struct CrashDetectedSignal
    {
        public Vector2Int Position;
        public ColorType ColorA;
        public ColorType ColorB;
    }
}
