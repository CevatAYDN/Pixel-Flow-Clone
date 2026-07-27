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

### 🔴 Hata 11: PixelFlowSetupWindow Sekme Yığını Tek Dosyada Fazla Şişti
- **Belirti**: Editör kontrol merkezi kurulum, tanılama, veri yönetimi, performans, garaj, reklam ve doğrulama akışlarını tek giriş üzerinden topluyor; yeni katkı yapan biri hangi sekmenin hangi sorumluluğu taşıdığını anlamak için birden fazla parça arasında gidip gelmek zorunda kalıyor.
- **Kök Neden**: `PixelFlowSetupWindow.cs` ana kabuğu ve `PixelFlowSetupWindow.SceneSetup.cs`, `PixelFlowSetupWindow.DataManager.cs`, `PixelFlowSetupWindow.GameAndDiagnostics.cs`, `PixelFlowSetupWindow.HybridCasualTabs.cs` parçaları tek bir editör module'unun farklı yüzleri olarak büyümüş durumda. Bu yapı çalışıyor ama interface büyük ve gezinme yükü yüksek.
- **Çözüm**: Mevcut kabuğu koruyup sekme gruplarını daha net iç module'lara ayırmak, ortak header/sidebar iskeletini tek yerde tutmak ve her grubun implementation'unu daha küçük dosyalara bölmek.
- **Neden tekrar hatırlanmalı**: Bu alan tekrar tekrar editör ve konfigürasyon değişikliklerinin merkezi oldu; geçmişte yapılan sahne kurulum ve doğrulama işleri de aynı yüzeyde toplandığı için tekrar eden iş önerilerini buradan ayıklamak gerekiyor.

### 🔴 Hata 12: PreBuildDataValidator Editör Kurulumuyla Fazla Örtüşüyor
- **Belirti**: Kurulum penceresi, build öncesi validation ve test bağlamı aynı config varlıklarını farklı yollarla kontrol ediyor.
- **Kök Neden**: `PreBuildDataValidator.cs` hem play-mode/build gate görevi görüyor hem de editör kurulum akışının bir uzantısı gibi davranıyor; `PixelFlowSetupWindow.SceneSetup.cs` içindeki otomatik asset oluşturma mantığı ile aynı veri kurallarını tekrar okuyor.
- **Çözüm**: Validation çekirdeğini ayrı tutup editör ve build tarafını ince adapter'larla beslemek.
- **Neden tekrar hatırlanmalı**: Önceki düzeltmeler config eksiklikleri ve setup akışındaki kopukluklar etrafında döndü; bu yüzey yeniden öneri üretmeye çok açık olduğu için hafızada görünür kalmalı.

### 🔴 Hata 13: GameTestContext Çok Geniş Bir Test Bağlamı Haline Geldi
- **Belirti**: EditMode testleri aynı factory üzerinden çok sayıda config oluşturuyor ve bağlam kuruyor.
- **Kök Neden**: `GameTestContext.cs` içinde config üretimi, DI wiring ve level factory işleri tek module'da birleşmiş durumda.
- **Çözüm**: Config factory'lerini ve context wiring'i ayrı küçük module'lara bölmek.
- **Neden tekrar hatırlanmalı**: Testlerde eksik config yaşanan önceki hatalar, bu yüzeyin zaten hassas olduğunu gösterdi; burada yeniden aynı setup yükünü büyütmemek gerekiyor.

### 🔴 Hata 14: Güvenli Kayıt Hattı Birden Fazla Module'a Yayılıyor
- **Belirti**: `StrictEncryptedStorageService`, `EncryptedCloudSaveAdapter`, `GridStateSerializer` ve `CloudSaveManager` kayıt davranışını farklı açılardan dokuyor; aynı kayıt kavramı birden fazla seam üzerinden okunuyor.
- **Kök Neden**: Yerel kayıt, şifreleme, cloud sync ve save/load kararları ayrı implementation'lara yayılmış durumda; `CloudSaveManager` içindeki geri dönüş değerleri de testteki sözleşmelerle aynı dili konuşmuyor.
- **Çözüm**: Kayıt kararlarını tek bir çekirdek module'da toplamak, adapter'ları sadece ortam farkı için kullanmak ve save/load sözleşmesini tek isimli bir module'dan yönetmek.
- **Neden tekrar hatırlanmalı**: Önceki kayıt hataları aynı yüzeyde tekrar etti; bu yüzden yeni çalışma açılırken aynı problem kümesini yeniden kurmamak için not düşülmeli.

