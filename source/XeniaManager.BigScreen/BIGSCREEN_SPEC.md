# Xenia Manager BigScreen — Project Spec

> **Status:** Living document. Update as the project evolves.
> **Project:** `source/XeniaManager.BigScreen/`
> **Branch:** `feature/big-screen`
> **Stack:** Avalonia 12.1.0 · .NET 10 · CommunityToolkit.Mvvm · FluentAvalonia · SDL3-CS (planned)

---

## 1. Overview & Vision

**XeniaManager.BigScreen** is a fullscreen **Xbox Series-style dashboard** ("big screen mode") for Xenia Manager. It runs as its own Avalonia desktop app and references `XeniaManager.Core` for the game library, profiles, artwork and launching.

**Vision:** a controller-driven, big-screen experience — browse an alphabetical game carousel, pick a game, and launch straight into Xenia. Browse screenshots in a gallery with a focused modal viewer. Configure dashboard visuals (background, accent, vignette) from a settings screen. All choices persist across sessions.

**Non-goals (for now):** localization (deferred), launching BigScreen *from* the main app (deferred — Quit simply exits the process for now).

---

## 2. Current State (done)

### Dashboard shell (`Views/MainWindow.axaml`)
- **Header:** avatar icon (PersonCircle), gamertag from the **Canary profile** (`ProfileManager.LoadProfiles(XeniaVersion.Canary)` + profile GPD), gamerscore as an `IconStat` (Star), wifi/battery icons (VM state, not real hardware yet), live clock (1s `DispatcherTimer`).
- **Game card row:** **max 6 recent games** (`RecentGames`, its own VM instances — independent selection from the library). Each `GameCard` shows its box art (zoom-to-fill, bottom-anchored, rounded clip); the selected card grows (200→250) and shows the title bar. A transparent `BorderOverlay` larger than the card carries **all** border strokes (inactive `CardBorder`, hover/selected accent) so nothing sits on top of the art edges.
- **Option card row:** `Library` · `Media` · `Settings` · `Quit`. Hover shows the accent border; selection is driven by `IsSelected` (controller focus), not by keyboard focus.
- **Fullscreen:** `WindowState="FullScreen"`, 1920×1080 fallback, centered column layout so header aligns with content rows.

### Overlay screens
Full-screen pages rendered over the dashboard; **Enter/click** opens, **B/Escape** closes, focus returns to the option row. Bottom hint bars use the `InputHint` control (coloured circle keycap + label).
- **Library** — horizontal carousel of all games (`LibraryCard`s: box art with a **13% top crop** (bottom-anchored, ~366px art region), title, playtime row, achievements/gamerscore row from the profile GPD). Left/Right iterates (clamped at both ends — no wrap), the row scrolls once the selection passes the middle. **Y** cycles the sort (Alphabetical → Time Played → Last Played; indicator top-right via `IconStat`). Disc stub shown when the library is empty.
- **Media** — screenshot gallery scanned from `Emulators/Xenia Canary/screenshots/**` (recursive, common image extensions), rendered as rounded 320×180 thumbs (placeholder; see roadmap).
- **Settings** — background type dropdown, primary/accent colour fields (swatch + hex + palette popup), vignette slider, background image picker.
- **Quit** — closes the app.

### Base app data sharing (`Program.cs` + `Services/BaseAppLocator.cs`)
- BigScreen **reads the base Xenia Manager's data folders** (library, games, artwork, profiles): `Program.Main` calls `AppPathResolver.SetBaseDirectory(...)` before anything resolves paths (Core change, base app unaffected).
- Resolution order: `--base-dir <path>` arg → `XeniaManager.exe` next to BigScreen (production layout) → repo sibling project with the same bin config → fall back to the BigScreen folder itself.
- `dashboard-settings.json` stays next to the BigScreen executable.

