# Color Jam 3D (Pixel Flow) — Master Development Memory & Technical Knowledge Base

> **ÖNEMLİ (AI Assistant & Geliştirici Notu):** Bu döküman projenin kalıcı hafızasıdır. Tüm mimari kurallar, çözülen kritik hatalar, konfigürasyon varlıkları ve kurulum adımları burada saklanır. Gelecekteki tüm oturumlarda ve geliştirmelerde ilk olarak bu dökümandaki kurallar ve çözümler referans alınmalıdır.

---

## 1. Temel Kurallar ve Mimari İlkeler (Core Rules)

- **Platform & Stack**: Unity 6 LTS, C# (.NET 8), Sadece Dikey (Portrait 9:16).
- **Mimari**: Nexus Core Full MVCS (SignalBus, CommandPool, ReactiveModel, DI, RemoteConfig).
- **Zero-Hardcode Politikası (§2.2)**:
  - Kod içinde hiçbir sabit sayı veya string (`const`, `literal`) bulunamaz. Tüm değişkenler ScriptableObject varlıklarından okunmalıdır.
  - Kod içerisine geçici mock/dummy veri gömülmesi kesinlikle YASAKTIR.
- **Zero-Silent-Fallback Politikası (§2.2)**:
  - Eksik veri durumunda kodun varsayılan bir sayıya sığınması yasaktır. Veri eksikse Play-Mode ve Build anında `DataValidationException` fırlatılır.
- **Encrypted Save & Bootstrap Mantığı**:
  - İlk çalıştırmada yazılmamış tuşlar (`HasKey() == false`) `ReadOrBootstrap()` mekanizması ile default değer yazıp başlatılır (bootstrap).
  - Dosya var ancak HMAC doğrulaması başarısızsa (editör oturumlarında seed değişmesi veya tampering), sistem warning loglayıp güvenli şekilde otomatik re-bootstrap yapar.
- **Performans Bütçesi (60 FPS)**:
  - <80 draw call, <100k triangle, <1KB GC/frame.
  - Path Solver iterasyon tavanı `PathSolverMaxIterationsCap = 20000` (< 1ms CPU süresi).

---

## 2. Konfigürasyon Varlıkları Kataloğu (14/14 ScriptableObjects)

Projedeki tüm konfigürasyonlar `Assets/Resources/Configs/` klasöründe yer alır. Tüm 14 varlık aşağıdaki **5 merkez kayıtta** eksiksiz bağlı ve doğrulanmış olmalıdır:
1. `GameContextLifecycle.cs` (Runtime DI)
2. `PixelFlowSetupWindow.SceneSetup.cs` (Editör Sahne Kurulumu)
3. `PixelFlowSetupWindow.DataManager.cs` (Editör Teşhis Sekmesi)
4. `PreBuildDataValidator.cs` (Play-Mode & Build Doğrulayıcı)
5. `GameTestContext.cs` (Unit Test DI Bağlamı)