### 🔴 Hata 15: Raporlar Güncel Kodu Tam Yansıtmıyor
- **Belirti**: `config_validation_report.md`, `editor_status_report.md`, `release_readiness_report.md`, `project_status_report.md` ve `development_update_report.md` dosyaları bazı alanlarda kodun şu anki durumunu fazla iyimser gösteriyor.
- **Kök Neden**: Raporlar; 3D araç modelleri, UI prefab ayrımı, reklam/IAP/cloud/backend entegrasyonları ve editör araçlarının gerçek sınırlarını aynı güncellenme seviyesinde taşımıyor.
- **Çözüm**: Bu raporlar, kodla çelişen maddeler için tekrar doğrulanmalı ve bir sonraki çalışma öncesi tek bir güncel durum kaynağına indirgenmeli.
- **Neden tekrar hatırlanmalı**: Kod incelemesinde yanlış olumlu algı yaratmamak için dokümanların son durumla senkron olması gerekiyor.

### 🔴 Hata 16: CloudSaveManager Statik Geri Dönüş Yolu Module Sınırını Bulandırıyor
- **Belirti**: `CloudSaveManager.SyncToCloudAsync(IPlayerPrefsService, ...)` statik shortcut'ı doğrudan `EncryptedCloudSaveAdapter` üreterek kayıt hattını by-pass ediyor.
- **Kök Neden**: Instance module ile adapter rolü aynı sınıfta hem runtime hem legacy çağrıları taşıyor.
- **Çözüm**: Statik shortcut'u kaldırıp yalnızca DI üzerinden gelen `ICloudSaveAdapter` ile çalışmak; legacy kullanım için ayrı bir adapter veya migration notu bırakmak.
- **Neden tekrar hatırlanmalı**: Kayıt hattı zaten dağınık; bu shortcut, tekrar aynı yüzeyde gizli seam oluşturuyor.

### 🔴 Hata 17: GameTestContext Ekonomi Konfigürasyonu Boş Kalıyor
- **Belirti**: Test bağlamında `EconomyConfigAsset` oluşturuluyor ama bazı ekonomik alanlar açık şekilde doldurulmuyordu; bu da test ile üretim davranışı arasında gereksiz fark bırakıyordu.
- **Kök Neden**: `GameTestContext.cs` içinde config üretimi ile validasyon beklentileri aynı module içinde tam hizalanmıyordu.
- **Çözüm**: Test economy config'ini `IapProducts` listesiyle minimum geçerli değerlerle doldurmak ve bunu tek fixture üreticisi altında toplamak.
- **Neden tekrar hatırlanmalı**: Testler sonradan kırılmaya açık olduğu için bu config boşluğu tekrar üretilmemeli.

### 🔴 Hata 18: PixelFlowSetupWindow.SceneSetup Başlangıç Seviye Ataması Yardımcıya Bölündü
- **Belirti**: Scene setup içindeki başlangıç seviye seçimi ana akışta yer alıyordu ve editör kurulum module'unu şişiriyordu.
- **Kök Neden**: `PixelFlowSetupWindow.SceneSetup.cs` içinde `GameBootstrapper.initialLevel` seçimi doğrudan scene setup akışına gömülüydü.
- **Çözüm**: Başlangıç seviye seçimi `EnsureInitialLevel(GameBootstrapper boot)` yardımcı metoduna taşındı.
- **Neden tekrar hatırlanmalı**: Bu helper, scene setup akışını biraz sadeleştirir ama module'ü tek başına yeterince derinleştirmez; daha büyük ayrışma gerekirse tekrar bakılmalı.

