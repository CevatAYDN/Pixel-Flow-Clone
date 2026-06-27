using UnityEngine;
using System;
using PixelFlow.Data;
using PixelFlow.Models;
using Nexus.Core;

namespace PixelFlow.Views
{
    [Mediator(typeof(CellMediator))]
    public class CellView : View
    {
        public event Action<Vector2Int> OnPointerDown;
        public event Action<Vector2Int> OnPointerDrag;
        public event Action<Vector2Int> OnPointerUp;

        public Vector2Int GridPosition { get; private set; }
        
        public void Setup(Vector2Int pos)
        {
            GridPosition = pos;
        }

        public void UpdateVisuals(ColorType color, CellState state)
        {
            // TODO: Update sprite/color
        }

        private void OnMouseDown() => OnPointerDown?.Invoke(GridPosition);
        
        private void OnMouseEnter()
        {
            if (Input.GetMouseButton(0))
                OnPointerDrag?.Invoke(GridPosition);
        }
        
        private void OnMouseUp() => OnPointerUp?.Invoke(GridPosition);
    }
}
