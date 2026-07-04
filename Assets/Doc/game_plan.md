Gönderdiğin `com.nexus.core` paketinin içeriğini inceledim ve tek kelimeyle harika bir temel. Bu paket, tam anlamıyla kurumsal seviyede bir MVCS (Model-View-Command-Signal) mimarisi inşa etmen için ihtiyacın olan tüm modern araçları (Dependency Injection, Event Driven yapı, Reactive Modeller) barındırıyor.

Soruna net bir cevap vereyim: **Kesinlikle ilk adım Proje Mimarisi ve Klasör Yapısını kurmak olmalıdır.**

Elinde `NexusDI`, `ContextBuilder`, `SignalBus` ve `CommandPool` gibi inanılmaz güçlü mimari bileşenler var. Grid prototiplemesine klasör yapısını ve Dependency Injection (DI) bağlamlarını (Context) kurmadan başlarsak, bu güçlü core paketini kullanmanın hiçbir esprisi kalmaz ve kodlar spagettiye dönüşür.

Hazırladığımız **Neon Transit** GDD'sini, bu `com.nexus.core` mimarisine oturtmak için yapmamız gereken eşleştirme ve kurmamız gereken klasör yapısı şöyledir:

### ⚙️ Nexus Core ve Neon Transit Mimari Eşleşmesi

1. **Signals (Olaylar):** Araç kaza yaptığında veya hücreye tıklandığında birbirine sıkı sıkıya bağlı kodlar yazmayacağız. Bunun yerine `NetworkSignalBus` veya `SignalBus` kullanarak `CrashDetectedSignal` gibi olaylar fırlatacağız.


2. **Commands (Komutlar):** GDD'deki o kritik "Geri Al (Undo)" veya "Viyadük Yerleştir" mekaniklerini, doğrudan paketteki `CommandPool` üzerinden çalıştıracağımız Command sınıflarıyla yöneteceğiz.


3. **Models (Veri Katmanı):** Ana ekrandaki şehir vergisi veya grid'in durumu gibi verileri, paketteki `IReactiveModel` arayüzünü uygulayan ve `ObservableProperty` kullanan modellerde tutacağız. Böylece veri değiştiğinde UI otomatik güncellenecek.


4. **Views & Mediators (Görsel Katman):** Arayüzü ve oyun alanını yönetmek için paketteki `Mediator` sınıfından türetilen sınıflar yazılacak ve bunlar `ViewBinder` ile sahnedeki View'lara (MonoBehaviour) bağlanacaktır. Mevcut `Nexus` dizininin yanına, oyuna özel kodlarımızı barındıran modüler yapımız kurulmuştur. Kod yapısı `Assets/Scripts/PixelFlow/` altında şu şekilde organize edilmiştir:

```text
Assets/
├── Plugins/
├── Nexus/                          <-- Nexus Core Paketi (Framework)
└── Scripts/
                ├── HubHUDView.cs
                └── CellView.cs

```

Bu yapı, GDD'deki vizyonu, senin Core paketindeki bileşenlerle sorunsuz bir şekilde konuşturacaktır. AI asistanlarına görev verirken bu ağacı referans göstermen, onların tam olarak doğru katmana kod yazmasını sağlar.


**GAME** **DESIGN** **DOCUMENT** (**GDD**) — Neon Transit
Proje: Neon Transit
Sürüm: 2.0.0 (Tam Sürüm)
Tarih: Temmuz **2026**
Platform: Mobil (iOS & Android)
Tür: Hibrit-Casual (Bulmaca + Idle Şehir Kurma)
Motor: Unity **2023** **LTS** + **URP** (Universal Render Pipeline)
## Hedef FPS: 60 FPS (giriş seviyesi cihazlarda 30 FPS fallback)
## Oyun Özeti (Executive Pitch)
Neon Transit, modern mobil oyuncuların en çok sevdiği iki dinamiği bir araya getiren bir hibrit-casual yapımıdır: Tatmin edici, minimalist bir bulmaca mekaniği ve uzun vadeli bir büyüme hissi sunan şehir inşası.
Oyuncu, kaotik bir metropolün altyapı mimarı rolünü üstlenir. Amaç, minimalist bir grid üzerinde farklı renklerdeki evleri ve iş yerlerini en optimum şekilde birbirine bağlayarak trafiği su gibi akıtmaktır. Bulmaca ekranında çözülen her trafik düğümü, ana ekrandaki izometrik neon şehrin büyümesini, canlanmasını ve pasif gelir (vergi) üretmesini sağlar. Oyun, oyuncuya günlük hayatın stresinden uzaklaşabileceği, dokunsal (tactile) ve **ASMR** kalitesinde pürüzsüz bir deneyim satmayı hedefler.
Tek Cümlelik Pitch: *Trafik düğümlerini çöz, neon şehri büyüt.*
Benzer Yapımlar ve Farklılaştırıcı Unsurlar (**USP**):
| Rakip | Benzerlik | Neon Transit'in Farkı |
| --- | --- | --- |
| Mini Metro | Minimalist hat çizme | Neon Transit'te kaza mekaniği + idle şehir kurma meta katmanı var |
| Mini Motorways | Araç akışı + grid | Neon Transit'te viyadük taktik katmanı ve idle ekonomi var |
| Traffic Run / Traffic Escape | Trafik temalı bulmaca | Neon Transit soyut trafik değil, şehir büyütme döngüsüyle kalıcılık sağlar |
| Two Dots | Renk bağlama bulmacası | Neon Transit'te fiziksel akış simülasyonu + görsel tatmin çok daha yüksek |
| ## Townscaper | Şehir kurma tatmini | Neon Transit'te şehir kurma bulmaca başarısına bağlı, amaç-driven |
2. Çekirdek Mekanik ve Oynanış (Core Gameplay Logic)
Oyunun çekirdek mekaniği, iki nokta arasında hat çizme (Line Routing) mantığına dayanır ancak bunu dinamik ve fiziksel bir simülasyona dönüştürür.
2.1 Düğümler (Nodes)
Izgara (Grid) üzerinde başlangıç ve bitiş noktaları bulunur. Bu noktalar soyut şekiller değil, somut şehir elemanlarıdır:
| Renk | Başlangıç (Kaynak) | Bitiş (Hedef) | Açıklama |
| --- | --- | --- | --- |
| 🔵 Mavi | Konut Blokları | Ofis Kuleleri | Sabah işe gidiş trafiği |
| 🔴 Kırmızı | Konut Blokları | Hastaneler | Acil durum trafiği |
| 🟡 Sarı | Konut Blokları | Okullar | Eğitim trafiği |
| 🟢 Yeşil | Konut Blokları | Parklar | Hafta sonu rekreasyon trafiği |
| 🟣 Mor | Ofis Kuleleri | **AVM**'ler | Alışveriş/Öğle trafiği |
Renk Körlüğü Önlemi (Çift Kodlama): Her renk çifti, renk bağımsız bir şekil ikonu ile de eşleştirilir:
Mavi = ● Daire
Kırmızı = ▲ Üçgen
Sarı = ■ Kare
Yeşil = ◆ Elmas
Mor = ★ Yıldız
Bu sayede protanopi, deuteranopi ve tripanopi kullanıcıları renk yerine şekil üzerinden de bağlantı kurabilir.
2.2 Grid Yapısı
Bulmaca Ekranı: 2D top-down (kuş bakışı) grid. Düz çizim ve okunabilirlik için.
Hub Ekranı (Şehir): 3D izometrik görünüm. Low-poly, neon ışıklandırma.
Geçiş: Seamless Zoom-in — Kamera 2D'den 3D'ye sinematik bir Lerp animasyonu ile geçer. Yükleme ekranı yoktur.
2.3 Çizim ve Akış (Path Drawing)
Oyuncu parmağıyla aynı renkteki kaynak ve hedef düğümü birleştirir. Çizim mantığı şu şekilde işler:
Dokunma: Oyuncu bir kaynak düğüme (ev) parmağını koyar.
Sürükleme: Parmağını sürükledikçe hat grid hücreleri üzerinden pürüzsüz bir neon yol olarak oluşur. Hat, grid hücrelerinin kenarlarından geçer (yalnızca orthogonal movement — yukarı/aşağı/sağ/sol). Çapraz hareket desteklenmez.
Bağlantı: Hat, aynı renk hedef düğüme ulaştığında bağlantı tamamlanır.
Anında Simülasyon: Bağlantı kurulduğu anda minik vektörel araçlar hat üzerinde akmaya başlar. Araçlar *Hayalet Modu*nda — yarı şeffaf, parlama efektli — çalışır.
Onay: Oyuncu tüm bağlantıları kurup *Başlat* butonuna bastığında (veya otomatik olarak tüm düğümler eşleştiğinde), hayalet modu kalkar ve araçlar gerçek simülasyona geçer.
2.4 Kaza Mekaniği (The Crash) — **DETAYLI** **TEKN**İK **AKI**Ş
Klasik akış oyunlarının aksine, farklı renkteki iki yol birbirini dik kestiğinde sistem çizimi engellemez veya eski yolu silmez. Kaza mantığı şu aşamalarda işler:

**ÖNEMLİ**: Kaza tespiti **hem çizim aşamasında (Playing) hem de simülasyon aşamasında (Simulating)** çalışır. Çizim sırasında araçlar hayalet modunda akar — eğer iki farklı renkli araç viyadüksüz bir kesişimde karşılaşırsa KAZA hemen tetiklenir. Oyuncunun tüm yolları tamamlamasını beklemeye gerek yoktur — anlık geri bildirim sayesinde sorunu hemen görür, düzeltir (undo/viyadük) ve çizmeye devam eder. Simülasyon fazında Flow Score threshold'una ulaşma kontrolü çalışır (bkz. §2.8); çizim sırasında tamamlama kontrolü yapılmaz.

