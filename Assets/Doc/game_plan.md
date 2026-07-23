# Color Jam 3D — Hybrid-Casual Traffic Puzzle & Collection Spec

**Version:** 6.0.0 (Global Production & Scalability Masterpiece Edition)  
**Date:** Temmuz 2026  
**Engine:** Unity 2023 LTS / Unity 6 + URP (Vibrant 3D Toy/Pastel Shader System)  
**Architecture:** Nexus Core Full MVCS (SignalBus, CommandPool, ReactiveModel, RemoteConfig, DI, Strict SOLID)  
**Target Market:** iOS / Android (Portrait Mode), Geniş Casual Mobil Kitle (35+ Kadın & General Casual Puzzle Player)

---

## 1. Proje Özeti & 100M+ İndirme Stratejisi

**Color Jam 3D** — Dokunsal (tactile), 3D oyuncak estetiğinde renk eşleştirme ve trafik akış bulmacası + Araç & Durak Koleksiyonu meta oyunu.

### 1.1 Tek Cümle Vizyonu
*Çapraz olmayan yolları parmağınla bağla, renkli sevimli araçların zıplayarak hedefe ulaşmasını sağla, yolculardan kazandığın bozuk paralarla eğlenceli araç skin'lerini (Dondurma Arabası, Canavar Kamyon, Altın Otobüs) aç!*

### 1.2 Ticari & Pazar Odaklı Dönüşüm İlkeleri
1. **Yüksek CTR Görsel Stil (Ad Conversion):** Dark-mode/neon kaplama kaldırılarak TikTok/Facebook reklamlarında en yüksek tıklamayı (CTR) alan parlak, pastel zeminli, 3D tatlı/plastik oyuncak araç estetiğine geçildi.
2. **Anında Dopamin & Sıfır Sürtünme:** Oyuncuyu bekleten karmaşık kriz panelleri ve zamanlayıcılar kaldırıldı. Kaza durumunda araçlar komik şekilde zıpar (bouncy physics) ve 1-tap geri alma ile akış bozulmaz.
3. **En Kolay & En Eğlenceli Meta (Araç & Durak Koleksiyonu):** Karmaşık şehir inşa etme yükü yerine geliştirilmesi en kolay, oyuncu için en tatmin edici koleksiyon meta'sı eklendi. Oyuncu kazandığı paralarla araç skin'leri açar ve **bir sonraki bulmacada yollarda kendi açtığı renkli araçları izler**.
4. **CPI-First Hızlı Test:** 16 haftalık kapalı kodlama yerine **1. haftada 3 günlük MVP ile $500 CPI reklam testi** yapılır (Hedef CPI < $0.35).

---

## 2. MEVCUT EDİTÖR ALTYAPISININ GENİŞLETİLMESİ VE SIFIR MOCK/HARDCODE POLİTİKASI

