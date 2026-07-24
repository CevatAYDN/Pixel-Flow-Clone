using UnityEngine;
using PixelFlow.Services;

namespace PixelFlow.Editor.Tests
{
    /// <summary>
    /// Minimal no-op stub for <see cref="IGridViewProvider"/>.
    /// Satisfies DI injection in VehicleSimulator without requiring
    /// a live GridView in EditMode tests.
    /// </summary>
    public sealed class StubGridViewProvider : IGridViewProvider
    {
        public Transform GridTransform => null;
    }
}
