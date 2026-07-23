using UnityEngine;

namespace PixelFlow.Services
{
    /// <summary>
    /// GridView sahne referansını DI üzerinden sağlar.
    /// game_plan.md §15.9 KURAL 8: GameObject.Find yerine DI kullanılır.
    /// </summary>
    public interface IGridViewProvider
    {
        Transform GridTransform { get; }
    }
}
