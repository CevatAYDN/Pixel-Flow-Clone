#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

namespace PixelFlow.Editor
{
    public partial class PixelFlowSetupWindow : EditorWindow
    {
        private readonly string[] _tabNames = {
            "🕹️ Oyun Kontrol",
            "🔍 Sahne Tanılama",
            "🎮 Seviye Stüdyosu",
            "🧩 Toplu Çözücü",
            "📦 Data Yöneticisi",
            "💰 Ekonomi & Isı Haritası",
            "🔬 Nexus",
            "⚡ Performans",
            "🎨 Garaj & Skin Stüdyosu",
            "📺 Reklam & Monetization",
            "🛡️ Pre-Build Validator",
            "🧰 Araçlar"
        };

        private void RenderSelectedTabContent(int idx)
        {
            switch (idx)
            {
                case 0: BuildGameControlTab(_content); break;
                case 1: BuildSceneDiagnosticsTab(_content); break;
                case 2: BuildLevelTab(_content); break;
                case 3: BuildBatchSolverTab(_content); break;
                case 4: BuildDataTab(_content); break;
                case 5: BuildEconomyHeatmapTab(_content); break;
                case 6: BuildNexusTab(_content); break;
                case 7: BuildPerformanceTab(_content); break;
                case 8: BuildGarageTab(_content); break;
                case 9: BuildAdMonetizationTab(_content); break;
                case 10: BuildValidatorTab(_content); break;
                case 11: BuildToolsTab(_content); break;
                default: BuildGameControlTab(_content); break;
            }
        }
    }
}
#endif
