#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using Nexus.Core;
using PixelFlow.Models;
using PixelFlow.Services;
using PixelFlow.Signals;
using PixelFlow.Data;

namespace PixelFlow.Editor
{
    public partial class PixelFlowSetupWindow : EditorWindow
    {
        // ─── TAB 0: OYUN KONTROL ───
        private void BuildGameControlTab(VisualElement parent)
        {
            var playCard = Card("🕹️ Oyun Kontrol");
            playCard.Add(Row("Editor Modu:", StatusLabel(!Application.isPlaying, "Editör", "Oynanıyor")));
            playCard.Add(Row("Oyun Durumu:", StatusLabel(Application.isPlaying, "Oynanıyor", "Durmuş")));

            var btnRow = new VisualElement { style = { flexDirection = FlexDirection.Row, marginTop = 6 } };
            if (Application.isPlaying)
            {
                btnRow.Add(MakeBtn("⏹ Durdur", () => EditorApplication.isPlaying = false));
                btnRow.Add(MakeBtn("Level Bitir", () => { FireSignal(new LevelCompletedSignal()); }));
            }
            else
                btnRow.Add(MakeBtn("▶ Play", () => EditorApplication.isPlaying = true));
            playCard.Add(btnRow);
            parent.Add(playCard);

            var bootCard = Card("⚡ Bootstrapper");
            var boot = FindAnyObjectByType<GameBootstrapper>(FindObjectsInactive.Include);
            if (boot != null)
            {
                bool hasLevel = boot.initialLevel != null;
                string levelName = hasLevel ? boot.initialLevel.name : "Yok";
                bootCard.Add(Row("Initial Level:", StatusLabel(hasLevel, levelName)));
                var so = new SerializedObject(boot);
                var lvlProp = so.FindProperty("initialLevel");
                var field = new PropertyField(lvlProp);
                field.style.marginTop = 4;
                bootCard.Add(field);
            }
            else bootCard.Add(InfoLabel("Bootstrapper sahnede bulunamadı."));
            parent.Add(bootCard);
        }

        // ─── TAB 1: SAHNE TANILAMA ───
        private void BuildSceneDiagnosticsTab(VisualElement parent)
        {
            var diagCard = Card("🔍 Sahne Tanılama & Audit");
            int ok = 0, total = 0;
            void DiagRow(string name, bool okFlag) { diagCard.Add(Row(name, StatusLabel(okFlag))); total++; if (okFlag) ok++; }

            var boot = FindAnyObjectByType<GameBootstrapper>(FindObjectsInactive.Include);
            DiagRow("Root", FindAnyObjectByType<Root>(FindObjectsInactive.Include) != null);
            DiagRow("Canvas", FindAnyObjectByType<Canvas>(FindObjectsInactive.Include) != null);
            DiagRow("EventSystem", FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>(FindObjectsInactive.Include) != null);
            DiagRow("GameBootstrapper", boot != null);
            DiagRow("GridView", FindAnyObjectByType<Views.GridView>(FindObjectsInactive.Include) != null);
            DiagRow("HUDView", FindAnyObjectByType<Views.HUDView>(FindObjectsInactive.Include) != null);
            DiagRow("MainMenuView", FindAnyObjectByType<Views.MainMenuView>(FindObjectsInactive.Include) != null);
            DiagRow("SettingsView", FindAnyObjectByType<Views.SettingsView>(FindObjectsInactive.Include) != null);
            diagCard.Add(Row($"Toplam: {ok}/{total}", StatusLabel(ok == total, "TAMAM", "EKSİK")));
            parent.Add(diagCard);
        }

        // ─── TAB 6: NEXUS ───
        private void BuildNexusTab(VisualElement parent)
        {
            var card = Card("🔬 Nexus MVCS Core Inspector");
            var root = FindAnyObjectByType<Root>(FindObjectsInactive.Include);

            void Diag(string name, System.Func<object> getVal)
            {
                var val = getVal();
                var r = new VisualElement { style = { flexDirection = FlexDirection.Row, marginBottom = 2 } };
                var l = new Label(name); l.style.width = 200; l.style.color = new Color(0.78f, 0.82f, 0.85f); l.style.fontSize = 11;
                r.Add(l);
                var v = val is bool b ? StatusLabel(b) : new Label(val?.ToString() ?? "-") { style = { color = Color.white, fontSize = 11 } };
                r.Add(v);
                card.Add(r);
            }

            Diag("Nexus Root Instance", () => root != null);
            Diag("Context Initialized", () => root?.IsInitialized ?? false);
            Diag("Container Active", () => root?.Context?.Container != null);
            Diag("SignalBus Bound", () => root?.Context?.Container?.Resolve<ISignalBus>() != null);
            Diag("IGridModel Bound", () => root?.Context?.Container?.Resolve<IGridModel>() != null);
            Diag("IGameStateModel Bound", () => root?.Context?.Container?.Resolve<IGameStateModel>() != null);
            Diag("IProgressModel Bound", () => root?.Context?.Container?.Resolve<IProgressModel>() != null);
            Diag("IInventoryModel Bound", () => root?.Context?.Container?.Resolve<IInventoryModel>() != null);
            Diag("IDailyCrisisModel Bound", () => root?.Context?.Container?.Resolve<IDailyCrisisModel>() != null);

            parent.Add(card);
        }

        // ─── TAB 7: PERFORMANS ───
        private void BuildPerformanceTab(VisualElement parent)
        {
            var card = Card("⚡ Performans & Bütçe Denetimi (<80 DC, <100k Tris, <1KB GC)");
            card.Add(Row("Target Frame Rate:", new Label(Application.targetFrameRate.ToString()) { style = { color = Color.white, fontSize = 11 } }));
            card.Add(Row("Quality Level:", new Label(QualitySettings.names[QualitySettings.GetQualityLevel()]) { style = { color = Color.white, fontSize = 11 } }));
            card.Add(Row("System Memory:", new Label($"{SystemInfo.systemMemorySize} MB") { style = { color = Color.white, fontSize = 11 } }));
            card.Add(Row("Graphics Memory:", new Label($"{SystemInfo.graphicsMemorySize} MB") { style = { color = Color.white, fontSize = 11 } }));
            card.Add(Row("GC Allocated (Current):", new Label($"{System.GC.GetTotalMemory(false) / 1024} KB") { style = { color = Color.white, fontSize = 11 } }));
            parent.Add(card);
        }
    }
}
#endif
