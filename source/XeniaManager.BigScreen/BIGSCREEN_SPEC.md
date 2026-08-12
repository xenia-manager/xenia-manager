# Xenia Manager BigScreen — Project Spec

> **Status:** Living document. Update as the project evolves.
> **Project:** `source/XeniaManager.BigScreen/`
> **Branch:** `feature/big-screen`
> **Stack:** Avalonia 12.1.0 · .NET 10 · CommunityToolkit.Mvvm · FluentAvalonia · SDL3-CS · Microsoft.Extensions.DependencyInjection (transitive via Core)

---

## 1. Overview & Vision

**XeniaManager.BigScreen** is a fullscreen **Xbox Series-style dashboard** ("big screen mode") for Xenia Manager. It runs as its own Avalonia desktop app and references `XeniaManager.Core` for the game library, profiles, artwork and launching.

**Vision:** a controller-driven, big-screen experience — browse an alphabetical game carousel, pick a game, and launch straight into Xenia. Browse screenshots in a gallery with a focused modal viewer. Configure dashboard visuals (background, accent, vignette) from a settings screen. All choices persist across sessions.

**Non-goals (for now):** other languages beyond the English key set (translations deferred — the app is fully keyed and wired), and the **"Launch Big Screen by default"** settings toggle + auto-launch at startup (queued — see 5.12; the nav launch button is done). BigScreen's Quit returns to Xenia Manager, launching `XeniaManager.exe` if it isn't running, or closes everything per the 5.6 toggle.

---

## 2. Current State (done)

### Dashboard shell (`Views/MainWindow.axaml`)
- **Header:** avatar icon (PersonCircle), gamertag from the **Canary profile** (`ProfileManager.LoadProfiles(XeniaVersion.Canary)` + profile GPD), gamerscore as an `IconStat` (Star), **live** wifi + controller battery icons (10s/5s polls), live clock (1s `DispatcherTimer`).
- **Game card row:** **max 6 recent games** (`RecentGames`, its own VM instances — independent selection from the library). Each `GameCard` shows its box art (zoom-to-fill, bottom-anchored, rounded clip); the selected card grows (200→250) and shows the title bar. A transparent `BorderOverlay` larger than the card carries **all** border strokes (inactive `CardBorder`, hover/selected accent) so nothing sits on top of the art edges.
- **Option card row:** `Library` · `Media` · `Settings` · `Quit`. Hover shows the accent border; selection is driven by `IsSelected` (controller focus), not by keyboard focus.
- **Fullscreen:** plain `Window` (no title bar) with `WindowState=FullScreen` **forced in code** at construction and before `Show()` — the XAML attribute is not reliable on the DI creation path; 1920×1080 fallback, centered column layout so header aligns with content rows.

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

### Boot splash (Feature 3)
- **Separate `SplashWindow`** (fullscreen, borderless, Topmost) shown before the main window; startup is deferred via `Dispatcher.UIThread.Post(StartApp, DispatcherPriority.Background)` so the splash **paints before any loading work** (the UniGetUI pattern — an in-window splash cannot cover process/Avalonia startup).
- Content: TV logo + "Xenia Big Screen" + live status text + tweened progress bar, over the **dashboard's radial background** built from the saved `primary_color` (same stops/constants as `BackgroundService`); logo + bar use the saved `accent_color` so the splash matches the dashboard — no green→red flash.
- **Boot pipeline** (`MainWindowViewModel.InitializeAsync`, cancellable, per-stage minimum 400ms dwell): Loading Profile → Loading Settings (settings JSON + background brush/image decode) → Loading Dashboard (library JSON + recent cards) → Loading Library (per-game GPD stats off-thread + cards, chunked progress) → Loading Media (screenshot scan in `Task.Run`) → Loading Done (1s hold). Total minimum ~3s.
- Input (keyboard, gamepad, mouse activation) is **gated until `IsInitialized`** — no stray input can act during the splash.
- Main window is created under the splash (fullscreen), revealed when the pipeline completes; boot failures log and still reveal.

### Main app integration (Feature 1)
- **"Big Screen" nav button** in Xenia Manager (`MainView.axaml`, `Tv` icon, tooltip "Open Big Screen") → `NavigationService` `BigScreen` tag → launches `XeniaManager.BigScreen.exe` resolved **side-by-side or via the repo-sibling bin folder** (same config, matching `BaseAppLocator`); missing exe → localized warning box.
- `UiSettings.Window.LaunchBigScreen` property reserved in Core for the deferred auto-launch toggle (5.12).

