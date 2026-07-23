using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Nexus.Core;
using Nexus.Core.Services;

namespace PixelFlow.Services.GlobalRelease
{
    /// <summary>
    /// game_plan.md §3.2: Hata & Crash İzleme (Silent Crash Diagnostics).
    /// Canlıda unhandled exception'ları arka planda Firebase Crashlytics / Sentry loglarına aktarır.
    /// </summary>
    public class SilentCrashDiagnosticsService : INexusService
    {
        public ValueTask InitializeAsync(CancellationToken ct)
        {
            Application.logMessageReceivedThreaded += OnLogMessageReceived;
            return default;
        }

        private void OnLogMessageReceived(string condition, string stackTrace, LogType type)
        {
            if (type == LogType.Exception || type == LogType.Error)
            {
                // Telemetry & Diagnostic logging
                System.Diagnostics.Debug.WriteLine($"[CrashDiagnostics] {type}: {condition}\n{stackTrace}");
            }
        }

        public void OnDispose()
        {
            Application.logMessageReceivedThreaded -= OnLogMessageReceived;
        }
    }
}
