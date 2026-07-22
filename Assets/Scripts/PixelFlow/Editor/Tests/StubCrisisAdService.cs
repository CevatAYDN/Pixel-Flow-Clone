using System.Threading;
using System.Threading.Tasks;
using Nexus.Core;
using PixelFlow.Services;

namespace PixelFlow.Editor.Tests
{
    /// <summary>
    /// Minimal no-op stub for <see cref="ICrisisAdService"/>.
    /// Satisfies DI injection in VehicleSimulator without requiring
    /// ad SDK integrations in EditMode tests.
    /// </summary>
    public sealed class StubCrisisAdService : ICrisisAdService, INexusService
    {
        public int RetryCount => 0;
        public bool IsViaductExhausted => false;

        public ValueTask InitializeAsync(CancellationToken ct) => default;
        public void OnDispose() { }

        public void RecordCrisisAttempt()
        {
            RetryCount_Internal++;
        }

        public void ResetRetryCount()
        {
            RetryCount_Internal = 0;
        }

        public void CheckViaductExhaustion()
        {
        }

        public int RetryCount_Internal { get; private set; }
    }
}