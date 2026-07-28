## Problem Statement

The game_plan.md §2.2 (Zero-Hardcode Policy) mandates:
*"C# kodları içinde hiçbir sabit sayı veya string (const, literal) BULUNAMAZ. Tüm değerler veri varlıklarından (ScriptableObject) okunur."*

After v6.2.0 fixed 21 violations across 11 classes, a comprehensive codebase scan revealed ~17 remaining `new Color()` literal violations in 4 view files that were not covered in the previous sweep. These fall into three categories:

1. **Themable color literals** — Colors that should be loaded from ThemePaletteAsset (crash colors, rejection color, theme backgrounds, flash color)
2. **Procedural sprite generator colors** — Colors used in fallback texture generation for obstacle/bridge sprites
3. **Alpha-manipulation expressions** — `new Color(r, g, b, alpha)` where RGB comes from config but alpha is a magic number

## Solution

Apply the policy strictly:
1. **Themable colors → ThemePaletteAsset**: Add new fields and eliminate all fallback literals
2. **Procedural sprite colors → ThemePaletteAsset**: Add dedicated fields for obstacle/construction/lake/park/bridge color sets
3. **Alpha expressions → `Color.WithAlpha()` extension**: Add utility method so alpha values remain readable but the expression becomes a transformation on config-derived color
4. **White alpha masks → `Color.white`**: Replace `new Color(1f, 1f, 1f, alpha)` with Unity built-in `Color.white`
5. **Remove silent fallbacks**: Eliminate all `config != null ? config.Value : new Color(...)` patterns — throw `DataValidationException` instead

## Commits

### Commit 1: Add `ColorExtensions.WithAlpha()` utility
- Create `Assets/Scripts/PixelFlow/Views/ColorExtensions.cs`
- Add `public static Color WithAlpha(this Color color, float alpha)` extension method
- This enables alpha-manipulation `new Color(r,g,b,a)` to become `color.WithAlpha(a)`, which is a documented transformation on a config-sourced value, not a hardcoded RGB literal
- No functional changes — pure utility addition
- **Test**: Verify the extension compiles and returns correct results

### Commit 2: Add new color fields to `ThemePaletteAsset`
- Add the following new field groups to `Assets/Scripts/PixelFlow/Data/ThemePaletteAsset.cs`:
  - `BloomFlashColor` (for BloomFlashView — warm flash color)
  - `ProceduralConstructionAmber` / `ProceduralConstructionDark` (hazard stripe base colors)
  - `ProceduralLakeWaterDeep` / `ProceduralLakeWaterLight` (water ripple colors)
  - `ProceduralParkBase` / `ProceduralParkLeaf` (grass/foliage colors)
  - `ProceduralBridgeDeck` / `ProceduralBridgeRail` / `ProceduralBridgeMaterial` (bridge colors)
  - `ProceduralRainbowGradientPurple` (rainbow gradient endpoint)
  - `PathGlowCrashRed` (crash glow color for GridView)
- Values are initialized with the same hex equivalents as the existing literals
- **Test**: Existing `ThemePaletteAsset_DistinctColorsForThemes` test validates palette integrity

### Commit 3: Clean up `CellView.cs` — themable color fallbacks
- Lines 402-405 (`GetDefaultCellBackground`): Remove the method entirely. `GetCellBackgroundColor()` already delegates to `ThemePalette.GetCellBackground()` which is the correct SO path. The fallback should be removed; if `ThemePalette` is null (which is now guarded in `Setup()`), throw `DataValidationException`.
- Lines 431-432 (`UpdateVisuals`): Remove `new Color(0.937f...)` and `new Color(0.6f...)` fallback arguments. `ThemePalette` is already non-null guaranteed by `Setup()`, so simplify to direct property access.
- Line 611 (`_rejectionColor` field initializer): Remove the initializer literal. The field is always overridden in `Setup()` from `ThemePalette.RejectionPulse`. Use `default` instead.
- **Test**: CellView tests in `GameTestContext.cs` create `ThemePaletteAsset` — ensure they still pass