### Background system (`Services/BackgroundService.cs`)
- **5 modes** (`Models/BackgroundMode.cs`): `Image`, `Solid`, `LinearGradient`, `RadialGradient`, `Dynamic` (selected game's artwork, falls back to linear).
- Gradients are **derived from the primary colour** (mix toward black, subtle slate ramp — never near-black or white ends).
- **Vignette** overlay on image-based backgrounds only (Image mode / Dynamic with art), opacity 0–1.
- Persisted to **`dashboard-settings.json`** next to the executable (`Models/DashboardSettings.cs`).
- `ApplyResources()` pushes tokens into `Application.Resources` at load and after every change, so `DynamicResource` bindings update live. Falls back to the linear gradient when a brush can't be built (e.g. missing image).

### Theming
- `Resources/Themes/DarkGradient.axaml` — token dictionary: `CardBackground`, `CardTitleBar`, `CardBorder`, `AccentColor`, `TextPrimary/Secondary`, `SystemAccentColor*` (Color-typed variants), slider/control overrides, accent-fill family.
- `Resources/Themes/Controls.axaml` — `ControlTheme`s for the custom controls (`ColorPickerField`, `PalettePicker`).
- `Resources/BigScreenStyle.axaml` — global styles (cards, settings rows, shadows, text halos).
- `Resources/Language/en.axaml` — default-language keys (playtime) + `LocalizationHelper.Initialize(...)` in `App.axaml.cs` so Core's `PlaytimeFormatter` renders real text. Full localization remains deferred.
- FluentAvalonia theme (`FluentAvaloniaTheme`) like the main app.

### Custom controls (`Controls/`)
- **`ColorPickerField`** — swatch + hex text box; clicking the swatch opens a palette popup. Two palettes: muted slates/greys (primary) and 10 accent colours incl. white/light grey.
- **`PalettePicker`** — horizontal StackPanel of colour swatches (background card, spacing, margin); raises `SelectedColorChanged`.
- **`IconStat`** — icon + text row (`Icon` Symbol, `Stat`, `IconSize`, `FontSize`, `Spacing`, `IconRotation`). Used in the header (gamerscore), library sort indicator, and library cards.
- **`InputHint`** — keycap + label (`KeyColour`, `Icon`/`Char` glyph, `Text`): transparent circle with coloured 2px outline, glyph coloured to match, white label. Xbox-standard colours per usage (Y amber, A green, B red).
- **`LibraryCard`** — carousel card: box art (bottom-anchored, top 13% cropped, rounded clip via `RectangleGeometry`), title, playtime row, achievements/gamerscore row; `CardBorder` 2px inactive → accent on selection (outer border only).
- **`GameCard`** — dashboard tile: box art + title bar on selection, grow 200→250, `BorderOverlay` carries the border strokes above the art.
- **`OptionsCard`** — dashboard option tile.

### Settings persistence
`DashboardSettings` JSON via `BackgroundService` (`Load`/`Save`), `ColorJsonConverter` (ARGB hex) for `Color` values.

---

## 3. Architecture

```
source/XeniaManager.BigScreen/
├── App.axaml / App.axaml.cs        # Application shell, theme wiring, localization init, MainWindow + VM setup
├── Program.cs                       # Entry point; redirects base dir to the base app's folder
├── ViewLocator.cs                   # VM → View resolution
├── Views/
│   └── MainWindow.axaml(.cs)        # Dashboard + overlay layer; focus/selection/keys
├── ViewModels/
│   ├── MainWindowViewModel.cs       # Profile, clock, background, overlay state, settings, sort, RecentGames/Games
│   ├── ViewModelBase.cs
│   └── Items/
│       ├── GameCardViewModel.cs     # Core Game ref, Title, Boxart, stat strings, IsSelected, BackgroundArt
│       └── OptionsCardViewModel.cs  # Title, Icon, TargetScreen
├── Controls/
│   ├── GameCard.axaml(.cs)          # Dashboard game tile (art + BorderOverlay strokes)
│   ├── OptionsCard.axaml(.cs)       # Dashboard option tile
│   ├── LibraryCard.axaml(.cs)       # Carousel card: box art + title + stat rows (rounded art clip)
│   ├── IconStat.axaml(.cs)          # Icon + text stat row
│   ├── InputHint.axaml(.cs)         # Keycap + label hint
│   ├── LibraryOverlay.axaml(.cs)    # Library carousel + clamped scroll + empty stub
│   ├── MediaOverlay.axaml(.cs)      # Media gallery (placeholder)
│   ├── SettingsOverlay.axaml(.cs)   # Settings screen
│   ├── ColorPickerField.cs          # Swatch + hex + palette popup
│   └── PalettePicker.cs             # Swatch row
├── Services/
│   ├── BaseAppLocator.cs            # Resolves the base Xenia Manager folder (--base-dir / side-by-side / sibling)
│   ├── BackgroundService.cs         # Settings load/save, brush factory, ApplyResources
│   └── ColorJsonConverter.cs
├── Models/
│   ├── DashboardSettings.cs         # Persisted user-facing options
│   ├── BackgroundMode.cs / BackgroundModeOption.cs
│   ├── LibrarySort.cs               # Alphabetical / TimePlayed / LastPlayed
│   └── OverlayScreen.cs
└── Resources/
    ├── BigScreenStyle.axaml
    ├── Language/en.axaml            # Default-language keys for Core's PlaytimeFormatter
    ├── Themes/DarkGradient.axaml · Controls.axaml
    └── Art/                          # Sample wallpapers for testing
```

**Data flow**
- Selection: focus/click on a card → `IsSelected` → styled via `.selected` class / pseudo-class → visuals. **Dashboard (`RecentGames`) and library (`Games`) hold separate VM instances with independent selections.**
- Settings: VM property → `BackgroundService.Settings` → `Save()` + `ApplyResources()` → `Application.Resources` → `DynamicResource` bindings.
- Overlays: `OptionsCardViewModel.TargetScreen` → `MainWindowViewModel.OpenScreen()` → `CurrentScreen` → per-screen `IsVisible` on the overlay layer.
- Sorting: **Y** in the library → `MainWindowViewModel.CycleSort()` → re-orders `Games` (title asc, playtime desc, last-played desc), keeps the selection, re-scrolls.
- Base dir: `Program.Main` → `BaseAppLocator.Resolve(args)` → `AppPathResolver.SetBaseDirectory(...)` → all Core paths (library, games, artwork, profiles, logs) resolve against the base app's folder.

**Core integration points (available in `XeniaManager.Core`)**
- `GameManager.LoadLibrary()` / `GameManager.Games` — real library.
- `Game` — `Title`, `GameId`, `Playtime` (**minutes**, use `PlaytimeFormatter.Format`), `LastPlayed` (`DateTime?`), `Artwork`, `Compatibility`, `FileLocations`.
- `GameArtwork.CachedBoxart` / `CachedBackground` / `CachedIcon` — cached bitmaps.
- `AccountContent` + `GpdFile` — per-game achievement counts / gamerscore (`GpdFile.Achievements`, `GetTotalGamerscore()`).
- `Launcher.LaunchGameASync(Game, Settings, ...)` — game launch (needs Core `Settings`).
- `ProfileManager.LoadProfiles(XeniaVersion.Canary)` — profile/gamertag.

---

## 4. Design System

### Tokens (`DarkGradient.axaml`)
| Token | Purpose |
|---|---|
| `CardBackground` / `CardTitleBar` | Card surfaces |
| `CardBorder` | Reserved 3–4px borders (no layout shift on focus) |
| `AccentColor` | Selected/hover accent (runtime-configurable) |
| `TextPrimary` / `TextSecondary` | Text |
| `SystemAccentColor` + Light1–3 / Dark1–3 | FluentAvalonia accent (Color-typed, runtime variants) |
| `SliderTrackValueFill` / `SliderThumbBackground` | Slider fill/knob (accent) |
| `ControlOutlineBrush` / `TextControlBorderBrush` | ComboBox/TextBox borders (+ hover/disabled), focused = accent |
| `AccentFillColor*` / `TextOnAccentFillColor*` | Dropdown selected-item bar etc. |

### Interaction model
- **Controller = focus → `IsSelected`** drives the accent visuals (per-row independent).
- **Mouse hover** (`:pointerover`) shows the accent border.
- Reserved border thickness so focus/hover never changes element size.

---

## 5. Roadmap (next)

> Tick items off as they land. Progress lives in this list.

### 5.1 Real library + profile
- [x] Load actual games via `GameManager.LoadLibrary()`; replace the 6 fake games
- [x] Real Canary profile behaviour: gamertag + gamerscore in the header from the actual profile
- [x] Per-game achievement stats from the profile's GPDs (reuses the header's loaded `ProfileGpd` → `TitleEntry` per game)