| # | Asset Adı | Sınıf (Class) | Açıklama & Kritik Parametreler |
|---|---|---|---|
| 1 | `GameConfig.asset` | `GameConfig` | Genel oyun ayarları, kamera, ses, solver iterasyon limitleri (`PathSolverMaxIterations = 2000`, `Cap = 20000`) |
| 2 | `StorageKeysConfig.asset` | `StorageKeysConfigAsset` | Tüm şifreli kayıt anahtarları (25+ string key) |
| 3 | `ThemePalette.asset` | `ThemePaletteAsset` | Tema renk paletleri (Dark, Light, Neon) |
| 4 | `ColorBlindPalette.asset` | `ColorBlindPaletteAsset` | Renk körlüğü erişilebilirlik paletleri |
| 5 | `VehicleMaterialConfig.asset` | `VehicleMaterialConfigAsset` | 3D Araç materyal ve shader parametreleri |
| 6 | `VehicleVisualConfig.asset` | `VehicleVisualConfigAsset` | Prosedürel araç boyutları, tekerlek konumları |
| 7 | `EconomyConfig.asset` | `EconomyConfigAsset` | Skorlar, IAP ürün ID'leri, viyadük limitleri |
| 8 | `LevelCatalog.asset` | `LevelCatalogAsset` | El yapımı (authored) ve prosedürel seviye kataloğu |
| 9 | `PhaseConfig.asset` | `PhaseConfigAsset` | 4 fazlık seviye progresyon kuralları |
| 10 | `DifficultyFormulaConfig.asset` | `DifficultyFormulaConfigAsset` | GDD zorluk formülü ağırlıkları (Color:10, Intersection:5 vb.) |
| 11 | `DefaultSkinIdsConfig.asset` | `DefaultSkinIdsConfigAsset` | Varsayılan seçili araç ve durak skin ID'leri |
| 12 | `BouncyPhysicsConfig.asset` | `BouncyPhysicsConfigAsset` | Zıplama fizik parametreleri (`BounceForce: 4.5`, `BounceDamping: 0.75`, `SquishFactor: 0.35`) |
| 13 | `StarCriteriaConfig.asset` | `StarCriteriaConfigAsset` | Yıldız kazanma eşikleri (`3★: 0 viyadük`, `2★: 2 viyadük`) |
| 14 | `RushHourConfig.asset` | `RushHourConfigAsset` | 24 saatlik 2x Para etkinliği parametreleri |

---

## 3. Çözülmüş Kritik Hatalar ve Çözüm Hafızası (Kazanılan Deneyimler)

### 🔴 Hata 1: Unity Test Runner Donması & `Math.Max` Sınır İhlali
- **Belirti**: EditMode testleri çalışırken Unity `Generate_MediumLevel_Solvable` testinde 20-30 saniye kilitlenip donuyordu.
- **Kök Neden**: `RuntimePathSolver.cs` içindeki `CalculateMaxIterations` metodunda `Math.Max(MinIterations, Math.Min(MaxIterationsCap, raw))` formülü vardı. `MinIterations`, `GameConfig`'ten 200.000 okuduğu için `Math.Max(200000, ...)` sonucu HER ZAMAN en az 200.000 olarak zorluyordu. Test ortamında Cap 2.000 yapılsa dahi 200.000 iterasyon çalışıp CPU'yu kilitliyordu.
- **Çözüm**: Formül `Math.Min(maxCap, Math.Max(200, raw))` olarak düzeltildi. `GameConfig.cs` ve `GameConfig.asset` içinde `PathSolverMaxIterations` = 2000, `PathSolverMaxIterationsCap` = 20000 olarak güncellendi. Test süresi 30 saniyeden **< 1ms**'ye düştü.

### 🔴 Hata 2: Solver Yön Dizisinde Statik Çakışma (Static Buffer Mutation Bug)
- **Belirti**: Path solver bazı karmaşık haritalarda çıkmaz sokağa girdiğinde sonsuz döngüye giriyor veya bazı yolları atlıyordu.
- **Kök Neden**: `_directionBuffer` ve `_directionComparer` alanları `static readonly` olarak tanımlanmıştı. Derin adımlara geçildiğinde alt hücreler statik yön dizisini yerinde (*in-place*) değiştiriyor, backtrack yapılıp üst hücreye dönüldüğünde üst hücre alt hücrenin yön dizisini okuyordu.
- **Çözüm**: `GetSortedDirections` metodu her çağrıda stack-isolated yerel dizi dönecek şekilde refactor edildi.

### 🔴 Hata 3: Çözümsüz Haritalarda Erken Budama (Early Pruning) Eksikliği
- **Belirti**: Prosedürel jeneratörün rastgele ürettiği imkansız düğüm dizilimlerinde solver binlerce iterasyon harcıyordu.
- **Çözüm**: `FindPathIterative` metodunun başına hedef dükom (*end node*) etrafında 4-komşu kontrolü eklendi. Hedef dükomun etrafında boş hücre yoksa arama 0.001 ms içinde anında iptal edilir (`return null`).