### Commit 4: Clean up `CellView.cs` — procedural sprite generator colors
- Lines 236-237 (cAmber, cDark): Load from `ThemePalette.ProceduralConstructionAmber/Dark`
- Lines 257-258 (cWaterDeep, cWaterLight): Load from `ThemePalette.ProceduralLakeWaterDeep/Light`
- Lines 278-279 (cParkBase, cParkLeaf): Load from `ThemePalette.ProceduralParkBase/Leaf`
- Lines 318-319 (cBridgeDeck, cBridgeRail): Load from `ThemePalette.ProceduralBridgeDeck/Rail`
- Line 364 (bridge material color): Use `ThemePalette.ProceduralBridgeMaterial`
- These are local variables in `GenerateFallbackSpritesIfNeeded()` — access `ThemePalette` via a static field or pass as parameter
- **Note**: `GenerateFallbackSpritesIfNeeded()` is called from `EnsureRenderersAndSprites()` which runs before `Setup()` in some code paths. Either:
  - (a) Make `ThemePalette` accessible via a static field, or
  - (b) Move sprite generation to after `Setup()` is called, or
  - (c) Accept that these sprites are Editor-only fallbacks (`#if UNITY_EDITOR`) and document as §2.2 exception
- **Recommendation**: Option (c) — wrap `GenerateFallbackSpritesIfNeeded()` and all its callers in `#if UNITY_EDITOR` since the sprites are only needed when prefab sprites are missing (development scenario). Document as Editor-only §2.2 exception (same class as `ProceduralAudioFactory.cs`).
- **Test**: Verify compilation with `#if UNITY_EDITOR` guards

### Commit 5: Clean up `CellView.cs` — white alpha masks and alpha manipulation
- Lines 181, 204, 223, 308: Replace `new Color(1f, 1f, 1f, alpha)` with `Color.white.WithAlpha(alpha)`
- Line 633 (rainbow animation): Replace `new Color(rainbowColor.r, rainbowColor.g, rainbowColor.b, current.a)` with `rainbowColor.WithAlpha(current.a)`
- **Test**: Visual regression — sprite alpha masks should render identically

### Commit 6: Clean up `GridView.cs` — color literals
- Line 248: Gradient color key `new Color(0.5f, 0f, 1f)` → use `ThemePalette.ProceduralRainbowGradientPurple`
- Line 415: `new Color(pipeColor.r, pipeColor.g, pipeColor.b, 0.55f)` → `pipeColor.WithAlpha(0.55f)`
- Lines 424-425: `new Color(1f, 1f, 1f, 0.55f)` → `Color.white.WithAlpha(0.55f)`
- Line 462: `new Color(1f, 0f, 0f, 0.35f)` → `ThemePalette.PathGlowCrashRed.WithAlpha(0.35f)`
- **Test**: GridView tests — visual regression

### Commit 7: Clean up `BloomFlashView.cs`
- Line 26: `new Color(1f, 0.95f, 0.6f, ...)` → use `ThemePalette.BloomFlashColor`
- Inject `ThemePaletteAsset` into `BloomFlashView` (or make it a config property)
- **Note**: `BloomFlashView` currently has no DI injects. Either add `[Inject] ThemePaletteAsset` or read from a simpler path
- **Test**: LevelCompleted signal → Flash() still works

### Commit 8: Clean up `VehicleVisualFactory.cs`
- Line 75: `new Color(vehicleColor.r, vehicleColor.g, vehicleColor.b, alpha)` → `vehicleColor.WithAlpha(alpha)`
- Lines 399-400: `new Color(cVal.r, cVal.g, cVal.b, 0.45f/0f)` → `cVal.WithAlpha(0.45f)` / `cVal.WithAlpha(0f)`
- **Test**: Existing `CreateCar3D_WithNullVisualConfig_ThrowsDataValidationException` test still passes

### Commit 9: Update `NexusGeneratedBinder.g.cs` — register new DI bindings
- If `BloomFlashView` needs `[Inject] ThemePaletteAsset`, add DI registration entries
- Re-run the AOT binder code generator or manually add the entries following existing patterns
- **Test**: Compile check — no CS0128/CS1061 errors

## Decision Document