### 🔴 Hata 19: UI Ekranları Sert Kodlanmış Metin ve Kimlik Aramasına Aşırı Bağımlı
- **Belirti**: `MainMenuView`, `HUDView`, `SettingsView`, `GarageView`, `LevelSelectView`, `DailyCrisisView`, `StarPassView` ve `TutorialView` içinde çok sayıda Türkçe/İngilizce doğrudan string, sayı ve simge var; button/text referansları da çoğunlukla isim eşleşmesiyle bulunuyor.
- **Kök Neden**: UI module'ları localization service'i tam seam olarak kullanmak yerine hem gösterim metnini hem referans keşfini aynı yerde elle yönetiyor.
- **Çözüm**: Ekran metinlerini localization key'lerine taşımak, button etiketlerini semantics-based isimlendirmek ve auto-wire için ad-hoc string eşleşmelerini azaltmak.
- **Neden tekrar hatırlanmalı**: Bu yüzeyde tasarım ve localization hataları sürekli tekrar üretilebilir; ekran üreticileri de aynı kırılgan sözleşmeyi kopyalıyor.

### 🔴 Hata 20: UI Prefab Üreticisi ve Auto-Reference Aracı Aynı Kırılgan Sözleşmeyi Üretiyor
- **Belirti**: `UIPrefabCreator.cs` prefab içine doğrudan ekran metinleri ve emoji yerleştiriyor; `AutoReferenceEditor.cs` ise referansları çoğunlukla isim benzerliğiyle çözüyor.
- **Kök Neden**: UI üretim module'u ile runtime view module'u arasında ortak, tip güvenli bir isim ve localization seam'i yok.
- **Çözüm**: Prefab üretimini localization key'leriyle hizalamak, auto-reference için isim eşleşmesini azaltmak ve view referanslarını mümkün olduğunca sahne/prefab sözleşmesine sabitlemek.
- **Neden tekrar hatırlanmalı**: UI ekranlarında tekrar eden tasarım ve button kırıkları çoğunlukla bu iki üretici yüzeyden doğuyor.

### 🔴 Hata 21: UIPrefabCreator Hala Hardcoded UI Üretimi Yapıyor
- **Belirti**: Main menu, HUD ve garage prefab üretimi sırasında başlık, coin, button ve skin metinleri doğrudan string olarak yazılıyor.
- **Kök Neden**: Prefab üretici, view module'larının localization/seam sözleşmesini paylaşmıyor; üretilen UI gerçek runtime ekranlarından bağımsız davranabiliyor.
- **Çözüm**: Prefab üreticisini view sözleşmesiyle aynı kaynak metinleri kullanacak şekilde hizalamak veya bu üretimi tamamen runtime view kurulumuna devretmek.
- **Neden tekrar hatırlanmalı**: UI değişikliklerinde tekrar hardcoded metin üretmemek için bu yüzey görünür kalmalı.

### 🔴 Plan Karşılaştırması: `game_plan.md` ile Mevcut Implementasyon Arasındaki Doğrulanmış Farklar
- **Planın istediği ama kodda tam doğrulanmayan durumlar**
  - `PixelFlowSetupWindow` içinde planın tarif ettiği 12 sekmeli tek merkez yapısı var; ancak `HybridCasualTabs` içindeki `Garaj & Skin Stüdyosu`, `Reklam & Monetization`, `Pre-Build Validator` ve `Araçlar` sekmeleri planın anlattığı kadar derin değil.
  - `BuildGarageTab` skin listesini ve asset seçimini gösteriyor; fakat planın istediği editör içi canlı 3D model/ses önizlemesi burada görünmüyor.
  - `BuildAdMonetizationTab` yalnızca `GameConfig` değerlerini okuyor; placement ID, rewarded ödül oranları ve UMP/ATT ayarlarını editörden yönetme akışı yok.
  - `PreBuildDataValidator` config varlığı ve bazı alanları doğruluyor; ancak tüm ScriptableObject referanslarını ve bütün seviyeler için çözülebilirlik kontrolünü tek başına doğrulayan bir kapsama sahip olduğu bu incelemede görülmedi.
  - `PrivacyComplianceService`, `InAppReviewService` ve `LocalNotificationService` var; buna karşın Crashlytics/Sentry, gerçek push notification teslimi ve açık cloud-save senkronizasyonu doğrulanmadı.

