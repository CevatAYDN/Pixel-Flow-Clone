using UnityEngine;
using System;
using PixelFlow.Core;
using PixelFlow.Data;
using PixelFlow.Models;
using PixelFlow.Services;
using Nexus.Core;

namespace PixelFlow.Views
{
    public class CellView : View
    {
        [Inject] public ThemePaletteAsset ThemePalette { get; set; }
        [Inject, OptionalInject] public IObstacleService ObstacleService { get; set; }
        [Inject, OptionalInject] public GameConfig Config { get; set; }
        [Header("Sprite Renderers")]
        [SerializeField] private SpriteRenderer _bgRenderer;
        [SerializeField] private SpriteRenderer _borderRenderer;
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

        private static Sprite _fallbackCircle, _fallbackSquare, _fallbackTriangle, _fallbackDiamond, _fallbackStar, _fallbackWarning, _fallbackBg, _fallbackConstruction, _fallbackLake, _fallbackPark, _fallbackArrow, _fallbackBridge;
        private static Sprite _fallbackBorderFrame;
        private static Color _fallbackBorderColor;

        public Vector2Int GridPosition { get; private set; }

        public void Setup(Vector2Int pos)
        {
            GridPosition = pos;
            EnsureRenderersAndSprites();
            if (ThemePalette == null)
                throw new DataValidationException("[CellView] ThemePaletteAsset is not injected. Bind ThemePaletteAsset in GameContextLifecycle.");
            _rejectionColor = ThemePalette.RejectionPulse;
            _fallbackBorderColor = ThemePalette.FallbackBorderColor;
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

            // Border overlay: hücre çerçevesi için ayrı SpriteRenderer
            // tema renginden bağımsız sabit bir renk ile çizilir
            if (_borderRenderer == null)
            {
                var borderObj = transform.Find("Border");
                if (borderObj != null) _borderRenderer = borderObj.GetComponent<SpriteRenderer>();
                if (_borderRenderer == null)
                {
                    var newBorder = new GameObject("Border");
                    newBorder.transform.SetParent(transform, false);
                    _borderRenderer = newBorder.AddComponent<SpriteRenderer>();
                }
            }
            _borderRenderer.transform.localPosition = new Vector3(0, 0, -0.01f);
            // sortingOrder varsayılan (0): Z=-0.01f → bg (0)'ın önünde,
            // bridge (-0.3f), dot (-0.4f) gibi elemanların arkasında kalır

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
                GenerateFallbackSpritesIfNeeded(ThemePalette);
                _bgRenderer.sprite = _fallbackBg;
            }
        }

