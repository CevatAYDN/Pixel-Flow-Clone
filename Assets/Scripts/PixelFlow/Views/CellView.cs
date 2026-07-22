using UnityEngine;
using System;
using PixelFlow.Data;
using PixelFlow.Models;
using Nexus.Core;

namespace PixelFlow.Views
{
    public class CellView : View
    {
        [Header("Sprite Renderers")]
        [SerializeField] private SpriteRenderer _bgRenderer;
        [SerializeField] private SpriteRenderer _dotRenderer;
        [SerializeField] private SpriteRenderer _bridgeRenderer;
        [SerializeField] private SpriteRenderer _warningRenderer;
        [SerializeField] private SpriteRenderer _oneWayArrow;

        [Header("3D Objects (Prefab-assigned)")]
        [SerializeField] private GameObject _bg3D;
        [SerializeField] private GameObject _dot3D;
        [SerializeField] private GameObject _bridge3D;
        [SerializeField] private GameObject _obstacle3D;

        [Header("Shape Sprites (Assign in Prefab)")]
        [SerializeField] private Sprite _squareSprite;
        [SerializeField] private Sprite _circleSprite;
        [SerializeField] private Sprite _triangleSprite;
        [SerializeField] private Sprite _diamondSprite;
        [SerializeField] private Sprite _starSprite;
        [SerializeField] private Sprite _warningSprite;

        private static Sprite _fallbackCircle, _fallbackSquare, _fallbackTriangle, _fallbackDiamond, _fallbackStar, _fallbackWarning, _fallbackBg;

        public Vector2Int GridPosition { get; private set; }

        public void Setup(Vector2Int pos)
        {
            GridPosition = pos;
            EnsureRenderersAndSprites();
        }

        private void Awake()
        {
            EnsureRenderersAndSprites();
        }

        public void EnsureRenderersAndSprites()
        {
            if (_bgRenderer == null)
            {
                _bgRenderer = GetComponent<SpriteRenderer>();
                if (_bgRenderer == null)
                {
                    var bgObj = new GameObject("Background");
                    bgObj.transform.SetParent(transform, false);
                    _bgRenderer = bgObj.AddComponent<SpriteRenderer>();
                }
            }
            if (_bgRenderer != null)
            {
                _bgRenderer.transform.localPosition = new Vector3(0, 0, 0f);
            }

            if (_dotRenderer == null)
            {
                var dotObj = transform.Find("DotNode");
                if (dotObj != null) _dotRenderer = dotObj.GetComponent<SpriteRenderer>();
                if (_dotRenderer == null)
                {
                    var newDot = new GameObject("DotNode");
                    newDot.transform.SetParent(transform, false);
                    _dotRenderer = newDot.AddComponent<SpriteRenderer>();
                }
            }
            _dotRenderer.transform.localPosition = new Vector3(0, 0, -0.4f);

            if (_bridgeRenderer == null)
            {
                var bridgeObj = transform.Find("Bridge");
                if (bridgeObj != null) _bridgeRenderer = bridgeObj.GetComponent<SpriteRenderer>();
                if (_bridgeRenderer == null)
                {
                    var newBridge = new GameObject("Bridge");
                    newBridge.transform.SetParent(transform, false);
                    _bridgeRenderer = newBridge.AddComponent<SpriteRenderer>();
                }
            }
            _bridgeRenderer.transform.localPosition = new Vector3(0, 0, -0.2f);

            if (_warningRenderer == null)
            {
                var warnObj = transform.Find("Warning");
                if (warnObj != null) _warningRenderer = warnObj.GetComponent<SpriteRenderer>();
                if (_warningRenderer == null)
                {
                    var newWarn = new GameObject("Warning");
                    newWarn.transform.SetParent(transform, false);
                    _warningRenderer = newWarn.AddComponent<SpriteRenderer>();
                }
            }
            _warningRenderer.transform.localPosition = new Vector3(0, 0, -0.5f);

            if (_bgRenderer != null && _bgRenderer.sprite == null)
            {
                GenerateFallbackSpritesIfNeeded();
                _bgRenderer.sprite = _fallbackBg;
            }
        }

