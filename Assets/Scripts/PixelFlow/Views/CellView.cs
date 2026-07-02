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
        [SerializeField] private SpriteRenderer _bgRenderer;
        [SerializeField] private SpriteRenderer _dotRenderer;
        [SerializeField] private SpriteRenderer _bridgeRenderer;
        [SerializeField] private SpriteRenderer _warningRenderer;

        // 3D Low-Poly Fallbacks (assigned in inspector for 3D hubs)
        [SerializeField] private GameObject _bg3D;
        [SerializeField] private GameObject _dot3D;
        [SerializeField] private GameObject _bridge3D;

        public Vector2Int GridPosition { get; private set; }

        private static Sprite s_squareSprite;
        private static Sprite s_circleSprite;
        private static Sprite s_triangleSprite;
        private static Sprite s_diamondSprite;
        private static Sprite s_starSprite;
        private static Sprite s_warningSprite;

        private void Awake()
        {
            EnsureSprite(_bgRenderer);
            EnsureCircleSprite(_dotRenderer);
            EnsureSprite(_bridgeRenderer);

            if (_warningRenderer == null)
            {
                GameObject warningObj = new GameObject("WarningIcon");
                warningObj.transform.SetParent(transform, false);
                _warningRenderer = warningObj.AddComponent<SpriteRenderer>();
            }
            EnsureWarningSprite(_warningRenderer);

            if (_bgRenderer != null) _bgRenderer.sortingOrder = 0;
            if (_bridgeRenderer != null) _bridgeRenderer.sortingOrder = 2;
            if (_dotRenderer != null) _dotRenderer.sortingOrder = 3;
            if (_warningRenderer != null) _warningRenderer.sortingOrder = 4;

            if (_bridge3D == null)
            {
                CreateBridgeVisual();
            }
        }

        private void CreateBridgeVisual()
        {
            _bridge3D = new GameObject("Bridge3D");
            _bridge3D.transform.SetParent(transform, false);
            _bridge3D.transform.localPosition = new Vector3(0f, 0f, -0.3f);

            var deck = GameObject.CreatePrimitive(PrimitiveType.Cube);
            deck.name = "Deck";
            deck.transform.SetParent(_bridge3D.transform, false);
            deck.transform.localScale = new Vector3(0.85f, 0.1f, 0.4f);
            deck.transform.localPosition = new Vector3(0f, 0.15f, 0f);

            var deckR = deck.GetComponent<Renderer>();
            if (deckR != null)
            {
                deckR.material = new Material(Shader.Find("Sprites/Default"));
                deckR.material.color = new Color(0.35f, 0.35f, 0.4f, 1f);
            }

            for (int side = -1; side <= 1; side += 2)
            {
                var pillar = GameObject.CreatePrimitive(PrimitiveType.Cube);
                pillar.name = "Pillar";
                pillar.transform.SetParent(_bridge3D.transform, false);
                pillar.transform.localScale = new Vector3(0.15f, 0.25f, 0.15f);
                pillar.transform.localPosition = new Vector3(side * 0.3f, -0.05f, 0f);

                var pillR = pillar.GetComponent<Renderer>();
                if (pillR != null)
                {
                    pillR.material = new Material(Shader.Find("Sprites/Default"));
                    pillR.material.color = new Color(0.25f, 0.25f, 0.3f, 1f);
                }
            }

            _bridge3D.SetActive(false);
        }

        private static void EnsureSprite(SpriteRenderer renderer)
        {
            if (renderer == null) return;
            if (renderer.sprite != null && renderer.sprite == s_squareSprite) return;

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
            if (renderer.sprite != null && renderer.sprite == s_circleSprite) return;

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

        private static void EnsureTriangleSprite(SpriteRenderer renderer, int radius = 32)
        {
            if (renderer == null) return;
            if (renderer.sprite != null && renderer.sprite == s_triangleSprite) return;

            if (s_triangleSprite == null)
            {
                int size = radius * 2;
                Texture2D tex = new Texture2D(size, size);
                for (int y = 0; y < size; y++)
                {
                    float pct = (float)y / size; // 0 at bottom, 1 at top
                    int halfWidth = Mathf.RoundToInt((size / 2) * pct);
                    int cx = size / 2;
                    for (int x = 0; x < size; x++)
                    {
                        if (x >= cx - halfWidth && x <= cx + halfWidth)
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
                s_triangleSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
                s_triangleSprite.hideFlags = HideFlags.DontSave;
            }
            renderer.sprite = s_triangleSprite;
        }

        private static void EnsureDiamondSprite(SpriteRenderer renderer, int radius = 32)
        {
            if (renderer == null) return;
            if (renderer.sprite != null && renderer.sprite == s_diamondSprite) return;

            if (s_diamondSprite == null)
            {
                int size = radius * 2;
                Texture2D tex = new Texture2D(size, size);
                int cx = size / 2;
                int cy = size / 2;
                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        if (Mathf.Abs(x - cx) + Mathf.Abs(y - cy) <= radius)
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
                s_diamondSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
                s_diamondSprite.hideFlags = HideFlags.DontSave;
            }
            renderer.sprite = s_diamondSprite;
        }

        private static void EnsureStarSprite(SpriteRenderer renderer, int radius = 32)
        {
            if (renderer == null) return;
            if (renderer.sprite != null && renderer.sprite == s_starSprite) return;

            if (s_starSprite == null)
            {
                int size = radius * 2;
                Texture2D tex = new Texture2D(size, size);
                int cx = size / 2;
                int cy = size / 2;
                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        float dx = Mathf.Abs(x - cx);
                        float dy = Mathf.Abs(y - cy);
                        if (Mathf.Pow(dx, 0.6f) + Mathf.Pow(dy, 0.6f) <= Mathf.Pow(radius, 0.6f))
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
                s_starSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
                s_starSprite.hideFlags = HideFlags.DontSave;
            }
            renderer.sprite = s_starSprite;
        }

        private static void EnsureWarningSprite(SpriteRenderer renderer, int size = 64)
        {
            if (renderer == null) return;
            if (renderer.sprite != null && renderer.sprite == s_warningSprite) return;

            if (s_warningSprite == null)
            {
                Texture2D tex = new Texture2D(size, size);
                for (int y = 0; y < size; y++)
                    for (int x = 0; x < size; x++)
                        tex.SetPixel(x, y, Color.clear);

                Color warningYellow = new Color(1f, 0.85f, 0.24f);
                for (int y = 0; y < size; y++)
                {
                    float pct = (float)y / size;
                    int halfWidth = Mathf.RoundToInt((size / 2) * pct);
                    int cx = size / 2;
                    for (int x = cx - halfWidth; x <= cx + halfWidth; x++)
                    {
                        if (x >= 0 && x < size)
                        {
                            tex.SetPixel(x, size - 1 - y, warningYellow);
                        }
                    }
                }

                int midX = size / 2;
                int lineStart = Mathf.RoundToInt(size * 0.35f);
                int lineEnd = Mathf.RoundToInt(size * 0.65f);
                for (int y = lineStart; y <= lineEnd; y++)
                {
                    tex.SetPixel(midX, y, Color.black);
                    tex.SetPixel(midX - 1, y, Color.black);
                    tex.SetPixel(midX + 1, y, Color.black);
                }
                int dotY = Mathf.RoundToInt(size * 0.22f);
                tex.SetPixel(midX, dotY, Color.black);
                tex.SetPixel(midX - 1, dotY, Color.black);
                tex.SetPixel(midX + 1, dotY, Color.black);
                tex.SetPixel(midX, dotY + 1, Color.black);
                tex.SetPixel(midX, dotY - 1, Color.black);

                tex.Apply();
                tex.hideFlags = HideFlags.DontSave;
                s_warningSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
                s_warningSprite.hideFlags = HideFlags.DontSave;
            }
            renderer.sprite = s_warningSprite;
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

        public void AssignShapeSprite(SpriteRenderer renderer, ColorType colorType)
        {
            if (renderer == null) return;
            switch (colorType)
            {
                case ColorType.Blue:
                    EnsureCircleSprite(renderer);
                    break;
                case ColorType.Red:
                    EnsureTriangleSprite(renderer);
                    break;
                case ColorType.Yellow:
                    EnsureSprite(renderer); // Square
                    break;
                case ColorType.Green:
                    EnsureDiamondSprite(renderer);
                    break;
                case ColorType.Purple:
                    EnsureStarSprite(renderer);
                    break;
                default:
                    EnsureCircleSprite(renderer);
                    break;
            }
        }

        public void UpdateVisuals(CellData cellData, AppTheme theme)
        {
            Color cellBg = GetCellBackgroundColor(theme);
            _bgRenderer.transform.localScale = new Vector3(0.92f, 0.92f, 1f);

            // 3D GameObjects
            if (_bg3D != null) _bg3D.SetActive(true);
            if (_dot3D != null) _dot3D.SetActive(cellData.State == CellState.Node);
            if (_bridge3D != null) _bridge3D.SetActive(cellData.HasViaduct || cellData.State == CellState.Bridge);

            // Kaza Çakışma Uyarısı (2 veya daha fazla yol var ve viyadük yoksa)
            bool hasConflict = cellData.PathColors.Count >= 2 && !cellData.HasViaduct;
            if (_warningRenderer != null)
            {
                _warningRenderer.enabled = hasConflict;
                _warningRenderer.transform.localScale = new Vector3(0.4f, 0.4f, 1f);
                _warningRenderer.transform.localPosition = new Vector3(0f, 0f, -0.3f);
            }

            switch (cellData.State)
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
                    _dotRenderer.color = GetColor(cellData.Color);
                    AssignShapeSprite(_dotRenderer, cellData.Color);
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