- **Planla uyumlu olarak doğrulanan durumlar**
  - Mevcut editör kabuğu sıfırdan yeniden yazılmamış; ana `PixelFlowSetupWindow` partial yapılarla korunmuş.
  - `LevelDataEditor` içinde 3D toy theme, bouncy physics, star criteria ve mevcut grid edit araçları mevcut.
  - `GameConfig` içinde interstitial, rewarded, in-app review, daily chest ve diğer global-release ayarları için veri alanları bulunuyor.
  - `GameContextLifecycle`, `SceneSetup`, `DataManager`, `GameAndDiagnostics` ve `HybridCasualTabs` parçalarıyla setup penceresi modülerleştirilmiş.

- **Planla çelişen veya eksik kalan noktalar**
  - `Zero-Hardcode` hedefi birebir sağlanmıyor; `GameConfig` ve bazı global-release servislerinde literal default değerler var.
  - `Zero-Silent-Fallback` hedefi de tam sağlanmış değil; bazı platform servisleri plugin yoksa warning loglayıp devam ediyor.
  - `SetupScene` mevcut ve 14 config asset bağlayabiliyor; bu, planın tek merkezli kurulum beklentisine en yakın parça.
  - `GarageView` runtime skin kartlarını ve equip/buy akışını üretiyor; fakat planın editör içi canlı önizleme kısmı burada yok.

### 🔎 Güncel Gerçeklik Notu
- Bu proje çekirdek oyun döngüsü, editör araçları ve bazı global-release servislerini içeriyor; fakat plan dokümanındaki tam production-ready kapsam ve katı zero-hardcode politikası kodda tamamlanmış görünmüyor.
- Bundan sonraki geliştirmelerde önce bu dosyadaki doğrulanmış kurallar ve bu bölümdeki farklar referans alınmalı.

### ✅ Bu Turda Güçlendirilen Yüzeyler
- `PixelFlowSetupWindow.HybridCasualTabs.cs` içinde garaj sekmesi araç ve durak skin'lerini birlikte gösterir hale getirildi; reklam sekmesi placement ve reward bilgilerini açar hale geldi; araç merkezi config üretme ve katalog yenileme kısayolları kazandı.
- `PreBuildDataValidator.cs` artık duplicate level index, procedural difficulty sanity, authored level solvability, skin display name ve reklam placement ID kontrollerini de yapıyor.
- `GameConfig.cs` içine release/policy bayrakları ve notification key alanları eklendi; notification ve localization servisleri bu bayraklara bağlandı.
- `EditorDataManager.cs`, `DataManagerController.cs` ve `AssetCreator.cs` 14/14 config setini tamamlayacak şekilde genişletildi.
- `GlobalRelease` servislerinde fallback metinler ve sessiz davranışlar azaltıldı; notification akışı localization anahtarlarına dayandı.

### 🔴 Hata 24: TextMeshPro `\u2605` Font Atlas Uyarısı ve Engellerin Görsel Anlam Eksikliği
- **Belirti**: `LiberationSans SDF` fontunda `\u2605` (★) karakterinin bulunamadığına dair TMP uyarısı alınıyor ve yoldaki engeller (İnşaat, Gölet, Park, Tek Yön) kullanıcıya görsel anlam ifade etmeyen düz renkli kareler olarak görünüyordu.
- **Kök Neden**: 
  1. `LevelSelectView.cs`: Yıldız metinleri üretilirken default TMP font atlasında yer almayan `★` karakteri kullanılıyordu.
  2. `CellView.cs`: Engel hücresi görselleri prefab veya özel sprite olmadığında düz renkli geometrik şekillere düşüyordu (`game_plan.md §9.4` ihlali).
