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
        // Not: Input akışı tek bir yerden (GridView.Update) okunur.
        // Bu view'in OnMouse olayları kaldırıldı çünkü GridView zaten global olarak
        // Pointer.current'tan okuyup Bresenham-like interpolation yapıyor.
        // Çift yol (cell-level + grid-level) yarış koşuluna yol açıyordu.

        [SerializeField] private SpriteRenderer _bgRenderer;
        [SerializeField] private SpriteRenderer _dotRenderer;
        [SerializeField] private SpriteRenderer _bridgeRenderer;

        public Vector2Int GridPosition { get; private set; }

        private static Sprite s_squareSprite;
        private static Sprite s_circleSprite;

        private void Awake()
        {
            EnsureSprite(_bgRenderer);
            EnsureCircleSprite(_dotRenderer);
            EnsureSprite(_bridgeRenderer);

            if (_bgRenderer != null) _bgRenderer.sortingOrder = 0;
            if (_bridgeRenderer != null) _bridgeRenderer.sortingOrder = 2;
            if (_dotRenderer != null) _dotRenderer.sortingOrder = 3;
        }

        private static void EnsureSprite(SpriteRenderer renderer)
        {
            if (renderer == null) return;
            if (renderer.sprite != null) return;

            if (s_squareSprite == null)
            {
                Texture2D tex = new Texture2D(4, 4);
                for (int x = 0; x < 4; x++)
                    for (int y = 0; y < 4; y++)
                        tex.SetPixel(x, y, Color.white);
                tex.Apply();
                tex.hideFlags = HideFlags.DontSave;
                s_squareSprite = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
                s_squareSprite.hideFlags = HideFlags.DontSave;
            }
            renderer.sprite = s_squareSprite;
        }

        private static void EnsureCircleSprite(SpriteRenderer renderer, int radius = 32)
        {
            if (renderer == null) return;
            if (renderer.sprite != null) return;

            if (s_circleSprite == null)
            {
                int size = radius * 2;
                Texture2D tex = new Texture2D(size, size);
                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        float dx = x - radius + 0.5f;
                        float dy = y - radius + 0.5f;
                        if (dx * dx + dy * dy <= radius * radius)
                        {
                            tex.SetPixel(x, y, Color.white);
                        }
                        else
                        {
                            tex.SetPixel(x, y, Color.clear);
                        }
                    }
                }
                tex.Apply();
                tex.hideFlags = HideFlags.DontSave;
                s_circleSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
                s_circleSprite.hideFlags = HideFlags.DontSave;
            }
            renderer.sprite = s_circleSprite;
        }

        public void Setup(Vector2Int pos)
        {
            GridPosition = pos;
        }

        public static Color GetCellBackgroundColor(AppTheme theme)
        {
            switch (theme)
            {
                case AppTheme.Dark:
                    return new Color(0.15f, 0.15f, 0.18f, 1f);
                case AppTheme.Light:
                    return new Color(0.92f, 0.92f, 0.94f, 1f);
                case AppTheme.Neon:
                    return new Color(0.12f, 0.08f, 0.22f, 1f);
                default:
                    return new Color(0.2f, 0.2f, 0.2f, 1f);
            }
        }

        public void UpdateVisuals(ColorType color, CellState state, AppTheme theme)
        {
            Color cellBg = GetCellBackgroundColor(theme);
            _bgRenderer.transform.localScale = new Vector3(0.92f, 0.92f, 1f);

            switch (state)
            {
                case CellState.Empty:
                    _bgRenderer.color = cellBg;
                    _bgRenderer.enabled = true;
                    _dotRenderer.enabled = false;
                    _bridgeRenderer.enabled = false;
                    break;

                case CellState.Node:
                    _bgRenderer.color = cellBg;
                    _bgRenderer.enabled = true;
                    _dotRenderer.enabled = true;
                    _dotRenderer.color = GetColor(color);
                    _dotRenderer.transform.localScale = new Vector3(0.45f, 0.45f, 1f);
                    _bridgeRenderer.enabled = false;
                    break;

                case CellState.Path:
                    _bgRenderer.color = cellBg;
                    _bgRenderer.enabled = true;
                    _dotRenderer.enabled = false;
                    _bridgeRenderer.enabled = false;
                    break;

                case CellState.Bridge:
                    _bgRenderer.color = cellBg;
                    _bgRenderer.enabled = true;
                    _dotRenderer.enabled = false;
                    _bridgeRenderer.enabled = true;
                    _bridgeRenderer.color = Color.white;
                    break;
            }
        }

        public static Color GetColor(ColorType colorType)
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

        }
}
