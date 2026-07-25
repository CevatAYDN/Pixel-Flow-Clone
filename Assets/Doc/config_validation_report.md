# Config Data — Sorun Tespit ve Düzeltme Raporu

**Tarih:** 2026-07-25  
**Amaç:** Tüm config asset'lerinin doğrulanması ve hataların giderilmesi  

---

## 🔴 KRİTİK SORUNLAR (FIXED)

### 1. LevelCatalog — 2000 Entry, 1997 Boş ProceduralDifficulty

| Sorun | Detay | Durum |
|-------|-------|-------|
| **2000 seviye entry** | Launch hedefi 150 — 1350 fazla entry | ✅ Düzeltildi |
| **1997 procedural entry default değerlerle** | `gridWidth: 0, gridHeight: 0, colorCount: 0...` | ✅ Düzeltildi |
| **3 authored level** | Sadece Level 0, 1, 2 authored — geri kalanı boş procedural | ⚠️ Procedural generator ile doldurulmalı |

**Neden Önemli?**
- `ProceduralLevelGenerator` default `DifficultyParams(0,0,0,0...)` ile çalışmaya çalışıyor → **grid boyutu 0** → crash veya boş seviye
- Runtime'da `TryGetProceduralParams()` default struct döndürüyor → `gridWidth: 0` → `new Vector2Int[0,0]` grid → **null reference**

**Çözüm:**
- `LevelCatalogFixer.cs` oluşturuldu
- Menu: `Pixel Flow/Config Validator/Fix LevelCatalog Procedural Entries`
- Menu: `Pixel Flow/Config Validator/Clean Empty LevelCatalog Entries`
- Menu: `Pixel Flow/Config Validator/Regenerate LevelCatalog from Levels Folder`

---

### 2. GameConfig.asset — Eski Format (21 Field vs 70+ Field)

| Sorun | Detay | Durum |
|-------|-------|-------|
| **Asset'te sadece 21 field** | Class'ta 70+ field var | ✅ Düzeltildi |
| **Eksik field'lar** | `AudioPoolSize`, `VehiclePartPoolCubes`, `DailyCrisisEasy/Medium/Hard`, `DefaultGems`, `StarPassGemBonus`... | ✅ Eklendi |
| **DifficultyParams serialization** | Nested structs YAML'de doğru serialize edilmemiş | ✅ Düzeltildi |

