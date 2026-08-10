# Xenia Manager BigScreen — Project Spec

> **Status:** Living document. Update as the project evolves.
> **Project:** `source/XeniaManager.BigScreen/`
> **Branch:** `feature/big-screen`
> **Stack:** Avalonia 12.1.0 · .NET 10 · CommunityToolkit.Mvvm · FluentAvalonia · SDL3-CS (planned)

---

## 1. Overview & Vision

**XeniaManager.BigScreen** is a fullscreen **Xbox Series-style dashboard** ("big screen mode") for Xenia Manager. It runs as its own Avalonia desktop app and references `XeniaManager.Core` for the game library, profiles, artwork and launching.

**Vision:** a controller-driven, big-screen experience — browse an alphabetical game carousel, pick a game, and launch straight into Xenia. Browse screenshots in a gallery with a focused modal viewer. Configure dashboard visuals (background, accent, vignette) from a settings screen. All choices persist across sessions.

**Non-goals (for now):** localization (deferred), launching BigScreen *from* the main app (deferred — the base app is the intended launcher, with a future "big screen by default" toggle; BigScreen's Quit returns to it, launching `XeniaManager.exe` if it isn't running, or closes everything per the 5.6 toggle).

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
- **Media** — screenshot gallery scanned from `Emulators/Xenia Canary/screenshots/**` (recursive, common image extensions), 4-across 16:9 grid that scrolls down (clamped at both ends — no wrap-back), **Y** cycles the sort (Newest First / Oldest First / By Game; indicator top-right via `IconStat`, capture date from file write time). Click/Enter → full-screen **modal viewer** (opaque backdrop, uniform-stretched image, faded chevrons that hide at the ends, B/Escape closes; caption shows game title + parsed capture date). Camera stub shown when the gallery is empty.
- **Settings** — background type dropdown, primary/accent colour fields (swatch + hex + palette popup), vignette slider, background image picker.
- **Quit** — closes the app.

### Base app data sharing (`Program.cs` + `Services/BaseAppLocator.cs`)
- BigScreen **reads the base Xenia Manager's data folders** (library, games, artwork, profiles): `Program.Main` calls `AppPathResolver.SetBaseDirectory(...)` before anything resolves paths (Core change, base app unaffected).
- Resolution order: `--base-dir <path>` arg → `XeniaManager.exe` next to BigScreen (production layout) → repo sibling project with the same bin config → fall back to the BigScreen folder itself.
- `dashboard-settings.json` stays next to the BigScreen executable.

