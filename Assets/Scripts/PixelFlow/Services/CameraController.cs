using System.Collections;
using UnityEngine;
using Nexus.Core;
using PixelFlow.Models;

namespace PixelFlow.Services
{
    public class CameraController : View
    {
        [Inject] public IGameStateModel GameStateModel { get; set; }

        private Camera _cam;
        private Coroutine _transition;
        private Vector3 _hubPosition = new Vector3(8f, 12f, -8f);
        private Quaternion _hubRotation = Quaternion.Euler(45f, 45f, 0f);
        private float _hubSize = 7f;

        private Vector3 _puzzlePosition;
        private Quaternion _puzzleRotation = Quaternion.Euler(0f, 0f, 0f);
        private float _puzzleSize;

        private void Awake()
        {
            _cam = GetComponent<Camera>();
            if (_cam == null) _cam = Camera.main;
        }

        public void SetPuzzleView(float centerX, float centerY, float orthoSize)
        {
            _puzzlePosition = new Vector3(centerX, centerY, -10f);
            _puzzleSize = orthoSize;
        }

        public void TransitionToHub()
        {
            if (_transition != null) StopCoroutine(_transition);
            _transition = StartCoroutine(LerpCamera(_hubPosition, _hubRotation, _hubSize, 0.8f));
        }

        public void TransitionToPuzzle()
        {
            if (_transition != null) StopCoroutine(_transition);
            _transition = StartCoroutine(LerpCamera(_puzzlePosition, _puzzleRotation, _puzzleSize, 0.8f));
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