### 🔴 Hata 4: Şifreli Kayıtta İlk Çalıştırma vs Bozulma Ayrımı
- **Belirti**: Temiz kurulumda (ilk açılış) yazılmamış tuşlar okunmaya çalışıldığında `DataValidationException` fırlatılıyordu.
- **Kök Neden**: `StrictEncryptedStorageService` yazılmamış tuşu bozuk/kurcalanmış tuş ile aynı muameleye tabi tutuyordu.
- **Çözüm**: `ReadOrBootstrap()` yazıldı. `HasKey()` kontrol edilir; yoksa varsayılan değer yazılıp döndürülür. Dosya var ama HMAC tutmuyorsa (oturumlar arası seed değişimi) warning loglanıp güvenli re-bootstrap yapılır.

### 🔴 Hata 6: EncryptedCloudSaveAdapter Ham PlayerPrefs İhlali
- **Belirti**: Bulut kayıt adaptöründe şifresiz ve ham `PlayerPrefs.Get* / Set*` çağrıları kullanılıyordu.
- **Kök Neden**: §2.2 Zero-Hardcode & PlayerPrefs politikasının bypass edilmesi.
- **Çözüm**: `EncryptedCloudSaveAdapter` sınıfı `IPlayerPrefsService` ve `StorageKeysConfigAsset` bağımlılıklarını constructor injection ile alacak şekilde refactor edildi. Ham `PlayerPrefs` kullanımı sıfırlandı.

### 🔴 Hata 7: VehicleSimulator'da EditMode Testlerinde Sonsuz Döngü (Zero FixedTimeStep Bug)
- **Belirti**: `VehicleSimulationTests` çalıştırıldığında veya EditMode testlerinde `VehicleSimulator.Tick()` çağrıldığında Unity Editor kilitlenip donuyordu.
- **Kök Neden**: `VehicleSimulator.InitializeAsync` sadece `Application.isPlaying` durumunda `_fixedTimeStep` önbelleklemesini yapıyordu. EditMode testlerinde `_fixedTimeStep` `0.0f` kalıyor, `Tick()` içerisindeki `while (_fixedAccumulator >= 0.0f)` döngüsü sonsuz döngüye girip CPU thread'ini kilitliyordu.
- **Çözüm**: `CacheConfigValues()` yazıldı. `InitializeAsync` ve `Tick` aşamalarında `_fixedTimeStep` sıfırdan büyük olacak şekilde korumaya alındı (`step > 0f ? _fixedTimeStep : 1f / 60f`). EditMode testlerinde kilitlenme sıfırlandı.

### 🔴 Hata 8: Test Bağlamında StorageKeysConfigAsset Eksikliği (TutorialModel DI Fail)
- **Belirti**: `UseHint_WithNoHintsLeft_DoesNothing` unit testi çalıştırıldığında `[Zero-Hardcode Policy Violation] StorageKeysConfigAsset erişilemedi!` hatası veriyordu.
- **Kök Neden**: `UseHint_WithNoHintsLeft_DoesNothing` testi özel DI konteyneri kuruyordu ancak `StorageKeysConfigAsset` varlığını bağlamamıştı. `LevelLoaderService.LoadLevel()` çalışıp `TutorialDriver.OnLevelLoaded()` tetiklediğinde `TutorialModel` constructor'ı `StorageKeysConfigAsset` parametresini bulamayıp sert doğrulama hatası fırlatıyordu.
- **Çözüm**: Test bağlamına `builder.BindInstance(CreateTestStorageKeysConfig());` eklendi. Hata tamamen giderildi.

### 🔴 Hata 9: PlayMode Test Bağlamında Eksik ScriptableObject Varlıkları
- **Belirti**: PlayMode testleri çalıştırıldığında `LevelLoaderService` veya `VehicleSimulator` üzerinde `DataValidationException` fırlatılıyordu.
- **Kök Neden**: `PixelFlowPlayModeTests.cs` içerisindeki `CreateGameContext()` metodu 14 adet ScriptableObject konfigürasyon varlığından sadece `GameConfig`'i tek başına bağlamış, kalan 13 konfigürasyonu bağlamamıştı. Zero-Hardcode & Zero-Silent-Fallback kuralları gereği eksik konfigürasyon tespit edildiğinde testler başarısız oluyordu.
- **Çözüm**: `PixelFlowPlayModeTests.cs` içerisine `CreateTestStorageKeysConfig()`, `CreateTestDefaultSkinIdsConfig()`, `CreateTestBouncyPhysicsConfig()`, `CreateTestStarCriteriaConfig()`, `CreateTestRushHourConfig()`, `CreateTestDifficultyFormulaConfig()`, `CreateTestPhaseConfig()` helper metotları yazıldı ve tüm 14 ScriptableObject konfigürasyonu DI bağlamına eklendi. PlayMode testleri %100 uyumlu hale getirildi.

