using System;
using System.Threading;
using System.Threading.Tasks;
using Nexus.Core;
using Nexus.Core.Services;
using PixelFlow.Data;
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
        private readonly StorageKeysConfigAsset _storageKeys;

        [Inject] public GameConfig Config { get; set; }

        public DailyCrisisModel(IPlayerPrefsService prefs, StorageKeysConfigAsset storageKeys = null)
        {
            _prefs = prefs ?? throw new System.ArgumentNullException(nameof(prefs));
            _storageKeys = storageKeys;
            LoadState();
        }

        // game_plan.md §2.2: kriz hedef skorları GameConfig'ten gelir. Build'de config yoksa fail-loud;
        // editor/testte SO varsayılan instance'ı (cache'li).
        private GameConfig _resolvedConfig;
        private GameConfig ResolveConfig()
        {
            if (Config != null) return Config;
            throw new DataValidationException("GameConfig erişilemedi! DailyCrisisModel kriz skorları yüklenemiyor. GameContextLifecycle'da GameConfig yüklü olmalı.");
        }

        public ValueTask OnBind(CancellationToken ct) => default;

        public int CurrentDailySeed => GetTodayUtcSeed();
        public int StreakCount { get; private set; }
        public int BadgesEarned { get; private set; }

        private bool[] _completedDaily = new bool[3];
        private int _lastCompletedSeed = 0;

        public event Action OnDailyCrisisUpdated;

        private string KeyStreak => _storageKeys != null && !string.IsNullOrEmpty(_storageKeys.KeyCrisisStreak) ? _storageKeys.KeyCrisisStreak : throw new DataValidationException("StorageKeysConfigAsset.KeyCrisisStreak missing!");
        private string KeyBadges => _storageKeys != null && !string.IsNullOrEmpty(_storageKeys.KeyCrisisBadges) ? _storageKeys.KeyCrisisBadges : throw new DataValidationException("StorageKeysConfigAsset.KeyCrisisBadges missing!");
        private string KeySeed => _storageKeys != null && !string.IsNullOrEmpty(_storageKeys.KeyCrisisSeed) ? _storageKeys.KeyCrisisSeed : throw new DataValidationException("StorageKeysConfigAsset.KeyCrisisSeed missing!");
        private string KeyFlags => _storageKeys != null && !string.IsNullOrEmpty(_storageKeys.KeyCrisisFlags) ? _storageKeys.KeyCrisisFlags : throw new DataValidationException("StorageKeysConfigAsset.KeyCrisisFlags missing!");

        private void LoadState()
        {
            StreakCount = _prefs.GetInt(KeyStreak, 0);
            BadgesEarned = _prefs.GetInt(KeyBadges, 0);
            _lastCompletedSeed = _prefs.GetInt(KeySeed, 0);

            int currentSeed = GetTodayUtcSeed();
            if (_lastCompletedSeed != currentSeed)
            {
                _completedDaily[0] = false;
                _completedDaily[1] = false;
                _completedDaily[2] = false;

                if (_lastCompletedSeed > 0 && currentSeed - _lastCompletedSeed > 1)
                {
                    StreakCount = 0;
                    _prefs.SetInt(KeyStreak, 0);
                }
            }
            else
            {
                int flags = _prefs.GetInt(KeyFlags, 0);
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

            _prefs.SetInt(KeyBadges, BadgesEarned);
            _prefs.SetInt(KeyStreak, StreakCount);
            _prefs.SetInt(KeySeed, todaySeed);
            _prefs.SetInt(KeyFlags, flags);
            _prefs.Save();

            OnDailyCrisisUpdated?.Invoke();
        }

        public int GetCrisisScore(int crisisIndex)
        {
            var scores = ResolveConfig().DailyCrisisTargetScores;
            if (scores != null && crisisIndex >= 0 && crisisIndex < scores.Length)
                return scores[crisisIndex];
            return scores != null && scores.Length > 0 ? scores[0] : 0;
        }

        private static int GetTodayUtcSeed()
        {
            DateTime now = DateTime.UtcNow;
            return now.Year * 10000 + now.Month * 100 + now.Day;
        }
    }
}
