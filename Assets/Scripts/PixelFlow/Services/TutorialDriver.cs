using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Nexus.Core;
using PixelFlow.Data;
using PixelFlow.Models;
using UnityEngine;

namespace PixelFlow.Services
{
    public interface ITutorialDriver
    {
        void StartStep(TutorialStep step);
        void CompleteCurrentStep();
        void OnLevelLoaded(int levelIndex);
        bool IsStepActive(TutorialStep step);
        TutorialStep CurrentStep { get; }
    }

    /// <summary>
    /// GDD §8: Tutorial adımlarını yöneten servis. level_index'e göre
    /// otomatik StartStep tetikler. PlayerPrefs'te hangi step'lerin
    /// tamamlandığını saklar (kalıcı).
    /// </summary>
    public class TutorialDriver : ITutorialDriver, INexusService
    {
        [Inject] public ITutorialModel TutorialModel { get; set; }
        [Inject] public IPlayerPrefsService PlayerPrefsService { get; set; }

        private const string PrefKey = "PF_TutorialCompleted";
        private HashSet<int> _completedSteps = new HashSet<int>();

        public TutorialStep CurrentStep => TutorialModel?.CurrentStep ?? TutorialStep.None;

        public ValueTask InitializeAsync(CancellationToken ct)
        {
            int packed = PlayerPrefsService?.GetInt(PrefKey, 0) ?? 0;
            _completedSteps = UnpackSteps(packed);
            return default;
        }

        public void OnDispose() { Persist(); }

        public bool IsStepActive(TutorialStep step)
        {
            return TutorialModel != null && TutorialModel.IsActive && TutorialModel.CurrentStep == step;
        }

        public void StartStep(TutorialStep step)
        {
            if (_completedSteps.Contains((int)step)) return;
            TutorialModel?.StartStep(step);
        }

        public void CompleteCurrentStep()
        {
            if (TutorialModel == null) return;
            var step = TutorialModel.CurrentStep;
            _completedSteps.Add((int)step);
            TutorialModel.CompleteStep(step);
            Persist();
        }

        public void OnLevelLoaded(int levelIndex)
        {
            // GDD §8.1 — Seviyeye göre tutorial adımı tetikle.
            TutorialStep trigger = MapLevelToStep(levelIndex);
            if (trigger != TutorialStep.None)
            {
                StartStep(trigger);
            }
        }

        public static TutorialStep MapLevelToStep(int levelIndex)
        {
            // 0-based level index. Seviye 1 = idx 0.
            switch (levelIndex)
            {
                case 0:  return TutorialStep.TouchAndDrag;
                case 1:  return TutorialStep.ColorMatch;
                case 2:  return TutorialStep.VehicleFlow;
                case 3:  return TutorialStep.LevelComplete;
                case 4:  return TutorialStep.ReturnToHub;
                case 5:
                case 6:
                case 7:  return TutorialStep.TaxCollect;
                case 8:
                case 9:
                case 10:
                case 11: return TutorialStep.SecondColor;
                case 12: return TutorialStep.CrashIntro;     // Seviye 13 GDD
                case 13: return TutorialStep.ViaductIntro;
                case 14:
                case 15:
                case 16: return TutorialStep.ViaductIntro;
                case 17: return TutorialStep.UndoIntro;
                case 19: return TutorialStep.OneWayIntro;   // Seviye 20 GDD (viyadüğe alternatif)
                case 28: return TutorialStep.ObstacleIntro; // Seviye 29 GDD
                default: return TutorialStep.None;
            }
        }

        private void Persist()
        {
            int packed = PackSteps(_completedSteps);
            PlayerPrefsService?.SetInt(PrefKey, packed);
        }

        // 32 step bitmask'e sığar; basit liste olarak serialize et.
        private static int PackSteps(HashSet<int> steps)
        {
            int result = steps.Count;
            int shift = 0;
            foreach (var s in steps)
            {
                result |= (s + 1) << shift;
                shift += 5;
                if (shift >= 28) break;
            }
            return result;
        }

        private static HashSet<int> UnpackSteps(int packed)
        {
            var set = new HashSet<int>();
            int count = packed & 0x1F;
            int shift = 5;
            for (int i = 0; i < count; i++)
            {
                int val = (packed >> shift) & 0x1F;
                if (val > 0) set.Add(val - 1);
                shift += 5;
                if (shift >= 32) break;
            }
            return set;
        }
    }
}
