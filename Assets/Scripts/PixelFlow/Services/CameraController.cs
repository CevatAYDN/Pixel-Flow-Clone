using System.Collections;
using UnityEngine;
using Nexus.Core;
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
        public IGameStateModel GameStateModel { get; set; }
        public ISignalBus SignalBus { get; set; }

        private Camera _cam;
        private Coroutine _transition;
        private ISignalSubscription _gridSub;

        // Hub: izometrik 45° görünüm, tüm şehir görünür.
        private readonly Vector3 _hubPosition = new Vector3(8f, 12f, -8f);
        private readonly Quaternion _hubRotation = Quaternion.Euler(45f, 45f, 0f);
        private const float HubSize = 7f;

        // Puzzle: top-down 90° görünüm, grid tam ekran.
        private Vector3 _puzzlePosition;
        private Quaternion _puzzleRotation = Quaternion.Euler(0f, 0f, 0f);
        private float _puzzleSize;

        // İlk state atamasında tekrar tetiklenmesini engelle.
        private bool _initialApplied;

        private void Awake()
        {
            _cam = GetComponent<Camera>();
            if (_cam == null) _cam = Camera.main;
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
            if (!_initialApplied)
            {
                _initialApplied = true;
                return;
            }

            // GDD §5.1: "Oyuncu Hub ekranında bir mahalleye tıkladığında, kamera gökyüzündeki
            // izometrik geniş açıdan pürüzsüz bir şekilde aşağı süzülerek doğrudan bulmaca alanına
            // odaklanır. Geri dönüşte aynı animasyon ters yönde çalışır."
            if (state == GameState.MainMenu)
            {
                TransitionToHub();
            }
            else if (state == GameState.Playing || state == GameState.Simulating || state == GameState.Paused)
            {
                // Puzzle görünümüne geçiş sadece bir kez GridMediator tarafından SetPuzzleView ile ayarlanır.
                if (_puzzleSize > 0f)
                {
                    TransitionToPuzzle();
                }
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
            _transition = StartCoroutine(LerpCamera(_hubPosition, _hubRotation, HubSize, 0.8f));
        }

        public void TransitionToPuzzle()
        {
            if (_puzzleSize <= 0f) return;
            if (_transition != null) StopCoroutine(_transition);
            _transition = StartCoroutine(LerpCamera(_puzzlePosition, _puzzleRotation, _puzzleSize, 0.8f));
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
            TriggerShake(0.35f, 0.45f);
            // İsteğe bağlı: kamera pozisyonunu gridPos'a doğru 0.3 birim kaydır.
            // Hızlı geri dönüş için kısa bir offset.
            if (_cam == null) return;
            StartCoroutine(DoCrashFocus(gridPos));
        }

        private IEnumerator DoCrashFocus(Vector2Int gridPos)
        {
            if (_cam == null) yield break;
            Vector3 originalPos = _cam.transform.position;
            Vector3 target = new Vector3(gridPos.x, gridPos.y, originalPos.z);
            Vector3 dir = (target - originalPos).normalized * 0.4f;
            Vector3 focused = originalPos + dir;
            float t = 0f;
            const float dur = 0.18f;
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
