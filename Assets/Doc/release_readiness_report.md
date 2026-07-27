# Pixel Flow — Yayına Hazırlık Eksiklik Raporu

**Tarih:** 2026-07-25  
**Amaç:** Mağazaya çıkış öncesi kritik eksikliklerin tespiti ve önceliklendirilmesi  

---

## 🔴 KRİTİK — YAYINI ENGELLEYEN (Blocker)

### 1. 3D Araç Modelleri — %100 Eksik

| Durum | Açıklama |
|-------|----------|
| ❌ **YOK** | `Models/Cars/SportCar/` **boş klasör** — hiçbir .fbx/.obj/.blend dosyası yok |
| ❌ **YOK** | `VehicleSkinConfig.Prefab3D` → `{fileID: 0}` — tüm 33 skin için prefab null |
| ❌ **YOK** | `VehicleSkinConfig.Icon` → `{fileID: 0}` — tüm skin ikonları null |
| ❌ **YOK** | `VehicleSkinConfig.EngineSound/HornSound` → `{fileID: 0}` — ses dosyaları null |
| ⚠️ **VAR** | `VehicleVisualFactory.CreateCar3D()` → `GameObject.Instantiate()` ile **procedural** araç üretiyor |

**Detay:** Kod `VehicleVisualFactory.cs` içinde `CreateCar3D()` ve `CreateTrain3D()` metotları var. Bunlar `Resources.Load<GameObject>()` ile prefab yüklemeye çalışıyor ama **Prefab3D null olduğu için** fallback olarak **procedural primitive-based** (cube + sphere) araç üretiyor. Yani oyun çalışır ama **görsel kalite çok düşük**.

**Öneri:** En azından launch için 3 temel araç modeli (car, truck, bus) professional olarak üretilmeli. 33 skin'in her biri için ayrı model yerine **renk varyasyonu** yeterli.

---

### 2. UI Prefab'ları — Sadece 1 Prefab Var

| Prefab | Durum |
|--------|-------|
| `CellView.prefab` | ✅ Var (tek prefab) |
| `HUDView.prefab` | ❌ YOK — Canvas Scene'de inline |
| `MainMenuView.prefab` | ❌ YOK — Canvas Scene'de inline |
| `GarageView.prefab` | ❌ YOK — Canvas Scene'de inline |
| `SettingsView.prefab` | ❌ YOK — Canvas Scene'de inline |
| `GridView.prefab` | ❌ YOK — Canvas Scene'de inline |
| `SplashView.prefab` | ❌ YOK — Canvas Scene'de inline |
| `LevelSelectView.prefab` | ❌ YOK — Canvas Scene'de inline |

**Detay:** Tüm UI elementleri `SampleScene.unity` (13.857 satır) içinde **inline** olarak tanımlanmış. Hiçbir UI ekranı prefab olarak ayrılmamış. Bu şu anlama geliyor:
- Scene dosyası **tek parça**, düzenlenmesi zor
- Her ekran değişiminde tüm scene reload edilmeli
- UI elementlerini test etmek için scene'i açmak gerekiyor
- Prefab bazlı workflow yok

---

### 3. Seviye İçeriği & Katalog Yönetimi

| Kategori | Planlanan | Mevcut | Durum |
|----------|-----------|--------|-------|
| Authored LevelData | 150 seviye | **3 adet** (Level1, Level2, Level3) | ⚠️ Diskteki varlıklara kısıtlı |
| Katalog Yapısı | Kısıtlı Katalog | ✅ Strictly diske bağlı | ✅ Sahte doldurma ve canlı üretim engellendi |

**Detay:** `LevelCatalog.asset` otomatik sahte seviye doldurma döngüsünden arındırılmış ve strictly diskteki varlıklara kısıtlanmıştır. Diskte olmayan seviyeler (örn. Seviye 4) için oyun anında kendiliğinden seviye üretilmesi engellenmiştir (`LevelProgressionService`). İsteğe bağlı tekil seviye üretimi `PixelFlowSetupWindow` üzerindeki `Tek Seviye Ekle` aracı ile kontrollü yapılır.

---

### 4. Ses Dosyaları — Boş Klasörler

