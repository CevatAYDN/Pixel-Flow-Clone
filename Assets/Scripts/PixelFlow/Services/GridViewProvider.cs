using UnityEngine;
using Nexus.Core;
using PixelFlow.Views;
using System.Threading;
using System.Threading.Tasks;

namespace PixelFlow.Services
{
    /// <summary>
    /// IGridViewProvider implementasyonu. GridView sahne referansını cache'ler.
    /// game_plan.md §15.9 KURAL 8: FindAnyObjectByType yalnızca bu provider içinde,
    /// diğer servisler DI üzerinden erişir.
    /// </summary>
    public class GridViewProvider : IGridViewProvider, INexusService
    {
        private Transform _cached;

        public Transform GridTransform
        {
            get
            {
                if (_cached == null)
                {
                    var gridView = Object.FindAnyObjectByType<GridView>();
                    _cached = gridView != null ? gridView.transform : null;
                }
                return _cached;
            }
        }

        public ValueTask InitializeAsync(CancellationToken ct) => default;
        public void OnDispose() { _cached = null; }
    }
}
