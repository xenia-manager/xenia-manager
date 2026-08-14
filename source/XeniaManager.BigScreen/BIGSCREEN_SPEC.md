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

### Dashboard shell (`Views/Shell/MainWindow.axaml`)
- **Header:** avatar icon (PersonCircle), gamertag from the **Canary profile** (`ProfileManager.LoadProfiles(XeniaVersion.Canary)` + profile GPD), gamerscore as an `IconStat` (Star), **live** Wi-Fi + controller battery icons (10s/5s polls), live clock (1s `DispatcherTimer`).
- **Game card row:** **max 6 recent games** (`RecentGames`, its own VM instances — independent selection from the library). Each `GameCard` shows its **box art or disc icon** per the `card_image_mode` setting (default Icon; Box Art mode is a **portrait tile filled bottom-anchored by the box art with the top ~12% cropped** — falls back to the icon when art is missing; Icon mode is a zoom-to-fill square disc); the selected card grows (200→250 wide, height follows the mode) and shows the title bar. A transparent `BorderOverlay` larger than the card carries **all** border strokes (inactive `CardBorder`, hover/selected accent) so nothing sits on top of the art edges.
- **Option card row:** `Library` · `Gallery` · `Settings` · `Quit`. Hover shows the accent border; selection is driven by `IsSelected` (controller focus), not by keyboard focus.
- **Fullscreen:** plain `Window` (no title bar) with `WindowState=FullScreen` **forced in code** at construction and before `Show()` — the XAML attribute is not reliable on the DI creation path; 1920×1080 fallback, centered column layout so header aligns with content rows.

### Overlay screens
Full-screen pages rendered over the dashboard; **Enter/click** opens, **B/Escape** closes, focus returns to the option row. Bottom hint bars use the `InputHint` control (coloured circle keycap + label).
- **Library** — all games in a horizontal **carousel** (`LibraryCard`s: box art with a **13% top crop** (bottom-anchored, ~366px art region), title, playtime row, achievements/gamerscore row from the profile GPD) or a vertical **list** with a details pane (`LibraryListItem` + `GameDetailsPanel`). Left/Right iterates (clamped at both ends — no wrap), the row scrolls once the selection passes the middle; in list mode Up/Down iterates. **X** cycles the sort (Alphabetical → Time Played → Last Played; indicator top-right via `IconStat`). **View/V** swaps the layout (Carousel ↔ List, persisted via `library_view_mode`); the details pane shows marketplace DB info (bio, genre, developer, publisher, release date — loading/no-info states, stale-fetch guard + negative cache). Disc stub shown when the library is empty.
- **Gallery** — screenshot gallery scanned from `Emulators/Xenia Canary/screenshots/**` (recursive, common image extensions), 4-across 16:9 grid that scrolls down (clamped at both ends — no wrap-back), **X** cycles the sort (Newest First / Oldest First / By Game; indicator top-right via `IconStat`, **capture date decoded from the file name** (`{GAMEID} - {yyyy-MM-ddTHH-mm-ss}`, write time as fallback — `ScreenshotFileNameParser`)). Click/Enter → **screenshot viewer modal** (on the modal stack — see the viewer section below). Camera stub shown when the gallery is empty. Hints: Back · Select (A) · Sort (X).
- **Settings** — background type dropdown, library view dropdown, **card image dropdown** (Box Art / Icon, default Icon), primary/accent colour fields (swatch + hex + palette popup), vignette slider, background image picker.
- **Quit** — closes the app.
- **Hint bars** — order per screen: Library = Back (B red) → Play (A green) → Sort (X blue) → Details (Y amber) → Swap View (faded-white `CaretLeft`, `HintKeyBack` token); Gallery = Back → Select (A) → Sort (X blue); Settings = Back/Close. **One hint bar at a time:** only the top modal's hint bar shows (`ModalViewModelBase.IsHintBarVisible`), and the overlay screens hide theirs while any modal is open.

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

### Boot splash (Feature 3, FA built-in)
- **FluentAvalonia's built-in `AppWindow.SplashScreen`** (`IFAApplicationSplashScreen`): `MainWindow` is an `FAAppWindow` with `SplashScreen = new AppSplashScreen()`; FA shows the splash, runs `RunTasks` (the boot pipeline, dispatched onto the UI thread), then reveals the fullscreen dashboard. No separate splash window — the old standalone `SplashWindow` (and its top-line chrome bug) is gone.
- The splash window FA creates is **forced fullscreen + borderless** from `SplashScreenView.OnAttachedToVisualTree` (it is centered by default).
- Content: TV logo + "Xenia Big Screen" + live status text + tweened progress bar, over the **dashboard's radial background** built by `BackgroundBrushFactory.CreateRadial` from the saved `primary_color`; logo + bar use the saved `accent_color` so the splash matches the dashboard — no green→red flash.
- **Boot pipeline** (`MainWindowViewModel.InitializeAsync`, cancellable, per-stage minimum 400ms dwell): Loading Settings (settings JSON + background brush/image decode) → Loading Profile (profile + header) → Loading Dashboard (library JSON + recent cards) → Loading Library (per-game GPD stats off-thread + cards, chunked progress) → Loading Game Data (per-game content/patch/achievement-GPD preload + marketplace details, one stage, chunked progress, per-step timing logs) → Loading Gallery (screenshot scan in `Task.Run`) → Loading Done (1s hold). Total minimum ~3s (`MinimumShowTime` 2s + dwells). Config files are deliberately **not** preloaded — they're the costliest step and only the game settings pane uses them, so they load lazily on the pane's first open.
- Input (keyboard, gamepad, mouse activation) is **gated until `IsInitialized`** — no stray input can act during the splash.
- Boot failures log and the window still reveals.

### Main app integration (Feature 1)
- **"Big Screen" nav button** in Xenia Manager (`MainView.axaml`, `Tv` icon, tooltip "Open Big Screen") → `NavigationService` `BigScreen` tag → launches `XeniaManager.BigScreen.exe` resolved **side-by-side or via the repo-sibling bin folder** (same config, matching `BaseAppLocator`); missing exe → localised warning box.

### Theming
- `Resources/Themes/DarkGradient.axaml` — token dictionary: `CardBackground`, `CardTitleBar`, `CardBorder`, `AccentColor`, `TextPrimary/Secondary`, `HintKeyX/Y/A/B/Back`, `CardShadow`/`CardShadowSelected` (`BoxShadows`!), `SystemAccentColor*` (Colour-typed variants), slider/control overrides, accent-fill family.
- `Resources/Themes/Controls.axaml` — `ControlTheme`s for the custom controls (`ColorPickerField`, `PalettePicker`).
- `Resources/BigScreenStyle.axaml` — global styles (cards, shared `.screen-title`/`.card-title`/`.hint-bar`/`.empty-state` classes, text halos).
- `Resources/Language/en.axaml` — **full key set for every user-facing string** (main-app naming convention), wired via `{DynamicResource}` in XAML and `LocalizationHelper.GetText` in code; `LocalizationHelper.Initialize(...)` in `App.axaml.cs`. Other languages = new files only.
- FluentAvalonia theme (`FluentAvaloniaTheme`) like the main app.

