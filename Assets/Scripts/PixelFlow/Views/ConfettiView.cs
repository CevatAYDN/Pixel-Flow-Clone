using UnityEngine;
using PixelFlow.Signals;
using Nexus.Core;
using System.Collections.Generic;

namespace PixelFlow.Views
{
    [Mediator(typeof(ConfettiMediator))]
    public class ConfettiView : View
    {
        [SerializeField] private int _particleCount = 80;
        [SerializeField] private GameObject _confettiPrefab;

        private ConfettiPool _pool;

        protected override void OnBind(IContext context)
        {
            _pool = new ConfettiPool(_confettiPrefab, _particleCount, transform);
        }

        protected override void OnUnbind()
        {
            _pool?.Dispose();
            _pool = null;
        }

        public void Burst()
        {
            for (int i = 0; i < _particleCount; i++)
            {
                var particle = _pool.Get();
                if (particle == null) break;

                float angle = Random.Range(0f, Mathf.PI * 2f);
                float speed = Random.Range(2f, 5f);
                Vector3 dir = new Vector3(Mathf.Cos(angle) * speed, Random.Range(3f, 6f), Mathf.Sin(angle) * speed);
                particle.Init(dir, Random.Range(1.2f, 2.4f));
            }
        }
    }

    /// <summary>
    /// Object pool for confetti particles. Eliminates runtime CreatePrimitive/Destroy GC pressure.
    /// </summary>
    public class ConfettiPool : System.IDisposable
    {
        private readonly GameObject _prefab;
        private readonly Transform _parent;
        private readonly Stack<ConfettiParticle> _available = new Stack<ConfettiParticle>();
        private readonly List<ConfettiParticle> _active = new List<ConfettiParticle>();

        public ConfettiPool(GameObject prefab, int count, Transform parent)
        {
            _prefab = prefab;
            _parent = parent;

            for (int i = 0; i < count; i++)
            {
                GameObject go;
                if (_prefab != null)
                    go = Object.Instantiate(_prefab, _parent);
                else
                {
                    go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    go.name = "Confetti";
                    go.transform.SetParent(_parent, false);
                    go.transform.localScale = Vector3.one * 0.08f;
                    var collider = go.GetComponent<Collider>();
                    if (collider != null) Object.Destroy(collider);
                }
                go.SetActive(false);
                var particle = go.AddComponent<ConfettiParticle>();
                particle.OnReturnToPool += Return;
                _available.Push(particle);
            }
        }

        public ConfettiParticle Get()
        {
            if (_available.Count == 0) return null;
            var particle = _available.Pop();
            particle.gameObject.SetActive(true);
            _active.Add(particle);
            return particle;
        }

        private void Return(ConfettiParticle particle)
        {
            _active.Remove(particle);
            particle.gameObject.SetActive(false);
            _available.Push(particle);
        }

        public void Dispose()
        {
            foreach (var p in _available) { if (p != null) Object.Destroy(p.gameObject); }
            foreach (var p in _active) { if (p != null) Object.Destroy(p.gameObject); }
            _available.Clear();
            _active.Clear();
        }
    }

    [RequireComponent(typeof(Renderer))]
    public class ConfettiParticle : MonoBehaviour
    {
        public System.Action<ConfettiParticle> OnReturnToPool;

        private Vector3 _velocity;
        private float _life;
        private float _startLife;
        private Renderer _renderer;
        private MaterialPropertyBlock _propBlock;
        private static readonly int _colorId = Shader.PropertyToID("_Color");

        private void Awake()
        {
            _renderer = GetComponent<Renderer>();
            _propBlock = new MaterialPropertyBlock();
        }

        public void Init(Vector3 vel, float life)
        {
            _velocity = vel;
            _life = life;
            _startLife = life;

            // Random color via MaterialPropertyBlock (no new Material allocation)
            Color color = Random.ColorHSV(0f, 1f, 0.7f, 1f, 0.9f, 1f);
            _renderer.GetPropertyBlock(_propBlock);
            _propBlock.SetColor(_colorId, color);
            _renderer.SetPropertyBlock(_propBlock);

            transform.localScale = Vector3.one * 0.08f;
            transform.localRotation = Quaternion.identity;
        }

        private void Update()
        {
            _life -= Time.deltaTime;
            if (_life <= 0f)
            {
                OnReturnToPool?.Invoke(this);
                return;
            }
            _velocity.y -= 9.81f * Time.deltaTime;
            transform.localPosition += _velocity * Time.deltaTime;
            transform.Rotate(Vector3.one * 200f * Time.deltaTime);
            float t = _life / _startLife;
            transform.localScale = Vector3.one * 0.08f * t;
        }
    }

    public class ConfettiMediator : Mediator<ConfettiView>
    {
        protected override void OnBind()
        {
            Subscribe<LevelCompletedSignal>(_ => View.Burst());
        }
    }
}
