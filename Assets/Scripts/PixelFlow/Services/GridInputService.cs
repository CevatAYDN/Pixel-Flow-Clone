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
        private int _activePointerId = -1;
        private bool _clickedOutside;
        private Vector2Int _lastGridPos = new Vector2Int(-1, -1);

        public GridInputResult ProcessInput(Camera gameplayCamera, int gridWidth, int gridHeight)
        {
            // 1. Detect pressed state
            bool isPressed = false;
            int pointerId = -1;
            Vector2 screenPos = Vector2.zero;
            bool detected = DetectPressedState(out isPressed, out pointerId, out screenPos);

            if (!detected)
                return GridInputResult.None;

            // 2. If no camera, can't convert to grid
            if (gameplayCamera == null)
                return GridInputResult.None;

            // 3. Process the input
            if (isPressed)
            {
                return ProcessPressed(pointerId, screenPos, gameplayCamera, gridWidth, gridHeight);
            }
            else
            {
                return ProcessReleased(pointerId);
            }
        }

        public void Reset()
        {
            _isPointerDown = false;
            _activePointerId = -1;
            _clickedOutside = false;
            _lastGridPos = new Vector2Int(-1, -1);
        }

        private bool DetectPressedState(out bool isPressed, out int pointerId, out Vector2 screenPos)
        {
            isPressed = false;
            pointerId = -1;
            screenPos = Vector2.zero;
            bool detected = false;

            var mouse = UnityEngine.InputSystem.Mouse.current;
            var touchscreen = UnityEngine.InputSystem.Touchscreen.current;

            // If currently dragging, stick to the active device
            if (_isPointerDown)
            {
                if (mouse != null && _activePointerId == mouse.deviceId)
                {
                    isPressed = mouse.leftButton.isPressed;
                    pointerId = mouse.deviceId;
                    screenPos = mouse.position.ReadValue();
                    detected = true;
                }
                else if (touchscreen != null && _activePointerId == touchscreen.deviceId)
                {
                    if (touchscreen.touches.Count > 0)
                    {
                        var touch = touchscreen.touches[0];
                        isPressed = touch.press.isPressed;
                        screenPos = touch.position.ReadValue();
                    }
                    else
                    {
                        isPressed = false;
                    }
                    pointerId = touchscreen.deviceId;
                    detected = true;
                }
                else if (_activePointerId == 9999)
                {
                    bool legacyPressed = false;
                    Vector3 legacyPos = Vector3.zero;
                    try
                    {
                        legacyPressed = UnityEngine.Input.GetMouseButton(0);
                        legacyPos = UnityEngine.Input.mousePosition;
                    }
                    catch (System.InvalidOperationException) { }

                    isPressed = legacyPressed;
                    pointerId = 9999;
                    screenPos = legacyPos;
                    detected = true;
                }
            }

            // If not currently dragging or active device lost, detect which device is pressed
            if (!detected)
            {
                if (touchscreen != null && touchscreen.touches.Count > 0 && touchscreen.touches[0].press.isPressed)
                {
                    var touch = touchscreen.touches[0];
                    isPressed = true;
                    pointerId = touchscreen.deviceId;
                    screenPos = touch.position.ReadValue();
                    detected = true;
                }
                else if (mouse != null && mouse.leftButton.isPressed)
                {
                    isPressed = true;
                    pointerId = mouse.deviceId;
                    screenPos = mouse.position.ReadValue();
                    detected = true;
                }
                else
                {
                    bool legacyPressed = false;
                    Vector3 legacyPos = Vector3.zero;
                    try
                    {
                        legacyPressed = UnityEngine.Input.GetMouseButton(0);
                        legacyPos = UnityEngine.Input.mousePosition;
                    }
                    catch (System.InvalidOperationException) { }

                    if (legacyPressed)
                    {
                        isPressed = true;
                        pointerId = 9999;
                        screenPos = legacyPos;
                        detected = true;
                    }
                    else
                    {
                        var pointer = UnityEngine.InputSystem.Pointer.current;
                        if (pointer != null)
                        {
                            isPressed = pointer.press.isPressed;
                            pointerId = pointer.deviceId;
                            screenPos = pointer.position.ReadValue();
                            detected = true;
                        }
                    }
                }
            }

            return detected;
        }

        private GridInputResult ProcessPressed(int pointerId, Vector2 screenPos, Camera cam, int gridWidth, int gridHeight)
        {
            // Different pointer started dragging — ignore
            if (_isPointerDown && pointerId != _activePointerId)
                return GridInputResult.None;

            Vector3 worldPos = cam.ScreenToWorldPoint(screenPos);
            int gx = Mathf.RoundToInt(worldPos.x);
            int gy = Mathf.RoundToInt(worldPos.y);

            bool insideGrid = gx >= 0 && gx < gridWidth && gy >= 0 && gy < gridHeight;

            if (!_isPointerDown)
            {
                // Pointer Down
                _isPointerDown = true;
                _activePointerId = pointerId;
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

        private GridInputResult ProcessReleased(int pointerId)
        {
            if (!_isPointerDown || pointerId != _activePointerId)
                return GridInputResult.None;

            _isPointerDown = false;
            _activePointerId = -1;

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