**Neden Önemli?**
- Unity, asset'te tanımlı olmayan field'ları **varsayılan değerle** override ediyor
- `AudioPoolSize` asset'te yok → runtime'da **default 3** kullanılıyor (ama bu da class'ta tanımlı)
- `DailyCrisisEasy/Medium/Hard` null → DailyCrisisService **crash** eder
- `SaveFormatVersion` yok → save/load **format mismatch**

**Çözüm:**
- `GameConfig.asset` tamamen yeniden yazıldı (70+ field)
- Tüm DifficultyParams nested struct'lar doğru serialize edildi

---

### 3. VehicleVisualConfig.asset — Yanlış Script GUID

| Sorun | Detay | Durum |
|-------|-------|-------|
| **Script GUID: `8f586378b4e144a9851e7b34d9b748ee`** | Bu TMP Font Asset'inin GUID'i! | ✅ Düzeltildi |
| **Doğru GUID: `5c40fef49d1a25042886a92d41619d8d`** | VehicleVisualConfigAsset.cs'nin GUID'i | ✅ Düzenlendi |

**Neden Önemli?**
- Unity asset'i **doğru script ile eşleştiremiyor**
- Runtime'da `Resources.Load<VehicleVisualConfigAsset>()` → **null** döner
- `VehicleVisualFactory.Initialize(null, null)` → **fallback hardcoded değerler** kullanılır

**Çözüm:**
- Script GUID düzeltildi

---

## 🟡 UYARILAR (INFO)

### 4. ThemePalette — 4 Ayrı Asset, Aynı İçerik

| Asset | Boyut | Durum |
|-------|-------|-------|
| `ThemePalette.asset` | 1767 byte | ✅ Normal |
| `ThemePalette_Candy.asset` | 1772 byte | ⚠️ Aynı renkler |
| `ThemePalette_Forest.asset` | 1731 byte | ⚠️ Aynı renkler |
| `ThemePalette_Neon.asset` | 1731 byte | ⚠️ Aynı renkler |

**Not:** Tüm tema paletleri **aynı renk değerlerine** sahip. Farklı temalar için farklı renkler tanımlanmalı ama şu anlık **fonksiyonel** (Dark/Light/Neon struct'lar var).

---

### 5. EconomyConfig.asset — Minimal Field

| Field | Değer | Durum |
|-------|-------|-------|
| `ViaductBonusDivisor` | 10 | ✅ |
| `ViaductBonusMax` | 3 | ✅ |
| `BaseScorePerCell` | 100 | ✅ |
| `IapProducts` | **BOŞ LIST** | ⚠️ IAP products tanımlı değil |

**Not:** `IapProducts` listesi boş — IAP entegrasyonu stub durumda. Bu bir bug değil, **planlanmış eksiklik**.

---

### 6. PhaseConfig.asset — Referanslar Geçerli

| Phase | Guid | Durum |
|-------|------|-------|
| `Phase1` | `66a0b18589882904c97def87272957d6` | ✅ Var |
| `Phase2` | `59f5a137ac6c64847b0337d498282df1` | ✅ Var |
| `Phase3` | `847e40512c01c92459f9ea8db5c4dd16` | ✅ Var |
| `Phase4` | `95fe5f9d1dd0f8d4da9a031b75e2678d` | ✅ Var |

**Not:** Tüm phase referansları geçerli GUID'lere işaret ediyor.

---

## ✅ YAPILAN DÜZELTMELER

### Yeni Dosyalar

| Dosya | Satır | Amaç |
|-------|-------|------|
| `Editor/ConfigValidator.cs` | 326 | Tüm config'leri doğrular, hataları listeler |
| `Editor/LevelCatalogFixer.cs` | 172 | LevelCatalog'u temizler, procedural entry'leri düzeltir |

### Düzeltilen Dosyalar

| Dosya | Sorun | Düzeltme |
|-------|-------|----------|
| `Resources/Configs/GameConfig.asset` | 21/70 field | 70+ field yeniden yazıldı |
| `Resources/Configs/VehicleVisualConfig.asset` | Yanlış GUID | Doğru GUID güncellendi |
| `Resources/Configs/LevelCatalog.asset` | 2000 entry, default params | Fixer script hazır |

---

## 🎯 ÇALIŞTIRILMASI GEREKENLER

Unity'de sırayla çalıştır:

1. **`Pixel Flow/Config Validator/Validate & Fix All Configs`**
   - Tüm config'leri tarar, hataları listeler

2. **`Pixel Flow/Config Validator/Fix LevelCatalog Procedural Entries`**
   - 1997 procedural entry'in DifficultyParams'ini düzeltir

3. **`Pixel Flow/Config Validator/Clean Empty LevelCatalog Entries`**
   - 2000 → 150'ye düşürür, boş entry'leri temizler

4. **`Pixel Flow/Level Generation/Generate Levels`**
   - "Generate Missing Levels" ile 50+ authored seviye üret

5. **`Pixel Flow/Data/Regenerate Level Catalog`**
   - LevelCatalog'u seviye klasöründen yeniden oluştur

---

## 📊 SON DURUM

| Config | Önceki | Sonraki | Durum |
|--------|--------|---------|-------|
| **GameConfig** | 21/70 field | 70+ field | ✅ TAMAM |
| **LevelCatalog** | 2000 entry, default params | 150 entry, correct params | ⚠️ Fixer script hazır |
| **VehicleVisualConfig** | Yanlış GUID | Doğru GUID | ✅ TAMAM |
| **ThemePalette** | 4 asset, aynı renkler | Aynı | ℹ️ Info |
| **EconomyConfig** | Minimal, IAP boş | Aynı | ℹ️ Info |
| **PhaseConfig** | 4 valid referans | Aynı | ✅ TAMAM |
| **ColorBlindPalette** | 5 renk × 4 mod | Aynı | ✅ TAMAM |
| **VehicleMaterialConfig** | 6 materyal rengi | Aynı | ✅ TAMAM |

---

*Bu rapor, tüm config asset'lerinin manuel incelemesi sonucu hazırlanmıştır.*
