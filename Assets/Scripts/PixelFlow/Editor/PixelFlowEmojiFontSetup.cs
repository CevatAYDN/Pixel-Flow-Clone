#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using TMPro;
using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace PixelFlow.EditorTools
{
    /// <summary>
    /// Creates a TMP font asset from a system emoji font and sets it as fallback
    /// on LiberationSans SDF so emoji glyphs render instead of showing □.
    /// 
    /// Usage: Tools > PixelFlow > Setup Emoji Fallback Font
    /// 
    /// NOTE: Color emoji fonts (Segoe UI Emoji, Apple Color Emoji) use bitmap
    /// glyphs that don't convert well to SDF. For best results, use an outline-based
    /// emoji font like Noto Emoji (download from https://fonts.google.com/noto/specimen/Noto+Emoji)
    /// or Noto Sans Symbols 2.
    /// </summary>
    public static class PixelFlowEmojiFontSetup
    {
        private const string EmojiFontAssetPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/Emoji Fallback.asset";
        private const string SystemFontCopyPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/SystemEmoji.ttf";

        [MenuItem("Tools/PixelFlow/Setup Emoji Fallback Font")]
        public static void SetupEmojiFallback()
        {
            // Step 1: Ensure emoji support is enabled in TMP Settings (always runs)
            EnableEmojiSupport();

            // Step 2: Try to find and use a system emoji font
            if (TrySetupAutomatic())
                return;

            // Step 3: If automatic failed, show manual instructions
            ShowManualInstructions();
        }

        private static bool TrySetupAutomatic()
        {
            string emojiFontPath = FindSystemEmojiFont();
            if (string.IsNullOrEmpty(emojiFontPath))
            {
                Debug.LogWarning("[PixelFlowEmojiFontSetup] No system emoji font found. Falling back to manual setup.");
                return false;
            }

            Debug.Log($"[PixelFlowEmojiFontSetup] Found emoji font: {emojiFontPath}");

            // Import font file into project
            if (!File.Exists(SystemFontCopyPath))
            {
                File.Copy(emojiFontPath, SystemFontCopyPath, overwrite: true);
                AssetDatabase.ImportAsset(SystemFontCopyPath);
            }

            Font systemFont = AssetDatabase.LoadAssetAtPath<Font>(SystemFontCopyPath);
            if (systemFont == null)
            {
                Debug.LogError("[PixelFlowEmojiFontSetup] Failed to load imported emoji font.");
                return false;
            }

            // Check if font asset already exists
            TMP_FontAsset existingAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(EmojiFontAssetPath);
            if (existingAsset != null)
            {
                Debug.Log("[PixelFlowEmojiFontSetup] Emoji TMP font asset already exists — updating fallback.");
                LinkFallback(existingAsset);
                return true;
            }

            // Try to create TMP font asset
            TMP_FontAsset emojiFontAsset = CreateEmojiFontAsset(systemFont);
            if (emojiFontAsset == null)
            {
                Debug.LogWarning("[PixelFlowEmojiFontSetup] Could not create SDF font from this emoji font. " +
                    "Color emoji fonts (like Segoe UI Emoji) often use bitmap glyphs incompatible with SDF rendering. " +
                    "Falling back to manual setup.");
                return false;
            }

            // Verify glyphs were actually generated
            int glyphCount = emojiFontAsset.glyphTable?.Count ?? 0;
            if (glyphCount == 0)
            {
                Debug.LogWarning($"[PixelFlowEmojiFontSetup] Font asset created but glyph table is empty " +
                    $"(emoji font may use incompatible bitmap glyphs). Falling back to manual setup.");
                Object.DestroyImmediate(emojiFontAsset, true);
                return false;
            }

            Debug.Log($"[PixelFlowEmojiFontSetup] Created emoji TMP font asset with {glyphCount} glyphs.");

            // Save the asset
            AssetDatabase.CreateAsset(emojiFontAsset, EmojiFontAssetPath);

            // Save material if it exists (clean up stale files first)
            if (emojiFontAsset.material != null)
            {
                string matPath = Path.Combine(
                    Path.GetDirectoryName(EmojiFontAssetPath),
                    "Emoji Fallback.mat"
                );
                // Delete stale material from previous failed runs
                if (File.Exists(matPath))
                    AssetDatabase.DeleteAsset(matPath);
                AssetDatabase.CreateAsset(emojiFontAsset.material, matPath);
            }

            AssetDatabase.SaveAssets();
            LinkFallback(emojiFontAsset);
            EnableEmojiSupport();

            EditorUtility.DisplayDialog("Emoji Fallback Setup Complete",
                $"Created emoji TMP font asset from:\n{Path.GetFileName(emojiFontPath)}\n\n" +
                $"Glyphs generated: {glyphCount}\n\n" +
                "LiberationSans SDF will now use this emoji font to render emoji.\n" +
                "If some emoji still show □, try downloading Noto Emoji (an outline-based emoji font)\n" +
                "from https://fonts.google.com/noto/specimen/Noto+Emoji and re-run this tool.",
                "OK");

            return true;
        }

        private static TMP_FontAsset CreateEmojiFontAsset(Font font)
        {
            try
            {
                // Try SDFAA rendering first (works with outline-based emoji fonts)
                TMP_FontAsset asset = TMP_FontAsset.CreateFontAsset(
                    font,
                    90,                         // font size
                    5,                          // padding
                    UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA,
                    1024,                       // atlas width
                    1024                        // atlas height
                );

                // If no glyphs generated, try with different settings
                if (asset == null || asset.glyphTable == null || asset.glyphTable.Count == 0)
                {
                    Debug.Log("[PixelFlowEmojiFontSetup] SDFAA produced no glyphs, trying smoothed mode...");
                    if (asset != null) Object.DestroyImmediate(asset, true);

                    asset = TMP_FontAsset.CreateFontAsset(
                        font,
                        90,
                        5,
                        UnityEngine.TextCore.LowLevel.GlyphRenderMode.SMOOTH,
                        1024,
                        1024
                    );
                }

                return asset;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[PixelFlowEmojiFontSetup] Error creating font asset: {ex.Message}");
                return null;
            }
        }

        private static void LinkFallback(TMP_FontAsset emojiFont)
        {
            string mainFontPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";
            TMP_FontAsset mainFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(mainFontPath);

            if (mainFont == null)
            {
                Debug.LogError("[PixelFlowEmojiFontSetup] LiberationSans SDF font asset not found at: " + mainFontPath);
                return;
            }

            // Check if already linked
            bool alreadyLinked = mainFont.fallbackFontAssetTable != null &&
                mainFont.fallbackFontAssetTable.Any(f => f != null && f.name == emojiFont.name);

            if (!alreadyLinked)
            {
                if (mainFont.fallbackFontAssetTable == null)
                    mainFont.fallbackFontAssetTable = new List<TMP_FontAsset>();

                mainFont.fallbackFontAssetTable.Add(emojiFont);
                EditorUtility.SetDirty(mainFont);
                AssetDatabase.SaveAssets();
                Debug.Log("[PixelFlowEmojiFontSetup] ✓ Emoji fallback linked to LiberationSans SDF.");
            }
            else
            {
                Debug.Log("[PixelFlowEmojiFontSetup] Emoji fallback already linked.");
            }
        }

        private static void EnableEmojiSupport()
        {
            var tmpSettings = Resources.Load<TMP_Settings>("TMP Settings");
            if (tmpSettings == null)
            {
                Debug.LogWarning("[PixelFlowEmojiFontSetup] TMP Settings not found.");
                return;
            }

            var so = new SerializedObject(tmpSettings);
            bool changed = false;

            var emojiProp = so.FindProperty("m_enableEmojiSupport");
            if (emojiProp != null && !emojiProp.boolValue)
            {
                emojiProp.boolValue = true;
                changed = true;
            }

            if (changed)
            {
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(tmpSettings);
                AssetDatabase.SaveAssets();
                Debug.Log("[PixelFlowEmojiFontSetup] ✓ Emoji support enabled in TMP Settings.");
            }
        }

        private static string FindSystemEmojiFont()
        {
            // Windows — Segoe UI Emoji
            string winDir = System.Environment.GetEnvironmentVariable("WINDIR");
            if (!string.IsNullOrEmpty(winDir))
            {
                string fontsDir = Path.Combine(winDir, "Fonts");
                string[] candidates = {
                    Path.Combine(fontsDir, "Segoe UI Emoji.ttf"),
                    Path.Combine(fontsDir, "seguiemj.ttf"),
                    Path.Combine(fontsDir, "Segoe UI Symbol.ttf"),
                };
                foreach (var path in candidates)
                    if (File.Exists(path)) return path;
            }

            // macOS — Apple Color Emoji
            string[] macCandidates = {
                "/System/Library/Fonts/Apple Color Emoji.ttc",
                "/System/Library/Fonts/Apple Color Emoji.ttf",
            };
            foreach (var path in macCandidates)
                if (File.Exists(path)) return path;

            // Linux — Noto Emoji
            string[] linuxCandidates = {
                "/usr/share/fonts/truetype/noto/NotoEmoji-Regular.ttf",
                "/usr/share/fonts/opentype/noto/NotoEmoji-Regular.ttf",
                "/usr/share/fonts/noto/NotoEmoji-Regular.ttf",
            };
            foreach (var path in linuxCandidates)
                if (File.Exists(path)) return path;

            return null;
        }

        private static void ShowManualInstructions()
        {
            string instructions =
                "To manually set up an emoji fallback font:\n\n" +
                "1. Download an outline-based emoji font (recommended):\n" +
                "   • Noto Emoji: https://fonts.google.com/noto/specimen/Noto+Emoji\n" +
                "   • Noto Sans Symbols 2: https://fonts.google.com/noto/specimen/Noto+Sans+Symbols+2\n\n" +
                "2. Import the .ttf into your Unity project\n\n" +
                "3. Window → TextMeshPro → Font Asset Creator\n" +
                "   • Source Font: select the imported .ttf\n" +
                "   • Rendering Mode: SDFAA\n" +
                "   • Character Set: Unicode Range (Hex)\n" +
                "   • Unicode Range: 1F300-1F9FF (Emoticons & Emoji)\n" +
                "   • Generate Font Asset\n\n" +
                "4. Select LiberationSans SDF in the Project window\n" +
                "   • In Inspector → Fallback Font Assets → click +\n" +
                "   • Drag the generated emoji font asset into the slot\n\n" +
                "5. Edit → Project Settings → TextMesh Pro\n" +
                "   • Enable 'Enable Emoji Support'";

            EditorUtility.DisplayDialog(
                "Emoji Font — Manual Setup Required",
                instructions,
                "OK"
            );

            Debug.Log("[PixelFlowEmojiFontSetup] Manual setup instructions:\n" + instructions);
        }
    }
}
#endif
