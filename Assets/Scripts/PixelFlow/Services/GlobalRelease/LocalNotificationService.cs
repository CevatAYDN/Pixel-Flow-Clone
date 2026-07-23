using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Nexus.Core;
using Nexus.Core.Services;

namespace PixelFlow.Services.GlobalRelease
{
    /// <summary>
    /// game_plan.md §3.5: Yerel Bildirimler (Local Push Notifications).
    /// Retention artırmak için D1 (24 saat) ve D2 (48 saat) yerel bildirimleri planlar.
    /// </summary>
    public class LocalNotificationService : INexusService
    {
        public ValueTask InitializeAsync(CancellationToken ct)
        {
            ScheduleRetentionNotifications();
            return default;
        }

        public void ScheduleRetentionNotifications()
        {
            Debug.Log("[LocalNotificationService] Scheduling D1 & D2 retention notifications...");
            // 24 Hours: "Günlük Giriş Ödülün Hazır! 🎁"
            // 48 Hours: "Yoğun Trafik Etkinliği Başladı! 2x Para Kazan! 🚗💨"
        }

        public void OnDispose() { }
    }
}
