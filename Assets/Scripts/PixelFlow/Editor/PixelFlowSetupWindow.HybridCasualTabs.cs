#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using PixelFlow.Data;
using System.IO;

namespace PixelFlow.Editor
{
    partial class PixelFlowSetupWindow
    {
        // ═══════════════════════════════════════════════════
        // SEKME 9: GARAJ & SKIN STÜDYOSU (No-Code Skin Manager)
        // ═══════════════════════════════════════════════════
        private void DrawGarageSkinStudioTab()
        {
            GUILayout.BeginVertical(_cardStyle);
            GUILayout.Label("🎨 Garaj & Skin Stüdyosu (No-Code Skin Manager)", _sectionHeaderStyle);
            GUILayout.Label("Kod yazmadan yeni 3D araç skin'leri ekleyin, fiyatlandırın ve 3D önizleme yapın.", _miniInfoStyle);
            GUILayout.Space(8);

            if (GUILayout.Button("✨ Yeni VehicleSkinConfig Asset Oluştur", GUILayout.Height(30)))
            {
                CreateNewVehicleSkinAsset();
            }

            GUILayout.Space(10);
            GUILayout.Label("📦 Projedeki Araç Skin'leri", EditorStyles.boldLabel);

            var guids = AssetDatabase.FindAssets("t:VehicleSkinConfig");
            if (guids.Length == 0)
            {
                EditorGUILayout.HelpBox("Henüz VehicleSkinConfig varlığı bulunamadı. Yukarıdaki butona tıklayarak oluşturun.", MessageType.Info);
            }
            else
            {
                foreach (var guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    var skin = AssetDatabase.LoadAssetAtPath<VehicleSkinConfig>(path);
                    if (skin == null) continue;

                    GUILayout.BeginVertical(EditorStyles.helpBox);
                    GUILayout.BeginHorizontal();
                    GUILayout.Label($"<b>{skin.DisplayName}</b> ({skin.SkinId})", new GUIStyle(EditorStyles.label) { richText = true });
                    GUILayout.Label($"Color: {skin.ColorFamily} │ {skin.UnlockCoinCost} Gold", EditorStyles.miniLabel);
                    if (GUILayout.Button("Düzenle", GUILayout.Width(60)))
                    {
                        Selection.activeObject = skin;
                        EditorGUIUtility.PingObject(skin);
                    }
                    GUILayout.EndHorizontal();
                    GUILayout.EndVertical();
                    GUILayout.Space(2);
                }
            }

            GUILayout.EndVertical();
        }

        private void CreateNewVehicleSkinAsset()
        {
            string dir = "Assets/Resources/Configs/Skins";
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            string path = AssetDatabase.GenerateUniqueAssetPath($"{dir}/VehicleSkin_New.asset");
            var skin = ScriptableObject.CreateInstance<VehicleSkinConfig>();
            skin.SkinId = $"skin_{System.DateTime.Now.Ticks}";
            skin.DisplayName = "Yeni Araç Skini";
            
            AssetDatabase.CreateAsset(skin, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Selection.activeObject = skin;
            EditorGUIUtility.PingObject(skin);
            Debug.Log($"[PixelFlow] Yeni VehicleSkinConfig oluşturuldu: {path}");
        }

        // ═══════════════════════════════════════════════════
        // SEKME 10: REKLAM & MONETIZATION
        // ═══════════════════════════════════════════════════
        private void DrawAdMonetizationTab()
        {
            GUILayout.BeginVertical(_cardStyle);
            GUILayout.Label("📺 Reklam & Monetization Ayarları", _sectionHeaderStyle);
            GUILayout.Label("Rewarded Ad ödülleri, 2x Coin çarpanları ve Interstitial baraj ayarları.", _miniInfoStyle);
            GUILayout.Space(8);

            var gameConfig = Resources.Load<GameConfig>("Configs/GameConfig");
            if (gameConfig == null)
            {
                EditorGUILayout.HelpBox("GameConfig Resources/Configs/GameConfig.asset konumunda bulunamadı!", MessageType.Error);
            }
            else
            {
                EditorGUILayout.HelpBox("Tüm reklam parametreleri GameConfig Asset üzerinden yönetilir (Zero-Hardcode).", MessageType.Info);
                if (GUILayout.Button("GameConfig Asset'ini Seç ve Düzenle", GUILayout.Height(30)))
                {
                    Selection.activeObject = gameConfig;
                    EditorGUIUtility.PingObject(gameConfig);
                }
            }

            GUILayout.EndVertical();
        }

        // ═══════════════════════════════════════════════════
        // SEKME 11: PRE-BUILD VALIDATOR
        // ═══════════════════════════════════════════════════
        private void DrawPreBuildValidatorTab()
        {
            GUILayout.BeginVertical(_cardStyle);
            GUILayout.Label("🛡️ Pre-Build Data Validator (Zero-Hardcode Denetçi)", _sectionHeaderStyle);
            GUILayout.Label("Build almadan veya Play Mode başlatmadan önce veri eksikliklerini ve hatalarını tarayın.", _miniInfoStyle);
            GUILayout.Space(10);

            if (GUILayout.Button("🔍 Proje Verilerini Şimdi Tara ve Doğrula", GUILayout.Height(34)))
            {
                if (PreBuildDataValidator.ValidateAllData(out string errorMessage))
                {
                    EditorUtility.DisplayDialog("Veri Doğrulama Başarılı", "✅ Tüm GameConfig ve Seviye verileri eksiksiz ve geçerli!", "Tamam");
                }
                else
                {
                    EditorUtility.DisplayDialog("Veri Hataları Bulundu", $"❌ Hata:\n\n{errorMessage}", "Tamam");
                }
            }

            GUILayout.EndVertical();
        }
    }
}
#endif
