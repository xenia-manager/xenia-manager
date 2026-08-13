# Xenia Manager BigScreen — Project Spec

> **Status:** Living document. Update as the project evolves.
> **Project:** `source/XeniaManager.BigScreen/`
> **Branch:** `feature/big-screen`
> **Stack:** Avalonia 12.1.0 · .NET 10 · CommunityToolkit.Mvvm · FluentAvalonia · SDL3-CS · Microsoft.Extensions.DependencyInjection (transitive via Core)

---

## 1. Overview & Vision

**XeniaManager.BigScreen** is a fullscreen **Xbox Series-style dashboard** ("big screen mode") for Xenia Manager. It runs as its own Avalonia desktop app and references `XeniaManager.Core` for the game library, profiles, artwork and launching.

**Vision:** a controller-driven, big-screen experience — browse the game library (carousel or list), pick a game, and launch straight into Xenia. Browse screenshots in a gallery with a focused modal viewer. Configure dashboard visuals (background, accent, vignette) from a settings screen. All choices persist across sessions.

**Non-goals (for now):** other languages beyond the English key set (translations deferred — the app is fully keyed and wired). BigScreen's Quit returns to Xenia Manager, launching `XeniaManager.exe` if it isn't running, or closes everything per the 5.6 toggle.

---

## 2. Current State (done)

### Dashboard shell (`Views/MainWindow.axaml`)
- **Header:** avatar icon (PersonCircle), gamertag from the **Canary profile** (`ProfileManager.LoadProfiles(XeniaVersion.Canary)` + profile GPD), gamerscore as an `IconStat` (Star), **live** wifi + controller battery icons (10s/5s polls), live clock (1s `DispatcherTimer`).
- **Game card row:** **max 6 recent games** (`RecentGames`, its own VM instances — independent selection from the library). Each `GameCard` shows its **box art or disc icon** per the `card_image_mode` setting (default Icon; Box Art mode falls back to the icon when art is missing; zoom-to-fill, bottom-anchored, rounded clip); the selected card grows (200→250) and shows the title bar. A transparent `BorderOverlay` larger than the card carries **all** border strokes (inactive `CardBorder`, hover/selected accent) so nothing sits on top of the art edges.
- **Option card row:** `Library` · `Media` · `Settings` · `Quit`. Hover shows the accent border; selection is driven by `IsSelected` (controller focus), not by keyboard focus.
- **Fullscreen:** plain `Window` (no title bar) with `WindowState=FullScreen` **forced in code** at construction and before `Show()` — the XAML attribute is not reliable on the DI creation path; 1920×1080 fallback, centered column layout so header aligns with content rows.

### Overlay screens
Full-screen pages rendered over the dashboard; **Enter/click** opens, **B/Escape** closes, focus returns to the option row. Bottom hint bars use the `InputHint` control (coloured circle keycap + label).
- **Library** — all games in a horizontal **carousel** (`LibraryCard`s: box art with a **13% top crop** (bottom-anchored, ~366px art region), title, playtime row, achievements/gamerscore row from the profile GPD) or a vertical **list** with a details pane (`LibraryListItem` + `GameDetailsPanel`). Left/Right iterates (clamped at both ends — no wrap), the row scrolls once the selection passes the middle; in list mode Up/Down iterates. **Y** cycles the sort (Alphabetical → Time Played → Last Played; indicator top-right via `IconStat`). **View/V** swaps the layout (Carousel ↔ List, persisted via `library_view_mode`); the details pane shows marketplace DB info (bio, genre, developer, publisher, release date — loading/no-info states, stale-fetch guard + negative cache). Disc stub shown when the library is empty.
- **Media** — screenshot gallery scanned from `Emulators/Xenia Canary/screenshots/**` (recursive, common image extensions), 4-across 16:9 grid that scrolls down (clamped at both ends — no wrap-back), **Y** cycles the sort (Newest First / Oldest First / By Game; indicator top-right via `IconStat`, capture date from file write time). Click/Enter → full-screen **modal viewer** (opaque backdrop, uniform-stretched image, faded chevrons that hide at the ends, B/Escape closes; caption shows game title + parsed capture date). Camera stub shown when the gallery is empty. Hints: Back · Select (A) · Sort (Y).
- **Settings** — background type dropdown, library view dropdown, **card image dropdown** (Box Art / Icon, default Icon), primary/accent colour fields (swatch + hex + palette popup), vignette slider, background image picker.
- **Quit** — closes the app.
- **Hint bars** — order per screen: Library = Back (B red) → Play (A green) → Sort (Y amber) → Swap View (faded-white `CaretLeft`, `HintKeyBack` token); Media = Back → Select (A) → Sort; Viewer/Settings = Back/Close.

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

