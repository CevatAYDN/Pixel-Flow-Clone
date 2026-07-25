using System;
using System.Threading;
using System.Threading.Tasks;
using Nexus.Core;

namespace PixelFlow.Services
{
    public interface IDailyLoginStreakService : INexusService
    {
        void CheckDailyLogin();
        int GetCurrentStreak();
        DateTime? GetLastLoginTime();
        void ResetStreak();
    }
}