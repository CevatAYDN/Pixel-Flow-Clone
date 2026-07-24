using System.Collections;
using UnityEngine;
using Nexus.Core;
using PixelFlow.Data;
using PixelFlow.Models;
using PixelFlow.Signals;

namespace PixelFlow.Services
{
    /// <summary>
    /// Seamless zoom-in/out kamera kontrolü. GDD §5.1: "Cinemachine Lerp, 0.8s
    /// ease-in-out" (gerçek Cinemachine kullanmadan, MonoBehaviour coroutine ile).
    /// [Mediator] attribute ile CameraControllerMediator tarafından otomatik
    /// bağlanır; DI enjeksiyonları Mediator üzerinden yapılır.
    /// </summary>
    [Mediator(typeof(CameraControllerMediator))]
    public class CameraController : View
    {
        [Inject] public ICameraProvider CameraProvider { get; set; }
        [Inject] public IGameStateModel GameStateModel { get; set; }
        [Inject] public ISignalBus SignalBus { get; set; }
        [Inject, OptionalInject] public GameConfig Config { get; set; }

        private Camera _cam;
        private Coroutine _transition;
        private ISignalSubscription _gridSub;

        // Hub: izometrik 45° görünüm, tüm şehir görünür. GameConfig'ten okunur.
        private Vector3 HubPosition => Config != null ? Config.HubCameraPosition : throw new DataValidationException("GameConfig.HubCameraPosition erişilemedi!");
        private Quaternion HubRotation => Config != null ? Quaternion.Euler(Config.HubCameraEuler) : throw new DataValidationException("GameConfig.HubCameraEuler erişilemedi!");
        private float HubSize => Config != null ? Config.HubCameraSize : throw new DataValidationException("GameConfig.HubCameraSize erişilemedi!");
        private float TransitionDuration => Config != null ? Config.StateTransitionDuration : throw new DataValidationException("GameConfig.StateTransitionDuration erişilemedi!");

        // Puzzle: top-down 90° görünüm, grid tam ekran.
        private Vector3 _puzzlePosition;
        private Quaternion _puzzleRotation = Quaternion.Euler(0f, 0f, 0f);
        private float _puzzleSize;

        // İlk state atamasında tekrar tetiklenmesini engelle.
        private bool _initialApplied;

        private void Awake()
        {
            _cam = GetComponent<Camera>();
            if (_cam == null)
                _cam = GetComponentInChildren<Camera>();
        }

        protected override void OnBind(IContext context)
        {
            base.OnBind(context);
            if (GameStateModel != null)
            {
                GameStateModel.OnStateChanged += HandleStateChanged;
            }
        }

        protected override void OnUnbind()
        {
            base.OnUnbind();
            if (GameStateModel != null)
            {
                GameStateModel.OnStateChanged -= HandleStateChanged;
            }
            _gridSub?.Dispose();
        }

        // Public köprü metotlar — Mediator tarafından çağrılır.
        public void MediatorOnBind() => OnBind(Context);
        public void MediatorOnUnbind() => OnUnbind();

        private void HandleStateChanged(GameState state)
        {
            if (state == GameState.MainMenu)
            {
                TransitionToHub();
            }
            else if (state == GameState.Playing || state == GameState.Simulating || state == GameState.Paused)
            {
                TransitionToPuzzle();
            }
        }

        public void SetPuzzleView(float centerX, float centerY, float orthoSize)
        {
            _puzzlePosition = new Vector3(centerX, centerY, -10f);
            _puzzleSize = orthoSize;
        }

        public void TransitionToHub()
        {
            if (_transition != null) StopCoroutine(_transition);
            _transition = StartCoroutine(LerpCamera(HubPosition, HubRotation, HubSize, TransitionDuration));
        }