| Klasör | Durum |
|--------|-------|
| `Resources/Audio/AMB/` | ❌ Boş |
| `Resources/Audio/MUSIC/` | ❌ Boş |
| `Resources/Audio/SFX/` | ❌ Boş |

**Detay:** Tüm ses referansları null. `AudioService.cs` mimarisi hazır ancak `AudioClip` asset'leri beklenmektedir.

---

### 5. Tema Görselleri & Engel Dokuları (ThemePaletteAsset)

| Durum | Açıklama |
|-------|----------|
| ✅ **VAR** | `ThemePaletteAsset` renk paletleri ve `ObstaclePalette` yapıları tam entegre |
| ✅ **VAR** | `CellView.cs` prosedürel engel ikonları/dokuları (İnşaat hazard stripes, Gölet su dalgaları, Park çim deseni, Tek Yön okları) |
| ✅ **VAR** | `CellView.cs` ve `ThemePaletteAsset.cs` üzerinde hardcoded fallback renkler kaldırıldı, katı `DataValidationException` eklendi (§2.2) |
| ⚠️ **VAR** | `BentoGlass.shader`, `GlowPulse.shader`, `VehicleGhost.shader` — shader'lar mevcut |

---

## 🟡 ÖNEMLİ — YAYINI GECİKTİREN (Major)

### 6. Garage Ekonomi Akışı Tam Değil

| Sistem | Durum |
|--------|-------|
| GarageView UI | ✅ Var |
| GarageMediator | ✅ Var |
| InventoryModel | ✅ Var |
| VehicleSkinConfig | ✅ Var (33 skin data) |
| **Skin Unlock Command** | ⚠️ Var ama **para çekme + onay akışı** eksik |
| **Skin Equip** | ⚠️ UI event'leri var ama **ekonomi doğrulama** tam değil |
| **Stop Skin Unlock** | ⚠️ `StopSkinUnlockCommand` var ama durak skin açma akışı test edilmemiş |

---

### 7. Reklam SDK Entegrasyonu Yok

| Placement | Durum |
|-----------|-------|
| Interstitial (her 3 seviye) | ⚠️ `InterstitialAdCommand` var ama **mediation SDK yok** |
| Rewarded (2x coin, extra undo) | ⚠️ `RewardedAdCommand` var ama **SDK yok** |
| `AdManagerService` | ⚠️ Stub — AppLovin MAX / ironSource bağlanmalı |
| `CrisisAdService` | ⚠️ Kriz çözümü reklamı — SDK bekliyor |

---

### 8. IAP (In-App Purchase) Yok

| Ürün | Durum |
|------|-------|
| No Ads ($2.99) | ❌ Kod yok |
| Starter Pack ($0.99) | ❌ Kod yok |
| Coin/Gem Pack'ler | ❌ Kod yok |
| Star Pass ($4.99) | ❌ Kod yok |
| `IapIntegrationService` | ⚠️ 139 satır stub — Unity IAP bağlanmalı |

---

### 9. Cloud Save — Simülasyon

| Sistem | Durum |
|--------|-------|
| `CloudSaveManager` | ⚠️ Local AES-256 kayıt var |
| Firebase/Firestore | ❌ Bağlı değil |
| Apple Game Center | ❌ Yok |
| Google Play Games | ❌ Yok |
| Conflict Resolution | ⚠️ Last-write-wins simülasyonu var ama backend yok |

---

### 10. Global Release Servisleri — Stub

| Servis | Durum |
|--------|-------|
| PrivacyComplianceService | ⚠️ UMP SDK yok |
| SilentCrashDiagnosticsService | ⚠️ Crashlytics/Sentry yok |
| InAppReviewService | ⚠️ StoreKit/Play native yok |
| LocalNotificationService | ⚠️ Native scheduling yok |
| AnalyticsService | ⚠️ Canlı backend event şeması yok |

---

## 🟢 DÜŞÜK ÖNCELİKLİ — Post-Launch (Minor)

### 11. Yerelleştirme Tabloları

