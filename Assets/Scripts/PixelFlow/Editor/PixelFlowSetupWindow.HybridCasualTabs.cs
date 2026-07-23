#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using PixelFlow.Data;
using System.IO;
using System.Linq;

namespace PixelFlow.Editor
{
    partial class PixelFlowSetupWindow
    {
        // ═══════════════════════════════════════════════════
        // SEKME 2: GARAJ & SKİN STÜDYOSU (game_plan.md §2.1.B1)
        // ═══════════════════════════════════════════════════
        private Vector2 _garageScrollPos;

        private void DrawGarageSkinStudioTab()
        {
            _garageScrollPos = EditorGUILayout.BeginScrollView(_garageScrollPos);
            GUILayout.BeginVertical(_cardStyle);
            GUILayout.Label("🎨 Garaj & Skin Stüdyosu (No-Code Skin Manager)", _sectionHeaderStyle);
            GUILayout.Label("Kod yazmadan yeni 3D araç skin'leri ekleyin, renk ailesi atayın, 3D model ve ses efektlerini canlandırın.", _miniInfoStyle);
            GUILayout.Space(8);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("✨ Yeni VehicleSkinConfig Asset Oluştur", GUILayout.MinHeight(30)))
            {
                CreateNewVehicleSkinAsset();
            }
            if (GUILayout.Button("🍦 Standart 3D Skin Paketini Oluştur (Dondurma Arabası, Canavar Kamyon, Altın Otobüs)", GUILayout.MinHeight(30)))
            {
                CreateStandardSkinSuite();
            }
            GUILayout.EndHorizontal();

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
                    // 3D Preview Thumbnail
                    Texture2D previewTex = skin.Prefab3D != null ? AssetPreview.GetAssetPreview(skin.Prefab3D) : null;
                    if (previewTex != null)
                    {
                        GUILayout.Label(previewTex, GUILayout.Width(64), GUILayout.Height(64));
                    }
                    else
                    {
                        GUILayout.Box("3D Model\nYok", GUILayout.Width(64), GUILayout.Height(64));
                    }

                    GUILayout.BeginVertical();
                    EditorGUI.BeginChangeCheck();
                    string newName = EditorGUILayout.TextField("Skin İsmi", skin.DisplayName);
                    string newId = EditorGUILayout.TextField("Skin ID", skin.SkinId);
                    ColorType newColor = (ColorType)EditorGUILayout.EnumPopup("Renk Ailesi", skin.ColorFamily);
                    int newCost = EditorGUILayout.IntField("Altın Bedeli", skin.UnlockCoinCost);
                    bool newReqAd = EditorGUILayout.Toggle("Reklam ile Açılır", skin.RequiresRewardedAd);
                    var newPrefab = (GameObject)EditorGUILayout.ObjectField("3D Prefab", skin.Prefab3D, typeof(GameObject), false);
                    var newIcon = (Sprite)EditorGUILayout.ObjectField("UI İkonu", skin.Icon, typeof(Sprite), false);
                    var newEngineSound = (AudioClip)EditorGUILayout.ObjectField("Motor Sesi", skin.EngineSound, typeof(AudioClip), false);
                    var newHornSound = (AudioClip)EditorGUILayout.ObjectField("Korna Sesi", skin.HornSound, typeof(AudioClip), false);

                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(skin, "Update Vehicle Skin Config");
                        skin.DisplayName = newName;
                        skin.SkinId = newId;
                        skin.ColorFamily = newColor;
                        skin.UnlockCoinCost = newCost;
                        skin.RequiresRewardedAd = newReqAd;
                        skin.Prefab3D = newPrefab;
                        skin.Icon = newIcon;
                        skin.EngineSound = newEngineSound;
                        skin.HornSound = newHornSound;
                        EditorUtility.SetDirty(skin);
                    }
                    GUILayout.EndVertical();

                    GUILayout.BeginVertical(GUILayout.Width(90));
                    if (GUILayout.Button("🔍 Seç", GUILayout.MinHeight(24)))
                    {
                        Selection.activeObject = skin;
                        EditorGUIUtility.PingObject(skin);
                    }
                    if (skin.EngineSound != null && GUILayout.Button("🔊 Motor Sesi", GUILayout.MinHeight(24)))
                    {
                        PlayAudioClipInEditor(skin.EngineSound);
                    }
                    if (skin.HornSound != null && GUILayout.Button("📯 Korna Sesi", GUILayout.MinHeight(24)))
                    {
                        PlayAudioClipInEditor(skin.HornSound);
                    }
                    GUILayout.EndVertical();