        public void TransitionToPuzzle()
        {
            if (_puzzleSize <= 0f)
            {
                _puzzlePosition = new Vector3(2f, 2f, -10f);
                _puzzleSize = Config != null ? Config.PuzzleFallbackCameraSize : throw new DataValidationException("GameConfig.PuzzleFallbackCameraSize erişilemedi!");
            }
            _puzzleRotation = Quaternion.Euler(0f, 0f, 0f);
            if (_transition != null) StopCoroutine(_transition);
            _transition = StartCoroutine(LerpCamera(_puzzlePosition, _puzzleRotation, _puzzleSize, TransitionDuration));
        }

        private Coroutine _shakeCoroutine;

        public void TriggerShake(float intensity = 0.25f, float duration = 0.35f)
        {
            if (_shakeCoroutine != null) StopCoroutine(_shakeCoroutine);
            _shakeCoroutine = StartCoroutine(DoShake(intensity, duration));
        }

        private IEnumerator DoShake(float intensity, float duration)
        {
            if (_cam == null) yield break;
            Vector3 originalPos = _cam.transform.position;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float damp = 1f - (elapsed / duration);
                Vector3 offset = (Vector3)Random.insideUnitCircle * intensity * damp;
                _cam.transform.position = originalPos + offset;
                yield return null;
            }

            _cam.transform.position = originalPos;
        }

        /// <summary>
        /// GDD §2.4: Kaza anında kamerayı çarpışma hücresine odakla + hafif sarsıntı.
        /// </summary>
        public void FocusOnCrash(Vector2Int gridPos)
        {
            float shakeIntensity = Config != null ? Config.CrashShakeIntensity : throw new DataValidationException("GameConfig.CrashShakeIntensity erişilemedi!");
            float shakeDuration = Config != null ? Config.CrashShakeDuration : throw new DataValidationException("GameConfig.CrashShakeDuration erişilemedi!");
            TriggerShake(shakeIntensity, shakeDuration);
            // İsteğe bağlı: kamera pozisyonunu gridPos'a doğru kaydır.
            // Hızlı geri dönüş için kısa bir offset.
            if (_cam == null) return;
            StartCoroutine(DoCrashFocus(gridPos));
        }

        private IEnumerator DoCrashFocus(Vector2Int gridPos)
        {
            if (_cam == null) yield break;
            Vector3 originalPos = _cam.transform.position;
            Vector3 target = new Vector3(gridPos.x, gridPos.y, originalPos.z);
            float focusOffset = Config != null ? Config.CrashFocusOffset : throw new DataValidationException("GameConfig.CrashFocusOffset erişilemedi!");
            Vector3 dir = (target - originalPos).normalized * focusOffset;
            Vector3 focused = originalPos + dir;
            float t = 0f;
            float dur = Config != null ? Config.CameraTransitionDuration : throw new DataValidationException("GameConfig.CameraTransitionDuration erişilemedi!");
            while (t < dur)
            {
                if (_cam == null) yield break;
                t += Time.deltaTime;
                _cam.transform.position = Vector3.Lerp(originalPos, focused, t / dur);
                yield return null;
            }
            t = 0f;
            while (t < dur)
            {
                if (_cam == null) yield break;
                t += Time.deltaTime;
                _cam.transform.position = Vector3.Lerp(focused, originalPos, t / dur);
                yield return null;
            }
            if (_cam != null)
                _cam.transform.position = originalPos;
        }

        private IEnumerator LerpCamera(Vector3 targetPos, Quaternion targetRot, float targetSize, float duration)
        {
            if (_cam == null) yield break;

            Vector3 startPos = _cam.transform.position;
            Quaternion startRot = _cam.transform.rotation;
            float startSize = _cam.orthographicSize;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                t = t < 0.5f ? 2f * t * t : -1f + (4f - 2f * t) * t; // ease-in-out

                _cam.transform.position = Vector3.Lerp(startPos, targetPos, t);
                _cam.transform.rotation = Quaternion.Slerp(startRot, targetRot, t);
                _cam.orthographicSize = Mathf.Lerp(startSize, targetSize, t);
                yield return null;
            }

            _cam.transform.position = targetPos;
            _cam.transform.rotation = targetRot;
            _cam.orthographicSize = targetSize;
        }
    }
}