### Theming
- `Resources/Themes/DarkGradient.axaml` — token dictionary: `CardBackground`, `CardTitleBar`, `CardBorder`, `AccentColor`, `TextPrimary/Secondary`, `HintKeyY/A/B/Back`, `CardShadow`/`CardShadowSelected` (`BoxShadows`!), `SystemAccentColor*` (Color-typed variants), slider/control overrides, accent-fill family.
- `Resources/Themes/Controls.axaml` — `ControlTheme`s for the custom controls (`ColorPickerField`, `PalettePicker`).
- `Resources/BigScreenStyle.axaml` — global styles (cards, shared `.screen-title`/`.card-title`/`.hint-bar`/`.empty-state` classes, text halos).
- `Resources/Language/en.axaml` — **full key set for every user-facing string** (main-app naming convention), wired via `{DynamicResource}` in XAML and `LocalizationHelper.GetText` in code; `LocalizationHelper.Initialize(...)` in `App.axaml.cs`. Other languages = new files only.
- FluentAvalonia theme (`FluentAvaloniaTheme`) like the main app.

### Custom controls (`Controls/`)
- **`ColorPickerField`** — swatch + hex text box; clicking the swatch opens a palette popup. Two palettes: muted slates/greys (primary) and 10 accent colours incl. white/light grey.
- **`PalettePicker`** — horizontal StackPanel of colour swatches (background card, spacing, margin); raises `SelectedColorChanged`.
- **`IconStat`** — icon + text row (`Icon` Symbol, `Stat`, `IconSize`, `FontSize`, `Spacing`, `IconRotation`). Used in the header (gamerscore), library sort indicator, and library cards.
- **`InputHint`** — keycap + label (`KeyColour`, `Icon`/`Char` glyph, `Text`): transparent circle with coloured 2px outline, glyph coloured to match, white label. Xbox-standard colours per usage (Y amber, A green, B red; the back/view button uses a faded-white `CaretLeft` via `HintKeyBack`).
- **`LibraryCard`** — carousel card: box art (bottom-anchored, top 13% cropped, rounded clip via `RectangleGeometry`), title, playtime row, achievements/gamerscore row; `CardBorder` 2px inactive → accent on selection (outer border only).
- **`LibraryListItem`** — list-view row: disc icon + title, accent border on selection/hover.
- **`GameDetailsPanel`** — list-view details pane: disc art, playtime/achievements/gamerscore, marketplace DB bio + metadata strip (genre, developer, publisher, released), loading bar / no-info states.
- **`GameCard`** — dashboard game tile: box art or disc icon per `card_image_mode` (bottom-anchored, rounded clip) + title bar on selection, border overlay strokes; grows 200→250 on selection.
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
│   ├── LibraryView.axaml(.cs)       # Library carousel + list, clamped scroll, details pane + empty stub
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
│   ├── SettingsViewModel.cs         # Appearance options + persistence + quit toggle + library view + card image
│   ├── ScreenViewModel.cs           # Base for overlay screens: ScreenBackground brush
│   ├── ViewModelBase.cs
│   └── Items/
│       ├── GameCardViewModel.cs     # Core Game ref, Title, Boxart/DiscArt layers (card_image_mode), stat strings, IsSelected, BackgroundArt
│       ├── GameDetailsViewModel.cs  # Details pane: local card stats + DB info (bio/genre/developer/publisher/released)
│       ├── ScreenshotItemViewModel.cs # Path, Title, CapturedAt (+ text), GameTitle, Image, IsSelected
│       └── OptionsCardViewModel.cs  # Title, Icon, TargetScreen
├── Controls/
│   ├── GameCard.axaml(.cs)          # Dashboard game tile: box art + title bar on selection (grow 200→250)
│   ├── OptionsCard.axaml(.cs)       # Dashboard option tile
│   ├── ScreenshotCard.axaml(.cs)    # Media tile: 16:9 screenshot, 6px corners
│   ├── LibraryCard.axaml(.cs)       # Carousel card: box art + title + stat rows (rounded art clip)
│   ├── LibraryListItem.axaml(.cs)   # List row: disc icon + title (accent on select/hover)
│   ├── GameDetailsPanel.axaml(.cs)  # Details pane: art, local stats, DB bio + metadata strip
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
│   ├── IBackgroundService.cs / IGameLibraryService.cs / IProfileService.cs / IScreenshotLibraryService.cs
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
│   ├── LibraryViewMode.cs / LibraryViewModeOption.cs
│   ├── CardImageMode.cs / CardImageModeOption.cs
│   ├── GameStatInfo.cs              # Achievement/gamerscore counters (unlocked / total)
│   ├── LibrarySort.cs               # Alphabetical / TimePlayed / LastPlayed
│   ├── MediaSort.cs                 # NewestFirst / OldestFirst / ByGame
│   └── OverlayScreen.cs             # GamepadButton moved to Core (Models/GamepadButton.cs)
└── Resources/
    ├── BigScreenStyle.axaml        # Shared card/screen-title/hint-bar/empty-state styles
    ├── Language/en.axaml           # Full key set for every user-facing string (+ Core playtime keys)
    ├── Themes/DarkGradient.axaml · Controls.axaml
    └── Art/                          # Sample wallpapers for testing