### Theming
- `Resources/Themes/DarkGradient.axaml` — token dictionary: `CardBackground`, `CardTitleBar`, `CardBorder`, `AccentColor`, `TextPrimary/Secondary`, `HintKeyY/A/B`, `CardShadow`/`CardShadowSelected` (`BoxShadows`!), `SystemAccentColor*` (Color-typed variants), slider/control overrides, accent-fill family.
- `Resources/Themes/Controls.axaml` — `ControlTheme`s for the custom controls (`ColorPickerField`, `PalettePicker`).
- `Resources/BigScreenStyle.axaml` — global styles (cards, shared `.screen-title`/`.card-title`/`.hint-bar`/`.empty-state` classes, text halos).
- `Resources/Language/en.axaml` — **full key set for every user-facing string** (main-app naming convention), wired via `{DynamicResource}` in XAML and `LocalizationHelper.GetText` in code; `LocalizationHelper.Initialize(...)` in `App.axaml.cs`. Other languages = new files only.
- FluentAvalonia theme (`FluentAvaloniaTheme`) like the main app.

### Custom controls (`Controls/`)
- **`ColorPickerField`** — swatch + hex text box; clicking the swatch opens a palette popup. Two palettes: muted slates/greys (primary) and 10 accent colours incl. white/light grey.
- **`PalettePicker`** — horizontal StackPanel of colour swatches (background card, spacing, margin); raises `SelectedColorChanged`.
- **`IconStat`** — icon + text row (`Icon` Symbol, `Stat`, `IconSize`, `FontSize`, `Spacing`, `IconRotation`). Used in the header (gamerscore), library sort indicator, and library cards.
- **`InputHint`** — keycap + label (`KeyColour`, `Icon`/`Char` glyph, `Text`): transparent circle with coloured 2px outline, glyph coloured to match, white label. Xbox-standard colours per usage (Y amber, A green, B red).
- **`LibraryCard`** — carousel card: box art (bottom-anchored, top 13% cropped, rounded clip via `RectangleGeometry`), title, playtime row, achievements/gamerscore row; `CardBorder` 2px inactive → accent on selection (outer border only).
- **`GameCard`** — dashboard game tile: box art (bottom-anchored, rounded clip) + title bar on selection, border overlay strokes; grows 200→250 on selection.
- **`ScreenshotCard`** — media gallery tile: 16:9 screenshot with 6px rounded clip, title bar on selection, border overlay strokes.
- **`OptionsCard`** — dashboard option tile.

### Settings persistence
`DashboardSettings` JSON via `BackgroundService` (`Load`/`Save`), `ColorJsonConverter` (ARGB hex) for `Color` values.

---

## 3. Architecture

```
source/XeniaManager.BigScreen/
├── App.axaml / App.axaml.cs        # Application shell, theme wiring, localization init, DI container (App.Services) + MainWindow resolution
├── Program.cs                       # Entry point; redirects base dir to the base app's folder
├── ViewLocator.cs                   # VM → View resolution (ViewModels.XViewModel → Views.XView)
├── Views/
│   ├── MainWindow.axaml(.cs)        # Shell (plain Window, fullscreen forced in code): header, background/fade layers, dashboard + overlay screens, input routing
│   ├── DashboardView.axaml(.cs)     # Recent games row + options row + empty stub
│   ├── LibraryView.axaml(.cs)       # Library carousel + clamped scroll + empty stub
│   ├── MediaView.axaml(.cs)         # Media gallery grid + nested viewer sub-screen
│   ├── MediaViewerView.axaml(.cs)   # Full-screen screenshot viewer (chevrons, caption)
│   ├── SettingsView.axaml(.cs)      # Settings screen (owns the background image picker)
│   ├── SplashWindow.axaml(.cs)      # Boot splash window (fullscreen, Topmost, shows before the main window)
│   └── SplashContent.axaml(.cs)     # Splash visuals: logo, live status, tweened bar, radial background from saved primary/accent
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
│   ├── GameCard.axaml(.cs)          # Dashboard game tile: box art + title bar on selection (grow 200→250)
│   ├── OptionsCard.axaml(.cs)       # Dashboard option tile
│   ├── ScreenshotCard.axaml(.cs)    # Media tile: 16:9 screenshot, 6px corners
│   ├── LibraryCard.axaml(.cs)       # Carousel card: box art + title + stat rows (rounded art clip)
│   ├── IconStat.axaml(.cs)          # Icon + text stat row
│   ├── InputHint.axaml(.cs)         # Keycap + label hint
│   ├── ColorPickerField.cs          # Swatch + hex + palette popup
│   └── PalettePicker.cs             # Swatch row
├── Services/
│   ├── BaseAppLocator.cs            # Resolves the base Xenia Manager folder (--base-dir / side-by-side / sibling)
│   ├── BackgroundService.cs         # Settings load/save, brush factory, ApplyResources (IBackgroundService)
│   ├── ColorJsonConverter.cs
│   ├── DashboardNavigationController.cs # Row state machine, selection movement, option activation (view focus/scroll via events)
│   ├── GameLibraryService.cs        # Wraps Core GameManager: load, game list, recent-games selection (IGameLibraryService)
│   ├── GamepadService.cs            # SDL3 polling, button/axis normalization, open/close gamepad (IGamepadService)
│   ├── IBackgroundService.cs / IGamepadService.cs / IGameLibraryService.cs / IProfileService.cs / IScreenshotLibraryService.cs
│   ├── InputRouter.cs               # Command-driven: key/gamepad → Command → per-screen handler (one state machine, no duplicated branching)
│   ├── ProfileService.cs            # Canary profile, gamertag/gamerscore, per-game achievement/GPD stats (IProfileService)
│   ├── ScreenshotLibraryService.cs  # Recursive screenshot scan, extension filter, game-title matching (IScreenshotLibraryService)
│   └── ServiceConfigurator.cs       # DI registration (mirrors the main app: singleton services + VMs, App.Services)
├── Constants/
│   ├── AppConstants.cs              # BaseAppExecutable, RecentGamesLimit, SettingsFileName
│   ├── TimingConstants.cs           # Gamepad/battery/wifi/clock polls, fade, splash stage/done/minimum timings
│   ├── FormatConstants.cs           # Clock/capture-date formats, XUID format
│   ├── XboxConstants.cs             # ProfileContentTitleId (FFFE07D1)
│   └── LayoutConstants.cs           # Vignette step, gradient mixes, accent tint step, carousel fallbacks
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
    ├── BigScreenStyle.axaml        # Shared card/screen-title/hint-bar/empty-state styles
    ├── Language/en.axaml           # Full key set for every user-facing string (+ Core playtime keys)
    ├── Themes/DarkGradient.axaml · Controls.axaml
    └── Art/                          # Sample wallpapers for testing
```

