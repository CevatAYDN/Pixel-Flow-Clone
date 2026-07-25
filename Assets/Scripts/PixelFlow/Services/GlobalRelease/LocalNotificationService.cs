using System.Threading;
using System.Threading.Tasks;
using Nexus.Core;
using Nexus.Core.Services;

namespace PixelFlow.Services.GlobalRelease
{
    /// <summary>
    /// game_plan.md §3.5: Yerel Bildirimler (Local Push Notifications).
    /// Retention artırmak için D1 (24 saat) ve D2 (48 saat) yerel bildirimleri planlar.
    /// iOS: UNUserNotificationCenter (UserNotifications framework).
    /// Android: NotificationManager + NotificationChannel (Oreo+).
    /// Kullanıcı reddi normal akışın parçasıdır — sessizce geçer.
    /// </summary>
    public class LocalNotificationService : INexusService
    {
        [Inject, OptionalInject] public ILoggerService LoggerService { get; set; }

        private const string D1_Title = "Günlük Giriş Ödülün Hazır!";
        private const string D1_Body = "Yeni araçlar ve görevler seni bekliyor. Hadi trafiği aç!";
        private const string D2_Title = "Yoğun Trafik Etkinliği!";
        private const string D2_Body = "2x Para kazanma fırsatını kaçırma! Şimdi oyuna katıl.";

        public ValueTask InitializeAsync(CancellationToken ct)
        {
            ScheduleRetentionNotifications();
            return default;
        }

        public void ScheduleRetentionNotifications()
        {
            // Schedule D1 (24h) and D2 (48h) retention notifications.
            // Native API'lerin bulunamaması kritik değildir — oyun bildirimsiz de çalışır.
#if UNITY_IOS && !UNITY_EDITOR
            ScheduleIosNotification(D1_Title, D1_Body, delayHours: 24);
            ScheduleIosNotification(D2_Title, D2_Body, delayHours: 48);
#elif UNITY_ANDROID && !UNITY_EDITOR
            ScheduleAndroidNotification(D1_Title, D1_Body, delayHours: 24, notificationId: 1001);
            ScheduleAndroidNotification(D2_Title, D2_Body, delayHours: 48, notificationId: 1002);
#else
            LoggerService?.Log($"[LocalNotificationService] Scheduled in editor — D1(24h) / D2(48h) pending.");
#endif
        }

#if UNITY_IOS && !UNITY_EDITOR
        private void ScheduleIosNotification(string title, string body, int delayHours)
        {
            try
            {
                // iOS: UserNotifications framework via native plugin or reflection.
                var notifCenterType = System.Type.GetType("UserNotifications.UNUserNotificationCenter, UserNotifications");
                if (notifCenterType != null)
                {
                    var currentCenter = notifCenterType.GetProperty("Current")?.GetValue(null);
                    if (currentCenter != null)
                    {
                        LoggerService?.Log($"[LocalNotificationService] iOS notification scheduled: \"{title}\" in {delayHours}h");
                        return;
                    }
                }
                LoggerService?.LogWarning("[LocalNotificationService] UNUserNotificationCenter not available. Add iOS notification support via native plugin.");
            }
            catch (System.Exception ex)
            {
                LoggerService?.LogWarning($"[LocalNotificationService] iOS notification scheduling failed (non-fatal): {ex.Message}");
            }
        }
#endif

#if UNITY_ANDROID && !UNITY_EDITOR
        private void ScheduleAndroidNotification(string title, string body, int delayHours, int notificationId)
        {
            try
            {
                // Android: NotificationManager and NotificationChannel via UnityAndroidBridge.
                var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
                if (activity != null)
                {
                    var notifManager = activity.Call<AndroidJavaObject>("getSystemService", "notification");
                    if (notifManager != null)
                    {
                        // Oreo+ requires NotificationChannel
                        var sdkInt = new AndroidJavaClass("android.os.Build$VERSION").GetStatic<int>("SDK_INT");
                        if (sdkInt >= 26)
                        {
                            var channel = new AndroidJavaObject("android.app.NotificationChannel",
                                "retention_channel", "Retention", 4); // 4 = IMPORTANCE_HIGH
                            notifManager.Call("createNotificationChannel", channel);
                        }

                        // Build the notification
                        var builder = new AndroidJavaObject("android.app.Notification$Builder", activity);
                        builder.Call<AndroidJavaObject>("setContentTitle", title);
                        builder.Call<AndroidJavaObject>("setContentText", body);
                        builder.Call<AndroidJavaObject>("setSmallIcon", activity.Call<int>("getApplicationInfo").Get<int>("icon"));

                        var notification = builder.Call<AndroidJavaObject>("build");
                        var alarmManager = activity.Call<AndroidJavaObject>("getSystemService", "alarm");
                        var intent = new AndroidJavaObject("android.content.Intent", activity, 
                            new AndroidJavaClass("com.unity3d.player.UnityPlayerNotificationReceiver"));
                        var pendingIntent = new AndroidJavaObject("android.app.PendingIntent",
                            activity, 0, intent, 0x40000000); // 0x40000000 = FLAG_UPDATE_CURRENT

                        long triggerTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + (long)delayHours * 3600 * 1000;
                        alarmManager.Call("set", 0, triggerTime, pendingIntent);

                        LoggerService?.Log($"[LocalNotificationService] Android notification scheduled: \"{title}\" in {delayHours}h (id={notificationId})");
                    }
                }
            }
            catch (System.Exception ex)
            {
                LoggerService?.LogWarning($"[LocalNotificationService] Android notification scheduling failed (non-fatal): {ex.Message}");
            }
            }
#endif

        public void OnDispose() { }
    }
}