### Custom controls (`Controls/Cards|Modals|Profiles|Settings|Primitives|Splash/`)
- **`ColorPickerField`** — swatch + hex text box; clicking the swatch opens a palette popup. Two palettes: muted slates/greys (primary) and 10 accent colours incl. white/light grey.
- **`PalettePicker`** — horizontal StackPanel of colour swatches (background card, spacing, margin); raises `SelectedColorChanged`.
- **`IconStat`** — icon + text row (`Icon` Symbol, `Stat`, `IconSize`, `FontSize`, `Spacing`, `IconRotation`). Used in the header (gamerscore), library sort indicator, and library cards.
- **`InputHint`** — keycap + label (`KeyColour`, `Icon`/`Char` glyph, `Text`): transparent circle with coloured 2px outline, glyph coloured to match, white label. Xbox-standard colours per usage (X blue, Y amber, A green, B red; the back/view button uses a faded-white `CaretLeft` via `HintKeyBack`).
- **`LibraryCard`** — carousel card: box art (bottom-anchored, top 13% cropped, rounded clip via `RectangleGeometry`), title, playtime row, achievements/gamerscore row; `CardBorder` 2px inactive → accent on selection (outer border only).
- **`LibraryListItem`** — list-view row: disc icon + title, accent border on selection/hover.
- **`GameDetailsPanel`** — list-view details pane: disc art, playtime/achievements/gamerscore, marketplace DB bio + metadata strip (genre, developer, publisher, released), loading bar / no-info states.
- **`GameCard`** — dashboard game tile: box art or disc icon per `card_image_mode` (rounded clip; Box Art mode is a portrait tile filled bottom-anchored with the top ~12% cropped, Icon mode a square fill) + title bar on selection, border overlay strokes; grows on selection (200→250 wide, height follows the mode).
- **`ScreenshotCard`** — gallery tile: 16:9 screenshot with 6px rounded clip, title bar on selection, border overlay strokes.
- **`OptionsCard`** — dashboard option tile.

### Settings persistence
`DashboardSettings` JSON via `BackgroundService` (`Load`/`Save`), `ColorJsonConverter` (ARGB hex) for `Color` values.

### Modal system (push/pop stack with dispose)
- **`IModalService`/`ModalService`** (DI singleton) — full-screen modal stack: `ShowAsync` (with/without a typed result), `Close` pops the top modal, disposes `IDisposable` VMs and raises `StackChanged`; modals nest naturally (a modal can await another on top of itself). Modals are created fresh per open — no state carries, no stale subscriptions.
- **`ModalViewModelBase`/`ModalViewModelBase<TResult>`** — result delivery, `HandleInput(NavigationCommand)` (virtual; base closes on Back), `Dispose()` hook, own close `Task`.
- **Modal layer** (MainWindow, sibling of the content grid) — full-window transparent black backdrop (`ModalBackdrop`, 30% `#4D000000`) + **`ModalHost`**, which renders the **whole stack bottom→top as layered entries** (later modals overlay earlier ones — a confirmation sits on top of the view beneath it); only the top entry is hit-testable. The backdrop hides while the **screenshot viewer modal** is the top entry (its own opaque backdrop covers the window). When the stack empties, the screen under the first modal is **restored** (`_screenBeforeModal`, e.g. a game modal opened from the library lands back in the library, not the dashboard).
- **`ConfirmationModal`** (`Controls/`) — reusable prompt: header, message and **two controller-friendly option buttons** (Left/Right selects, A activates the selection, B cancels); resolves `bool?` — option 1 `true`, option 2 `false`, B `null` (callers decide what cancel means, e.g. "stay put"); fixed 640×300 card centered on its **own 30% black backdrop filling the content area**; card/buttons use the standard card surfaces (`CardBackground`, `CardBorderInner` borders, accent on selection). Reused by Manage Profiles delete / import-replace / export-saves / unsaved-changes prompts.
- Router dispatch order: **modal stack (top modal) → overlay screens → dashboard**; modals swallow all input while open. The router is command-driven: key/gamepad → `NavigationCommand` → per-layer handlers (`HandleLibrary`/`HandleGallery`/`MoveUp`/`MoveDown`/…).