**Navigation:** the shell hosts the dashboard as a `ContentControl` (`MainWindowViewModel.Dashboard`) and the three overlay screens as pre-instantiated `ContentControl`s whose content never changes (`MainWindowViewModel.Library` / `Media` / `Settings`, visibility flipped via `Is*Screen`). Views are created **once at startup** — opening a screen is a pure visibility flip (instant), and all boot-time work (profile, library, screenshot scan) happens behind the splash window. The viewer nests as a sub-screen (`MediaViewModel.Viewer` → `MediaViewerView`, created per open). The window interacts with the overlay views via live visual-tree lookups (`Find<T>()`) for focus/scroll requests raised by `DashboardNavigationController`.

**Boot:** `Program.Main` → `App` builds DI → `SplashWindow` shown immediately, startup deferred via `Dispatcher.Post(StartApp, Background)` so the splash paints first → `MainWindowViewModel.InitializeAsync` runs the six staged loads (cancellable, per-stage dwell, progress via `IProgress`) → splash closes on completion (3s minimum total) → input un-gated (`IsInitialized`).

**Data flow**
- DI: `App.Services` (built by `ServiceConfigurator.ConfigureServices()`) → singleton services (`IBackgroundService`, `IProfileService`, `IGameLibraryService`, `IScreenshotLibraryService`, `IGamepadService`, `DashboardNavigationController`, `InputRouter`) + `MainWindowViewModel` + `MainWindow` (parameterless ctor resolving from `App.Services` — the XAML loader requires it).
- Input: keyboard (`OnWindowKeyDown`) and gamepad (`IGamepadService.ButtonPressed`) → `InputRouter` (key/button → `Command` → per-screen handler) → `DashboardNavigationController` actions (move/select/activate) → selection `IsSelected` → styled visuals; the controller raises focus/scroll requests the window fulfills. All input is gated until the boot pipeline completes.
- Navigation: `OptionsCardViewModel.TargetScreen` → `MainWindowViewModel.OpenScreen()` → `CurrentScreen` (a screen VM) → the matching overlay's `IsVisible` flips; null shows the dashboard. Media nests its own viewer sub-screen (`MediaViewModel.Viewer` → `MediaViewerView`).
- Selection: focus/click on a card → `IsSelected` → styled via `.selected` class / pseudo-class → visuals. **Dashboard (`DashboardViewModel.RecentGames`) and library (`LibraryViewModel.Games`) hold separate VM instances with independent selections.**
- Settings: VM property → `IBackgroundService.Settings` → `Save()` + `ApplyResources()` → `Application.Resources` → `DynamicResource` bindings; `SettingsViewModel.AppearanceChanged` → dashboard rebuilds its background.
- Localization: XAML `{DynamicResource Key}` + C# `LocalizationHelper.GetText("Key")` → `Resources/Language/en.axaml`.
- Sorting: **Y** in the library → `LibraryViewModel.CycleSort()` → re-orders `Games` (title asc, playtime desc, last-played desc), keeps the selection, re-scrolls.
- Data: `IProfileService` (profile + GPD stats) · `IGameLibraryService` (Core `GameManager` wrapper) · `IScreenshotLibraryService` (scan + game-title matching) feed the child VMs; Core paths (library, games, artwork, profiles, logs) resolve against the base app's folder via `Program.Main` → `BaseAppLocator.Resolve(args)` → `AppPathResolver.SetBaseDirectory(...)`.

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
- [x] Card layout: box art on top (`CachedBoxart`, bottom-anchored with a 13% top crop)
- [x] Game name underneath
- [x] Total achievements under the name
- [x] Gamerscore under the name
- [x] Total time played (minutes via `PlaytimeFormatter`)

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
- [x] Manual walkthrough of everything; rewrites where necessary
- [x] Maintainability sweep: deduplication, DRY, SOLID
- [x] **Codebase compliance sweep** against root `CONTRIBUTING.md`:
  - [x] Naming: `_camelCase` private fields, PascalCase methods/properties, Hungarian-prefixed XAML names (`Cmb`, `Txt`, `Btn`, `Tbl`, `Sp`, `Grd`, `Sv`)
  - [x] AXAML property order (x:Name → x:DataType → grid placement → bindings → layout → style → events)
  - [x] XML doc comments on public/internal members; sparse inline comments
  - [x] Logger-based error handling (`Logger.Error<T>`, `Logger.LogExceptionDetails<T>`)
  - [x] 4-space indent, braces on new lines, file-scoped namespaces, alphabetical usings
  - [x] MVVM: keep code-behind minimal; business logic in Core

