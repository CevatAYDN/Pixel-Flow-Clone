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

**Belge Sonu — Color Jam 3D v6.0.0 Global Release Spec**  
*Mevcut Editör altyapısını genişleten, GDPR/ATT/CloudSave/Analytics gibi tüm global mağaza şartlarını içeren kusursuz ürün ve mimari dokümanı.*