        private static void GenerateFallbackSpritesIfNeeded()
        {
            if (_fallbackSquare != null) return;

            int size = 128;
            Vector2 center = new Vector2(size * 0.5f, size * 0.5f);

            // 1. Square / Bg with rounded corners and anti-aliased borders
            Texture2D texSquare = new Texture2D(size, size);
            Color[] colorsSq = new Color[size * size];
            float cornerRadius = 24f;
            float outerHalfWidth = 58f;
            float innerHalfWidth = 54f;
            Color borderColor = new Color(0.18f, 0.22f, 0.32f, 0.85f);
            Color innerColor = Color.white;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float px = x - center.x;
                    float py = y - center.y;

                    // Outer rounded rectangle
                    float dxOuter = Mathf.Max(Mathf.Abs(px) - outerHalfWidth + cornerRadius, 0f);
                    float dyOuter = Mathf.Max(Mathf.Abs(py) - outerHalfWidth + cornerRadius, 0f);
                    float distOuter = Mathf.Sqrt(dxOuter * dxOuter + dyOuter * dyOuter) - cornerRadius;

                    // Inner rounded rectangle
                    float dxInner = Mathf.Max(Mathf.Abs(px) - innerHalfWidth + (cornerRadius - 4f), 0f);
                    float dyInner = Mathf.Max(Mathf.Abs(py) - innerHalfWidth + (cornerRadius - 4f), 0f);
                    float distInner = Mathf.Sqrt(dxInner * dxInner + dyInner * dyInner) - (cornerRadius - 4f);

                    float alphaOuter = Mathf.Clamp01(1f - (distOuter + 0.5f));
                    float alphaInner = Mathf.Clamp01(1f - (distInner + 0.5f));

                    Color pixelColor = Color.Lerp(borderColor, innerColor, alphaInner);
                    pixelColor.a *= alphaOuter;
                    colorsSq[y * size + x] = pixelColor;
                }
            }
            texSquare.SetPixels(colorsSq);
            texSquare.Apply();
            _fallbackSquare = Sprite.Create(texSquare, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 128f);
            _fallbackBg = _fallbackSquare;

            // 2. Circle with anti-aliasing
            Texture2D texCircle = new Texture2D(size, size);
            Color[] colorsCirc = new Color[size * size];
            float radius = size * 0.42f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float sqrDist = (new Vector2(x, y) - center).sqrMagnitude;
                    float dist = Mathf.Sqrt(sqrDist);
                    float alpha = Mathf.Clamp01(radius - dist);
                    colorsCirc[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
            }
            texCircle.SetPixels(colorsCirc);
            texCircle.Apply();
            _fallbackCircle = Sprite.Create(texCircle, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 128f);

            // 3. Triangle with anti-aliasing
            Texture2D texTri = new Texture2D(size, size);
            Color[] colorsTri = new Color[size * size];
            Vector2 A = new Vector2(size * 0.5f, size * 0.85f);
            Vector2 B = new Vector2(size * 0.18f, size * 0.22f);
            Vector2 C = new Vector2(size * 0.82f, size * 0.22f);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 P = new Vector2(x, y);
                    float d1 = DistToLine(P, A, B);
                    float d2 = DistToLine(P, B, C);
                    float d3 = DistToLine(P, C, A);
                    float dist = Mathf.Min(d1, Mathf.Min(d2, d3));
                    float alpha = Mathf.Clamp01(dist);
                    colorsTri[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
            }
            texTri.SetPixels(colorsTri);
            texTri.Apply();
            _fallbackTriangle = Sprite.Create(texTri, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 128f);

