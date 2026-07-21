# Neon Transit — Pure Puzzle Logic (Data-Driven, Nexus Core Architecture)

**Version:** 3.0.0 (Puzzle-Only)  
**Date:** Temmuz 2026  
**Engine:** Unity 2023 LTS + URP  
**Architecture:** Nexus Core MVCS (SignalBus, CommandPool, ReactiveModel, DI)

---

## 1. Proje Özeti

**Neon Transit** — Minimalist, grid-tabanlı, renk-eşleştirme trafik bulmacası.  
**Tek Cümle:** *Orthogonal grid üzerinde renkli düğümleri kaza yapmadan bağla, viyadük/OneWord taktikleriyle kesişimleri çöz, Flow Score threshold'a ulaşarak seviyeyi tamamla.*

**Kapsam:** Sadece **puzzle gameplay** (grid, path drawing, crash/viaduct/OneWay, vehicle simulation, win condition).  
**Kapsam Dışı:** Hub/şehir ekranı, idle ekonomi, vergi/upgrade, meta progression, LiveOps, reklam/IAP, cloud save, sosyal, sezonluk içerik.

---

## 2. Nexus Core Mimarisi ile Eşleşme

| Nexus Core Bileşeni | Puzzle Kullanımı |
|---|---|
| **SignalBus** | `InputInteractionSignal`, `CrashDetectedSignal`, `PlaceViaductSignal`, `UndoSignal`, `CheckWinConditionSignal`, `GridUpdatedSignal`, `LevelCompletedSignal`, `LevelFailedSignal` |
| **CommandPool** | `ProcessInputCommand`, `PlaceViaductCommand`, `UndoCommand`, `RedoCommand`, `LoadLevelCommand`, `CheckWinConditionCommand`, `StartSimulationCommand`, `PauseSimulationCommand` |
| **IReactiveModel** | `GridModel` (cell data, paths, nodes), `GameSessionModel` (state, viaduct limit, crisis count), `LevelConfigModel` (current level data) |
| **INexusService** | `PathService` (draw/clear/backtrack), `VehicleSimulator` (ghost/solid sim, collision), `ObstacleService` (OneWay/NarrowPass/Ferry), `WinConditionService` (Flow Score), `LevelLoaderService` (data-driven load) |
| **Mediator + ViewBinder** | `GridMediator` ↔ `GridView`/`CellView`, `HUDMediator` ↔ `HUDView` (viaduct count, stars, level info), `CrisisMediator` ↔ `CrisisPanelView` |

**Context Yapısı:**
```
GameContext (Gameplay Scene)
├── ConfigContext (ScriptableObject configs)
├── Services: PathService, VehicleSimulator, ObstacleService, WinConditionService, LevelLoaderService
├── Models: GridModel, GameSessionModel, LevelConfigModel
├── Commands: ProcessInput, PlaceViaduct, Undo/Redo, LoadLevel, CheckWin, StartSimulation
├── Signals: InputInteraction, CrashDetected, PlaceViaduct, Undo/Redo, CheckWin, GridUpdated, LevelComplete/Fail
├── Mediators: GridMediator, HUDMediator, CrisisMediator
└── Views: GridView, CellView, HUDView, CrisisPanelView
```

---

## 3. Veri Yapıları (Data Structures)

### 3.1 Enum Tanımları

```csharp
// Assets/Scripts/PixelFlow/Core/Enums.cs

public enum ColorType { Blue, Red, Yellow, Green, Purple }
public enum ShapeType { Circle, Triangle, Square, Diamond, Star }  // Color-blind pairing
public enum NodeType { Home, Office, Hospital, School, Park, Mall }
public enum CellState { Empty, Node, Path, Bridge, Obstacle }
public enum ObstacleType { None, Construction, Lake, Park, OneWay, Ferry, NarrowPass }
public enum Direction { Up, Down, Left, Right }
public enum GameState { Boot, Loading, Playing, Simulating, Paused, LevelComplete, LevelFailed }
public enum WinConditionType { FlowScoreThreshold, FullGridCoverage }
public enum ViaductResolution { Reroute, OneWay, Viaduct }
public enum CrisisChoice { Undo, Viaduct }
```

