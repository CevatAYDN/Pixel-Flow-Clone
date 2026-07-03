using UnityEngine;
using PixelFlow.Signals;
using Nexus.Core;

namespace PixelFlow.Views
{
    [Mediator(typeof(CoinFlowMediator))]
    public class CoinFlowView : View
    {
        [SerializeField] private int _particleCount = 12;

        public void SpawnFlow()
        {
            for (int i = 0; i < _particleCount; i++)
            {
                GameObject p = GameObject.CreatePrimitive(PrimitiveType.Cube);
                p.name = "GoldParticle";
                p.transform.SetParent(transform, false);
                p.transform.localScale = Vector3.one * 0.12f;
                p.transform.position = new Vector3(Random.Range(-9f, 9f), Random.Range(5f, 6f), 0f);
                var r = p.GetComponent<Renderer>();
                if (r != null) r.material.color = new Color(1f, 0.85f, 0.24f);
                Destroy(p.GetComponent<Collider>());
                p.AddComponent<CoinFlowParticle>().Init(new Vector3(0f, -1f, 0f), Random.Range(0.8f, 1.4f));
            }
        }
    }

    public class CoinFlowParticle : MonoBehaviour
    {
        private Vector3 _target;
        private float _speed;
        private float _life;

        public void Init(Vector3 dir, float life)
        {
            _target = Vector3.zero;
            _speed = 8f;
            _life = life;
        }

        private void Update()
        {
            _life -= Time.deltaTime;
            if (_life <= 0f) { Destroy(gameObject); return; }
            Vector3 toCenter = (_target - transform.position);
            transform.position += toCenter.normalized * _speed * Time.deltaTime;
            transform.localScale = Vector3.one * 0.12f * (1f - _life * 0.5f);
        }
    }

    public class CoinFlowMediator : Mediator<CoinFlowView>
    {
        protected override void OnBind()
        {
            Subscribe<CoinCollectionSignal>(_ => View.SpawnFlow());
        }
    }
}