        private static void GenerateFallbackSpritesIfNeeded(ThemePaletteAsset palette)
        {
            if (_fallbackSquare != null) return;

#if UNITY_EDITOR
            Debug.LogWarning("[CellView] Using runtime fallback sprites — assign shape sprites in the prefab for production quality.");
#endif

            // Zero-Hardcode §2.2: tüm renkler ThemePaletteAsset'ten okunur
            bool hasPalette = palette != null;
            Color cAmber = hasPalette ? palette.ProceduralConstructionAmber : new Color(0.95f, 0.65f, 0.1f, 1f);
            Color cDark = hasPalette ? palette.ProceduralConstructionDark : new Color(0.2f, 0.15f, 0.05f, 1f);
            Color cWaterDeep = hasPalette ? palette.ProceduralLakeWaterDeep : new Color(0.12f, 0.38f, 0.75f, 1f);
            Color cWaterLight = hasPalette ? palette.ProceduralLakeWaterLight : new Color(0.40f, 0.75f, 0.95f, 1f);
            Color cParkBase = hasPalette ? palette.ProceduralParkBase : new Color(0.18f, 0.52f, 0.24f, 1f);
            Color cParkLeaf = hasPalette ? palette.ProceduralParkLeaf : new Color(0.35f, 0.75f, 0.38f, 1f);
            Color cBridgeDeck = hasPalette ? palette.ProceduralBridgeDeck : new Color(0.82f, 0.84f, 0.90f, 1f);
            Color cBridgeRail = hasPalette ? palette.ProceduralBridgeRail : new Color(0.95f, 0.75f, 0.20f, 1f);

            int size = 128;
            Vector2 center = new Vector2(size * 0.5f, size * 0.5f);

            // 1. Square / Bg with rounded corners and anti-aliased borders
            // Grid çerçeve iyileştirmesi: border daha kalın (innerHalfWidth 54→48),
            // ayrıca tema renginden bağımsız border overlay sprite'ı oluşturulur
            Texture2D texSquare = new Texture2D(size, size);
            Color[] colorsSq = new Color[size * size];
            float cornerRadius = 24f;
            float outerHalfWidth = 57f;
            float innerHalfWidth = 48f; // 54→48: border genişliği 4px → 9px
            Color borderColor = _fallbackBorderColor;
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

            // 1b. Border frame overlay sprite (tema renginden bağımsız çerçeve)
            // Sadece border bölgesini içeren ayrı bir texture — center kısmı saydam
            Texture2D texBorderFrame = new Texture2D(size, size);
            Color[] colorsBorder = new Color[size * size];
            float borderOuterHW = 57f;
            float borderInnerHW = 47f; // border kalınlığından 1px içeriden başla (üst üste binme)
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float px = x - center.x;
                    float py = y - center.y;

                    // Outer edge
                    float dxBO = Mathf.Max(Mathf.Abs(px) - borderOuterHW + cornerRadius, 0f);
                    float dyBO = Mathf.Max(Mathf.Abs(py) - borderOuterHW + cornerRadius, 0f);
                    float distBO = Mathf.Sqrt(dxBO * dxBO + dyBO * dyBO) - cornerRadius;

                    // Inner edge (border inner side)
                    float dxBI = Mathf.Max(Mathf.Abs(px) - borderInnerHW + (cornerRadius - 2f), 0f);
                    float dyBI = Mathf.Max(Mathf.Abs(py) - borderInnerHW + (cornerRadius - 2f), 0f);
                    float distBI = Mathf.Sqrt(dxBI * dxBI + dyBI * dyBI) - (cornerRadius - 2f);

                    float alphaOut = Mathf.Clamp01(1f - (distBO + 0.5f));
                    float alphaIn = Mathf.Clamp01(1f - (distBI + 0.5f));

                    // Sadece border bölgesi (outer ile inner arası): alpha = outerAlpha * (1 - innerAlpha)
                    // Dışarıdaki alan: alphaOut
                    // İçerideki alan: alphaIn
                    // Border bölgesi: alphaOut * (1 - alphaIn) — yani dışarıda görünür, içeride saydam
                    float borderAlpha = alphaOut * (1f - alphaIn);
                    colorsBorder[y * size + x] = Color.white.WithAlpha(borderAlpha);
                }
            }
            texBorderFrame.SetPixels(colorsBorder);
            texBorderFrame.Apply();
            _fallbackBorderFrame = Sprite.Create(texBorderFrame, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 128f);

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
                    colorsCirc[y * size + x] = Color.white.WithAlpha(alpha);
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
                    colorsTri[y * size + x] = Color.white.WithAlpha(alpha);
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
                    colorsDia[y * size + x] = Color.white.WithAlpha(alpha);
                }
            }
            texDiamond.SetPixels(colorsDia);
            texDiamond.Apply();
            _fallbackDiamond = Sprite.Create(texDiamond, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 128f);

            _fallbackStar = _fallbackDiamond;
            _fallbackWarning = _fallbackTriangle;

            // 5. Construction Hazard Stripes Sprite
            Texture2D texConst = new Texture2D(size, size);
            Color[] colorsConst = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    bool stripe = ((x + y) / 16) % 2 == 0;
                    float distFromCenter = Vector2.Distance(new Vector2(x, y), center);
                    float alpha = Mathf.Clamp01(size * 0.45f - distFromCenter);
                    Color col = stripe ? cAmber : cDark;
                    col.a *= alpha;
                    colorsConst[y * size + x] = col;
                }
            }
            texConst.SetPixels(colorsConst);
            texConst.Apply();
            _fallbackConstruction = Sprite.Create(texConst, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 128f);

            // 6. Lake Water Ripple Sprite
            Texture2D texLake = new Texture2D(size, size);
            Color[] colorsLake = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center);
                    float ripple = (Mathf.Sin(dist * 0.25f) + 1f) * 0.5f;
                    float alpha = Mathf.Clamp01(size * 0.44f - dist);
                    Color col = Color.Lerp(cWaterDeep, cWaterLight, ripple * 0.6f);
                    col.a *= alpha;
                    colorsLake[y * size + x] = col;
                }
            }
            texLake.SetPixels(colorsLake);
            texLake.Apply();
            _fallbackLake = Sprite.Create(texLake, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 128f);

            // 7. Park Grass & Foliage Sprite
            Texture2D texPark = new Texture2D(size, size);
            Color[] colorsPark = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center);
                    float leafPattern = (Mathf.Sin(x * 0.3f) * Mathf.Cos(y * 0.3f) + 1f) * 0.5f;
                    float alpha = Mathf.Clamp01(size * 0.44f - dist);
                    Color col = Color.Lerp(cParkBase, cParkLeaf, leafPattern * 0.5f);
                    col.a *= alpha;
                    colorsPark[y * size + x] = col;
                }
            }
            texPark.SetPixels(colorsPark);
            texPark.Apply();
            _fallbackPark = Sprite.Create(texPark, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 128f);

            // 8. OneWay Arrow Sprite
            Texture2D texArrow = new Texture2D(size, size);
            Color[] colorsArrow = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float nx = (x - center.x) / (size * 0.5f);
                    float ny = (y - center.y) / (size * 0.5f);
                    bool inShaft = (nx >= -0.6f && nx <= 0.1f && Mathf.Abs(ny) <= 0.2f);
                    bool inHead = (nx >= 0.1f && nx <= 0.6f && Mathf.Abs(ny) <= (0.6f - nx));
                    float alpha = (inShaft || inHead) ? 1f : 0f;
                    colorsArrow[y * size + x] = Color.white.WithAlpha(alpha);
                }
            }
            texArrow.SetPixels(colorsArrow);
            texArrow.Apply();
            _fallbackArrow = Sprite.Create(texArrow, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 128f);

            // 9. Viaduct Bridge Sprite
            Texture2D texBridge = new Texture2D(size, size);
            Color[] colorsBridge = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float nx = (x - center.x) / (size * 0.5f);
                    float ny = (y - center.y) / (size * 0.5f);
                    bool inDeck = (Mathf.Abs(ny) <= 0.28f && Mathf.Abs(nx) <= 0.46f);
                    bool inRail = (Mathf.Abs(ny) >= 0.20f && Mathf.Abs(ny) <= 0.32f && Mathf.Abs(nx) <= 0.46f);
                    float alpha = (inDeck || inRail) ? 1f : 0f;
                    Color col = inRail ? cBridgeRail : cBridgeDeck;
                    col.a *= alpha;
                    colorsBridge[y * size + x] = col;
                }
            }
            texBridge.SetPixels(colorsBridge);
            texBridge.Apply();
            _fallbackBridge = Sprite.Create(texBridge, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 128f);
        }

        private static float DistToLine(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 v = b - a;
            Vector2 n = new Vector2(-v.y, v.x).normalized;
            return Vector2.Dot(p - a, n);
        }

        private void EnsureProceduralBridge3D()
        {
            if (_bridge3D != null) return;

            var bridgeObj = new GameObject("ProceduralBridge3D");
            bridgeObj.transform.SetParent(transform, false);
            bridgeObj.transform.localPosition = new Vector3(0f, 0f, -0.3f);

            var deck = GameObject.CreatePrimitive(PrimitiveType.Cube);
            deck.name = "BridgeDeck";
            deck.transform.SetParent(bridgeObj.transform, false);
            deck.transform.localPosition = new Vector3(0f, 0f, 0f);
            deck.transform.localScale = new Vector3(0.85f, 0.4f, 0.15f);

            var deckRenderer = deck.GetComponent<MeshRenderer>();
            if (deckRenderer != null)
            {
                var mat = new Material(Shader.Find("Sprites/Default") ?? Shader.Find("Standard"));
                mat.color = ThemePalette != null ? ThemePalette.ProceduralBridgeMaterial : new Color(0.78f, 0.80f, 0.88f, 1f);
                deckRenderer.sharedMaterial = mat;
            }

            var railLeft = GameObject.CreatePrimitive(PrimitiveType.Cube);
            railLeft.name = "RailLeft";
            railLeft.transform.SetParent(bridgeObj.transform, false);
            railLeft.transform.localPosition = new Vector3(0f, 0.18f, -0.1f);
            railLeft.transform.localScale = new Vector3(0.85f, 0.06f, 0.2f);
            var railLeftRenderer = railLeft.GetComponent<MeshRenderer>();
            if (railLeftRenderer != null)
            {
                railLeftRenderer.sharedMaterial = deckRenderer?.sharedMaterial;
            }

            var railRight = GameObject.CreatePrimitive(PrimitiveType.Cube);
            railRight.name = "RailRight";
            railRight.transform.SetParent(bridgeObj.transform, false);
            railRight.transform.localPosition = new Vector3(0f, -0.18f, -0.1f);
            railRight.transform.localScale = new Vector3(0.85f, 0.06f, 0.2f);
            var railRightRenderer = railRight.GetComponent<MeshRenderer>();
            if (railRightRenderer != null)
            {
                railRightRenderer.sharedMaterial = deckRenderer?.sharedMaterial;
            }

            _bridge3D = bridgeObj;
        }

        public Color GetCellBackgroundColor(AppTheme theme)
        {
            // ThemePalette, Setup()'de null controlünden geçer — DataValidationException fırlatır
            return ThemePalette.GetCellBackground(theme);
        }

        public void AssignShapeSprite(SpriteRenderer renderer, ColorType colorType)
        {
            if (renderer == null) return;
            GenerateFallbackSpritesIfNeeded(ThemePalette);
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

            // ThemePalette, Setup()'de null controlünden geçer — direkt property access
            Color crashBright = ThemePalette.CrashPulseBright;
            Color crashDark = ThemePalette.CrashPulseDark;

            if (crashPos.x >= 0 && crashPos.y >= 0 && GridPosition == crashPos)
            {
                float pulse = (Mathf.Sin(Time.time * 8f) + 1f) * 0.5f;
                cellBg = Color.Lerp(crashBright, crashDark, pulse);
            }

            // Rainbow Road detection: tüm 5 renge sahip hücrelerde gökkuşağı animasyonu
            _isRainbow = cellData.PathColorCount >= 5;
            if (_isRainbow)
            {
                // Rastgele offset ile her hücre farklı fazda başlasın
                _rainbowHueOffset = (GridPosition.x * 0.137f + GridPosition.y * 0.269f) % 1f;
            }

            GenerateFallbackSpritesIfNeeded(ThemePalette);
            EnsureProceduralBridge3D();

            _bgRenderer.transform.localScale = new Vector3(0.96f, 0.96f, 1f); // 0.92→0.96: hücreler arası boşluk azaltıldı

            // Border overlay: tema renginden bağımsız sabit beyaz çerçeve (görünürlük artışı)
            if (_borderRenderer != null)
            {
                GenerateFallbackSpritesIfNeeded(ThemePalette);
                _borderRenderer.enabled = true;
                _borderRenderer.sprite = _fallbackBorderFrame;
                _borderRenderer.color = Color.white.WithAlpha(0.25f); // sabit düşük alpha — her temada görünür
                _borderRenderer.transform.localScale = new Vector3(0.96f, 0.96f, 1f);
            }

            bool isBridge = cellData.HasViaduct || cellData.State == CellState.Bridge;

            if (_bg3D != null) _bg3D.SetActive(true);
            if (_dot3D != null) _dot3D.SetActive(cellData.State == CellState.Node);
            if (_bridge3D != null) _bridge3D.SetActive(isBridge);

            if (_bridgeRenderer != null)
            {
                _bridgeRenderer.enabled = isBridge;
                if (isBridge)
                {
                    if (_bridgeRenderer.sprite == null)
                        _bridgeRenderer.sprite = _fallbackBridge;
                    _bridgeRenderer.color = Color.white;
                    _bridgeRenderer.transform.localScale = new Vector3(0.9f, 0.9f, 1f);
                    _bridgeRenderer.transform.localPosition = new Vector3(0f, 0f, -0.3f);
                }
            }

            bool hasConflict = cellData.PathColorCount >= 2 && !cellData.HasViaduct;
            if (_warningRenderer != null)
            {
                // Rainbow hücrelerde conflict warning gösterme (rainbow zaten çok renk demek)
                _warningRenderer.enabled = hasConflict && !_isRainbow;
                _warningRenderer.transform.localScale = new Vector3(0.4f, 0.4f, 1f);
                _warningRenderer.transform.localPosition = new Vector3(0f, 0f, -0.3f);
                if (hasConflict && !_isRainbow && _warningSprite != null)
                    _warningRenderer.sprite = _warningSprite;
            }

            if (cellData.ObstacleType != ObstacleType.None)
            {
                ApplyObstacleVisual(cellBg, cellData.ObstacleType);
            }
            else
            {
                switch (cellData.State)
                {
                    case CellState.Empty:
                        _bgRenderer.color = cellBg;
                        _bgRenderer.enabled = true;
                        _dotRenderer.enabled = false;
                        if (_oneWayArrow != null) _oneWayArrow.enabled = false;
                        break;

                    case CellState.Node:
                        _bgRenderer.color = cellBg;
                        _bgRenderer.enabled = true;
                        _dotRenderer.enabled = true;
                        _dotRenderer.color = GetColor(cellData.Color);
                        AssignShapeSprite(_dotRenderer, cellData.Color);
                        _dotRenderer.transform.localScale = new Vector3(0.45f, 0.45f, 1f);
                        if (_oneWayArrow != null) _oneWayArrow.enabled = false;
                        break;

                    case CellState.Path:
                        _bgRenderer.color = cellBg;
                        _bgRenderer.enabled = true;
                        _dotRenderer.enabled = false;
                        if (_oneWayArrow != null) _oneWayArrow.enabled = false;
                        break;

                    case CellState.Obstacle:
                        ApplyObstacleVisual(cellBg, cellData.ObstacleType);
                        break;

                    case CellState.Bridge:
                        _bgRenderer.color = cellBg;
                        _bgRenderer.enabled = true;
                        _dotRenderer.enabled = false;
                        if (_oneWayArrow != null) _oneWayArrow.enabled = false;
                        break;
                }
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

            if (ThemePalette == null)
            {
                ThemePalette = Resources.Load<ThemePaletteAsset>("Configs/ThemePalette");
                if (ThemePalette == null)
                    throw new DataValidationException("[CellView] ThemePaletteAsset (Resources/Configs/ThemePalette) missing! Zero-Hardcode & Zero-Silent-Fallback policy requires ThemePaletteAsset.");
            }

            var pal = ThemePalette.GetObstaclePalette(type);
            switch (type)
            {
                case ObstacleType.Lake:
                    baseBg = pal.Background; iconColor = pal.Icon; iconSprite = pal.Sprite != null ? pal.Sprite : (_circleSprite != null ? _circleSprite : _fallbackLake); iconScale = pal.IconScale > 0 ? pal.IconScale : 0.85f; break;
                case ObstacleType.Park:
                    baseBg = pal.Background; iconColor = pal.Icon; iconSprite = pal.Sprite != null ? pal.Sprite : (_diamondSprite != null ? _diamondSprite : _fallbackPark); iconScale = pal.IconScale > 0 ? pal.IconScale : 0.85f; break;
                case ObstacleType.Construction:
                    baseBg = pal.Background; iconColor = pal.Icon; iconSprite = pal.Sprite != null ? pal.Sprite : (_triangleSprite != null ? _triangleSprite : _fallbackConstruction); iconScale = pal.IconScale > 0 ? pal.IconScale : 0.85f; break;
                case ObstacleType.OneWay:
                    baseBg = cellBg * 0.8f; iconColor = pal.Icon; iconSprite = pal.Sprite != null ? pal.Sprite : _fallbackArrow;
                    iconScale = pal.IconScale > 0 ? pal.IconScale : 0.7f; showOneWayArrow = true; 
                    Vector2Int dir = ObstacleService != null ? ObstacleService.GetOneWayDirection(GridPosition) : Vector2Int.right;
                    arrowAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                    break;
                case ObstacleType.Ferry:
                    bool isFerryBlocked = ObstacleService != null && ObstacleService.IsFerryBlocked(GridPosition);
                    baseBg = isFerryBlocked ? pal.Background * 0.5f : pal.Background;
                    iconColor = isFerryBlocked ? pal.Icon * 0.5f : pal.Icon;
                    iconSprite = pal.Sprite != null ? pal.Sprite : (_circleSprite != null ? _circleSprite : _fallbackLake); 
                    iconScale = pal.IconScale > 0 ? pal.IconScale : 0.75f; 
                    break;
                case ObstacleType.NarrowPass:
                    baseBg = pal.Background; iconColor = pal.Icon; iconSprite = pal.Sprite != null ? pal.Sprite : (_squareSprite != null ? _squareSprite : _fallbackSquare); iconScale = pal.IconScale > 0 ? pal.IconScale : 0.5f; break;
                default:
                    baseBg = cellBg * 0.6f; iconColor = cellBg * 0.4f; break;
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

        /// <summary>
        /// PERFORMANS: GridView.OnTick'te animasyonlu hücre takibi için.
        /// true ise hücre şu anda bounce, rejection veya rainbow animasyonu oynatıyor demektir.
        /// </summary>
        public bool IsAnimating => _isBouncing || _isRejecting || _isRainbow;

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
        private Color _rejectionColor; // Overridden by ThemePaletteAsset in Setup()
        private float RejectionPulseFrequency => Config != null ? Config.RejectionPulseFrequency
            : throw new DataValidationException("[CellView] GameConfig.RejectionPulseFrequency erişilemedi!");

        // Rainbow Road visual effect
        private bool _isRainbow = false;
        private float _rainbowHueOffset = 0f;

        public void TickAnimation(float deltaTime)
        {
            // 3 bool check: bounce, reject, rainbow — çoğu hücre çoğu frame'de animasyonsuz
            if (!_isBouncing && !_isRejecting && !_isRainbow)
                return;

            if (_isRainbow && !_isRejecting)
            {
                // Gökkuşağı renk döngüsü: HSL hue 0→1 arasında gezdir
                float hue = Mathf.Repeat(Time.time * 0.4f + _rainbowHueOffset, 1f);
                Color rainbowColor = Color.HSVToRGB(hue, 0.85f, 1f);
                if (_bgRenderer != null)
                {
                    Color current = _bgRenderer.color;
                    _bgRenderer.color = rainbowColor.WithAlpha(current.a);
                }
            }

            if (_isBouncing)
            {
                _bounceTimer += deltaTime;
                if (_bounceTimer >= _bounceDuration)
                {
                    _isBouncing = false;
                    transform.localScale = _baseLocalScale;
                }
                else
                {
                    float t = _bounceTimer / _bounceDuration;
                    float freq = 2.5f;
                    float decay = 4.0f;
                    float amplitude = Mathf.Sin(t * freq * Mathf.PI) * Mathf.Exp(-decay * t);
                    float scaleFactor = 1f + (_bounceScale - 1f) * amplitude;
                    transform.localScale = _baseLocalScale * scaleFactor;
                }
            }

            if (_isRejecting)
            {
                _rejectionTimer += deltaTime;
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
                        float pulse = (Mathf.Sin(Time.time * RejectionPulseFrequency) + 1f) * 0.5f;
                        _bgRenderer.color = Color.Lerp(_rejectionColor, _rejectionOriginalColor, t + pulse * (1f - t) * 0.3f);
                    }
                }
            }
        }

        public void TriggerBounceAnimation(float pressScale = 0.95f, float duration = 0.12f)
        {
            Nexus.Core.Services.NexusLog.Info("CellView", "TriggerBounceAnimation", "?", "Cell " + GridPosition + " triggered bounce");
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
            Nexus.Core.Services.NexusLog.Warn("CellView", "TriggerThirdColorRejectionPulse", "?", "Cell " + GridPosition + " triggered third-color rejection pulse");
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