            // 4. Diamond with anti-aliasing
            Texture2D texDiamond = new Texture2D(size, size);
            Color[] colorsDia = new Color[size * size];
            float diamondRadius = size * 0.44f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = Mathf.Abs(x - center.x);
                    float dy = Mathf.Abs(y - center.y);
                    float edgeDist = (diamondRadius - (dx + dy)) * 0.7071f;
                    float alpha = Mathf.Clamp01(edgeDist);
                    colorsDia[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
            }
            texDiamond.SetPixels(colorsDia);
            texDiamond.Apply();
            _fallbackDiamond = Sprite.Create(texDiamond, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 128f);

            _fallbackStar = _fallbackDiamond;
            _fallbackWarning = _fallbackTriangle;
        }

        private static float DistToLine(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 v = b - a;
            Vector2 n = new Vector2(-v.y, v.x).normalized;
            return Vector2.Dot(p - a, n);
        }

        public static Color GetCellBackgroundColor(AppTheme theme)
        {
            switch (theme)
            {
                case AppTheme.Dark:
                    return new Color(0.043f, 0.059f, 0.098f, 1f);
                case AppTheme.Light:
                    return new Color(0.92f, 0.92f, 0.94f, 1f);
                case AppTheme.Neon:
                    return new Color(0.078f, 0.055f, 0.157f, 1f);
                default:
                    return new Color(0.043f, 0.059f, 0.098f, 1f);
            }
        }

        public void AssignShapeSprite(SpriteRenderer renderer, ColorType colorType)
        {
            if (renderer == null) return;
            GenerateFallbackSpritesIfNeeded();
            Sprite sprite = null;
            switch (colorType)
            {
                case ColorType.Blue:    sprite = _circleSprite != null ? _circleSprite : _fallbackCircle;   break;
                case ColorType.Red:     sprite = _triangleSprite != null ? _triangleSprite : _fallbackTriangle; break;
                case ColorType.Yellow:  sprite = _squareSprite != null ? _squareSprite : _fallbackSquare;   break;
                case ColorType.Green:   sprite = _diamondSprite != null ? _diamondSprite : _fallbackDiamond;  break;
                case ColorType.Purple:  sprite = _starSprite != null ? _starSprite : _fallbackStar;     break;
                default:                sprite = _circleSprite != null ? _circleSprite : _fallbackCircle;   break;
            }
            if (sprite != null)
                renderer.sprite = sprite;
        }

        public void UpdateVisuals(CellData cellData, AppTheme theme, Vector2Int crashPos = default)
        {
            Color cellBg = GetCellBackgroundColor(theme);

            if (crashPos.x >= 0 && crashPos.y >= 0 && GridPosition == crashPos)
            {
                float pulse = (Mathf.Sin(Time.time * 8f) + 1f) * 0.5f;
                cellBg = Color.Lerp(new Color(0.937f, 0.267f, 0.267f), new Color(0.6f, 0.1f, 0.1f), pulse);
            }

            _bgRenderer.transform.localScale = new Vector3(0.92f, 0.92f, 1f);

            if (_bg3D != null) _bg3D.SetActive(true);
            if (_dot3D != null) _dot3D.SetActive(cellData.State == CellState.Node);
            if (_bridge3D != null) _bridge3D.SetActive(cellData.HasViaduct || cellData.State == CellState.Bridge);

            bool hasConflict = cellData.PathColorCount >= 2 && !cellData.HasViaduct;
            if (_warningRenderer != null)
            {
                _warningRenderer.enabled = hasConflict;
                _warningRenderer.transform.localScale = new Vector3(0.4f, 0.4f, 1f);
                _warningRenderer.transform.localPosition = new Vector3(0f, 0f, -0.3f);
                if (hasConflict && _warningSprite != null)
                    _warningRenderer.sprite = _warningSprite;
            }

            switch (cellData.State)
            {
                case CellState.Empty:
                    _bgRenderer.color = cellBg;
                    _bgRenderer.enabled = true;
                    _dotRenderer.enabled = false;
                    _bridgeRenderer.enabled = false;
                    if (_oneWayArrow != null) _oneWayArrow.enabled = false;
                    break;

                case CellState.Node:
                    _bgRenderer.color = cellBg;
                    _bgRenderer.enabled = true;
                    _dotRenderer.enabled = true;
                    _dotRenderer.color = GetColor(cellData.Color);
                    AssignShapeSprite(_dotRenderer, cellData.Color);
                    _dotRenderer.transform.localScale = new Vector3(0.45f, 0.45f, 1f);
                    _bridgeRenderer.enabled = false;
                    if (_oneWayArrow != null) _oneWayArrow.enabled = false;
                    break;

                case CellState.Path:
                    _bgRenderer.color = cellBg;
                    _bgRenderer.enabled = true;
                    _dotRenderer.enabled = false;
                    _bridgeRenderer.enabled = false;
                    if (_oneWayArrow != null) _oneWayArrow.enabled = false;
                    break;

                case CellState.Obstacle:
                    ApplyObstacleVisual(cellBg, cellData.ObstacleType);
                    break;

                case CellState.Bridge:
                    _bgRenderer.color = cellBg;
                    _bgRenderer.enabled = true;
                    _dotRenderer.enabled = false;
                    _bridgeRenderer.enabled = true;
                    _bridgeRenderer.color = Color.white;
                    if (_oneWayArrow != null) _oneWayArrow.enabled = false;
                    break;
            }
        }

        private void ApplyObstacleVisual(Color cellBg, ObstacleType type)
        {
            if (_bgRenderer == null) return;
            Color baseBg = cellBg;
            Color iconColor = Color.white;
            Sprite iconSprite = _squareSprite;
            float iconScale = 0.55f;
            bool showOneWayArrow = false;
            float arrowAngle = 0f;

            switch (type)
            {
                case ObstacleType.Lake:
                    baseBg = new Color(0.10f, 0.28f, 0.55f, 1f);
                    iconColor = new Color(0.20f, 0.55f, 0.85f, 1f);
                    iconSprite = _circleSprite;
                    break;
                case ObstacleType.Park:
                    baseBg = new Color(0.15f, 0.40f, 0.20f, 1f);
                    iconColor = new Color(0.25f, 0.65f, 0.30f, 1f);
                    iconSprite = _diamondSprite;
                    break;
                case ObstacleType.Construction:
                    baseBg = new Color(0.55f, 0.40f, 0.10f, 1f);
                    iconColor = new Color(0.85f, 0.65f, 0.15f, 1f);
                    iconSprite = _triangleSprite;
                    break;
                case ObstacleType.OneWay:
                    baseBg = cellBg * 0.8f;
                    iconColor = new Color(0.8f, 0.8f, 0.85f, 1f);
                    iconSprite = _triangleSprite;
                    iconScale = 0.6f;
                    showOneWayArrow = true;
                    arrowAngle = 0f;
                    break;
                case ObstacleType.Ferry:
                    baseBg = new Color(0.15f, 0.35f, 0.50f, 1f);
                    iconColor = new Color(0.30f, 0.65f, 0.85f, 1f);
                    iconSprite = _diamondSprite;
                    iconScale = 0.6f;
                    break;
                case ObstacleType.NarrowPass:
                    baseBg = new Color(0.45f, 0.45f, 0.50f, 1f);
                    iconColor = new Color(0.85f, 0.85f, 0.90f, 1f);
                    iconSprite = _squareSprite;
                    iconScale = 0.35f;
                    break;
                default:
                    baseBg = cellBg * 0.6f;
                    iconColor = cellBg * 0.4f;
                    break;
            }

            _bgRenderer.color = baseBg;
            _bgRenderer.enabled = true;
            if (_dotRenderer != null)
            {
                _dotRenderer.enabled = iconSprite != null;
                if (iconSprite != null)
                {
                    _dotRenderer.sprite = iconSprite;
                    _dotRenderer.color = iconColor;
                    _dotRenderer.transform.localScale = new Vector3(iconScale, iconScale, 1f);
                }
            }
            if (_bridgeRenderer != null) _bridgeRenderer.enabled = false;
            if (_oneWayArrow != null)
            {
                _oneWayArrow.enabled = showOneWayArrow;
                if (showOneWayArrow)
                {
                    _oneWayArrow.color = iconColor;
                    _oneWayArrow.transform.localScale = new Vector3(0.5f, 0.5f, 1f);
                    _oneWayArrow.transform.localRotation = Quaternion.Euler(0f, 0f, arrowAngle);
                    _oneWayArrow.transform.localPosition = new Vector3(0f, 0f, -0.25f);
                }
            }
        }

        // GC-free animations variables
        private float _bounceScale = 1f;
        private float _bounceDuration = 0f;
        private float _bounceTimer = 0f;
        private bool _isBouncing = false;
        private Vector3 _baseLocalScale = Vector3.one;

        private float _rejectionDuration = 0f;
        private float _rejectionTimer = 0f;
        private bool _isRejecting = false;
        private Color _rejectionOriginalColor;
        private Color _rejectionColor = new Color(0.937f, 0.267f, 0.267f, 1f);
        private const float _rejectionPulseFrequency = 15f;

        private void Update()
        {
            if (_isBouncing)
            {
                _bounceTimer += Time.deltaTime;
                if (_bounceTimer >= _bounceDuration)
                {
                    _isBouncing = false;
                    transform.localScale = _baseLocalScale;
                }
                else
                {
                    float t = _bounceTimer / _bounceDuration;
                    float freq = 2.5f; // Oscillations frequency
                    float decay = 4.0f; // Damping decay rate
                    float amplitude = Mathf.Sin(t * freq * Mathf.PI) * Mathf.Exp(-decay * t);
                    float scaleFactor = 1f + (_bounceScale - 1f) * amplitude;
                    transform.localScale = _baseLocalScale * scaleFactor;
                }
            }

            if (_isRejecting)
            {
                _rejectionTimer += Time.deltaTime;
                if (_rejectionTimer >= _rejectionDuration)
                {
                    _isRejecting = false;
                    if (_bgRenderer != null)
                    {
                        _bgRenderer.color = _rejectionOriginalColor;
                    }
                }
                else
                {
                    if (_bgRenderer != null)
                    {
                        float t = _rejectionTimer / _rejectionDuration;
                        float pulse = (Mathf.Sin(Time.time * _rejectionPulseFrequency) + 1f) * 0.5f;
                        _bgRenderer.color = Color.Lerp(_rejectionColor, _rejectionOriginalColor, t + pulse * (1f - t) * 0.3f);
                    }
                }
            }
        }

        public void TriggerBounceAnimation(float pressScale = 0.95f, float duration = 0.12f)
        {
            if (!_isBouncing)
            {
                _baseLocalScale = Vector3.one;
            }
            _bounceScale = pressScale;
            _bounceDuration = duration;
            _bounceTimer = 0f;
            _isBouncing = true;
        }

        /// <summary>
        /// GDD §4.2: 3. renk reddedildiğinde hücrede kırmızı pulse animasyonu oynatır.
        /// ProcessInputCommand'den çağrılır (CanDrawPath false döndüğünde).
        /// </summary>
        public void TriggerThirdColorRejectionPulse(float duration = 0.6f)
        {
            if (_bgRenderer == null) return;
            if (!_isRejecting)
            {
                _rejectionOriginalColor = _bgRenderer.color;
            }
            _rejectionDuration = duration;
            _rejectionTimer = 0f;
            _isRejecting = true;
        }

        public static Color GetColor(ColorType colorType)
        {
            return GetColor(colorType, ColorBlindMode.None);
        }

        public static Color GetColor(ColorType colorType, ColorBlindMode colorBlindMode)
        {
            return ColorBlindPalette.Remap(colorType, colorBlindMode);
        }
    }
}
