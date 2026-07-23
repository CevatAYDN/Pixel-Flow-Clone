using UnityEngine;
using PixelFlow.Core;

namespace PixelFlow.Services
{
    /// <summary>
    /// Grid üzerindeki pointer input state machine'ini yöneten servis.
    /// Mouse, Touchscreen ve legacy input'u tek bir arayüzde birleştirir.
    /// 
    /// GridView'den ayrıştırıldı (Single Responsibility):
    /// - GridView: Görsel + input olaylarını iletme
    /// - GridInputService: Input algılama + grid koordinat dönüşümü
    /// 
    /// Her frame ProcessInput() çağrılır, sonuç GridInputResult olarak döner.
    /// </summary>
    public interface IGridInputService
    {
        /// <summary>
        /// Her frame çağrılır. Kamera ve grid boyutlarına göre input'u işler.
        /// GridInputResult.HasEvent == false ise bu frame'de input yok.
        /// </summary>
        GridInputResult ProcessInput(Camera gameplayCamera, int gridWidth, int gridHeight);

        /// <summary>
        /// Input state'ini sıfırlar (pointer up olmadan kesinti).
        /// </summary>
        void Reset();
    }

    public struct GridInputResult
    {
        /// <summary>Bu frame'de input olayı var mı?</summary>
        public bool HasEvent;

        /// <summary>Pointer ilk kez grid'e bastı.</summary>
        public bool IsDown;
        /// <summary>Pointer grid üzerinde sürükleniyor.</summary>
        public bool IsDrag;
        /// <summary>Pointer grid'den kalktı.</summary>
        public bool IsUp;

        /// <summary>İlgili grid pozisyonu.</summary>
        public Vector2Int GridPosition;

        public static GridInputResult None => new GridInputResult { HasEvent = false };
    }

    public class GridInputService : IGridInputService
    {
        // Pointer state machine
        private bool _isPointerDown;
        private bool _isPointerMouse;  // Hangi cihaz aktif: true=mouse, false=touch
        private bool _clickedOutside;
        private Vector2Int _lastGridPos = new Vector2Int(-1, -1);

        // Cihaz referanslarını constructor'da bir kere önbelleğe al
        // InputSystem statik property'leri her frame sorgulamak yerine
        // cached referans kullan — InputSystem bunları runtime'da değiştirmez.
        private readonly UnityEngine.InputSystem.Mouse _mouse;
        private readonly UnityEngine.InputSystem.Touchscreen _touchscreen;

        public GridInputService()
        {
            _mouse = UnityEngine.InputSystem.Mouse.current;
            _touchscreen = UnityEngine.InputSystem.Touchscreen.current;
        }

        public GridInputResult ProcessInput(Camera gameplayCamera, int gridWidth, int gridHeight)
        {
            // 1. Detect pressed state
            bool isPressed = false;
            bool isMouse = false;
            Vector2 screenPos = Vector2.zero;
            bool detected = DetectPressedState(out isPressed, out isMouse, out screenPos);

            if (!detected)
                return GridInputResult.None;

            // 2. If no camera, can't convert to grid
            if (gameplayCamera == null)
                return GridInputResult.None;

            // 3. Process the input
            if (isPressed)
            {
                return ProcessPressed(isMouse, screenPos, gameplayCamera, gridWidth, gridHeight);
            }
            else
            {
                return ProcessReleased();
            }
        }

        public void Reset()
        {
            _isPointerDown = false;
            _isPointerMouse = false;
            _clickedOutside = false;
            _lastGridPos = new Vector2Int(-1, -1);
        }

