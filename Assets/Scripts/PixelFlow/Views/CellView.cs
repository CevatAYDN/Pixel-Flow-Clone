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

        [SerializeField] private SpriteRenderer _bgRenderer;
        [SerializeField] private SpriteRenderer _dotRenderer;
        [SerializeField] private SpriteRenderer _bridgeRenderer;

        public Vector2Int GridPosition { get; private set; }

        private bool _isDragging;

        private void Awake()
        {
            EnsureSprite(_bgRenderer);
            EnsureSprite(_dotRenderer);
            EnsureSprite(_bridgeRenderer);
        }

        private static void EnsureSprite(SpriteRenderer renderer)
        {
            if (renderer == null || renderer.sprite != null) return;
            Texture2D tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, Color.white);
            tex.Apply();
            renderer.sprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f));
        }

        public void Setup(Vector2Int pos)
        {
            GridPosition = pos;
        }

        public void UpdateVisuals(ColorType color, CellState state)
        {
            switch (state)
            {
                case CellState.Empty:
                    _bgRenderer.color = Color.white;
                    _bgRenderer.enabled = true;
                    _dotRenderer.enabled = false;
                    _bridgeRenderer.enabled = false;
                    break;

                case CellState.Node:
                    _bgRenderer.color = GetColor(color);
                    _bgRenderer.enabled = true;
                    _dotRenderer.enabled = true;
                    _dotRenderer.color = Color.white;
                    _bridgeRenderer.enabled = false;
                    break;

                case CellState.Path:
                    Color pathColor = GetColor(color);
                    pathColor.a = 0.7f;
                    _bgRenderer.color = pathColor;
                    _bgRenderer.enabled = true;
                    _dotRenderer.enabled = false;
                    _bridgeRenderer.enabled = false;
                    break;

                case CellState.Bridge:
                    _bgRenderer.color = GetColor(color);
                    _bgRenderer.enabled = true;
                    _dotRenderer.enabled = false;
                    _bridgeRenderer.enabled = true;
                    _bridgeRenderer.color = Color.white;
                    break;
            }
        }

        private static Color GetColor(ColorType colorType)
        {
            switch (colorType)
            {
                case ColorType.Red:     return Color.red;
                case ColorType.Green:   return Color.green;
                case ColorType.Blue:    return Color.blue;
                case ColorType.Yellow:  return Color.yellow;
                case ColorType.Orange:  return new Color(1f, 0.5f, 0f);
                case ColorType.Purple:  return new Color(0.5f, 0f, 0.5f);
                case ColorType.Cyan:    return Color.cyan;
                case ColorType.Magenta: return Color.magenta;
                default:                return Color.gray;
            }
        }

        private void OnMouseDown()
        {
            _isDragging = true;
            OnPointerDown?.Invoke(GridPosition);
        }
        
        private void OnMouseEnter()
        {
            if (_isDragging)
                OnPointerDrag?.Invoke(GridPosition);
        }
        
        private void OnMouseUp()
        {
            _isDragging = false;
            OnPointerUp?.Invoke(GridPosition);
        }
    }
}