### 5.2 Library carousel
- [x] Left-to-right **alphabetical** carousel (sortable with **Y**: Alphabetical / Time Played / Last Played)
- [ ] Selected card **centred** — *decided against*: standard left-to-right list, scrolls once the selection passes the middle, clamped at both ends (no wrap)
- [x] Card layout: box art on top (`CachedBoxart`, bottom-anchored with a 13% top crop)
- [x] Game name underneath
- [x] Total achievements under the name
- [x] Gamerscore under the name
- [x] Total time played (minutes via `PlaytimeFormatter`)
- [ ] Last played (`Game.LastPlayed`) — *removed from the card by request* (playtime row moved above achievements)

### 5.3 Media gallery
- [ ] Clean wrap panel with adequate spacing over all screenshots
- [ ] Click a screenshot → **modal focus**
- [ ] Subtle chevron arrows in the modal for navigation (visual affordance)
- [ ] B/Escape closes the modal

### 5.4 Game launch
- [ ] Launch from the carousel via `Launcher.LaunchGameASync` (needs Core `Settings`)

### 5.5 Dynamic background
- [ ] Populate `GameCardViewModel.BackgroundArt` from `CachedBackground`
- [ ] Dynamic mode works with real art (fallback to linear gradient when missing)

### 5.6 Quit behaviour toggle
- [ ] **Classic CheckBox** in Settings
- [ ] Default **off** = returns to base Xenia
- [ ] **On** = quits the application
- [ ] Tagline explaining the behaviour
- [ ] Persisted in `DashboardSettings`

