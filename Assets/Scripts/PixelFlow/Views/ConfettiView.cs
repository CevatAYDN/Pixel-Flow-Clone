using UnityEngine;
using PixelFlow.Signals;
using Nexus.Core;

namespace PixelFlow.Views
{
    [Mediator(typeof(ConfettiMediator))]
    public class ConfettiView : View
    {
        [SerializeField] private int _particleCount = 80;

        public void Burst()
        {
            for (int i = 0; i < _particleCount; i++)
            {
                GameObject p = GameObject.CreatePrimitive(PrimitiveType.Cube);
                p.name = "Confetti";
                p.transform.SetParent(transform, false);
                p.transform.localScale = Vector3.one * 0.08f;
                float angle = Random.Range(0f, Mathf.PI * 2f);
                float speed = Random.Range(2f, 5f);
                Vector3 dir = new Vector3(Mathf.Cos(angle) * speed, Random.Range(3f, 6f), Mathf.Sin(angle) * speed);
                p.GetComponent<Renderer>().material.color = Random.ColorHSV(0f, 1f, 0.7f, 1f, 0.9f, 1f);
                p.AddComponent<ConfettiParticle>().Init(dir, Random.Range(1.2f, 2.4f));
                Destroy(p.GetComponent<Collider>());
            }
        }
    }

    public class ConfettiParticle : MonoBehaviour
    {
        private Vector3 _velocity;
        private float _life;
        private float _startLife;

        public void Init(Vector3 vel, float life)
        {
            _velocity = vel;
            _life = life;
            _startLife = life;
        }

        private void Update()
        {
            _life -= Time.deltaTime;
            if (_life <= 0f)
            {
                Destroy(gameObject);
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
