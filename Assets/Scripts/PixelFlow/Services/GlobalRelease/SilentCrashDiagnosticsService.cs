using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Nexus.Core;
using Nexus.Core.Services;
using PixelFlow.Data;

namespace PixelFlow.Services.GlobalRelease
{
    /// <summary>
    /// game_plan.md §3.2: Hata & Crash İzleme (Silent Crash Diagnostics).
    /// Canlıda unhandled exception'ları ve hataları arka planda toplar.
    /// Gerçek crash reporter (Firebase Crashlytics / Sentry) entegrasyonu
    /// için ICrashReporter arayüzü üzerinden genişletilebilir.
    /// </summary>
    public interface ICrashReporter : IDisposable
    {
        void LogException(Exception exception);
        void LogMessage(string condition, string stackTrace, LogType type);
        void SetUserId(string userId);
        void SetCustomKey(string key, string value);
    }

    /// <summary>
    /// Null-object pattern for when no crash reporter SDK is installed.
    /// </summary>
    public sealed class NullCrashReporter : ICrashReporter
    {
        public void Dispose() { }
        public void LogException(Exception exception) { }
        public void LogMessage(string condition, string stackTrace, LogType type) { }
        public void SetUserId(string userId) { }
        public void SetCustomKey(string key, string value) { }
    }

    /// <summary>
    /// Varsayılan konsol crash reporter — herhangi bir SDK olmadığında
    /// Unity konsoluna yapılandırılmış log yazar.
    /// </summary>
    public sealed class ConsoleCrashReporter : ICrashReporter
    {
        private readonly ILoggerService _logger;
        private readonly bool _includeStackTraces;

        public ConsoleCrashReporter(ILoggerService logger = null, bool includeStackTraces = true)
        {
            _logger = logger;
            _includeStackTraces = includeStackTraces;
        }

        public void Dispose() { }

        public void LogException(Exception exception)
        {
            var msg = $"[CrashDiagnostics] EXCEPTION: {exception.GetType().Name}: {exception.Message}";
            if (_includeStackTraces)
                msg += $"\n{exception.StackTrace}";
            LogInternal(msg);
        }

        public void LogMessage(string condition, string stackTrace, LogType type)
        {
            if (type != LogType.Exception && type != LogType.Error) return;
            var msg = $"[CrashDiagnostics] {type}: {condition}";
            if (_includeStackTraces && !string.IsNullOrEmpty(stackTrace))
                msg += $"\n{stackTrace}";
            LogInternal(msg);
        }

        public void SetUserId(string userId) { }
        public void SetCustomKey(string key, string value) { }

        private void LogInternal(string msg)
        {
            if (_logger != null)
                _logger.Log(msg);
            else
                Debug.Log(msg);
        }
    }

    /// <summary>
    /// Silent crash diagnostics service with pluggable report backends.
    /// Subscribe to Application.logMessageReceivedThreaded and forwards
    /// to all registered ICrashReporter instances.
    /// </summary>
    public class SilentCrashDiagnosticsService : INexusService
    {
        [Inject, OptionalInject] public ILoggerService LoggerService { get; set; }
        [Inject, OptionalInject] public GameConfig Config { get; set; }

        private readonly List<ICrashReporter> _reporters = new List<ICrashReporter>();

        public SilentCrashDiagnosticsService()
        {
            // Register the console reporter as the default backend.
            // Replace with FirebaseCrashReporter / SentryCrashReporter in production.
            RegisterReporter(new ConsoleCrashReporter(null, includeStackTraces: true));
        }

        public void RegisterReporter(ICrashReporter reporter)
        {
            if (reporter != null)
                _reporters.Add(reporter);
        }

        public void ClearReporters()
        {
            foreach (var r in _reporters)
                r.Dispose();
            _reporters.Clear();
        }

        public ValueTask InitializeAsync(CancellationToken ct)
        {
            if (_reporters.Count > 0 && _reporters[0] is ConsoleCrashReporter consoleReporter)
            {
                _reporters[0] = new ConsoleCrashReporter(LoggerService, includeStackTraces: true);
            }
            Application.logMessageReceivedThreaded += OnLogMessageReceived;
            return default;
        }

        private void OnLogMessageReceived(string condition, string stackTrace, LogType type)
        {
            for (int i = 0; i < _reporters.Count; i++)
                _reporters[i].LogMessage(condition, stackTrace, type);
        }

        public void OnDispose()
        {
            Application.logMessageReceivedThreaded -= OnLogMessageReceived;
            ClearReporters();
        }
    }
}