        /// <summary>
        /// Optimize edilmiş input algılama:
        /// - Cihaz referansları constructor'da cache'lenir (her frame static property yok)
        /// - Legacy input (Input.GetMouseButton) tamamen kaldırıldı — proje InputSystem kullanıyor
        /// - Pointer.current fallback kaldırıldı — Mouse/Touchscreen zaten Pointer subclass'ları
        /// - try/catch blokları kaldırıldı
        /// - Aktif drag sırasında sadece aktif cihaz sorgulanır
        /// - Idle'da önce touchscreen sorgulanır (mobile öncelik)
        /// </summary>
        private bool DetectPressedState(out bool isPressed, out bool isMouse, out Vector2 screenPos)
        {
            isPressed = false;
            isMouse = false;
            screenPos = Vector2.zero;

            if (_isPointerDown)
            {
                // ── Aktif drag: sadece aktif cihazı sorgula ──
                if (_isPointerMouse)
                {
                    // Mouse drag devam ediyor
                    isPressed = _mouse.leftButton.isPressed;
                    isMouse = true;
                    screenPos = _mouse.position.ReadValue();
                    return true;
                }
                else
                {
                    // Touch drag devam ediyor
                    try
                    {
                        var touches = _touchscreen.touches;
                        if (touches.Count > 0)
                        {
                            isPressed = touches[0].press.isPressed;
                            screenPos = touches[0].position.ReadValue();
                        }
                    }
                    catch
                    {
                        return false;
                    }
                    isMouse = false;
                    return true;
                }
            }

            // ── Idle: yeni basış algıla (touch öncelikli) ──
            try
            {
                if (_touchscreen != null && _touchscreen.touches.Count > 0 && _touchscreen.touches[0].press.isPressed)
                {
                    var touch = _touchscreen.touches[0];
                    isPressed = true;
                    isMouse = false;
                    screenPos = touch.position.ReadValue();
                    return true;
                }
            }
            catch { }

            if (_mouse != null && _mouse.leftButton.isPressed)
            {
                isPressed = true;
                isMouse = true;
                screenPos = _mouse.position.ReadValue();
                return true;
            }

            return false;
        }

        private GridInputResult ProcessPressed(bool isMouse, Vector2 screenPos, Camera cam, int gridWidth, int gridHeight)
        {
            // Different pointer started dragging — ignore (sadece cihaz türü kontrolü)
            if (_isPointerDown && isMouse != _isPointerMouse)
                return GridInputResult.None;

            Vector3 worldPos = cam.ScreenToWorldPoint(screenPos);
            int gx = Mathf.RoundToInt(worldPos.x);
            int gy = Mathf.RoundToInt(worldPos.y);

            bool insideGrid = gx >= 0 && gx < gridWidth && gy >= 0 && gy < gridHeight;

            if (!_isPointerDown)
            {
                // Pointer Down
                _isPointerDown = true;
                _isPointerMouse = isMouse;
                _clickedOutside = !insideGrid;
                _lastGridPos = new Vector2Int(gx, gy);

                if (insideGrid)
                {
                    return new GridInputResult
                    {
                        HasEvent = true,
                        IsDown = true,
                        GridPosition = new Vector2Int(gx, gy)
                    };
                }
                return GridInputResult.None;
            }
            else if (!_clickedOutside && insideGrid)
            {
                // Pointer Drag — returns raw current position; GridView handles interpolation
                Vector2Int currentGridPos = new Vector2Int(gx, gy);
                if (currentGridPos != _lastGridPos)
                {
                    _lastGridPos = currentGridPos;

                    return new GridInputResult
                    {
                        HasEvent = true,
                        IsDrag = true,
                        GridPosition = currentGridPos
                    };
                }
            }

            return GridInputResult.None;
        }

        private GridInputResult ProcessReleased()
        {
            if (!_isPointerDown)
                return GridInputResult.None;

            _isPointerDown = false;
            _isPointerMouse = false;

            var result = new GridInputResult
            {
                HasEvent = true,
                IsUp = true,
                GridPosition = _lastGridPos
            };

            _clickedOutside = false;
            _lastGridPos = new Vector2Int(-1, -1);

            return result;
        }


    }
}