| Dil | Durum |
|-----|-------|
| `LocalizationService` | ✅ Kod var |
| CSV tablo | ⚠️ Var ama **tüm metinler İngilizce/Türkçe** — 15 dil eksik |
| RTL desteği | ⚠️ Kod altyapısı var ama Arapça/İbranice test edilmemiş |

---

### 12. Editör Üreticileri

| Araç | Durum |
|------|-------|
| `GenerateLevels.cs` | ✅ Procedural level üretici var |
| `PhaseAssetGenerator.cs` ✅ Faz asset üretici var |
| `PixelFlowLevelStudioWindow.cs` | ✅ Seviye stüdyosu penceresi var |
| **Authored seviye içeriği** | ❌ Editör araçları var ama **150 seviye elle tasarlanmamış** |

---

## 📊 ÖZET TABLO

| Kategori | Durum | Yayını Etkiler mi? |
|----------|-------|-------------------|
| Core Gameplay Logic | ✅ %95 tamam | Hayır — oynanabilir |
| MVCS Architecture | ✅ %100 tamam | Hayır — sağlam mimari |
| Test Coverage | ⚠️ %70 — 43 test var | Kısmen — mantık testli |
| Editor Tools | ✅ %90 tamam | Hayır — editör çalışır |
| **3D Asset'ler (modeller)** | ❌ %0 — procedural fallback | **EVET — görsel kalite** |
| **UI Prefab'ları** | ❌ %5 — sadece CellView | **EVET — bakım zorluğu** |
| **Ses dosyaları** | ❌ %0 — boş klasörler | **EVET — sessiz oyun** |
| **Seviye içeriği** | ❌ %2 — 3/150 authored | **EVET — içerik yetersiz** |
| **Reklam SDK** | ❌ Stub | EVET — monetizasyon yok |
| **IAP** | ❌ Stub | EVET — gelir yok |
| **Cloud/Analytics** | ❌ Stub | EVET — veri yok |
| **Global Release** | ❌ Stub'lar | EVET — mağaza reddi riski |

---

## 🎯 ÖNERİLEN YOL HARİTASI

### FAZ 0 — MVP Test (Hafta 1) — ✅ Zaten Yapılabilir
Procedural araçlar + 3 authored seviye + 30+ procedural seviye ile **$500 CPI testi** yapılabilir. Oyun **oynanır** ama görsel kalite düşük.

### FAZ 1 — Görsel Polishing (Hafta 2-4)
1. **3 temel araç modeli** üret (car, truck, train) — pastel toy estetiğinde
2. **Her skin için renk varyasyonu** — 33 model yerine 3 model × 11 renk
3. **Ses dosyaları** — SFX (pop, bounce, crash, coin) + background music
4. **UI Prefab'ları** — her ekran için ayrı prefab oluştur
5. **Tema geçişleri** — arkaplan/materyal güncelleme

### FAZ 2 — İçerik Üretimi (Hafta 3-6)
1. **50 authored seviye** (editör araçları hazır) — 20 kolay + 20 orta + 10 zor
2. **Procedural generator ayarı** — geri kalan 100 seviye için tuning
3. **Durak skin'leri** — 12 durak için görsel asset

### FAZ 3 — SDK Entegrasyonları (Hafta 5-8)
1. **AppLovin MAX** — reklam mediation
2. **Unity IAP** — store ürünleri
3. **Firebase** — cloud save, analytics, crashlytics
4. **Google UMP** — GDPR consent
5. **iOS ATT** — tracking prompt

### FAZ 4 — Soft Launch (Hafta 7-10)
1. **Canada/Australia** test pazarı
2. **KPI izleme** — D1 > %45, CPI < $0.35
3. **Balance tuning** — RemoteConfig ile zorluk ayarı
4. **Bug fix** — crash log'larına göre

### FAZ 5 — Global Release (Hafta 10+)
1. **Tüm diller** — 15 dil yerelleştirme tamamlansın
2. **Star Pass** — sezonluk içerik
3. **CI/CD** — Unity Cloud Build + Fastlane
4. **ASO optimizasyonu** — icon, screenshot, video

---

*Bu rapor, game_plan.md spec'i ile mevcut durum karşılaştırması sonucu hazırlanmıştır.*