- **Çözüm**: 
  1. `LevelSelectView.cs`: `BuildStarString` içerisindeki Unicode `★` karakteri font atlası uyumlu `*` karakteri ile değiştirildi.
  2. `CellView.cs`: `_fallbackConstruction`, `_fallbackLake`, `_fallbackPark`, `_fallbackArrow` static değişken bildirimleri tamamlanarak CS0103 derleme hataları çözüldü.
  3. **Zero-Hardcode & Zero-Silent-Fallback Entegrasyonu (§2.2)**: `CellView.cs` içindeki tüm hardcoded renk ve fallback dalları temizlendi. Tüm engel paletleri, ikon ölçekleri ve renkler doğrudan `ThemePaletteAsset` ScriptableObject varlığından okunacak şekilde bağlandı. Asset bulunamadığı durumda sessizce devam etmek yerine katı `DataValidationException` fırlatılması sağlandı.
  4. 3D Araç Modeli Desteği: `VehicleSkinConfig.Prefab3D` ve `VehicleVisualFactory.CreateFromSkin` entegrasyonu belgelendi ve 3D model prefab kullanım altyapısı doğrulandı.

### 🔴 Hata 25: `LevelCatalog` İçinde `AuthoredLevel NULL` Uyarısı ve Otomatik Tamir
- **Belirti**: `PreBuildDataValidator` Play Mode öncesinde `LevelCatalog içindeki LevelIndex 3 için AuthoredLevel NULL!` uyarısı veriyor ve oyunu başlatmayı engelliyordu.
- **Kök Neden**: `LevelCatalog.asset` içerisinde Seviye #3 için *"Hazır Tasarlanmış Seviye"* bayrağı (`UseProceduralFallback = false`) açık kalmış, ancak disktaki ilgili seviye asset'i silindiği veya koptuğu için `AuthoredLevel` referansı NULL'a düşmüştü.
- **Çözüm**: 
  1. `PreBuildDataValidator.cs`: Doğrulama sırasında `AuthoredLevel == null` ve `UseProceduralFallback == false` olan girdiler tespit edildiğinde oyunu kilitletmek yerine otomatik olarak `UseProceduralFallback = true` yapılarak seviye kataloğu anında tamir edildi.
  2. `PixelFlowSetupWindow.RegenerateLevelCatalog`: Katalog yenileme fonksiyonuna kopuk asset referanslarını prosedürel üretime geçiren otomatik düzeltme mantığı eklendi.
  3. `ProceduralLevelGeneratorValidationTests.cs`: `LevelCatalog_NullAuthoredLevel_AutoRepairsToProceduralFallback` testi eklendi.

### 🔴 Hata 26: İstenmeyen Otomatik Seviye Doldurma ve Canlı Seviye Üretimi
- **Belirti**: Projede yalnızca 3 adet somut seviye asset'i (`Level1.asset`, `Level2.asset`, `Level3.asset`) bulunmasına rağmen sistem otomatik olarak 150 seviyelik katalog dolduruyor ve 3. seviye bittiğinde kendiliğinden Seviye 4 üretiyordu.
- **Kök Neden**: 
  1. `PixelFlowSetupWindow.RegenerateLevelCatalog`: Katalog yenilenirken 150 sayısına ulaşana kadar otomatik olarak `UseProceduralFallback = true` bayraklı sahte katalog girdileri ekliyordu.
  2. `LevelProgressionService.GetOrGenerateLevel`: Katalogda ve diskte bulunmayan seviyeler için oyun anında otomatik prosedürel jeneratörü çağırıyordu.
- **Çözüm**: 
  1. `PixelFlowSetupWindow.RegenerateLevelCatalog`: Otomatik 150 sahte seviye doldurma döngüsü kaldırıldı. Katalog **yalnızca diskte fiziksel olarak var olan** `LevelData` dosyalarını indeksleyecek şekilde kısıtlandı.
  2. `LevelProgressionService.cs`: Katalogda ve diskte yer almayan seviye indeksleri için oyun anında canlı seviye üretilmesi engellendi (`null` dönerek seviye paketinin bittiğini bildirir).
  3. `ProceduralLevelGeneratorValidationTests.cs`: `LevelProgressionService_UnindexedLevel_DoesNotAutoGenerateProceduralLevel` unit testi ile doğrulandı.

