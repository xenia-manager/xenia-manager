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
- **Header:** avatar icon (PersonCircle), gamertag + gamerscore from the **Canary profile** (`ProfileManager.LoadProfiles(XeniaVersion.Canary)` + profile GPD), wifi/battery icons (VM state, not real hardware yet), live clock (1s `DispatcherTimer`).
- **Game card row:** 6 slots bound to `Games`. Each `GameCard` shows a title bar when selected; the selected card grows (200→250) with an accent border.
- **Option card row:** `Library` · `Media` · `Settings` · `Quit`. Hover shows the accent border; selection is driven by `IsSelected` (controller focus), not by keyboard focus.
- **Fullscreen:** `WindowState="FullScreen"`, 1920×1080 fallback, centered column layout so header aligns with content rows.

### Overlay screens
Full-screen pages rendered over the dashboard; **Enter/click** opens, **B/Escape** closes, focus returns to the option row.
- **Library** — scrollable list of the fake games (placeholder; see roadmap).
- **Media** — screenshot gallery scanned from `Emulators/Xenia Canary/screenshots/**` (recursive, common image extensions), rendered as rounded 320×180 thumbs (placeholder; see roadmap).
- **Settings** — background type dropdown, primary/accent colour fields (swatch + hex + palette popup), vignette slider, background image picker.
- **Quit** — closes the app.

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
- FluentAvalonia theme (`FluentAvaloniaTheme`) like the main app.

### Custom controls (`Controls/`)
- **`ColorPickerField`** — swatch + hex text box; clicking the swatch opens a palette popup. Two palettes: muted slates/greys (primary) and 10 accent colours incl. white/light grey.
- **`PalettePicker`** — horizontal StackPanel of colour swatches (background card, spacing, margin); raises `SelectedColorChanged`.
- **`GameCard`**, **`OptionsCard`** — dashboard tiles.

### Settings persistence
`DashboardSettings` JSON via `BackgroundService` (`Load`/`Save`), `ColorJsonConverter` (ARGB hex) for `Color` values.

---

## 3. Architecture

```
source/XeniaManager.BigScreen/
├── App.axaml / App.axaml.cs        # Application shell, theme wiring, MainWindow + VM setup
├── Program.cs                       # Entry point (classic desktop lifetime)
├── ViewLocator.cs                   # VM → View resolution
├── Views/
│   └── MainWindow.axaml(.cs)        # Dashboard + overlay layer; focus/selection/keys
├── ViewModels/
│   ├── MainWindowViewModel.cs       # Profile, clock, background, overlay state, settings
│   ├── ViewModelBase.cs
│   └── Items/
│       ├── GameCardViewModel.cs     # Title, IsSelected, BackgroundArt (dynamic bg slot)
│       └── OptionsCardViewModel.cs  # Title, Icon, TargetScreen
├── Controls/
│   ├── GameCard.axaml(.cs)          # Dashboard game tile
│   ├── OptionsCard.axaml(.cs)       # Dashboard option tile
│   ├── LibraryOverlay.axaml(.cs)    # Library screen (placeholder)
│   ├── MediaOverlay.axaml(.cs)      # Media gallery (placeholder)
│   ├── SettingsOverlay.axaml(.cs)   # Settings screen
│   ├── ColorPickerField.cs          # Swatch + hex + palette popup
│   └── PalettePicker.cs             # Swatch row
├── Services/
│   ├── BackgroundService.cs         # Settings load/save, brush factory, ApplyResources
│   └── ColorJsonConverter.cs
├── Models/
│   ├── DashboardSettings.cs         # Persisted user-facing options
│   ├── BackgroundMode.cs / BackgroundModeOption.cs
│   └── OverlayScreen.cs
└── Resources/
    ├── BigScreenStyle.axaml
    ├── Themes/DarkGradient.axaml · Controls.axaml
    └── Art/                          # Sample wallpapers for testing
```

**Data flow**
- Selection: focus/click on a card → `IsSelected` → styled via `.selected` class / pseudo-class → visuals.
- Settings: VM property → `BackgroundService.Settings` → `Save()` + `ApplyResources()` → `Application.Resources` → `DynamicResource` bindings.
- Overlays: `OptionsCardViewModel.TargetScreen` → `MainWindowViewModel.OpenScreen()` → `CurrentScreen` → per-screen `IsVisible` on the overlay layer.

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
- [ ] Load actual games via `GameManager.LoadLibrary()`; replace the 6 fake games
- [ ] Real Canary profile behaviour: gamertag + gamerscore in the header from the actual profile
- [ ] Per-game achievement stats from the profile's GPDs

### 5.2 Library carousel
- [ ] Left-to-right **alphabetical** carousel
- [ ] Selected card **centred**
- [ ] Card layout: box art on top (`CachedBoxart`)
- [ ] Game name underneath
- [ ] Total achievements under the name
- [ ] Gamerscore under the name
- [ ] Total time played (minutes via `PlaytimeFormatter`)
- [ ] Last played (`Game.LastPlayed`)
- [ ] **Title text auto-shrinks** when it would overflow its bounding box

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
- [ ] **No games:** placeholder with subtle disc icon + "No games found"
- [ ] **No screenshots:** placeholder
- [ ] **Fewer than 6 recent games:** `IsVisible` toggle on the 6 dashboard slots (hide empty ones)

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
- Persisted settings land in `source/XeniaManager.BigScreen/bin/{Debug|Release}/net10.0/dashboard-settings.json`.
- Opens fullscreen; Esc/B navigates back; Alt+F4 quits.