Projemizde halihazırda bulunan gelişmiş editör altyapısı ([`PixelFlowSetupWindow.cs`](file:///c:/Users/wwwla/Documents/Github/Pixel-Flow-Clone/Assets/Scripts/PixelFlow/Editor/PixelFlowSetupWindow.cs) ve [`LevelDataEditor.cs`](file:///c:/Users/wwwla/Documents/Github/Pixel-Flow-Clone/Assets/Scripts/PixelFlow/Editor/LevelDataEditor.cs)) **aynen korunacak, asla sıfırdan parallel yeni editör yazılmayacak**, mevcut sekme ve kontrol yapısı yeni Hybrid-Casual gereksinimlerine göre genişletilecektir.

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                 PixelFlowSetupWindow (Mevcut Kontrol Merkezi)                │
│                                                                             │
│ [🕹️ Oyun Kontrol] [🔍 Sahne Tanılama] [🎮 Seviye Stüdyosu] [🧩 Toplu Çözücü]│
│ [📦 Data Yöneticisi] [💰 Ekonomi & Isı Haritası] [🔬 Nexus] [⚡ Performans] │
│                                                                             │
│ ─── YENİ EKLENECEK HİBRİT-CASUAL SEKMELER (MEVCUT YAPIYA ENTEGRE) ────────  │
│ 🎨 [Garaj & Skin Stüdyosu]   📺 [Reklam & Monetization]   🛡️ [Pre-Build Validator]│
└──────────────────────────────────────┬──────────────────────────────────────┘
                                       │
┌──────────────────────────────────────▼──────────────────────────────────────┐
│                    ScriptableObject Asset Database (Data Only)               │
└─────────────────────────────────────────────────────────────────────────────┘
```

### 2.1 Mevcut Editör Araçlarının Genişletilme Planı (No Parallel Systems)

#### A. Mevcut `LevelDataEditor.cs` Genişletmesi:
- **Hali hazırda var olanlar:** Visual Grid Editor (Node, Path, Bridge, Obstacle, OneWay, Eraser fırçaları), Otomatik Zorluk Puanlaması ve Solver Testi.
- **Genişletme:** Seviye bazlı 3D Toy Teması önizlemesi ve Zıplayan Araç (Bouncy Physics) parametre ayarları bu inspector'a eklenecektir.

#### B. Mevcut `PixelFlowSetupWindow.cs` Genişletmesi (8 Sekmeye Ek 3 Yeni Sekme):
1. 🎨 **Sekme 9: Garaj & Skin Stüdyosu:** 3D Araç skin'i tanımlama, renk ailesi atama, 3D model ve ses efektlerini Editör Play Mode başlatmadan canlı önizleme.
2. 📺 **Sekme 10: Reklam & Monetization Ayarları:** Interstitial seviye barajları, Rewarded Ad ödül miktarları ve placement ID yönetimi.
3. 🛡️ **Sekme 11: Pre-Build Validator:** Build almadan önce tüm ScriptableObject referanslarını ve seviye çözülebilirliğini doğrulayan kontrol paneli.

### 2.2 Strict Zero-Hardcode & Zero-Mock Data Policy
1. **Sıfır Hardcoded Veri (Zero Hardcoded Data):** C# kodları içinde hiçbir sabit sayı veya string (`const`, `literal`) BULUNAMAZ. Tüm değerler veri varlıklarından (`ScriptableObject`) okunur.
2. **Sıfır Mock / Dummy Veri (Zero Mock Data):** Test veya geliştirme amacıyla kod içine geçici/çöp varsayılan veri gömülmesi KESİNLİKLE YASAKTIR.
3. **Sıfır Sessiz Fallback (Zero Silent Fallbacks):** Eksik veri olduğunda kodun varsayılan bir sayıya sığınması (`if (config == null) return 10.0f;`) YASAKTIR. Veri eksikse Play-Mode ve Build anında sert hata (`DataValidationException`) fırlatılır.

---

## 3. GLOBAL MAĞAZA ÇIKIŞI & PRODÜKSİYON STANDARTLARI (GLOBAL RELEASE READY)

Oyunun App Store & Google Play'de küresel ölçekte (Global Launch) sorunsuz yayınlanması için gereken tüm prodüksiyon sistemleri tam olarak tanımlanmıştır:

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                     GLOBAL RELEASE PRODUCTION INTEGRATION                   │
│                                                                             │
│  ┌───────────────────┐  ┌───────────────────┐  ┌─────────────────────────┐  │
│  │ GDPR / UMP Consent│  │ iOS ATT Prompt    │  │ Crashlytics / Diagnostic│  │
│  │ (EU Compliance)   │  │ (Ad Attribution)  │  │ (Exception Tracking)    │  │
│  └─────────┬─────────┘  └─────────┬─────────┘  └────────────┬────────────┘  │
│            │                      │                         │               │
│            ▼                      ▼                         ▼               │
│  ┌───────────────────┐  ┌───────────────────┐  ┌─────────────────────────┐  │
│  │ Cloud Save Sync   │  │ Store Review API  │  │ Push Notifications      │  │
│  │ (Cross-Device)    │  │ (In-App Rating)   │  │ (Retention Boost)       │  │
│  └───────────────────┘  └───────────────────┘  └─────────────────────────┘  │
└─────────────────────────────────────────────────────────────────────────────┘
```

### 3.1 Yasal Gizlilik & İzin Uyum Sistemi (Privacy & Compliance)
- **iOS App Tracking Transparency (ATT):** iOS 14.5+ cihazlarda ad attribution ve kişiselleştirilmiş reklamlar için oyuncudan izin isteyen native ATT akışı.
- **GDPR & CCPA Consent (Google UMP):** Avrupa Birliği (AB) ve Kaliforniya oyuncuları için ilk açılışta Google User Messaging Platform (UMP) üzerinden Rıza Yönetimi (Consent Management).

### 3.2 Hata & Crash İzleme (Silent Crash Diagnostics)
- **Firebase Crashlytics / Sentry Integration:** Canlıda 100M+ oyuncudan gelen unhandled exception'lar arka planda sessizce loglanır. Oyuncuya asla kırmızı Unity konsol hatası gösterilmez.

### 3.3 Mağaza İçi Değerlendirme Akışı (In-App Review API)
- Oyuncu Seviye 10 veya 15'i tamamladığında Apple StoreKit / Android In-App Review API otomatik tetiklenir ve oyundan çıkmadan 5 yıldız vermesi sağlanır.

### 3.4 Bulut Kayıt & Cihazlar Arası Senkronizasyon (Cloud Save)
- Oyuncu cihaz değiştirdiğinde veya oyunu silip yüklediğinde satın aldığı IAP'ler, açtığı skin'ler ve seviye ilerlemesi Google Play Games / Apple Game Center Cloud Save üzerinden otomatik geri yüklenir.

### 3.5 Yerel Bildirimler (Local Push Notifications)
- D1/D7/D30 elde tutmayı (retention) artırmak için:
  - "Günlük Giriş Ödülün Hazır! 🎁" (24 saat sonra)
  - "Yoğun Trafik Etkinliği Başladı! 2x Para Kazan! 🚗💨" (48 saat sonra)

---

## 4. Mimari İlkeler & Nexus Core Altyapısı (Enterprise Clean Architecture)

- **SignalBus:** Strongly-typed decoupled sinyaller.
- **CommandPool:** Zero GC için yeniden kullanılabilir command havuzu.
- **ReactiveModel:** UniRx/Observable ile reaktif model takibi.
- **Dependency Injection (DI):** Servisler DI container ile inject edilir.
- **SOLID İlkeleri:** SRP, OCP, ISP, DIP %100 uygulanır.

---

## 5. Performans & Güvenlik

- **Performans:** `< 1KB GC/frame`, `GenericObjectPool` ile obje havuzlaması, 60 FPS hedefi.
- **Güvenlik:** AES-256 şifreli JSON kayıt sistemi (`SaveService`).

---

## 6. LiveOps, Analitik & Global Diller

- **LiveOps:** Daily Login Streak (7. gün VIP Skin), Rush Hour Event (24s Çift Para) ve Star Pass.
- **Analitik & Churn:** Terk edilen seviyelerin tespiti ve zorluğunun RemoteConfig ile anında düşürülmesi.
- **15 Dil & RTL:** Data-driven CSV yerelleştirme ve Arapça/İbranice RTL desteği.

---

## 7. Pazara Giriş & CPI-First Yol Haritası

```
FAZ 0: 3 Günlük MVP (Hafta 1)  ──►  CPI Testi ($500 Bütçe)  ──► Gate: CPI < $0.35 ?
                                                                  │
                                                        ┌─────────┴─────────┐
                                                        ▼                   ▼
                                                     [EVET]               [HAYIR]
                                                        │                   │
                                                        ▼                   ▼
                                                FAZ 1: İçerik & ASMR     Projeyi İptal Et /
                                                (Hafta 2-4) D1 > %45     Pivot Yap
                                                        │
                                                        ▼
                                                FAZ 2: Meta & Soft Launch
                                                (Hafta 5-8) LTV > CPI
                                                        │
                                                        ▼
                                                FAZ 3: Global Scale (100M+)
```

---

## 8. OYUN MANTIĞI & CORE LOOP (Game Design Core)

### 8.1 Core Loop (Tek Oturum Döngüsü — 60-90 saniye)

```
┌────────────────────────────────────────────────────────────────────┐
│                        CORE LOOP (per level)                        │
│                                                                    │
│  1. SEVİYE BAŞLAR                                                  │
│     └─ Grid ekrana gelir, renkli araçlar başlangıç node'larında    │
│        bekler, hedef duraklar (renkli) görünür                     │
│                                                                    │
│  2. OYUNCU YOL ÇİZER (Drag Input)                                  │
│     └─ Parmakla başlangıç → hedef arası yol bağlanır              │
│     └─ Yollar çapraz OLAMAZ (sadece yatay/dikey)                  │
│     └─ Yollar birbirinin ÜZERİNDEN geçemez (çakışma yok)          │
│     └─ Köprü (Bridge) node'ları üstten geçişe izin verir          │
│                                                                    │
│  3. ARAÇLAR HAREKET EDER (Auto-Run)                                │
│     └─ Yol çizilince araç otomatik ilerler                        │
│     └─ Doğru renkte durağa ulaşırsa → "POP!" patlama + coin      │
│     └─ Yanlış renk / çıkmaz → araç komik şekilde zıplar (bouncy)  │
│                                                                    │
│  4. ÇAKIŞMA / KAZA DURUMU                                          │
│     └─ İki araç aynı node'a gelirse → bouncy collision animasyonu │
│     └─ Oyuncu 1-tap "Geri Al" (Undo) ile son hamleyi geri alır    │
│     └─ Ceza YOK, sadece zaman kaybı (sürtünmesiz tasarım)         │
│                                                                    │
│  5. SEVİYE TAMAMLANIR                                              │
│     └─ Tüm araçlar doğru durağa ulaştı → Yıldız (1-3★)           │
│     └─ Coin kazanılır (temel + bonus + speed bonus)                │
│     └─ Sonraki seviye otomatik açılır                             │
└────────────────────────────────────────────────────────────────────┘
```

### 8.2 Input Modeli (Tek El, Portrait)

| Input | Aksiyon |
|-------|---------|
| Drag (başlangıç node → hedef) | Yol çizimi |
| Tap (çizilmiş yol) | Yolu sil / geri al |
| Tap (Undo butonu) | Son hamleyi geri al |
| Tap (Play butonu) | Tüm araçları aynı anda hareket ettir |
| Long-press (araç) | Araç skin önizleme |

### 8.3 Zorluk Mekanikleri (Progressive Complexity)

| Seviye Aralığı | Mekanik | Örnek |
|----------------|---------|-------|
| 1-5 | Tek araç, düz yol | Tutorial: kırmızı araç → kırmızı durak |
| 6-15 | 2-3 araç, yol çakışması | İki yol kesişmemeli |
| 16-30 | Köprü (Bridge) | Üstten/alttan geçiş |
| 31-50 | Tek Yön (OneWay) | Sadece belirtilen yönde ilerleme |
| 51-75 | Engel (Obstacle) | Yol çizilemeyen bloklar |
| 76-100 | Zamanlı kapılar | Kapı açılınca araç geçmeli |
| 100+ | Kombinasyon | Tüm mekanikler birleşik |

### 8.4 Meta Loop (Uzun Vadeli Döngü)

```
CORE LOOP (60-90s)
    │
    ▼ Coin kazan
    │
META: GARAJ & KOLEKSİYON
    │
    ├─ Yeni araç skin'i aç (Dondurma Arabası, Canavar Kamyon, vb.)
    ├─ Durak skin'i aç (Pastane, Lunapark, vb.)
    ├─ Tema aç (Pastel Orman, Neon Şehir, vb.)
    │
    ▼ Açılan skin'ler BİR SONRAKİ bulmacada görünür
    │
    ▼ Görsel ödül → motivasyon → tekrar core loop
```

### 8.5 Oturum Yapısı (Session Design)

- **Hedef oturum süresi:** 3-5 dakika (3-4 seviye)
- **Interstitial reklam:** Her 3 seviyede bir (RemoteConfig ile ayarlanabilir)
- **Rewarded ad touchpoint'leri:**
  - Seviye sonunda 2x coin
  - Ekstra Undo hakkı (sınırsız undo yerine 3 ücretsiz + rewarded)
  - Günlük sandık 2x açma
- **Oturum sonu tetikleyici:** Enerji sistemi YOK (sürtünmesiz), bunun yerine "bir sonraki seviye" merakı ve koleksiyon ilerlemesi

---

## 9. EK-A: ECONOMY DESIGN DOC

### 9.1 Para Birimleri

| Para Birimi | Kazanım | Harcama | Enflasyon Kontrolü |
|-------------|---------|---------|-------------------|
| **Coin** (yumuşak) | Seviye tamamlama, günlük giriş, rewarded ad | Skin açma, tema açma | Sink: skin fiyatları seviye ile artar |
| **Gem** (sert) | Seviye 3★, achievement, IAP | Nadir skin, speed-up, özel sandık | Sınırlı free kaynak, IAP ile hızlanır |
| **Ticket** (etkinlik) | Etkinlik görevleri | Etkinlik mağazası | Etkinlik bitince sıfırlanır |

### 9.2 Coin Source/Sink Dengesi

```
SOURCE (günlük ~800-1200 coin, aktif oyuncu):
  ├─ Seviye tamamlama: 50-150 coin (zorluğa göre)
  ├─ 3★ bonus: +25 coin
  ├─ Speed bonus: +10 coin (par süresi altında)
  ├─ Günlük giriş: 100-500 coin (streak)
  └─ Rewarded ad (2x): +100 coin

SINK:
  ├─ Common skin: 500 coin
  ├─ Rare skin: 1.500 coin
  ├─ Epic skin: 4.000 coin
  ├─ Legendary skin: 10.000 coin
  ├─ Tema paketi: 2.000 coin
  └─ Durak skin'i: 800 coin
```

**Denge kuralı:** Oyuncu günde 1 skin (common) açabilmeli. Epic skin ~4 gün, Legendary ~10 gün. IAP ile bu süre kısalır ama asla "pay-to-win" olmaz (skin'ler sadece kozmetik).

### 9.3 IAP Kataloğu

| Ürün | Fiyat (USD) | İçerik | Tip |
|------|-------------|--------|-----|
| No Ads | $2.99 | Tüm interstitial kaldırılır | Non-consumable |
| Starter Pack | $0.99 | 1000 coin + 50 gem + 1 rare skin | Non-consumable (tek sefer) |
| Coin Pack S | $1.99 | 2.500 coin | Consumable |
| Coin Pack M | $4.99 | 7.500 coin | Consumable |
| Coin Pack L | $9.99 | 20.000 coin | Consumable |
| Gem Pack S | $2.99 | 100 gem | Consumable |
| Gem Pack M | $7.99 | 350 gem | Consumable |
| Star Pass (Sezonluk) | $4.99 | 30 gün premium ödül track | Subscription-like |
| VIP Bundle | $14.99 | No Ads + 5000 coin + 200 gem + 1 legendary | Non-consumable |

### 9.4 Ad Placement Map

| Placement | Format | Frekans | Tetikleyici |
|-----------|--------|---------|-------------|
| Level Complete | Interstitial | Her 3 seviyede | Seviye tamamlanma ekranı |
| 2x Coin | Rewarded | Sınırsız | Seviye sonu coin ekranı |
| Extra Undo | Rewarded | Seviye başına 2 | Undo hakkı bitince |
| Daily Chest x2 | Rewarded | Günde 1 | Günlük sandık ekranı |
| Revive (nadiren) | Rewarded | Seviye başına 1 | Tüm araçlar sıkışınca |
| Lucky Wheel | Rewarded | Günde 3 | Ana menü |

**Kural:** İlk 5 seviyede HİÇBİR reklam gösterilmez (onboarding koruması).

---

## 10. EK-B: CONTENT PIPELINE DOC

### 10.1 Launch İçerik Hedefi

| Kategori | Adet | Not |
|----------|------|-----|
| Seviye (launch) | 150 | 50 kolay + 50 orta + 50 zor |
| Araç skin'i | 20 | 8 common, 6 rare, 4 epic, 2 legendary |
| Durak skin'i | 10 | Tema bazlı |
| Tema paketi | 4 | Pastel, Orman, Deniz, Gece |
| Etkinlik şablonu | 3 | Rush Hour, Collection Frenzy, Star Race |

### 10.2 Zorluk Eğrisi (Difficulty Pacing)

```
Zorluk
  ▲
  │         ╱╲      ╱╲        ╱╲
  │       ╱    ╲  ╱    ╲    ╱    ╲
  │     ╱        ╲        ╱        ╲
  │   ╱            ╲    ╱            ╲
  │ ╱                ╲╱                ╲
  └──────────────────────────────────────► Seviye
  1   10   20   30   40   50  ...  150

  Her "tepe" yeni mekanik tanıtımı
  Her "vadi" rahatlama / mastery seviyesi
```

**Pacing kuralı:** Her 5 zor seviyeden sonra 2 kolay "nefes" seviyesi. Yeni mekanik tanıtımı her 15 seviyede bir.

### 10.3 Post-Launch İçerik Takvimi

| Periyot | İçerik |
|---------|--------|
| Haftalık | 10 yeni seviye + 1 mini etkinlik |
| 2 Haftalık | 1 yeni araç skin'i |
| Aylık | 1 tema paketi + 1 büyük etkinlik |
| Sezonluk (8 hafta) | Star Pass sezonu + 1 legendary skin |

### 10.4 Seviye Üretim Pipeline'ı

```
1. LevelDataEditor ile grid tasarımı (5-10 dk/seviye)
2. Otomatik Solver testi (çözülebilirlik doğrulama)
3. Zorluk puanı otomatik hesaplama
4. Playtest (3 kişi minimum)
5. RemoteConfig ile zorluk ayarı (live tuning)
```

---

## 11. EK-C: TECHNICAL BUDGET DOC

### 11.1 App Size Budget

| Kategori | Bütçe | Not |
|----------|-------|-----|
| Base APK/IPA | < 80 MB | İlk indirme |
| Addressables (ilk açılış) | < 40 MB | Ek asset download |
| Toplam ilk deneyim | < 120 MB | CPI optimizasyonu için kritik |
| Sezonluk içerik (OTA) | < 20 MB/sezon | Addressables ile |

### 11.2 Performance Budget

| Metrik | Hedef | Ölçüm |
|--------|-------|-------|
| FPS | 60 (mid-range cihaz) | Galaxy A14 / iPhone SE 2 |
| GC Alloc/frame | < 1 KB | Unity Profiler |
| Memory (peak) | < 350 MB | Low-end: 2GB RAM cihaz |
| Load time (cold start) | < 3 saniye | Splash → gameplay |
| Level transition | < 0.5 saniye | Seviye geçişi |
| Battery drain | < %8/saat | Continuous play |
| Thermal throttling | Yok (30 dk test) | Mid-range cihaz |

### 11.3 Minimum Device Spec

| Platform | Minimum | Hedef |
|----------|---------|-------|
| Android | Android 8.0, 2GB RAM, Adreno 505 | Android 12+, 4GB RAM |
| iOS | iPhone 8, iOS 14 | iPhone 12+, iOS 16 |

### 11.4 Teknik Altyapı Kararları

| Sistem | Karar | Gerekçe |
|--------|-------|---------|
| Asset delivery | Unity Addressables | OTA içerik, app size kontrolü |
| Backend | Firebase (Auth, Firestore, RemoteConfig, Crashlytics) | Hızlı entegrasyon, scale |
| Analytics | Firebase Analytics + Adjust | Attribution + event tracking |
| Ad Mediation | MAX (AppLovin) veya ironSource | Bidding, yüksek eCPM |
| Cloud Save | Firebase Firestore | Cross-device, gerçek zamanlı |
| CI/CD | Unity Cloud Build + Fastlane | Otomatik build + deploy |
| Deep Linking | Adjust + Unity Deep Link | UA attribution, deferred |
| Offline mode | Tam oynanabilir | Sync sonraki açılışta |
| Anti-cheat | Server-side coin validation (Firestore rules) | Economy koruma |
| Key management | Android Keystore / iOS Keychain | AES key asla plaintext saklanmaz |

### 11.5 Offline Play Spec

- Tüm seviyeler offline oynanabilir
- Coin/skin kazanımı lokal kaydedilir (AES-256)
- Online olunca Firestore'a sync edilir
- Rewarded ad offline'da gösterilmez (ödül verilmez)
- Interstitial offline'da gösterilmez (UX koruması)

---

## 12. EK-D: UA PLAYBOOK (User Acquisition)

### 12.1 Creative Stratejisi

| Format | Adet (launch) | İterasyon |
|--------|---------------|-----------|
| Video (15s) | 10 varyasyon | Haftalık 2 yeni |
| Video (30s) | 5 varyasyon | 2 haftada 1 yeni |
| Playable ad | 3 varyasyon | Aylık 1 yeni |
| Static (screenshot) | 15 varyasyon | Haftalık 3 yeni |

**Creative temaları:**
1. "Yol çiz, araçlar zıplasın" (core mechanic showcase)
2. "Bu seviyeyi çözebilir misin?" (challenge/puzzle hook)
3. "Koleksiyonunu tamamla" (collection/meta hook)
4. "Fail → funny bounce → retry" (humor/ASMR hook)

### 12.2 ASO Stratejisi

| Element | Strateji |
|---------|----------|
| App name | "Color Jam 3D: Traffic Puzzle" |
| Subtitle (iOS) | "Draw Roads, Match Colors" |
| Keywords | color puzzle, traffic jam, car game, brain teaser |
| Icon | Parlak pastel zemin + 3D araç + %20 kontrast |
| Screenshots | İlk 3: gameplay (çizim anı), Son 2: koleksiyon/garaj |
| Video preview | 15s core loop + bouncy collision + coin pop |

### 12.3 LTV Modeli (Hedef)

| Gün | LTV (USD) | Kaynak |
|-----|-----------|--------|
| D0 | $0.02 | Interstitial + ilk session |
| D7 | $0.12 | Rewarded + interstitial |
| D30 | $0.35 | IAP conversion (%2) + ad revenue |
| D90 | $0.55 | Whale IAP + sustained ad |
| D180 | $0.70 | Long-tail |

**Gate:** LTV D30 > CPI ($0.35) → scale. LTV D30 < CPI → optimize veya pivot.

### 12.4 Kanal Stratejisi

| Kanal | Bütçe Oranı | Hedef |
|-------|-------------|-------|
| Meta (FB/IG) | %40 | Broad casual kitle |
| TikTok | %30 | Genç casual, viral creative |
| Google UAC | %20 | Intent-based, search |
| Unity Ads / AppLovin | %10 | Cross-promo, gaming kitle |

---

## 13. EK-E: KPI DASHBOARD SPEC

### 13.1 Hedef KPI'lar

| Metrik | Hedef | Kritik Eşik |
|--------|-------|-------------|
| D1 Retention | > %45 | < %35 = pivot |
| D7 Retention | > %20 | < %15 = sorun |
| D30 Retention | > %10 | < %7 = churn riski |
| ARPDAU | > $0.04 | < $0.02 = monetizasyon sorunu |
| Session Length | > 6 dk | < 3 dk = core loop sorunu |
| Sessions/Day | > 3 | < 2 = habit sorunu |
| Level Completion Rate | > %85 | < %70 = zorluk sorunu |
| Crash-Free Rate | > %99.5 | < %99 = acil fix |
| Store Rating | > 4.5★ | < 4.0 = review bomb riski |
| CPI (blended) | < $0.35 | > $0.50 = creative sorunu |
| ROAS D7 | > %30 | < %20 = scale durdur |
| IAP Conversion | > %2 | < %1 = fiyat/ürün sorunu |

### 13.2 RemoteConfig ile Live Tuning Parametreleri

| Parametre | Varsayılan | Tuning Aralığı |
|-----------|-----------|----------------|
| interstitial_frequency | 3 (her N seviye) | 2-6 |
| coin_multiplier | 1.0 | 0.5-3.0 |
| rewarded_undo_limit | 2 | 1-5 |
| first_ad_level | 6 (ilk reklam seviyesi) | 5-10 |
| difficulty_modifier | 1.0 | 0.7-1.3 |
| daily_chest_coins | 100 | 50-300 |
| event_coin_multiplier | 2.0 | 1.5-3.0 |

### 13.3 Analytics Event Map

| Event | Tetikleyici | Parametreler |
|-------|-------------|--------------|
| level_start | Seviye başlar | level_id, difficulty, attempt |
| level_complete | Seviye biter | level_id, stars, time, coins |
| level_fail | Araçlar sıkışır | level_id, fail_reason, attempt |
| undo_used | Geri alma | level_id, undo_count |
| skin_unlocked | Skin açılır | skin_id, rarity, currency |
| ad_impression | Reklam gösterilir | placement, format |
| ad_rewarded | Rewarded izlendi | placement, reward_amount |
| iap_purchase | Satın alma | product_id, price |
| session_start | App açılır | session_count, last_session_gap |
| daily_claim | Günlük ödül | streak_day, reward |
| event_join | Etkinliğe katılma | event_id |

---

## 14. EK-F: COMPLIANCE & LEGAL CHECKLIST

| Gereksinim | Durum | Implementasyon |
|------------|-------|----------------|
| GDPR (AB) | Zorunlu | Google UMP SDK, consent before ad |
| CCPA (Kaliforniya) | Zorunlu | "Do Not Sell" butonu, UMP |
| COPPA (13 yaş altı) | Zorunlu | Age gate ilk açılışta, < 13 = no ad tracking |
| iOS ATT | Zorunlu | ATT prompt before IDFA access |
| Data Deletion (GDPR Art.17) | Zorunlu | Settings > "Hesabımı Sil" → Firestore wipe |
| Age Rating | Zorunlu | ESRB: E / PEGI: 3 (şiddet yok) |
| Google Play Data Safety | Zorunlu | Form: veri toplama = analytics + ads |
| Apple Privacy Nutrition Label | Zorunlu | App Store Connect > Privacy |
| Korea PIPL | Koşullu | Korea target ise ek consent |
| China regulations | Hariç | China launch planı yok (şu an) |

---

## 15. EK-G: TECHNICAL IMPLEMENTATION BLUEPRINT (AI-Ready Architecture Spec)

> **Bu bölüm, bir yapay zeka geliştiricinin kodu sıfır ambiguity ile yazabilmesi için tasarlanmıştır.**
> Tüm class isimleri, namespace'ler, dosya yolları, bağımlılık ilişkileri ve implementasyon sırası kesin olarak tanımlanmıştır.

### 15.1 Nexus Core Altyapısı (GameContainer/Nexus)

**Konum:** `GameContainer/Nexus/Packages/com.nexus.core/Runtime/`
**Namespace:** `Nexus.Core`

#### 15.1.1 Temel Arayüzler (Bir AI'nın bilmesi gereken TÜM interface'ler)

```csharp
// ─── COMMAND PATTERN ───
// Signal alan senkron command
public interface ICommand<in TSignal> where TSignal : struct
{
    void Execute(TSignal signal);
}

// Signal alan asenkron command
public interface IAsyncCommand<in TSignal> where TSignal : struct
{
    ValueTask ExecuteAsync(TSignal signal, CancellationToken ct);
}

// Pool'dan yeniden kullanılacak command'lar bunu implement eder
public interface IResettable
{
    void Reset();
}

// ─── SERVICE LIFECYCLE ───
public interface INexusService
{
    ValueTask InitializeAsync(CancellationToken ct);
    void OnDispose();
}

// ─── REACTIVE MODEL ───
public interface IReactiveModel
{
    ValueTask OnBind(CancellationToken ct);
}

// ─── VIEW (MVCS) ───
public interface IView
{
    void Bind(IContext context);
    void Unbind();
}

// ─── CONTEXT LIFECYCLE (Oyunun DI registration noktası) ───
public interface IContextLifecycle
{
    void OnConfigure(IContextBuilder builder);
    ValueTask OnInitializeAsync(CancellationToken ct);
    ValueTask OnStartAsync(CancellationToken ct);
    void OnDispose();
}

// ─── SIGNAL BUS ───
public interface ISignalBus
{
    void Fire<T>(T signal) where T : struct;
    ValueTask FireAsync<T>(T signal) where T : struct;
    void FireThreadSafe<T>(T signal) where T : struct;
    void FireNextFrame<T>(T signal) where T : struct;
    ISignalSubscription Subscribe<T>(Action<T> handler) where T : struct;
    ISignalSubscription SubscribeAsync<T>(Func<T, CancellationToken, ValueTask> handler) where T : struct;
}

// ─── GAME STATE MACHINE ───
public interface IGameState
{
    ValueTask OnEnterAsync(object args, CancellationToken ct);
    ValueTask OnExitAsync(CancellationToken ct);
    void OnTick(float deltaTime);
}

public interface IGameStateMachine
{
    IGameState CurrentState { get; }
    void RegisterState<TState>(TState state) where TState : class, IGameState;
    Task ChangeStateAsync<TState>(object args = null) where TState : class, IGameState;
}
```

#### 15.1.2 DI Registration Kuralları (IContextBuilder)

```csharp
// Model bağlama (singleton, IReactiveModel lifecycle alır)
builder.BindReactiveModel<IInterface, Implementation>();

// Service bağlama (singleton, INexusService lifecycle alır)
builder.BindService<IInterface, Implementation>();

// Lazy service (ilk erişimde oluşturulur)
builder.BindLazyService<IInterface, Implementation>();

// Signal → Command bağlama
builder.BindSignal<MySignal>().To<MyCommand>();
builder.BindCommand<MySignal, MyCommand>(ExecutionMode.Sequential, priority: 0);

// ScriptableObject instance bağlama
builder.BindInstance(myScriptableObject);

// Düz interface bağlama (lifecycle yok)
builder.Bind<IInterface, Implementation>();
```

#### 15.1.3 Nexus Core Hazır Servisleri

| Servis | Interface | Görev |
|--------|-----------|-------|
| `EconomyService` | `IEconomyService` | Coin/Gem bakiye yönetimi |
| `AdService` | `IAdService` | Interstitial/Rewarded reklam |
| `IapService` | `IIapService` | In-App Purchase |
| `AnalyticsService` | `IAnalyticsService` | Event tracking |
| `AudioService` | `IAudioService` | Ses efektleri & müzik |
| `LocalizationService` | `ILocalizationService` | Çoklu dil |
| `ObjectPoolService` | `IObjectPoolService` | Generic obje havuzu |
| `ProgressionService` | `IProgressionService` | Seviye ilerleme |
| `WindowManager` | `IWindowManager` | UI panel yönetimi |
| `FeedbackService` | `IFeedbackService` | Haptic + visual feedback |
| `HapticService` | `IHapticService` | Titreşim |
| `TickService` | `ITickService` | Frame tick dağıtımı |
| `LoggerService` | `ILoggerService` | Loglama |
| `EncryptedStorageService` | `IPlayerPrefsService` | AES-256 şifreli kayıt |

---

### 15.2 PixelFlow Oyun Katmanı (Assets/Scripts/PixelFlow)

**Namespace:** `PixelFlow` (alt namespace'ler: `.Data`, `.Models`, `.Services`, `.Commands`, `.Signals`, `.Views`)

#### 15.2.1 Dosya Yapısı (Mevcut — Kesin)

```
Assets/Scripts/PixelFlow/
├── GameBootstrapper.cs          ← MonoBehaviour, Root'u bekler, DI resolve eder
├── GameContextLifecycle.cs      ← IContextLifecycle, TÜM DI binding'ler burada
├── PixelFlow.asmdef
│
├── Data/                        ← ScriptableObject'ler & struct'lar (SADECE VERİ)
│   ├── LevelData.cs             ← Seviye tanımı (grid, nodes, obstacles, solutions)
│   ├── GameConfig.cs            ← Global oyun ayarları
│   ├── EconomyConfigAsset.cs    ← Ekonomi balance değerleri
│   ├── ThemePaletteAsset.cs     ← Tema renk paletleri
│   ├── VehicleSkinConfig.cs     ← Araç skin tanımları
│   ├── VehicleMaterialConfigAsset.cs ← Araç materyal renkleri
│   ├── ColorBlindPaletteAsset.cs ← Renk körlüğü paleti
│   ├── LevelCatalogAsset.cs     ← Merkezi seviye kataloğu
│   ├── LevelPack.cs             ← Seviye paketi
│   ├── PhaseDefinitionAsset.cs  ← Faz tanımı
│   ├── GddColorPalette.cs       ← GDD renk paleti
│   └── DataValidationException.cs ← Sert hata (zero fallback)
│
├── Models/                      ← Reactive Model'ler (IReactiveModel)
│   ├── GameStateModel.cs        ← GameState enum + state machine
│   ├── GridModel.cs             ← Grid hücreleri, path'ler, çakışma
│   ├── LevelModel.cs            ← Aktif seviye verisi
│   ├── GameSessionModel.cs      ← Oturum (score, time, viaducts)
│   ├── ProgressModel.cs         ← Genel ilerleme (unlock'lar)
│   ├── InventoryModel.cs        ← Açılan skin'ler
│   ├── SettingsModel.cs         ← Oyuncu ayarları
│   ├── SoundModel.cs            ← Ses durumu
│   ├── TutorialModel.cs         ← Tutorial state
│   ├── DailyCrisisModel.cs      ← Günlük kriz
│   ├── HintModel.cs             ← İpucu state
│   ├── VehicleInstance.cs       ← Runtime araç instance
│   ├── GridSnapshot.cs          ← Grid anlık görüntü (undo/save)
│   └── CloudSaveManager.cs      ← Cloud save logic
│
├── Services/                    ← INexusService implementasyonları
│   ├── PathService.cs           ← Yol çizim/validasyon
│   ├── VehicleSimulator.cs      ← Araç hareket simülasyonu
│   ├── VehicleMovementService.cs ← Araç hareket detayları
│   ├── GridInputService.cs      ← Dokunmatik input → grid koordinat
│   ├── LevelLoaderService.cs    ← Seviye yükleme
│   ├── LevelProgressionService.cs ← Seviye sıralaması
│   ├── ObstacleService.cs       ← Engel yönetimi
│   ├── HintService.cs           ← İpucu sistemi
│   ├── PowerUpService.cs        ← Güç-up'lar
│   ├── ScoreCalculator.cs       ← Skor hesaplama
│   ├── BouncyCollisionHandler.cs ← Zıplama fiziği
│   ├── CameraController.cs      ← Kamera kontrolü
│   ├── GameHistoryService.cs    ← Undo/Redo geçmişi
│   ├── GameplayTimerService.cs  ← Süre takibi
│   ├── DailyCrisisService.cs    ← Günlük kriz
│   ├── CrisisAdService.cs       ← Kriz reklamı
│   ├── TutorialDriver.cs        ← Tutorial akışı
│   ├── RuntimePathSolver.cs     ← BFS/DFS path solver
│   ├── ProceduralLevelGenerator.cs ← Prosedürel seviye
│   ├── ProceduralAudioFactory.cs ← Prosedürel ses
│   ├── GridStateSerializer.cs   ← Save/Load serialization
│   ├── LocalEconomyValidator.cs ← Anti-cheat (local)
│   └── GlobalRelease/           ← Global mağaza servisleri
│       ├── PrivacyComplianceService.cs
│       ├── SilentCrashDiagnosticsService.cs
│       ├── InAppReviewService.cs
│       └── LocalNotificationService.cs
│
├── Commands/                    ← ICommand<TSignal> implementasyonları
│   ├── ProcessInputCommand.cs   ← Input → grid işlemi
│   ├── CheckWinConditionCommand.cs ← Kazanma kontrolü
│   ├── LoadLevelCommand.cs      ← Seviye yükleme
│   ├── StartSimulationCommand.cs ← Simülasyon başlat
│   ├── PauseSimulationCommand.cs ← Simülasyon durdur
│   ├── UndoCommand.cs           ← Geri al
│   ├── RedoCommand.cs           ← İleri al
│   ├── SaveProgressCommand.cs   ← İlerleme kaydet
│   ├── PlaceViaductCommand.cs   ← Viyadük yerleştir
│   ├── UseHintCommand.cs        ← İpucu kullan
│   ├── ClearJamCommand.cs       ← Sıkışıklık temizle
│   ├── RainbowRoadCommand.cs    ← Gökkuşağı yol
│   ├── ChangeThemeCommand.cs    ← Tema değiştir
│   ├── InterstitialAdCommand.cs ← Reklam göster
│   └── LevelVictoryCompositeHandler.cs ← Zafer composite
│
├── Signals/                     ← struct Signal tanımları
│   ├── InputInteractionSignal.cs
│   ├── CheckWinConditionSignal.cs
│   ├── LoadLevelSignal.cs
│   ├── StartSimulationSignal.cs
│   ├── PauseSimulationSignal.cs
│   ├── LevelCompletedSignal.cs
│   ├── LevelFailedSignal.cs
│   ├── GridUpdatedSignal.cs
│   ├── CrashDetectedSignal.cs
│   ├── UndoSignal.cs
│   ├── RedoSignal.cs
│   ├── PlaceViaductSignal.cs
│   ├── RequestHintSignal.cs
│   ├── ProgressUpdatedSignal.cs
│   ├── CollectionSignals.cs
│   ├── FlowSignals.cs
│   └── ... (diğerleri)
│
├── Views/                       ← IView + Mediator (MVCS View katmanı)
│   ├── GridView.cs + GridMediator.cs
│   ├── HUDView.cs + HUDMediator.cs
│   ├── MainMenuView.cs + MainMenuMediator.cs
│   ├── GarageView.cs + GarageMediator.cs
│   ├── SettingsView.cs + SettingsMediator.cs
│   ├── SplashView.cs + SplashMediator.cs
│   ├── DailyCrisisView.cs + DailyCrisisMediator.cs
│   ├── TutorialView.cs
│   ├── VehicleVisualFactory.cs
│   ├── VehiclePartPool.cs
│   ├── CellView.cs
│   ├── ConfettiView.cs
│   ├── BloomFlashView.cs
│   └── SoundHandlerView.cs / ThemeHandlerView.cs
│
└── Editor/                      ← Editör araçları (Runtime'a dahil DEĞİL)
    ├── PixelFlowSetupWindow.cs  ← Ana editör penceresi (11 sekme)
    ├── LevelDataEditor.cs       ← Visual grid editör
    ├── PreBuildDataValidator.cs ← Build öncesi doğrulama
    └── Tests/                   ← Editör testleri
```

#### 15.2.2 GameState Machine (Kesin Tanım)

```csharp
namespace PixelFlow.Models
{
    public enum GameState
    {
        Boot,           // Uygulama açıldı, Root bekleniyor
        Loading,        // Asset/seviye yükleniyor
        MainMenu,       // Ana menü / Hub
        Playing,        // Oyuncu yol çiziyor (input aktif)
        Simulating,     // Araçlar hareket ediyor (input pasif)
        Paused,         // Duraklatıldı / Kriz paneli
        LevelCompleted, // Seviye başarıyla bitti
        LevelFailed     // Seviye başarısız (nadiren)
    }
}
```

**İzin Verilen Geçişler:**

```
Boot → Loading → MainMenu
Boot → MainMenu (direkt)
Boot → Playing (save restore)
MainMenu → Playing
Playing ↔ Paused
Playing → Simulating
Simulating → Playing (araçlar durdu, tekrar çizim)
Simulating → LevelCompleted
Simulating → LevelFailed
LevelCompleted → MainMenu
LevelCompleted → Playing (sonraki seviye)
LevelFailed → Playing (retry)
LevelFailed → MainMenu
```

#### 15.2.3 Data Model Şemaları (ScriptableObject)

**LevelData** (`Assets/Scripts/PixelFlow/Data/LevelData.cs`):

```csharp
[CreateAssetMenu(fileName = "LevelData", menuName = "PixelFlow/LevelData")]
public class LevelData : ScriptableObject
{
    public int levelIndex;
    public int width;                    // Grid genişliği (min 3)
    public int height;                   // Grid yüksekliği (min 3)
    public ToyThemeType toyTheme;        // 3D tema
    public BouncyPhysicsConfig bouncyPhysics; // Zıplama parametreleri
    public List<GridNode> initialNodes;  // Başlangıç node'ları (2 per color)
    public List<PathSolution> solutions; // Otor çözümleri
    public List<Vector2Int> bridgePositions; // Köprü pozisyonları
    public int viaductLimit;             // Viyadük hakkı
    public bool requireFullGridCoverage; // Tüm grid kaplanmalı mı
    public int flowScoreThreshold;       // Kazanma eşiği
    public int difficultyScore;          // Otomatik zorluk puanı
    public StarCriteria stars;           // Yıldız kriterleri
    public TutorialEvent tutorialEvent;  // Tutorial tetikleyici
    public List<ObstacleData> obstacles; // Engeller
    public List<OneWayCell> oneWayCells; // Tek yön hücreleri
}
```

**GridNode** (struct):

```csharp
public struct GridNode
{
    public Vector2Int position;  // Grid koordinatı (x, y)
    public ColorType color;      // Red, Green, Blue, Yellow, Purple
    public ShapeType shape;      // Renk körlüğü şekli
    public NodeType type;        // Home, Office, Hospital, School, Park, Mall
    public bool isSource;        // true=başlangıç, false=hedef
    public int pairIndex;        // Çift indeksi
}
```

**CellData** (runtime, GridModel içinde):

```csharp
public class CellData
{
    public CellState State;       // Empty, Node, Path, Bridge, Obstacle
    public ColorType Color;       // Hücrenin ana rengi
    public byte PathColorsMask;   // Bitmask: hangi renkler bu hücreden geçiyor
    public bool HasViaduct;       // Viyadük var mı
}
```

#### 15.2.4 Signal → Command Bağlama Haritası (Mevcut)

| Signal (struct) | Command | Görev |
|-----------------|---------|-------|
| `InputInteractionSignal` | `ProcessInputCommand` | Dokunma → yol çiz/sil |
| `CheckWinConditionSignal` | `CheckWinConditionCommand` | Kazanma kontrolü |
| `LoadLevelSignal` | `LoadLevelCommand` | Seviye yükle |
| `StartSimulationSignal` | `StartSimulationCommand` | Araçları hareket ettir |
| `PauseSimulationSignal` | `PauseSimulationCommand` | Simülasyonu durdur |
| `UndoSignal` | `UndoCommand` | Son hamleyi geri al |
| `RedoSignal` | `RedoCommand` | Geri alınan hamleyi ileri al |
| `PlaceViaductSignal` | `PlaceViaductCommand` | Viyadük yerleştir |
| `RequestHintSignal` | `UseHintCommand` | İpucu göster |
| `ClearJamSignal` | `ClearJamCommand` | Sıkışıklık temizle |
| `ActivateRainbowRoadSignal` | `RainbowRoadCommand` | Gökkuşağı yol power-up |
| `ChangeThemeSignal` | `ChangeThemeCommand` | Tema değiştir |
| `LevelCompletedSignal` | `SaveProgressCommand` | İlerlemeyi kaydet |
| `RequestInterstitialAdSignal` | `InterstitialAdCommand` | Reklam göster |

#### 15.2.5 Reactive Model Bağımlılık Grafiği

```
GameBootstrapper (MonoBehaviour)
    │
    ▼ Resolve from DI Container
    │
    ├── IGameStateModel ← GameStateModel (state geçişleri)
    ├── IGridModel ← GridModel (hücreler, path'ler, çakışma)
    ├── ILevelModel ← LevelModel (aktif LevelData)
    ├── IGameSessionModel ← GameSessionModel (score, time, viaducts)
    ├── IProgressModel ← ProgressModel (unlock'lar, yıldızlar)
    ├── IInventoryModel ← InventoryModel (açılan skin'ler)
    ├── ISettingsModel ← SettingsModel (oyuncu tercihleri)
    ├── ISoundModel ← SoundModel (ses açık/kapalı)
    ├── ITutorialModel ← TutorialModel (tutorial state)
    ├── IDailyCrisisModel ← DailyCrisisModel (günlük kriz)
    └── IHintModel ← HintModel (ipucu durumu)
```

---

### 15.3 MVCS Katman Kuralları (Bir AI'nın UYMASI GEREKEN kurallar)

#### KURAL 1: Signal = struct, Command = class

```csharp
// Signal: SADECE veri taşır, logic YOK
namespace PixelFlow.Signals
{
    public struct MyNewSignal
    {
        public int LevelId;
        public ColorType Color;
    }
}

// Command: SADECE logic, state TUTMAZ (pool'dan yeniden kullanılır)
namespace PixelFlow.Commands
{
    public class MyNewCommand : ICommand<MyNewSignal>, IResettable
    {
        [Inject] private IGridModel _grid;
        [Inject] private ISignalBus _signalBus;

        public void Execute(MyNewSignal signal)
        {
            // Logic burada
            _signalBus.Fire(new GridUpdatedSignal());
        }

        public void Reset() { /* Pool reuse için state temizle */ }
    }
}
```

#### KURAL 2: Model = state, Service = davranış

```csharp
// Model: State tutar, IReactiveModel implement eder
namespace PixelFlow.Models
{
    public interface IMyModel { int Value { get; } void SetValue(int v); }

    public class MyModel : IMyModel, IReactiveModel
    {
        public int Value { get; private set; }
        public void SetValue(int v) => Value = v;
        public ValueTask OnBind(CancellationToken ct) => default;
    }
}

// Service: Davranış sağlar, INexusService implement eder
namespace PixelFlow.Services
{
    public interface IMyService { void DoWork(); }

    public class MyService : IMyService, INexusService
    {
        [Inject] private IMyModel _model;

        public void DoWork() { /* ... */ }
        public ValueTask InitializeAsync(CancellationToken ct) => default;
        public void OnDispose() { }
    }
}
```

#### KURAL 3: View = SADECE görsel, Mediator = köprü

```csharp
// View: MonoBehaviour, SADECE UI günceller, logic YOK
namespace PixelFlow.Views
{
    public class MyView : MonoBehaviour, IView
    {
        public void Bind(IContext context) { /* Signal subscribe */ }
        public void Unbind() { /* Unsubscribe */ }
        public void UpdateScore(int score) { /* UI güncelle */ }
    }

    // Mediator: View ↔ Model/Service köprüsü
    public class MyMediator : MonoBehaviour
    {
        [Inject] private IMyModel _model;
        // View eventlerini command'lara çevirir
    }
}
```

#### KURAL 4: Data = ScriptableObject, Zero Hardcode

```csharp
// YANLIŞ — YASAK:
const int MAX_COINS = 100;
if (config == null) return 10.0f;

// DOĞRU:
[Inject] private EconomyConfigAsset _config;
int maxCoins = _config.MaxCoinsPerLevel;
// config null ise → DataValidationException fırlatılır (build'de)
```

#### KURAL 5: DI Registration = GameContextLifecycle.OnConfigure

```csharp
// Yeni bir servis eklerken:
// 1. Interface + Implementation yaz
// 2. GameContextLifecycle.OnConfigure'a ekle:
builder.BindService<IMyService, MyService>();

// Yeni bir model eklerken:
builder.BindReactiveModel<IMyModel, MyModel>();

// Yeni bir signal→command eklerken:
builder.BindSignal<MySignal>().To<MyCommand>();
```

#### KURAL 6: Editör araçları oyunu yönetir

- Seviye tasarımı → `LevelDataEditor.cs` (Visual Grid Editor)
- Skin tanımlama → `PixelFlowSetupWindow` Sekme 9 (Garaj & Skin Stüdyosu)
- Reklam ayarları → `PixelFlowSetupWindow` Sekme 10
- Build doğrulama → `PreBuildDataValidator.cs` (Sekme 11)
- **Asla yeni parallel editör yazma, mevcut yapıya sekme/inspector ekle**

---

### 15.4 Core Algoritma Spesifikasyonları

#### 15.4.1 Yol Geçerlilik Kontrolü (PathService)

```
GİRDİ: startPos (Vector2Int), endPos (Vector2Int), color (ColorType)
ÇIKTI: bool isValid, List<Vector2Int> path

KURALLAR:
1. Yol SADECE yatay/dikey (çapraz YASAK)
2. Yol Obstacle hücresinden GEÇEMEZ
3. Yol başka bir rengin path'ini KESMEMELİ (çakışma = crash)
4. OneWay hücresinde sadece allowedDirection'da ilerleyebilir
5. Bridge hücresinde iki farklı renk aynı hücreyi kullanabilir (üst/alt)
6. Yol grid sınırları dışına ÇIKAMAZ

ALGORİTMA: BFS (Breadth-First Search)
- Queue<(Vector2Int pos, List<Vector2Int> path)>
- Her adımda 4 komşu kontrol edilir (Up, Down, Left, Right)
- Ziyaret edilen hücreler HashSet<Vector2Int> ile takip edilir
- Hedefe ulaşınca path döner
```

#### 15.4.2 Çakışma Tespiti (Crash Detection)

```
TETİKLEYİCİ: Her yol çiziminden sonra (ProcessInputCommand)

ALGORİTMA:
1. Tüm hücreleri tara
2. Her hücrede PathColorsMask kontrol et
3. Eğer PathColorCount >= 2 VE HasViaduct == false → CRASH
4. CrashDetectedSignal ateşle (Position, ColorA, ColorB)
5. GameState → Paused (kriz paneli gösterilir)

ÇÖZÜM SEÇENEKLERİ (oyuncu seçer):
- Undo: Son hamleyi geri al
- Viaduct: Viyadük yerleştir (viaductLimit > 0 ise)
```

#### 15.4.3 Kazanma Koşulu (CheckWinConditionCommand)

```
TETİKLEYİCİ: Simülasyon bitince (tüm araçlar durunca)

KONTROLLER:
1. Tüm source node'ların karşılık gelen target'a ulaştı mı?
   - Her renk için: source → target path var mı?
2. requireFullGridCoverage == true ise:
   - Tüm Empty hücreler bir path tarafından kaplanmış mı?
3. flowScoreThreshold kontrolü:
   - Mevcut flow score >= threshold mı?

SONUÇ:
- BAŞARILI → LevelCompletedSignal (stars, coins, time)
- BAŞARISIZ → LevelFailedSignal (reason)
```

#### 15.4.4 Araç Hareket Simülasyonu (VehicleSimulator)

```
TETİKLEYİCİ: StartSimulationSignal

ALGORİTMA (her frame, TickService.OnTick):
1. Her VehicleInstance için:
   a. Mevcut path'te bir sonraki node'a ilerle
   b. Hız = VehicleSkinConfig.MoveSpeed (data-driven)
   c. Hedef node'a ulaşınca:
      - Doğru renk durak → "POP!" → coin → araç yok olur
      - Yanlış renk → bouncy bounce → araç geri döner
   d. İki araç aynı node'a gelirse:
      - BouncyCollisionHandler tetiklenir
      - Her iki araç zıplar (BounceForce, BounceDamping)
      - CrashDetectedSignal ateşlenir

FİZİK PARAMETRELERİ (LevelData.bouncyPhysics):
- BounceForce: 4.5f (zıplama kuvveti)
- BounceDamping: 0.75f (sönümleme)
- SquishFactor: 0.35f (ezilme/büzülme)
```

#### 15.4.5 Runtime Path Solver (RuntimePathSolver)

```
AMAÇ: Bir seviyenin çözülebilir olduğunu doğrulamak (editör + runtime)

ALGORİTMA: Backtracking DFS
1. Tüm source-target çiftlerini listele
2. Her çift için BFS ile olası path'leri bul
3. Çakışma kontrolü ile backtracking yap
4. Tüm çiftler çakışmasız path bulursa → SOLVABLE
5. Herhangi bir çift path bulamazsa → UNSOLVABLE

KULLANIM:
- Editör: LevelDataEditor "Solver Test" butonu
- Runtime: ProceduralLevelGenerator (ürettiği seviye çözülebilir mi?)
- Pre-Build: PreBuildDataValidator (tüm seviyeler çözülebilir mi?)
```

---

### 15.5 Implementasyon Sırası (Dependency Graph)

```
FAZ 1: TEMEL ALTYAPI (Zaten mevcut ✓)
  ├── Nexus Core package (GameContainer/Nexus)
  ├── GameContextLifecycle (DI bindings)
  ├── GameBootstrapper (boot sequence)
  └── Data/ (ScriptableObject'ler)

FAZ 2: CORE GAMEPLAY (Zaten mevcut ✓)
  ├── Models/ (GridModel, GameStateModel, LevelModel, GameSessionModel)
  ├── Services/ (PathService, VehicleSimulator, GridInputService)
  ├── Commands/ (ProcessInput, CheckWinCondition, LoadLevel)
  ├── Signals/ (tüm signal struct'ları)
  └── Views/ (GridView, HUDView)

FAZ 3: META & KOLEKSİYON (Kısmen mevcut)
  ├── InventoryModel (mevcut ✓)
  ├── GarageView + GarageMediator (mevcut ✓)
  ├── VehicleSkinConfig (mevcut ✓)
  ├── CollectionSignals (mevcut ✓)
  └── YENİ: Skin unlock command, tema satın alma, durak skin'i

FAZ 4: MONETİZASYON (Kısmen mevcut)
  ├── AdService (Nexus Core ✓)
  ├── IapService (Nexus Core ✓)
  ├── EconomyService (Nexus Core ✓)
  ├── InterstitialAdCommand (mevcut ✓)
  ├── CrisisAdService (mevcut ✓)
  └── YENİ: Rewarded ad placement'ları, IAP ürün kataloğu, Star Pass

FAZ 5: LİVEOPS & RETENTİON (Kısmen mevcut)
  ├── DailyCrisisService (mevcut ✓)
  ├── ProgressionService (Nexus Core ✓)
  ├── LocalNotificationService (mevcut ✓)
  └── YENİ: Rush Hour event, Star Pass season, haftalık challenge

FAZ 6: GLOBAL RELEASE (Mevcut ✓)
  ├── PrivacyComplianceService ✓
  ├── SilentCrashDiagnosticsService ✓
  ├── InAppReviewService ✓
  ├── LocalNotificationService ✓
  └── CloudSaveManager ✓

FAZ 7: POLISH & OPTİMİZASYON
  ├── YENİ: Addressables entegrasyonu
  ├── YENİ: CI/CD pipeline (Unity Cloud Build + Fastlane)
  ├── YENİ: A/B test framework (RemoteConfig)
  └── YENİ: Performance profiling & optimization pass
```

---

### 15.6 Yeni Özellik Ekleme Reçetesi (AI Recipe)

> **Bir AI geliştirici yeni bir özellik eklerken TAM OLARAK bu adımları izler:**

#### Örnek: "Rush Hour Event" (2x Coin Etkinliği) Ekleme

**Adım 1 — Data (ScriptableObject):**
```
Dosya: Assets/Scripts/PixelFlow/Data/RushHourConfigAsset.cs
Namespace: PixelFlow.Data
İçerik: [CreateAssetMenu] class, event süresi, coin multiplier, tetikleyici koşullar
Konum: Resources/Configs/RushHourConfig.asset
```

**Adım 2 — Signal:**
```
Dosya: Assets/Scripts/PixelFlow/Signals/RushHourSignals.cs
Namespace: PixelFlow.Signals
İçerik: struct RushHourStartedSignal { int DurationSeconds; float Multiplier; }
         struct RushHourEndedSignal { }
```

**Adım 3 — Model (state gerekiyorsa):**
```
Dosya: Assets/Scripts/PixelFlow/Models/RushHourModel.cs
Namespace: PixelFlow.Models
İçerik: interface IRushHourModel + class RushHourModel : IReactiveModel
         State: IsActive, RemainingTime, Multiplier
```

**Adım 4 — Service (davranış):**
```
Dosya: Assets/Scripts/PixelFlow/Services/RushHourService.cs
Namespace: PixelFlow.Services
İçerik: interface IRushHourService + class : INexusService
         Timer yönetimi, event başlatma/bitirme
```

**Adım 5 — Command (logic):**
```
Dosya: Assets/Scripts/PixelFlow/Commands/StartRushHourCommand.cs
Namespace: PixelFlow.Commands
İçerik: class : ICommand<RushHourStartedSignal>, IResettable
         [Inject] model, service, signalBus
```

**Adım 6 — DI Registration:**
```
Dosya: Assets/Scripts/PixelFlow/GameContextLifecycle.cs
Ekle:
  builder.BindReactiveModel<IRushHourModel, RushHourModel>();
  builder.BindService<IRushHourService, RushHourService>();
  builder.BindSignal<RushHourStartedSignal>().To<StartRushHourCommand>();
  // ScriptableObject yükle:
  var rushConfig = Resources.Load<RushHourConfigAsset>("Configs/RushHourConfig");
  if (rushConfig == null) throw new DataValidationException("...");
  builder.BindInstance(rushConfig);
```

**Adım 7 — View (UI gerekiyorsa):**
```
Dosya: Assets/Scripts/PixelFlow/Views/RushHourBannerView.cs
Namespace: PixelFlow.Views
İçerik: MonoBehaviour, IView, Subscribe<RushHourStartedSignal> → banner göster
```

**Adım 8 — Editör (opsiyonel):**
```
Dosya: PixelFlowSetupWindow'a yeni sekme veya mevcut sekmeye alan ekle
İçerik: Event parametrelerini editörden ayarlama
```

**Adım 9 — Test:**
```
Dosya: Assets/Scripts/PixelFlow/Editor/Tests/RushHourTests.cs
Namespace: PixelFlow.Editor.Tests
İçerik: NUnit test, MockContext ile signal fire → model state doğrula
```

---

### 15.7 Test Stratejisi

```
TEST KATMANLARI:

1. Editör Testleri (Assets/Scripts/PixelFlow/Editor/Tests/)
   - Framework: NUnit + NexusTestHarness
   - Kapsam: Command logic, Model state, Service behavior
   - Mock: MockContext, InMemoryPlayerPrefsService, StubAudioService
   - Çalıştırma: Unity Test Runner (Edit Mode)

2. PlayMode Testleri (Assets/Scripts/PixelFlow/Tests/PlayMode/)
   - Kapsam: View binding, sahne yükleme, araç hareketi
   - Çalıştırma: Unity Test Runner (Play Mode)

3. Nexus Core Testleri (GameContainer/Nexus/Packages/com.nexus.core/Tests/)
   - Kapsam: SignalBus, CommandPool, DI, Lifecycle
   - Çalıştırma: Unity Test Runner

TEST YAZMA KURALI:
- Her yeni Command için minimum 1 test
- Her yeni Model state değişimi için 1 test
- Her yeni Service metodu için 1 test
- MockContext kullan, gerçek sahne YÜKLEME
```

---

### 15.8 Naming Convention & Dosya Kuralları

| Tip | Naming | Dosya Yeri | Örnek |
|-----|--------|-----------|-------|
| Signal | `{Name}Signal` (struct) | `Signals/` | `RushHourStartedSignal` |
| Command | `{Action}Command` (class) | `Commands/` | `StartRushHourCommand` |
| Model Interface | `I{Name}Model` | `Models/` | `IRushHourModel` |
| Model Class | `{Name}Model` | `Models/` | `RushHourModel` |
| Service Interface | `I{Name}Service` | `Services/` | `IRushHourService` |
| Service Class | `{Name}Service` | `Services/` | `RushHourService` |
| View | `{Name}View` | `Views/` | `RushHourBannerView` |
| Mediator | `{Name}Mediator` | `Views/` | `RushHourMediator` |
| Config SO | `{Name}ConfigAsset` | `Data/` | `RushHourConfigAsset` |
| Test | `{Name}Tests` | `Editor/Tests/` | `RushHourTests` |

---

### 15.9 Kritik Kısıtlamalar (AI'nın ASLA Yapmaması Gerekenler)

1. **ASLA** `const`, `static readonly` ile oyun değeri tanımlama → ScriptableObject kullan
2. **ASLA** `if (x == null) return defaultValue;` → `DataValidationException` fırlat
3. **ASLA** yeni parallel editör penceresi yazma → mevcut `PixelFlowSetupWindow`'a ekle
4. **ASLA** MonoBehaviour'da game logic yazma → Command/Service'e taşı
5. **ASLA** View'dan doğrudan Model'e yazma → Signal → Command → Model akışını kullan
6. **ASLA** `new` ile service/model oluşturma → DI container'dan `Resolve<T>()` kullan
7. **ASLA** `Resources.Load` ile runtime'da asset yükleme → `GameContextLifecycle.OnConfigure`'da yükle, `BindInstance` ile bağla
8. **ASLA** `GameObject.Find` kullanma → DI veya `[SerializeField]` reference kullan
9. **ASLA** `Update()` içinde heavy logic çalıştırma → `TickService` veya `ITickable` kullan
10. **ASLA** GC allocate eden pattern kullanma (her frame `new List`, string concat, LINQ) → `CommandPool`, `ObjectPoolService`, struct signal kullan

---

## 16. EK-H: NEXUS CORE İÇ YAPI & REFERENCE IMPLEMENTATION (Deep Architecture)

> **Bu bölüm, bir AI'nın mevcut sistemi BOZMADAN extend edebilmesi için Nexus Core'un iç yapısını
> ve PixelFlow'un gerçek çalışan kodunu referans olarak sunar.**

### 16.1 SignalBus İç Yapısı (Nasıl Çalışır?)

**Dosya:** `GameContainer/Nexus/Packages/com.nexus.core/Runtime/Core/SignalBus.cs`

```
SignalBus.Fire<T>(signal) çağrıldığında İÇERİDE olan:

1. REENTRANCY CHECK
   └─ AsyncLocal<int> s_stackDepth kontrolü (max 10)
   └─ Aşıldıysa → NexusReentrancyException (sonsuz signal döngüsü koruması)

2. HANDLER LOOKUP
   └─ Dictionary<Type, List<CommandHandlerInfo>> _commandHandlers
   └─ Signal type → kayıtlı command listesi (priority sıralı)
   └─ Lock-free okuma: volatile snapshot (_commandHandlersReadCopy)

3. COMMAND INSTANTIATION (Zero-GC)
   └─ CommandPoolManager.GetCommand(commandType)
   └─ Pool'dan al (varsa) veya DI container'dan yeni oluştur
   └─ Command instance'a [Inject] alanları inject edilir

4. EXECUTION
   └─ Sync: ICommand<TSignal>.Execute(signal)
   └─ Async: IAsyncCommand<TSignal>.ExecuteAsync(signal, ct)
   └─ ExecutionMode.Sequential → sırayla
   └─ ExecutionMode.Exclusive → tek seferde (diğerleri bekler)

5. POOL RETURN
   └─ CommandPool.Return(command)
   └─ IResettable.Reset() çağrılır (state temizliği)
   └─ NexusDI.ClearInjectedReferences(command) (inject alanları null'lanır)

6. SUBSCRIBER DISPATCH
   └─ Dictionary<Type, SubscriptionNode> _subscriptions (linked list per type)
   └─ Her subscriber Action<T> çağrılır
   └─ SubscriptionNode pool'dan reuse edilir (SubscriptionNodePool)

7. ERROR HANDLING
   └─ Exception yakalanır → OnUnhandledException event + Logger.LogError
   └─ IRecoveryStrategy.OnCommandFailed() çağrılır (varsa)
   └─ DefaultRecoveryStrategy: 3 retry → skip
```

**Kritik İç Detaylar:**

```csharp
// SignalBus constructor — Context tarafından oluşturulur
public SignalBus(NexusDI container, CommandPoolManager poolManager, IContext context)

// Command registration (GameContextLifecycle.OnConfigure'dan çağrılır)
public void RegisterCommand(Type signalType, Type commandType, ExecutionMode mode, int priority, bool isAsync)

// Async overflow koruması
private int _inFlightAsyncCommands;  // max 100 concurrent async command

// Thread-safe dispatch (background thread'den)
public void FireThreadSafe<T>(T signal)  // HybridQueue'ya enqueue eder, main thread'de drain

// Deferred dispatch (bir sonraki frame)
public void FireNextFrame<T>(T signal)   // HybridQueue'ya enqueue, next Update'de drain
```

---

### 16.2 CommandPool İç Yapısı (Zero-GC Nasıl Sağlanır?)

**Dosya:** `GameContainer/Nexus/Packages/com.nexus.core/Runtime/Core/CommandPool.cs`

```
CommandPool Yaşam Döngüsü:

1. OLUŞTURMA (Context init)
   └─ CommandPoolManager(container, initialSize: 4, maxSize: 64)
   └─ Her command type için ayrı pool (ConcurrentDictionary<Type, CommandPool>)
   └─ İlk oluşturulma: container.Resolve(type) → DI inject yapılır

2. GET (Signal fire anında)
   └─ Pool'da varsa → Pop() → döndür (0 alloc)
   └─ Pool boşsa → _factory() → yeni instance (nadir)

3. RETURN (Execute bittikten sonra)
   └─ Cleanup(command):
       ├─ IResettable.Reset() → mutable state temizle
       └─ NexusDI.ClearInjectedReferences(command) → [Inject] alanları null
   └─ Pool.Count < maxSize → Push (reuse için sakla)
   └─ Pool dolu → discard (GC'ye bırak)

4. STATE LEAK KORUMASI
   └─ Command type'ta mutable field var AMA IResettable yok → WARNING log
   └─ Bu, pool reuse'da eski state'in sızmasını önler
```

**CommandPoolManager API:**

```csharp
public class CommandPoolManager
{
    public CommandPoolManager(NexusDI container, int initialSize = 4, int maxSize = 64);
    public object GetCommand(Type commandType);      // Pool'dan al veya oluştur
    public void ReturnCommand(Type commandType, object command);  // Pool'a geri koy
    public void Clear();                             // Tüm pool'ları temizle
}
```

---

### 16.3 Context & DI Container İç Yapısı

**Dosya:** `GameContainer/Nexus/Packages/com.nexus.core/Runtime/Core/Context.cs`

```
Context Oluşturma Sırası (Root MonoBehaviour tarafından):

1. new Context(parent, contextData)
   ├─ NexusDI(parent?.Container)          ← DI container (hierarchical)
   ├─ Container.BindInstance(Container)   ← Self-reference
   ├─ Container.BindInstance<IContext>(this)
   ├─ CommandPoolManager(Container, 4, 64)
   ├─ SignalBus(Container, PoolManager, this)
   ├─ Container.BindInstance<ISignalBus>(bus)
   ├─ HybridQueue(bus)                    ← Thread-safe signal queue
   └─ ViewBinder(this, Container)         ← View ↔ Mediator otomatik bağlama

2. Configure(lifecycles[])
   ├─ ContextBuilder oluştur
   ├─ Her lifecycle.OnConfigure(builder) çağrılır
   │   └─ BindReactiveModel, BindService, BindSignal, BindInstance...
   ├─ Attribute scan ([SignalHandler] attribute'ları)
   └─ Strict injection ayarı

3. InitializeAsync()
   ├─ Tüm IReactiveModel.OnBind(ct) çağrılır
   ├─ Tüm INexusService.InitializeAsync(ct) çağrılır
   └─ Lazy service'ler pending queue'ya alınır

4. StartAsync()
   └─ Her lifecycle.OnStartAsync(ct) çağrılır
```

**NexusDI Injection Mekanizması:**

```csharp
// [Inject] attribute'lu alanlar/property'ler otomatik resolve edilir
public class MyCommand : ICommand<MySignal>
{
    [Inject] public IGridModel GridModel { get; set; }  // Auto-injected
    [Inject] public ISignalBus SignalBus { get; set; }  // Auto-injected

    public void Execute(MySignal signal) { /* ... */ }
}

// StrictInjection = true ise:
// - Çözülemeyen [Inject] → InvalidOperationException (silent null YOK)
// - [OptionalInject] → null kalabilir (hata vermez)

// Hiyerarşik DI:
// - Child context, parent'ın binding'lerini görebilir
// - Child'da override edilmezse parent'tan resolve edilir
```

---

### 16.4 Mediator<TView> İç Yapısı (View ↔ Signal Köprüsü)

**Dosya:** `GameContainer/Nexus/Packages/com.nexus.core/Runtime/Lifecycle/Mediator.cs`

```csharp
// Mediator base class — TÜM mediator'lar bunu extend eder
public abstract class Mediator<TView> : IMediator where TView : class
{
    protected TView View { get; private set; }        // Bağlı view instance
    protected ISignalBus SignalBus { get; private set; }  // Signal erişimi

    // Lifecycle
    public void Bind(object view, ISignalBus signalBus)  // ViewBinder çağırır
    {
        View = view as TView;
        SignalBus = signalBus;
        OnBind();  // ← Override et, subscription'ları burada yap
    }

    public void Unbind()
    {
        OnUnbind();  // ← Override et, event unsubscribe
        // Tüm Subscribe<T>() ile yapılan subscription'lar OTOMATIK dispose edilir
        View = null;
        SignalBus = null;
    }

    // Signal subscription (auto-dispose on Unbind)
    protected void Subscribe<T>(Action<T> handler) where T : struct;
    protected void SubscribeAsync<T>(Func<T, CancellationToken, ValueTask> handler) where T : struct;

    // View validity check (Unity Object destruction koruması)
    protected bool IsViewValid { get; }
    protected void ExecuteIfViewValid(Action<TView> action);
}
```

**ViewBinder Otomatik Bağlama:**

```
ViewBinder (Context içinde):
1. Sahne'de [Mediator(typeof(XMediator))] attribute'lu MonoBehaviour arar
2. XMediator instance'ı oluşturur (DI container'dan)
3. [Inject] alanlarını inject eder
4. mediator.Bind(viewInstance, signalBus) çağırır
5. View destroy edilince → mediator.Unbind() otomatik

Yani: View'da Subscribe YOK, Mediator'da Subscribe VAR.
```

---

### 16.5 REFERENCE IMPLEMENTATION: Grid Sistemi (Tam Çalışan Kod)

> **Bir AI yeni bir View/Mediator/Command yazarken BU pattern'i BİREBİR takip eder.**

#### 16.5.1 GridView (View Katmanı)

```csharp
// Dosya: Assets/Scripts/PixelFlow/Views/GridView.cs
namespace PixelFlow.Views
{
    [Mediator(typeof(GridMediator))]  // ← ViewBinder bu attribute'u okur
    public class GridView : TickableView  // TickableView = MonoBehaviour + ITickable
    {
        // EVENT'LER — Mediator bunlara subscribe olur
        public event System.Action<Vector2Int> OnGlobalPointerDown;
        public event System.Action<Vector2Int> OnGlobalPointerDrag;
        public event System.Action<Vector2Int> OnGlobalPointerUp;

        // SERIALIZED — Inspector'dan atanır
        [SerializeField] private CellView _cellPrefab;
        [SerializeField] private Transform _gridContainer;

        // INJECT — DI container'dan otomatik gelir
        [Inject] public ICameraProvider CameraProvider { get; set; }
        [Inject] public ILoggerService LoggerService { get; set; }
        [Inject, OptionalInject] public GameConfig Config { get; set; }

        // PUBLIC API — Mediator bunları çağırır
        public bool IsInitialized => _cells != null;
        public void UpdateGridVisuals(CellData[,] grid, int w, int h, ThemeType theme, Dictionary<ColorType, List<Vector2Int>> paths) { /* ... */ }
        public void UpdateDifferential(CellData[,] grid, ThemeType theme, HashSet<Vector2Int> changed, Vector2Int crashPos, HashSet<Vector2Int> stateChanged) { /* ... */ }
        public void UpdatePathVisuals(Dictionary<ColorType, List<Vector2Int>> paths, CellData[,] grid, Vector2Int crashPos, ColorType crashA, ColorType crashB) { /* ... */ }

        // TICK — Her frame çağrılır (input processing)
        protected override void OnTick(float deltaTime)
        {
            // 1. Cell animasyonları tick
            // 2. Pinch zoom
            // 3. Input state machine (GridInputService)
            // 4. Event fire: OnGlobalPointerDown/Drag/Up
        }

        // KURAL: View'da GAME LOGIC YOK
        // KURAL: View'da SignalBus.Fire() YOK (Mediator yapar)
        // KURAL: View'da Model'e erişim YOK (Mediator üzerinden)
    }
}
```

#### 16.5.2 GridMediator (Köprü Katmanı)

```csharp
// Dosya: Assets/Scripts/PixelFlow/Views/GridMediator.cs
namespace PixelFlow.Views
{
    public class GridMediator : Mediator<GridView>
    {
        // INJECT — DI container'dan
        [Inject] public IGridModel GridModel { get; set; }
        [Inject] public ISettingsModel SettingsModel { get; set; }
        [Inject] public ILoggerService Logger { get; set; }
        [Inject] public PixelFlow.Services.IAudioService AudioService { get; set; }

        // ON BIND — Subscription'lar BURADA (constructor'da DEĞİL)
        protected override void OnBind()
        {
            // Signal → Handler (auto-dispose on Unbind)
            Subscribe<GridUpdatedSignal>(HandleGridUpdated);
            Subscribe<ThemeChangedSignal>(HandleThemeChanged);
            Subscribe<ThirdColorRejectionSignal>(HandleThirdColorRejection);

            // View Event → Signal Fire
            View.OnGlobalPointerDown += HandleGlobalPointerDown;
            View.OnGlobalPointerDrag += HandleGlobalPointerDrag;
            View.OnGlobalPointerUp += HandleGlobalPointerUp;
        }

        protected override void OnUnbind()
        {
            // View event unsubscribe (Signal subscription'lar OTOMATIK dispose)
            View.OnGlobalPointerDown -= HandleGlobalPointerDown;
            View.OnGlobalPointerDrag -= HandleGlobalPointerDrag;
            View.OnGlobalPointerUp -= HandleGlobalPointerUp;
        }

        // VIEW EVENT → SIGNAL (Input → Command tetikleme)
        private void HandleGlobalPointerDown(Vector2Int pos)
        {
            // UI üzerindeyse yoksay
            if (EventSystem.current?.IsPointerOverGameObject() == true) return;
            // Signal fire → ProcessInputCommand çalışır
            SignalBus.Fire(new InputInteractionSignal { Type = InputType.PointerDown, GridPosition = pos });
        }

        // SIGNAL → VIEW UPDATE (Model değişikliği → görsel güncelleme)
        private void HandleGridUpdated(GridUpdatedSignal signal)
        {
            if (!View.IsInitialized && GridModel.Width > 0)
            {
                InitializeAndCenter();
                return;
            }
            // Differential update (sadece değişen hücreler)
            View.UpdateDifferential(GridModel.Grid, SettingsModel.CurrentTheme, _changedCells, crashPos, _stateChangedCells);
            View.UpdatePathVisuals(GridModel.Paths, GridModel.Grid, crashPos, ...);
        }

        // KURAL: Mediator'da GAME LOGIC YOK (sadece köprü)
        // KURAL: Mediator'da Model'e YAZMA YOK (Signal → Command → Model)
        // KURAL: Mediator'dan Model OKUMA serbest (görsel güncelleme için)
    }
}
```

#### 16.5.3 ProcessInputCommand (Command Katmanı)

```csharp
// Dosya: Assets/Scripts/PixelFlow/Commands/ProcessInputCommand.cs
namespace PixelFlow.Commands
{
    public class ProcessInputCommand : ICommand<InputInteractionSignal>, IResettable
    {
        // INJECT — Tüm bağımlılıklar (pool return'de null'lanır)
        [Inject] public IGridModel GridModel { get; set; }
        [Inject] public ISignalBus SignalBus { get; set; }
        [Inject] public ISoundModel SoundModel { get; set; }
        [Inject] public IPathService PathService { get; set; }
        [Inject] public IGameStateModel GameStateModel { get; set; }
        [Inject] public IGameHistoryService HistoryService { get; set; }
        [Inject] public IGameSessionModel GameSessionModel { get; set; }
        [Inject] public ILevelModel LevelModel { get; set; }
        [Inject] public ISaveThrottler SaveThrottler { get; set; }
        [Inject] public IHapticService HapticService { get; set; }
        [Inject] public IObstacleService ObstacleService { get; set; }
        [Inject] public IPlayerPrefsService PlayerPrefsService { get; set; }
        [Inject] public ILoggerService LoggerService { get; set; }

        public void Execute(InputInteractionSignal signal)
        {
            // 1. STATE GUARD — Sadece Playing/Paused'da çalışır
            var state = GameStateModel.CurrentState;
            if (state == GameState.Simulating) return;
            if (state != GameState.Playing && state != GameState.Paused) return;

            // 2. BOUNDS CHECK
            if (signal.GridPosition.x < 0 || signal.GridPosition.y < 0 ||
                signal.GridPosition.x >= GridModel.Width ||
                signal.GridPosition.y >= GridModel.Height) return;

            // 3. PAUSED STATE — Kriz çözümü (viaduct veya undo)
            if (state == GameState.Paused)
            {
                if (signal.Type == InputType.PointerDown)
                {
                    var cell = GridModel.Grid[signal.GridPosition.x, signal.GridPosition.y];
                    if (cell.PathColorCount >= 2 && !cell.HasViaduct)
                    {
                        SignalBus.Fire(new PlaceViaductSignal { Position = signal.GridPosition });
                        HapticService?.Vibrate(HapticType.Medium);
                        return;
                    }
                    else
                    {
                        GameStateModel.SetState(GameState.Playing);
                    }
                }
                else return;
            }

            // 4. PLAYING STATE — Yol çizim/silme
            if (signal.Type == InputType.PointerDown)
            {
                EnsureHistoryRecorded();  // Undo için snapshot
                // ... path çizim logic (PathService üzerinden)
            }
            else if (signal.Type == InputType.Drag)
            {
                // ... drag ile yol uzatma
            }
            else if (signal.Type == InputType.PointerUp)
            {
                // ... yol tamamlama, win condition check
                SignalBus.Fire(new CheckWinConditionSignal());
                RequestSave();  // Throttled save
            }

            // 5. GRID UPDATED — View'a bildir
            SignalBus.Fire(new GridUpdatedSignal());
        }

        public void Reset()
        {
            // Pool reuse için mutable state temizle
            // (Bu command'da mutable field yok, ama pattern gereği implement edilir)
        }

        // KURAL: Command'da MonoBehaviour referansı YOK
        // KURAL: Command'da coroutine YOK
        // KURAL: Command'da static state YOK (pool reuse)
        // KURAL: Command'dan View'a erişim YOK (Signal → Mediator → View)
    }
}
```

---

### 16.6 Save/Load Serialization Formatı

**Dosya:** `Assets/Scripts/PixelFlow/Services/GridStateSerializer.cs`

#### 16.6.1 Serialization Format

```
FORMAT: JSON (Unity JsonUtility)
STORAGE: IPlayerPrefsService (EncryptedStorageService → AES-256 şifreli dosya)
KEY: "NT_PuzzleSave_"

YAPI:
GridSaveData {
    int levelIndex          // Aktif seviye indeksi
    int width, height       // Grid boyutları
    List<CellSaveData> cells // Tüm hücreler
    List<PathSaveData> paths // Tüm yollar
    int availableViaducts   // Kalan viyadük hakkı
    int maxViaducts         // Toplam viyadük
    float elapsedTime       // Geçen süre
    int score               // Skor
    int stars               // Kazanılan yıldız
    int targetFlowScore     // Hedef flow score
    int activeColor         // Aktif renk (int)ColorType
    int lastPosX, lastPosY  // Son çizim pozisyonu
    List<int> lockedColors  // Kilitli renkler
}

CellSaveData {
    int x, y               // Pozisyon
    int state              // (int)CellState
    int color              // (int)ColorType
    byte pathsMask         // Bitmask: bit 1=Red, 2=Green, 3=Blue, 4=Yellow, 5=Purple
    bool hasViaduct        // Viyadük var mı
    int underColor         // Viyadük alt renk
    int overColor          // Viyadük üst renk
    int obstacleType       // (int)ObstacleType
}

PathSaveData {
    int color              // (int)ColorType
    List<Vector2Int> positions  // Yol pozisyonları (sıralı)
}
```

#### 16.6.2 EncryptedStorageService (AES-256 Key Yönetimi)

```
KEY GENERATION (cihaza bağlı):
1. deviceId = SystemInfo.deviceUniqueIdentifier
2. rawKeySeed = "{deviceId}_{salt}_{Application.identifier}"
3. deviceBoundKey = SHA256(rawKeySeed)  → 32 byte
4. randomSeed = RandomNumberGenerator (ilk çalıştırmada)
5. obfuscatedSeed = randomSeed XOR deviceBoundKey
6. obfuscatedSeed → PlayerPrefs'te saklanır (Base64)

ENCRYPTION:
- Algorithm: AES-256-CBC
- Key: SHA256(randomSeed + "enc") → 32 byte
- IV: Her yazmada random (dosya başına prepend)
- HMAC: SHA256(randomSeed + "hmac") → integrity check

STORAGE:
- Her key-value çifti ayrı dosya: {storageFolder}/{keyHash}.dat
- Dosya formatı: [IV (16 byte)][HMAC (32 byte)][AES-CBC ciphertext]
- In-memory cache: Dictionary<string, string> (hızlı okuma)
- Dirty tracking: HashSet<string> _dirtyKeys (toplu yazma)

CLOUD SAVE CONFLICT RESOLUTION:
1. Local save timestamp vs Cloud save timestamp karşılaştır
2. Yeni olan kazanır (last-write-wins)
3. Cloud verisi parse edilemezse → local korunur (silent fallback DEĞİL, log + koru)
4. Local save bozuksa → cloud'dan restore
5. Her ikisi de bozuksa → DataValidationException (yeni oyun başlar)
```

---

### 16.7 ScriptableObject Oluşturma & Inspector Pattern

#### 16.7.1 SO Oluşturma Reçetesi

```csharp
// 1. Class tanımı (Data/ klasöründe)
namespace PixelFlow.Data
{
    [CreateAssetMenu(fileName = "RushHourConfig", menuName = "PixelFlow/RushHourConfig")]
    public class RushHourConfigAsset : ScriptableObject
    {
        [Header("Event Timing")]
        [Tooltip("Etkinlik süresi (saniye)")]
        [Range(60, 3600)]
        public int DurationSeconds = 1800;

        [Tooltip("Coin çarpanı")]
        [Range(1.0f, 5.0f)]
        public float CoinMultiplier = 2.0f;

        [Header("Trigger Conditions")]
        [Tooltip("Minimum seviye (bu seviyeden önce event tetiklenmez)")]
        [Range(1, 150)]
        public int MinLevel = 10;

        [Tooltip("Tetikleyici: son oturumdan bu yana geçen saat")]
        [Range(12, 72)]
        public int TriggerAfterHours = 24;
    }
}

// 2. Asset oluşturma:
//    Unity Editor → Assets > Create > PixelFlow > RushHourConfig
//    Konum: Assets/Resources/Configs/RushHourConfig.asset

// 3. DI Registration (GameContextLifecycle.OnConfigure):
var rushConfig = Resources.Load<RushHourConfigAsset>("Configs/RushHourConfig");
if (rushConfig == null)
    throw new DataValidationException("Resources/Configs/RushHourConfig.asset bulunamadı!");
builder.BindInstance(rushConfig);
```

#### 16.7.2 CustomEditor Pattern (LevelDataEditor Referansı)

```csharp
// Dosya: Assets/Scripts/PixelFlow/Editor/LevelDataEditor.cs
namespace PixelFlow.Editor
{
    [CustomEditor(typeof(LevelData))]
    public class LevelDataEditor : UnityEditor.Editor
    {
        private enum EditMode { None, Node, Path, Bridge, Obstacle, OneWay, Eraser }
        private EditMode _currentMode = EditMode.Node;
        private ColorType _currentColor = ColorType.Red;
        private LevelData _data;

        private void OnEnable()
        {
            _data = (LevelData)target;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // 1. Header + Difficulty Badge
            // 2. Grid Size controls (width/height slider)
            // 3. Edit Mode toolbar (Node/Path/Bridge/Obstacle/OneWay/Eraser)
            // 4. Visual Grid (GUILayout.BeginArea + hücre butonları)
            // 5. Node Properties (color, shape, type, isSource, pairIndex)
            // 6. Solver Test button
            // 7. Validation warnings

            serializedObject.ApplyModifiedProperties();
        }

        // KURAL: CustomEditor SADECE Editor/ klasöründe
        // KURAL: Runtime koduna Editor kodu SIZMAZ (#if UNITY_EDITOR)
        // KURAL: Yeni SO için CustomEditor yazmak OPSIYONEL (default Inspector yeterli olabilir)
        // KURAL: Karmaşık SO'lar (LevelData gibi) için CustomEditor ZORUNLU
    }
}
```

---

### 16.8 Async/Await Kuralları (Unity-Specific)

```
KURAL 1: ValueTask > Task (tercih)
  - ValueTask = struct, heap alloc YOK (sync completion'da)
  - Task = class, her zaman heap alloc
  - Nexus Core tüm lifecycle'da ValueTask kullanır

KURAL 2: CancellationToken ZORUNLU
  - Her async metot CancellationToken alır
  - Context.LifetimeToken kullanılır (context dispose → cancel)
  - CancellationToken.None ASLA kullanılmaz (test hariç)

KURAL 3: Main Thread Kısıtı
  - Transform, GameObject, UI → SADECE main thread
  - SignalBus.Fire() → main thread (sync)
  - SignalBus.FireThreadSafe() → herhangi bir thread (main'e marshal)
  - SignalBus.FireAsync() → main thread'de başlar, await ile devam edebilir

KURAL 4: Fire-and-Forget Pattern
  - SignalBus.FireAsyncAndForget(signal) → beklemez, hata log'lanır
  - SafeAsyncRunner.Run(func, context) → async void DEĞİL, Task ile catch
  - ASLA "async void" kullanma (exception kaybolur)

KURAL 5: Coroutine ↔ Async Karışımı
  - GameBootstrapper: IEnumerator (Unity coroutine) — Root bekleme için
  - Nexus lifecycle: ValueTask (async/await) — modern pattern
  - YENİ kod: HER ZAMAN async/await kullan, coroutine YAZMA
  - Mevcut coroutine'leri refactor etme (çalışıyorsa dokunma)

KURAL 6: Dispose Pattern
  - CancellationTokenSource → using veya explicit Dispose
  - ISignalSubscription → Mediator.Unbind() otomatik dispose
  - INexusService.OnDispose() → Context dispose'da çağrılır
```

---

### 16.9 Addressables Convention (Faz 7)

```
KEY FORMAT:
  "{Category}/{SubCategory}/{AssetName}"
  Örnek: "Levels/Pack01/Level_001"
         "Skins/Vehicles/IceCreamTruck"
         "Themes/PastelToy/Materials"

LABEL SYSTEM:
  "level-pack-{N}"     → Seviye paketi (toplu indirme)
  "skin-{rarity}"      → Skin rarity grubu
  "theme-{name}"       → Tema paketi
  "event-{id}"         → Etkinlik asset'leri
  "base"               → İlk açılışta zorunlu (base APK'da)

DOWNLOAD FLOW:
  1. App açılır → base label'lı asset'ler hazır (APK içinde)
  2. Seviye paketi gerekince → Addressables.DownloadDependenciesAsync("level-pack-2")
  3. Progress callback → UI'da download bar gösterilir
  4. Tamamlandı → cache'e yazılır, sonraki açılışta instant

BÜTÇE:
  - Base APK: < 80 MB (tüm base label'lı asset'ler)
  - İlk açılış download: < 40 MB (level-pack-1 + theme-default)
  - Sezonluk OTA: < 20 MB (event asset'leri)
```

---

### 16.10 Nexus Core'u Extend Etme Kuralları

```
YAPILABİLİR (AI bunları yapabilir):
✅ Yeni Signal struct tanımlama (PixelFlow.Signals)
✅ Yeni Command class yazma (PixelFlow.Commands)
✅ Yeni Model + Interface yazma (PixelFlow.Models)
✅ Yeni Service + Interface yazma (PixelFlow.Services)
✅ Yeni View + Mediator yazma (PixelFlow.Views)
✅ GameContextLifecycle.OnConfigure'a yeni binding ekleme
✅ Yeni ScriptableObject + CustomEditor yazma
✅ PixelFlowSetupWindow'a yeni sekme ekleme
✅ Yeni test yazma (Editor/Tests/)

YAPILAMAZ (AI bunları YAPMAMALI):
❌ Nexus Core package'ini değiştirme (GameContainer/Nexus/Packages/com.nexus.core/)
❌ SignalBus, CommandPool, Context, NexusDI class'larını modify etme
❌ Mediator<TView> base class'ını değiştirme
❌ IContextLifecycle interface'ini değiştirme
❌ Yeni lifecycle hook ekleme (OnConfigure/OnInitializeAsync/OnStartAsync sabit)
❌ Root MonoBehaviour'un boot sequence'ini değiştirme
❌ ViewBinder otomatik bağlama mantığını değiştirme

NEXUS CORE'DA DEĞİŞİKLİK GEREKİRSE:
→ İnsan geliştirici müdahalesi gerekir
→ AI sadece "şu değişiklik gerekli" diye ÖNERİ yazar, uygulamaz
```

---

## 17. EK-I: GERÇEK KOD REFERANSI (Real Working Code — No Pseudo-Code)

> **Bu bölümdeki tüm kodlar projedeki GERÇEK dosyalardan alınmıştır. `/* ... */` YOKTUR.
> Bir AI bu kodları BİREBİR kalıp olarak kullanır ve extend eder.**

### 17.1 GameContextLifecycle.cs — TAM İÇERİK (DI Binding Merkezi)

**Dosya:** `Assets/Scripts/PixelFlow/GameContextLifecycle.cs`

```csharp
using System.Threading;
using System.Threading.Tasks;
using Nexus.Core;
using Nexus.Core.Services;
using PixelFlow.Data;
using PixelFlow.Models;
using PixelFlow.Services;
using UnityEngine;

namespace PixelFlow
{
    public class GameContextLifecycle : MonoBehaviour, IContextLifecycle
    {
        public void OnConfigure(IContextBuilder builder)
        {
            // PlayerPrefs servisi — AES-256 şifreli
            builder.Bind<IPlayerPrefsService, EncryptedStorageService>();

            // Nexus Core Servisleri
            builder.BindService<IFeedbackService, FeedbackService>();
            builder.BindService<IObjectPoolService, ObjectPoolService>();
            builder.BindService<IWindowManager, WindowManager>();
            builder.BindService<IEconomyService, EconomyService>();
            builder.BindService<IProgressionService, ProgressionService>();
            builder.BindService<ITickService, TickService>();
            builder.BindService<IAdService, AdService>();
            builder.BindService<IIapService, IapService>();
            builder.BindService<IAnalyticsService, AnalyticsService>();

            // Nexus Altyapı Bağımlılıkları
            builder.Bind<Nexus.Core.Services.IAudioService, Nexus.Core.Services.AudioService>();
            builder.Bind<Nexus.Core.Services.IAudioRootProvider, Nexus.Core.Services.DefaultAudioRootProvider>();
            builder.Bind<Nexus.Core.Services.IUIAssetProvider, Nexus.Core.Services.ResourcesUIAssetProvider>();
            builder.Bind<Nexus.Core.Services.INetworkEconomyValidator, LocalEconomyValidator>();

            // PixelFlow Özel Servisleri
            builder.BindService<IPathService, PathService>();
            builder.BindService<IGameHistoryService, GameHistoryService>();
            builder.BindService<IVehicleSimulator, VehicleSimulator>();
            builder.BindService<ICameraProvider, CameraProvider>();
            builder.BindService<PixelFlow.Services.IAudioService, PixelFlow.Services.AudioService>();
            builder.BindService<IGameplayTimerService, GameplayTimerService>();
            builder.Bind<ITimeProvider, UnityTimeProvider>();
            builder.BindService<ISaveThrottler, SaveThrottler>();
            builder.BindService<IHapticService, HapticService>();
            builder.BindService<ILoggerService, LoggerService>();
            builder.BindService<ITutorialDriver, TutorialDriver>();
            builder.BindService<ICrisisAdService, CrisisAdService>();
            builder.BindService<IObstacleService, ObstacleService>();
            builder.BindService<ILocalizationService, LocalizationService>();
            builder.Bind<ILocalizationTableProvider, ResourceLocalizationTableProvider>();
            builder.BindService<IDailyCrisisService, DailyCrisisService>();
            builder.Bind<IPathSolver, RuntimePathSolver>();
            builder.BindService<IHintService, HintService>();
            builder.BindService<IPowerUpService, PowerUpService>();
            builder.Bind<ILevelProgressionService, LevelProgressionService>();
            builder.BindService<ILevelLoaderService, LevelLoaderService>();

            // Global Release Production Services
            builder.BindService<PixelFlow.Services.GlobalRelease.PrivacyComplianceService>();
            builder.BindService<PixelFlow.Services.GlobalRelease.SilentCrashDiagnosticsService>();
            builder.BindService<PixelFlow.Services.GlobalRelease.InAppReviewService>();
            builder.BindService<PixelFlow.Services.GlobalRelease.LocalNotificationService>();

            // Recovery strategy
            builder.BindInstance<IRecoveryStrategy>(new DefaultRecoveryStrategy(maxRetries: 3));

            // Reactive Models
            builder.BindReactiveModel<IGridModel, GridModel>();
            builder.BindReactiveModel<ILevelModel, LevelModel>();
            builder.BindReactiveModel<IProgressModel, ProgressModel>();
            builder.BindReactiveModel<IGameStateModel, GameStateModel>();
            builder.BindReactiveModel<IGameSessionModel, GameSessionModel>();
            builder.BindReactiveModel<IHintModel, HintModel>();
            builder.BindReactiveModel<ISettingsModel, SettingsModel>();
            builder.BindReactiveModel<ISoundModel, SoundModel>();
            builder.BindReactiveModel<ITutorialModel, TutorialModel>();
            builder.BindReactiveModel<IDailyCrisisModel, DailyCrisisModel>();
            builder.BindReactiveModel<IInventoryModel, InventoryModel>();

            // Signal → Command Bindings
            builder.BindSignal<PixelFlow.Signals.InputInteractionSignal>().To<PixelFlow.Commands.ProcessInputCommand>();
            builder.BindSignal<PixelFlow.Signals.CheckWinConditionSignal>().To<PixelFlow.Commands.CheckWinConditionCommand>();
            builder.BindSignal<PixelFlow.Signals.LoadLevelSignal>().To<PixelFlow.Commands.LoadLevelCommand>();
            builder.BindSignal<PixelFlow.Signals.RequestHintSignal>().To<PixelFlow.Commands.UseHintCommand>();
            builder.BindSignal<PixelFlow.Signals.ActivateRainbowRoadSignal>().To<PixelFlow.Commands.RainbowRoadCommand>();
            builder.BindSignal<PixelFlow.Signals.ClearJamSignal>().To<PixelFlow.Commands.ClearJamCommand>();
            builder.BindSignal<PixelFlow.Signals.ChangeThemeSignal>().To<PixelFlow.Commands.ChangeThemeCommand>();
            builder.BindCommand<PixelFlow.Signals.LevelCompletedSignal, PixelFlow.Commands.SaveProgressCommand>(ExecutionMode.Exclusive, priority: 0);
            builder.BindSignal<PixelFlow.Signals.UndoSignal>().To<PixelFlow.Commands.UndoCommand>();
            builder.BindSignal<PixelFlow.Signals.RedoSignal>().To<PixelFlow.Commands.RedoCommand>();
            builder.BindSignal<PixelFlow.Signals.PlaceViaductSignal>().To<PixelFlow.Commands.PlaceViaductCommand>();
            builder.BindSignal<PixelFlow.Signals.RequestInterstitialAdSignal>().To<PixelFlow.Commands.InterstitialAdCommand>();
            builder.BindSignal<PixelFlow.Signals.StartSimulationSignal>().To<PixelFlow.Commands.StartSimulationCommand>();
            builder.BindSignal<PixelFlow.Signals.PauseSimulationSignal>().To<PixelFlow.Commands.PauseSimulationCommand>();
            builder.BindSignal<PixelFlow.Signals.LoadedInitialLevelSignal>();
            builder.BindSignal<PixelFlow.Signals.FlowScoreUpdatedSignal>();
            builder.BindSignal<PixelFlow.Signals.ProgressUpdatedSignal>();

            // ScriptableObject yükleme (Zero Silent Fallback)
            var config = UnityEngine.Resources.Load<GameConfig>("Configs/GameConfig");
            if (config == null)
            {
#if !UNITY_EDITOR
                throw new DataValidationException("Resources/Configs/GameConfig.asset bulunamadı!");
#else
                config = UnityEngine.ScriptableObject.CreateInstance<GameConfig>();
                config.name = "GameConfig (Runtime Default)";
#endif
            }
            builder.BindInstance(config);

            var palette = UnityEngine.Resources.Load<ThemePaletteAsset>("Configs/ThemePalette");
            if (palette == null)
            {
#if !UNITY_EDITOR
                throw new DataValidationException("Resources/Configs/ThemePalette.asset bulunamadı!");
#else
                palette = UnityEngine.ScriptableObject.CreateInstance<ThemePaletteAsset>();
#endif
            }
            builder.BindInstance(palette);

            var colorBlindPalette = UnityEngine.Resources.Load<ColorBlindPaletteAsset>("Configs/ColorBlindPalette");
            if (colorBlindPalette == null)
            {
#if !UNITY_EDITOR
                throw new DataValidationException("Resources/Configs/ColorBlindPalette.asset bulunamadı!");
#else
                colorBlindPalette = UnityEngine.ScriptableObject.CreateInstance<ColorBlindPaletteAsset>();
#endif
            }
            builder.BindInstance(colorBlindPalette);
            Models.ColorBlindPalette.Initialize(colorBlindPalette);

            var vehicleMatConfig = UnityEngine.Resources.Load<VehicleMaterialConfigAsset>("Configs/VehicleMaterialConfig");
            if (vehicleMatConfig == null)
            {
#if !UNITY_EDITOR
                throw new DataValidationException("Resources/Configs/VehicleMaterialConfig.asset bulunamadı!");
#else
                vehicleMatConfig = UnityEngine.ScriptableObject.CreateInstance<VehicleMaterialConfigAsset>();
#endif
            }
            builder.BindInstance(vehicleMatConfig);
            Views.VehicleVisualFactory.Initialize(vehicleMatConfig);

            var economyConfig = UnityEngine.Resources.Load<EconomyConfigAsset>("Configs/EconomyConfig");
            if (economyConfig == null)
            {
#if !UNITY_EDITOR
                throw new DataValidationException("Resources/Configs/EconomyConfig.asset bulunamadı!");
#else
                economyConfig = UnityEngine.ScriptableObject.CreateInstance<EconomyConfigAsset>();
#endif
            }
            builder.BindInstance(economyConfig);

            var levelCatalog = UnityEngine.Resources.Load<LevelCatalogAsset>("Configs/LevelCatalog");
            if (levelCatalog == null)
            {
#if !UNITY_EDITOR
                throw new DataValidationException("Resources/Configs/LevelCatalog.asset bulunamadı!");
#else
                levelCatalog = UnityEngine.ScriptableObject.CreateInstance<LevelCatalogAsset>();
#endif
            }
            builder.BindInstance(levelCatalog);
        }

        public ValueTask OnInitializeAsync(CancellationToken ct) => default;
        public ValueTask OnStartAsync(CancellationToken ct) => default;
        public void OnDispose() { }
    }
}
```

**YENİ BİR SERVİS EKLERKEN:** Bu dosyanın `OnConfigure` metoduna tek satır ekle:
```csharp
builder.BindService<IMyNewService, MyNewService>();
```

**YENİ BİR MODEL EKLERKEN:**
```csharp
builder.BindReactiveModel<IMyNewModel, MyNewModel>();
```

**YENİ BİR SIGNAL→COMMAND EKLERKEN:**
```csharp
builder.BindSignal<MyNewSignal>().To<MyNewCommand>();
```

**YENİ BİR SCRIPTABLEOBJECT EKLERKEN:**
```csharp
var myAsset = UnityEngine.Resources.Load<MyConfigAsset>("Configs/MyConfig");
if (myAsset == null)
{
#if !UNITY_EDITOR
    throw new DataValidationException("Resources/Configs/MyConfig.asset bulunamadı!");
#else
    myAsset = UnityEngine.ScriptableObject.CreateInstance<MyConfigAsset>();
#endif
}
builder.BindInstance(myAsset);
```

---

### 17.2 PixelFlowSetupWindow.cs — SEKME SİSTEMİ (Gerçek Kod)

**Dosya:** `Assets/Scripts/PixelFlow/Editor/PixelFlowSetupWindow.cs`

```csharp
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Button = UnityEngine.UIElements.Button;

namespace PixelFlow.Editor
{
    public partial class PixelFlowSetupWindow : EditorWindow
    {
        [MenuItem("Pixel Flow/Kurulum Yardımcısı")]
        public static void ShowWindow()
        {
            var window = GetWindow<PixelFlowSetupWindow>("Pixel Flow Kontrol Merkezi");
            window.minSize = new Vector2(850, 720);
            window.RefreshData();
        }

        private int _selectedTab = 1;
        private VisualElement _contentContainer;
        private List<Button> _sidebarButtons = new List<Button>();

        private void CreateGUI()
        {
            RefreshData();
            var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(
                "Assets/Scripts/PixelFlow/Editor/PixelFlowSetupWindow.uss");
            if (styleSheet != null) rootVisualElement.styleSheets.Add(styleSheet);

            rootVisualElement.style.backgroundColor = new StyleColor(new Color(0.06f, 0.09f, 0.16f));
            rootVisualElement.style.flexDirection = FlexDirection.Column;
            rootVisualElement.style.paddingTop = 8;
            rootVisualElement.style.paddingBottom = 8;
            rootVisualElement.style.paddingLeft = 8;
            rootVisualElement.style.paddingRight = 8;

            rootVisualElement.Add(BuildHeader());

            var workspace = new VisualElement();
            workspace.style.flexDirection = FlexDirection.Row;
            workspace.style.flexGrow = 1;

            workspace.Add(BuildSidebar());

            _contentContainer = new ScrollView();
            _contentContainer.style.flexGrow = 1;
            _contentContainer.style.paddingLeft = 12;
            _contentContainer.style.paddingRight = 12;
            workspace.Add(_contentContainer);

            rootVisualElement.Add(workspace);
            SelectTab(_selectedTab);
        }

        private VisualElement BuildSidebar()
        {
            var sidebar = new VisualElement();
            sidebar.style.width = 220;
            sidebar.style.flexShrink = 0;
            sidebar.style.backgroundColor = new StyleColor(new Color(0.12f, 0.16f, 0.23f));
            SetStyleBorder(sidebar, new Color(0.2f, 0.25f, 0.33f), 1f);
            sidebar.style.borderTopLeftRadius = 12;
            sidebar.style.borderTopRightRadius = 12;
            sidebar.style.borderBottomLeftRadius = 12;
            sidebar.style.borderBottomRightRadius = 12;
            sidebar.style.paddingTop = 12;
            sidebar.style.paddingBottom = 12;
            sidebar.style.paddingLeft = 8;
            sidebar.style.paddingRight = 8;
            sidebar.style.marginRight = 10;

            _sidebarButtons.Clear();

            AddSidebarSection(sidebar, "OYUN & İÇERİK");
            AddSidebarNavButton(sidebar, 0, "🕹️ Oyun Kontrol");
            AddSidebarNavButton(sidebar, 1, "🎮 Seviye Stüdyosu");
            AddSidebarNavButton(sidebar, 2, "🎨 Garaj Stüdyosu");

            AddSidebarSection(sidebar, "DATA & EKONOMİ");
            AddSidebarNavButton(sidebar, 3, "📦 Data Yöneticisi");
            AddSidebarNavButton(sidebar, 4, "💰 Ekonomi & Isı Haritası");
            AddSidebarNavButton(sidebar, 5, "📺 Reklam Ayarları");

            AddSidebarSection(sidebar, "MÜHENDİSLİK & TEST");
            AddSidebarNavButton(sidebar, 6, "🧩 Toplu Çözücü");
            AddSidebarNavButton(sidebar, 7, "🔍 Sahne Tanılama");
            AddSidebarNavButton(sidebar, 8, "🔬 Nexus İzleyici");
            AddSidebarNavButton(sidebar, 9, "⚡ Performans");
            AddSidebarNavButton(sidebar, 10, "🛡️ Pre-Build Validator");

            return sidebar;
        }

        private void AddSidebarSection(VisualElement sidebar, string title)
        {
            var label = new Label(title);
            label.style.fontSize = 10;
            label.style.color = new StyleColor(new Color(0.39f, 0.45f, 0.55f));
            label.style.marginTop = 10;
            label.style.marginBottom = 4;
            label.style.paddingLeft = 6;
            sidebar.Add(label);
        }

        private void AddSidebarNavButton(VisualElement sidebar, int tabIdx, string text)
        {
            var btn = new Button(() => SelectTab(tabIdx)) { text = text };
            btn.style.backgroundColor = new StyleColor(Color.clear);
            btn.style.color = new StyleColor(new Color(0.58f, 0.64f, 0.72f));
            btn.style.fontSize = 11;
            btn.style.paddingTop = 8;
            btn.style.paddingBottom = 8;
            btn.style.paddingLeft = 10;
            btn.style.paddingRight = 10;
            btn.style.marginBottom = 2;
            SetStyleBorder(btn, Color.clear, 0f);
            btn.style.borderTopLeftRadius = 8;
            btn.style.borderTopRightRadius = 8;
            btn.style.borderBottomLeftRadius = 8;
            btn.style.borderBottomRightRadius = 8;
            _sidebarButtons.Add(btn);
            sidebar.Add(btn);
        }

        private void SelectTab(int tabIdx)
        {
            _selectedTab = tabIdx;
            for (int i = 0; i < _sidebarButtons.Count; i++)
            {
                bool isActive = (i == tabIdx);
                _sidebarButtons[i].style.backgroundColor = isActive
                    ? new StyleColor(new Color(0.23f, 0.51f, 0.96f))
                    : new StyleColor(Color.clear);
                _sidebarButtons[i].style.color = isActive
                    ? new StyleColor(Color.white)
                    : new StyleColor(new Color(0.58f, 0.64f, 0.72f));
            }
            RebuildContentPanel();
        }

        private void RebuildContentPanel()
        {
            if (_contentContainer == null) return;
            _contentContainer.Clear();

            switch (_selectedTab)
            {
                case 0: _contentContainer.Add(BuildGameControllerUIToolkitView()); break;
                case 1: _contentContainer.Add(BuildLevelStudioUIToolkitView()); break;
                case 2: _contentContainer.Add(BuildGarageUIToolkitView()); break;
                case 3: _contentContainer.Add(BuildDataManagerUIToolkitView()); break;
                case 4: _contentContainer.Add(BuildEconomyUIToolkitView()); break;
                case 5: _contentContainer.Add(BuildAdsUIToolkitView()); break;
                case 6: _contentContainer.Add(BuildBatchSolverUIToolkitView()); break;
                case 7: _contentContainer.Add(BuildDiagnosticsUIToolkitView()); break;
                case 8: _contentContainer.Add(BuildNexusUIToolkitView()); break;
                case 9: _contentContainer.Add(BuildPerformanceUIToolkitView()); break;
                case 10: _contentContainer.Add(BuildValidatorUIToolkitView()); break;
            }
        }

        private VisualElement BuildCard(string title)
        {
            var card = new VisualElement();
            card.style.backgroundColor = new StyleColor(new Color(0.12f, 0.16f, 0.23f));
            SetStyleBorder(card, new Color(0.2f, 0.25f, 0.33f), 1f);
            card.style.borderTopLeftRadius = 12;
            card.style.borderTopRightRadius = 12;
            card.style.borderBottomLeftRadius = 12;
            card.style.borderBottomRightRadius = 12;
            card.style.paddingTop = 14;
            card.style.paddingBottom = 14;
            card.style.paddingLeft = 16;
            card.style.paddingRight = 16;
            card.style.marginBottom = 12;

            if (!string.IsNullOrEmpty(title))
            {
                var titleLbl = new Label(title);
                titleLbl.style.fontSize = 13;
                titleLbl.style.color = new StyleColor(new Color(0.97f, 0.98f, 0.99f));
                titleLbl.style.marginBottom = 10;
                card.Add(titleLbl);
            }
            return card;
        }

        private static void SetStyleBorder(VisualElement elem, Color color, float width = 1f)
        {
            elem.style.borderTopColor = new StyleColor(color);
            elem.style.borderBottomColor = new StyleColor(color);
            elem.style.borderLeftColor = new StyleColor(color);
            elem.style.borderRightColor = new StyleColor(color);
            elem.style.borderTopWidth = width;
            elem.style.borderBottomWidth = width;
            elem.style.borderLeftWidth = width;
            elem.style.borderRightWidth = width;
        }

        private void RefreshData()
        {
            RefreshLevelsCache();
            RunDiagnostics();
        }

        private void RefreshLevelsCache()
        {
            _cachedLevels.Clear();
            var guids = AssetDatabase.FindAssets("t:LevelData");
            foreach (var guid in guids)
            {
                var lvl = AssetDatabase.LoadAssetAtPath<LevelData>(AssetDatabase.GUIDToAssetPath(guid));
                if (lvl != null) _cachedLevels.Add(lvl);
            }
            _cachedLevels = _cachedLevels.OrderBy(l => l.levelIndex).ToList();
        }
    }
}
#endif
```

**YENİ SEKME EKLEME REÇETESİ (AI bunu BİREBİR takip eder):**

```
1. BuildSidebar() metoduna yeni buton ekle:
   AddSidebarNavButton(sidebar, 11, "🆕 Yeni Sekme Adı");

2. RebuildContentPanel() switch'ine ekle:
   case 11: _contentContainer.Add(BuildMyNewTabView()); break;

3. Yeni partial class dosyası oluştur:
   Assets/Scripts/PixelFlow/Editor/PixelFlowSetupWindow.MyNewTab.cs

4. Dosya içeriği:
   #if UNITY_EDITOR
   namespace PixelFlow.Editor
   {
       partial class PixelFlowSetupWindow
       {
           private VisualElement BuildMyNewTabView()
           {
               var root = new VisualElement();
               var card = BuildCard("🆕 Yeni Sekme Başlığı");
               // ... UI elemanları ekle
               root.Add(card);
               return root;
           }
       }
   }
   #endif
```

---

### 17.3 GridMediator.cs — TAM İÇERİK (Reference Mediator)

**Dosya:** `Assets/Scripts/PixelFlow/Views/GridMediator.cs`

```csharp
using Nexus.Core;
using Nexus.Core.Services;
using PixelFlow.Data;
using PixelFlow.Models;
using PixelFlow.Signals;
using System.Collections.Generic;
using UnityEngine;

namespace PixelFlow.Views
{
    public class GridMediator : Mediator<GridView>
    {
        [Inject] public IGridModel GridModel { get; set; }
        [Inject] public ISettingsModel SettingsModel { get; set; }
        [Inject] public ILoggerService Logger { get; set; }
        [Inject] public PixelFlow.Services.IAudioService AudioService { get; set; }

        private CellState[,] _previousCellStates;
        private ColorType[,] _previousCellColors;
        private byte[,] _previousPathColorMasks;
        private readonly HashSet<Vector2Int> _changedCells = new HashSet<Vector2Int>();
        private readonly HashSet<Vector2Int> _stateChangedCells = new HashSet<Vector2Int>();
        private readonly HashSet<ColorType> _completedColors = new HashSet<ColorType>();

        protected override void OnBind()
        {
            Subscribe<GridUpdatedSignal>(HandleGridUpdated);
            Subscribe<ThemeChangedSignal>(HandleThemeChanged);
            Subscribe<ThirdColorRejectionSignal>(HandleThirdColorRejection);

            View.OnGlobalPointerDown += HandleGlobalPointerDown;
            View.OnGlobalPointerDrag += HandleGlobalPointerDrag;
            View.OnGlobalPointerUp += HandleGlobalPointerUp;

            ComputeAndCacheCells();

            if (GridModel.Width > 0 && GridModel.Height > 0)
            {
                InitializeAndCenter();
            }
        }

        protected override void OnUnbind()
        {
            View.OnGlobalPointerDown -= HandleGlobalPointerDown;
            View.OnGlobalPointerDrag -= HandleGlobalPointerDrag;
            View.OnGlobalPointerUp -= HandleGlobalPointerUp;
        }

        private void HandleGlobalPointerDown(Vector2Int pos)
        {
            if (UnityEngine.EventSystems.EventSystem.current != null
                && UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject()) return;
            SignalBus.Fire(new InputInteractionSignal { Type = InputType.PointerDown, GridPosition = pos });
        }

        private void HandleGlobalPointerDrag(Vector2Int pos)
        {
            if (UnityEngine.EventSystems.EventSystem.current != null
                && UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject()) return;
            SignalBus.Fire(new InputInteractionSignal { Type = InputType.Drag, GridPosition = pos });
        }

        private void HandleGlobalPointerUp(Vector2Int pos)
        {
            if (UnityEngine.EventSystems.EventSystem.current != null
                && UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject()) return;
            SignalBus.Fire(new InputInteractionSignal { Type = InputType.PointerUp, GridPosition = pos });
        }

        private void HandleThemeChanged(ThemeChangedSignal signal)
        {
            if (GridModel.Width > 0 && GridModel.Height > 0)
            {
                View.UpdateGridVisuals(GridModel.Grid, GridModel.Width, GridModel.Height,
                    SettingsModel.CurrentTheme, GridModel.Paths, GridModel.LastCrashPosition.Value);
            }
        }

        private void HandleGridUpdated(GridUpdatedSignal signal)
        {
            if (!View.IsInitialized && GridModel.Width > 0 && GridModel.Height > 0)
            {
                InitializeAndCenter();
                return;
            }
            if (GridModel.Width <= 0 || GridModel.Height <= 0) return;

            Vector2Int crashPos = GridModel.LastCrashPosition.Value;
            ComputeAndCacheCells();
            if (_changedCells.Count > 0)
            {
                View.UpdateDifferential(GridModel.Grid, SettingsModel.CurrentTheme,
                    _changedCells, crashPos, _stateChangedCells);
            }
            View.UpdatePathVisuals(GridModel.Paths, GridModel.Grid, crashPos,
                GridModel.CrashColorA.Value, GridModel.CrashColorB.Value);

            // Path connection feedback
            var currentCompletedColors = new HashSet<ColorType>();
            foreach (var kvp in GridModel.Paths)
            {
                var color = kvp.Key;
                var path = kvp.Value;
                if (path == null || path.Count < 2) continue;
                var startCell = GridModel.Grid[path[0].x, path[0].y];
                var endCell = GridModel.Grid[path[path.Count - 1].x, path[path.Count - 1].y];
                if (startCell.State == CellState.Node && startCell.Color == color
                    && endCell.State == CellState.Node && endCell.Color == color)
                {
                    currentCompletedColors.Add(color);
                }
            }

            foreach (var color in currentCompletedColors)
            {
                if (!_completedColors.Contains(color))
                {
                    AudioService?.PlaySfx(PixelFlow.Services.SfxType.CoinCollect);
                    var path = GridModel.Paths[color];
                    View.TriggerJuicyBounce(path[0], 1.25f, 0.4f);
                    View.TriggerJuicyBounce(path[path.Count - 1], 1.25f, 0.4f);
                }
            }

            _completedColors.Clear();
            foreach (var color in currentCompletedColors) _completedColors.Add(color);
            GridModel.LastCrashPosition.Value = new Vector2Int(-1, -1);
        }

        private void ComputeAndCacheCells()
        {
            _changedCells.Clear();
            _stateChangedCells.Clear();
            int w = GridModel.Width;
            int h = GridModel.Height;
            if (w <= 0 || h <= 0) return;

            bool needsResize = _previousCellStates == null
                || _previousCellStates.GetLength(0) != w
                || _previousCellStates.GetLength(1) != h;

            if (needsResize)
            {
                _previousCellStates = new CellState[w, h];
                _previousCellColors = new ColorType[w, h];
                _previousPathColorMasks = new byte[w, h];
            }

            for (int x = 0; x < w; x++)
            {
                for (int y = 0; y < h; y++)
                {
                    var current = GridModel.Grid[x, y];
                    if (needsResize)
                    {
                        _changedCells.Add(new Vector2Int(x, y));
                        _stateChangedCells.Add(new Vector2Int(x, y));
                    }
                    else
                    {
                        bool stateChanged = _previousCellStates[x, y] != current.State;
                        bool colorChanged = _previousCellColors[x, y] != current.Color;
                        bool pathColorsChanged = _previousPathColorMasks[x, y] != current.PathColorsMask;
                        if (stateChanged || colorChanged || pathColorsChanged)
                        {
                            _changedCells.Add(new Vector2Int(x, y));
                            if (stateChanged) _stateChangedCells.Add(new Vector2Int(x, y));
                        }
                    }
                    _previousCellStates[x, y] = current.State;
                    _previousCellColors[x, y] = current.Color;
                    _previousPathColorMasks[x, y] = current.PathColorsMask;
                }
            }
        }

        private void InitializeAndCenter()
        {
            ComputeAndCacheCells();
            View.InitializeGrid(GridModel.Width, GridModel.Height);
            View.UpdateGridVisuals(GridModel.Grid, GridModel.Width, GridModel.Height,
                SettingsModel.CurrentTheme, GridModel.Paths, GridModel.LastCrashPosition.Value);
            View.CenterCamera(GridModel.Width, GridModel.Height);

            var cam = View.GetCachedCamera();
            float cx = (GridModel.Width - 1) * 0.5f;
            float cy = (GridModel.Height - 1) * 0.5f;
            if (cam != null)
            {
                var camCtrl = cam.GetComponent<PixelFlow.Services.CameraController>();
                if (camCtrl != null)
                {
                    float size = cam.orthographicSize;
                    camCtrl.SetPuzzleView(cx, cy, size);
                    camCtrl.TransitionToPuzzle();
                }
            }
        }

        private void HandleThirdColorRejection(ThirdColorRejectionSignal signal)
        {
            if (!View.IsInitialized) return;
            View.TriggerThirdColorRejectionPulse(signal.Position);
        }
    }
}
```

---

### 17.4 SignalBus.FireInternal — GERÇEK DISPATCH LOGIC

**Dosya:** `GameContainer/Nexus/Packages/com.nexus.core/Runtime/Core/SignalBus.cs` (satır 534-666)

```csharp
private void FireInternal<T>(T signal, bool isCrossContextSource) where T : struct
{
    var type = typeof(T);

    NexusRuntime.Metrics.RecordSignalDispatched();
    NexusRuntime.Metrics.RecordTrace(SignalTraceLabel<T>.Fire);

    // Async handler varsa sync Fire() YASAK — exception fırlatır
    bool hasAsync = _hasAsyncHandlerReadCopy.TryGetValue(type, out var asyncFlag) && asyncFlag;
    bool hasAsyncSubscriptions = HasAsyncSubscriptions(type);

    if (hasAsync || hasAsyncSubscriptions)
    {
        throw new NexusSyncAsyncMismatchException(
            $"Synchronous Fire() was called for signal '{typeof(T).FullName}', " +
            "but it has asynchronous handlers. Use FireAsync() instead.");
    }

    // REENTRANCY CHECK (max 10 derinlik)
    s_stackDepth.Value++;
    if (s_stackDepth.Value > MaxStackDepth)
    {
        s_stackDepth.Value--;
        throw new NexusReentrancyException(
            $"Stack overflow detected. Reentrancy limit of {MaxStackDepth} exceeded for signal {typeof(T).FullName}");
    }

    try
    {
        // Plugin interceptor'ları çalıştır (iptal edebilir)
        bool interceptorCancelled = false;
        if (_context is Context ctx && ctx.HasInterceptors)
        {
            object boxedSignal = signal;
            var plugins = ctx.PluginsReadOnlyCopy;
            for (int i = 0; i < plugins.Count; i++)
            {
                var interceptors = plugins[i].context.Interceptors;
                for (int j = 0; j < interceptors.Count; j++)
                {
                    if (!interceptors[j].Intercept(ref boxedSignal))
                    {
                        interceptorCancelled = true;
                        break;
                    }
                }
                if (interceptorCancelled) break;
            }
            signal = (T)boxedSignal;
        }

        if (interceptorCancelled) return;

        // Cross-Context broadcast (varsa)
        if (!isCrossContextSource)
        {
            var crossContextAttr = type.GetCustomAttribute<CrossContextAttribute>();
            if (crossContextAttr != null)
                BroadcastCrossContext(signal, crossContextAttr.ScopeTag);
        }

        // ═══ EXECUTION ORDER GUARANTEE ═══
        // Phase 1: COMMANDS (state mutate eder)
        if (_commandHandlersReadCopy.TryGetValue(type, out var handlers))
        {
            foreach (var handler in handlers)
            {
                ExecuteCommand(handler, signal);
            }
        }

        // Phase 2: SUBSCRIPTIONS (final state'i gözlemler)
        if (_subscriptionsReadCopy.TryGetValue(type, out var node))
        {
            var current = node;
            while (current != null)
            {
                if (current.IsActive && current.Handler is Action<T> syncSub)
                {
                    syncSub(signal);
                }
                current = current.Next;
            }
        }

        // Phase 3: Composite triggers
        ProcessCompositeTriggers(type);
    }
    catch (Exception ex)
    {
        ErrorCollection.CollectException(ex, ErrorCollection.ErrorCategory.Signal, typeof(T).Name);
        NexusRuntime.Logger?.LogError($"[Nexus] Signal '{typeof(T).Name}' dispatch failed: {ex.Message}");
    }
    finally
    {
        s_stackDepth.Value--;
        if (_pendingCleanups) SweepDeadNodes();
    }
}
```

**KRİTİK BİLGİ:** Commands HER ZAMAN subscriptions'dan ÖNCE çalışır.
Yani: `SignalBus.Fire(signal)` → Command'lar state değiştirir → Mediator'lar yeni state'i okur.

---

### 17.5 LevelDataEditor.cs — CUSTOMEDITOR PATTERN (Gerçek Kod)

**Dosya:** `Assets/Scripts/PixelFlow/Editor/LevelDataEditor.cs` (ilk 230 satır)

```csharp
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using PixelFlow.Data;
using PixelFlow.Services;
using System.Collections.Generic;
using System.Linq;

namespace PixelFlow.Editor
{
    [CustomEditor(typeof(LevelData))]
    public class LevelDataEditor : UnityEditor.Editor
    {
        private enum EditMode { None, Node, Path, Bridge, Obstacle, OneWay, Eraser }
        private EditMode _currentMode = EditMode.Node;
        private ColorType _currentColor = ColorType.Red;
        private ObstacleType _currentObstacleType = ObstacleType.Construction;
        private Vector2Int _currentOneWayDirection = Vector2Int.right;
        private ShapeType _currentShape = ShapeType.Circle;
        private NodeType _currentNodeType = NodeType.Home;
        private bool _currentIsSource = true;
        private int _currentPairIndex = 0;
        private LevelData _data;
        private Vector2Int _lastPaintedCell = new Vector2Int(-1, -1);
        private bool _requireFullGridCoverage = true;
        private bool _mirrorHorizontal = false;
        private bool _mirrorVertical = false;
        private float _cellSizeSlider = 38f;

        private void OnEnable()
        {
            _data = (LevelData)target;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            InitStyles();

            // Auto-align shapes with colors
            bool updatedAny = false;
            if (_data?.initialNodes != null)
            {
                for (int i = 0; i < _data.initialNodes.Count; i++)
                {
                    var node = _data.initialNodes[i];
                    var expectedShape = GetDefaultShapeForColor(node.color);
                    if (node.shape != expectedShape)
                    {
                        node.shape = expectedShape;
                        _data.initialNodes[i] = node;
                        updatedAny = true;
                    }
                }
            }
            if (updatedAny) EditorUtility.SetDirty(_data);

            int complexityScore = CalculateComplexityScore(_data);
            string tierName = GetDifficultyTierName(complexityScore);

            // Header
            GUILayout.BeginVertical(_cardStyle);
            GUILayout.Label("Pixel Flow Level Editor (Master Studio)", _headerStyle);
            GUILayout.Label($"Difficulty: {tierName} ({complexityScore} pts)", _tierBadgeStyle);
            GUILayout.EndVertical();

            // Grid Configuration
            GUILayout.BeginVertical(_cardStyle);
            GUILayout.Label("Grid Configuration", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            int newLevelIndex = EditorGUILayout.IntField("Level Index", _data.levelIndex);
            int newWidth = EditorGUILayout.IntSlider("Width", _data.width, 3, 10);
            int newHeight = EditorGUILayout.IntSlider("Height", _data.height, 3, 10);
            int newViaductLimit = EditorGUILayout.IntSlider("Viaduct Limit", _data.viaductLimit, 0, 10);
            int newFlowThreshold = EditorGUILayout.IntSlider("Flow Score Target", _data.flowScoreThreshold, 1, 50);
            bool newCoverage = EditorGUILayout.Toggle("Require Full Grid Coverage", _data.requireFullGridCoverage);

            // 3D Toy Theme & Bouncy Physics
            ToyThemeType newToyTheme = (ToyThemeType)EditorGUILayout.EnumPopup("3D Toy Theme", _data.toyTheme);
            float newBounceForce = EditorGUILayout.Slider("Bounce Force", _data.bouncyPhysics.BounceForce, 1f, 10f);
            float newBounceDamping = EditorGUILayout.Slider("Bounce Damping", _data.bouncyPhysics.BounceDamping, 0.1f, 1.0f);
            float newSquishFactor = EditorGUILayout.Slider("Squish Factor", _data.bouncyPhysics.SquishFactor, 0.05f, 0.8f);

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(_data, "Change Level Settings");
                _data.levelIndex = newLevelIndex;
                _data.width = newWidth;
                _data.height = newHeight;
                _data.viaductLimit = newViaductLimit;
                _data.flowScoreThreshold = newFlowThreshold;
                _data.requireFullGridCoverage = newCoverage;
                _data.toyTheme = newToyTheme;
                _data.bouncyPhysics = new BouncyPhysicsConfig
                {
                    BounceForce = newBounceForce,
                    BounceDamping = newBounceDamping,
                    SquishFactor = newSquishFactor
                };
                SanitizeGridBounds();
                EditorUtility.SetDirty(_data);
            }
            GUILayout.EndVertical();

            // Edit Mode Toolbar
            GUILayout.BeginVertical(_cardStyle);
            GUILayout.Label("Editor Controls", EditorStyles.boldLabel);
            GUILayout.BeginHorizontal();
            EditMode[] modes = { EditMode.Node, EditMode.Path, EditMode.Bridge, EditMode.Obstacle, EditMode.OneWay, EditMode.Eraser };
            foreach (var m in modes)
            {
                bool isSelected = _currentMode == m;
                GUI.backgroundColor = isSelected ? new Color(0.2f, 0.6f, 1f) : Color.white;
                if (GUILayout.Button(m.ToString(), GUILayout.Height(28)))
                {
                    _currentMode = m;
                    _lastPaintedCell = new Vector2Int(-1, -1);
                }
            }
            GUI.backgroundColor = Color.white;
            GUILayout.EndHorizontal();

            // Symmetry tools
            _mirrorHorizontal = GUILayout.Toggle(_mirrorHorizontal, "Mirror Horizontal (X)");
            _mirrorVertical = GUILayout.Toggle(_mirrorVertical, "Mirror Vertical (Y)");
            _cellSizeSlider = EditorGUILayout.Slider("Cell Zoom Size", _cellSizeSlider, 24f, 60f);

            // Visual Grid (tıklanabilir hücreler)
            DrawVisualGrid();

            // Solver Test
            if (GUILayout.Button("🧪 Test Solvability", GUILayout.Height(30)))
            {
                RunSolverTest();
            }

            GUILayout.EndVertical();
            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif
```

---

### 17.6 Garage Sekmesi — HYBRID-CASUAL TAB PATTERN (Gerçek Kod)

**Dosya:** `Assets/Scripts/PixelFlow/Editor/PixelFlowSetupWindow.HybridCasualTabs.cs`

```csharp
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
        private Vector2 _garageScrollPos;

        private void DrawGarageSkinStudioTab()
        {
            _garageScrollPos = EditorGUILayout.BeginScrollView(_garageScrollPos);
            GUILayout.BeginVertical(_cardStyle);
            GUILayout.Label("🎨 Garaj & Skin Stüdyosu", _sectionHeaderStyle);
            GUILayout.Label("Kod yazmadan yeni 3D araç skin'leri ekleyin.", _miniInfoStyle);
            GUILayout.Space(8);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("✨ Yeni VehicleSkinConfig Asset Oluştur", GUILayout.MinHeight(30)))
            {
                CreateNewVehicleSkinAsset();
            }
            if (GUILayout.Button("🍦 Standart 3D Skin Paketini Oluştur", GUILayout.MinHeight(30)))
            {
                CreateStandardSkinSuite();
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(10);
            GUILayout.Label("📦 Projedeki Araç Skin'leri", EditorStyles.boldLabel);

            var guids = AssetDatabase.FindAssets("t:VehicleSkinConfig");
            if (guids.Length == 0)
            {
                EditorGUILayout.HelpBox("Henüz VehicleSkinConfig bulunamadı.", MessageType.Info);
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
                    Texture2D previewTex = skin.Prefab3D != null
                        ? AssetPreview.GetAssetPreview(skin.Prefab3D) : null;
                    if (previewTex != null)
                        GUILayout.Label(previewTex, GUILayout.Width(64), GUILayout.Height(64));
                    else
                        GUILayout.Box("3D Model\nYok", GUILayout.Width(64), GUILayout.Height(64));

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
                    GUILayout.EndHorizontal();
                    GUILayout.EndVertical();
                }
            }
            GUILayout.EndVertical();
            EditorGUILayout.EndScrollView();
        }
    }
}
#endif
```

---

## 18. EK-J: FULL SOURCE CONTEXT — GridView, ProcessInputCommand & Test Framework (Level 5)

> **Bu bölüm, belgedeki SON `/* ... */` boşluklarını kapatır.
> Artık AI, View'dan Command'a, Command'dan Test'e kadar TÜM zinciri gerçek kodla görebilir.**

### 18.1 GridView.cs — TAM İÇERİK (View Katmanı, `/* ... */` YOK)

**Dosya:** `Assets/Scripts/PixelFlow/Views/GridView.cs`

```csharp
using UnityEngine;
using System.Collections.Generic;
using Nexus.Core;
using Nexus.Core.Services;
using PixelFlow.Core;
using PixelFlow.Data;
using PixelFlow.Models;
using PixelFlow.Services;

namespace PixelFlow.Views
{
    [Mediator(typeof(GridMediator))]
    public class GridView : TickableView
    {
        public event System.Action<Vector2Int> OnGlobalPointerDown;
        public event System.Action<Vector2Int> OnGlobalPointerDrag;
        public event System.Action<Vector2Int> OnGlobalPointerUp;

        [SerializeField] private CellView _cellPrefab;
        [SerializeField] private Transform _gridContainer;

        [Inject] public ICameraProvider CameraProvider { get; set; }
        [Inject] public ILoggerService LoggerService { get; set; }
        [Inject, OptionalInject] public PixelFlow.Data.GameConfig Config { get; set; }

        private Camera _cam;
        private CellView[,] _cells;
        private List<CellView> _instantiatedCells = new List<CellView>();
        private Queue<CellView> _cellPool = new Queue<CellView>();
        public bool IsInitialized => _cells != null;

        private IGridInputService _inputService;
        private Vector2Int _lastDragPos = new Vector2Int(-1, -1);
        private float _targetZoom;
        private float ConfigMinZoom => Config != null ? Config.MinZoom : 8f;
        private float ConfigMaxZoom => Config != null ? Config.MaxZoom : 12f;
        private static Shader _glowPulseShader;

        private void Awake()
        {
            UnityEngine.InputSystem.EnhancedTouch.EnhancedTouchSupport.Enable();
            _inputService = new GridInputService();
        }

        protected override void OnTick(float deltaTime)
        {
            if (_cells == null) return;

            // Tick cell animations
            int cellW = _cells.GetLength(0);
            int cellH = _cells.GetLength(1);
            for (int x = 0; x < cellW; x++)
                for (int y = 0; y < cellH; y++)
                    _cells[x, y]?.TickAnimation(deltaTime);

            HandlePinchZoom();

            if (_cam == null) _cam = CameraProvider?.MainCamera;
            if (_cam == null) return;

            // UI üzerindeyse grid input yoksay
            var es = UnityEngine.EventSystems.EventSystem.current;
            if (es != null && es.IsPointerOverGameObject()) return;

            var result = _inputService?.ProcessInput(_cam, _cells.GetLength(0), _cells.GetLength(1));
            if (result == null || !result.Value.HasEvent) return;

            var r = result.Value;
            if (r.IsDown)
            {
                _lastDragPos = r.GridPosition;
                OnGlobalPointerDown?.Invoke(r.GridPosition);
            }
            else if (r.IsDrag)
            {
                // Interpolate through intermediate cells for smooth line drawing
                var currentPos = r.GridPosition;
                var tempPos = _lastDragPos;
                if (tempPos.x >= 0)
                {
                    while (tempPos != currentPos)
                    {
                        int dx = currentPos.x - tempPos.x;
                        int dy = currentPos.y - tempPos.y;
                        if (Mathf.Abs(dx) >= Mathf.Abs(dy))
                            tempPos.x += System.Math.Sign(dx);
                        else
                            tempPos.y += System.Math.Sign(dy);
                        OnGlobalPointerDrag?.Invoke(tempPos);
                    }
                }
                _lastDragPos = currentPos;
            }
            else if (r.IsUp)
            {
                _lastDragPos = new Vector2Int(-1, -1);
                OnGlobalPointerUp?.Invoke(r.GridPosition);
            }
        }

        private void EnsurePool(int requiredSize)
        {
            if (_cellPrefab == null) return;
            int currentCount = _instantiatedCells.Count;
            if (currentCount < requiredSize)
            {
                int toCreate = requiredSize - currentCount;
                for (int i = 0; i < toCreate; i++)
                {
                    if (_gridContainer == null) _gridContainer = transform;
                    var cell = Instantiate(_cellPrefab, _gridContainer);
                    cell.gameObject.SetActive(false);
                    _cellPool.Enqueue(cell);
                    _instantiatedCells.Add(cell);
                }
            }
        }

        public void InitializeGrid(int width, int height)
        {
            // Mevcut hücreleri pool'a geri koy
            if (_cells != null)
            {
                for (int x = 0; x < _cells.GetLength(0); x++)
                    for (int y = 0; y < _cells.GetLength(1); y++)
                        if (_cells[x, y] != null)
                        {
                            _cells[x, y].gameObject.SetActive(false);
                            _cellPool.Enqueue(_cells[x, y]);
                        }
            }

            // Path line'ları temizle
            foreach (var kvp in _pathLines)
            {
                if (kvp.Value != null)
                {
                    if (kvp.Value.material != null) Destroy(kvp.Value.material);
                    Destroy(kvp.Value.gameObject);
                }
            }
            _pathLines.Clear();

            EnsurePool(width * height);

            _cells = new CellView[width, height];
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    var cell = _cellPool.Dequeue();
                    cell.gameObject.SetActive(true);
                    cell.transform.localPosition = new Vector3(x, y, 0);
                    cell.Setup(new Vector2Int(x, y));
                    _cells[x, y] = cell;
                }
            }
        }

        private Dictionary<ColorType, LineRenderer> _pathLines = new Dictionary<ColorType, LineRenderer>();
        private Dictionary<ColorType, LineRenderer> _glowLines = new Dictionary<ColorType, LineRenderer>();
        private HashSet<ColorType> _previousPathColors = new HashSet<ColorType>();
        private static Shader _cachedSpriteShader;

        public void UpdateGridVisuals(CellData[,] gridData, int width, int height,
            AppTheme theme, Dictionary<ColorType, List<Vector2Int>> paths, Vector2Int crashPos = default)
        {
            if (_cells == null) return;
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                    _cells[x, y].UpdateVisuals(gridData[x, y], theme, crashPos);
            UpdatePathVisuals(paths, gridData);
        }

        public void UpdateDifferential(CellData[,] gridData, AppTheme theme,
            HashSet<Vector2Int> changedCells, Vector2Int crashPos = default,
            HashSet<Vector2Int> stateChangedCells = null)
        {
            if (_cells == null || changedCells == null) return;
            foreach (var pos in changedCells)
            {
                if (pos.x >= 0 && pos.x < _cells.GetLength(0) &&
                    pos.y >= 0 && pos.y < _cells.GetLength(1))
                {
                    _cells[pos.x, pos.y].UpdateVisuals(gridData[pos.x, pos.y], theme, crashPos);
                    bool shouldBounce = stateChangedCells != null && stateChangedCells.Contains(pos);
                    if (shouldBounce)
                        _cells[pos.x, pos.y].TriggerBounceAnimation(0.95f, 0.12f);
                }
            }
        }

        public void TriggerJuicyBounce(Vector2Int position, float scale = 1.25f, float duration = 0.4f)
        {
            if (_cells == null) return;
            if (position.x >= 0 && position.x < _cells.GetLength(0) &&
                position.y >= 0 && position.y < _cells.GetLength(1))
                _cells[position.x, position.y].TriggerBounceAnimation(scale, duration);
        }

        public void TriggerThirdColorRejectionPulse(Vector2Int position)
        {
            if (_cells == null) return;
            if (position.x >= 0 && position.x < _cells.GetLength(0) &&
                position.y >= 0 && position.y < _cells.GetLength(1))
                _cells[position.x, position.y].TriggerThirdColorRejectionPulse();
        }

        public void UpdatePathVisuals(Dictionary<ColorType, List<Vector2Int>> paths,
            CellData[,] gridData = null, Vector2Int crashPos = default,
            ColorType crashColorA = ColorType.None, ColorType crashColorB = ColorType.None)
        {
            if (paths == null) return;

            // Eski path'leri gizle
            foreach (var prevColor in _previousPathColors)
            {
                if (!paths.ContainsKey(prevColor))
                {
                    if (_pathLines.TryGetValue(prevColor, out var oldLr))
                        oldLr.gameObject.SetActive(false);
                    if (_glowLines.TryGetValue(prevColor, out var oldGlow))
                        oldGlow.gameObject.SetActive(false);
                }
            }

            foreach (var kvp in paths)
            {
                ColorType colorType = kvp.Key;
                List<Vector2Int> pathPositions = kvp.Value;

                if (pathPositions == null || pathPositions.Count < 2)
                {
                    if (_pathLines.TryGetValue(colorType, out var inactiveLr))
                        inactiveLr.gameObject.SetActive(false);
                    if (_glowLines.TryGetValue(colorType, out var inactiveGlow))
                        inactiveGlow.gameObject.SetActive(false);
                    continue;
                }

                // LineRenderer oluştur (yoksa)
                if (!_pathLines.TryGetValue(colorType, out var lineRenderer))
                {
                    GameObject lineObj = new GameObject($"PathLine_{colorType}");
                    lineObj.transform.SetParent(_gridContainer);
                    lineRenderer = lineObj.AddComponent<LineRenderer>();
                    lineRenderer.startWidth = 0.2f;
                    lineRenderer.endWidth = 0.2f;
                    lineRenderer.numCornerVertices = 8;
                    lineRenderer.numCapVertices = 8;
                    lineRenderer.useWorldSpace = false;
                    lineRenderer.sortingOrder = 5;
                    Shader spriteShader = _cachedSpriteShader ?? (_cachedSpriteShader = Shader.Find("Sprites/Default"));
                    lineRenderer.material = new Material(spriteShader);
                    _pathLines[colorType] = lineRenderer;
                }

                // Glow LineRenderer oluştur (yoksa)
                if (!_glowLines.TryGetValue(colorType, out var glowRenderer))
                {
                    GameObject glowObj = new GameObject($"PathLineGlow_{colorType}");
                    glowObj.transform.SetParent(_gridContainer);
                    glowRenderer = glowObj.AddComponent<LineRenderer>();
                    glowRenderer.startWidth = 0.55f;
                    glowRenderer.endWidth = 0.55f;
                    glowRenderer.numCornerVertices = 8;
                    glowRenderer.numCapVertices = 8;
                    glowRenderer.useWorldSpace = false;
                    glowRenderer.sortingOrder = 2;
                    if (_glowPulseShader == null)
                        _glowPulseShader = Shader.Find("Hidden/PixelFlow/GlowPulse") ?? _cachedSpriteShader;
                    glowRenderer.material = new Material(_glowPulseShader);
                    _glowLines[colorType] = glowRenderer;
                }

                lineRenderer.gameObject.SetActive(true);
                lineRenderer.positionCount = pathPositions.Count;
                glowRenderer.gameObject.SetActive(true);
                glowRenderer.positionCount = pathPositions.Count;

                Color pipeColor = CellView.GetColor(colorType);
                Color glowColor = new Color(pipeColor.r, pipeColor.g, pipeColor.b, 0.55f);
                lineRenderer.startColor = pipeColor;
                lineRenderer.endColor = pipeColor;
                glowRenderer.startColor = glowColor;
                glowRenderer.endColor = glowColor;

                // Pozisyonları ayarla (viaduct Z-offset ile)
                int gw = gridData != null ? gridData.GetLength(0) : 0;
                int gh = gridData != null ? gridData.GetLength(1) : 0;
                for (int i = 0; i < pathPositions.Count; i++)
                {
                    Vector2Int gridPos = pathPositions[i];
                    float z = -0.12f;
                    float zGlow = -0.11f;
                    if (gridPos.x >= 0 && gridPos.x < gw && gridPos.y >= 0 && gridPos.y < gh)
                    {
                        var cell = gridData[gridPos.x, gridPos.y];
                        if (cell.HasViaduct && cell.OverColor == colorType)
                        {
                            z = -0.32f;
                            zGlow = -0.31f;
                        }
                    }
                    lineRenderer.SetPosition(i, new Vector3(gridPos.x, gridPos.y, z));
                    glowRenderer.SetPosition(i, new Vector3(gridPos.x, gridPos.y, zGlow));
                }
            }

            _previousPathColors.Clear();
            foreach (var kvp in paths) _previousPathColors.Add(kvp.Key);
        }

        public void CenterCamera(int width, int height)
        {
            if (_cam == null) _cam = CameraProvider?.MainCamera;
            if (_cam == null) return;
            float cx = (width - 1) * 0.5f;
            float cy = (height - 1) * 0.5f;
            _cam.transform.position = new Vector3(cx, cy, -10f);
            float requiredSize = Mathf.Max(width, height) * 0.7f;
            _cam.orthographicSize = Mathf.Clamp(requiredSize, ConfigMinZoom, ConfigMaxZoom);
        }

        public Camera GetCachedCamera()
        {
            if (_cam == null) _cam = CameraProvider?.MainCamera;
            return _cam;
        }

        private void HandlePinchZoom()
        {
            if (_cam == null) return;
            if (UnityEngine.Input.touchCount == 2)
            {
                var t0 = UnityEngine.Input.GetTouch(0);
                var t1 = UnityEngine.Input.GetTouch(1);
                float prevDist = Vector2.Distance(t0.position - t0.deltaPosition, t1.position - t1.deltaPosition);
                float curDist = Vector2.Distance(t0.position, t1.position);
                float delta = prevDist - curDist;
                _cam.orthographicSize = Mathf.Clamp(_cam.orthographicSize + delta * 0.01f, ConfigMinZoom, ConfigMaxZoom);
            }
        }
    }
}
```

---

### 18.2 ProcessInputCommand.cs — TAM İÇERİK (Command Katmanı, `/* ... */` YOK)

**Dosya:** `Assets/Scripts/PixelFlow/Commands/ProcessInputCommand.cs`

```csharp
using System;
using Nexus.Core;
using Nexus.Core.Services;
using PixelFlow.Models;
using PixelFlow.Signals;
using PixelFlow.Data;
using PixelFlow.Services;
using UnityEngine;
using System.Collections.Generic;

namespace PixelFlow.Commands
{
    public class ProcessInputCommand : ICommand<InputInteractionSignal>, IResettable
    {
        [Inject] public IGridModel GridModel { get; set; }
        [Inject] public ISignalBus SignalBus { get; set; }
        [Inject] public ISoundModel SoundModel { get; set; }
        [Inject] public IPathService PathService { get; set; }
        [Inject] public IGameStateModel GameStateModel { get; set; }
        [Inject] public IGameHistoryService HistoryService { get; set; }
        [Inject] public IGameSessionModel GameSessionModel { get; set; }
        [Inject] public ILevelModel LevelModel { get; set; }
        [Inject] public ISaveThrottler SaveThrottler { get; set; }
        [Inject] public IHapticService HapticService { get; set; }
        [Inject] public IObstacleService ObstacleService { get; set; }
        [Inject] public IPlayerPrefsService PlayerPrefsService { get; set; }
        [Inject] public ILoggerService LoggerService { get; set; }

        private void EnsureHistoryRecorded()
        {
            HistoryService.Record(GridModel, GameSessionModel);
        }

        private Action _cachedSaveAction;
        private void RequestSave()
        {
            if (_cachedSaveAction == null)
                _cachedSaveAction = () => GridStateSerializer.Save(GridModel, GameSessionModel, LevelModel, PlayerPrefsService);
            SaveThrottler?.TryRequestSave(_cachedSaveAction);
        }

        public void Execute(InputInteractionSignal signal)
        {
            var state = GameStateModel.CurrentState;

            // STATE GUARD: Simülasyon modunda grid tıklamaları state'i değiştirmez
            if (state == GameState.Simulating) return;
            if (state != GameState.Playing && state != GameState.Paused) return;

            // BOUNDS CHECK
            if (signal.GridPosition.x < 0 || signal.GridPosition.y < 0 ||
                signal.GridPosition.x >= GridModel.Width || signal.GridPosition.y >= GridModel.Height)
                return;

            var currentCell = GridModel.Grid[signal.GridPosition.x, signal.GridPosition.y];

            // ═══ PAUSED STATE — Kriz çözümü ═══
            if (state == GameState.Paused)
            {
                if (signal.Type == InputType.PointerDown)
                {
                    if (currentCell.PathColorCount >= 2 && !currentCell.HasViaduct)
                    {
                        SignalBus.Fire(new PlaceViaductSignal { Position = signal.GridPosition });
                        HapticService?.Vibrate(HapticType.Medium);
                        return;
                    }
                    else
                    {
                        GameSessionModel?.MarkCrisisUndoUsed();
                        GameStateModel.SetState(GameState.Playing);
                        state = GameState.Playing;
                    }
                }
                else return;
            }

            // ═══ POINTER DOWN — Renk seçimi / path başlatma ═══
            if (signal.Type == InputType.PointerDown)
            {
                ColorType clickedColor = currentCell.Color != ColorType.None ? currentCell.Color
                    : currentCell.PathColorCount > 0 ? currentCell.FirstPathColor
                    : ColorType.None;

                if (clickedColor != ColorType.None)
                {
                    if (GridModel.LockedColors.Contains(clickedColor)) return;

                    EnsureHistoryRecorded();
                    GridModel.ActiveColor.Value = clickedColor;
                    GridModel.LastPosition.Value = signal.GridPosition;

                    if (!GridModel.Paths.ContainsKey(clickedColor))
                        GridModel.Paths[clickedColor] = new List<Vector2Int>();

                    if (currentCell.State == CellState.Node)
                    {
                        // Kaynak node'dan yeni path başlat
                        PathService.ClearPath(clickedColor);
                        GridModel.Paths[clickedColor].Add(signal.GridPosition);
                    }
                    else if (currentCell.State == CellState.Path || currentCell.State == CellState.Bridge)
                    {
                        // Mevcut path'e dokunuldu → backtrack
                        PathService.BacktrackPath(clickedColor, signal.GridPosition);
                    }
                    HapticService?.Vibrate(HapticType.Light);
                }
            }
            // ═══ DRAG — Yol uzatma ═══
            else if (signal.Type == InputType.Drag)
            {
                if (GridModel.ActiveColor.Value == ColorType.None) return;

                // Bounds check
                if (signal.GridPosition.x < 0 || signal.GridPosition.y < 0 ||
                    signal.GridPosition.x >= GridModel.Width || signal.GridPosition.y >= GridModel.Height)
                    return;

                // Sadece komşu hücreye drag izin ver (Manhattan distance = 1)
                int distance = Mathf.Abs(signal.GridPosition.x - GridModel.LastPosition.Value.x)
                             + Mathf.Abs(signal.GridPosition.y - GridModel.LastPosition.Value.y);
                if (distance != 1) return;

                if (!GridModel.Paths.TryGetValue(GridModel.ActiveColor.Value, out var path))
                {
                    GridModel.ActiveColor.Value = ColorType.None;
                    return;
                }

                // Backtrack: zaten path'te olan hücreye geri dön
                if (path.Contains(signal.GridPosition))
                {
                    EnsureHistoryRecorded();
                    PathService.BacktrackPath(GridModel.ActiveColor.Value, signal.GridPosition);
                    GridModel.LastPosition.Value = signal.GridPosition;
                    SignalBus.Fire(new GridUpdatedSignal());
                    return;
                }

                // Bridge çıkış yön kontrolü
                var lastCell = GridModel.Grid[GridModel.LastPosition.Value.x, GridModel.LastPosition.Value.y];
                if ((lastCell.HasViaduct || lastCell.State == CellState.Bridge) && path.Count >= 2)
                {
                    Vector2Int bridgePos = GridModel.LastPosition.Value;
                    Vector2Int entryDir = bridgePos - path[path.Count - 2];
                    Vector2Int moveDir = signal.GridPosition - bridgePos;
                    if (moveDir != entryDir) return; // Düz devam zorunlu
                }

                // Obstacle kontrolü
                if (currentCell.State == CellState.Obstacle) return;

                // OneWay kontrolü
                Vector2Int drawMoveDir = signal.GridPosition - GridModel.LastPosition.Value;
                if (ObstacleService != null)
                {
                    if (ObstacleService.IsOneWay(signal.GridPosition, drawMoveDir) ||
                        ObstacleService.IsOneWay(GridModel.LastPosition.Value, drawMoveDir))
                        return;
                }

                // BOŞ HÜCRE → Path uzat
                bool isDrawableObstacle = currentCell.State == CellState.Obstacle &&
                    (currentCell.ObstacleType == ObstacleType.Ferry || currentCell.ObstacleType == ObstacleType.NarrowPass);

                if (currentCell.State == CellState.Empty || (isDrawableObstacle && currentCell.PathColorCount == 0))
                {
                    EnsureHistoryRecorded();
                    currentCell.Color = GridModel.ActiveColor.Value;
                    currentCell.State = CellState.Path;
                    if (!currentCell.HasPathColor(GridModel.ActiveColor.Value))
                        currentCell.AddPathColor(GridModel.ActiveColor.Value);
                    path.Add(signal.GridPosition);
                    GridModel.LastPosition.Value = signal.GridPosition;
                    SignalBus.Fire(new GridUpdatedSignal());
                    SoundModel.PlayDrawSound(path.Count);
                }
                // HEDEF NODE → Bağlantı tamamla
                else if (currentCell.State == CellState.Node && currentCell.Color == GridModel.ActiveColor.Value)
                {
                    if (path.Count > 0 && path[path.Count - 1] == signal.GridPosition) return;
                    EnsureHistoryRecorded();
                    if (!currentCell.HasPathColor(GridModel.ActiveColor.Value))
                        currentCell.AddPathColor(GridModel.ActiveColor.Value);
                    path.Add(signal.GridPosition);
                    GridModel.LastPosition.Value = signal.GridPosition;
                    GridModel.ActiveColor.Value = ColorType.None;
                    SignalBus.Fire(new GridUpdatedSignal());
                    SignalBus.Fire(new CheckWinConditionSignal());
                    HapticService?.Vibrate(HapticType.Medium);
                    RequestSave();
                }
                // DOLU HÜCRE (başka renk path) → Bridge crossing
                else if (currentCell.State == CellState.Path || currentCell.State == CellState.Bridge)
                {
                    if (currentCell.HasPathColor(GridModel.ActiveColor.Value)) return;

                    // Çakışan path'i backtrack et
                    if (currentCell.PathColorCount > 0)
                    {
                        ColorType existingColor = currentCell.FirstPathColor;
                        if (GridModel.Paths.TryGetValue(existingColor, out var otherPath))
                        {
                            Vector2Int entryDir = signal.GridPosition - GridModel.LastPosition.Value;
                            if (!BridgeValidationUtility.IsValidBridgeCrossing(otherPath, path, signal.GridPosition, entryDir))
                            {
                                EnsureHistoryRecorded();
                                PathService.BacktrackPath(existingColor, signal.GridPosition);
                            }
                        }
                    }

                    // Max 2 path limiti
                    if (currentCell.PathColorCount >= BridgeValidationUtility.MaxPathsPerBridge)
                    {
                        ColorType firstColor = currentCell.FirstPathColor;
                        EnsureHistoryRecorded();
                        PathService.BacktrackPath(firstColor, signal.GridPosition);
                    }

                    if (currentCell.PathColorCount >= BridgeValidationUtility.MaxPathsPerBridge) return;

                    EnsureHistoryRecorded();
                    if (currentCell.PathColorCount == 0)
                        currentCell.UnderColor = GridModel.ActiveColor.Value;
                    else
                        currentCell.OverColor = GridModel.ActiveColor.Value;

                    currentCell.AddPathColor(GridModel.ActiveColor.Value);
                    path.Add(signal.GridPosition);
                    GridModel.LastPosition.Value = signal.GridPosition;

                    // Viyadüksüz kesişim → uyarı
                    if (!currentCell.HasViaduct && currentCell.PathColorCount >= 2)
                        SignalBus.Fire(new PathIntersectionWarningSignal { Position = signal.GridPosition });

                    SignalBus.Fire(new GridUpdatedSignal());
                }
            }
            // ═══ POINTER UP — Path tamamlama / state reset ═══
            else if (signal.Type == InputType.PointerUp)
            {
                if (GridModel.ActiveColor.Value != ColorType.None)
                {
                    EnsureHistoryRecorded();
                    GridModel.ActiveColor.Value = ColorType.None;
                    GridModel.LastPosition.Value = new Vector2Int(-1, -1);
                    RequestSave();
                }
            }
        }

        public void Reset()
        {
            // Command pool reuse — mutable state yok
        }
    }
}
```

---

### 18.3 Test Framework — GERÇEK NUNIT TEST (GameTestContext + ProcessInputCommandTests)

**Dosya:** `Assets/Scripts/PixelFlow/Editor/Tests/GameTestContext.cs`

```csharp
using System.Collections.Generic;
using Nexus.Core;
using Nexus.Core.Services;
using PixelFlow.Commands;
using PixelFlow.Data;
using PixelFlow.Models;
using PixelFlow.Services;
using PixelFlow.Signals;
using UnityEngine;

namespace PixelFlow.Editor.Tests
{
    public static class GameTestContext
    {
        public static NexusTestContext CreateGameContext()
        {
            return NexusTestHarness.CreateContext(builder =>
            {
                builder.Bind<IPlayerPrefsService, InMemoryPlayerPrefsService>();
                builder.BindService<IPathService, PathService>();
                builder.BindService<IGameHistoryService, GameHistoryService>();
                builder.Bind<IPathSolver, RuntimePathSolver>();
                builder.Bind<IHintService, HintService>();
                builder.BindService<IVehicleSimulator, VehicleSimulator>();
                builder.BindService<ISaveThrottler, SaveThrottler>();
                builder.BindService<IHapticService, HapticService>();
                builder.Bind<ILoggerService, LoggerService>();
                builder.BindService<ICrisisAdService, CrisisAdService>();
                builder.BindService<IObstacleService, ObstacleService>();
                builder.BindService<ITutorialDriver, TutorialDriver>();
                builder.BindService<IPowerUpService, PowerUpService>();
                builder.BindService<PixelFlow.Services.IAudioService, PixelFlow.Services.AudioService>();
                builder.Bind<IFeedbackService, FeedbackService>();
                builder.Bind<Nexus.Core.Services.IAudioService, StubAudioService>();
                builder.Bind<ITimeProvider, UnityTimeProvider>();
                builder.Bind<ITickService, TickService>();
                builder.BindReactiveModel<IDailyCrisisModel, DailyCrisisModel>();
                builder.BindReactiveModel<IGridModel, GridModel>();
                builder.BindReactiveModel<ILevelModel, LevelModel>();
                builder.BindReactiveModel<IProgressModel, ProgressModel>();
                builder.BindReactiveModel<IGameStateModel, GameStateModel>();
                builder.BindReactiveModel<IGameSessionModel, GameSessionModel>();
                builder.BindReactiveModel<IHintModel, HintModel>();
                builder.BindReactiveModel<ISettingsModel, SettingsModel>();
                builder.BindReactiveModel<ISoundModel, SoundModel>();
                builder.BindReactiveModel<ITutorialModel, TutorialModel>();
                builder.BindReactiveModel<IInventoryModel, InventoryModel>();
                builder.Bind<ILevelProgressionService, LevelProgressionService>();
                builder.BindInstance<IRecoveryStrategy>(new DefaultRecoveryStrategy(maxRetries: 3));
                builder.Bind<ICameraProvider, StubCameraProvider>();
                builder.BindService<ILevelLoaderService, LevelLoaderService>();

                // Signal → Command bindings (production ile aynı)
                builder.BindSignal<InputInteractionSignal>().To<ProcessInputCommand>();
                builder.BindSignal<CheckWinConditionSignal>().To<CheckWinConditionCommand>();
                builder.BindSignal<LoadLevelSignal>().To<LoadLevelCommand>();
                builder.BindSignal<RequestHintSignal>().To<UseHintCommand>();
                builder.BindSignal<ChangeThemeSignal>().To<ChangeThemeCommand>();
                builder.BindSignal<LevelCompletedSignal>().To<SaveProgressCommand>();
                builder.BindSignal<StartSimulationSignal>().To<StartSimulationCommand>();
                builder.BindSignal<PauseSimulationSignal>().To<PauseSimulationCommand>();
                builder.BindSignal<UndoSignal>().To<UndoCommand>();
                builder.BindSignal<RedoSignal>().To<RedoCommand>();
            });
        }

        public static LevelData CreateTestLevel(int index = 0)
        {
            var level = ScriptableObject.CreateInstance<LevelData>();
            level.levelIndex = index;
            level.width = 5;
            level.height = 5;
            level.initialNodes = new List<GridNode>
            {
                new GridNode { position = new Vector2Int(0, 0), color = ColorType.Red },
                new GridNode { position = new Vector2Int(4, 0), color = ColorType.Red },
                new GridNode { position = new Vector2Int(0, 4), color = ColorType.Blue },
                new GridNode { position = new Vector2Int(4, 4), color = ColorType.Blue },
                new GridNode { position = new Vector2Int(2, 0), color = ColorType.Green },
                new GridNode { position = new Vector2Int(2, 4), color = ColorType.Green },
            };
            level.bridgePositions = new List<Vector2Int> { new Vector2Int(2, 2) };
            level.viaductLimit = 3;
            level.flowScoreThreshold = 5;
            return level;
        }
    }
}
```

**Dosya:** `Assets/Scripts/PixelFlow/Editor/Tests/ProcessInputCommandTests.cs`

```csharp
using NUnit.Framework;
using Nexus.Core;
using PixelFlow.Data;
using PixelFlow.Models;
using PixelFlow.Services;
using PixelFlow.Signals;
using System.Collections.Generic;
using UnityEngine;
using static PixelFlow.Editor.Tests.GameTestContext;

namespace PixelFlow.Editor.Tests
{
    [TestFixture]
    public class ProcessInputCommandTests
    {
        private NexusTestContext _ctx;
        private IGridModel _grid;
        private IGameStateModel _state;

        [SetUp]
        public void SetUp()
        {
            _ctx = CreateGameContext();
            _grid = _ctx.GetModel<IGridModel>();
            _state = _ctx.GetModel<IGameStateModel>();
        }

        [TearDown]
        public void TearDown()
        {
            _ctx?.Dispose();
        }

        private void LoadLevel()
        {
            var level = CreateTestLevel();
            _ctx.Dispatch(new LoadLevelSignal { LevelToLoad = level });
        }

        [Test]
        public void PointerDownOnNode_ActivatesColor()
        {
            LoadLevel();
            _ctx.Dispatch(new InputInteractionSignal { Type = InputType.PointerDown, GridPosition = new Vector2Int(0, 0) });
            Assert.AreEqual(ColorType.Red, _grid.ActiveColor.Value);
            Assert.AreEqual(new Vector2Int(0, 0), _grid.LastPosition.Value);
        }

        [Test]
        public void PointerDownOnEmptyCell_DoesNothing()
        {
            LoadLevel();
            _grid.Grid[3, 3].State = CellState.Empty;
            _ctx.Dispatch(new InputInteractionSignal { Type = InputType.PointerDown, GridPosition = new Vector2Int(3, 3) });
            Assert.AreEqual(ColorType.None, _grid.ActiveColor.Value);
        }

        [Test]
        public void DragToAdjacentCell_ExtendsPath()
        {
            LoadLevel();
            _ctx.Dispatch(new InputInteractionSignal { Type = InputType.PointerDown, GridPosition = new Vector2Int(0, 0) });
            _ctx.Dispatch(new InputInteractionSignal { Type = InputType.Drag, GridPosition = new Vector2Int(1, 0) });
            Assert.IsTrue(_grid.Paths.ContainsKey(ColorType.Red));
            Assert.AreEqual(2, _grid.Paths[ColorType.Red].Count);
            Assert.AreEqual(CellState.Path, _grid.Grid[1, 0].State);
            Assert.AreEqual(ColorType.Red, _grid.Grid[1, 0].Color);
        }

        [Test]
        public void DragToNonAdjacentCell_Ignored()
        {
            LoadLevel();
            _ctx.Dispatch(new InputInteractionSignal { Type = InputType.PointerDown, GridPosition = new Vector2Int(0, 0) });
            _ctx.Dispatch(new InputInteractionSignal { Type = InputType.Drag, GridPosition = new Vector2Int(2, 0) });
            Assert.AreEqual(1, _grid.Paths[ColorType.Red].Count);
        }

        [Test]
        public void PointerUp_ResetsState()
        {
            LoadLevel();
            _ctx.Dispatch(new InputInteractionSignal { Type = InputType.PointerDown, GridPosition = new Vector2Int(0, 0) });
            Assert.AreEqual(ColorType.Red, _grid.ActiveColor.Value);
            _ctx.Dispatch(new InputInteractionSignal { Type = InputType.PointerUp, GridPosition = new Vector2Int(0, 0) });
            Assert.AreEqual(ColorType.None, _grid.ActiveColor.Value);
            Assert.AreEqual(new Vector2Int(-1, -1), _grid.LastPosition.Value);
        }

        [Test]
        public void DragToSecondNode_CompletesConnection()
        {
            var level = ScriptableObject.CreateInstance<LevelData>();
            level.levelIndex = 0;
            level.width = 3;
            level.height = 1;
            level.initialNodes = new List<GridNode>
            {
                new GridNode { position = new Vector2Int(0, 0), color = ColorType.Red },
                new GridNode { position = new Vector2Int(2, 0), color = ColorType.Red },
            };
            _ctx.Dispatch(new LoadLevelSignal { LevelToLoad = level });

            _ctx.Dispatch(new InputInteractionSignal { Type = InputType.PointerDown, GridPosition = new Vector2Int(0, 0) });
            _ctx.Dispatch(new InputInteractionSignal { Type = InputType.Drag, GridPosition = new Vector2Int(1, 0) });
            _ctx.Dispatch(new InputInteractionSignal { Type = InputType.Drag, GridPosition = new Vector2Int(2, 0) });
            Assert.AreEqual(ColorType.None, _grid.ActiveColor.Value,
                "ActiveColor should clear after connecting to target node");
        }

        [Test]
        public void InputInNonPlayingState_Ignored()
        {
            LoadLevel();
            _state.SetState(GameState.LevelCompleted);
            _ctx.Dispatch(new InputInteractionSignal { Type = InputType.PointerDown, GridPosition = new Vector2Int(0, 0) });
            Assert.AreEqual(ColorType.None, _grid.ActiveColor.Value,
                "Input should be blocked when state is not Playing");
        }

        [Test]
        public void DragToOccupiedCell_MaxTwoPathsLimit()
        {
            LoadLevel();
            _grid.Grid[2, 0].State = CellState.Path;
            _grid.Grid[2, 0].AddPathColor(ColorType.Blue);
            _grid.Grid[2, 0].AddPathColor(ColorType.Green);

            _ctx.Dispatch(new InputInteractionSignal { Type = InputType.PointerDown, GridPosition = new Vector2Int(0, 0) });
            _ctx.Dispatch(new InputInteractionSignal { Type = InputType.Drag, GridPosition = new Vector2Int(1, 0) });
            _ctx.Dispatch(new InputInteractionSignal { Type = InputType.Drag, GridPosition = new Vector2Int(2, 0) });

            Assert.AreEqual(2, _grid.Paths[ColorType.Red].Count,
                "Red path should not extend into cell occupied by 2 other colors");
        }
    }
}
```

**YENİ TEST YAZMA REÇETESİ (AI bunu BİREBİR takip eder):**

```
1. Dosya: Assets/Scripts/PixelFlow/Editor/Tests/MyNewFeatureTests.cs
2. Namespace: PixelFlow.Editor.Tests
3. Pattern:
   - [TestFixture] class
   - [SetUp]: _ctx = CreateGameContext(); _model = _ctx.GetModel<IMyModel>();
   - [TearDown]: _ctx?.Dispose();
   - [Test]: _ctx.Dispatch(new MySignal { ... }); Assert.AreEqual(...);
4. Mock: InMemoryPlayerPrefsService, StubAudioService, StubCameraProvider
5. SAHNE YÜKLEME — tüm testler EditMode'da çalışır
```

---

**Belge Sonu — Color Jam 3D v6.5.0 (Level 5: Full Source Context Edition)**
*GridView tam implementasyon, ProcessInputCommand tam logic (path çizim/bridge crossing/OneWay/obstacle), GameTestContext + NUnit test framework gerçek kodu ile tamamlanmış AI-First Development blueprint. Belgede artık `/* ... */` YOKTUR.*