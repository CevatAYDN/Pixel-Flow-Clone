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
            if (_dotRenderer == null)
            {
                var dotObj = transform.Find("DotNode");
                if (dotObj != null) _dotRenderer = dotObj.GetComponent<SpriteRenderer>();
                if (_dotRenderer == null)
                {
                    var newDot = new GameObject("DotNode");
                    newDot.transform.SetParent(transform, false);
                    newDot.transform.localPosition = new Vector3(0, 0, -0.1f);
                    _dotRenderer = newDot.AddComponent<SpriteRenderer>();
                }
            }
            if (_bridgeRenderer == null)
            {
                var bridgeObj = transform.Find("Bridge");
                if (bridgeObj != null) _bridgeRenderer = bridgeObj.GetComponent<SpriteRenderer>();
                if (_bridgeRenderer == null)
                {
                    var newBridge = new GameObject("Bridge");
                    newBridge.transform.SetParent(transform, false);
                    newBridge.transform.localPosition = new Vector3(0, 0, -0.2f);
                    _bridgeRenderer = newBridge.AddComponent<SpriteRenderer>();
                }
            }
            if (_warningRenderer == null)
            {
                var warnObj = transform.Find("Warning");
                if (warnObj != null) _warningRenderer = warnObj.GetComponent<SpriteRenderer>();
                if (_warningRenderer == null)
                {
                    var newWarn = new GameObject("Warning");
                    newWarn.transform.SetParent(transform, false);
                    newWarn.transform.localPosition = new Vector3(0, 0, -0.3f);
                    _warningRenderer = newWarn.AddComponent<SpriteRenderer>();
                }
            }

            if (_bgRenderer != null && _bgRenderer.sprite == null)
            {
                GenerateFallbackSpritesIfNeeded();
                _bgRenderer.sprite = _fallbackBg;
            }
        }

        private static void GenerateFallbackSpritesIfNeeded()
        {
            if (_fallbackSquare != null) return;

            int size = 64;
            Texture2D texSquare = new Texture2D(size, size);
            Color[] colorsSq = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    bool border = (x <= 2 || x >= size - 3 || y <= 2 || y >= size - 3);
                    colorsSq[y * size + x] = border ? new Color(0.2f, 0.25f, 0.35f, 0.8f) : Color.white;
                }
            }
            texSquare.SetPixels(colorsSq);
            texSquare.Apply();
            _fallbackSquare = Sprite.Create(texSquare, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 64f);
            _fallbackBg = _fallbackSquare;

            Texture2D texCircle = new Texture2D(size, size);
            Color[] colorsCirc = new Color[size * size];
            float r = size * 0.45f;
            Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center);
                    colorsCirc[y * size + x] = dist <= r ? Color.white : Color.clear;
                }
            }
            texCircle.SetPixels(colorsCirc);
            texCircle.Apply();
            _fallbackCircle = Sprite.Create(texCircle, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 64f);

            Texture2D texTri = new Texture2D(size, size);
            Color[] colorsTri = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float normY = (float)y / size;
                    float halfWidth = normY * 0.45f * size;
                    float distFromMid = Mathf.Abs(x - size * 0.5f);
                    colorsTri[y * size + x] = (distFromMid <= halfWidth && y >= 6) ? Color.white : Color.clear;
                }
            }
            texTri.SetPixels(colorsTri);
            texTri.Apply();
            _fallbackTriangle = Sprite.Create(texTri, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 64f);

            Texture2D texDiamond = new Texture2D(size, size);
            Color[] colorsDia = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = Mathf.Abs(x - size * 0.5f) / (size * 0.45f);
                    float dy = Mathf.Abs(y - size * 0.5f) / (size * 0.45f);
                    colorsDia[y * size + x] = (dx + dy <= 1f) ? Color.white : Color.clear;
                }
            }
            texDiamond.SetPixels(colorsDia);
            texDiamond.Apply();
            _fallbackDiamond = Sprite.Create(texDiamond, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 64f);

            _fallbackStar = _fallbackDiamond;
            _fallbackWarning = _fallbackTriangle;
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

        private Coroutine _bounceCoroutine;

        public void TriggerBounceAnimation(float pressScale = 0.95f, float duration = 0.12f)
        {
            if (_bounceCoroutine != null) StopCoroutine(_bounceCoroutine);
            _bounceCoroutine = StartCoroutine(DoBounceAnimation(pressScale, duration));
        }

        private System.Collections.IEnumerator DoBounceAnimation(float pressScale, float duration)
        {
            Vector3 originalScale = Vector3.one;
            float elapsed = 0f;
            float halfDuration = duration * 0.5f;
            Vector3 pressedScale = originalScale * pressScale;

            while (elapsed < halfDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / halfDuration;
                transform.localScale = Vector3.Lerp(originalScale, pressedScale, t);
                yield return null;
            }

            elapsed = 0f;
            while (elapsed < halfDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / halfDuration;
                transform.localScale = Vector3.Lerp(pressedScale, originalScale, t);
                yield return null;
            }

            transform.localScale = originalScale;
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
