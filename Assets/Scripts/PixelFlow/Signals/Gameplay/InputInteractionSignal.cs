using UnityEngine;
using PixelFlow.Data;

namespace PixelFlow.Signals
{
    public enum InputType { PointerDown, Drag, PointerUp }

    public struct InputInteractionSignal
    {
        public InputType Type { get; set; }
        public Vector2Int GridPosition { get; set; }
        public ColorType Color { get; set; }
    }
}