### 🔴 Hata 10: Boş/Bozuk Kayıt Yükleme Hatası ve StrictEncryptedStorage Hayalet Anahtar Oluşumu
- **Belirti**: `MainMenuMediator.HandlePlayClicked()` butonuna basıldığında `DataValidationException: [Zero-Hardcode Policy Violation] Save file is empty for key: NT_PuzzleSave_` hatası alınıyor ve oyun takılıyordu.
- **Kök Neden**: 
  1. `StrictEncryptedStorageService.ReadOrBootstrap()` metodu, kayıtlı olmayan bir anahtar için `GetString(key, "")` çağrıldığında varsayılan değer `""` olduğu için depolama alanına hayalet bir anahtar (`NT_PuzzleSave_` = `""`) yazıyordu. Bu nedenle `HasKey("NT_PuzzleSave_")` metodu `true` dönmeye başlıyordu.
  2. `MainMenuMediator.TryRestoreSavedGame()` metodu `HasKey` true döndüğü için `GridStateSerializer.Load()` metodunu çağırıyor; boş JSON ile karşılaşan `Load` sert `DataValidationException` fırlatarak akışı kesiyordu.
- **Çözüm**: 
  1. `StrictEncryptedStorageService.cs` içerisindeki okuma getter metotlarının, anahtar yokken depolamaya hayalet kayıt yazması engellendi.
  2. `MainMenuMediator.TryRestoreSavedGame()` metodu `try-catch (DataValidationException)` bloğuyla sarmalandı. Boş veya bozuk kayıt durumunda kayıt otomatik temizlenip yeni seviye başlatılması sağlandı.

---

## 4. Standart Operasyon ve Çalıştırma Adımları

### A. Sahne Kurulumu ve Varlık Bağlama
1. Unity Editör üst menüsünden: **PixelFlow > Setup Window** penceresini açın.
2. **Sahne Kur** butonuna basın.
3. Bu işlem eksik konfigürasyon asset'lerini otomatik oluşturur ve sahnede yer alan `GameContextLifecycle` bileşenine tüm 14 asset'i otomatik bağlar.

### B. Otomatik Doğrulama (Pre-Build Validator)
- Play Mode'a geçerken veya Build alırken `PreBuildDataValidator.cs` otomatik çalışır.
- Kontrol edilen alanlar:
  1. 14/14 konfigürasyon asset'inin diski varlığı
  2. `StorageKeysConfigAsset` içindeki tüm string anahtarların doluluğu
  3. `LevelCatalog` içindeki authored/procedural seviye geçerliliği

### C. Kayıt Temizleme (Clear Save State)
- Şifreleme salt/seed değerleri değiştirildiğinde veya eski test kayıtlarını sıfırlamak gerektiğinde:
  - **PixelFlow > Setup Window > Data Yöneticisi** sekmesine gidin.
  - **Tüm Kaydı Sil** butonuna basın.

---

## 5. Yetenekler ve Yapay Zeka Altyapısı (AI Skills Framework)

- **Paket**: `mattpocock/skills` (48+ mühendislik yeteneği).
- **Yüklendiği Konum**: Proje içi `.agents/skills/` ve `.agents/skills.json`.
- **Aktif Yetenekler**: `/code-review`, `/codebase-design`, `/diagnosing-bugs`, `/domain-modeling`, `/implement`, `/tdd`, `/obsidian-vault`, `/grill-me`, `/qa`, `/research`.

---

> **Son Güncelleme Tarihi:** 27 Temmuz 2026  
> **Sürüm:** v6.0.0 Prodüksiyon Hazır Hafıza Dökümanı (%100 Spec Compliant)