```

**Navigation:** the shell hosts the dashboard as a `ContentControl` (`MainWindowViewModel.Dashboard`) and the three overlay screens as pre-instantiated `ContentControl`s whose content never changes (`MainWindowViewModel.Library` / `Media` / `Settings`, visibility flipped via `Is*Screen`). Views are created **once at startup** — opening a screen is a pure visibility flip (instant), and all boot-time work (profile, library, screenshot scan) happens behind the splash window. The viewer nests as a sub-screen (`MediaViewModel.Viewer` → `MediaViewerView`, created per open). The window interacts with the overlay views via live visual-tree lookups (`Find<T>()`) for focus/scroll requests raised by `DashboardNavigationController`.

**Boot:** `Program.Main` → `App` builds DI → `SplashWindow` shown immediately, startup deferred via `Dispatcher.Post(StartApp, Background)` so the splash paints first → `MainWindowViewModel.InitializeAsync` runs the six staged loads (cancellable, per-stage dwell, progress via `IProgress`) → splash closes on completion (3s minimum total) → input un-gated (`IsInitialized`).

**Data flow**
- DI: `App.Services` (built by `ServiceConfigurator.ConfigureServices()`) → singleton services (`IBackgroundService`, `IProfileService`, `IGameLibraryService`, `IScreenshotLibraryService`, `IGamepadInputService`, `DashboardNavigationController`, `InputRouter`) + `MainWindowViewModel` + `MainWindow` (parameterless ctor resolving from `App.Services` — the XAML loader requires it).
- Input: keyboard (`OnWindowKeyDown`) and gamepad (`IGamepadInputService.ButtonPressed`) → `InputRouter` (key/button → `Command` → per-screen handler) → `DashboardNavigationController` actions (move/select/activate) → selection `IsSelected` → styled visuals; the controller raises focus/scroll requests the window fulfills. All input is gated until the boot pipeline completes.
- Navigation: `OptionsCardViewModel.TargetScreen` → `MainWindowViewModel.OpenScreen()` → `CurrentScreen` (a screen VM) → the matching overlay's `IsVisible` flips; null shows the dashboard. Media nests its own viewer sub-screen (`MediaViewModel.Viewer` → `MediaViewerView`).
- Selection: focus/click on a card → `IsSelected` → styled via `.selected` class / pseudo-class → visuals. **Dashboard (`DashboardViewModel.RecentGames`) and library (`LibraryViewModel.Games`) hold separate VM instances with independent selections.**
- Settings: VM property → `IBackgroundService.Settings` → `Save()` + `ApplyResources()` → `Application.Resources` → `DynamicResource` bindings; `SettingsViewModel.AppearanceChanged` → dashboard rebuilds its background; `LibraryViewModeChanged` / `CardImageChanged` → live layout / card-image swaps.
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
- `GamepadInputService` / `IGamepadInputService` — SDL3 gamepad polling (moved from BigScreen; `GamepadButton` enum incl. `View`).
- `ReleaseDateFormatter.Format(...)` — ordinal release dates (+ unit tests).

---

## 4. Design System

### Tokens (`DarkGradient.axaml`)
| Token | Purpose |
|---|---|
| `CardBackground` / `CardTitleBar` | Card surfaces |
| `CardBorder` | Reserved 3–4px borders (no layout shift on focus) |
| `AccentColor` | Selected/hover accent (runtime-configurable) |
| `TextPrimary` / `TextSecondary` | Text |
| `HintKeyY` / `HintKeyA` / `HintKeyB` / `HintKeyBack` | Keycap colours (Y amber, A green, B red, back/view faded white) |
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
- [x] Real library via `GameManager.LoadLibrary()` (6 fake games replaced)
- [x] Real Canary profile: gamertag + gamerscore in the header
- [x] Per-game achievement/gamerscore stats from the profile GPD (`TitleEntry`, per-game GPD fallback)

### 5.2 Library carousel
- [x] Alphabetical carousel, sortable with **Y** (selection follows the list index — viewport stays put)
- [x] Card: box art (`CachedBoxart`, bottom-anchored, top-cropped), title, playtime, achievements, gamerscore

### 5.3 Media gallery
- [x] 4-across 16:9 grid, adequate spacing, fits the window width
- [x] Click/Enter → modal viewer
- [x] Faded chevrons in the modal (hide at the ends, brighten on hover)
- [x] B/Escape closes (viewer, then overlay)
- [x] Grid scrolls down, clamped at both ends, no wrap-back
- [x] **Y** sorts: Newest First / Oldest First / By Game (indicator top-right, date from file write time); selection follows the list index

### 5.4 Game launch
- [x] Launch via `Launcher.LaunchGameASync` (Core `Settings`)
- [x] **A**/**Enter** launches (library carousel + dashboard cards)
- [x] Window disabled while running (`EventManager.DisableWindow`), re-enabled on exit
- [x] Library refresh after the session (playtime/last played + card rebuild)

### 5.5 Dynamic background
- [x] `BackgroundArt` from `CachedBackground`
- [x] Dynamic mode with real art (radial fallback when missing)
- [x] Selection in either row drives the art; library selection reveals on overlay close
- [x] Fade-through-black on art swaps (latest-wins); settings changes stay instant

### 5.6 Quit behaviour toggle
- [x] Toggle in Settings ("Return to Xenia Manager on Quit", radio-switch style)
- [x] **On** = returns to Xenia Manager (launches it if not running), then closes BigScreen
- [x] **Off** = closes BigScreen only
- [x] Persisted (`ReturnToXeniaOnQuit`, default true)

### 5.7 Empty states
- [x] No games → disc stub (dashboard row and library screen)
- [x] No screenshots → camera stub (media gallery)
- [x] Dashboard row capped at 6 (fewer games = fewer cards, no empty slots)

### 5.8 SDL3 gamepad input
- [x] `ppy.SDL3-CS` + `AllowUnsafeBlocks` for the pointer API
- [x] `GamepadInputService` (now in Core): graceful init failure, UI-thread poll, deadzone edge detection (no hold repeat); D-pad, left stick and bumpers normalize onto the D-pad values; opens/adds/closes gamepads; raw input traced
- [x] D-pad + A/B mapped to the selection model
- [x] Dashboard **row-state driven** (not keyboard focus): Up/Down switches rows with a fixed column mapping (games 2-3 → option 2, 4-5 → option 3, etc.); A acts on the active row only; options cleared when returning to games; no games = options row stays active
- [x] Left/Right via D-pad, stick and bumpers; Up/Down with row jumps in media
- [x] Settings stays keyboard-only; input gated while the window is disabled (game running)
- [x] Sorting keeps the viewport fixed (selection follows the list index) — library and media

### 5.9 Real controller battery / wifi
- [x] Battery icon from `SDL_GetGamepadPowerInfo` (5s poll; warning when unknown, full when charging, tiers otherwise)
- [x] Wifi icon from `NetworkInterface` (10s poll; `WiFi` / `WiFiOff`)
- [x] Header icons aligned (fixed-size centred boxes)

### 5.10 Final hardening phase
- [x] Manual walkthrough of everything; rewrites where necessary
- [x] Maintainability sweep: deduplication, DRY, SOLID
- [x] Compliance sweep against root `CONTRIBUTING.md`: naming (incl. Hungarian XAML names), AXAML property order, XML docs, logger error handling, formatting, MVVM

### 5.11 Post-sweep engineering
- [x] Command-driven `InputRouter` (key/button → `Command` → per-screen handler; kills duplicated branching)
- [x] DI (`ServiceConfigurator` + `App.Services`); single `IGamepadInputService` singleton (double-instantiation was dropping ~half of gamepad input — fixed)
- [x] Logging density raised (~1/58 lines) with level discipline
- [x] `Constants/` extraction — zero magic values left in C#
- [x] Declaration order, expression bodies, explicit types (repo convention)
- [x] Performance: pre-instantiated overlays, boot-time screenshot scan, first-game pre-selection, pre-warmed background art, viewer↔gallery selection sync, `IsViewerOpen` notification fix

### 5.12 Main app launch button + splash + localization
- [x] Big Screen launch button (main app): nav item + `NavigationService` launch (side-by-side or repo sibling); missing exe → localized warning
- [x] Boot splash: separate window, deferred startup, six staged statuses, tweened bar, 3s minimum, saved-colour background (no green→red flash); input gated until `IsInitialized`
- [x] Localization key set + wiring (main-app naming convention)
- [x] AXAML/style consolidation (shared classes, `HintKeyY/A/B`, `CardShadow` tokens, App.axaml data templates)
- [x] `ArtTile` removed (`GameCard`/`ScreenshotCard` standalone again)
- [x] Stability: `BoxShadows` cast crash, FAAppWindow title bar removed, background-image decode moved behind the splash

### 5.13 Library list view + details pane
- [x] `LibraryViewMode` (Carousel/List) persisted as `library_view_mode`; Settings dropdown + live swap
- [x] `LibraryListItem` rows (disc icon + title, accent on select/hover) with vertical clamped scroll
- [x] `GameDetailsPanel`: disc art, playtime/achievements/gamerscore, DB bio + genre/developer/publisher/released
- [x] Stale-fetch guard (generation counter) + negative cache around `XboxDatabase.GetFullGameInfo`
- [x] `ReleaseDateFormatter` in Core (+ unit tests) — ordinal release dates
- [x] `GamepadButton.View` in Core; **View/V** swaps the view
- [x] Up/Down navigates the list; scroll-to-selected handles both geometries (layout swaps re-center on the selection)

### 5.14 Card image mode + hint bars
- [x] `CardImageMode` (Box Art / Icon) persisted as `card_image_mode`, **default Icon**; Settings dropdown; dashboard cards swap live
- [x] Box Art mode keeps the disc icon as fallback when art is missing
- [x] Library hint bar: Back (B red) → Play (A green) → Sort (Y amber) → Swap View (faded-white `CaretLeft`, `HintKeyBack` token)
- [x] Media hint bar: Back → Select (A) → Sort (Y)

### 5.15 Post-review: housekeeping
- [ ] **T1.** Formatting sweep (JetBrains Rider: Code Cleanup → Reformat on the whole solution; normalizes AI double blank lines). Manual fix of misaligned `en.axaml` entries in both apps (`XeniaManager/Resources/Language/en.axaml` + BigScreen's)
- [ ] **T2.** Delete test artwork (`Resources/Art/fr65z3.jpg`, `Resources/Art/SL1qfH.jpg`)
- [ ] **T3.** Move `Services/ColorJsonConverter.cs` → `Converters/` folder (namespace `XeniaManager.BigScreen.Converters`)
- [ ] **T4.** SplashScreen rewrite — `MainWindow` becomes `FAAppWindow`; boot splash uses FluentAvalonia's built-in `AppWindow.SplashScreen` (`IFAApplicationSplashScreen` + `SplashScreenContent`, `RunTasks` hosts the boot pipeline) per shazzaam7/dotnet-templates pattern; delete `SplashWindow`; fixes the "line at top" splash bug

### 5.16 Controller input: primary controller + rescan (Core)
- [ ] **T5.** `GamepadInputService` (Core): open **all** gamepads; expose `ConnectedGamepads` (name, battery, charging, isPrimary), `SetPrimary(id)`, `Rescan()`; `IsConnected/BatteryPercent/IsCharging` reflect the primary; `StateChanged` fires on connect/disconnect/rescan. Docs read: CONTRIBUTING.md + repo wiki (SDL database / gamecontrollerdb update pattern on the desktop Manage page)

### 5.17 Profiles & identity
- [ ] **T6.** Profile switching — `ProfileService` loads all Canary profiles; selected profile persisted (`profile_xuid` in `dashboard-settings.json`); header gamertag/gamerscore + per-game achievement stats follow the selection
- [ ] **T7.** Manage Profiles popup — port desktop `ManageProfilesDialog` (create / delete / import / export via `ProfileManager`) as a BigScreen overlay

### 5.18 Game actions (right-click menu + popups)
- [ ] **T8.** Game context menu — `ContextFlyout` on `GameCard`/`LibraryCard`/`LibraryListItem` (right-click) + controller path (**X** on the selected card) opening the same full-screen menu: Achievements · Title Updates · Marketplace Content · Screenshots · Patches · Settings
- [ ] **T9.** Achievements popup — port `ContentViewerDialogViewModel` achievements: stats header, All/Unlocked/Locked filter, scrollable rows (name, description, gamerscore, unlock date; image only when unlocked)
- [ ] **T10.** Title Updates popup — `GameContent.InstallerHeaderFiles` (XUID `0000000000000000`) + delete
- [ ] **T11.** Marketplace Content popup — `GameContent.MarketplaceContentHeaderFiles` + delete
- [ ] **T12.** Screenshots popup — per-game gallery from `screenshots/<GAMEID_UPPER>`, reusing `ScreenshotCard` + viewer
- [ ] **T13.** Patches popup — Download (PatchesDatabase + `PatchManager.DownloadPatchAsync`), Modify (PatchFile entries: enable/disable + command edit, port `PatchConfigurationViewModel`), Remove (`PatchManager.RemovePatchAsync`)
- [ ] **T14.** Game Settings popup — port config editor (`ConfigEditorViewModel` + `ConfigUiSettings.AllSettings`) into an overlay; saves to `Game.FileLocations.Config`
- [ ] **T15.** Disc selection popup on launch — when `IsMultiDisc`: full-screen disc list (labels, "Last Played" marker), A launches / B cancels (mirror `DiscSelectionDialog`)

### 5.19 Dashboard, header & settings
- [ ] **T16.** Time format setting — 12h/24h (persisted in `DashboardSettings`); clock + capture dates follow it
- [ ] **T17.** Controllers section (Settings) — detected gamepads, battery %, primary selection, **Rescan** button
- [ ] **T18.** Settings controller navigation — rows focusable; Up/Down moves, A activates (ComboBox opens, checkbox toggles, button clicks, palette opens), Left/Right adjusts sliders; `InputRouter.HandleOverlay` extended
- [ ] **T19.** Media hub — Media becomes two entries: **Screenshots** (existing gallery) + **Installed Content** (per-game browser → title updates / marketplace, XUID `0000000000000000`)
- [ ] **T20.** Boxart full width/height — in `CardImageMode.BoxArt` show full box art (no 13% top crop); Icon-mode fallback unchanged
- [ ] **T21.** Header — network icon adapts to ethernet (`Ethernet`/`Wifi`/`WifiOff`); battery **% text below** the battery icon
- [ ] **T22.** Background type default → **Dynamic** (`DashboardSettings.Mode` + settings default)
- [ ] **T23.** Xenia version indicator — per-game badge/icon on library cards, list rows + details (reuse Core `XeniaVersionToIconConverter`/`XeniaVersionToStringConverter`)
- [ ] **T24.** Game compatibility indicator — colored rating dot + label on cards (Core `CompatibilityRatingColorConverter`), DB URL as tooltip
- [ ] **T25.** List-view achievements — scrollable locked/unlocked achievement section in the list-view details pane
- [ ] **T26.** XConfig editor — port `EditXConfigDialog` (language, country, AV HDMI size, default profile) via `XConfigManager` as a Settings overlay (hidden when `XConfigExists` is false)
- [ ] **T27.** Input gating while a game runs — gamepad already gated by `IsEnabled`; add the same gate to `OnWindowKeyDown`, `OnCardGotFocus`, `OnOptionCardPressed`

### 5.20 Desktop app integration
- [ ] **T28.** Hide + disable main window when BigScreen opens — `NavigationService.LaunchBigScreen` keeps the `Process` handle, `EventManager.DisableWindow()` + `Hide()`; restore (`Show()` + `EnableWindow()`) on BigScreen exit
- [ ] **T29.** BigScreen start by default — `--bigscreen` CLI arg (desktop `Program.cs` launches BigScreen and hides the main window) + Settings toggle "Start in Big Screen" (persisted)

### 5.21 MediaSort expansion (full desktop parity)
- [ ] **T30.** Expand `MediaSort` with the desktop app's full `GameSortOption` list, sorted by each screenshot's owning game (resolved from the parent folder's game ID): **Title** (alphabetical), **Time Played**, **Compatibility**, **TitleId**, **MediaId**, **XeniaVersion**, **Last Played** — alongside the existing **Newest First / Oldest First / By Game**. Y still cycles; indicator + selection-preserving resort keep working (`MediaViewModel.ApplyMediaSort`)

---

## 5.22 Extra info & conventions (T1–T30)

> **Notes & conventions for the tasks above**
> - All new user-facing strings go into `en.axaml` (BigScreen + desktop), keys follow the existing naming convention; other languages deferred per spec §1.
> - Follow `CONTRIBUTING.md` throughout: XML docs, `Logger` usage (no silent catches), Hungarian XAML names, 4-space indent, AXAML property order, file-scoped namespaces, MVVM (thin code-behind).
> - Cross-cutting behaviour: mouse right-click **and** controller (X button) must reach the same game-action menu; all popups are full-screen overlays consistent with the existing screen system (visibility-flip, `InputRouter` commands, `InputHint` bars); popups exit with B/Escape.
> - Core work first (`GamepadInputService`, any shared helpers), then BigScreen, then the desktop app.
> - Verification per task: `dotnet build "Xenia Manager.sln"`, `dotnet test tests/XeniaManager.Tests`, manual smoke run (launch, right-click menu, popups, controller input).
> - Artwork/icon choices reuse FluentIcons + existing token dictionary (`DarkGradient.axaml`); no new colors without adding tokens.
> - This roadmap supersedes/supplements §5.14 items; update §3 architecture tree (`Services/`, `Controls/`, `Views/`) as new files land.

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
