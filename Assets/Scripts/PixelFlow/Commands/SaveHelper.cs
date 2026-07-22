using System;
using Nexus.Core.Services;
using PixelFlow.Models;
using PixelFlow.Services;

namespace PixelFlow.Commands
{
    /// <summary>
    /// GridStateSerializer.Save çağrılarını SaveThrottler üzerinden batch'ler.
    /// Tüm Command'ler bu helper'ı kullanır — böylece her command'de ayrı
    /// _cachedSaveAction field'ı ve Save() metodu tanımlamak gerekmez.
    ///
    /// SaveThrottler (2s throttle) zaten aynı Action'ı üst üste eklenirse
    /// sadece sonuncuyu saklar ve throttle süresi dolunca çalıştırır.
    /// Bu helper sadece lambda/closure tekrarını ortadan kaldırır.
    /// </summary>
    internal static class SaveHelper
    {
        /// <summary>
        /// SaveThrottler üzerinden GridStateSerializer.Save çağrısını batch'ler.
        /// Her çağrıda yeni bir closure oluşturulur ancak SaveThrottler
        /// sadece son Action'ı tutar — throttle süresi boyunca gelen
        /// diğer çağrılar otomatik olarak deduplicate edilir.
        /// </summary>
        public static void TrySave(
            ISaveThrottler throttler,
            IGridModel grid,
            IGameSessionModel session,
            ILevelModel level,
            IPlayerPrefsService prefs)
        {
            if (throttler == null) return;
            throttler.TryRequestSave(() => GridStateSerializer.Save(grid, session, level, prefs));
        }
    }
}