### 🔴 Hata 27: Editör Penceresinde `MissingReferenceException: initialLevel` Hatası
- **Belirti**: `PixelFlowSetupWindow` açılırken veya `Oyun Kontrol` sekmesine geçilirken `MissingReferenceException: The variable initialLevel of GameBootstrapper doesn't exist anymore.` hatası alınıyordu.
- **Kök Neden**: Unity'de silinen/kopan bir `UnityEngine.Object` (varsayılan silinmiş `LevelData` referansı) üzerinde C# null-conditional (`boot.initialLevel?.name`) operatörü kullanıldığında C# referansı null görmediği için Unity'nin aşırı yüklenmiş `== null` operatörünü atlayıp `.name` özelliğine erişmeye çalışıyor ve `MissingReferenceException` fırlatıyordu.
- **Çözüm**: `PixelFlowSetupWindow.GameAndDiagnostics.cs` içerisinde `?.name` kullanımı kaldırıldı; açık Unity `operator== null` kontrolü (`bool hasLevel = boot.initialLevel != null;`) ile güvenli hale getirildi.

### 🔴 Hata 34: HUD Viyadük, Temizle, Gökkuşağı ve İleri Al Butonlarının İşlevsizliği
- **Belirti**: Ekranın alt kısmında yer alan VİYADÜK, TEMİZLE, GÖKKUŞAĞI ve İLERİ AL butonlarına basıldığında hiçbir tepki alınmıyor, Viyadük yerleştirilemiyordu.
- **Kök Neden**: `HUDView.cs` ve `HUDMediator.cs` dosyalarında bu 4 buton için `Button` alanları, olay delegeleri (`event Action`) ve tıklama dinleyicileri (`onClick.AddListener`) tanımlanmamıştı/yorum satırına alınmıştı.
- **Çözüm**: 
  1. `HUDView.cs`: `_viaductButton`, `_rainbowRoadButton`, `_clearJamButton`, `_redoButton` alanları, olay delegeleri ve `AutoWireUIReferences` / `BindHUDButtons` bağlamları eklendi.
  2. `HUDMediator.cs`: **VİYADÜK** butonuna basıldığında ızgaradaki kesişme noktası taranarak anında 3D Viyadük köprüsü yerleştirilmesi sağlandı; kesişim yoksa uyarı verildi. **TEMİZLE** butonuna basıldığında tüm çizili yollar temizlendi. **GÖKKUŞAĞI** ve **İLERİ AL** butonları ilgili sinyallere bağlandı.

### 🔴 Hata 31: Çarpışma (Crash) Sonrasında Simülasyonun Durmaması ve Araçların Kilitlenmesi
- **Belirti**: Çarpışma anında Toast uyarısı (`"Kaza! Araçlar çarpıştı!"`) ve zıplama animasyonu çalışmasına rağmen simülasyon durmuyor, araçlar aynı hücrede takılarak her kare sürekli kaza tetikliyordu.
- **Kök Neden**: `VehicleSimulator.cs` içindeki `TriggerCrash` metodu `_isSimulating` bayrağını `false` yapmıyordu ve araçları başlangıç pozisyonlarına çekmiyordu.
- **Çözüm**: `TriggerCrash` metodu güncellenerek çarpışma anında `_isSimulating = false;` ile simülasyon durduruldu, `ResetVehiclePositionsToStart()` ile araçlar başlangıç node'larına çekildi. Oyuncu cezasız biçimde 1-tap Undo yapabilir veya yolunu yeniden çizebilir hale getirildi (`game_plan.md §1.2 & §8.1`).

### 🔴 Hata 28: `VehicleVisualFactory` İçinde Hardcoded Fallback Kullanımı (§2.2 İhlali)
- **Belirti**: `VehicleVisualConfigAsset` yüklenemediğinde veya null olduğunda `VehicleVisualFactory` sessizce C# içi hardcoded varsayılan boyut struct'larına düşüyordu (`game_plan.md §2.2 Zero-Silent-Fallback` ihlali).
- **Kök Neden**: `CreateCar3D` me `CreateTrain3D` metotlarında `_visualConfig != null ? ... : CreateDefaultCarConfig()` ternary kontrolü kullanılıyordu.
- **Çözüm**: `_visualConfig == null` olduğunda sessizce devam etmek yerine katı `DataValidationException` fırlatılması sağlandı. `VehicleAndGenerationTests.cs` içerisine `PixelFlow.Data.DataValidationException` namespace'i ile `CreateCar3D_WithNullVisualConfig_ThrowsDataValidationException` unit testi eklendi ve tam yeşillendi.

