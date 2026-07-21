using System;
using System.Threading;
using System.Threading.Tasks;
using Nexus.Core;
using Nexus.Core.Services;
using PixelFlow.Services;
using UnityEngine;

namespace PixelFlow.Models
{
    public interface IDailyCrisisModel
    {
        int CurrentDailySeed { get; }
        int StreakCount { get; }
        int BadgesEarned { get; }
        bool IsCrisisCompleted(int crisisIndex);
        void CompleteCrisis(int crisisIndex);
        int GetCrisisScore(int crisisIndex);
        event Action OnDailyCrisisUpdated;
    }

    public class DailyCrisisModel : IDailyCrisisModel, IReactiveModel
    {
        private readonly IPlayerPrefsService _prefs;

        public DailyCrisisModel(IPlayerPrefsService prefs)
        {
            _prefs = prefs ?? throw new System.ArgumentNullException(nameof(prefs));
            LoadState();
        }

        public ValueTask OnBind(CancellationToken ct) => default;

        public int CurrentDailySeed => GetTodayUtcSeed();
        public int StreakCount { get; private set; }
        public int BadgesEarned { get; private set; }

        private bool[] _completedDaily = new bool[3];
        private int _lastCompletedSeed = 0;

        public event Action OnDailyCrisisUpdated;

        private const string PrefKeyStreak = "NT_CrisisStreak";
        private const string PrefKeyBadges = "NT_CrisisBadges";
        private const string PrefKeySeed = "NT_CrisisLastSeed";
        private const string PrefKeyFlags = "NT_CrisisFlags";

        private void LoadState()
        {
            StreakCount = _prefs.GetInt(PrefKeyStreak, 0);
            BadgesEarned = _prefs.GetInt(PrefKeyBadges, 0);
            _lastCompletedSeed = _prefs.GetInt(PrefKeySeed, 0);

            int currentSeed = GetTodayUtcSeed();
            if (_lastCompletedSeed != currentSeed)
            {
                _completedDaily[0] = false;
                _completedDaily[1] = false;
                _completedDaily[2] = false;

                if (_lastCompletedSeed > 0 && currentSeed - _lastCompletedSeed > 1)
                {
                    StreakCount = 0;
                    _prefs.SetInt(PrefKeyStreak, 0);
                }
            }
            else
            {
                int flags = _prefs.GetInt(PrefKeyFlags, 0);
                _completedDaily[0] = (flags & 1) != 0;
                _completedDaily[1] = (flags & 2) != 0;
                _completedDaily[2] = (flags & 4) != 0;
            }
        }

        public bool IsCrisisCompleted(int crisisIndex)
        {
            if (crisisIndex < 0 || crisisIndex >= 3) return false;
            return _completedDaily[crisisIndex];
        }

        public void CompleteCrisis(int crisisIndex)
        {
            if (crisisIndex < 0 || crisisIndex >= 3 || _completedDaily[crisisIndex]) return;

            int todaySeed = GetTodayUtcSeed();
            _completedDaily[crisisIndex] = true;
            BadgesEarned++;

            int flags = (_completedDaily[0] ? 1 : 0) | (_completedDaily[1] ? 2 : 0) | (_completedDaily[2] ? 4 : 0);

            if (_completedDaily[0] && _completedDaily[1] && _completedDaily[2])
            {
                StreakCount++;
            }

            _lastCompletedSeed = todaySeed;

            _prefs.SetInt(PrefKeyBadges, BadgesEarned);
            _prefs.SetInt(PrefKeyStreak, StreakCount);
            _prefs.SetInt(PrefKeySeed, todaySeed);
            _prefs.SetInt(PrefKeyFlags, flags);
            _prefs.Save();

            OnDailyCrisisUpdated?.Invoke();
        }

        public int GetCrisisScore(int crisisIndex)
        {
            switch (crisisIndex)
            {
                case 0: return 20; // Easy crisis
                case 1: return 35; // Medium crisis
                case 2: return 55; // Hard crisis
                default: return 20;
            }
        }

        private static int GetTodayUtcSeed()
        {
            DateTime now = DateTime.UtcNow;
            return now.Year * 10000 + now.Month * 100 + now.Day;
        }
    }
}