                    GUILayout.EndHorizontal();
                    GUILayout.EndVertical();
                    GUILayout.Space(6);
                }
            }

            GUILayout.EndVertical();
            EditorGUILayout.EndScrollView();
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

        private void CreateStandardSkinSuite()
        {
            string dir = "Assets/Resources/Configs/Skins";
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            CreateSkinIfMissing($"{dir}/VehicleSkin_IceCreamTruck.asset", "skin_icecream", "Dondurma Arabası 🍦", ColorType.Yellow, 250, false);
            CreateSkinIfMissing($"{dir}/VehicleSkin_MonsterTruck.asset", "skin_monstertruck", "Canavar Kamyon 🚘", ColorType.Red, 500, false);
            CreateSkinIfMissing($"{dir}/VehicleSkin_GoldenBus.asset", "skin_goldenbus", "Altın Otobüs 🚌", ColorType.Yellow, 1000, true);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[PixelFlow] Standart 3D Araç skin paketi oluşturuldu.");
        }

        private void CreateSkinIfMissing(string path, string skinId, string displayName, ColorType color, int cost, bool reqAd)
        {
            var existing = AssetDatabase.LoadAssetAtPath<VehicleSkinConfig>(path);
            if (existing != null) return;

            var skin = ScriptableObject.CreateInstance<VehicleSkinConfig>();
            skin.SkinId = skinId;
            skin.DisplayName = displayName;
            skin.ColorFamily = color;
            skin.UnlockCoinCost = cost;
            skin.RequiresRewardedAd = reqAd;
            AssetDatabase.CreateAsset(skin, path);
        }

        private static void PlayAudioClipInEditor(AudioClip clip)
        {
            if (clip == null) return;
            System.Type audioUtil = System.Type.GetType("UnityEditor.AudioUtil, UnityEditor");
            if (audioUtil != null)
            {
                var method = audioUtil.GetMethod("PlayPreviewClip", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public, null, new[] { typeof(AudioClip), typeof(int), typeof(bool) }, null);
                if (method != null)
                {
                    method.Invoke(null, new object[] { clip, 0, false });
                    return;
                }
            }
            AudioSource.PlayClipAtPoint(clip, Vector3.zero);
        }

        // ═══════════════════════════════════════════════════
        // SEKME 5: REKLAM & MONETIZATION (game_plan.md §2.1.B2)
        // ═══════════════════════════════════════════════════
        private Vector2 _adsScrollPos;

        private void DrawAdMonetizationTab()
        {
            _adsScrollPos = EditorGUILayout.BeginScrollView(_adsScrollPos);
            GUILayout.BeginVertical(_cardStyle);
            GUILayout.Label("📺 Reklam & Monetization Ayarları (game_plan.md §2.1.B2)", _sectionHeaderStyle);
            GUILayout.Label("Rewarded Ad ödülleri, 2x Coin çarpanları, Interstitial seviye barajları ve Placement ID yönetimi.", _miniInfoStyle);
            GUILayout.Space(8);

            var gameConfig = Resources.Load<GameConfig>("Configs/GameConfig");
            if (gameConfig == null)
            {
                EditorGUILayout.HelpBox("GameConfig Resources/Configs/GameConfig.asset konumunda bulunamadı!", MessageType.Error);
                if (GUILayout.Button("GameConfig Asset Oluştur", GUILayout.MinHeight(30)))
                {
                    CreateGameConfigAsset();
                }
            }
            else
            {
                EditorGUILayout.HelpBox("Tüm reklam parametreleri GameConfig Asset üzerinden yönetilir (Zero-Hardcode).", MessageType.Info);
                GUILayout.Space(6);

                EditorGUI.BeginChangeCheck();

                GUILayout.Label("🎬 Interstitial Reklam Ayarları", EditorStyles.boldLabel);
                EditorGUI.indentLevel++;
                int newInterInterval = EditorGUILayout.IntSlider("Seviye Barajı (Kaç seviyede bir)", gameConfig.InterstitialLevelInterval, 1, 10);
                int newMinLevelInter = EditorGUILayout.IntField("Minimum Seviye (İlk Interstitial)", gameConfig.MinLevelForInterstitial);
                int newMaxRetries = EditorGUILayout.IntField("Maksimum Kriz Denemesi", gameConfig.MaxRetriesBeforeInterstitial);
                EditorGUI.indentLevel--;

                GUILayout.Space(8);
                GUILayout.Label("🎁 Rewarded Ad & Ödül Ayarları", EditorStyles.boldLabel);
                EditorGUI.indentLevel++;
                int newCoinReward = EditorGUILayout.IntField("Rewarded Ad Altın Ödülü", gameConfig.RewardedAdCoinReward);
                int newHintReward = EditorGUILayout.IntField("Rewarded Ad İpucu Ödülü", gameConfig.RewardedAdHintReward);
                float newDoubleCoinMult = EditorGUILayout.Slider("Seviye Sonu 2x Para Çarpanı", gameConfig.DoubleCoinMultiplier, 1.5f, 5.0f);
                EditorGUI.indentLevel--;

                GUILayout.Space(8);
                GUILayout.Label("🆔 Placement ID Yönetimi", EditorStyles.boldLabel);
                EditorGUI.indentLevel++;
                string newInterId = EditorGUILayout.TextField("Interstitial Placement ID", gameConfig.InterstitialPlacementId);
                string newRewardedId = EditorGUILayout.TextField("Rewarded Placement ID", gameConfig.RewardedPlacementId);
                string newBannerId = EditorGUILayout.TextField("Banner Placement ID", gameConfig.BannerPlacementId);
                EditorGUI.indentLevel--;

                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(gameConfig, "Change GameConfig Ad Settings");
                    gameConfig.InterstitialLevelInterval = newInterInterval;
                    gameConfig.MinLevelForInterstitial = newMinLevelInter;
                    gameConfig.MaxRetriesBeforeInterstitial = newMaxRetries;
                    gameConfig.RewardedAdCoinReward = newCoinReward;
                    gameConfig.RewardedAdHintReward = newHintReward;
                    gameConfig.DoubleCoinMultiplier = newDoubleCoinMult;
                    gameConfig.InterstitialPlacementId = newInterId;
                    gameConfig.RewardedPlacementId = newRewardedId;
                    gameConfig.BannerPlacementId = newBannerId;

                    EditorUtility.SetDirty(gameConfig);
                    AssetDatabase.SaveAssets();
                }

                GUILayout.Space(10);
                if (GUILayout.Button("GameConfig Asset'ini Inspector'da Seç", GUILayout.MinHeight(28)))
                {
                    Selection.activeObject = gameConfig;
                    EditorGUIUtility.PingObject(gameConfig);
                }
            }

            GUILayout.EndVertical();
            EditorGUILayout.EndScrollView();
        }

        // ═══════════════════════════════════════════════════
        // SEKME 10: PRE-BUILD VALIDATOR (game_plan.md §2.1.B3)
        // ═══════════════════════════════════════════════════
        private Vector2 _validatorScrollPos;

        private void DrawPreBuildValidatorTab()
        {
            _validatorScrollPos = EditorGUILayout.BeginScrollView(_validatorScrollPos);
            GUILayout.BeginVertical(_cardStyle);
            GUILayout.Label("🛡️ Pre-Build Data Validator (Zero-Hardcode Denetçi)", _sectionHeaderStyle);
            GUILayout.Label("Build almadan veya Play Mode başlatmadan önce veri eksikliklerini ve hatalarını tarayın.", _miniInfoStyle);
            GUILayout.Space(10);

            if (GUILayout.Button("🔍 Proje Verilerini Şimdi Tara ve Doğrula", GUILayout.MinHeight(34)))
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

            GUILayout.Space(10);
            GUILayout.Label("📋 Otomatik Denetim Maddeleri", EditorStyles.boldLabel);

            var gameConfig = Resources.Load<GameConfig>("Configs/GameConfig");
            bool configOk = gameConfig != null && gameConfig.VehicleSpeed > 0f;
            DrawDiagnosticRow("1. GameConfig Asset Bütünlüğü", configOk, "Geçerli", "GameConfig eksik veya geçersiz");

            bool levelCountOk = _cachedLevels.Count > 0;
            DrawDiagnosticRow("2. Seviye Kataloğu Kaydı", levelCountOk, $"{_cachedLevels.Count} seviye hazır", "Hiç seviye yok");

            var skinGuids = AssetDatabase.FindAssets("t:VehicleSkinConfig");
            bool skinOk = skinGuids.Length > 0;
            DrawDiagnosticRow("3. Araç Skin Varlıkları", skinOk, $"{skinGuids.Length} skin mevcut", "Henüz skin oluşturulmamış");

            GUILayout.EndVertical();
            EditorGUILayout.EndScrollView();
        }
    }
}
#endif
