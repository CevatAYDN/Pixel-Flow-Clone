#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using PixelFlow.Data;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PixelFlow.Editor
{
    partial class PixelFlowSetupWindow
    {
        // ─── Data Manager State ───
        private Vector2 _dataManagerScroll;
        private Dictionary<string, List<DataAssetInfo>> _dataGroupCache;
        private bool _dataCacheDirty = true;
        private string _dataSearchFilter = "";
        private int _dataSortMode = 0; // 0=name, 1=type, 2=folder
        private readonly string[] _dataSortOptions = { "İsim", "Tür", "Klasör" };
        private DataAssetInfo _selectedDataAsset;

        private struct DataAssetInfo
        {
            public string Name;
            public string Path;
            public string TypeName;
            public System.Type ScriptableType;
            public string Category;
            public long FileSizeBytes;
            public string LastModified;
            public bool IsValid;
        }

        // ═══════════════════════════════════════════════════
        // SEKME 4: DATA YÖNETİCİSİ
        // ═══════════════════════════════════════════════════

        private void DrawDataManagerTab()
        {
            if (_dataCacheDirty) RefreshDataCache();

            DrawDataManagerToolbar();
            GUILayout.Space(4);

            _dataManagerScroll = GUILayout.BeginScrollView(_dataManagerScroll);
            DrawAssetOverview();
            DrawConfigAssetsSection();
            DrawLevelAssetsSection();
            DrawAudioAssetsSection();
            DrawLocalizationSection();
            DrawDataValidationPanel();
            GUILayout.EndScrollView();
        }

        private void DrawDataManagerToolbar()
        {
            GUILayout.BeginVertical(_cardStyle);
            GUILayout.Label("📦 Veri Odaklı Varlık Yöneticisi", _sectionHeaderStyle);
            GUILayout.Label("Resources/ altındaki tüm ScriptableObject ve data-driven varlıkları yönetin.",
                EditorStyles.miniLabel);
            GUILayout.Space(6);

            GUILayout.BeginHorizontal();
            _dataSearchFilter = EditorGUILayout.TextField("🔍 Filtrele", _dataSearchFilter, GUILayout.Width(300));
            _dataSortMode = GUILayout.Toolbar(_dataSortMode, _dataSortOptions, GUILayout.Height(20), GUILayout.Width(300));
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("🔄 Tara", GUILayout.Height(22), GUILayout.Width(80)))
            {
                _dataCacheDirty = true;
                AssetDatabase.Refresh();
            }
            if (GUILayout.Button("📂 Resources'u Aç", GUILayout.Height(22), GUILayout.Width(120)))
            {
                EditorUtility.OpenWithDefaultApp(Path.GetFullPath("Assets/Resources"));
            }
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
            GUILayout.Space(6);
        }

        private void DrawAssetOverview()
        {
            int totalAssets = _dataGroupCache?.Values.Sum(g => g.Count) ?? 0;
            int configCount = _dataGroupCache?.ContainsKey("Config") == true ? _dataGroupCache["Config"].Count : 0;
            int levelCount = _dataGroupCache?.ContainsKey("Level") == true ? _dataGroupCache["Level"].Count : 0;
            long totalSize = _dataGroupCache?.Values.Sum(g => g.Sum(a => a.FileSizeBytes)) ?? 0;

            GUILayout.BeginVertical(_cardStyle);
            GUILayout.Label($"📊 Özet: {totalAssets} data-driven varlık ({FormatSize(totalSize)})", _sectionHeaderStyle);
            GUILayout.Space(4);

            GUILayout.BeginHorizontal();
            DrawStatBox("⚙️ Konfig", configCount.ToString(), new Color(0.2f, 0.6f, 1f));
            DrawStatBox("🎮 Seviye", levelCount.ToString(), new Color(0.2f, 0.85f, 0.3f));
            DrawStatBox("🔊 Ses", (_dataGroupCache?.ContainsKey("Audio") == true ? _dataGroupCache["Audio"].Count : 0).ToString(), new Color(0.9f, 0.7f, 0.2f));
            DrawStatBox("🌐 Yerelleştirme", (_dataGroupCache?.ContainsKey("Localization") == true ? _dataGroupCache["Localization"].Count : 0).ToString(), new Color(0.8f, 0.4f, 0.8f));
            DrawStatBox("📁 Toplam", $"{totalSize / 1024} KB", new Color(0.6f, 0.6f, 0.6f));
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
            GUILayout.Space(6);
        }

        private void DrawStatBox(string label, string value, Color color)
        {
            var style = new GUIStyle(EditorStyles.helpBox)
            {
                normal = { textColor = color },
                alignment = TextAnchor.MiddleCenter,
                fontSize = 11,
                fontStyle = FontStyle.Bold
            };
            var valStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                normal = { textColor = color },
                fontSize = 18,
                alignment = TextAnchor.MiddleCenter
            };
            GUILayout.BeginVertical(style, GUILayout.Width(110), GUILayout.Height(50));
            GUILayout.Label(value, valStyle);
            GUILayout.Label(label, new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleCenter });
            GUILayout.EndVertical();
        }

        private void DrawConfigAssetsSection()
        {
            if (_dataGroupCache == null || !_dataGroupCache.TryGetValue("Config", out var configs))
                return;

            GUILayout.BeginVertical(_cardStyle);
            GUILayout.Label($"⚙️ Konfigürasyon Varlıkları ({configs.Count})", _sectionHeaderStyle);
            GUILayout.Space(4);

            DrawDataTableHeader(new[] { "Varlık", "Tür", "Boyut", "Son Güncelleme", "Durum", "İşlem" },
                new[] { 180, 160, 70, 130, 80, 100 });

            foreach (var asset in ApplyFilterAndSort(configs))
            {
                DrawDataRow(asset);
            }
            GUILayout.EndVertical();
            GUILayout.Space(6);
        }

        private void DrawLevelAssetsSection()
        {
            if (_dataGroupCache == null || !_dataGroupCache.TryGetValue("Level", out var levels))
                return;

            GUILayout.BeginVertical(_cardStyle);
            GUILayout.Label($"🎮 Seviye Varlıkları ({levels.Count})", _sectionHeaderStyle);
            GUILayout.Space(4);

            DrawDataTableHeader(new[] { "Seviye", "Tür", "Boyut", "Son Güncelleme", "Çözüm", "İşlem" },
                new[] { 180, 160, 70, 130, 80, 100 });

            foreach (var asset in ApplyFilterAndSort(levels))
            {
                DrawDataRow(asset);
            }
            GUILayout.EndVertical();
            GUILayout.Space(6);
        }

        private void DrawAudioAssetsSection()
        {
            if (_dataGroupCache == null || !_dataGroupCache.TryGetValue("Audio", out var audioAssets))
                return;

            GUILayout.BeginVertical(_cardStyle);
            GUILayout.Label($"🔊 Ses Varlıkları ({audioAssets.Count})", _sectionHeaderStyle);
            GUILayout.Space(4);

            if (audioAssets.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    "Henüz ses dosyası eklenmemiş. Resources/Audio/ klasörüne .wav veya .ogg dosyalarını koyun.",
                    MessageType.Info);
            }
            else
            {
                DrawDataTableHeader(new[] { "Dosya", "Klasör", "Boyut", "Son Güncelleme", "Durum", "İşlem" },
                    new[] { 180, 160, 70, 130, 80, 100 });

                foreach (var asset in ApplyFilterAndSort(audioAssets))
                {
                    DrawDataRow(asset);
                }
            }
            GUILayout.EndVertical();
            GUILayout.Space(6);
        }

        private void DrawLocalizationSection()
        {
            if (_dataGroupCache == null || !_dataGroupCache.TryGetValue("Localization", out var locAssets))
                return;

            GUILayout.BeginVertical(_cardStyle);
            GUILayout.Label($"🌐 Yerelleştirme Tabloları ({locAssets.Count})", _sectionHeaderStyle);
            GUILayout.Space(4);

            if (locAssets.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    "Henüz yerelleştirme dosyası eklenmemiş. Resources/Localization/ klasörüne .json dosyalarını koyun.",
                    MessageType.Info);
            }
            else
            {
                DrawDataTableHeader(new[] { "Dosya", "Tür", "Boyut", "Son Güncelleme", "Durum", "İşlem" },
                    new[] { 180, 160, 70, 130, 80, 100 });

                foreach (var asset in ApplyFilterAndSort(locAssets))
                {
                    DrawDataRow(asset);
                }
            }
            GUILayout.EndVertical();
            GUILayout.Space(6);
        }

        private void DrawDataValidationPanel()
        {
            GUILayout.BeginVertical(_cardStyle);
            GUILayout.Label("✅ Varlık Doğrulama & Bakım", _sectionHeaderStyle);
            GUILayout.Space(4);

            int missingCount = 0;
            int warningCount = 0;
            if (_dataGroupCache != null)
            {
                foreach (var group in _dataGroupCache)
                {
                    foreach (var asset in group.Value)
                    {
                        if (!asset.IsValid) missingCount++;
                        if (asset.FileSizeBytes == 0) warningCount++;
                    }
                }
            }

            GUILayout.BeginHorizontal();
            DrawInfoRow("⚠️ Eksik/Tanımsız Varlık:", $"{missingCount}");
            DrawInfoRow("📭 Boş Varlık (0 byte):", $"{warningCount}");
            DrawInfoRow("📦 Toplu Boyut:", $"{_dataGroupCache?.Values.Sum(g => g.Sum(a => a.FileSizeBytes)) / 1024f:F1} KB");
            GUILayout.EndHorizontal();

            GUILayout.Space(6);
            GUILayout.BeginHorizontal();

            GUI.backgroundColor = new Color(0.2f, 0.6f, 1f);
            if (GUILayout.Button("🔄 Asset Database'i Yenile", GUILayout.Height(24), GUILayout.Width(180)))
            {
                AssetDatabase.Refresh();
                _dataCacheDirty = true;
            }
            if (GUILayout.Button("🧹 Boş Konfig Varlığı Oluştur", GUILayout.Height(24), GUILayout.Width(200)))
            {
                CreateDefaultConfigAssets();
                _dataCacheDirty = true;
            }
            if (GUILayout.Button("📋 Export Asset List (CSV)", GUILayout.Height(24), GUILayout.Width(180)))
            {
                ExportDataAssetList();
            }
            GUI.backgroundColor = Color.white;

            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
        }

        // ═══════════════════════════════════════════════════
        // YARDIMCI METODLAR
        // ═══════════════════════════════════════════════════

        private void DrawDataTableHeader(string[] headers, int[] widths)
        {
            GUILayout.BeginHorizontal(EditorStyles.toolbar);
            for (int i = 0; i < headers.Length; i++)
            {
                GUILayout.Label(headers[i], EditorStyles.boldLabel, GUILayout.Width(widths[i]));
            }
            GUILayout.EndHorizontal();
        }

        private void DrawDataRow(DataAssetInfo asset)
        {
            bool isSelected = _selectedDataAsset.Path == asset.Path;

            GUILayout.BeginHorizontal(isSelected ? EditorStyles.helpBox : EditorStyles.toolbar);

            // Name
            GUILayout.Label(asset.Name, GUILayout.Width(180));

            // Type
            GUILayout.Label(asset.TypeName, EditorStyles.miniLabel, GUILayout.Width(160));

            // Size
            GUILayout.Label(FormatSize(asset.FileSizeBytes), GUILayout.Width(70));

            // Last modified
            GUILayout.Label(asset.LastModified, EditorStyles.miniLabel, GUILayout.Width(130));

            // Status badge
            if (!asset.IsValid)
                GUILayout.Label("⚠ Boş", _warnBadgeStyle, GUILayout.Width(80));
            else if (asset.FileSizeBytes == 0)
                GUILayout.Label("📭", GUILayout.Width(80));
            else
                GUILayout.Label("✅", _okBadgeStyle, GUILayout.Width(80));

            // Actions
            if (GUILayout.Button("Seç", GUILayout.Height(16), GUILayout.Width(45)))
            {
                _selectedDataAsset = asset;
                var obj = AssetDatabase.LoadAssetAtPath<ScriptableObject>(asset.Path);
                if (obj != null)
                {
                    Selection.activeObject = obj;
                    EditorGUIUtility.PingObject(obj);
                }
            }
            if (GUILayout.Button("Aç", GUILayout.Height(16), GUILayout.Width(40)))
            {
                var obj = AssetDatabase.LoadAssetAtPath<ScriptableObject>(asset.Path);
                if (obj != null)
                {
                    Selection.activeObject = obj;
                    EditorGUIUtility.PingObject(obj);
                    AssetDatabase.OpenAsset(obj);
                }
            }

            GUILayout.EndHorizontal();
        }

        private List<DataAssetInfo> ApplyFilterAndSort(List<DataAssetInfo> assets)
        {
            var filtered = assets.AsEnumerable();

            // Apply search filter
            if (!string.IsNullOrEmpty(_dataSearchFilter))
            {
                string filter = _dataSearchFilter.ToLower();
                filtered = filtered.Where(a =>
                    a.Name.ToLower().Contains(filter) ||
                    a.TypeName.ToLower().Contains(filter) ||
                    a.Category.ToLower().Contains(filter));
            }

            // Apply sort
            switch (_dataSortMode)
            {
                case 0: filtered = filtered.OrderBy(a => a.Name); break;
                case 1: filtered = filtered.OrderBy(a => a.TypeName).ThenBy(a => a.Name); break;
                case 2: filtered = filtered.OrderBy(a => a.Category).ThenBy(a => a.Name); break;
            }

            return filtered.ToList();
        }

        private static string FormatSize(long bytes)
        {
            if (bytes < 1024) return $"{bytes} B";
            if (bytes < 1024 * 1024) return $"{bytes / 1024f:F1} KB";
            return $"{bytes / (1024f * 1024f):F2} MB";
        }

        // ═══════════════════════════════════════════════════
        // DATA CACHE
        // ═══════════════════════════════════════════════════

        private void RefreshDataCache()
        {
            _dataGroupCache = new Dictionary<string, List<DataAssetInfo>>
            {
                ["Config"] = ScanConfigAssets(),
                ["Level"] = ScanLevelAssets(),
                ["Audio"] = ScanAudioAssets(),
                ["Localization"] = ScanLocalizationAssets()
            };
            _dataCacheDirty = false;
        }

        private List<DataAssetInfo> ScanConfigAssets()
        {
            var assets = new List<DataAssetInfo>();
            var guids = AssetDatabase.FindAssets("t:ScriptableObject", new[] { "Assets/Resources/Configs" });
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var info = BuildAssetInfo(path, "Config");
                if (info.HasValue) assets.Add(info.Value);
            }
            return assets;
        }

        private List<DataAssetInfo> ScanLevelAssets()
        {
            var assets = new List<DataAssetInfo>();
            var guids = AssetDatabase.FindAssets("t:LevelData", new[] { "Assets/Resources/Levels" });
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var info = BuildAssetInfo(path, "Level");
                if (info.HasValue) assets.Add(info.Value);
            }

            // Also find LevelPack assets
            var packGuids = AssetDatabase.FindAssets("t:LevelPack", new[] { "Assets/Resources/Levels" });
            foreach (var guid in packGuids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var info = BuildAssetInfo(path, "Level");
                if (info.HasValue)
                {
                    var infoVal = info.Value;
                    infoVal.TypeName = "LevelPack";
                    assets.Add(infoVal);
                }
            }
            return assets;
        }

        private List<DataAssetInfo> ScanAudioAssets()
        {
            var assets = new List<DataAssetInfo>();
            var guids = AssetDatabase.FindAssets("t:AudioClip", new[] { "Assets/Resources/Audio" });
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var info = BuildAssetInfo(path, "Audio");
                if (info.HasValue) assets.Add(info.Value);
            }
            return assets;
        }

        private List<DataAssetInfo> ScanLocalizationAssets()
        {
            var assets = new List<DataAssetInfo>();
            var guids = AssetDatabase.FindAssets("", new[] { "Assets/Resources/Localization" });
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.EndsWith(".meta")) continue;
                var info = BuildAssetInfo(path, "Localization");
                if (info.HasValue) assets.Add(info.Value);
            }
            return assets;
        }

        private DataAssetInfo? BuildAssetInfo(string assetPath, string category)
        {
            if (string.IsNullOrEmpty(assetPath) || !File.Exists(assetPath))
                return null;

            var fileName = Path.GetFileNameWithoutExtension(assetPath);
            var fileInfo = new FileInfo(assetPath);
            var obj = AssetDatabase.LoadAssetAtPath<ScriptableObject>(assetPath);

            string typeName = "Bilinmeyen";
            bool isValid = false;

            if (obj != null)
            {
                typeName = obj.GetType().Name;
                isValid = true;
            }
            else
            {
                // Try loading as AudioClip or TextAsset
                var audioClip = AssetDatabase.LoadAssetAtPath<AudioClip>(assetPath);
                if (audioClip != null)
                {
                    typeName = "AudioClip";
                    isValid = true;
                }
                else
                {
                    var textAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(assetPath);
                    if (textAsset != null)
                    {
                        typeName = "TextAsset";
                        isValid = true;
                    }
                }
            }

            // Get resources-relative path
            return new DataAssetInfo
            {
                Name = fileName,
                Path = assetPath,
                TypeName = typeName,
                ScriptableType = obj?.GetType(),
                Category = category,
                FileSizeBytes = fileInfo?.Length ?? 0,
                LastModified = fileInfo?.LastWriteTime.ToString("dd MMM HH:mm") ?? "?",
                IsValid = isValid
            };
        }

        // ═══════════════════════════════════════════════════
        // ASSET YÖNETİMİ
        // ═══════════════════════════════════════════════════

        private void CreateDefaultConfigAssets()
        {
            string configDir = "Assets/Resources/Configs";
            if (!Directory.Exists(configDir)) Directory.CreateDirectory(configDir);

            TryCreateAsset<GameConfig>(configDir + "/GameConfig.asset");
            TryCreateAsset<ThemePaletteAsset>(configDir + "/ThemePalette.asset");
            TryCreateAsset<ColorBlindPaletteAsset>(configDir + "/ColorBlindPalette.asset");
            TryCreateAsset<VehicleMaterialConfigAsset>(configDir + "/VehicleMaterialConfig.asset");
            TryCreateAsset<EconomyConfigAsset>(configDir + "/EconomyConfig.asset");
            TryCreateAsset<LevelCatalogAsset>(configDir + "/LevelCatalog.asset");

            AssetDatabase.SaveAssets();
            Debug.Log("[PixelFlow] Varsayılan konfigürasyon varlıkları oluşturuldu.");
        }

        private void TryCreateAsset<T>(string path) where T : ScriptableObject
        {
            if (File.Exists(path)) return;
            var instance = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(instance, path);
            Debug.Log($"[PixelFlow] {typeof(T).Name} oluşturuldu: {path}");
        }

        private void ExportDataAssetList()
        {
            string csvPath = Path.Combine(Application.dataPath, "../DataAssets_Export.csv");
            var lines = new List<string>
            {
                "Name,Path,Type,Category,SizeBytes,LastModified,Valid"
            };

            if (_dataGroupCache != null)
            {
                foreach (var group in _dataGroupCache)
                {
                    foreach (var asset in group.Value)
                    {
                        lines.Add($"{asset.Name},{asset.Path},{asset.TypeName},{asset.Category}," +
                                  $"{asset.FileSizeBytes},{asset.LastModified},{asset.IsValid}");
                    }
                }
            }

            File.WriteAllLines(csvPath, lines);
            Debug.Log($"[PixelFlow] Asset list exported to: {csvPath}");
            EditorUtility.RevealInFinder(csvPath);
        }
    }
}
#endif
