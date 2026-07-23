using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using PixelFlow.Views;
using PixelFlow.Services;
using System.Linq;

namespace PixelFlow.Editor.Tests
{
    /// <summary>
    /// Diagnostic tests for verifying SceneSetup generates correct UI bindings,
    /// EventSystem is configured, and runtime fallbacks work.
    /// </summary>
    public class PixelFlowDiagnosticTests
    {
        // ═══════════════════════════════════════════════════════════
        // SCENE SETUP: UI Binding Integrity Tests
        // ═══════════════════════════════════════════════════════════

        [Test]
        public void SettingsView_HasAllSerializedBindings()
        {
            var settingsViews = Resources.FindObjectsOfTypeAll<SettingsView>();
            Assert.Greater(settingsViews.Length, 0, "No SettingsView found in scene.");

            var view = settingsViews[0];
            Assert.NotNull(view, "SettingsView itself is null!");

            var so = new SerializedObject(view);
            Assert.NotNull(so.FindProperty("_masterVolumeSlider"), "Missing _masterVolumeSlider prop");
            Assert.NotNull(so.FindProperty("_sfxVolumeSlider"), "Missing _sfxVolumeSlider prop");
            Assert.NotNull(so.FindProperty("_musicVolumeSlider"), "Missing _musicVolumeSlider prop");
            Assert.NotNull(so.FindProperty("_closeButton"), "Missing _closeButton prop");
            Assert.NotNull(so.FindProperty("_colorBlindNoneButton"), "Missing _colorBlindNoneButton prop");
            Assert.NotNull(so.FindProperty("_colorBlindProtanButton"), "Missing _colorBlindProtanButton prop");
            Assert.NotNull(so.FindProperty("_colorBlindDeutanButton"), "Missing _colorBlindDeutanButton prop");
            Assert.NotNull(so.FindProperty("_colorBlindTritanButton"), "Missing _colorBlindTritanButton prop");
            Assert.NotNull(so.FindProperty("_hapticsToggle"), "Missing _hapticsToggle prop");

            // Verify values are actually assigned
            Assert.NotNull(so.FindProperty("_masterVolumeSlider").objectReferenceValue, "_masterVolumeSlider not assigned!");
            Assert.NotNull(so.FindProperty("_sfxVolumeSlider").objectReferenceValue, "_sfxVolumeSlider not assigned!");
            Assert.NotNull(so.FindProperty("_musicVolumeSlider").objectReferenceValue, "_musicVolumeSlider not assigned!");
            Assert.NotNull(so.FindProperty("_closeButton").objectReferenceValue, "_closeButton not assigned!");
            Assert.NotNull(so.FindProperty("_colorBlindNoneButton").objectReferenceValue, "_colorBlindNoneButton not assigned!");
            Assert.NotNull(so.FindProperty("_colorBlindProtanButton").objectReferenceValue, "_colorBlindProtanButton not assigned!");
            Assert.NotNull(so.FindProperty("_colorBlindDeutanButton").objectReferenceValue, "_colorBlindDeutanButton not assigned!");
            Assert.NotNull(so.FindProperty("_colorBlindTritanButton").objectReferenceValue, "_colorBlindTritanButton not assigned!");
            Assert.NotNull(so.FindProperty("_hapticsToggle").objectReferenceValue, "_hapticsToggle not assigned!");
        }

        [Test]
        public void HUDView_HasGarageRainbowClearJamButtons()
        {
            var hudViews = Resources.FindObjectsOfTypeAll<HUDView>();
            Assert.Greater(hudViews.Length, 0, "No HUDView found in scene.");

            var view = hudViews[0];
            var so = new SerializedObject(view);

            var garageProp = so.FindProperty("_garageButton");
            Assert.NotNull(garageProp, "Missing _garageButton serialized property!");
            Assert.NotNull(garageProp.objectReferenceValue, "_garageButton not assigned!");

            var rainbowProp = so.FindProperty("_rainbowRoadButton");
            Assert.NotNull(rainbowProp, "Missing _rainbowRoadButton serialized property!");
            Assert.NotNull(rainbowProp.objectReferenceValue, "_rainbowRoadButton not assigned!");

            var clearJamProp = so.FindProperty("_clearJamButton");
            Assert.NotNull(clearJamProp, "Missing _clearJamButton serialized property!");
            Assert.NotNull(clearJamProp.objectReferenceValue, "_clearJamButton not assigned!");
        }

        [Test]
        public void EventSystem_IsConfiguredCorrectly()
        {
            var eventSystem = Object.FindAnyObjectByType<EventSystem>();
            Assert.NotNull(eventSystem, "No EventSystem found in scene!");

            var inputModule = eventSystem.GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            Assert.NotNull(inputModule, "InputSystemUIInputModule not found on EventSystem!");
        }

        // ═══════════════════════════════════════════════════════════
        // AUDIO FALLBACK TESTS
        // ═══════════════════════════════════════════════════════════

        [Test]
        public void AudioClipProvider_ReturnsFallbackClipForMissingFiles()
        {
            // This tests that the procedural audio fallback works
            var clip = AudioClipProvider.Load(SfxType.Crash);
            Assert.NotNull(clip, "AudioClipProvider returned null for Crash type!");
            Assert.AreEqual(clip.name, "Crash", "Fallback clip name should match the SfxType.");

            var uiClip = AudioClipProvider.Load(SfxType.UIClick);
            Assert.NotNull(uiClip, "AudioClipProvider returned null for UIClick type!");
            Assert.Greater(uiClip.samples, 0, "Fallback clip should have samples.");
        }

        // ═══════════════════════════════════════════════════════════
        // LEVEL DATA INTEGRITY TESTS
        // ═══════════════════════════════════════════════════════════

        [Test]
        public void LevelDataAssets_HaveNoMissingScriptRefs()
        {
            var allLevelAssets = AssetDatabase.FindAssets("t:ScriptableObject", new[] { "Assets/Resources/Levels" })
                .Select(guid => AssetDatabase.LoadAssetAtPath<ScriptableObject>(AssetDatabase.GUIDToAssetPath(guid)))
                .Where(so => so != null)
                .ToArray();

            Assert.Greater(allLevelAssets.Length, 0, "No LevelData assets found in Resources/Levels!");

            foreach (var asset in allLevelAssets)
            {
                var so = new SerializedObject(asset);
                var prop = so.GetIterator();
                bool hasMissingRef = false;

                while (prop.NextVisible(true))
                {
                    if (prop.propertyType == SerializedPropertyType.ObjectReference &&
                        prop.objectReferenceValue == null)
                    {
                        var entityId = prop.objectReferenceEntityIdValue;
                        if (!entityId.Equals(default(GlobalObjectId)))
                        {
                            hasMissingRef = true;
                            Debug.LogWarning($"[Test] Missing script ref found in {asset.name}: {prop.propertyPath}");
                        }
                    }
                }

                Assert.IsFalse(hasMissingRef, $"LevelData asset '{asset.name}' has missing script references! Run Tools → PixelFlow → Fix Missing Script References.");
            }
        }
    }
}