### Background system (`Services/BackgroundService.cs`)
- **5 modes** (`Models/BackgroundMode.cs`): `Image`, `Solid`, `LinearGradient`, `RadialGradient`, `Dynamic` (selected game's artwork, falls back to radial).
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
- **`ScreenshotCard`** — media tile: GameCard-style layout (Tile / ArtClip / TitleBar / BorderOverlay), 4px stroke grey → accent on hover/selection.
- **`OptionsCard`** — dashboard option tile.

### Settings persistence
`DashboardSettings` JSON via `BackgroundService` (`Load`/`Save`), `ColorJsonConverter` (ARGB hex) for `Color` values.

---

## 3. Architecture

```
source/XeniaManager.BigScreen/
├── App.axaml / App.axaml.cs        # Application shell, theme wiring, localization init, MainWindow + VM setup
├── Program.cs                       # Entry point; redirects base dir to the base app's folder
├── ViewLocator.cs                   # VM → View resolution (ViewModels.XViewModel → Views.XView)
├── Views/
│   ├── MainWindow.axaml(.cs)        # Shell: header, background/fade layers, dashboard + overlay ContentControls, input routing
│   ├── DashboardView.axaml(.cs)     # Recent games row + options row + empty stub
│   ├── LibraryView.axaml(.cs)       # Library carousel + clamped scroll + empty stub
│   ├── MediaView.axaml(.cs)         # Media gallery grid + nested viewer sub-screen
│   ├── MediaViewerView.axaml(.cs)   # Full-screen screenshot viewer (chevrons, caption)
│   └── SettingsView.axaml(.cs)      # Settings screen (owns the background image picker)
├── ViewModels/
│   ├── MainWindowViewModel.cs       # Composition root: child VMs, CurrentScreen navigation, launch/quit/refresh
│   ├── HeaderViewModel.cs           # Profile, clock, wifi + controller battery state
│   ├── DashboardViewModel.cs        # RecentGames, Options, background brush + fade-through-black
│   ├── LibraryViewModel.cs          # Games carousel + sort (ScreenViewModel base)
│   ├── MediaViewModel.cs            # Screenshots + sort + viewer sub-screen (ScreenViewModel base)
│   ├── MediaViewerViewModel.cs      # Current screenshot, caption, prev/next stepping
│   ├── SettingsViewModel.cs         # Appearance options + persistence + quit toggle
│   ├── ScreenViewModel.cs           # Base for overlay screens: ScreenBackground brush
│   ├── ViewModelBase.cs
│   └── Items/
│       ├── GameCardViewModel.cs     # Core Game ref, Title, Boxart, stat strings, IsSelected, BackgroundArt
│       ├── ScreenshotItemViewModel.cs # Path, Title, CapturedAt (+ text), GameTitle, Image, IsSelected
│       └── OptionsCardViewModel.cs  # Title, Icon, TargetScreen
├── Controls/
│   ├── GameCard.axaml(.cs)          # Dashboard game tile (art + BorderOverlay strokes)
│   ├── OptionsCard.axaml(.cs)       # Dashboard option tile
│   ├── ScreenshotCard.axaml(.cs)    # Media tile (GameCard layout + 4px stroke)
│   ├── LibraryCard.axaml(.cs)       # Carousel card: box art + title + stat rows (rounded art clip)
│   ├── IconStat.axaml(.cs)          # Icon + text stat row
│   ├── InputHint.axaml(.cs)         # Keycap + label hint
│   ├── ColorPickerField.cs          # Swatch + hex + palette popup
│   └── PalettePicker.cs             # Swatch row
├── Services/
│   ├── BaseAppLocator.cs            # Resolves the base Xenia Manager folder (--base-dir / side-by-side / sibling)
│   ├── BackgroundService.cs         # Settings load/save, brush factory, ApplyResources
│   ├── ColorJsonConverter.cs
│   ├── DashboardNavigationController.cs # Row state machine, selection movement, option activation (view focus/scroll via events)
│   ├── GameLibraryService.cs        # Wraps Core GameManager: load, game list, recent-games selection
│   ├── GamepadService.cs            # SDL3 polling, button/axis normalization, open/close gamepad
│   ├── IGamepadService.cs           # Abstraction over the SDL gamepad subsystem (input + battery state)
│   ├── InputRouter.cs               # Translates keyboard + gamepad input into navigation actions (one state machine)
│   ├── ProfileService.cs            # Canary profile, gamertag/gamerscore, per-game achievement/GPD stats
│   └── ScreenshotLibraryService.cs  # Recursive screenshot scan, extension filter, game-title matching
├── Utilities/
│   ├── SelectionHelper.cs           # ISelectable + single-selection helpers (move/select/resort-preserving)
│   ├── EnumCycleHelper.cs           # Generic enum + colour palette cycling
│   └── ImageFormats.cs              # Shared screenshot extensions + file-picker patterns
├── Models/
│   ├── DashboardSettings.cs         # Persisted user-facing options
│   ├── BackgroundMode.cs / BackgroundModeOption.cs
│   ├── GameStatInfo.cs              # Achievement/gamerscore counters (unlocked / total)
│   ├── LibrarySort.cs               # Alphabetical / TimePlayed / LastPlayed
│   ├── MediaSort.cs                 # NewestFirst / OldestFirst / ByGame
│   ├── GamepadButton.cs             # Dpad/ABXY/bumpers (stick normalized onto Dpad)
│   └── OverlayScreen.cs
└── Resources/
    ├── BigScreenStyle.axaml
    ├── Language/en.axaml            # Default-language keys for Core's PlaytimeFormatter
    ├── Themes/DarkGradient.axaml · Controls.axaml
    └── Art/                          # Sample wallpapers for testing
```

**Navigation:** the shell hosts the dashboard as a `ContentControl` (`MainWindowViewModel.Dashboard`) and the overlay layer as another (`MainWindowViewModel.CurrentScreen`, null = dashboard visible). Both resolve through the `ViewLocator` (registered in `App.axaml`): `LibraryViewModel → LibraryView`, `MediaViewModel → MediaView` (which nests `MediaViewerViewModel → MediaViewerView` as its own sub-screen), `SettingsViewModel → SettingsView`. Overlay views are created per open; the window interacts with them via live visual-tree lookups (focus/scroll requests raised by `DashboardNavigationController`).

**Data flow**
- Input: keyboard (`OnWindowKeyDown`) and gamepad (`GamepadService.ButtonPressed`) → `InputRouter` → `DashboardNavigationController` actions (move/select/activate) → selection `IsSelected` → styled visuals; the controller raises focus/scroll requests the window fulfills (live visual-tree lookups).
- Navigation: `OptionsCardViewModel.TargetScreen` → `MainWindowViewModel.OpenScreen()` → `CurrentScreen` (a screen VM) → `ContentControl` → `ViewLocator` resolves the overlay view; null shows the dashboard. Media nests its own viewer sub-screen (`MediaViewModel.Viewer` → `MediaViewerView`).
- Selection: focus/click on a card → `IsSelected` → styled via `.selected` class / pseudo-class → visuals. **Dashboard (`DashboardViewModel.RecentGames`) and library (`LibraryViewModel.Games`) hold separate VM instances with independent selections.**
- Settings: VM property → `BackgroundService.Settings` → `Save()` + `ApplyResources()` → `Application.Resources` → `DynamicResource` bindings; `SettingsViewModel.AppearanceChanged` → dashboard rebuilds its background.
- Sorting: **Y** in the library → `LibraryViewModel.CycleSort()` → re-orders `Games` (title asc, playtime desc, last-played desc), keeps the selection, re-scrolls.
- Data: `ProfileService` (profile + GPD stats) · `GameLibraryService` (Core `GameManager` wrapper) · `ScreenshotLibraryService` (scan + game-title matching) feed the child VMs; Core paths (library, games, artwork, profiles, logs) resolve against the base app's folder via `Program.Main` → `BaseAppLocator.Resolve(args)` → `AppPathResolver.SetBaseDirectory(...)`.

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
- [x] Left-to-right **alphabetical** carousel (sortable with **Y**: Alphabetical / Time Played / Last Played; selection follows the list index, not the element — viewport stays put)
- [ ] Selected card **centred** — *decided against*: standard left-to-right list, scrolls once the selection passes the middle, clamped at both ends (no wrap)
- [x] Card layout: box art on top (`CachedBoxart`, bottom-anchored with a 13% top crop)
- [x] Game name underneath
- [x] Total achievements under the name
- [x] Gamerscore under the name
- [x] Total time played (minutes via `PlaytimeFormatter`)
- [ ] Last played (`Game.LastPlayed`) — *removed from the card by request* (playtime row moved above achievements)

### 5.3 Media gallery
- [x] Clean wrap panel with adequate spacing over all screenshots (4-across 16:9 grid, card size fits the window width)
- [x] Click a screenshot → **modal focus**
- [x] Subtle chevron arrows in the modal for navigation (visual affordance, faded 0.45 → full on hover)
- [x] B/Escape closes the modal (then the overlay)
- [x] Grid scrolls down like the library scrolls sideways; clamped at both ends, no wrap-back
- [x] Sort with **Y**: Newest First / Oldest First / By Game (indicator top-right, capture date from file write time). Selection follows the **list index**, not the element — the viewport stays put and the card at the same position is selected (no fly-across)

### 5.4 Game launch
- [x] Launch from the carousel via `Launcher.LaunchGameASync` (needs Core `Settings`)
- [x] **A**/**Enter** launches the selected game (library carousel and dashboard cards)
- [x] Window disables while the game runs (`EventManager.DisableWindow`) and re-enables on exit
- [x] Library refreshes after the session (playtime / last played via `GameManager.LoadLibrary()` + card rebuild)

### 5.5 Dynamic background
- [x] Populate `GameCardViewModel.BackgroundArt` from `CachedBackground`
- [x] Dynamic mode works with real art (fallback to radial gradient when missing)
- [x] Selection in **either** row (dashboard or library) drives the artwork; library selection reveals on overlay close
- [x] Fade-through-black transition on art swaps (180ms each way, latest-wins on rapid switching); settings changes stay instant

### 5.6 Quit behaviour toggle
- [x] Toggle switch in Settings ("Return to Xenia Manager on Quit") — radio-switch style (track + sliding thumb, accent when on)
- [x] **On** (default) = Quit returns to Xenia Manager — launches `XeniaManager.exe` from the base dir if it isn't already running, then closes BigScreen
- [x] **Off** = Quit closes BigScreen only ("Closes Everything")
- [x] Tagline explaining the behaviour ("On: Returns to Xenia Manager · Off: Closes Everything")
- [x] Persisted in `DashboardSettings` (`ReturnToXeniaOnQuit`, default true)

### 5.7 Empty states
- [x] **No games:** placeholder with subtle disc icon + "No games found" (dashboard row **and** library screen)
- [x] **No screenshots:** placeholder (camera icon + "No screenshots found" in the media gallery)
- [x] **Fewer than 6 recent games:** dashboard row is capped at 6 via the `RecentGames` collection (fewer games = fewer cards, no empty slots)

### 5.8 SDL3 gamepad input
- [x] Add `ppy.SDL3-CS` package (win-x64 native included, `AllowUnsafeBlocks` for the pointer API)
- [x] `GamepadService`: SDL gamepad init (graceful failure), UI-thread 50ms poll of button + left-stick axis events (deadzone edge detection, no hold repeat); D-pad, left stick (X/Y) and bumpers all normalize onto the D-pad values; opens the first gamepad so events flow, handles add/remove; full NLog tracing of raw input
- [x] D-pad + A/B navigation mapped to the existing focus/selection model
- [x] Dashboard is **row-state driven** (explicit games/options row, not keyboard focus): D-pad Up/Down switches rows with a fixed column mapping (game 1 → option 1 · games 2-3 → option 2 · games 4-5 → option 3 · game 6 → option 4; up: option 1 → game 1 · 2 → 2 · 3 → 4 · 4 → 6); A acts on the active row only (options never also launch the game); options row clears when returning to games; no games = options row stays active (incl. empty-library startup/refresh)
- [x] Left/Right via D-pad, left stick and bumpers (library/media/modal); Up/Down via D-pad and stick (media row jumps)
- [x] Settings remains keyboard-only; input gated while the window is disabled (game running)
- [x] Sorting keeps the viewport fixed and selection follows the **list index** (not the element), so no fly-across — library and media

### 5.9 Real controller battery / wifi
- [x] Wire header battery icon to live controller state (`SDL_GetGamepadPowerInfo`, 5s poll; `BatteryWarning` when no controller/unknown, `Battery10` when wired/charging, tier icons otherwise)
- [x] Wire header wifi icon to live state (`NetworkInterface` Wireless80211 check, 10s poll; `WiFi` full signal / `WiFiOff`)
- [x] Header icons aligned (fixed-size centred boxes); `ControllerConnected` now live

### 5.10 Final hardening phase
- [ ] Manual walkthrough of everything; rewrites where necessary
- [x] Maintainability sweep: deduplication, DRY, SOLID
- [x] **Codebase compliance sweep** against root `CONTRIBUTING.md`:
  - [x] Naming: `_camelCase` private fields, PascalCase methods/properties, Hungarian-prefixed XAML names (`Cmb`, `Txt`, `Btn`, `Tbl`, `Sp`, `Grd`, `Sv`)
  - [x] AXAML property order (x:Name → x:DataType → grid placement → bindings → layout → style → events)
  - [x] XML doc comments on public/internal members; sparse inline comments
  - [x] Logger-based error handling (`Logger.Error<T>`, `Logger.LogExceptionDetails<T>`)
  - [x] 4-space indent, braces on new lines, file-scoped namespaces, alphabetical usings
  - [x] MVVM: keep code-behind minimal; business logic in Core

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