### 5.11 Post-sweep engineering
- [x] **Command-driven `InputRouter` rewrite** — key/button → `Command` enum → per-screen handler; kills the duplicated keyboard/gamepad branching; extracted `CloseOverlay`/`MoveDashboard`/`Activate` helpers
- [x] **DI adoption (mirrors main app)** — `ServiceConfigurator` + `App.Services` (Microsoft.Extensions.DependencyInjection, transitive via Core); interface + impl for all services; `MainWindow` ctor-injected; single `IGamepadService` singleton (the double-instantiation bug that dropped ~half of gamepad input was found and fixed)
- [x] **Logging alignment** — density raised to ~1 per 58 lines (was 1/113; main app ~1/21): lifecycle milestones, navigation, launches, settings changes (Info), transitions/moves (Debug/Trace), slider values at Debug to avoid spam
- [x] **Constants extraction** — `Constants/` (App/Timing/Format/Xbox/Layout), mirrors Core's per-domain static classes; zero magic values left in C#
- [x] **CPP-style declaration order** (repo convention) — fields → properties → constructors → methods; nothing called before it's declared; expression bodies for one-liners; explicit types (no `var`)
- [x] **Performance fixes** — overlay screens pre-instantiated at startup (open = visibility flip, no per-open view creation); screenshot scan moved to boot; library first-game pre-selected on open (VM-side, avoids startup background clobber); first library game's background art pre-warmed at boot; viewer selection follows the stepped screenshot (grid lands on the last viewed image on close); missing `IsViewerOpen` change notification fixed

### 5.12 Main app launch button + splash + localization
- [x] **Big Screen launch button (main app)** — "Big Screen" nav item (Tv icon, "Open Big Screen" tooltip) → `NavigationService` launches the exe side-by-side or from the repo-sibling bin folder; missing exe → localized warning; keys in en.axaml
- [x] **Boot splash screen** — separate `SplashWindow` (paints before any loading via deferred startup), six staged loading statuses with per-stage dwell + tweened bar + 3s minimum + 1s "Loading Done" hold, dashboard-style radial background + saved accent (no green→red flash); input gated until `IsInitialized`; main window is a plain `Window` with fullscreen forced in code
- [x] **Localization key set + wiring** — every user-facing string keyed in `en.axaml` (main-app naming convention) and wired via `{DynamicResource}` / `LocalizationHelper.GetText`
- [x] **AXAML/style consolidation** — shared `.screen-title`/`.card-title`/`.hint-bar`/`.empty-state` styles, `HintKeyY/A/B` + `CardShadow`/`CardShadowSelected` tokens, Window.DataTemplates → App.axaml, IconStat FontSize de-hiding, GameCard border selector combine
- [x] **ArtTile removed** — `ArtTile` abstraction undone; `GameCard`/`ScreenshotCard` are standalone controls again (own Tile / ArtClip / TitleBar / BorderOverlay)
- [x] **Stability fixes** — `BoxShadow`→`BoxShadows` resource cast crash; FAAppWindow title bar removed (plain Window + code-forced fullscreen); background-image decode moved behind the splash
- [ ] **"Launch Big Screen by default" settings toggle + auto-launch at startup** (main app) — `UiSettings.Window.LaunchBigScreen` field is ready in Core; add the Settings toggle card, wire the partial, and launch BigScreen at startup when enabled (main app stays open in the background)

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