### 3.2 Renk-Şekil Çift Kodlama (Color-Blind Safe)

| ColorType | Hex | ShapeType | Icon |
|---|---|---|---|
| Blue | #00D4FF | Circle | ● |
| Red | #FF3D7F | Triangle | ▲ |
| Yellow | #FFD93D | Square | ■ |
| Green | #6BCB77 | Diamond | ◆ |
| Purple | #B36BFF | Star | ★ |

> **Kural:** Her `ColorType` → sabit `ShapeType` eşleşmesi (Settings'de değiştirilebilir ama default bu mapping).

### 3.3 CellData (Grid Hücresi)

```csharp
// Assets/Scripts/PixelFlow/Models/GridModel.cs

[Serializable]
public struct CellData {
    public CellState State;
    public List<ColorType> PathColors;      // Max 2 renk (viyadük ile)
    public bool HasViaduct;
    public ColorType UnderColor;            // Alt geçen renk
    public ColorType OverColor;             // Üst geçen renk (Z-offset: -0.4f)
    public ObstacleType ObstacleType;
    public Direction OneWayDirection;       // OneWay için
    public Vector2Int GridPosition;         // x, y
}
```

### 3.4 NodeData (Düğüm Bilgisi)

```csharp
[Serializable]
public struct NodeData {
    public Vector2Int Position;
    public ColorType Color;
    public ShapeType Shape;
    public NodeType Type;       // Home/Office/Hospital/School/Park/Mall
    public bool IsSource;       // true = ev (kaynak), false = hedef
    public int PairIndex;       // Aynı renkteki çiftin indeksi (0-based)
}
```

### 3.5 LevelConfig (ScriptableObject — Data-Driven Seviye)

```csharp
// Assets/Scripts/PixelFlow/Config/LevelConfig.asset

[CreateAssetMenu(menuName = "Neon Transit/Level Config", fileName = "LevelConfig_")]
public class LevelConfig : ScriptableObject {
    [Header("Identity")]
    public int LevelId;
    public int Phase;                    // 1-4
    public string LevelName;

    [Header("Grid")]
    public Vector2Int GridSize;          // 5x5 → 10x10

    [Header("Nodes")]
    public List<NodeData> Nodes;         // Kaynak + Hedef çiftleri

    [Header("Obstacles")]
    public List<ObstacleData> Obstacles; // Position + ObstacleType + params

    [Header("OneWay Cells (Phase 2+)")]
    public List<OneWayData> OneWayCells; // Position + Direction

    [Header("Viaduct Limit")]
    public int ViaductLimit;             // 3-6 arası

    [Header("Win Condition")]
    public WinConditionType WinCondition;
    public int FlowScoreThreshold;       // Phase 1: 3-5, Phase 4: 18-30
    public bool RequireFullGridCoverage; // Phase 3+ (Level 29+)

    [Header("Stars")]
    public StarCriteria Stars = new StarCriteria {
        OneStar = "complete",
        TwoStars = "viaducts_used <= 2",
        ThreeStars = "viaducts_used == 0"
    };

    [Header("Tutorial")]
    public TutorialEvent TutorialEvent;  // None, CrashIntro, ViaductIntro, OneWayIntro, ObstacleIntro

    [Header("Meta")]
    public int EstimatedSolveTimeSec;
    public int DifficultyScore;          // Computed: (Colors×10)+(Intersections×5)+(Obstacles×3)-(ViaductLimit×4)
}

[Serializable]
public struct ObstacleData {
    public Vector2Int Position;
    public ObstacleType Type;
    public Vector2Int Size;              // Lake: 2x2/3x3, Park: 1x1/L-shape
    public Direction FerryDirection;     // Ferry için başlangıç yönü
}

[Serializable]
public struct OneWayData {
    public Vector2Int Position;
    public Direction AllowedDirection;
}

[Serializable]
public struct StarCriteria {
    public string OneStar;
    public string TwoStars;
    public string ThreeStars;
}

public enum TutorialEvent { None, CrashIntro, ViaductIntro, OneWayIntro, ObstacleIntro }
```

### 3.6 PhaseDefinition (Progresyon Eğrisi — ScriptableObject)

```csharp
// Assets/Scripts/PixelFlow/Config/PhaseDefinition.asset

[CreateAssetMenu(menuName = "Neon Transit/Phase Definition")]
public class PhaseDefinition : ScriptableObject {
    public int PhaseNumber;              // 1-4
    public int LevelRangeStart, LevelRangeEnd; // 1-12, 13-28, 29-45, 46-60
    public Vector2Int GridSizeRange;     // Min-Max
    public int MinColors, MaxColors;     // 1→2, 2→3, 3→4, 4→5
    public bool CrashMechanicEnabled;
    public bool ViaductEnabled;
    public bool OneWayEnabled;
    public bool ObstaclesEnabled;
    public int BaseViaductLimit;
    public int FlowScoreThresholdMin, FlowScoreThresholdMax;
    public bool RequireFullGridCoverage;
    public List<ObstacleType> AllowedObstacles;
    public float EstimatedTimeMin, EstimatedTimeMax;
}
```

---

## 4. Çekirdek Mekanikler (Core Gameplay Logic)

### 4.1 Grid ve Hareket

- **Grid:** 2D array `CellData[Width, Height]`, boyut `LevelConfig.GridSize`
- **Hareket:** Sadece **orthogonal** (Up/Down/Left/Right). Çapraz **yok**.
- **Koordinat:** `(0,0)` = sol-alt (bottom-left), x sağa, y yukarı.

### 4.2 Çizim ve Yol (Path Drawing)

```
Input: Touch Down → Touch Drag → Touch Up
State: Playing
```

| Aşama | İşlem | Signal/Command |
|---|---|---|
| Touch Down | Grid hücresinde `NodeData` var mı? `IsSource` kontrolü. | `InputInteractionSignal(Down, pos)` → `ProcessInputCommand` |
| Drag | Geçerli komşu hücreye (orthogonal) yol segmenti ekle. `PathColors.Add(color)`. Override izinli. | `InputInteractionSignal(Drag, pos)` → `ProcessInputCommand` |
| Touch Up | Hedef node (`!IsSource`, aynı `ColorType`) ulaşıldı mı? Bağlantı tamam. | `InputInteractionSignal(Up, pos)` → `CheckWinConditionSignal` |

**PathService API:**
```csharp
public interface IPathService {
    bool CanDrawPath(ColorType color, Vector2Int from, Vector2Int to);
    void DrawPath(ColorType color, Vector2Int from, Vector2Int to);
    void ClearPath(ColorType color);
    void BacktrackPath(ColorType color, int steps);      // Partial undo
    void BreakPath(ColorType color);                     // Full clear
    List<Vector2Int> GetPathCells(ColorType color);
}
```

**Kurallar:**
- Aynı hücreye **max 2 farklı renk** girebilir (viyadükle).
- 3. renk girişi **reddedilir** (visual feedback: hücre kırmızı pulse).
- Aynı renk aynı hücreden geçerse **override** (uyarı yok).

### 4.3 Kaza Mekaniği (Crash) — ÇEKİRDEK

**İki Aşamalı Tetikleme:**

| Aşama | GameState | Tetikleyici | Çıktı |
|---|---|---|---|
| **Soft Warning** | Playing | Çizim sırasında 2 farklı renk aynı hücrede (viyadüksüz) | `PathIntersectionWarningSignal(cellPos)`, hücrede ⚠ ikonu |
| **Hard Crash** | Simulating | 2 farklı renkli araç **aynı anda** viyadüksüz hücrede (`distance < 0.5f`) | `CrashDetectedSignal(crashData)`, `GameState.Paused`, **Crisis Panel** |

**CrashData:**
```csharp
public struct CrashData {
    public Vector2Int CellPosition;
    public ColorType ColorA, ColorB;
    public Vector2Int VehicleA_Pos, VehicleB_Pos; // Grid koordinat
    public List<Vector2Int> PathSegmentsA, PathSegmentsB; // Etkilenen segmentler
}
```

**Crisis Panel Seçenekleri:**
1. **Undo (Geri Al)** → `UndoCommand` → Kaza yapan yolun **son segmenti** silinir (partial undo). `ViaductLimit--` (ceza). `GameState.Playing`.
2. **Viaduct** → `PlaceViaductCommand` → Hücre `Bridge` olur, `HasViaduct=true`, `UnderColor/OverColor` set. `ViaductLimit--`. `GameState.Simulating`.

> **Not:** 3 ardışık kaza denemesi → `LevelFailedSignal` (Interstitial ad tetiklenebilir — puzzle core'da sadece signal).

### 4.4 Viyadük Mekaniği

```csharp
public struct ViaductData {
    public Vector2Int Position;
    public ColorType UnderColor;
    public ColorType OverColor;
    public bool IsActive;
}
```

**Kurallar:**
- `ViaductLimit` (LevelConfig'den) → her yerleştirmede `--`, undo'da `++` (iade).
- Max 2 yol/köprü (Under + Over). 3. renk **yasak**.
- Z-Offset: Over = -0.4f, Under = -0.1f.
- `PlaceViaductCommand` → `HistoryService.Record()` (undo stack'e atılır).

**Üçlü Çözüm Stratejisi (Oyuncu Seçimi):**
| Strateji | Maliyet | Ne Zaman |
|---|---|---|
| **Rota Yeniden Çiz** (Reroute) | 0 viyadük, zaman | Her zaman |
| **OneWay** (Tek Yön) | 0 viyadük, taktik | Level 20+ (Phase 2+) |
| **Viyadük** | 1 viyadük hak | Her zaman (limit varsa) |

### 4.5 OneWay (Tek Yön Yol) — Phase 2+ (Level 20+)

```csharp
// ObstacleService
bool CanEnterOneWay(Vector2Int cell, Direction approachDir, ColorType color) {
    var data = GridModel.GetCell(cell);
    if (data.ObstacleType != ObstacleType.OneWay) return true;
    return data.OneWayDirection == approachDir; // Sadece izin verilen yönden giriş
}
```

- Aynı hücreden **farklı zamanlarda** 2 renk geçebilir (viyadüksüz çözüm).
- `ProcessInputCommand` çizim sırasında yön kontrolü yapar.
- `VehicleSimulator` hareket sırasında yön ihlali → araç bekletilir (Flow Score durur).

### 4.6 Engeller (Obstacles) — Phase 3+ (Level 29+)

| Tip | Grid Etkisi | Davranış |
|---|---|---|
| Construction | Tek hücre bloke | Geçiş yok |
| Lake | 2×2 veya 3×3 blok | Geçiş yok |
| Park | 1×1 veya L-şekli | Geçiş yok |
| Ferry | 1×1, her 10s'de yön değiştirir | Yön `FerryDirection`, araç bekler |
| NarrowPass | 1×1, tek araç genişliği | Kuyruk yönetimi (aşağı bak) |

### 4.7 NarrowPass Kuyruk Mantığı

```csharp
// ObstacleService
bool CanEnterNarrowPass(Vector2Int cell, ColorType color) {
    var data = GridModel.GetCell(cell);
    if (data.ObstacleType != ObstacleType.NarrowPass) return true;
    // Boşsa veya aynı renkse girilebilir
    return data.PathColors.Count == 0 || data.PathColors.Contains(color);
}

void OnVehicleEnteredNarrowPass(Vector2Int cell, ColorType color) {
    GridModel.GetCell(cell).PathColors.Add(color); // Lock to color
}

void OnVehicleExitedNarrowPass(Vector2Int cell, ColorType color) {
    var data = GridModel.GetCell(cell);
    // Sadece aynı renk çıktıysa serbest bırak
    if (data.PathColors.Contains(color) && data.PathColors.Count == 1) {
        data.PathColors.Clear();
    }
}
```

- Farklı renkli araç girmek istiyorsa → **spawn ertelenir** (VehicleSimulator'da).
- Flow Score bu rotada **durur** (visual: kırmızı pulse kenarlık).

### 4.8 Araç Simülasyonu (VehicleSimulator)

| State | Görünüm | Çarpışma | Amaç |
|---|---|---|---|
| **Playing** (Çizim) | Hayalet (%60 opak, küp) | **AKTİF** — Anlık kaza geri bildirimi | Oyuncu hatayı **anında görsün** |
| **Simulating** (Test) | Katı, neon | Flow Score için | Threshold doğrulama |
| **Paused** (Kaza) | Durduruldu | — | Oyuncu müdahale |

**Hareket:** Catmull-Rom spline + overshoot/settle viraj animasyonu  
**Spawn:** Her kaynak noddan periyodik, renkli araç doğurur  
**Flow Score:** Hedefe ulaşan her araç = +1 puan, kaynakta yeniden doğar (sirkülasyon)

---

## 5. Kazanma Koşulları (Win Conditions)

### 5.1 Temel: Flow Score Threshold (Aktif Doğrulama)

```
Flow Score = Σ (Her renk çifti için hedefe ulaşan araç sayısı)
```

| Phase | Level | Threshold | Açıklama |
|---|---|---|---|
| 1 | 1-12 | 3-5 | Sadece sirkülasyon doğrulama |
| 2 | 13-28 | 6-10 | Kaza-kurtarma taktikleri |
| 3 | 29-45 | 12-18 | Her renk min 3-4 ulaşım |
| 4 | 46-60 | 18-30 | Verimlilik, darboğaz yönetimi |

**Kontrol (her frame Simulating state'de):**
```csharp
// WinConditionService
void CheckWinCondition() {
    if (GridModel.AllColorPairsConnected()) {
        int totalFlow = VehicleSimulator.GetTotalFlowScore();
        if (totalFlow >= LevelConfigModel.Current.FlowScoreThreshold) {
            // Grid coverage kontrolü
            if (LevelConfigModel.Current.RequireFullGridCoverage) {
                if (GridModel.IsGridFullyCovered()) {
                    SignalBus.Fire(new LevelCompletedSignal());
                }
            } else {
                SignalBus.Fire(new LevelCompletedSignal());
            }
        }
    }
}
```

### 5.2 Kademeli Grid Kaplama

| Seviye | `RequireFullGridCoverage` |
|---|---|
| 1-28 | `false` (boş hücre kalabilir) |
| 29+ | `true` (%100 grid dolu olmalı — **Perfect Flow Clear**) |

### 5.3 Yıldız Sistemi

| Yıldız | Kriter |
|---|---|
| ⭐ | Bölüm tamamla (tüm renkler kazasız bağlandı) |
| ⭐⭐ | ≤ 2 viyadük kullan |
| ⭐⭐⭐ | **0 viyadük** (Perfect Flow) |

---

## 6. Undo / Redo Sistemi

- **CommandPool** + **HistoryService** (Nexus Core)
- Sınırsız stack
- **Partial Undo** destekli: Sadece kesişim sonrası segment silinebilir
- **Viaduct İadesi:** `PlaceViaductCommand` undo edilirse `ViaductLimit++`

```csharp
// Commands
public class UndoCommand : ICommand<UndoSignal> { ... }
public class RedoCommand : ICommand<RedoSignal> { ... }
public class PlaceViaductCommand : ICommand<PlaceViaductSignal>, IResettable { 
    // HistoryService.Record(this) 
}
```

---

## 7. Signal / Command / Model Envanteri

### 7.1 Signals

| Signal | Payload | Tetikleyici | Dinleyiciler |
|---|---|---|---|
| `InputInteractionSignal` | `InteractionType, Vector2Int, ColorType?` | GridView pointer event | `ProcessInputCommand` |
| `PathIntersectionWarningSignal` | `Vector2Int cellPos` | `ProcessInputCommand` (çizimde kesişim) | `HUDMediator` (⚠ göster) |
| `CrashDetectedSignal` | `CrashData` | `VehicleSimulator` (simülasyonda kaza) | `CrisisMediator`, `GameSessionModel` |
| `PlaceViaductSignal` | `Vector2Int cellPos` | CrisisPanelView tıklama | `PlaceViaductCommand` |
| `UndoSignal` / `RedoSignal` | — | HUDView butonları | `UndoCommand` / `RedoCommand` |
| `CheckWinConditionSignal` | — | Yol tamamlandığında / her frame Simulating'de | `CheckWinConditionCommand` |
| `GridUpdatedSignal` | `Vector2Int cellPos` | Her grid değişikliğinde | `GridMediator` → `CellView.Refresh()` |
| `LevelCompletedSignal` | `LevelResult (stars, time, viaductsUsed)` | `WinConditionService` | `GameSessionModel`, `LevelCompleteMediator` |
| `LevelFailedSignal` | `FailReason` | 3 kaza denemesi / timeout | `GameSessionModel` |
| `LoadLevelSignal` | `int levelId` | Boot / Next Level / Retry | `LoadLevelCommand` |
| `StartSimulationSignal` | — | Tüm düğümler bağlandığında | `VehicleSimulator` (state→Simulating) |
| `PauseSimulationSignal` | — | Kaza / Pause butonu | `VehicleSimulator` (state→Paused) |

### 7.2 Commands

| Command | Signal | Sorumluluk |
|---|---|---|
| `ProcessInputCommand` | `InputInteractionSignal` | Çizim/undo/viyadük input işle, `PathService` çağır, `GridUpdatedSignal` tetikle |
| `PlaceViaductCommand` | `PlaceViaductSignal` | `GridModel` hücreyi Bridge yap, `HistoryService.Record()`, `ViaductLimit--` |
| `UndoCommand` / `RedoCommand` | `UndoSignal`/`RedoSignal` | `HistoryService.Undo/Redo()`, viyadük iadesi |
| `LoadLevelCommand` | `LoadLevelSignal` | `LevelLoaderService.Load(levelId)` → `GridModel` init, `GameSessionModel` reset |
| `CheckWinConditionCommand` | `CheckWinConditionSignal` | `WinConditionService.Check()` |
| `StartSimulationCommand` | `StartSimulationSignal` | `VehicleSimulator.Start()`, `GameState.Simulating` |
| `PauseSimulationCommand` | `PauseSimulationSignal` | `VehicleSimulator.Pause()`, `GameState.Paused` |

### 7.3 Models (IReactiveModel)

| Model | Observable Fields | Sorumluluk |
|---|---|---|
| `GridModel` | `CellData[,] Grid`, `List<NodeData> Nodes`, `Dictionary<ColorType, List<Vector2Int>> Paths` | Grid state, path/viaduct/obstacle query |
| `GameSessionModel` | `GameState State`, `int CurrentLevelId`, `int ViaductsRemaining`, `int CrisisAttemptCount`, `int ViaductsUsed`, `float ElapsedTime` | Session state, limits, crisis tracking |
| `LevelConfigModel` | `LevelConfig CurrentConfig` | Aktif seviye verisi (ScriptableObject ref) |

---

## 8. Data-Driven Seviye Yükleme Akışı

```
LoadLevelSignal(levelId)
    → LoadLevelCommand
        → LevelLoaderService.Load(levelId)
            → Resources.Load<LevelConfig>($"Levels/Level_{levelId}")
            → GridModel.Initialize(config.GridSize)
            → GridModel.PlaceNodes(config.Nodes)
            → GridModel.PlaceObstacles(config.Obstacles)
            → GridModel.PlaceOneWays(config.OneWayCells)
            → GameSessionModel.Reset(config.ViaductLimit)
            → LevelConfigModel.SetCurrent(config)
            → SignalBus.Fire(GridUpdatedSignal) // Full refresh
            → SignalBus.Fire(StartSimulationSignal) // Auto-start sim if all connected
```

**Klasör Yapısı:**
```
Assets/
├── Resources/
│   └── Levels/
│       ├── LevelConfig_1.asset
│       ├── LevelConfig_2.asset
│       └── ... (60 hand-crafted + procedural daily crisis)
├── Scripts/PixelFlow/
│   ├── Config/
│   │   ├── LevelConfig.cs
│   │   ├── PhaseDefinition.cs
│   │   └── ColorShapeMapping.cs
│   ├── Core/Enums.cs
│   ├── Models/
│   │   ├── GridModel.cs
│   │   ├── GameSessionModel.cs
│   │   └── LevelConfigModel.cs
│   ├── Services/
│   │   ├── PathService.cs
│   │   ├── VehicleSimulator.cs
│   │   ├── ObstacleService.cs
│   │   ├── WinConditionService.cs
│   │   └── LevelLoaderService.cs
│   ├── Commands/
│   │   ├── ProcessInputCommand.cs
│   │   ├── PlaceViaductCommand.cs
│   │   ├── UndoCommand.cs / RedoCommand.cs
│   │   ├── LoadLevelCommand.cs
│   │   ├── CheckWinConditionCommand.cs
│   │   └── StartSimulationCommand.cs
│   ├── Signals/
│   │   ├── InputInteractionSignal.cs
│   │   ├── CrashDetectedSignal.cs
│   │   ├── PlaceViaductSignal.cs
│   │   ├── CheckWinConditionSignal.cs
│   │   └── LevelCompletedSignal.cs
│   ├── Mediators/
│   │   ├── GridMediator.cs
│   │   ├── HUDMediator.cs
│   │   └── CrisisMediator.cs
│   └── Views/
│       ├── GridView.cs
│       ├── CellView.cs
│       ├── HUDView.cs
│       └── CrisisPanelView.cs
```

---

## 9. Prosedürel Üretim (Sadece Günlük Krizler)

**Solution-First Algorithm:**

```csharp
// ProceduralLevelGenerator (Editor-only / Runtime for Daily Crisis)
public LevelConfig GenerateDailyCrisis(int seed, DifficultyTier tier) {
    // 1. Geçerli çözüm üret (tüm renkler kesişmeden bağla)
    var solution = GenerateValidSolution(gridSize: 10x10, colorCount: 3-5);
    
    // 2. Çözümden yola çıkarak kasıtlı kesişimler ekle
    var withIntersections = AddControlledIntersections(solution, viaductLimit: 4-6);
    
    // 3. Engel yerleştir (Phase 3-4: lake, park, construction)
    var withObstacles = PlaceObstacles(withIntersections, tier);
    
    // 4. OneWay hücreleri ekle (viyadüksüz alternatif)
    var withOneWays = AddOneWayAlternatives(withObstacles);
    
    // 5. Zorluk skoru hesapla ve tier'a göre ayarla
    var difficulty = CalculateDifficulty(withOneWays);
    AdjustToTier(ref withOneWays, tier, difficulty);
    
    return CreateLevelConfig(withOneWays);
}
```

**Zorluk Formülü:**
```
Difficulty = (ColorCount × 10) + (IntersectionCount × 5) + (ObstacleCount × 3) - (ViaductLimit × 4)
```
- Kolay: 15-25, Orta: 26-40, Zor: 41-60

---

## 10. Test Edilecek Kritik Puzzle Senaryoları

| Senaryo | Beklenen |
|---|---|
| Aynı hücreden 2 renk geçişi (viyadüksüz) | ⚠ Playing, KAZA Simulating |
| 3. renk aynı hücreye girmeye çalışır | **Reddedilir** (MaxPathsPerBridge = 2) |
| Viyadük yerleştir → Undo | Viyadük hakkı **iade edilir** |
| OneWay hücresine ters yön | **Giriş reddedilir** |
| Dar geçit dolu (farklı renk) | Spawn **ertelenir** |
| Flow Score threshold'a ulaşır | `LevelCompletedSignal` |
| Seviye 30+, grid %100 dolu değil | **Kazanılamaz** |
| 0 viyadük kullanıp bitir | ⭐⭐⭐ |

---

## 11. Erişilebilirlik (Puzzle Core İçin)

- **Renk + Şekil çift kodlama** (varsayılan aktif)
- **Haptic Feedback** (6 desen — kaza, viyadük, bağlantı, çizim, undo, win)
- **Reduce Motion** (OS ayarı) → particle/bloom/animasyon azalt
- **Minimum touch target** 48×48 dp

---

## 12. Geliştirme Yol Haritası (Puzzle Only)

| Aşama | Süre | Teslimat |
|---|---|---|
| **Core Architecture** | 2 hafta | Nexus Context, SignalBus, CommandPool, DI, ReactiveModel kurulu |
| **Grid + Path Drawing** | 2 hafta | Orthogonal çizim, multi-path, override, visual feedback |
| **Crash + Viaduct + Undo** | 2 hafta | 2-aşamalı kaza, viyadük placement, unlimited undo/redo |
| **Vehicle Sim + Flow Score** | 2 hafta | Ghost/solid mode, Catmull-Rom, collision detection, Flow Score |
| **Win Condition + Stars** | 1 hafta | Threshold check, grid coverage, 3-star criteria |
| **Obstacles (OneWay, Ferry, NarrowPass)** | 2 hafta | Phase-gated unlock, obstacle service |
| **Data-Driven Levels** | 1 hafta | ScriptableObject LevelConfig, LevelLoader, 12 Phase-1 levels |
| **Phase 2-4 Content** | 3 hafta | 48 level hand-crafted, procedural daily crisis generator |
| **Polish + Accessibility** | 1 hafta | Haptic, color-blind, reduce motion, juice |
| **Toplam** | ~16 hafta | 60 level + daily crisis, production-ready puzzle core |

---

## 13. Ekler

### A. Renk Paleti (Puzzle Görsel)

| İsim | Hex | Kullanım |
|---|---|---|
| Obsidyen Siyah | #0B0F19 | Grid zemin |
| Elektrik Mavisi | #00D4FF | Mavi yol/araç |
| Sıcak Pembe | #FF3D7F | Kırmızı yol/araç (CB) |
| Güneş Sarısı | #FFD93D | Sarı yol/araç |
| Nane Yeşili | #6BCB77 | Yeşil yol/araç |
| Ultraviyole | #B36BFF | Mor yol/araç |
| Başarı Yeşili | #4ADE80 | Bağlantı onay |
| Kriz Kırmızısı | #EF4444 | Kaza uyarısı |

### B. Haptic Referansları

| Olay | iOS | Android |
|---|---|---|
| Çizim başlangıç | Impact Light | 10ms, 30% |
| Düğüm bağlantı | Impact Medium | 30ms, 50% |
| Kaza | Notification Warning | 100ms, 100% + 50ms pause + 50ms |
| Viyadük yerleştir | Impact Heavy | 60ms, 80% |
| Bölüm tamamlama | Notification Success | 200ms, 70% ramp |

---

**Belge Sonu — Neon Transit Puzzle Core v3.0.0**  
*Şehir/ekonomi/meta katmanları tamamen çıkarılmıştır. Sadece saf bulmaca mantığı, data-driven seviye yapısı ve Nexus Core MVCS mimarisi içerir.*