### 5.7 Empty states
- [x] **No games:** placeholder with subtle disc icon + "No games found" (dashboard row **and** library screen)
- [ ] **No screenshots:** placeholder
- [x] **Fewer than 6 recent games:** dashboard row is capped at 6 via the `RecentGames` collection (fewer games = fewer cards, no empty slots)

### 5.8 SDL3 gamepad input
- [ ] Add `ppy.SDL3-CS` package (win-x64 native included)
- [ ] D-pad + A/B navigation mapped to the existing focus/selection model

### 5.9 Real controller battery / wifi
- [ ] Wire header battery icon to live controller state
- [ ] Wire header wifi icon to live state

### 5.10 Final hardening phase
- [ ] Manual walkthrough of everything; rewrites where necessary
- [ ] Maintainability sweep: deduplication, DRY, SOLID
- [ ] **Codebase compliance sweep** against root `CONTRIBUTING.md`:
  - [ ] Naming: `_camelCase` private fields, PascalCase methods/properties, Hungarian-prefixed XAML names (`Cmb`, `Txt`, `Btn`, `Tbl`, `Sp`, `Grd`, `Sv`)
  - [ ] AXAML property order (x:Name → x:DataType → grid placement → bindings → layout → style → events)
  - [ ] XML doc comments on public/internal members; sparse inline comments
  - [ ] Logger-based error handling (`Logger.Error<T>`, `Logger.LogExceptionDetails<T>`)
  - [ ] 4-space indent, braces on new lines, file-scoped namespaces, alphabetical usings
  - [ ] MVVM: keep code-behind minimal; business logic in Core

---

## 6. Build & Run

```bash
# Build the whole solution (includes BigScreen)
dotnet build "Xenia Manager.sln"

# Build just the BigScreen app
dotnet build source/XeniaManager.BigScreen/XeniaManager.BigScreen.csproj

# Run (Debug)
dotnet run --project source/XeniaManager.BigScreen/XeniaManager.BigScreen.csproj
```

- Part of `Xenia Manager.sln`; references `XeniaManager.Core`.
- **Base dir:** BigScreen resolves the base Xenia Manager's folder automatically (side-by-side deployment or repo sibling). Override with `XeniaManager.BigScreen.exe --base-dir <path>`. In the repo, Release BigScreen reads `source/XeniaManager/bin/Release/net10.0` (Debug reads Debug).
- Persisted settings land in `source/XeniaManager.BigScreen/bin/{Debug|Release}/net10.0/dashboard-settings.json`.
- Opens fullscreen; Esc/B navigates back; Alt+F4 quits.