### 🔴 Hata 38: HUD Viyadük Butonunun Hedef Hücre Seçim Mantığının İyileştirilmesi
- **Belirti**: Oyuncu HUD üzerindeki VİYADÜK butonuna bastığında viyadük köprüsü rastgele veya alakasız bir hücreye koyuluyordu.
- **Kök Neden**: `HUDMediator.HandleViaductClicked` metodu `cell.PathColorCount >= 2` şartını arıyordu. Ancak kesişim sürüklemesi engellendiği için bu koşul hiçbir zaman sağlanmıyor ve kod eski bayat `LastCrashPosition` koordinatına düşüyordu.
- **Çözüm**: 
  1. `HUDMediator.cs`: Hedef hücre seçimi önceliklendirildi: (1) Aktif kaza hücresi (`LastCrashPosition`), (2) Oyuncunun dokunduğu/çizdiği son yol hücresi (`LastPosition`), (3) Izgarada henüz viyadüğü olmayan çizili ilk yol hücresi.
  2. `unityMCP` üzerinden tüm **351/351 test başarıyla geçti.**

---

## 3.1. Domain-Scoped Locality (Sinyal ve Komut Klasörleşmesi)
- **Problem**: Oyunu ve altyapıyı ilk kez inceleyen yeni bir geliştirici, Düz (Flat) klasör yapısında sinyaller ile komutlar arasındaki ilişkiyi kurarken "Indirection / Dolaylılık" nedeniyle kodda kayboluyordu.
- **Çözüm**: 
  1. `Signals/` ve `Commands/` dizinleri 6 ana etki alanına (Domain) bölündü (`Gameplay`, `Level`, `Meta`, `Settings`, `History`, `Hints`). Tüm `.cs` ve `.meta` dosyaları GUID korumasıyla ilgili klasörlere taşındı.
  2. `GameContextLifecycle.cs` içindeki sinyal-komut bağlamları (`BindSignal`) etki alanlarına göre kategorize edilip belgelendi.
- **Sonuç**: Namespace veya oyun mantığında sıfır kırılma ile yerellik (locality) ve okunabilirlik maksimum seviyeye çıkarıldı.

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

- **Paket**: `mattpocock/skills` + `Game Studio Execution Team (Enriched)`.
- **Yüklendiği Konum**: Proje içi `.agents/skills/`.
- **Geliştirilmiş Oyun Stüdyosu Ajan Ekibi**:
  - `/game-studio`: Principal Director (Fable Method döngüsü, Fable Judge doğrulama kapısı & Çift Eksenli Code Review).
  - `/game-lead-architect`: Sistem Mimarisi (Domain-Scoped Locality, Derin Modül Tasarımı, 14/14 Config Vault & Şifreli Kayıt).
  - `/game-level-designer`: Seviye Tasarımcısı (TDD Seviye Doğrulaması, DFS/IDA* Solver & Softlock Önleyici Çok Adımlı Jeneratör).
  - `/game-ui-artist`: UI/UX Sanatçısı (AAA Mobil UI Tasarım Sistemi, HSL Paletleri, ButtonJuice, ColorBlind Desteği & Responsive Layout).
  - `/game-tech-artist`: Teknik Sanatçı (Zero-GC MaterialPropertyBlock, Harici 3D Prefab Entegrasyonu & 60 FPS Render Bütçesi).
  - `/game-qa-engineer`: QA & Doğrulama Mühendisi (Diagnosing Bugs Kök Neden Döngüsü, PreBuild Validator & NUnit Test Paketi).
- **Diğer Mühendislik Yetenekleri**: `/code-review`, `/codebase-design`, `/diagnosing-bugs`, `/domain-modeling`, `/implement`, `/tdd`, `/fable-method`, `/fable-loop`, `/fable-judge`, `/obsidian-vault`, `/grill-me`, `/qa`, `/research`.

---

> **Son Güncelleme Tarihi:** 27 Temmuz 2026  
> **Sürüm:** v6.0.0 Prodüksiyon Hazır Hafıza Dökümanı (%100 Spec Compliant)