### Game modal (T8–T14)
- **`GameModalViewModel`/`GameModalView`** — opened from any game card (**Y** = Details or right-click): a `modal-screen` card with the game's icon + title above a vertical options list (Achievements · Screenshots · Title Updates · Marketplace Content · Patches · Settings). The selected option's **pane renders on the right and updates live as the selection moves** (panes are created once and cached per option — no reloads when navigating back). A/Right enters the pane, B/Left returns to the options list, B there closes the modal. **Exactly one element highlights at a time** — a state contract, not styling: entering a pane clears the nav selection and calls `IGameModalPane.OnPaneEntered()` (selects the pane's first item); exiting calls `OnPaneExited()` (clears the pane) and re-selects the nav option.
- **Panes** (`ViewModels/Modals/`, all implementing `IGameModalPane.HandleInput`):
  - **Achievements** — stats header (Trophy + Star `IconStat`s), X-cycled sort (Achieved default → Gamerscore Awarded → Alphabetical), scroll-to-selected on Up/Down, rows with image only when unlocked (spoiler guard), empty state. Rows show the gamerscore as a star icon + number (no box). Panes start **unselected** — `SelectionHelper.MoveSelection` picks the first item on the first move.
  - **Screenshots** — the game's own folder (`{EmulatorDir}/screenshots/{GAMEID}`) as a 4-across grid; **Canary games reuse the boot-time gallery cache** (already decoded — no re-scan), other versions scan off-thread with a loading state; Left/Back return to the nav list; A opens the shared screenshot viewer modal.
  - **Title Updates / Marketplace Content** — one shared pane initialised per menu entry (single type — no switching), rows with display name + file name, A deletes (confirmation modal, package + `.header` removed), empty state; A is blocked when nothing is installed.
  - **Patches** — installed patch entries (A toggles enabled, instant save), Right opens the command editor (per-type validated Type/Address/Value fields, add/delete commands), pinned Remove action (confirmation); "Download New Patch" opens the download modal (X shortcut removed — the row is the only path).
  - **Game Settings** — full config editor port: sections from `ConfigUiSettings.AllSettings` (335 keys now in `en.axaml`), all five control types via `ConfigOptionRow` (toggle/slider/numberbox/combobox/textbox), controller row navigation (A focuses the editor control), X saves, unsaved-changes prompt on exit (Save/Discard/Cancel). **Virtualized**: a single `ListBox` over a flattened `Items` list (section headers + option rows) so only visible rows instantiate — the pane opens instantly; the config file itself loads lazily from `GameDataCache` on first open (never at boot). A **settle pass** (`MarkAllAsSaved` on a background-priority post) plus compare-first value guards mean a fresh pane never prompts about phantom changes.
- **Hint bars** — the game modal's hint bar is fully dynamic: B = Back always, A = `AHintText` per column (Select nav / Select achievements / View screenshots / Delete content / Toggle patches / Edit settings), X = `XHintText` where a pane has an X action (Sort achievements / Save settings). The patch download modal shows B Back · A Download.
- **Patch download modal** (`PatchDownloadViewModel`/`PatchDownloadView`) — dedicated modal: search prefilled with the game ID, generation-guarded Canary/Netplay results with source badges, A/Tap downloads (`PatchManager.DownloadPatchAsync`) and closes on success; visible Searching… / No patches found / failure status. The patches pane refreshes the patch cache and reloads its list when it closes.
- **Screenshot viewer modal** (`ScreenshotViewerViewModel`/`ScreenshotViewerView`, in `Views/Modals/`) — the full-window viewer (uniform-stretched image, faded chevrons that hide at the ends, caption with game title + capture date) now lives on the modal stack and is opened from **both** the gallery and the game modal's screenshots pane. It is the **only modal that ignores the modal backdrop layer** — `ModalBackdropVisible` (top modal is not the viewer) hides the 30% scrim so its own opaque `ViewerBackdrop` renders exactly as before. Left/Right steps, B closes.

### Disc selection modal (T15)
- **`DiscSelectionViewModel`/`DiscSelectionView`** — shown by `MainWindowViewModel.LaunchGame` when `IsMultiDisc` (before the window is disabled, so the modal stays interactive): a compact centered `modal-card` (880×460) over its own `ModalCardScrim` backdrop — game icon + name header with a "Disc Selection" line beneath, then **one `DiscOptionCard` per disc** in a centered horizontal row (custom `Label` or "Disc N", filled disc icon drawn from basic geometry in `TextSecondary` — accent when selected, "Last Played" / "File Missing" status line; missing files dimmed). Only the disc cards take input: Left/Right moves (skipping missing files, clamped), **A or click launches the selected disc** (`Close(discNumber)`), **B cancels** (`Close(null)` — `LaunchGame` aborts). Initial selection = last played disc (first valid fallback). Typed modal result via `ModalViewModelBase<int?>` + `ModalService.ShowAsync<int?>`. Hints: B Back · A Select.

### Game data cache
- **`GameDataCache`** — session-long per-game cache populated behind the splash in the **Loading Game Data** boot stage: installed content scans (`GameContent`), patch files (`PatchFile` + path) and achievement GPDs (`GpdFile`, active profile). **Config files are excluded from boot** (costliest step, ~120ms/game, only the settings pane needs it) — `GetConfig` loads lazily on the pane's first open and is cached after. Marketplace details preload (`LibraryViewModel.PreloadDetailsAsync`) runs inside the same boot stage (no separate splash step).
- **Per-step timing logs** — `Preloaded '{title}' - content Xms, patch Xms, GPD Xms, total Xms` per game, `Game data preloaded for N games in Xms` for the stage, `Details for '{title}' in Xms` + `Details preloaded for N games` for the details pass.
- **Edit-sync (the cache is the single source of truth and BigScreen is the only editor):** settings saves mutate the cached `ConfigFile` instance in place; settings discard → `ReloadConfig`; content delete → `RefreshContent` (re-scan); patch download/remove → `RefreshPatch` (re-load); profile switch → `ClearAchievementGpds` (dispose + clear). Panes read only from the cache — ctors do no I/O.
- **Screenshots pane** reuses the gallery's boot-time items for Canary games (same decoded bitmaps — no duplicate decode).

### Profiles & identity
- **ProfileService** loads **all** Canary profiles (`Profiles`); the active one is persisted as `profile_xuid` (hex PathXUID) and restored at boot (falls back to the first); `SwitchProfile` re-loads the profile GPD, saves and raises `ProfileChanged` (header identity + per-game achievement stats rebuild); `Refresh()` re-scans after external changes (Manage Profiles), keeping the active profile or falling back to the first.
- **Boot reorder:** Settings stage now runs before the Profile stage (the persisted XUID is needed to pick the profile).
- **Header `ProfileButton`** (avatar chip) — focusable + clickable; controller **Up** from the game row selects it (**Down** returns to game card 1); reserved 2px accent outline on selection/hover (`IsSelected` on `HeaderViewModel`).
- **Profile picker modal** (`ProfilePickerView`) — opened from the header chip: all profiles (active first, then alphabetical) with gamertag, country · language and per-profile gamerscore; Up/Down + A switches + B closes, **Y opens Manage Profiles** on top of the picker; "no profiles" stub.
- **Manage Profiles modal** (`ManageProfilesView`) — opened from a Profiles card in Settings or via Y in the picker: profile rows (scrollable) + an anchored **Create New Profile stub row** beneath; full desktop-dialogue port (create / delete / import (`ProfileManager.ImportProfileWithReplacement` + replace-confirm, `.xaccount`/`.zip` picker) / export (save picker + "include saves?" confirm) / edit gamertag (regex + 15-char validation, inline error) + country/language/subscription combos + Xbox Live toggle / Save (`SaveProfiles`)); edit panel hidden while the stub is selected; **unsaved edits prompt Save / Discard on exit and row switches (B cancels, staying put)**; deleting the active profile falls back to the first and refreshes the header/stats. Hints: B Back · X Delete · View Import · Start Export.

---

## 3. Architecture

```
source/XeniaManager.BigScreen/
├── App.axaml / App.axaml.cs        # Application shell, theme wiring, localization init, DI container (App.Services) + MainWindow resolution
├── Program.cs                       # Entry point; redirects base dir to the base app's folder
├── ViewLocator.cs                   # VM → View resolution (ViewModels.XViewModel → Views.XView)
├── Views/
│   ├── Shell/
│   │   └── MainWindow.axaml(.cs)      # Shell (FAAppWindow, fullscreen forced in code, WindowDecorations=None): header, background/fade layers, dashboard + overlay screens, input routing, built-in splash
│   ├── Dashboard/
│   │   └── DashboardView.axaml(.cs)   # Recent games row + options row + empty stub
│   ├── Screens/
│   │   ├── LibraryView.axaml(.cs)     # Library carousel + list, clamped scroll, details pane + empty stub
│   │   ├── GalleryView.axaml(.cs)     # Gallery grid + empty stub (viewer lives in Modals now)
│   │   └── SettingsView.axaml(.cs)    # Settings screen (owns the background image picker)
│   └── Modals/
│       ├── GameModalView.axaml(.cs)   # Game modal: icon+title, options list (left), live panes (right)
│       ├── AchievementsPaneView.axaml(.cs)   # Achievements pane (stats, sort, scrollable rows)
│       ├── GameScreenshotsPaneView.axaml(.cs) # Per-game screenshot grid pane
│       ├── InstalledContentPaneView.axaml(.cs) # Title Updates / Marketplace rows + delete
│       ├── PatchesPaneView.axaml(.cs) # Patches pane (entries + command editor + remove)
│       ├── GameSettingsPaneView.axaml(.cs)    # Config editor pane (sections + five control types)
│       ├── PatchDownloadView.axaml(.cs)       # Patch download modal (search + results + status)
│       ├── ScreenshotViewerView.axaml(.cs)    # Full-window screenshot viewer modal (chevrons, caption)
│       ├── DiscSelectionView.axaml(.cs) # Disc selection modal (compact card: game header + disc cards)
│       ├── ProfilePickerView.axaml(.cs) # Profile picker modal (A switches, Y = Manage, B closes)
│       └── ManageProfilesView.axaml(.cs) # Manage Profiles modal (rows + anchored create stub + edit panel)
├── ViewModels/
│   ├── Shell/
│   │   └── MainWindowViewModel.cs     # Composition root: child VMs, CurrentScreen navigation, launch/quit/refresh, IsModalOpen
│   ├── Dashboard/
│   │   ├── HeaderViewModel.cs         # Profile, clock, wifi + controller battery state (+ IsSelected for the avatar chip)
│   │   └── DashboardViewModel.cs      # RecentGames, Options, background brush + fade-through-black
│   ├── Screens/
│   │   ├── LibraryViewModel.cs        # Games carousel + sort (ScreenViewModel base)
│   │   ├── GalleryViewModel.cs        # Screenshots + sort (ScreenViewModel base)
│   │   ├── SettingsViewModel.cs       # Appearance options + persistence + quit toggle + library view + card image + Manage Profiles entry
│   │   └── ScreenViewModel.cs         # Base for overlay screens: ScreenBackground brush + hint-bar visibility
│   ├── Modals/
│   │   ├── ModalViewModelBase.cs(.Generic.cs) # Modal lifecycle: close TCS, HandleInput (Back closes by default), Dispose hook, IsHintBarVisible (top modal only); generic result delivery
│   │   ├── IGameModalPane.cs         # Pane contract: HandleInput(NavigationCommand)
│   │   ├── GameModalViewModel.cs     # Game modal: options list + cached panes, live display on navigation, pane input routing, single-highlight state
│   │   ├── AchievementsPaneViewModel.cs  # GPD achievements: stats, sort (Achieved/Gamerscore/Alphabetical), scroll event
│   │   ├── GameScreenshotsPaneViewModel.cs # Per-game screenshot scan (off-thread) + grid; viewer opens as modal
│   │   ├── InstalledContentPaneViewModel.cs # Title Updates / Marketplace rows + confirmed delete
│   │   ├── PatchesPaneViewModel.cs   # Patch entries (toggle/edit/remove), commands mode + editor
│   │   ├── PatchDownloadViewModel.cs # Download modal: search (generation guard), results, status, download-on-activate
│   │   ├── GameSettingsPaneViewModel.cs # Config editor: sections, controller nav, save/discard + exit prompt
│   │   ├── ConfigOptionViewModel.cs / ConfigSectionViewModel.cs # Config editor ports (+ ComboBoxOptionViewModel, IsSelected for nav)
│   │   ├── ScreenshotViewerViewModel.cs # Full-window viewer modal (step, caption); no modal backdrop
│   │   ├── DiscSelectionViewModel.cs # Disc selection modal (typed int? result; skip-missing navigation)
│   │   ├── ConfirmationModalViewModel.cs # Reusable 2-option prompt (Left/Right + A, B cancels → null; true/false/null result)
│   │   ├── ProfilePickerViewModel.cs  # Picker modal: rows, switch-active, Y → Manage Profiles
│   │   └── ManageProfilesViewModel.cs # Manage modal: rows + stub selection, edit fields + dirty tracking, create/delete/import/export, unsaved prompts
│   ├── ViewModelBase.cs
│   └── Items/
│       ├── GameCardViewModel.cs       # Core Game ref, Title, Boxart/DiscArt layers (card_image_mode), stat strings, IsSelected, BackgroundArt
│       ├── GameDetailsViewModel.cs    # Details pane: local card stats + DB info (bio/genre/developer/publisher/released)
│       ├── ScreenshotItemViewModel.cs # Path, Title, CapturedAt (+ text), GameTitle, Image, IsSelected
│       ├── OptionsCardViewModel.cs    # Title, Icon, TargetScreen
│       ├── GameActionItemViewModel.cs # Game modal option row: Title, Icon, GameModalPane, IsSelected
│       ├── AchievementItemViewModel.cs # Achievement row: name/description/gamerscore/date, image only when unlocked
│       ├── ContentItemViewModel.cs    # Installed content row: HeaderFile + reconstructed delete path
│       ├── PatchCommandItemViewModel.cs / PatchEntryItemViewModel.cs # Patch editor ports (validated values, ToPatchEntry/ToPatchCommand)
│       ├── PatchDownloadItemViewModel.cs # Download result: name + Canary/Netplay source badge
│       ├── PatchListRowViewModel.cs   # Patches list row: patch entry or download/remove action (+ PatchActionType enum)
│       ├── ProfileItemViewModel.cs    # Profile row: gamertag, country · language, gamerscore (loaded async), IsSelected/IsActive
│       ├── CreateProfileStubViewModel.cs # The anchored "Create New Profile" row (ISelectable)
│       └── DiscOptionItemViewModel.cs # Disc card row: label, last-played/missing status, IsSelected (ISelectable)
├── Controls/
│   ├── Cards/
│   │   ├── GameCard.axaml(.cs)        # Dashboard game tile: box art + title bar on selection (grow 200→250)
│   │   ├── OptionsCard.axaml(.cs)     # Dashboard option tile
│   │   ├── ScreenshotCard.axaml(.cs)  # Gallery tile: 16:9 screenshot, 6px corners
│   │   ├── LibraryCard.axaml(.cs)     # Carousel card: box art + title + stat rows (rounded art clip)
│   │   ├── LibraryListItem.axaml(.cs) # List row: disc icon + title (accent on select/hover)
│   │   ├── GameDetailsPanel.axaml(.cs) # Details pane: art, local stats, DB bio + metadata strip
│   │   ├── GameActionRow.axaml(.cs)   # Game modal option row (icon + title, accent on select/hover)
│   │   ├── DiscOptionCard.axaml(.cs)  # Disc selection card (filled disc icon + label + status line, dimmed when missing)
│   │   ├── AchievementRow.axaml(.cs)  # Achievement row (image/lock icon, name, description, star + gamerscore)
│   │   └── ContentRow.axaml(.cs)      # Installed content row (display name + file name + delete icon)
│   ├── Modals/
│   │   ├── ModalHost.axaml(.cs)       # Renders the modal stack bottom→top (later entries overlay; only the top gets input)
│   │   └── ConfirmationModal.axaml(.cs) # Generic prompt: header, message, two Left/Right option buttons (accent on selection)
│   ├── Profiles/
│   │   ├── ProfileButton.axaml(.cs)   # Header avatar chip: focusable, accent outline on selection/hover, opens the picker
│   │   ├── ProfileRow.axaml(.cs)      # Profile list row: avatar, gamertag, country · language, gamerscore
│   │   └── CreateProfileRow.axaml(.cs) # "+ Create New Profile" row (anchored beneath the list)
│   ├── Settings/
│   │   ├── GamepadCard.axaml(.cs)     # Settings controller row: name, status text, battery icon + % (accent on hover/select)
│   │   ├── ConfigOptionRow.axaml(.cs) # Config option row: label/comment + editor (toggle/slider/numberbox/combo/textbox)
│   │   ├── ColorPickerField.cs        # Swatch + hex + palette popup
│   │   └── PalettePicker.cs           # Swatch row
│   ├── Primitives/
│   │   ├── IconStat.axaml(.cs)        # Icon + text stat row
│   │   └── InputHint.axaml(.cs)       # Keycap + label hint
│   └── Splash/
│       ├── SplashScreenView.axaml(.cs) # FA built-in splash visuals: logo, live status, tweened bar, radial background from saved primary/accent; forces the splash window fullscreen
│       └── AppSplashScreen.cs         # IFAApplicationSplashScreen: hosts the splash view + runs the boot pipeline (RunTasks) on the UI thread
├── Converters/
│   └── ColorJsonConverter.cs        # ARGB hex JSON converter for Color values
├── Factories/
│   ├── BackgroundBrushFactory.cs    # Static brush builders: linear/radial/solid from a colour + vignette; single home for Mix math and stop offsets
│   └── IconFactory.cs               # Status-to-icon mapping: tiered battery (Battery0-10 / BatteryCharge0-10 / BatteryWarning) + network (WiFi / PlugConnected / WiFiOff)
├── Services/
│   ├── BaseAppLocator.cs            # Resolves the base Xenia Manager folder (--base-dir / side-by-side / sibling)
│   ├── BackgroundService.cs         # Settings load/save, brushes via BackgroundBrushFactory, ApplyResources (IBackgroundService)
│   ├── GameDataCache.cs             # Session cache: content scans, patch files, achievement GPDs (+ lazy configs); PreloadGame with per-step timing logs; edit-sync refresh/clear APIs
│   ├── DashboardNavigationController.cs # Row state machine (incl. profile row), selection movement, option activation (view focus/scroll via events)
│   ├── GameLibraryService.cs        # Wraps Core GameManager: load, game list, recent-games selection (IGameLibraryService)
│   ├── IBackgroundService.cs / IGameLibraryService.cs / IProfileService.cs / IScreenshotLibraryService.cs / IModalService.cs
│   ├── InputRouter.cs               # Command-driven: key/gamepad → NavigationCommand → dispatcher (modal stack → viewer → overlay → dashboard)
│   ├── ModalService.cs              # Push/pop modal stack: ShowAsync (typed result), Close pops + disposes, StackChanged (IModalService)
│   ├── ProfileService.cs            # All Canary profiles, active profile (persisted profile_xuid), switch/refresh, per-game achievement/GPD stats, per-profile gamerscore (IProfileService)
│   ├── ScreenshotLibraryService.cs  # Recursive screenshot scan, extension filter, game-title matching, filename-decoded metadata (IScreenshotLibraryService)
│   └── ServiceConfigurator.cs       # DI registration (mirrors the main app: singleton services + VMs, App.Services)
├── Constants/
│   ├── AppConstants.cs              # BaseAppExecutable, RecentGamesLimit, SettingsFileName
│   ├── TimingConstants.cs           # Gamepad/battery/wifi/clock polls, fade, splash stage/done/minimum timings
│   ├── FormatConstants.cs           # Clock/capture-date formats, screenshot file-name timestamp format, achievement unlock format, XUID format
│   ├── XboxConstants.cs             # ProfileContentTitleId (FFFE07D1)
│   └── LayoutConstants.cs           # Vignette step, gradient mixes, accent tint step, carousel fallbacks
├── Utilities/
│   ├── SelectionHelper.cs           # ISelectable + single-selection helpers (move/select/resort-preserving; MoveSelection picks the first from nothing)
│   ├── EnumCycleHelper.cs           # Generic enum + colour palette cycling
│   ├── ScreenshotFileNameParser.cs  # Decodes game ID + capture timestamp from Xenia screenshot file names ("{GAMEID} - {yyyy-MM-ddTHH-mm-ss}")
│   ├── ImageFormats.cs              # Shared screenshot extensions + file-picker patterns
│   ├── ProfileRowsHelper.cs         # Shared profile row building (active-first + alphabetical) + async gamerscore loading
│   ├── TaskUtilities.cs             # RunSafely<T>: logged fire-and-forget task execution
│   └── AccountInfoExtensions.cs     # PathXuidText() hex helper
├── Models/
│   ├── Settings/
│   │   ├── DashboardSettings.cs      # Persisted user-facing options (incl. primary_controller_guid, profile_xuid, time_format)
│   │   ├── BackgroundMode.cs / BackgroundModeOption.cs
│   │   ├── LibraryViewMode.cs / LibraryViewModeOption.cs
│   │   ├── CardImageMode.cs / CardImageModeOption.cs
│   │   └── TimeFormat.cs / TimeFormatOption.cs
│   ├── NavigationCommand.cs         # Public command set (Move/Activate/Back/CycleSort/ToggleView/Start/Details)
│   ├── GameModalPane.cs             # The six game modal panes (Achievements…Settings)
│   ├── AchievementSort.cs           # Achieved / GamerscoreAwarded / Alphabetical
│   ├── GameStatInfo.cs              # Achievement/gamerscore counters (unlocked / total)
│   ├── LibrarySort.cs               # Alphabetical / TimePlayed / LastPlayed
│   ├── GallerySort.cs               # NewestFirst / OldestFirst / ByGame
│   ├── NetworkStatus.cs             # Disconnected / Wifi / Ethernet (header network icon)
│   └── OverlayScreen.cs             # GamepadButton moved to Core (Models/GamepadButton.cs)
└── Resources/
    ├── BigScreenStyle.axaml        # Shared card/screen-title/hint-bar/empty-state styles
    ├── Language/en.axaml           # Full key set for every user-facing string (+ Core playtime keys)
    ├── Themes/DarkGradient.axaml · Controls.axaml
    └── Art/                          # Sample wallpapers for testing
```

**Navigation:** the shell hosts the dashboard as a `ContentControl` (`MainWindowViewModel.Dashboard`) and the three overlay screens as pre-instantiated `ContentControl`s whose content never changes (`MainWindowViewModel.Library` / `Gallery` / `Settings`, visibility flipped via `Is*Screen`). Views are created **once at startup** — opening a screen is a pure visibility flip (instant), and all boot-time work (profile, library, screenshot scan) happens behind the built-in splash. The screenshot viewer opens as a **modal on the modal stack** (`ScreenshotViewerViewModel` → `ScreenshotViewerView`) from both the gallery and the game modal's screenshots pane. The window interacts with the overlay views via live visual-tree lookups (`Find<T>()`) for focus/scroll requests raised by `DashboardNavigationController`.

**Boot:** `Program.Main` → `App` builds DI → `desktop.MainWindow = MainWindow` (an `FAAppWindow`) → FluentAvalonia shows its built-in splash (`AppSplashScreen`) → `RunTasks` dispatches `MainWindowViewModel.InitializeAsync` (staged loads, cancellable, per-stage dwell, progress via `IProgress`) onto the UI thread → splash closes on completion (3s minimum total) → fullscreen dashboard revealed, input un-gated (`IsInitialized`).

**Data flow**
- DI: `App.Services` (built by `ServiceConfigurator.ConfigureServices()`) → singleton services (`IBackgroundService`, `IProfileService`, `IGameLibraryService`, `IScreenshotLibraryService`, `IGamepadInputService`, `DashboardNavigationController`, `InputRouter`) + `MainWindowViewModel` + `MainWindow` (parameterless ctor resolving from `App.Services` — the XAML loader requires it).
- Input: keyboard (`OnWindowKeyDown`) and gamepad (`IGamepadInputService.ButtonPressed`) → `InputRouter` (key/button → `Command` → per-screen handler) → `DashboardNavigationController` actions (move/select/activate) → selection `IsSelected` → styled visuals; the controller raises focus/scroll requests the window fulfills. All input is gated until the boot pipeline completes.
- Navigation: `OptionsCardViewModel.TargetScreen` → `MainWindowViewModel.OpenScreen()` → `CurrentScreen` (a screen VM) → the matching overlay's `IsVisible` flips; null shows the dashboard. The gallery and the game modal's screenshots pane open the shared screenshot viewer modal (`ScreenshotViewerViewModel` → `ScreenshotViewerView`).
- Selection: focus/click on a card → `IsSelected` → styled via `.selected` class / pseudo-class → visuals. **Dashboard (`DashboardViewModel.RecentGames`) and library (`LibraryViewModel.Games`) hold separate VM instances with independent selections.**
- Settings: VM property → `IBackgroundService.Settings` → `Save()` + `ApplyResources()` → `Application.Resources` → `DynamicResource` bindings; `SettingsViewModel.AppearanceChanged` → dashboard rebuilds its background; `LibraryViewModeChanged` / `CardImageChanged` → live layout / card-image swaps.
- Localization: XAML `{DynamicResource Key}` + C# `LocalizationHelper.GetText("Key")` → `Resources/Language/en.axaml`.
- Sorting: **X** in the library → `LibraryViewModel.CycleSort()` → re-orders `Games` (title asc, playtime desc, last-played desc), keeps the selection, re-scrolls.
- Data: `IProfileService` (profile + GPD stats) · `IGameLibraryService` (Core `GameManager` wrapper) · `IScreenshotLibraryService` (scan + game-title matching) feed the child VMs; Core paths (library, games, artwork, profiles, logs) resolve against the base app's folder via `Program.Main` → `BaseAppLocator.Resolve(args)` → `AppPathResolver.SetBaseDirectory(...)`.

**Core integration points (available in `XeniaManager.Core`)**
- `GameManager.LoadLibrary()` / `GameManager.Games` — real library.
- `Game` — `Title`, `GameId`, `Playtime` (**minutes**, use `PlaytimeFormatter.Format`), `LastPlayed` (`DateTime?`), `Artwork`, `Compatibility`, `FileLocations`.
- `GameArtwork.CachedBoxart` / `CachedBackground` / `CachedIcon` — cached bitmaps.
- `AccountContent` + `GpdFile` — per-game achievement counts / gamerscore (`GpdFile.Achievements`, `GetTotalGamerscore()`).
- `Launcher.LaunchGameASync(Game, Settings, ...)` — game launch (needs Core `Settings`).
- `ProfileManager.LoadProfiles(XeniaVersion.Canary)` — profile/gamertag.
- `GamepadInputService` / `IGamepadInputService` — SDL3 gamepad polling (moved from BigScreen; `GamepadButton` enum incl. `View`); multi-gamepad tracking via `GamepadDeviceCollection` (all pads open, per-pad battery/GUID, primary selection, `Rescan`/`ReloadMappings`), pure mapping in `GamepadButtonMapper` + `StickTracker`; input flows from the primary pad only.
- `ReleaseDateFormatter.Format(...)` — ordinal release dates (+ unit tests).

---

## 4. Design System

### Tokens (`DarkGradient.axaml`)
| Token                                                             | Purpose                                                                 |
|-------------------------------------------------------------------|-------------------------------------------------------------------------|
| `CardBackground` / `CardTitleBar`                                 | Card surfaces                                                           |
| `CardBorder`                                                      | Reserved 3–4px borders (no layout shift on focus)                       |
| `AccentColor`                                                     | Selected/hover accent (runtime-configurable)                            |
| `TextPrimary` / `TextSecondary`                                   | Text                                                                    |
| `HintKeyX` / `HintKeyY` / `HintKeyA` / `HintKeyB` / `HintKeyBack` | Keycap colours (X blue, Y amber, A green, B red, back/view faded white) |
| `SystemAccentColor` + Light1–3 / Dark1–3                          | FluentAvalonia accent (Color-typed, runtime variants)                   |
| `SliderTrackValueFill` / `SliderThumbBackground`                  | Slider fill/knob (accent)                                               |
| `ControlOutlineBrush` / `TextControlBorderBrush`                  | ComboBox/TextBox borders (+ hover/disabled), focused = accent           |
| `AccentFillColor*` / `TextOnAccentFillColor*`                     | Dropdown selected-item bar etc.                                         |

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
- [x] Alphabetical carousel, sortable with **X** (selection follows the list index — viewport stays put)
- [x] Card: box art (`CachedBoxart`, bottom-anchored, top-cropped), title, playtime, achievements, gamerscore

### 5.3 Media gallery
- [x] 4-across 16:9 grid, adequate spacing, fits the window width
- [x] Click/Enter → modal viewer
- [x] Faded chevrons in the modal (hide at the ends, brighten on hover)
- [x] B/Escape closes (viewer, then overlay)
- [x] Grid scrolls down, clamped at both ends, no wrap-back
- [x] **X** sorts: Newest First / Oldest First / By Game (indicator top-right, **capture date decoded from the file name** with write-time fallback); selection follows the list index

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
- [x] `GamepadInputService` (now in Core): graceful init failure, UI-thread poll, deadzone edge detection (no hold repeat); D-pad, left stick and bumpers normalise onto the D-pad values; opens/adds/closes gamepads; raw input traced
- [x] D-pad + A/B mapped to the selection model
- [x] Dashboard **row-state driven** (not keyboard focus): Up/Down switches rows with a fixed column mapping (games 2-3 → option 2, 4-5 → option 3, etc.); A acts on the active row only; options cleared when returning to games; no games = options row stays active
- [x] Left/Right via D-pad, stick and bumpers; Up/Down with row jumps in media
- [x] Settings stays keyboard-only; input gated while the window is disabled (game running)
- [x] Sorting keeps the viewport fixed (selection follows the list index) — library and media

### 5.9 Real controller battery / Wi-Fi
- [x] Battery icon from `SDL_GetGamepadPowerInfo` (5s poll; warning when unknown, full when charging, tiers otherwise)
- [x] Wi-Fi icon from `NetworkInterface` (10s poll; `WiFi` / `WiFiOff`)
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

### 5.12 Main app launch button + splash + localisation
- [x] Big Screen launch button (main app): nav item + `NavigationService` launch (side-by-side or repo sibling); missing exe → localised warning
- [x] Boot splash: separate window, deferred startup, six staged statuses, tweened bar, 3s minimum, saved-colour background (no green→red flash); input gated until `IsInitialized`
- [x] Localization key set + wiring (main-app naming convention)
- [x] AXAML/style consolidation (shared classes, `HintKeyX/Y/A/B`, `CardShadow` tokens, App.axaml data templates)
- [x] `ArtTile` removed (`GameCard`/`ScreenshotCard` standalone again)
- [x] Stability: `BoxShadows` cast crash, FAAppWindow title bar removed, background-image decode moved behind the splash

### 5.13 Library list view + details pane
- [x] `LibraryViewMode` (Carousel/List) persisted as `library_view_mode`; Settings dropdown + live swap
- [x] `LibraryListItem` rows (disc icon + title, accent on select/hover) with vertical clamped scroll
- [x] `GameDetailsPanel`: disc art, playtime/achievements/gamerscore, DB bio + genre/developer/publisher/released
- [x] Stale-fetch guard (generation counter) + negative cache around `XboxDatabase.GetFullGameInfo`
- [x] `ReleaseDateFormatter` in Core (+ unit tests) — ordinal release dates
- [x] `GamepadButton.View` in Core; **View/V** swaps the view
- [x] Up/Down navigates the list; scroll-to-selected handles both geometries (layout swaps re-centre on the selection)

### 5.14 Card image mode + hint bars
- [x] `CardImageMode` (Box Art / Icon) persisted as `card_image_mode`, **default Icon**; Settings dropdown; dashboard cards swap live
- [x] Box Art mode keeps the disc icon as fallback when art is missing
- [x] Library hint bar: Back (B red) → Play (A green) → Sort (X blue) → Swap View (faded-white `CaretLeft`, `HintKeyBack` token)
- [x] Media hint bar: Back → Select (A) → Sort (X blue)

### 5.15 Post-review: housekeeping
- [x] **T1.** Formatting sweep (JetBrains Rider: Code Clean-up → Reformat on the whole solution; normalises AI double blank lines). Manual fix of misaligned `en.axaml` entries in both apps (`XeniaManager/Resources/Language/en.axaml` + BigScreen's)
- [x] **T2.** Delete test artwork (`Resources/Art/fr65z3.jpg`, `Resources/Art/SL1qfH.jpg`)
- [x] **T3.** Move `Services/ColorJsonConverter.cs` → `Converters/` folder (namespace `XeniaManager.BigScreen.Converters`)
- [x] **T4.** SplashScreen rewrite — `MainWindow` becomes `FAAppWindow`; boot splash uses FluentAvalonia's built-in `AppWindow.SplashScreen` (`IFAApplicationSplashScreen` + `SplashScreenContent`, `RunTasks` hosts the boot pipeline) per shazzaam7/dotnet-templates pattern; delete `SplashWindow`; fixes the "line at top" splash bug. Splash window forced fullscreen from `SplashScreenView.OnAttachedToVisualTree`; gradient math extracted to `Factories/BackgroundBrushFactory.cs`

### 5.16 Controller input: primary controller + rescan (Core)
- [x] **T5.** `GamepadInputService` (Core): opens **all** gamepads; `ConnectedGamepads` snapshots (name, GUID, battery, charging, isPrimary); `SetPrimary` / `SetPrimaryByGuid` (GUID persisted as `primary_controller_guid` in `dashboard-settings.json`, restored at boot); `Rescan()`; `ReloadMappings()`; **input flows from the primary pad only**; refactored into `GamepadDeviceCollection` + `GamepadButtonMapper` + `StickTracker`

### 5.17 Profiles & identity
- [x] **T6.** Profile switching — `ProfileService` loads all Canary profiles; selected profile persisted (`profile_xuid` in `dashboard-settings.json`); header gamertag/gamerscore + per-game achievement stats follow the selection
- [x] **T7.** Manage Profiles modal — port desktop `ManageProfilesDialog` (create / delete / import / export via `ProfileManager`) as a BigScreen modal

### 5.18 Game actions (game modal)
- [x] **T8.** Game modal — **Y** = Details on the selected card (and right-click on `GameCard`/`LibraryCard`/`LibraryListItem`) opens the game modal: icon + title above a vertical options list (Achievements · Screenshots · Title Updates · Marketplace Content · Patches · Settings) with the selected option's pane rendering live on the right; panes cached per option; A/Right enters the pane, B/Left returns, B closes. **Exactly one element highlights at a time** — a state contract: entering a pane clears the nav selection and calls `IGameModalPane.OnPaneEntered()` (selects the pane's first item); exiting calls `OnPaneExited()` and re-selects the nav option. Panes start unselected; `SelectionHelper.MoveSelection` picks the first item on the first move.
- [x] **T9.** Achievements pane — stats header (unlocked/total + gamerscore), **X-cycled sort**: Achieved (default) → Gamerscore Awarded → Alphabetical, scroll-to-selected on Up/Down, rows (name, description, gamerscore via star icon, unlock date; image only when unlocked), empty state
- [x] **T10.** Title Updates pane — `GameContent.InstallerHeaderFiles` rows + delete (confirmation modal, package + `.header` removed); A blocked when nothing is installed
- [x] **T11.** Marketplace Content pane — same shared pane initialised per menu entry (`MarketplaceContentHeaderFiles`)
- [x] **T12.** Screenshots pane — per-game gallery from `screenshots/<GAMEID_UPPER>`, reusing `ScreenshotCard`; A opens the shared screenshot viewer modal
- [x] **T13.** Patches pane — entries enable/disable (instant save), command editor (Type/Address/Value with per-type validation, add/delete), Remove with confirmation; **download moved to its own modal** (search prefilled with game ID, Canary/Netplay results, status states)
- [x] **T14.** Game Settings pane — config editor port (`ConfigUiSettings.AllSettings` + 335 localisation keys, five control types via `ConfigOptionRow`), controller row navigation, X saves, unsaved-changes prompt on exit
- [x] **T15.** Disc selection modal on launch — when `IsMultiDisc`: compact centered card (game icon + name header, "Disc Selection" line), one `DiscOptionCard` per disc (custom label or "Disc N", filled disc icon, "Last Played"/"File Missing" status; missing files dimmed and skipped by navigation), A/click launches the selected disc, B cancels; selection starts on the last played disc (first valid fallback)

### 5.19 Dashboard, header & settings
- [x] **T16.** Time format setting — 12h/24h (persisted in `DashboardSettings`); clock + capture dates follow it
- [x] **T17.** Controllers section (Settings): auto-detected list of `GamepadCard` rows with name, **Status: Primary/Secondary** (primary in accent) and tiered battery icon + % (via `IconFactory`); no UI buttons; SDL controller database updates silently in the background at boot; changing primary is **controller-only** (lands with T18)
- [ ] **T18.** Settings controller navigation: D-pad moves between rows; **A activates (sets primary controller on a gamepad row, opens dropdowns/checkbox/colour fields on the other rows)**; `GamepadCard` already carries `Classes.selected` + accent border, `SetPrimary` is wired in the VM
- [x] **T19.** Rename **Media → Gallery** — overlay, option-card label, screen title and `en.axaml` keys renamed; screenshot gallery only. Installed content (title updates / marketplace, XUID `0000000000000000`) does **not** get its own media entry — it lives in the game modal (T8/T10/T11)
- [x] **T20.** Boxart tile sizing — in `CardImageMode.BoxArt` the dashboard tile is portrait and sized so the box art fills it bottom-anchored with the top ~12% cropped; Icon-mode fallback unchanged
- [ ] **T21.** Header — network icon adapts to ethernet (`Ethernet`/`Wifi`/`WifiOff`); battery **% text below** the battery icon
- [ ] **T22.** Background type default → **Dynamic** (`DashboardSettings.Mode` + settings default)
- [ ] **T23.** Xenia version indicator — per-game badge/icon on library cards, list rows + details (reuse Core `XeniaVersionToIconConverter`/`XeniaVersionToStringConverter`)
- [ ] **T24.** Game compatibility indicator — coloured rating dot + label on cards (Core `CompatibilityRatingColorConverter`), DB URL as tooltip
- [ ] **T25.** List-view achievements — scrollable locked/unlocked achievement section in the list-view details pane
- [ ] **T26.** XConfig editor — port `EditXConfigDialog` (language, country, AV HDMI size, default profile) via `XConfigManager` as a Settings overlay (hidden when `XConfigExists` is false)
- [ ] **T27.** Input gating while a game runs — gamepad already gated by `IsEnabled`; add the same gate to `OnWindowKeyDown`, `OnCardGotFocus`, `OnOptionCardPressed`

### 5.20 Desktop app integration
- [ ] **T28.** Hide + disable main window when BigScreen opens — `NavigationService.LaunchBigScreen` keeps the `Process` handle, `EventManager.DisableWindow()` + `Hide()`; restore (`Show()` + `EnableWindow()`) on BigScreen exit
- [ ] **T29.** BigScreen start by default — `--bigscreen` CLI arg (desktop `Program.cs` launches BigScreen and hides the main window) + Settings toggle "Start in Big Screen" (persisted)

### 5.21 Gallery sort expansion (full desktop parity)
- [ ] **T30.** Expand `GallerySort` (renamed from `MediaSort` in T19) with the desktop app's full `GameSortOption` list, sorted by each screenshot's owning game (resolved from the parent folder's game ID): **Title** (alphabetical), **Time Played**, **Compatibility**, **TitleId**, **MediaId**, **XeniaVersion**, **Last Played** — alongside the existing **Newest First / Oldest First / By Game**. X still cycles; indicator + selection-preserving resort keep working (`GalleryViewModel.ApplyGallerySort`)

### 5.22 Screen animations
- [ ] **T31.** Screen animations — overlay screens, the game modal + its panes (T8–T14), the screenshot viewer modal and dashboard screen swaps must never open statically: very basic, very subtle motion only. Avalonia-native `Transitions`/`DoubleTransition` (opacity, translate, scale), ~150–250ms ease-in-out, one or two properties max; see §7 for full constraints + candidate approaches

---

## 5.23 Extra info & conventions (T1–T36)

> **Notes & conventions for the tasks above**
> - All new user-facing strings go into `en.axaml` (BigScreen + desktop), keys follow the existing naming convention; other languages deferred per spec §1.
> - Follow `CONTRIBUTING.md` throughout: XML docs, `Logger` usage (no silent catches), Hungarian XAML names, 4-space indent, AXAML property order, file-scoped namespaces, MVVM (thin code-behind).
> - Cross-cutting behaviour: mouse right-click **and** controller (**Y** = Details) must reach the same game modal; every popup is a **modal on the modal stack** (`ModalService`/`ModalViewModelBase`, `InputRouter` commands, `InputHint` bars, top-modal-only hint bars); modals exit with B/Escape.
> - Button conventions follow the **Xbox 360 dashboard**: **X = Sort** (library, gallery and in-modal lists), **Y = Details** (opens the game modal / details; also opens Manage Profiles from the profile picker), **A = Select/Activate**, **B = Back**, **View = Swap View** (carousel ↔ list). Keycap colours: X blue (`HintKeyX`), Y amber, A green, B red, View faded white.
> - Condition hygiene: any `if`/`while` condition with **more than 2 checks becomes a clearly named bool property at the top of the class (under the fields)**; conditions depending on event/argument values are split into nested guards of ≤2 checks each; keep conditions raw only when they are a single variable or two short checks. No magic numbers — `0`/`1`/`-1` count/​index idioms excepted; everything else gets a named constant (`Constants/`, e.g. `TimingConstants.ProgressReportInterval`).
> - Fire-and-forget async work goes through **`TaskUtilities.RunSafely<T>(...)`** — exceptions (including synchronous ones from modal-VM construction) are logged, never silently swallowed. Existing public bool properties (`ShowEmptyStub`, `IsOverlayOpen`, …) are the canonical checks for external code — raw collection `Count` checks in other classes should use them.
> - Core work first (`GamepadInputService`, any shared helpers), then BigScreen, then the desktop app.
> - Verification per task: `dotnet build "Xenia Manager.sln"`, `dotnet test tests/XeniaManager.Tests`, manual smoke run (launch, right-click menu, modals, controller input).
> - Artwork/icon choices reuse FluentIcons + existing token dictionary (`DarkGradient.axaml`); no new colours without adding tokens.
> - This roadmap supersedes/supplements §5.14 items; update §3 architecture tree (`Services/`, `Controls/`, `Views/`) as new files land.

### Untested ports
> Things I can't properly test or fully understand that I just used AI to try port over.

- Dual Disc Boot — no multi-disc games to test with
- Game Details Title Updates
- Game Details Marketplace Content
- Game Details Patches
- Game Details Settings

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

---

## 7. Screen Animations

> **Status:** Design intent only — the app should never open a static screen. Specifics to be narrowed down at implementation time.

### Goal
- Every modal/screen opens and closes with **very basic, very subtle** motion — nothing static, nothing flashy.
- Covers: overlay screens (Library / Gallery / Settings), the game modal + its panes (T8–T14), the screenshot viewer modal, and screen-to-screen swaps on the dashboard.

### Hard constraints
- **Avalonia-native only** — `Transitions` / `DoubleTransition` (opacity, translate, scale) / `ThicknessTransition`; no external animation libraries.
- **Cheap:** UI-thread property tweens only, no layout thrash, no re-renders of heavy content; long lists/cards animate via the container, not per-item (except a gentle stagger if trivial).
- **Subtle:** ~150–250ms, ease-in-out, one or two properties max (e.g. opacity 0→1 + 8–16px translate / 1.02 scale). No bouncing, no overshoot.
- Respect **reduced-motion** if cheap to add; never animate while a game is running (input already gated).

### Existing building blocks to reuse
- `DoubleTransition` fade-through-black already on `MainWindow.axaml` (180ms) and the `ProgressBar` value tween on the splash
- `ThicknessTransition` on the quit-toggle thumb; `Transitions` on `Border`/`ContentControl`
- Core `AnimationExtensions.AnimateOpacity(Window, from, to)` (ease-in-out) — window-level fades

### Candidate approaches (for the details pass)
- Overlays: fade + slight up/scale on the overlay `Panel` / `ContentControl` when `IsVisible` flips
- Game modal + its panes (T8–T14): same fade+translate on the modal/pane root; viewer: fade + zoom-out on close
- Selection feedback: keep existing `IsSelected` styling; optional 100ms grow/shrink on the selected card

---

## 8. AI note (read this first)

> **Do NOT update this document or tick roadmap items until the feature is verified working.**
>
> 1. Write the code.
> 2. Verify: `dotnet build "Xenia Manager.sln"` clean, `dotnet test tests/XeniaManager.Tests` green, and a **manual smoke run** of the actual feature (launch the app, exercise the UI path, check the logs).
> 3. **Only then** tick the roadmap checkbox and update §2 / §3.
>
> The roadmap checkboxes describe reality — a `[x]` on a broken, untested, or half-written feature is a lie that corrupts the whole document. If verification fails, fix the code first; the spec stays untouched until it's actually true.