Aşama 1 — Çizim Sırasında Görsel Uyarı (Soft Warning):
Oyuncu mevcut bir yolu kesen bir hat çizerse, kesişim noktasında küçük sarı bir uyarı ikonu (⚠) yanar. Aynı anda `PathIntersectionWarningSignal` ateşlenir.
Bu sadece görsel bir ipucudur, oyunu engellemez.
Amaç: Stratejik oyuncuya erken uyarı vererek *burada sorun olabilir* hissi yaratmak.
Aşama 2 — Simülasyon Başlatma (Hayalet Modu):
Tüm bağlantılar kurulduğunda araçlar hayalet modunda akmaya başlar.
Eğer iki farklı renkli yol kesişim noktasında aynı anda araç bulunuyorsa → Kaza tetiklenir.
Kaza anında:
Kesişim noktası kırmızı yanar (pulse efekti).
Tatlı bir duman efekti (puff particle).
Cihazda tok haptik titreşim (Haptic Feedback — .impact(.medium)).
Kısa korna/fren **SFX**'i.
Kaza yapan iki aracın bulunduğu hat segmentleri kırmızıya döner.
Aşama 3 — Kriz Çözüm Ekranı:
Ekranın üstünde bir *Trafik Krizi!* banner'ı belirir.
Oyuncuya iki seçenek sunulur:
🔄 Geri Al (Undo): Kaza yapan yollardan birini geri al ve yeniden çiz. (Ücretsiz, sınırsız — ama viyadük kullanma hakkını kaybeder.)
🌉 Viyadük Yerleştir: Kesişim noktasına köprü kur. (Detaylar §2.5'te.)
*Game Over* yoktur. Oyuncu istediği kadar deneme yapabilir. Ancak 3 başarısız denemeden sonra bir geçiş reklamı gösterilir (İlk 5 seviyede asla gösterilmez).
2.5 Viyadük / Üst Geçit Mekaniği — **DETAYLI** **TEKN**İK **AKI**Ş
Tanım: Viyadük, iki yolun kesiştiği noktada bir yolun diğerinin üstünden geçmesini sağlayan yapıdır.

**Viyadük Alternatifleri (Kesişim Çözüm Yolları)** — Bir kesişimi viyadüksüz çözmenin 3 yolu vardır. Bu, oyuncunun "her kaza = viyadük" rutinine düşmesini engeller:
1. **Rota Yeniden Çizimi (Routing Around)**: Kesişen yollardan birini geri alıp farklı bir rotayla çizmek — kesişimi tamamen önlemek. `Undo + Yeniden Çiz` ile yapılır. En verimli çözüm (viyadük hakkı korunur).
2. **Tek Yön Yolu (OneWay Yönlendirme)**: Seviye 20+ ile. Kesişim noktasındaki OneWay hücresini kullanarak iki rengin aynı hücreden **farklı zaman penceresinde** geçmesini sağlamak. OneWay yön kısıtı, kesişimi başka türlü önlenemeyecek renkler için tatmin edici bir taktik tercih sunar.
3. **Viyadük Yerleştirme**: Hızlı çözüm, tek tık, ama viyadük hakkı harcar. Mevcut GDD davranışını korur.

Tasarım Hedefi: Bir seviyede en az 2 kesişim varsa, en az biri **OneWay ile çözülebilir** olmalıdır (seviye 20+). Bu sayede viyadük "kolay ama maliyetli", OneWay "ücretsiz ama taktik ister" ikilemi oluşur.

**Uygulama Detayı**: `CellData` sınıfı `HasViaduct` (bool), `UnderColor` (alttan geçen renk) ve `OverColor` (üstten geçen renk) alanlarını tutar. `PlaceViaductCommand` tarafından yönetilir. Viyadük yerleştirildiğinde hücre `CellState.Bridge` durumuna geçer.

Limit Sistemi:
Her seviyede belirli sayıda Viyadük Hakkı verilir.
Varsayılan hak: 3 viyadük / seviye.
Seviye zorluk kademelerine göre değişir (bkz. §9 — Seviye Tasarım Tablosu).
Şehir ekonomi geliştirmeleri (Viyadük Üretim) sayesinde bu limit artırılabilir (LoadLevelCommand, CityEconomyModel.ViaductBonus ile session başlangıcına bonus ekler).
UI'da viyadük sayısı, Bento-Glass panelinde köprü ikonu ile gösterilir (örn: 🌉🌉🌉).
Hak bittiğinde: *Acil Durum Viyadüğü* için ödüllü reklam tetiklenir (+1 köprü hakkı).

Kontrol ve Etkileşim:
Kaza tespit edildikten sonra (Simülasyon aşamasında), oyun Paused durumuna geçer. Oyuncu kesişim noktasına tıklayarak viyadük yerleştirir.
Köprü yerleştirildiğinde: Üstten geçen yol (`OverColor`) daha yüksek Z-offset (-0.4f) ile render edilir, alttan geçen yol (`UnderColor`) normal offset'te (-0.1f) kalır.
Viyadük yerleştirme sonrası oyun otomatik olarak Simülasyon durumuna geri döner.
PlaceViaductCommand, HistoryService.Record() ile undo/redo snapshot'ı alır — viyadük geri alınabilir.
Görsel Detay:
Viyadük hücrelerinde 3D köprü görseli (deck + pillar) oluşturulur.
Üstteki yol, alttaki yolun %60 opaklığında görünür — oyuncu alttaki aracı hâlâ görebilir.
Köprü yerleştirme sırasında hücre `CellState.Bridge` olarak işaretlenir.
2.6 Undo (Geri Al) Sistemi
Undo Stack: Sınırsız geri alma. Oyuncu istediği kadar hamle geri alabilir.
Davranış: Geri alınan yol silinir, kaynak ve hedef düğümler tekrar bağlanabilir hale gelir.
Viadük İadesi: Viyadük kullanılarak çözülen bir kesişim geri alınırsa, viyadük hakkı iade edilir.
Kazadan Sonra: Kaza yapan yolun tamamı geri alınmak zorunda değildir — oyuncu yolun sadece kesişim noktasından sonraki segmentini de geri alabilir (partial undo).
2.7 Çoklu Yol ve Kesişim Kuralı (Multi-Path Routing)
Bir grid hücresinden aynı anda birden fazla renkli yol geçebilir.
Bu, yolların kesişmesine (intersection) izin verir — oyuncu stratejik olarak yolları üst üste çizebilir.
İki farklı renk aynı hücrede kesiştiğinde, bu hücre "çakışma noktası" olarak işaretlenir (⚠ soft warning ikonu).
Çakışma noktasında viyadük yoksa, simülasyon sırasında araçlar çarpışır (kaza).
Viyadük yerleştirildiğinde: bir yol "üstten" (OverColor — yükseltilmiş Z-offset), diğeri "alttan" (UnderColor) geçer.
Bir hücrede maksimum 2 farklı renk kesişebilir (viyadük limiti). 3+ renk kesişimi desteklenmez.
2.8 Kazanma Koşulu ve Izgara Kaplama Mantığı (Win Conditions & Grid Coverage)
- **Temel Kazanma Koşulu (Aktif Flow Doğrulama)**: Grid üzerindeki tüm renk çiftleri kaza yapmadan bağlandığında simülasyon fazı başlar. **Pasif 10 saniye bekleme yerine**, simülasyon boyunca **aktif doğrulama** çalışır:
  - **Trafik Akış Skoru (Flow Score)**: Her bağlanan renk çifti, araçlarının hedef düğüme ulaşma sayısına göre +puan üretir. Araç hedefe ulaştığında kaynaktan yeniden doğar (sirkülasyon).
  - **Kazanma Eşiği**: Tüm renk çiftlerinin toplam Flow Score'u, o seviye için tanımlı threshold değerine (örn. toplam renk sayısı × 5 ulaşım) ulaştığında **akış doğrulanmış** kabul edilir ve bölüm tamamlanır.
  - **Otokontrol**: Simülasyon sırasında herhangi bir araç beklerse, yavaşlarsa veya kısıtlanırsa (NarrowPass kuyruğu, OneWay yön ihlali denemesi) o rotadan Flow Score üretimi durur. Oyuncu **görsel olarak hangi rotanın tıkalı olduğunu** kenarlarındaki kırmızı pulse ile görür.
  - **Sonuç**: Oyuncu pasif izleyici değil, **simülasyon kalitesini izleyen aktif bir gözlemcidir**. Kötü planlama yapan oyuncu Flow Score'un yavaş ilerlediğini görür ve daha verimli rota arar. İyi planlama yapan oyuncu eşik değerine 8-15 saniyede ulaşır.
- **Flow Score Threshold Çarpanı**: Faz 1'de düşük (%50 düşük, sadece 1-2 ulaşım yeterli), Faz 3-4'te yüksek (her renkten en az 8-10 ulaşım). İleri seviyeler oyuncuyu "her araç en az N kez hedefe ulaşmalı" verimlilik hedefine iter.
- **Kademeli Izgara Kaplama Kuralı (`requireFullGridCoverage`)**:
  - **Erken ve Orta Seviyeler (Seviye 1–28)**: `requireFullGridCoverage = false` geçerlidir. Oyuncu grid üzerinde boş alan bıraksa dahi tüm renkleri kaza yapmadan eşleştirip Flow Score threshold'una ulaştığında bölüm kazanılır. Bu sayede düşük frustrasyon ve yüksek dopamin hedeflenir.
  - **İleri Zorluk Seviyeleri (Seviye 29+ & Zor Günlük Krizler)**: `requireFullGridCoverage = true` kuralı devreye girer. Tüm düğümler bağlansa dahi grid üzerinde tek bir boş hücre (`CellState.Empty`) kaldığı sürece zafer tetiklenmez. Oyuncu grid'in %100'ünü kaplamak zorundadır (*Perfect Flow Clear*).

Bonus Yıldız Sistemi (Seviye Başına):
| Yıldız | Kriter |
| --- | --- |
| ⭐ | Bölümü tamamlamak (tüm renk düğümlerini kazasız bağlamak) |
| ⭐⭐ | 2 veya daha az viyadük kullanarak tamamlamak |
| ⭐⭐⭐ | Hiç viyadük kullanmadan tamamlamak (Perfect Flow) |
## Meta Oyun ve Idle Ekonomi Raporu
Bulmaca ekranı oyuncunun *Altyapı Planlama Masası*dır. Buradaki başarılar, oyunun ana omurgası olan şehir ekranını besler.
3.1 İzometrik Hub (Ana Ekran)
Oyuncu oyunu açtığında, karşısına düşük poligonlu (low-poly), ışıklandırması pürüzsüz ve yaşayan izometrik bir şehir görünümü çıkar. Şehir, bulmacalarda ilerledikçe büyür ve gelişir.
3.2 Vergi Üretimi (Idle Döngü)
Bulmacalarda kurulan yollar, ana ekrandaki şehrin trafik akış hızını artırır.
Her bölüm geçildiğinde şehre kalıcı altyapı bonusu eklenir.
Vergi üretim hızı formülü:
```
Vergi/saniye = Temel_Hız × (1 + Altyapı_Bonusesi) × Şehir_Seviye_Çarpanı

Temel_Hız = 10 jeton/saniye (Seviye 1'den sonra)
Altyapı_Bonusesi = Geçilen_bölüm_sayısı × 0.15
Şehir_Seviye_Çarpanı = 1 + (Şehir_Seviyesi × 0.1)
```
Offline üretim: Oyuncu oyunda yokken üretim devam eder.
Maksimum offline süre: 8 saat.
Offline verim: Online üretimin %50'si.
Oyuncu geri döndüğünde biriktirilen vergi, Bento-Glass bir *Hoş Geldin* panelinde gösterilir.
3.3 Ekonomi Denge Tablosu (Balance Sheet)
| Seviye Aralığı | Vergi Hızı (jeton/sn) | Bina Maliyeti (1. kademe) | Tahmini Bekleme Süresi |
| --- | --- | --- | --- |
| 1–12 (Faz 1) | 10 → 28 | 50 → **800** | 0–5 saniye |
| 13–28 (Faz 2) | 30 → 52 | 1.**200** → 8.**000** | 1–5 dakika |
| 29–45 (Faz 3) | 55 → 80 | 12.**000** → 90.**000** | 5–30 dakika |
| 46–60 (Faz 4) | 82 → **110** | **120**.**000** → **800**.**000** | 30 dakika – 2 saat |
Logaritmik Maliyet Artışı (Sonraki Kademe):
```
Sonraki_Kademe_Maliyeti = Mevcut_Maliyet × (1.35 ^ Kademe_Numara)
```
Soft Cap Mekanizması:
Oyuncu bir bulmacada tıkandığında, şehre dönüp vergi toplar ve bekleme süresi yaşar.
Bekleme süresi, oyuncunun bulmaca becerisiyle orantılıdır — iyi oyuncular daha az bekler.
*Overclock* (reklamlı 2x hız) butonu, bekleme frustrasyonunu azaltır.
3.4 Upgrade Tree (Gelişim Ağacı)
Oyuncu biriken vergileri aşağıdaki kategorilerde harcar:
| Kategori | Yükseltme Örnekleri | Maks. Kademe | Maliyet Artışı |
| --- | --- | --- | --- |
| Vergi Depolama | Kapasite: 1K → **50K** → **500K** → 5M | 10 | ×1.5 / kademe |
| Vergi Üretim Hızı | Temel hız çarpanı ×1.0 → ×3.0 | 15 | ×1.4 / kademe |
| Viyadük Üretim | Bulmaca başına viyadük hakkı: 3 → 5 | 8 | ×1.6 / kademe |
| Offline Süre | Maks offline: 8s → 24s | 5 | ×2.0 / kademe |
| Mahalle Kilidi | Yeni bölge açma (her bölge yeni bina tipi + yeni görsel) | 6 (toplam) | Sabit fiyat (artan) |
Mahalle Açılım Sırası:
Merkez (Başlangıç)
Liman Bölgesi (Seviye 10)
Üniversite Kampüsü (Seviye 20)
Teknoloji Vadisi (Seviye 30)
Havalimanı Bölgesi (Seviye 42)
## Merkez Plaza (Seviye 55)
## Oyuncu Yaşam Döngüsü (15-20 Günlük İlerleme)
İçerik tüketimi ve zorluk eğrisi, oyuncunun oyuna bağlılığını (Retention) maksimize edecek şekilde 4 ana faza yayılmıştır:
Faz 1: Eğitim ve Bağımlılık (Gün 1-3 | Seviye 1-12)
Parametre	Değer
Grid Boyutu	5×5 → 6×6
Renk Sayısı	1 → 2
Kaza Mekaniği	❌ Aktif değil
Viyadük	❌ Yok
Engeller	❌ Yok
Bölüm Başına Süre	15–30 saniye
Hedef: Yüksek dopamin, düşük frustrasyon. Oyuncu sadece temel renk bağlama mantığına ve araç akışının tatminine odaklanır. Şehir geliştirmeleri çok ucuzdur — oyuncu anında büyüme hisseder.
Faz 2: Düğüm ve Çözüm (Gün 4-8 | Seviye 13-28)
Parametre	Değer
Grid Boyutu	7×7
Renk Sayısı	2 → 3
Kaza Mekaniği	✅ Aktif (Seviye 13'te tanıtılır)
Viyadük	✅ Aktif (3 hak/seviye)
Tek Yön Yollar	✅ Seviye 20+ (Faz 2'nin ikinci yarısı — viyadüğe alternatif taktik)
Engeller	❌ Yok
Bölüm Başına Süre	45–90 saniye
Hedef: Stratejik düşünmeyi tetiklemek. Kaza mekaniği ve viyadük kullanımı kademeli olarak öğretilir (bkz. §10 — Onboarding). Seviye 20+ ile birlikte tek yön yollar tanıtılarak oyuncuya kesişimleri viyadüksüz çözmenin ikinci yolunu açar.
Faz 3: Şehir Planlamacısı (Gün 9-15 | Seviye 29-45)
Parametre	Değer
Grid Boyutu	8×8 → 9×9
Renk Sayısı	3 → 4
Kaza Mekaniği	✅ Aktif
Viyadük	✅ Aktif (3–4 hak/seviye)
Engeller	✅ Göller, parklar, inşaat alanları
Tek Yön Yollar	✅ Seviye 29+ (Faz 2'den devamlı)
Bölüm Başına Süre	90–**150** saniye
Hedef: *Uzman Mimar* hissi. **D14** retention metriğini sabitlemek. Faz 2'de öğrenilen tek yön taktiği burada engellerle birleşir.
Faz 4: Endgame ve Canlı Operasyon (Gün 16-20+ | Seviye 46-60)
Parametre	Değer
Grid Boyutu	10×10
Renk Sayısı	4 → 5
Engeller	✅ Tüm engeller + hareketli engeller (feribot rotaları)
Bölüm Başına Süre	**120**–**240** saniye
Hedef: En karmaşık bulmacalar. *Merkez Plaza*nın kilidini açma motivasyonu.
Sonsuz Döngü: Günlük Trafik Krizleri (Daily Contracts)
60 seviye tamamlandığında oyun durmaz:
Her gün, haritada 3 adet prosedürel *Trafik Krizi* noktası belirir.
Her kriz, 10×10 grid üzerinde özel kısıtlamalı bir bulmacadır.
Kriz ödülleri: Nadir kozmetik tema parçaları, altın jetonlar ve *Mimarlık Rozetleri.*
## Prosedürel üretim algoritması her zaman çözülebilirlik garantisi ile çalışır (bkz. §9.3).
## Hissiyat (Game Feel) ve Görsel Standartlar
Oyunun premium hissettirmesi ve yüksek organik indirme potansiyeli için görsel tatmin en üst seviyede tutulmuştur.
5.1 Kesintisiz Yakınlaşma (Seamless Zoom-in)
Siyah yükleme ekranları tamamen yasaktır.
Oyuncu Hub ekranında bir mahalleye tıkladığında, kamera gökyüzündeki izometrik geniş açıdan pürüzsüz bir şekilde (Cinemachine Lerp, 0.8s ease-in-out) aşağı süzülerek doğrudan bulmaca alanına odaklanır.
Geri dönüşte aynı animasyon ters yönde çalışır.
5.2 Renk Psikolojisi ve Kontrast
Zemin: Mat Obsidyen Siyahı `#**0B0F19**`.
Neon Yollar: Yüksek kontrastlı parlak pastel neonlar:
Elektrik Mavisi `#**00D4FF**`
Sıcak Pembe `#**FF3D7F**`
Güneş Sarısı `#**FFD93D**`
Nane Yeşili `#**6BCB77**`
Ultraviyole `#**B36BFF**`
UI: Yarı şeffaf beyaz overlay'ler, neon alt çizgiler.
5.3 Bento-Glass Tasarım Sistemi
Tüm UI elemanları *Bento* kutusu düzenindedir.
Paneller: Glassmorphic efekt — arkasındaki şehrin ışıklarını kırar, bulanıklaştırır.
**CSS** karşılığı: `backdrop-filter: blur(20px); background: rgba(**255**,**255**,**255**,0.08);`
Kenarlık: 1px `rgba(**255**,**255**,**255**,0.12)` + hafif inner glow.
Köşe yarıçapı: 16px (mobil dokunma ergonomisi).
5.4 Game Juice (Oyun Hissi)
Araçlar: Virajları dönerken esnek bezier kavisler çizer (overshoot + settle animasyonu).
Bölüm Geçiş: Tüm ekran **URP** Bloom efektiyle yıkanır. Havai fişek + konfeti partikülleri.
Vergi Toplama: Her jeton toplandığında ekranın kenarından merkeze doğru altın renkli bir akış parçacığı süzülür.
Dokunma Hissi: Tüm tıklamalarda micro-bounce animasyonu (scale 1.0 → 0.95 → 1.0, 120ms).
5.5 Kamera Davranışı
| Durum | Kamera Açısı | Zoom | Süre |
| --- | --- | --- | --- |
| Hub — Genel Görünüm | 45° izometrik | Tam şehir görünür | — |
| Hub — Mahalle Odak | 45° izometrik | Mahalle odaklı | 0.5s lerp |
| Bulmaca Geçişi | 45° → 90° (top-down) | Grid'e tam zoom | 0.8s ease-in-out |
| Bulmaca İçi | 90° (top-down) | Grid tam ekranda | Sabit |
| ## Bulmaca — Pinch Zoom | 90° (top-down) | %50 – %150 arası | Kullanıcı kontrolü |
## Gelir Modeli ve Ekonomi (Monetization Strategy)
Oyuncuyu reklama boğup kaçırmayan, oyunun ritmine sadık kalarak harcama ve izleme isteği uyandıran hibrit model.
6.1 Ödüllü Reklamlar (Rewarded Ads)
| Tetikleyici | Ödül | Frekans Sınırı |
| --- | --- | --- |
| Overclock / Rush Hour (Hub ekranı) | 4 saat boyunca vergi üretimi ×2 | Günde max 6 |
| Acil Durum Viyadüğü (Bulmaca ekranı) | +1 viyadük hakkı | Bölüm başına max 2 |
| Offline Gelir ×3 (Hoş geldin paneli) | Offline birikimi %50 yerine %**150** alma | Her oturum açılışında 1 |
| Ekstra İpucu (Bulmaca ekranı) | Bir sonraki doğru bağlantıyı 3sn highlight | Bölüm başına max 3 |
6.2 Geçiş Reklamları (Interstitial Ads)
Zamanlama:
Bulmaca başarıyla tamamlandıktan sonra Hub'a dönüş sırasında.
Her 3 başarısız denemeden sonra (kaza krizi ekranında).
Koruma: İlk 5 seviyede asla gösterilmez.
Frekans: Minimum 3 dakika ara.
Kapatılabilirlik: 5 saniye sonra *Skip* butonu belirir.
6.3 Uygulama İçi Satın Alma (In-App Purchases)
| Ürün | Fiyat | İçerik | Tür |
| --- | --- | --- | --- |
| No-Ads | $2.99 | Tüm geçiş reklamlarını kalıcı kaldırır. Ödüllü reklamlar kalır. | Tek seferlik |
| Baş Mimar Paketi | $4.99 | 50.**000** jeton + *Siberpunk Gece* teması + kalıcı +1 viyadük hakkı | Tek seferlik (hoş geldin) |
| Jeton Paketi — Küçük | $0.99 | 10.**000** jeton | Tüketilebilir |
| Jeton Paketi — Orta | $2.99 | 35.**000** jeton | Tüketilebilir |
| Jeton Paketi — Büyük | $9.99 | **150**.**000** jeton | Tüketilebilir |
| Temalar (3 farklı) | $1.99 her biri | Görsel tema (Cyberpunk, Sakura, Arctic) | Kozmetik |
6.4 Reklam Gelir Hedefleri (Tahmini)
Metrik	Hedef Değer
Ort. günlük reklam izleme (kullanıcı başına)	2.5 – 4
Rewarded ad fill rate	≥ %85
Interstitial **CPM** (global ort.)	$8 – $15
Rewarded **CPM** (global ort.)	$15 – $30
## IAP dönüşüm oranı	%2 – %4
## Ekran Akış Şeması (UI/UX Screen Flow)
Kullanıcı deneyimi en kısa yoldan en yüksek tatmine ulaşacak şekilde optimize edilmiştir.
```
┌─────────────────────────────────────────────────────────────┐
│                     **BOOT** / **SPLASH** **SCREEN**                     │
│  Stüdyo logosu + minimalist yükleme animasyonu (max 3s)     │
└──────────────────────────┬──────────────────────────────────┘
                           ▼
┌─────────────────────────────────────────────────────────────┐
│                  **HUB** **EKRANI** (İzometrik Şehir)               │
│                                                              │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐      │
│  │ Vergi Sayacı │  │ Görev Paneli │  │ Ayarlar (⚙)  │      │
│  │ (Bento)      │  │ (Bento)      │  │              │      │
│  └──────────────┘  └──────────────┘  └──────────────┘      │
│                                                              │
│  ┌──────────────────────────────────────────────┐           │
│  │        Mahalle Seçim Paneli (Bento)           │           │
│  │  [Merkez] [Liman] [Üniversite] [Tekno] ...   │           │
│  └──────────────────────────────────────────────┘           │
│                                                              │
│  ┌──────────────┐  ┌──────────────┐                        │
│  │ Upgrade Ağacı│  │ Overclock 📺 │                        │
│  └──────────────┘  └──────────────┘                        │
└──────────────────────────┬──────────────────────────────────┘
                           ▼ (Mahalle/Bölge seçimi → Seamless Zoom-in)
┌─────────────────────────────────────────────────────────────┐
│              **CORE** **GAME** (Grid / Bulmaca Alanı)                │
│                                                              │
│  ┌──────────┐  ┌──────────┐  ┌──────────┐                  │
│  │ Geri Al  │  │ Viyadük  │  │ İpucu 💡 │                  │
│  │   (↩)   │  │  🌉 ×3   │  │  📺     │                  │
│  └──────────┘  └──────────┘  └──────────┘                  │
│                                                              │
│              ┌────────────────────┐                          │
│              │                    │                          │
│              │    **GRID** **ALANI**      │                          │
│              │                    │                          │
│              └────────────────────┘                          │
│                                                              │
│  ┌──────────────────────────────────────────────┐           │
│  │         Seviye: 14/60   ⭐⭐☆                │           │
│  └──────────────────────────────────────────────┘           │
└──────────────┬────────────────────────┬─────────────────────┘
               ▼                        ▼
┌─────────────────────────┐  ┌─────────────────────────────────┐
│   KRİZ **EKRANI** (Fail)    │  │   **ZAFER** **EKRANI** (Win State)      │
│                         │  │                                 │
│  *Trafik Krizi!* 🚨     │  │  *Bölüm Tamamlandı!* 🎉        │
│                         │  │                                 │
│  [↩ Geri Al] [Ücretsiz] │  │  Kazanılan: +2.**500** Vergi      │
│  [🌉 Viyadük] [📺 Reklam]│  │  ⭐⭐⭐ (3/3 yıldız)          │
│                         │  │                                 │
│  (Sadece kaza sonrası)  │  │  [▶ Sonraki]  [🏙 Hub'a Dön]  │
└─────────────────────────┘  └─────────────────────────────────┘
```
7.1 Ekran Geçiş Süreleri
| Geçiş | Süre | Animasyon |
| --- | --- | --- |
| Boot → Hub | max 3s | Logo fade-in + şehir fade-up |
| Hub → Bulmaca | 0.8s | Cinemachine zoom-in lerp |
| Bulmaca → Hub | 0.8s | Cinemachine zoom-out lerp |
| Kaza → Kriz Paneli | 0.3s | Panel slide-down + bounce |
| ## Bulmaca → Zafer | 0.5s | Bloom flash + confetti + panel scale-up |
## Onboarding ve Eğitim Akışı (Tutorial Design)
Oyuncuyu ilk 3 günde bağımlı hale getirmek için kademeli ve organik bir eğitim tasarımı.
8.1 Kademeli Öğretim Tablosu
| Seviye | Öğretilen Mekanik | Yöntem |
| --- | --- | --- |
| 1 | Dokunma ve sürükleme | Zorla yönlendirmeli (forced guide) — parmak izi animasyonu |
| 2 | Renk eşleştirme mantığı | Zorla yönlendirmeli — *Mavi evi mavi ofise bağla* |
| 3 | Araç akışı görsel tatmini | Organik — oyuncu serbest bırakılır, araçlar akmaya başlar |
| 4 | Bölüm tamamlama ve yıldız | Zorla yönlendirmeli — *Başlat* butonu highlight edilir |
| 5 | Hub ekranına dönüş | Zorla yönlendirmeli — *Şehrine dön* oku |
| 6–8 | Vergi toplama ve bina yükseltme | Kademeli — Bento panel vurgulanır, dokunma istenir |
| 9–12 | İkinci renk ekleme | Organik — yeni renk düğümü haritada belirir |
| 13 | ⚡ Kaza mekaniği | Zorla yönlendirmeli — Yollar bilerek kesiştirilir, kaza gösterilir |
| 14 | 🌉 Viyadük kullanımı | Zorla yönlendirmeli — Kaza sonrası viyadük butonu highlight edilir |
| 15–17 | Viyadük stratejisi | Organik — birden fazla kesişimli bölümler |
| 18 | Undo (geri al) öğretisi | Kademeli — İpucu balonu: *Yanlış mı çizdin? Geri al!* |
| 20 | ➡️ Tek yön yollar (alternatif taktik) | Kademeli — İpucu balonu: *Kesişimi viyadüksüz çöz: tek yön yolunu kullan!* |
| 21–28 | OneWay + viyadük kombinasyon stratejisi | Organik — Birden fazla kesişim; bazıları OneWay ile, bazıları viyadük ile |
| 29+ | Engeller (göller, parklar) | Organik — İlk engel haritada belirir, ipucu balonu ile |
8.2 Tutorial Tasarım İlkeleri
Asla metin duvarı yok: Her eğitim adımı max 1 cümle + görsel ok.
Tıklanabilir alanlar pulsate eder: Dikkat çekmek için hafif scale animasyonu.
Atlanabilir: İlk 5 seviye hariç, tutorial ipuçları 3 saniye sonra kaybolur.
Tekrar gerekmez: Her mekanik bir kez öğretilir, tekrar edilmez.
## Hata yapma özgürlüğü: Kaza mekaniği öğretilirken oyuncunun bilerek kaza yapması sağlanır — *bu normal, çözülür* hissi verilir.
## Seviye Tasarım Kuralları (Level Design)
9.1 Genel Kurallar
Tüm 60 seviye el yapımıdır (hand-crafted).
Prosedürel üretim yalnızca *Günlük Trafik Krizleri* (endgame) için kullanılır.
Her seviye en az 3 farklı çözüm yolu içermelidir (replayability).
Her seviye matematiksel olarak çözülebilir olmalıdır — QA sürecinde doğrulanır.
9.2 Seviye Şablonu (Level Template)
Her seviye aşağıdaki **JSON**-benzeri yapıda tanımlanır:
```json
{
    *level_id*: 14,
    *grid_size*: [7, 7],
    *nodes*: [
    {*type*: *home*, *color*: *blue*, *shape*: *circle*, *position*: [0, 3]},
    {*type*: *office*, *color*: *blue*, *shape*: *circle*, *position*: [6, 3]},
    {*type*: *home*, *color*: *red*, *shape*: *triangle*, *position*: [0, 0]},
    {*type*: *hospital*, *color*: *red*, *shape*: *triangle*, *position*: [6, 6]}
    ],
    *obstacles*: [],
    *oneway_cells*: [],
    *viaduct_limit*: 3,
    *flow_score_threshold*: 10,
    *target_stars*: {
    *1_star*: *complete*,
    *2_star*: *viaducts_used <= 2*,
    *3_star*: *viaducts_used == 0*
    },
    *tutorial_event*: *crash_intro*,
    *estimated_solve_time_sec*: 60
}
```
**`flow_score_threshold`**: O seviyede kazanmak için gerekli Flow Score toplamı (simülasyon başına ulaşım sayısı). Seviye zorluğuna göre değişir:
- Faz 1 (Lv. 1-12): 3-5 (sadece sirkülasyonu doğrular, frustrasyon yok)
- Faz 2 (Lv. 13-28): 6-10 (kaza-kurtarma taktikleri doğrulanır)
- Faz 3 (Lv. 29-45): 12-18 (her renkten en az 3-4 ulaşım)
- Faz 4 (Lv. 46-60): 18-30 (verimlilik, darboğaz yönetimi)

**`oneway_cells`**: Tek yön hücrelerin koordinatları + yön bilgisi (Seviye 20+ için).
9.3 Prosedürel Üretim Algoritması (Günlük Krizler İçin)
Çözüm-Önce Yaklaşım (Solution-First):
Önce geçerli bir çözüm üretilir (tüm renk çiftleri kesişmeden bağlanır).
Sonra çözümün üzerinden giderek bilerek kesişimler eklenir (viyadük gerektiren).
Bu yaklaşım, her zaman çözülebilirlik garantisi sağlar.
Kısıtlamalar:
Grid boyutu: 10×10 (sabit).
Renk sayısı: 3–5 (rastgele).
Düğüm sayısı: 4–8 çift (zorluk seviyesine göre).
Engel sayısı: 2–6 (göller, parklar).
Viyadük limiti: 4–6.
Minimum çözüm uzunluğu: Her yol en az 5 hücre kaplamalı.
Zorluk Skoru:
```
    Zorluk = (Renk_Sayısı × 10) + (Kesişim_Sayısı × 5) + (Engel_Sayısı × 3) - (Viyadük_Limiti × 4)
    ```
Kolay kriz: 15–25 puan
Orta kriz: 26–40 puan
Zor kriz: 41–60 puan
9.4 Engeller ve Özel Hücreler
| Engel Türü | Davrananış | İlk Görünüiş |
| --- | --- | --- |
| 🏗 İnşaat Alanı | Üzerinden yol geçirilemez, hücre bloke | Seviye 29 |
| 🌊 Gölet | 2×2 veya 3×3, üzerinden yol geçirilemez | Seviye 31 |
| 🌳 Park | 1×1 veya L-şekli, üzerinden yol geçirilemez | Seviye 33 |
| ➡️ Tek Yön Yol | Sadece tek yönde araç geçebilir (ok işareti ile) — viyadüğe taktik alternatif | Seviye 20 |
| ⛴ Feribot Rotası | Her 10 saniyede bir yön değiştirir (hareketli engel) | Seviye 48 |
| 🚧 Dar Geçit | Sadece 1 araç genişliğinde, sırayla geçiş | Seviye 52 |
9.5 Örnek Grid Şeması (Seviye 14 — İlk Kaza)
```
    0    1    2    3    4    5    6
    ┌────┬────┬────┬────┬────┬────┬────┐
0 │ 🏠 │    │    │    │    │    │    │
    │ 🔴 │    │    │    │    │    │    │
    ├────┼────┼────┼────┼────┼────┼────┤
1 │    │    │    │    │    │    │    │
    │    │    │    │    │    │    │    │
    ├────┼────┼────┼────┼────┼────┼────┤
2 │    │    │    │    │    │    │    │
    │    │    │    │    │    │    │    │
    ├────┼────┼────┼────┼────┼────┼────┤
3 │ 🏠 │    │    │    │    │    │ 🏢 │
    │ 🔵 │    │    │    │    │    │ 🔵 │
    ├────┼────┼────┼────┼────┼────┼────┤
4 │    │    │    │    │    │    │    │
    │    │    │    │    │    │    │    │
    ├────┼────┼────┼────┼────┼────┼────┤
5 │    │    │    │    │    │    │    │
    │    │    │    │    │    │    │    │
    ├────┼────┼────┼────┼────┼────┼────┤
6 │    │    │    │    │    │    │ 🏥 │
    │    │    │    │    │    │    │ 🔴 │
    └────┴────┴────┴────┴────┴────┴────┘

Çözüm: Mavi yol → düz yatay (3, satır 3)
    Kırmızı yol → dikey (6, sütun 0→6)
    Bu iki yol (3,3) ve (6,3) noktasında **KES**İŞİR → Kaza!
    Oyuncu viyadük kullanarak çözer.
## ```
## Teknik Gereksinimler
10.1 Motor ve Altyapı
| Bileşen | Seçim | Açıklama |
| --- | --- | --- |
| Oyun Motoru | Unity **2023** **LTS** | Cross-platform, güçlü 2D/3D desteği |
| Render Pipeline | **URP** (Universal Render Pipeline) | Bloom, glassmorphic blur, particle efektleri |
| 2D Fizik | Unity 2D Physics | Araç hareket simülasyonu |
| UI Framework | Unity UI Toolkit + Custom Shaders | Bento-Glass efektleri için custom shader |
| Reklam **SDK** | AdMob / ironSource (levelPlay) | Mediation katmanı ile |
| Analytics | Firebase Analytics + GameAnalytics | **KPI** takibi |
| Remote Config | Firebase Remote Config | Canlı denge ayarları |
| Crash Reporting | Firebase Crashlytics | Hata takibi |
| Cloud Save | Firebase Firestore / Unity Gaming Services | Oyuncu ilerlemesi |
10.2 Hedef Cihazlar ve Performans
| Parametre | Minimum | Hedef |
| --- | --- | --- |
| iOS | iPhone SE (**2020**) / **A13** | iPhone 12+ |
| Android | 2 GB **RAM** / Snapdragon **665** | 4 GB **RAM** / Snapdragon **730**+ |
| **APK** Boyutu | < **150** MB | < **100** MB |
| **RAM** Kullanımı | < **300** MB | < **500** MB |
| **FPS** | 30 **FPS** (minimum) | 60 **FPS** |
| Battery | 3 saatte %15'ten fazla tüketmemeli | — |
10.3 Save System
Otomatik Kayıt: Her işlem sonrası (Local + Cloud).
Bulmaca Ortasında Çıkış: Oyuncunun son çizim durumu (hangi yolların çizildiği, viyadüklerin konumu) kaydedilir. Geri döndüğünde tam olarak kaldığı yerden devam eder.
Multi-Device: Cloud save ile senkronizasyon. Çakışma durumunda *en son değiştirilen* veri geçerli olur.
Progression Sıfırlama: Ayarlar menüsünde *İlerlemeyi Sıfırla* seçeneği. 2 kez onay adımı + *RESET* yazma.
10.4 Offline Oynanabilirlik
Bulmaca ekranı tamamen offline oynanabilir.
Hub ekranı (idle gelir) offline çalışır, ancak reklam izleme gerektiren özellikler çalışmaz.
Günlük krizler internet bağlantısı gerektirir (sunucudan seed alınır).
## Offline gelir birikimi: Maks 8 saat, online hızın %50'si.
## Erişilebilirlik (Accessibility)
11.1 Renk Körlüğü Desteği
Özellik	Durum
Renk + Şekil çift kodlama	✅ Varsayılan aktif
Renk körlüğü modları (Protanopia, Deuteranopia, Tritanopia)	✅ Ayarlar menüsünde
Renk körlüğü modunda palet değişimi	✅ **WCAG** 2.1 AA kontrast oranları
11.2 Genel Erişilebilirlik
Özellik	Durum
Haptic feedback kapatılabilir	✅ Ayarlar menüsünde
Ses efektleri kapatılabilir	✅ Ayarlar menüsünde
Müzik kapatılabilir	✅ Ayarlar menüsünde
Animasyon azaltma (Reduce Motion)	✅ OS seviyesi ayar dinlenir
Minimum dokunma hedef boyutu	44×44 pt (Apple **HIG**) / 48×48 dp (Material)
Font boyutu ölçeklendirme	⚠️ Metin minimum, şekil ikonu ağırlıklı — düşük öncelik
Tek elle oynanabilirlik	✅ Tüm etkileşimler ekranın alt yarısında
11.3 Uyumluluk Standartları
Apple App Store Erişilebilirlik rehberine uygunluk.
Google Play Accessibility gereksinimlerine uygunluk.
## WCAG 2.1 Level AA kontrast oranları (metin/UI elemanları).
## Edge Case'ler ve Hata Yönetimi
Senaryo	Çözüm
Oyuncu aynı yolu iki kez çizer	İkinci çizim, ilkinin üzerine yazılır (override). Uyarı yok.
Tüm viyadükler bitti, bölüm çözülemez	*Acil Durum Viyadüğü* ödüllü reklam tetiklenir. Alternatif olarak *Geri Al* ile yolu değiştirme teşvik edilir. Eğer oyuncu reklam izlemek istemiyorsa ve gerçekten çözülemezse (tasarım hatası) → otomatik *Seviyeyi Geç* (grace skip).
Oyuncu bulmaca ortasında oyunu kapatır	Local save + Cloud save ile tam durum kaydedilir. Geri döndüğünde kaldığı yerden devam.
İnternet bağlantısı kopar (idle gelir)	Local clock ile hesaplanır. Geri bağlanınca Cloud ile senkronize. Sahte saat manipülasyonu koruması: Server-authoritative timestamp doğrulaması.
Birden fazla cihazda aynı hesap	Cloud save ile son değişiklik geçerli. Nadir çakışma durumunda *hangi cihazdan devam edilsin?* seçeneği sunulur.
Oyuncu hiç kaza yapmadan seviyeyi bitirir	Perfect Clear kabul edilir, 3 yıldız verilir. Viyadük kullanılmamış bonusları birikir.
Grid tamamen doldu ama bağlantı kurulamadı	Otomatik *Geri Al* önerisi + ipucu butonu highlight. Eğer tasarım hatası ise grace skip.
Oyuncu çok hızlı çizim yapıyor	Çizim hızı sınırlandırılmaz — ancak araç simülasyonu her zaman aynı hızda çalışır (render hızından bağımsız).
## Çok uzun süre seviyede kalma	5 dakika sonra nazik hatırlatma: *Mola vermek ister misin?* (kapatılabilir, agresif değil).
## Canlı Operasyon Planı (LiveOps)
13.1 Günlük Trafik Krizleri
Her gün 00:00 (**UTC**) sunucuda 3 yeni kriz seed'i oluşturulur.
Krizler 24 saat boyunca erişilebilir.
Her kriz tamamlandığında özel bir *Kriz Çözüldü* rozet parçası kazanılır.
7 günlük kriz streak'i: Özel kozmetik ödül.
13.2 Sezonluk Etkinlikler
Süre: Her sezon 4 hafta.
Tema: Her sezon farklı bir şehir teması (örn: *Tokyo Neon*, *Arctic Transit*, *Desert Express*).
İçerik:
20 özel sezon bulmacası (mevcut mekaniklere yeni engeller eklenir).
Sezon temasına özel kozmetik seti (binalar, yollar, araçlar).
Sezon geçişi: Sezon puanı tablosu (leaderboard yok, sadece kişisel ilerleme).
13.3 Görev Sistemi (Mission System)
| Görev Türü | Örnek | Ödül |
| --- | --- | --- |
| Günlük Görev | *3 bölüm tamamla* | **500** jeton |
| Haftalık Görev | *10 viyadük kullan* | 2.**000** jeton + 1 skin parçası |
| Sezon Görevi | *Tüm sezon bulmacalarını bitir* | Özel kozmetik set |
| Başarım (Achievement) | *Hiç kaza yapmadan 10 bölüm* | Kalıcı rozet + 5.**000** jeton |
13.4 Sosyal Özellikler (Faz 2 — Post-Launch)
Arkadaş listesine şehir ziyareti (async multiplayer).
*Bu bölümü çöz* challenge paylaşma (screenshot + deep link).
Global haftalık liderlik tablosu (en çok vergi üreten şehir).
13.5 Remote Config Kullanımı
Canlı denge ayarları (vergi hızı, bina maliyetleri, reklam frekansı).
A/B test altyapısı.
Etkinlik açma/kapama.
## Soft launch sırasında market bazlı denge farklılıkları.
13.6 Evergreen İçerik Cadence (D45+ Uzun Vadeli Retention)
Faz 4 sonrası 6 aylık LiveOps planı — long-tail retention (D45+) için:

**Haftalık Cadence** (Sabit):
- Pazartesi: Yeni Daily Crisis batch (3 puzzle, 24 saat ömürlü)
- Çarşamba: Weekly Challenge (zor bulmaca, 1 hafta ömürlü)
- Cuma: "Flash Grid" mini-event — küçük bulmaca, hızlı flow score, +50% jeton

**Aylık Cadence** (Sezon yapısına bağlı):
- Ayın 1'i: Yeni sezon teması (opsiyonel)
- Hafta 2: Yeni Challenge Set (1 büyük içerik paketi)
- Hafta 4: Topluluk bulmacası — Featured Player Puzzle (async)

**3 Aylık Evergreen Döngüsü**:
Her 3 ayda bir yeni **District** skin eklenir + 3 eski Daily Crisis puzzle'ı "Archive" bölümünde kalıcı oynanabilir hale gelir. Bu, D90+ retention için kritiktir.

**Kozmetik Çürüme Mekaniği**: Her sezonun temel teması, sezon bitiminden 2 hafta sonra "Classic" kozmetiklerine döner. 6 ay sonra geçmiş sezon temaları limited-time drop olarak geri gelir (FOMO yöntemi).

**Yıllık Theme Cycle (12 Ay)**: Yılda 2 yeni sezon + sezon aralarında 2 büyük içerik güncellemesi + geçmiş sezonlara Archive erişimi. Bu, eski oyuncuları "nostalji" ile geri çeker.

**Hedef**: D180+ retention %2-3. Top 10 yapımında long-tail retention kritiktir — bu cadence ile hedeflenir.
## KPI ve Başarı Metrikleri
14.1 Soft Launch Hedefleri
| Metrik | Hedef Değer | Minimum Geçerlilik |
| --- | --- | --- |
| D1 Retention | ≥ %45 | %38 |
| D7 Retention | ≥ %20 | %15 |
| **D14** Retention | ≥ %12 | %8 |
| **D30** Retention | ≥ %6 | %4 |
| Ort. Oturum Süresi | 8–12 dakika | 5 dakika |
| Günlük Oturum Sayısı | 2.5 – 3.5 | 2 |
| Ort. Seviye Tamamlama Süresi | 45–90 saniye | 30–**120** saniye |
| Seviye Tamamlama Oranı | ≥ %85 | %70 |
| İlk 10 Seviye留存 (tamamlayıp devam) | ≥ %70 | %55 |
| Reklam İzleme Oranı (Rewarded) | ≥ %40 | %25 |
| **ARPDAU** | $0.08 – $0.15 | $0.05 |
| **CPI** (Cost Per Install) | < $0.50 | < $1.00 |
14.2 Global Launch Hedefleri
Metrik	Hedef
İlk 30 gün indirme	**500**.**000**+
App Store kategorisi	Top 50 Puzzle
App Store rating	≥ 4.5 ★
**IAP** dönüşüm	≥ %3
**MAU** (Monthly Active Users) — 3. ay	**200**.**000**+
14.3 İzlenecek Olaylar (Analytics Events)
| Event | Parametre | Amaç |
| --- | --- | --- |
| `level_start` | level_id, grid_size, renk_sayısı | Hangi seviyelerde oyuncu kalıyor? |
| `level_complete` | level_id, süre, viyadük_kullanımı, yıldız_sayısı | Denge kontrolü |
| `level_fail` | level_id, hata_tipi (kaza/timeout) | Zorluk eğrisi analizi |
| `level_retry` | level_id, deneme_sayısı | Frustrasyon tespiti |
| `level_skip` | level_id, skip_tipi (grace/reklam) | Ödeme/kaçış oranı |
| `upgrade_purchased` | upgrade_tipi, kademe, maliyet | Ekonomi sağlığı |
| `ad_watched` | reklam_tipi, bağlam | Gelir optimizasyonu |
| `ad_skipped` | reklam_tipi | Reklam yorgunluğu |
| `session_start` | offline_süre, birikmiş_vergi | Return-to-play analizi |
| ## `tutorial_step` | step_id, tamamlandı (bool) | Eğitim etkinliği |
## Lokalizasyon Stratejisi
15.1 Lansman Dilleri
| Dil | Öncelik | Pazar |
| --- | --- | --- |
| İngilizliki | P0 | Global (fallback) |
| Türkçe | P0 | TR, **KIBRIS** |
| Almanca | P1 | **DACH** |
| Fransızca | P1 | FR, BE, CA |
| İspanyolca | P1 | **LATAM**, ES |
| Japonca | P2 | JP |
| Korece | P2 | KR |
| Arapça | P2 | **MENA** (**RTL** desteği gerekli) |
| Çince (Basitleştirilmiş) | P2 | CN (mağaza bağımsız) |
| Portekizce (Brezilya) | P3 | BR |
| Rusça | P3 | RU/**CIS** |
15.2 Teknik Gereksinimler
Tüm metinler LocalizationTable (**CSV**/**JSON**) üzerinden yönetilir.
**RTL** desteği: Arapça ve İbranice için CanvasLayout mirroring.
Font: Noto Sans (tüm dilleri kapsayan Unicode font) + fallback chain.
## Metin azaltma stratejisi: Oyun büyük oranda görsel/simgesel. Sadece tutorial, pop-up'lar ve ayarlar metin içerir — çeviri yükü minimumdur.
## Varlık ve Materyal Listesi (Asset Bill of Materials)
16.1 Görsel ve 3D/2D Varlıklar
| Varlık | Format | Adet | Açıklama |
| --- | --- | --- | --- |
| Grid Araçları | 3D Low-poly | 5 | Mavi, Kırmızı, Sarı, Yeşil, Mor araç |
| Düğüm Yapıları | 3D Low-poly | 8 | Ev(×2 varyant), Ofis, Hastane, Okul, Park, **AVM**, Merkez Plaza |
| Köprü Elemanı | 3D Low-poly | 1 | Viyadük modeli (alt+üst yol) |
| Şehir Modelleri | 3D Low-poly modüler | ~30 | Zemin blokları, ağaçlar, 4 kademe bina varyasyonları |
| Engel Objeleri | 3D Low-poly | 6 | İnşaat, gölet, park, tek yön tabelası, feribot, dar geçit |
| Mahalle Zeminleri | 3D Low-poly | 6 | Merkez, Liman, Üniversite, Teknoloji, Havalimanı, Plaza |
| **VFX** Partikülleri | Particle System | 5 | Kaza dumanı, neon yol akışı, bölüm sonu konfeti, jeton akışı, overclock parlaması |
| UI İkonları | **SVG** / **PNG** @2x-@3x | ~50 | Bento panel ikonları, ayarlar, butonlar |
| Tema Setleri | Texture Swap | 3 | Cyberpunk, Sakura, Arctic |
16.2 İşitsel Varlıklar (Audio/**SFX**)
| Varlık | Format | Süre | Açıklama |
| --- | --- | --- | --- |
| UI Tıklama | **WAV**/**OGG** | <0.5s | Mekanik switch hissi, tok ve pürüzsüz |
| Yol Çizme | **WAV**/**OGG** | Loop | Hafif neon vızıltı, sürükleme boyunca |
| Araç Motoru | **WAV**/**OGG** | Loop | Çok hafif elektrikli araç uğultusu |
| Kaza Efekti | **WAV**/**OGG** | <1s | Tok plastik çarpışma + fren |
| Korna | **WAV**/**OGG** | <1s | Kısa, tatlı korna (agresif değil) |
| Jeton Toplama | **WAV**/**OGG** | <0.3s | Kristal *ting* efekti |
| Bölüm Tamamlama | **WAV**/**OGG** | 2–3s | Zafer jingle, yükselen melodi |
| Viyadük Yerleştirme | **WAV**/**OGG** | <1s | *Click-lock* inşaat sesi |
| Ambient — Hub | **WAV**/**OGG** | Loop | Lo-fi rüzgar + uzak trafik uğultusu |
| Ambient — Bulmaca | **WAV**/**OGG** | Loop | Minimalist odaklanma ritmi |
| Ambient — Overclock | **WAV**/**OGG** | Loop | Enerjik, tempolu versiyon |
| ## Müzik — Ana Tema | MP3/OGG | 60–90s | Lo-fi synth, sakin ve premium |
## Geliştirme Yol Haritası (Production Roadmap)
| Aşama | Süre | İçerik |
| --- | --- | --- |
| Pre-Production | 4 hafta | Prototip (core mekanik), **GDD** finalizasyonu, teknik spike |
| Alpha | 8 hafta | Core gameplay + 20 seviye + temel Hub + idle sistemi |
| Closed Beta | 4 hafta | 40 seviye + tüm UI + monetization entegrasyonu |
| Soft Launch | 8 hafta | 60 seviye + LiveOps altyapısı + TR/CA/PH pazarları |
| Global Launch | — | Tüm diller + tam LiveOps + UA kampanyası |
17.1 Ekip Yapısı (Minimum)
| Rol | Kişi Sayısı | Sorumluluk |
| --- | --- | --- |
| Game Designer | 1 | Level design, denge, **GDD** sahipliği |
| Unity Developer (Gameplay) | 1–2 | Core mekanik, grid sistemi, araç simülasyonu |
| Unity Developer (Meta/Backend) | 1 | Idle ekonomi, save system, reklam, analytics |
| 3D Artist | 1 | Modeller, animasyonlar, shader'lar |
| UI/UX Designer | 1 | Bento-Glass sistem, ekran akışı, ikonlar |
| Sound Designer | 1 (freelance) | **SFX**, ambient, müzik |
| QA | 1 | Test, edge case'ler, cihaz uyumluluğu |
| ## Producer | 0.5 | Proje yönetimi, KPI takibi |
## Risk Analizi ve Mitigasyon
| Risk | Olasılık | Etki | Mitigasyon |
| --- | --- | --- | --- |
| Kaza mekaniği çok frustrasyon yaratır | Orta | Yüksek | Soft launch'ta A/B test: Kaza=uyarı vs kaza=kriz. Zorunlu değil, seçenekli. |
| Viyadük mekaniği yeterince anlaşılmaz | Orta | Orta | Kademeli tutorial (Seviye 13-14). İlk kaza bilerek tetiklenir. |
| Idle ekonomi çok hızlı/yavaş | Yüksek | Orta | Remote Config ile canlı denge ayarı. Soft launch'ta 3 farklı ekonomi varyantı test. |
| Renk körlüğü erişilebilirlik şikayetleri | Düşük | Yüksek | Çift kodlama varsayılan. Renk körlüğü modu hazır. |
| 60 el yapımı seviye üretim süresi | Orta | Orta | Prosedürel üretim aracı geliştir (İç tool). El yapımı seviyeler için template sistemi. |
| Rakip oyun (Mini Motorways vb.) pazar payı | Yüksek | Orta | USPsine odaklanma: Neon estetik + idle şehir + viyadük taktiği. UA kreatif'lerinde bu 3'lü vurgulanır. |
| ## APK boyutu büyür (>200MB) | Düşük | Orta | Asset bundling + Addressables. İlk indirme <100MB, geri kalanı on-demand. |
## Hukuki ve Uyumluluk
Konu	Gereksinim
**COPPA** (Çocuk gizliliği)	Oyun 12+ hedefliyor. Reklamlar age-gated. Veri toplama minimum.
**GDPR**	AB kullanıcıları için açık rıza + veri silme hakkı.
App Store Guidelines	**IAP** Apple komisyonu (%30). Reklam **IDFA** izin akışı (**ATT**).
Play Store Policies	Android reklam **SDK** uyumluluğu.
Telif Hakkı	Tüm assetler orijinal veya lisanslı. Üçüncü parti ses/müzik lisansları güvence altında.
## Yaş Sınıflandırması	PEGI 7 / ESRB E (Everyone) hedefleniyor. Şiddet yok, sadece araç kaza efekti (cartoon).
Ekler
Ek A: Renk Paleti Referansı
| İsim | Hex | Kullanım |
| --- | --- | --- |
| Obsidyen Siyah | `#**0B0F19**` | Ana zemin |
| Elektrik Mavisi | `#**00D4FF**` | Mavi araç/yol |
| Sıcak Pembe | `#**FF3D7F**` | Kırmızı araç/yol (renk körlüğü paletinde) |
| Güneş Sarısı | `#**FFD93D**` | Sarı araç/yol |
| Nane Yeşili | `#**6BCB77**` | Yeşil araç/yol |
| Ultraviyole | `#**B36BFF**` | Mor araç/yol |
| UI Beyaz | `rgba(**255**,**255**,**255**,0.08)` | Bento panel arka plan |
| UI Border | `rgba(**255**,**255**,**255**,0.12)` | Panel kenarlığı |
| Başarı Yeşili | `#**4ADE80**` | Başarılı bağlantı |
| Kriz Kırmızısı | `#**EF4444**` | Kaza uyarısı |
Ek B: Font Sistemi
| Kullanım | Font | Boyut |
| --- | --- | --- |
| Başlıklar | Inter Bold / Noto Sans Bold | 24–32sp |
| UI Metin | Inter Medium | 14–18sp |
| Sayılar (Jeton) | Inter Black (Tabular) | 18–24sp |
| Tutorial İpuçları | Inter Regular | 14sp |
Ek C: Haptic Feedback Referansları
| Durum | iOS Haptic | Android Haptic |
| --- | --- | --- |
| Yol çizimi başlangıcı | UI Impact (Light) | VibrationEffect (10ms, 30%) |
| Düğüm bağlantısı | UI Impact (Medium) | VibrationEffect (30ms, 50%) |
| Kaza | UI Notification (Warning) | VibrationEffect (100ms, **100**%) + 50ms pause + 50ms |
| Viyadük yerleştirme | UI Impact (Heavy) | VibrationEffect (60ms, 80%) |
| Vergi toplama | UI Selection | VibrationEffect (10ms, 20%) |
| ## Bölüm tamamlama | Notification Success | VibrationEffect (200ms, 70%) ramp up |
Belge Sonu — Neon Transit **GDD** v2.0.0
Hazırlayan: Oyun Tasarım Ekibi
Son Güncelleme: Temmuz **2026**
Sonraki Adım: Pre-Production başlangıcı — Core mekanik prototipi (2 hafta sprint)

---

## Ek D: Mevcut Implementasyon Durumu (Pixel-Flow-Clone → Neon Transit Geçişi)

Bu bölüm, GDD ile mevcut kod tabanı arasındaki mimari kararları ve uygulama detaylarını belgelemek için eklenmiştir.

### D.1 Framework
- **Nexus Core** MVCS mimarisi kullanılmaktadır (SignalBus, CommandPool, ReactiveModels, DI)
- Tüm komutlar `ICommand<T>` + `IResettable` implemente eder
- Modeller `IReactiveModel` + `INexusService` arayüzlerini kullanır
- Mediator'lar `Subscribe<T>()` ile sinyal dinler
- `GameContextLifecycle.OnConfigure()` tüm bağlamaları fluent API ile kaydeder

### D.2 Grid ve Yol Mimarisi
- `CellData`: `List<ColorType> PathColors` — bir hücreden birden fazla renk geçebilir
- `HasViaduct` (bool), `UnderColor`, `OverColor` — viyadük durumu
- `CellState`: `Empty`, `Node`, `Path`, `Bridge`
- Hareket: Sadece orthogonal (yatay/dikey), çapraz hareket yok
- `PathService`: ClearPath, BacktrackPath, BreakPath — her biri çoklu-yol uyumlu

### D.3 Araç Simülasyonu
- `VehicleSimulator`: `INexusService`, Unity Update döngüsüne `SimulationUpdater` MonoBehaviour ile bağlanır
- **Playing** state'inde: Araçlar hayalet modunda (%60 opak, 3D küp), çarpışma AKTİF — anlık kaza geri bildirimi
- **Simulating** state'inde: Araçlar katı modda, Flow Score threshold'una ulaşma doğrulanır → `LevelCompletedSignal`
- Kaza → `GameState.Paused` → `CrashDetectedSignal` → oyuncu düzeltir → Playing'e dönüş
- Mesafe-tabanlı çarpışma: iki farklı renkli araç <0.5f mesafede + viyadüksüz hücre → kaza

### D.4 Viyadük Sistemi
- `PlaceViaductSignal` → `PlaceViaductCommand` → hücre `HasViaduct=true`, `CellState.Bridge`
- `MaxPathsPerBridge = 2` (BridgeValidationUtility)
- History tracking: `HistoryService.Record()` undo/redo için
- Ekonomi entegrasyonu: `CityEconomyModel.ViaductBonus`, `LoadLevelCommand` session başlangıcına ekler

### D.5 Idle Ekonomi
- `CityEconomyModel`: `IReactiveModel` — jeton, vergi, geliştirme seviyeleri
- `TaxCollectionService`: `INexusService` — Unity Update döngüsünde vergi birikimi
- `UpgradeSignal` → `UpgradeCommand` → `CityEconomyModel.PurchaseUpgrade()`
- Offline kazanç: `CalculateOfflineEarnings()` — max 8 saat, %50 verim
- `HubHUDView` + `HubHUDMediator`: Bento-Glass UI, geliştirme paneli
- `CityHubView` + `CityHubMediator`: 3D low-poly izometrik şehir, bölge bazlı binalar

### D.6 Sinyal Envanteri
| Sinyal | Komut | Tetikleyici |
|---|---|---|
| `InputInteractionSignal` | `ProcessInputCommand` | GridView pointer event'ları |
| `CheckWinConditionSignal` | `CheckWinConditionCommand` | Yol tamamlandığında |
| `PlaceViaductSignal` | `PlaceViaductCommand` | Paused state'de kesişime tıklandığında |
| `UpgradeSignal` | `UpgradeCommand` | HubHUD geliştirme butonu |
| `CrashDetectedSignal` | (HUDMediator dinler) | VehicleSimulator çarpışma tespiti |
| `PathIntersectionWarningSignal` | (HUDMediator dinler) | Yol kesişimi oluştuğunda |
| `LoadLevelSignal` | `LoadLevelCommand` | Seviye yükleme |
| `UndoSignal` / `RedoSignal` | `UndoCommand` / `RedoCommand` | UI butonları |
| `LevelCompletedSignal` | `SaveProgressCommand` | Simülasyon başarılı |
| `TimerTickSignal` | `TimerCommand` | Her frame |
| `GridUpdatedSignal` | (Görsel güncelleme) | Her grid değişikliğinde |

### D.7 Test Kapsamı
- **EditMode**: ~45 test — grid, input, win condition, hint, solver, procedural generation, GDD palette, faz progresyon, save/restore, kriz mekaniği, renk körlüğü
- **PlayMode**: 10 test — full game flow, timer, session lifecycle, progress, viaduct, economy
- CityEconomyModel ve VehicleSimulator her iki modda da bağlanmıştır

### D.8 Foundation (State + Camera + Save) — Yeni
- **GameState flow**: Boot → Splash → Hub (MainMenu) veya Restore → Playing → LevelCompleted → Hub
- `ReturnToHubCommand`: level sonu otomatik hub dönüşü, viyadük hakkı kaybı
- `SaveThrottler`: per-input 2 saniyelik throttle ile diske yazma
- `GridStateSerializer`: `Load()` + `ApplyToGrid()` ile full restore
- `CameraController` + `CameraControllerMediator`: state değişimine otomatik zoom-in/out

### D.9 Progresyon & İçerik (GDD §3.5 + §9.1-9.5) — Yeni
- `PhaseDefinition`: 4 fazlık progresyon eğrisi (12/16/17/15 = 60 level)
- `GddColorPalette`: 5 standart renk (Mavi/Kırmızı/Sarı/Yeşil/Mor)
- `ProceduralLevelGenerator`: **Solution-First** algoritma + path kesişimlerinden köprü türetme + Faz 3-4 obstacle üretimi
- `CreatePhase1And2HandCraftedPack`: SetupWindow'da 12 el yapımı level üretici (deterministic seed=12345)

### D.10 Erişilebilirlik (GDD §11) — Yeni
- `ColorBlindPalette`: Protanopia/Deuteranopia/Tritanopia palette remap
- `HapticService`: iOS + Android haptic feedback (6 desen)
- `SettingsView` + `SettingsMediator`: Volume slider, renk körlüğü dropdown, haptic toggle
- `CellView.GetColor(colorType, colorBlindMode)`: palette-aware renk

### D.11 Crisis Mekaniği (GDD §2.4) — Yeni
- `GameSessionModel.MarkCrisisUndoUsed()`: kaza sonrası Geri Al seçildiğinde MaxViaducts -1
- `CrisisAdService.RecordCrisisAttempt()`: 3 retry sonrası `RequestInterstitialAdSignal` + `ViaductExhaustedSignal`
- `PlaceViaductCommand`: viyadük bittiğinde `ViaductExhaustedSignal` ateşler

### D.12 Ödüllü Reklam Event'leri (GDD §6.1) — Yeni (SDK'sız)
- `RequestRewardedAdSignal` + `RewardedAdType` enum (Overclock/EmergencyViaduct/OfflineTriple/ExtraHint)
- `RequestInterstitialAdSignal` (3 retry sonrası)
- `RewardedAdCommand` + `InterstitialAdCommand`: SDK adapter'ı bağlanana kadar placeholder

### D.13 Engeller (GDD §9.4) — Yeni
- `ObstacleService`: OneWay (ters yön reddi), Ferry (10s yön değişimi), NarrowPass (kuyruk)
- `ProcessInputCommand`: OneWay yön kontrolü
- `VehicleSimulator.Tick()`: her frame obstacle update

### D.14 Audio (GDD §16.2) — Yeni
- `ProceduralAudioFactory`: 12 SFX tipi için runtime synth (Crash, Horn, Viaduct, LevelComplete, CoinCollect, UIClick, PathDraw, VehicleEngine + 3 ambient + MainTheme)
- `AudioService.InitializeAsync`: her tip için procedural clip atar, loop olanlar için `source.loop=true`

### D.15 Tutorial (GDD §8) — Yeni
- `TutorialDriver`: 12 step, level_index → step mapping, persistence (PlayerPrefs bitmask)
- `TutorialView` + `TutorialMediator`: ipucu balonu, parmak izi animasyonu, 3s auto-hide

### D.16 Hub UI (GDD §7) — Yeni
- `CityHubView`: tıklanabilir 3D district binalar (BoxCollider + OnMouseDown)
- `CityHubMediator.OnDistrictClicked`: `EnterDistrictSignal` ateşler
- `MahalleSelectorView`: 6 district butonu (UI tabanlı alternatif)
- `UpgradeTreeView`: 5 upgrade tree (Bento-Glass)

### D.17 Game Juice (GDD §5.4) — Yeni
- `BloomFlashView`: Level complete anında altın flash overlay (URP)
- `ConfettiView`: 80 kübik parçacık, yer çekimi ile düşme
- `CoinFlowView`: 12 altın kübik parçacık, ekran kenarından merkeze akış
- `HUDView.DoBloomFlash()`: built-in flash animasyonu
- `CellView.TriggerBounceAnimation(pressScale: 0.95f, duration: 0.12f)`: GDD §5.4 uyumlu micro-bounce (1.0→0.95→1.0, 120ms)
- `VehicleSimulator` bezier overshoot: virajlarda overshoot+settle animasyonu (Catmull-Rom spline + perp offset + settle ramp)

### D.18 Cloud Save Simülasyonu (GDD §10.3-10.4) — Yeni
- `Models/CloudSaveManager`: Firebase/Firestore adapter'ı bağlanana kadar local PlayerPrefs üzerinde cloud save simülasyonu
- `CloudSaveRecord`: PlayerId, TimestampUnix, LocalSaveJson, CloudSaveJson, Version
- `ResolveConflict`: "en son değiştirilen kazanır" stratejisi (GDD §10.3)
- `SyncToCloud`: Save sonrası otomatik cloud sync (timestamp güncelleme)
- `GameBootstrapper.Start()`: boot'ta conflict resolution uygular, kazanan save'i restore eder
- Server-authoritative timestamp doğrulaması: sahte clock manipülasyonuna karşı koruma

### D.19 NarrowPass Kuyruk Mantığı (GDD §9.4) — Yeni
- `ObstacleService.CanVehicleEnterNarrowPass(cell, color)`: hücre boşsa veya aynı renkteyse true
- `ObstacleService.OnVehicleEnteredNarrowPass(cell, color)`: hücreyi o renge kilitler
- `ObstacleService.OnVehicleLeftNarrowPass(cell, color)`: sadece aynı renk bıraktıysa serbest bırakır
- `VehicleSimulator.SpawnVehicle`: dar geçit dolu ve farklı renk ise spawn'ı erteler
- `VehicleSimulator.UpdateMovement`: araç dar geçide girdiğinde/çıktığında hook'ları çağırır

### D.20 IPlayerPrefsService String Desteği — Yeni
- Interface'e `GetString(key, defaultValue)` + `SetString(key, value)` eklendi
- `UnityPlayerPrefsService` gerçek PlayerPrefs string API'sine delege eder
- `InMemoryPlayerPrefsService` (test) string'leri ayrı dictionary'de saklar
- `GameBootstrapper` artık local save string'lerini IPlayerPrefsService üzerinden okur/yazar
- Cloud save PlayerId ve JSON payload'ları için kullanılır

### D.21 Upgrade Tree Visual Connections — Yeni
- `UpgradeTreeView._connectionContainer`: connection line'ların parent'ı
- 5 upgrade butonu arasında dependency-aware çizgiler: Storage→Rate, Storage→Viaduct, Rate→Offline, Viyaduct→District, Rate→District
- `_connectionColor` (mavi, %60 alpha) ve `_connectionMaxedColor` (yeşil, %80 alpha) — upgrade seviyesine göre renk değişimi
- Her `UpdateInfo()` çağrısında connection'lar yeniden çizilir (eski line'lar `ClearConnectionLines()` ile temizlenir)

### D.22 Tutorial Finger Animation — Yeni
- `TutorialView._fingerIndicator`: hareketli parmak ikonu
- `StartFingerAnimation(step)`: step'e göre hedef pozisyonu hesaplar (TouchAndDrag → 0,-100; Crash → -120,-50 vb.)
- `Update()`: parmak hedef pozisyona doğru PingPong ile hareket eder + scale pulse (1±0.08, 4Hz)
- `_fingerTarget` inspector field'ı varsa ona kilitlenir, yoksa step-specific fallback pozisyon

### D.23 Test Coverage Genişletme — Yeni
- **NarrowPass kuyruk testleri (4)**: serbest/dolu/ayrılış/yanlış renk ayrılışı
- **CloudSave conflict testleri (5)**: no-cloud/local-newer/cloud-newer/PlayerId round-trip/SyncToCloud
- **ObstacleType testleri (2)**: default + LoadLevel population
- **Crisis testleri (4)**: MarkCrisisUndoUsed/MinimumOne/RetryCount/BonusViaduct
- **SaveRestore test (1)**: RetryCount reset on new level load
- Toplam EditMode test sayısı: ~50+ (Phase2AndAccessibilityTests + mevcut PixelFlowGameLogicTests)

### D.24 Mimari Kararlar — Yeni
- **BentoGlass URP Shader** (`Assets/Scripts/PixelFlow/Shaders/BentoGlass.shader`): SDF rounded box + corner radius + border + noise-based blur approximation + highlight
- **PixelTextHelper** (`Assets/Scripts/PixelFlow/Views/PixelTextHelper.cs`): TMP_PRESENT define ile legacy Text / TMP_Text köprüsü (TextMeshPro migration için altyapı)
- **CellData.ObstacleType** field eklendi → LoadLevelCommand populates → CellView.ApplyObstacleVisual() render eder
- **CameraController** [Mediator] attribute + CameraControllerMediator ile DI — public field'lar (GameStateModel, SignalBus) Mediator tarafından inject edilir