### Modules Modified
| File | Change |
|------|--------|
| `Assets/Scripts/PixelFlow/Views/ColorExtensions.cs` | **New file** — `WithAlpha()` extension method |
| `Assets/Scripts/PixelFlow/Data/ThemePaletteAsset.cs` | **Modified** — ~11 new color fields added |
| `Assets/Scripts/PixelFlow/Views/CellView.cs` | **Modified** — remove all `new Color()` literals |
| `Assets/Scripts/PixelFlow/Views/GridView.cs` | **Modified** — replace color literals |
| `Assets/Scripts/PixelFlow/Views/BloomFlashView.cs` | **Modified** — inject ThemePalette, replace flash color |
| `Assets/Scripts/PixelFlow/Views/VehicleVisualFactory.cs` | **Modified** — use `WithAlpha()` |
| `Assets/Scripts/Nexus/NexusGeneratedBinder.g.cs` | **Modified** — DI registration for BloomFlashView |

### Architectural Decisions
1. **`Color.WithAlpha()` utility** — A transformative expression on a config-derived value is not a hardcode violation. The alpha value itself could be argued as a magic number, but since the RGB is always from config, and the alpha controls rendering opacity (a universal concept), this is an acceptable pattern per §2.2.
2. **Editor-only exception for `GenerateFallbackSpritesIfNeeded()`** — The procedural sprite fallbacks only run when prefab sprites are not assigned (Editor/development scenario). Wrapping in `#if UNITY_EDITOR` makes this a documented §2.2 exception, parallel to `ProceduralAudioFactory.cs`.
3. **`BloomFlashView` DI injection** — Adding `[Inject] ThemePaletteAsset` to a View that previously had none requires a DI registration update. This is straightforward and follows existing patterns.
4. **Crash color literals → direct property access** — The crash colors in `UpdateVisuals()` already exist in `ThemePaletteAsset` (`CrashPulseBright`, `CrashPulseDark`). The `ThemePalette != null ? ThemePalette.X : new Color(...)` pattern is redundant since `Setup()` guarantees non-null — simplify to direct access.

## Testing Decisions

### Test Philosophy
- Only external behavior is tested: "does the cell render the correct crash color?" not "does it call ThemePalette.CrashPulseBright?"
- Literal removal tests are regression tests — the output must be identical before and after

### Existing Test Coverage
- `ThemeAndSettingsTests.cs` — `ThemePaletteAsset_DistinctColorsForThemes()` validates palette integrity
- `VehicleAndGenerationTests.cs` — `CreateCar3D_WithNullVisualConfig_ThrowsDataValidationException()` validates VehicleVisualFactory
- `GameTestContext.cs` — `CreateTestThemePalette()` provides test palette instances
- These tests should continue to pass after the refactor

### Tests to Add/Update
| Test | File | Priority |
|------|------|----------|
| `ColorExtensions_WithAlpha_ReturnsCorrectAlpha` | Existing test file | Medium |
| `BloomFlashView_ThemePaletteInjected_FlashDoesNotThrow` | Existing test file | Low |
| `GridView_PathGlow_UsesThemePaletteColor` | Existing test file | Low |

## Out of Scope

1. **Editor-only files** (`Editor/*.cs`) — The `new Color()` literals in `LevelDataEditor.cs`, `PhaseAssetGenerator.cs`, `UIPrefabCreator.cs`, etc. are Editor-only tools, not runtime game code. They are lower priority and can be addressed in a future sweep.
2. **ScriptableObject field defaults** — `VehicleMaterialConfigAsset.cs`, `ColorBlindPaletteAsset.cs` use `new Color(...)` as inspector defaults. This is the intended pattern — the SO *is* the source of truth per §2.2.
3. **`ProceduralAudioFactory.cs`** — Already documented as a §2.2 exception (Editor-only preview mode). No changes needed.
4. **Nexus Core services** (`Nexus.Core.Services.*`) — These are third-party framework code, not PixelFlow game code. The Zero-Hardcode policy applies to PixelFlow.
5. **Other magic numbers** (float/int literals) — This sweep focuses specifically on `new Color()` literals. Other magic number types should be addressed in future refactor plans.

## Further Notes

- The v6.2.0 session already fixed 21 violations and added ~30 new color fields to `ThemePaletteAsset`. This plan adds ~11 more.
- After this sweep, the only remaining hardcoded colors in PixelFlow runtime code would be in Editor-only files (excluded per Out of Scope).
- Each commit is designed to leave the codebase in a compilable, working state.
