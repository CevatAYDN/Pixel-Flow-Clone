using System;
using System.Threading;
using System.Threading.Tasks;
using Nexus.Core;
using PixelFlow.Models;
using UnityEngine;

namespace PixelFlow.Services
{
    /// <summary>
    /// GDD §10.3: "Her işlem sonrası (Local + Cloud)" kayıt gereksinimini throttle
    /// ederek uygular. ProcessInputCommand, PlaceViaductCommand, UseHintCommand
    /// ve benzer per-input mutasyonlar bu servise çağrı yapar; save işlemi en fazla
    /// her 2 saniyede bir gerçekten diske yazılır.
    /// </summary>
    public interface ISaveThrottler
    {
        void TryRequestSave(IGridModel grid, IGameSessionModel session, ILevelModel level);
        void ForceSave(IGridModel grid, IGameSessionModel session, ILevelModel level);
        float SecondsSinceLastSave { get; }
    }

    public class SaveThrottler : ISaveThrottler, INexusService
    {
        private const float ThrottleSeconds = 2f;

        private IGridModel _lastGrid;
        private IGameSessionModel _lastSession;
        private ILevelModel _lastLevel;
        private float _lastSaveTime = -999f;
        private bool _pendingSave;

        public float SecondsSinceLastSave =>
            _lastSaveTime < 0f ? 999f : Time.realtimeSinceStartup - _lastSaveTime;

        public ValueTask InitializeAsync(CancellationToken ct) => default;

        public void OnDispose()
        {
            FlushPending();
            _pendingSave = false;
        }

        public void TryRequestSave(IGridModel grid, IGameSessionModel session, ILevelModel level)
        {
            if (grid == null || session == null || level == null) return;

            _lastGrid = grid;
            _lastSession = session;
            _lastLevel = level;

            if (SecondsSinceLastSave >= ThrottleSeconds)
            {
                Flush();
            }
            else
            {
                _pendingSave = true;
            }
        }

        public void Tick()
        {
            if (_pendingSave && SecondsSinceLastSave >= ThrottleSeconds)
            {
                Flush();
            }
        }

        private void FlushPending()
        {
            if (_pendingSave) Flush();
        }

        public void ForceSave(IGridModel grid, IGameSessionModel session, ILevelModel level)
        {
            if (grid == null || session == null || level == null) return;
            _lastGrid = grid;
            _lastSession = session;
            _lastLevel = level;
            Flush();
        }

        /// <summary>
        /// Application pause/quit sırasında çağrılır. Pending save varsa diske yaz.
        /// </summary>
        public void Flush()
        {
            if (_lastGrid == null || _lastSession == null || _lastLevel == null) return;
            try
            {
                GridStateSerializer.Save(_lastGrid, _lastSession, _lastLevel);
                _lastSaveTime = Time.realtimeSinceStartup;
                _pendingSave = false;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SaveThrottler] Save failed: {ex.Message}");
            }
        }
    }
}
