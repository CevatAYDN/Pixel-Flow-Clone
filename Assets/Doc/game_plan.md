# Pixel Flow Klonu - %100 Tam Sürüm Geliştirme Planı (Nexus Core Tabanlı)

## 1. Proje Özeti
Bu proje, Google Play'deki "Pixel Flow" oyununun tüm oynanış mekaniklerini, ilerleme sistemlerini, ipucu entegrasyonunu ve görsel temalarını `com.nexus.core` paketi kullanılarak Unity'de eksiksiz bir şekilde klonlamayı hedefler.

## 2. Genişletilmiş Mimari Tasarım (MVCS)

### 2.1. Modeller (Veri Katmanı)
* **`GridModel`**: Gridin boyutları, hücre durumları (boş, duvar, nokta, yol, **köprü**), her rengin başlangıç/bitiş noktaları ve mevcut çizilen yol verisi (`Dictionary<ColorType, List<Vector2Int>>`).
* **`LevelModel`**: Mevcut paket (örn: 5x5 Başlangıç), seviye indeksi, hedeflenen bağlantı sayısı ve zorluk çarpanı.
* **`GameStateModel`**: Oyunun anlık state'i (MainMenu, Playing, Paused, LevelCompleted).
* **`ProgressModel`**: Oyuncunun kayıt dosyası (hangi seviyeler açık, kazanılan yıldızlar/paralar). JSON/PlayerPrefs ile serileştirilir.
* **`SettingsModel`**: Ses seviyesi, müzik seviyesi ve aktif görsel tema (Dark/Light/Neon).
* **`HintModel`**: Oyuncunun elindeki ipucu sayısı ve bekleme süreleri (Cooldown).

### 2.2. Sinyaller (İletişim Katmanı)
* **`InputInteractionSignal`**: Kullanıcı grid üzerinde dokunma/sürükleme yaptığında (PointerDown, Drag, PointerUp).
* **`GridUpdatedSignal`**: GridModel'de bir yol çizildiğinde veya silindiğinde.
* **`LevelCompletedSignal`**: Tüm eşleştirmeler yapılıp grid %100 dolduğunda.
* **`LoadLevelSignal`**: Belirli bir bölüm yüklenmek istendiğinde.
* **`ProgressUpdatedSignal`**: Oyuncu bir bölüm geçtiğinde kayıt dosyası değişirse.
* **`RequestHintSignal`**: Oyuncu ipucu butonuna bastığında.
* **`ThemeChangedSignal`**: Görsel tema (Dark/Light) değiştirildiğinde.

### 2.3. Komutlar (İş Mantığı Katmanı)
* **`LoadLevelCommand`**: Seviye verisini JSON veya ScriptableObject'ten okur, Grid'i inşa eder.
* **`ProcessInputCommand`**: Geçerli sürükleme hamlelerini hesaplar. Çapraz gitmeyi engeller, farklı renklerin kesişiminde (köprü hücresi değilse) eski yolu koparır, köprü hücresinde ise iki farklı rengin birbirinin üzerinden geçmesine izin verir.
* **`CheckWinConditionCommand`**: Tüm renkler bağlandı mı ve boş hücre kalmadı mı kontrol eder.
* **`UseHintCommand`**: `HintModel`'i kontrol eder, yeterli ipucu varsa çözülmemiş bir renk çiftinin doğru yolunu `GridModel`'e yazarak `GridUpdatedSignal` fırlatır.
* **`SaveProgressCommand`**: Bölüm bittiğinde veriyi diske/buluta kaydeder ve sonraki bölümün kilidini açar.

### 2.4. Görünümler ve Arabulucular (UI & Scene Katmanı)
* **`GridMediator` & `GridView`**: Hücreleri oluşturur. LineRenderer veya UI çizgileri ile renk akışını (Flow) çizer.
* **`LevelPackMediator` & `LevelPackView`**: Ana menüdeki 5x5, 6x6, 9x9 gibi zorluk paketlerini listeler.
* **`HUDMediator` & `HUDView`**: İlerleme yüzdesi, anlık hamle sayısı ve İpucu butonunu yönetir.
* **`SettingsMediator` & `SettingsView`**: Tema seçimi, ses ve müzik aç/kapat kontrollerini sağlar.

---

## 3. Geliştirme Fazları (Eksiksiz Yol Haritası)

### Faz 1: Core Altyapı ve Veri Modeli
1. `GameContext` oluşturulup Root'a bağlanacak.
2. Tüm Model'ler (Grid, Level, Progress, Settings) Dependency Injection ile Context'e eklenecek.
3. Seviye verilerini tutacak ScriptableObject yapısı (LevelData ve LevelPack) kurulacak.

### Faz 2: Çekirdek Oynanış (Grid ve Çizim)
1. `GridView` içinde grid hücreleri dinamik olarak instantiate edilecek.
2. `InputInteractionSignal` entegre edilecek (Dokunma, sürükleme, bırakma).
3. `ProcessInputCommand` yazılarak çizim mantığı kusursuzlaştırılacak (Geri gitme, yolu üzerine yazarak koparma, renk kesişimini engelleme).

### Faz 3: Kazanma, İlerleme ve Kayıt Sistemi
1. `CheckWinConditionCommand` ile %100 doluluk ve bağlantı kontrolü yapılacak.
2. Bölüm bitince `SaveProgressCommand` çalışarak veriyi JSON/PlayerPrefs olarak kaydedecek.
3. Bir sonraki bölümün kilidi açılacak ve `LevelCompletedSignal` ile UI gösterilecek.

### Faz 4: Paketler ve Seviye Seçim Ekranı
1. Ana menü tasarlanacak. Zorluk seviyelerine göre (Örn: 5x5 Regular, 6x6 Advanced) paket görünümleri oluşturulacak.
2. `ProgressModel` okunarak hangi paketlerin ve seviyelerin açık/kilitli olduğu görselleştirilecek.

### Faz 5: İpucu Sistemi (Hint System)
1. HUD'a bir ipucu butonu eklenecek.
2. `UseHintCommand` yazılarak, oyundaki doğru çözüm dosyasından (LevelData içinde bulunmalı) eksik bir yol çekilecek.
3. Sistem bu yolu otomatik olarak çizecek ve o renge kilit koyacak (oyuncu yanlışlıkla bozmasın diye).

### Faz 6: Görsel Cilalama ve Temalar
1. `ThemeChangedSignal` dinlenerek oyunun arka plan, grid çizgisi ve UI renkleri Dark/Light mode olarak değiştirilecek.
2. Çizgiler çizilirken ucunda parlayan bir efekt (VFX) oynatılacak.
3. Bölüm tamamlandığında "Tebrikler" animasyonları ve Particle System efektleri devreye girecek.

### Faz 7: Ses ve Optimizasyon
1. Çizgi her hücre ilerlediğinde farklı bir perdede (pitch) nota sesi çalacak (Pixel Flow'un imza hissi).
2. `com.nexus.core` Object Pooling kullanılarak Grid hücreleri ve LineRenderer'lar havuzlanacak (mobil cihazlarda kasmayı önlemek için).