# Xenia Manager BigScreen — Developer Handoff

> **Project:** `source/XeniaManager.BigScreen/`
> **Branch:** `feature/big-screen` (PR #572)
> **Status:** Complete. All roadmap items 5.1–5.23 implemented; see the appendix for the full checklist.

---

## 1. Overview

**XeniaManager.BigScreen** is a fullscreen **Xbox Series-style dashboard** ("big screen mode") for Xenia Manager. It runs as its own Avalonia desktop app and references `XeniaManager.Core` for the game library, profiles, artwork and launching.

The experience is controller-first: browse the game library (carousel or list), pick a game and launch straight into Xenia; browse screenshots in a gallery with a focused modal viewer; configure dashboard visuals (background, accent, vignette) and core settings from a settings screen. All choices persist across sessions.

**Stack:** Avalonia 12.1.0 · .NET 10 · CommunityToolkit.Mvvm · TweenAvalonia 0.3.0 · FluentAvalonia + SDL3-CS (via `XeniaManager.Core`) · Microsoft.Extensions.DependencyInjection (via Core)

### Integration contract
- BigScreen is **isolated** — every access to `XeniaManager.Core` is read-only. Its main-app integration is exactly two things: the **Launch Big Screen** button in the desktop app, and the **"Start in Big Screen"** default switch (T29). If isolation isn't holding, that's a bug.
- **Quit contract:** BigScreen's Quit returns to Xenia Manager (launching `XeniaManager.exe` if it isn't running), or closes everything per the quit toggle (§2.4). With "Return to Xenia Manager on Quit" off, BigScreen exits with `ProcessExitCodes.CloseEverything` (exit code 1) so the desktop app — if it launched BigScreen — shuts down through its normal `MainWindow.Close()` path instead of restoring its window. Any other exit (return-on, Alt+F4, crash) restores the desktop window.
- **Non-goals:** translations beyond the English key set (the app is fully keyed and wired — new languages are new files only); full config editing (BigScreen exposes a curated 18-option subset); gallery sort expansion beyond the three orders (§6).

---

## 2. Feature inventory

### 2.1 Dashboard shell (`Views/Shell/MainWindow.axaml`, `Views/Dashboard/DashboardView.axaml`)
- **Header:** avatar chip (`PersonCircle`, 40px circle, no fill, reserved 4px border that turns accent on hover/selection, no shadow); gamertag from the **Canary profile** (`ProfileManager.LoadProfiles(XeniaVersion.Canary)` + profile GPD); **white** gamerscore (star + number); **live** Wi-Fi + controller battery icons (10s/5s polls); live clock (1s `DispatcherTimer`). Header row 100px; insets `150,55,150,0` — the 55px top inset equals the dashboard's bottom inset so the frame is uniform.
- **Game card row:** max **8 recent games** (`RecentGames`, own VM instances — selection is independent of the library). Each card shows **box art or disc icon** per the `card_image_mode` setting (default Icon). Box Art mode is a portrait tile filled bottom-anchored by the box art with the top ~12% cropped, falling back to the icon when art is missing; Icon mode is a square disc fill. Cards are 153 wide; the focused card grows to 228 (animated Width/Height transitions) — icon mode stays square (153² → 228²), box art keeps its fixed 1.19 ratio (153×182 → 228×271). Row spacing 15, slot 271 (the focused box-art card's height). A `BorderOverlay` carries the strokes: the base border is transparent at the **same 5px** as the hover/selected accent stroke (nothing shifts on focus); each card floats on an even box shadow (offset 0,0).
- **Option card row:** `Library` · `Gallery` · `Settings` · `Quit`, 339×135 cards (icon 26 / text 20), spacing 16. Hover shows the accent border (same 5px as idle); selection is driven by `IsSelected` (controller focus), not keyboard focus.
- **Layout:** full-width grid with concrete insets — dashboard `150,0,150,55`; rows `*, 271, 145`; row gap 17; header and dashboard share the same 150px side boundaries. No centered Auto column (it re-centred on every selection change and pushed content offscreen on narrower displays).
- **Fullscreen:** plain `Window` (no title bar) with `WindowState=FullScreen` **forced in code** at construction and before `Show()` — the XAML attribute is not reliable on the DI creation path. 1920×1080 fallback.

### 2.2 Overlay screens
Full-screen pages rendered over the dashboard; **Enter/click** opens, **B/Escape** closes, focus returns to the option row. Bottom hint bars use the `InputHint` control (coloured circle keycap + label).

- **Library** — all games in a horizontal **carousel** (`LibraryCard`: box art with a 13% top crop, bottom-anchored ~366px art region, title, playtime row, achievements/gamerscore row from the profile GPD) or a vertical **list** with a details pane (`LibraryListItem` + `GameDetailsPanel`). Left/Right iterates (clamped at both ends, no wrap); the row scrolls once the selection passes the middle; in list mode Up/Down iterates. **X** cycles the sort (Alphabetical → Time Played → Last Played; indicator top-right via `IconStat`). **View/V** swaps the layout (Carousel ↔ List, persisted via `library_view_mode`). The details pane shows marketplace DB info (bio, genre, developer, publisher, release date — loading/no-info states, stale-fetch guard + negative cache) plus a **Xenia version icon** opposite the title (bare build icon + hover tooltip via Core converters) and a **compatibility row** as the first metadata entry (rating label with the coloured dot to its right, DB URL as hover tooltip). Disc stub when the library is empty.
- **Gallery** — screenshot gallery scanned from `Emulators/Xenia Canary/screenshots/**` (recursive, common image extensions), 4-across 16:9 grid that scrolls down (clamped at both ends, no wrap-back). **X** cycles the sort (Newest First / Oldest First / By Game; indicator top-right via `IconStat`, **capture date decoded from the file name** `{GAMEID} - {yyyy-MM-ddTHH-mm-ss}`, write time as fallback — `ScreenshotFileNameParser`). Click/Enter → the screenshot viewer modal (on the modal stack). Camera stub when the gallery is empty. Hints: Back · Select (A) · Sort (X).
- **Settings** — background type dropdown, library view dropdown, **card image dropdown** (Box Art / Icon, default Icon), primary/accent colour fields (swatch + hex + palette popup), vignette slider, background image picker. **XConfig section** (bottom of the screen, hidden when no Canary XConfig exists): a single **Resolution** dropdown (`AvHdmiScreenSize`, "R" prefix stripped, instant-saved). **Controller navigation:** D-pad walks the rows (fixed cards then connected gamepad rows — the `.settings-row` accent border marks the selection; rows start unselected, the first move selects the first row, the selection survives battery-poll rebuilds). **A** activates — gamepad rows set the primary controller, the quit toggle flips, Manage Profiles / Select Image act, dropdown rows open their native dropdown (**Up/Down** cycles, **A** commits, **B** restores the original), colour rows open the palette popup (**Left/Right** cycles), the vignette slider steps directly with Left/Right on selection (no editor). **Back** closes an open editor first, then the screen. Keyboard input stays on the native controls (Tab, arrows, hex typing).
- **Quit** — closes the app. With "Return to Xenia Manager on Quit" on, the base app is launched first if it isn't running; with it **off**, BigScreen sets its process exit code to `ProcessExitCodes.CloseEverything` (shared Core constant) so the desktop app — if it launched BigScreen — shuts down too instead of restoring its window.
- **Hint bars** — per screen: Library = Back (B red) → Play (A green) → Sort (X blue) → Details (Y amber) → Swap View (faded-white `CaretLeft`, `HintKeyBack` token); Gallery = Back → Select (A) → Sort (X blue); Settings = Back → Select (A). **One hint bar at a time:** only the top modal's hint bar shows (`ModalViewModelBase.IsHintBarVisible`), and overlay screens hide theirs while any modal is open.

### 2.3 Launch behaviour (`MainWindowViewModel.LaunchGame`)
- With the **Launch Games in Fullscreen** toggle on (default, Preferences section), the game's `Display.fullscreen` is forced for the session via `GameDataCache.GetConfig` — the original value is captured and restored after the session; skipped when the game runs Custom Xenia or has no config file yet.
- The same launch path injects the active profile into the game's `[Profiles] logged_profile_slot_0_xuid` so Xenia boots signed in (see §2.10). The config is copied to the emulator's default location at launch, so Xenia boots with the session's settings.
- Multi-disc games show the **disc selection modal** first (A launches the selected disc, B cancels, missing discs skipped).

### 2.4 Base app data sharing (`Program.cs` + `Services/BaseAppLocator.cs`)
- BigScreen **reads the base Xenia Manager's data folders** (library, games, artwork, profiles): `Program.Main` calls `AppPathResolver.SetBaseDirectory(...)` before anything resolves paths (Core change; base app unaffected).
- Resolution order: `--base-dir <path>` arg → `XeniaManager.exe` next to BigScreen (production layout) → repo sibling project with the same bin config → fall back to the BigScreen folder itself.
- `dashboard-settings.json` stays next to the BigScreen executable.

### 2.5 Background system (`Services/BackgroundService.cs`)
- **5 modes** (`Models/BackgroundMode.cs`): `Image`, `Solid`, `LinearGradient`, `RadialGradient`, `Dynamic` (selected game's artwork, falls back to radial).
- Gradients are **derived from the primary colour** (mix toward black, subtle slate ramp — never near-black or white ends).
- **Vignette** overlays image-based backgrounds only (Image mode / Dynamic with art), opacity 0–1.
- Persisted to **`dashboard-settings.json`** next to the executable (`Models/DashboardSettings.cs`).
- `ApplyResources()` pushes tokens into `Application.Resources` at load and after every change, so `DynamicResource` bindings update live. Falls back to the linear gradient when a brush can't be built (e.g. missing image).
- **Dynamic art crossfade:** a dedicated `Image` layer above the static window background, faded via `ArtOpacity` (300ms legs, latest-wins — a single cancellable fade instance is restarted on every art swap, so rapid selection keeps a black screen until you stop, then fades in).

### 2.6 Boot splash (FA built-in)
- **FluentAvalonia's built-in `AppWindow.SplashScreen`** (`IFAApplicationSplashScreen`): `MainWindow` is an `FAAppWindow` with `SplashScreen = new AppSplashScreen()`; FA shows the splash, runs `RunTasks` (the boot pipeline, dispatched onto the UI thread), then reveals the fullscreen dashboard. No separate splash window.
- The splash window FA creates is **forced fullscreen + borderless** from `SplashScreenView.OnAttachedToVisualTree` (it is centered by default).
- Content: TV logo + "Xenia Big Screen" + live status text + tweened progress bar, over the **dashboard's radial background** built by `BackgroundBrushFactory.CreateRadial` from the saved `primary_color`; logo + bar use the saved `accent_color` so the splash matches the dashboard.
- **Boot pipeline** (`MainWindowViewModel.InitializeAsync`, cancellable, per-stage minimum 400ms dwell): Loading Settings (settings JSON + background brush/image decode) → Loading Profile (profile + header) → Loading Dashboard (library JSON + recent cards) → Loading Library (per-game GPD stats off-thread + cards, chunked progress) → Loading Game Data (per-game content/patch/achievement-GPD preload + marketplace details, one stage, chunked progress, per-step timing logs) → Loading Gallery (screenshot scan in `Task.Run`) → Loading Done (1s hold). Total minimum ~3s (`MinimumShowTime` 2s + dwells). Config files are deliberately **not** preloaded — they're the costliest step and only the game settings pane uses them, so they load lazily on the pane's first open.
- Input (keyboard, gamepad, mouse activation) is **gated until `IsInitialized`**. Boot failures log and the window still reveals.

### 2.7 Main app integration
- **"Big Screen" nav button** in Xenia Manager (`MainView.axaml`, `Tv` icon, tooltip "Open Big Screen") → `NavigationService` `BigScreen` tag → launches `XeniaManager.BigScreen.exe` resolved **side-by-side or via the repo-sibling bin folder** (same config, matching `BaseAppLocator`); missing exe → localised warning box.
- **While BigScreen is open:** `LaunchBigScreen` keeps the `Process`, disables the main window (`EventManager`), hides it, `await`s `WaitForExitAsync()`, then restores (`Show()` + `EnableWindow()`). The catch path restores too, so a crashed BigScreen never strands the desktop UI. Exit-code contract as described in §1.
- **Start in Big Screen:** the `--bigscreen` CLI arg (case-insensitive) or the persisted **"Start in Big Screen"** toggle (`Core` `GeneralSettings.start_in_big_screen`, desktop Settings → General) launches BigScreen at startup. The hook runs on the **first window show only** — it unsubscribes itself, so the restore `Show()` after BigScreen exits can't relaunch it (this fixed a hard exit loop). The same toggle is mirrored in **BigScreen Settings → Preferences**, reading and writing the same Core `Settings`/`config.json` so both apps share one state.

### 2.8 Theming
- `Resources/Themes/DarkGradient.axaml` — token dictionary: `CardBackground`, `CardTitleBar`, `CardBorder`, `AccentColor`, `TextPrimary/Secondary`, `HintKeyX/Y/A/B/Back`, `CardShadow`/`CardShadowSelected` (`BoxShadows`, all offset 0,0), `SystemAccentColor*` (Colour-typed variants), slider/control overrides, accent-fill family.
- `Resources/Themes/Controls.axaml` — `ControlTheme`s for the custom controls (`ColorPickerField`, `PalettePicker`).
- `Resources/BigScreenStyle.axaml` — global styles (cards, shared `.screen-title`/`.card-title`/`.hint-bar`/`.empty-state` classes, text halos).
- `Resources/Language/en.axaml` — **full key set for every user-facing string** (main-app naming convention), wired via `{DynamicResource}` in XAML and `LocalizationHelper.GetText` in code; `LocalizationHelper.Initialize(...)` in `App.axaml.cs`. Other languages = new files only.
- FluentAvalonia theme (via Core) like the main app.

### 2.9 Custom controls (`Controls/Cards|Modals|Profiles|Settings|Primitives|Splash/`)
- **`ColorPickerField`** — swatch + hex text box; clicking the swatch opens a palette popup. Two palettes: muted slates/greys (primary) and 10 accent colours incl. white/light grey.
- **`PalettePicker`** — horizontal StackPanel of colour swatches; raises `SelectedColorChanged`.
- **`IconStat`** — icon + text row (`Icon` Symbol, `Stat`, `IconSize`, `FontSize`, `Spacing`, `IconRotation`). Used in the header (gamerscore), library sort indicator, and library cards.
- **`InputHint`** — keycap + label (`KeyColour`, `Icon`/`Char` glyph, `Text`): transparent circle with coloured 2px outline, glyph coloured to match, white label. Xbox-standard colours per usage (X blue, Y amber, A green, B red; the back/view button uses a faded-white `CaretLeft` via `HintKeyBack`).
- **`EmptyState`** — the shared stub control (disc / camera stubs).
- **`LibraryCard`** — carousel card: box art (bottom-anchored, top 13% cropped, rounded clip via `RectangleGeometry`), title, playtime row, achievements/gamerscore row; `CardBorder` 2px inactive → accent on selection (outer border only).
- **`LibraryListItem`** — list-view row: disc icon + title, accent border on selection/hover.
- **`GameDetailsPanel`** — list-view details pane: disc art, playtime/achievements/gamerscore, marketplace DB bio + metadata strip (genre, developer, publisher, released), loading bar / no-info states.
- **`GameCard`** — dashboard game tile: box art or disc icon per `card_image_mode` (rounded clip; Box Art mode is a portrait tile filled bottom-anchored with the top ~12% cropped, Icon mode a square fill) + title bar on selection, border overlay strokes (transparent idle / accent hover+selected, both 5px); grows on selection **153 → 228 wide** (animated), height follows the mode (icon stays square, box art keeps the 1.19 ratio).
- **`ScreenshotCard`** — gallery tile: 16:9 screenshot with 6px rounded clip, title bar on selection, border overlay strokes.
- **`OptionsCard`** — dashboard option tile.
- **`GamepadCard`** — Settings controller row: name, status text, battery icon + % (accent on hover/select).
- **`ProfileButton` / `ProfileRow` / `CreateProfileRow`** — header avatar chip + profile list rows.
- **`ModalHost` / `ConfirmationModal`** — modal stack renderer + the generic 2-option prompt.

### 2.10 Profiles & identity
- **ProfileService** loads **all** Canary profiles (`Profiles`); the active one is persisted as `profile_xuid` (hex PathXUID) and restored at boot (falls back to the first); `SwitchProfile` re-loads the profile GPD, saves and raises `ProfileChanged` (header identity + per-game achievement stats rebuild); `Refresh()` re-scans after external changes (Manage Profiles), keeping the active profile or falling back to the first.
- **Launch sign-in:** `LaunchGame` calls `EnsureActiveProfile()` (safety net — restores the persisted active profile when the boot-time restore fell back), then `InjectLaunchProfile(game)` writes the active profile's XUID into the game's config `[Profiles] logged_profile_slot_0_xuid` (the field Xenia itself persists after a manual sign-in — the only mechanism that actually signs a profile in on direct game boot). The config is copied to the emulator's default location at launch, so Xenia boots signed in. No restore — Xenia owns the slot, the value is re-injected on every launch and Xenia persists it after the session.
- **XConfig sync** (`SyncXConfigDefaultProfile`, boot/switch/refresh + launch): still writes `DefaultProfile` + `Language` + `Country` into the Canary XConfig — kept for dashboard-level defaults, but **not** what signs the game in (ineffective for direct game boots). Both edits guard on Custom-version games and missing config files.
- **Boot reorder:** the Settings stage runs before the Profile stage (the persisted XUID is needed to pick the profile).
- **Header `ProfileButton`** (avatar chip) — focusable + clickable; controller **Up** from the game row selects it (**Down** returns to the previously selected card; **Right** jumps to the next card after the current selection, wrapping from card 8 back to card 1); reserved 4px border that turns accent on selection/hover.
- **Profile picker modal** (`ProfilePickerView`) — opened from the header chip: all profiles (active first, then alphabetical) with gamertag, country · language and per-profile gamerscore; Up/Down + A switches + B closes, **Y opens Manage Profiles** on top of the picker; "no profiles" stub.
- **Manage Profiles modal** (`ManageProfilesView`) — opened from a Profiles card in Settings or via Y in the picker: profile rows (scrollable) + an anchored **Create New Profile stub row** beneath; full desktop-dialogue port (create / delete / import (`ProfileManager.ImportProfileWithReplacement` + replace-confirm, `.xaccount`/`.zip` picker) / export (save picker + "include saves?" confirm) / edit gamertag (regex + 15-char validation, inline error) + country/language/subscription combos + Xbox Live toggle / Save (`SaveProfiles`)); edit panel hidden while the stub is selected; **unsaved edits prompt Save / Discard on exit and row switches (B cancels, staying put)**; deleting the active profile falls back to the first and refreshes the header/stats. **Controller edit-panel navigation:** **A or Right** on a profile row enters the panel (clears the row accent — game-modal column contract; the panel border turns accent), **B or Left** returns to the list; the panel rows (gamertag, country, language, Xbox Live toggle, subscription tier, Save) are controller-navigable with reserved `.edit-field-row` borders — **A** toggles the live switch, saves on the Save row, focuses the gamertag text box for keyboard entry, or opens a dropdown editor (**Up/Down** cycles, **A** commits, **B** restores; subscription skipped while Xbox Live is off); panel state resets on row/stub switches. Hints: B Back · A Edit · X Delete · View Import · Start Export.

### 2.11 Game modal (Y = Details / right-click on any game card)
- **`GameModalViewModel`/`GameModalView`** — a `modal-screen` card with the game's icon + title above a vertical options list (Achievements · Screenshots · Title Updates · Marketplace Content · Patches · Settings). The selected option's **pane renders on the right and updates live as the selection moves** (panes are created once and cached per option — no reloads when navigating back). A/Right enters the pane, B/Left returns to the options list, B there closes the modal. **Exactly one element highlights at a time** — a state contract, not styling: entering a pane clears the nav selection and calls `IGameModalPane.OnPaneEntered()` (selects the pane's first item); exiting calls `OnPaneExited()` (clears the pane) and re-selects the nav option.
- **Panes** (`ViewModels/Modals/`, all implementing `IGameModalPane.HandleInput`):
  - **Achievements** — stats header (Trophy + Star `IconStat`s), X-cycled sort (**Achieved default → Gamerscore Awarded → Alphabetical**; Achieved = unlocked first, GPD order within each group), one flat scrollable list (scroll-to-selected on Up/Down, no section headers), rows with image only when unlocked, empty state. **Spoiler gating:** secret achievements (`ShowUnachieved` flag) hidden while locked — "Hidden Achievement" name, "This achievement contains spoilers" tagline, no gamerscore, dimmed. Rows show the gamerscore as a star icon + number (no box). Panes start **unselected** — `SelectionHelper.MoveSelection` picks the first item on the first move.
  - **Screenshots** — the game's own folder (`{EmulatorDir}/screenshots/{GAMEID}`) as a 4-across grid; **Canary games reuse the boot-time gallery cache** (already decoded — no re-scan), other versions scan off-thread with a loading state; Left/Back return to the nav list; A opens the shared screenshot viewer modal.
  - **Title Updates / Marketplace Content** — one shared pane initialised per menu entry (single type — no switching), rows with display name + file name, A deletes (confirmation modal, package + `.header` removed), empty state; A is blocked when nothing is installed.
  - **Patches** — installed patch entries (A toggles enabled, instant save; editing stays in the main app), pinned Remove action (confirmation); "Download New Patch" opens the download modal (the row is the only path).
  - **Game Settings** — **curated controller-friendly subset**: 18 rows (toggles/sliders/dropdowns only) as main-settings-style cards (label + control, accent selection, section headers). Full config editing stays in the main app. **Manual save:** X writes the config, edits only mark the pane dirty; exiting with unsaved changes prompts **Save / Discard / Cancel** (game title in the message). Controller: Up/Down rows, A flips toggles / opens dropdown editors, Up/Down cycles open combos, **Left/Right steps sliders directly (no editor)**, B restores combos (cancel). Curated sections: Display (fullscreen, letterbox) · Audio (audio system, mute, XMP, XMA decoder) · GPU (backend, async shader compilation, vsync, resolution scale X/Y) · General (apply patches, controller hotkeys, discord) · HID (vibration, stick deadzones) · UI (achievement notifications).
- **Hint bars** — the game modal's hint bar is fully dynamic: B = Back always, A = `AHintText` per column (Select nav / Select achievements / View screenshots / Delete content / Toggle patches / Edit settings), X = `XHintText` where a pane has an X action (Sort achievements / Save settings). The patch download modal shows B Back · A Download.
- **Patch download modal** (`PatchDownloadViewModel`/`PatchDownloadView`) — dedicated modal: search prefilled with the game ID, generation-guarded Canary/Netplay results with source badges, A/Tap downloads (`PatchManager.DownloadPatchAsync`) and closes on success; visible Searching… / No patches found / failure status. The patches pane refreshes the patch cache and reloads its list when it closes.
- **Screenshot viewer modal** (`ScreenshotViewerViewModel`/`ScreenshotViewerView`, in `Views/Modals/`) — the full-window viewer (uniform-stretched image, faded chevrons that hide at the ends, caption with game title + capture date) lives on the modal stack and opens from **both** the gallery and the game modal's screenshots pane. It is the **only modal that ignores the modal backdrop layer** — `ModalBackdropVisible` (top modal is not the viewer) hides the 30% scrim so its own opaque `ViewerBackdrop` renders exactly as before. Left/Right steps, B closes.

### 2.12 Disc selection modal
- **`DiscSelectionViewModel`/`DiscSelectionView`** — shown by `MainWindowViewModel.LaunchGame` when `IsMultiDisc` (before the window is disabled, so the modal stays interactive): a compact centered `modal-card` (880×460) over its own `ModalCardScrim` backdrop — game icon + name header with a "Disc Selection" line beneath, then **one `DiscOptionCard` per disc** in a centered horizontal row (custom `Label` or "Disc N", filled disc icon drawn from basic geometry in `TextSecondary` — accent when selected, "Last Played" / "File Missing" status line; missing files dimmed). Only the disc cards take input: Left/Right moves (skipping missing files, clamped), **A or click launches the selected disc** (`Close(discNumber)`), **B cancels** (`Close(null)` — `LaunchGame` aborts). Initial selection = last played disc (first valid fallback). Typed modal result via `ModalViewModelBase<int?>` + `ModalService.ShowAsync<int?>`. Hints: B Back · A Select.

### 2.13 Game data cache
- **`GameDataCache`** — session-long per-game cache populated behind the splash in the **Loading Game Data** boot stage: installed content scans (`GameContent`), patch files (`PatchFile` + path) and achievement GPDs (`GpdFile`, active profile). **Config files are excluded from boot** (costliest step, ~120ms/game, only the settings pane needs it) — `GetConfig` loads lazily on the pane's first open and is cached after. Marketplace details preload (`LibraryViewModel.PreloadDetailsAsync`) runs inside the same boot stage (no separate splash step).
- **Per-step timing logs** — `Preloaded '{title}' - content Xms, patch Xms, GPD Xms, total Xms` per game, `Game data preloaded for N games in Xms` for the stage, `Details for '{title}' in Xms` + `Details preloaded for N games` for the details pass.
- **Edit-sync (the cache is the single source of truth and BigScreen is the only editor):** settings saves mutate the cached `ConfigFile` instance in place; settings discard → `ReloadConfig`; content delete → `RefreshContent` (re-scan); patch download/remove → `RefreshPatch` (re-load); profile switch → `ClearAchievementGpds` (dispose + clear). Panes read only from the cache — ctors do no I/O.
- **Screenshots pane** reuses the gallery's boot-time items for Canary games (same decoded bitmaps — no duplicate decode).

### 2.14 Modal system (push/pop stack with dispose)
- **`IModalService`/`ModalService`** (DI singleton) — full-screen modal stack: `ShowAsync` (with/without a typed result), `Close` pops the top modal, disposes `IDisposable` VMs and raises `StackChanged`; modals nest naturally (a modal can await another on top of itself). Modals are created fresh per open — no state carries, no stale subscriptions.
- **`ModalViewModelBase`/`ModalViewModelBase<TResult>`** — result delivery, `HandleInput(NavigationCommand)` (virtual; base closes on Back), `Dispose()` hook, own close `Task`.
- **Modal layer** (MainWindow, sibling of the content grid) — full-window transparent black backdrop (`ModalBackdrop`, 30% `#4D000000`) + **`ModalHost`**, which renders the **whole stack bottom→top as layered entries** (later modals overlay earlier ones — a confirmation sits on top of the view beneath it); only the top entry is hit-testable. The backdrop hides while the **screenshot viewer modal** is the top entry (its own opaque backdrop covers the window). When the stack empties, the screen under the first modal is **restored** (`_screenBeforeModal`, e.g. a game modal opened from the library lands back in the library, not the dashboard).
- **`ConfirmationModal`** — reusable prompt: header, message and **two controller-friendly option buttons** (Left/Right selects, A activates the selection, B cancels); resolves `bool?` — option 1 `true`, option 2 `false`, B `null` (callers decide what cancel means, e.g. "stay put"); fixed 640×300 card centered on its **own 30% black backdrop** filling the content area; reused by Manage Profiles delete / import-replace / export-saves / unsaved-changes prompts.
- Router dispatch order: **modal stack (top modal) → overlay screens → dashboard**; modals swallow all input while open. The router is command-driven: key/gamepad → `NavigationCommand` → per-layer handlers (`HandleLibrary`/`HandleGallery`/`MoveUp`/`MoveDown`/…).

---

## 3. Architecture

```
source/XeniaManager.BigScreen/
├── App.axaml / App.axaml.cs        # Application shell, theme wiring, localization init, DI container (App.Services)
├── Program.cs                       # Entry point; redirects base dir to the base app's folder
├── ViewLocator.cs                   # VM → View resolution (ViewModels.XViewModel → Views.XView)
├── Views/
│   ├── Shell/
│   │   └── MainWindow.axaml(.cs)      # Shell (FAAppWindow, fullscreen forced in code): header, background/fade layers,
│   │                                  # dashboard + overlay screens, input routing, built-in splash
│   ├── Dashboard/
│   │   └── DashboardView.axaml(.cs)   # Recent games row + options row + empty stub + launch reveal fade
│   ├── Screens/
│   │   ├── LibraryView.axaml(.cs)     # Library carousel + list, clamped scroll, details pane + empty stub
│   │   ├── GalleryView.axaml(.cs)     # Gallery grid + empty stub (viewer lives in Modals)
│   │   └── SettingsView.axaml(.cs)    # Settings screen (background image picker; row classes, BringIntoView, editors)
│   └── Modals/
│       ├── GameModalView.axaml(.cs)   # Game modal: icon+title, options list (left), live panes (right)
│       ├── AchievementsPaneView.axaml(.cs)   # Achievements pane (stats, sort, scrollable rows)
│       ├── GameScreenshotsPaneView.axaml(.cs) # Per-game screenshot grid pane
│       ├── InstalledContentPaneView.axaml(.cs) # Title Updates / Marketplace rows + delete
│       ├── PatchesPaneView.axaml(.cs) # Patches pane (entries: toggle/remove + download)
│       ├── GameSettingsPaneView.axaml(.cs)    # Curated config pane (main-settings-style cards)
│       ├── PatchDownloadView.axaml(.cs)       # Patch download modal (search + results + status)
│       ├── ScreenshotViewerView.axaml(.cs)    # Full-window screenshot viewer modal (chevrons, caption)
│       ├── DiscSelectionView.axaml(.cs) # Disc selection modal (compact card: game header + disc cards)
│       ├── ProfilePickerView.axaml(.cs) # Profile picker modal (A switches, Y = Manage, B closes)
│       └── ManageProfilesView.axaml(.cs) # Manage Profiles modal (rows + anchored create stub + edit panel)
├── ViewModels/
│   ├── Shell/
│   │   └── MainWindowViewModel.cs     # Composition root: child VMs, screen navigation, launch/quit/refresh, IsModalOpen;
│   │                                  # launch config edits (fullscreen force + profile slot injection), quit exit-code
│   ├── Dashboard/
│   │   ├── HeaderViewModel.cs         # Profile, clock, wifi + controller battery state (+ avatar chip selection)
│   │   └── DashboardViewModel.cs      # RecentGames, Options, background brush + art crossfade (TweenAvalonia)
│   ├── Screens/
│   │   ├── LibraryViewModel.cs        # Games carousel/list + sort (ScreenViewModel base)
│   │   ├── GalleryViewModel.cs        # Screenshots + sort (ScreenViewModel base)
│   │   ├── SettingsViewModel.cs       # Appearance options + persistence + quit/fullscreen/start-in-Big-Screen toggles
│   │   │                              # + library view + card image + Manage Profiles + controller row navigation
│   │   └── ScreenViewModel.cs         # Overlay-screen base: background brush + hint-bar visibility
│   ├── Modals/
│   │   ├── ModalViewModelBase.cs(.Generic.cs) # Modal lifecycle: close TCS, HandleInput, Dispose hook, hint-bar visibility
│   │   ├── IGameModalPane.cs          # Pane contract: HandleInput(NavigationCommand)
│   │   ├── GameModalViewModel.cs      # Game modal: options list + cached panes, live display, single-highlight state
│   │   ├── AchievementsPaneViewModel.cs  # GPD achievements: stats, sort, flat scroll list
│   │   ├── GameScreenshotsPaneViewModel.cs # Per-game screenshot scan (off-thread) + grid
│   │   ├── InstalledContentPaneViewModel.cs # Title Updates / Marketplace rows + confirmed delete
│   │   ├── PatchesPaneViewModel.cs    # Patch entries (toggle/remove), download modal hookup
│   │   ├── PatchDownloadViewModel.cs  # Download modal: search (generation guard), results, status
│   │   ├── GameSettingsPaneViewModel.cs # Curated config rows: dirty tracking, X saves, Save/Discard/Cancel on exit
│   │   ├── ScreenshotViewerViewModel.cs # Full-window viewer modal (step, caption); no modal backdrop
│   │   ├── DiscSelectionViewModel.cs  # Disc selection modal (typed int? result; skip-missing navigation)
│   │   ├── ConfirmationModalViewModel.cs # Reusable 2-option prompt (Left/Right + A, B cancels → null)
│   │   ├── ProfilePickerViewModel.cs  # Picker modal: rows, switch-active, Y → Manage Profiles
│   │   └── ManageProfilesViewModel.cs # Manage modal: rows + stub, edit fields + dirty tracking, create/delete/import/export
│   ├── ViewModelBase.cs
│   └── Items/
│       ├── GameCardViewModel.cs       # Core Game ref, Title, Boxart/DiscArt layers (card_image_mode), IsSelected
│       ├── GameDetailsViewModel.cs    # Details pane: local stats + DB info (bio/genre/developer/publisher/released)
│       ├── ScreenshotItemViewModel.cs # Path, Title, CapturedAt (+ text), GameTitle, Image, IsSelected
│       ├── OptionsCardViewModel.cs    # Title, Icon, TargetScreen
│       ├── GameActionItemViewModel.cs # Game modal option row: Title, Icon, GameModalPane, IsSelected
│       ├── AchievementItemViewModel.cs # Achievement row: name/description/gamerscore/date, image when unlocked;
│       │                              # spoiler gating (secret + locked → Hidden Achievement, no score)
│       ├── ContentItemViewModel.cs    # Installed content row: HeaderFile + reconstructed delete path
│       ├── ConfigRowViewModel.cs      # Curated config row: label/section, control type, dirty tracking + restore
│       ├── PatchEntryItemViewModel.cs # Patch entry: name/author/enabled, ToPatchEntry roundtrip
│       ├── PatchDownloadItemViewModel.cs # Download result: name + Canary/Netplay source badge
│       ├── PatchListRowViewModel.cs   # Patches list row: patch entry or download/remove action
│       ├── ProfileItemViewModel.cs    # Profile row: gamertag, country · language, gamerscore (async), IsSelected/IsActive
│       ├── CreateProfileStubViewModel.cs # Anchored "Create New Profile" row (ISelectable)
│       ├── SettingsRowViewModel.cs    # Settings row: kind + selection (ISelectable)
│       ├── ManageProfilesRowViewModel.cs # Edit-panel row: kind + selection (ISelectable)
│       ├── GamepadItemViewModel.cs    # Gamepad card row: name, status, battery (ISelectable)
│       └── DiscOptionItemViewModel.cs # Disc card row: label, last-played/missing status, IsSelected (ISelectable)
├── Controls/
│   ├── Cards/
│   │   ├── GameCard.axaml(.cs)        # Dashboard game tile (grow 153→228, animated)
│   │   ├── OptionsCard.axaml(.cs)     # Dashboard option tile (339×135, transparent 5px base border)
│   │   ├── ScreenshotCard.axaml(.cs)  # Gallery tile: 16:9 screenshot, 6px corners
│   │   ├── LibraryCard.axaml(.cs)     # Carousel card: box art + title + stat rows (rounded art clip)
│   │   ├── LibraryListItem.axaml(.cs) # List row: disc icon + title (accent on select/hover)
│   │   ├── GameDetailsPanel.axaml(.cs) # Details pane: art, stats, DB bio + metadata strip (+ version chip + compat row)
│   │   ├── GameActionRow.axaml(.cs)   # Game modal option row (accent on select/hover)
│   │   ├── DiscOptionCard.axaml(.cs)  # Disc selection card (filled disc icon + label + status, dimmed when missing)
│   │   ├── AchievementRow.axaml(.cs)  # Achievement row (image/lock icon, name, description, star + gamerscore)
│   │   └── ContentRow.axaml(.cs)      # Installed content row (display name + file name + delete icon)
│   ├── Modals/
│   │   ├── ModalHost.axaml(.cs)       # Renders the modal stack bottom→top (only the top gets input)
│   │   └── ConfirmationModal.axaml(.cs) # Generic prompt: header, message, two Left/Right option buttons
│   ├── Profiles/
│   │   ├── ProfileButton.axaml(.cs)   # Header avatar chip: focusable, accent outline, opens the picker
│   │   ├── ProfileRow.axaml(.cs)      # Profile list row: avatar, gamertag, country · language, gamerscore
│   │   └── CreateProfileRow.axaml(.cs) # "+ Create New Profile" row (anchored beneath the list)
│   ├── Settings/
│   │   ├── GamepadCard.axaml(.cs)     # Controller row: name, status, battery icon + % (accent on hover/select)
│   │   ├── ColorPickerField.cs        # Swatch + hex + palette popup
│   │   └── PalettePicker.cs           # Swatch row
│   ├── Primitives/
│   │   ├── IconStat.axaml(.cs)        # Icon + text stat row
│   │   ├── InputHint.axaml(.cs)       # Keycap + label hint
│   │   └── EmptyState.axaml(.cs)      # Shared stub (disc/camera) control
│   └── Splash/
│       ├── SplashScreenView.axaml(.cs) # FA splash visuals: logo, live status, tweened bar, radial background;
│       │                               # forces the splash window fullscreen
│       └── AppSplashScreen.cs         # IFAApplicationSplashScreen: hosts the splash view + runs the boot pipeline
├── Converters/
│   └── ColorJsonConverter.cs        # ARGB hex JSON converter for Color values
├── Factories/
│   ├── BackgroundBrushFactory.cs    # Static brush builders: linear/radial/solid from a colour + vignette
│   ├── IconFactory.cs               # Status-to-icon mapping: tiered battery + network (WiFi/PlugConnected/WiFiOff)
│   ├── GameCardFactory.cs           # Dashboard/library card VM construction
│   ├── GameModalPaneFactory.cs      # Game modal pane construction per GameModalPane
│   └── ModalFactory.cs              # Modal VM construction
├── Services/
│   ├── BaseAppLocator.cs            # Resolves the base Xenia Manager folder (--base-dir / side-by-side / sibling)
│   ├── BackgroundService.cs         # Settings load/save, brushes via BackgroundBrushFactory, ApplyResources
│   ├── GameDataCache.cs             # Session cache: content scans, patch files, achievement GPDs (+ lazy configs)
│   ├── DashboardNavigationController.cs # Row state machine (incl. profile row), movement, option activation
│   ├── GameLibraryService.cs        # Wraps Core GameManager: load, game list, recent-games selection
│   ├── IBackgroundService.cs / IGameLibraryService.cs / IProfileService.cs / IScreenshotLibraryService.cs / IModalService.cs
│   ├── InputRouter.cs               # Command-driven: key/gamepad → NavigationCommand → per-layer dispatcher
│   ├── ModalService.cs              # Push/pop modal stack: ShowAsync (typed result), Close pops + disposes
│   ├── ProfileService.cs            # All Canary profiles, active profile (profile_xuid), switch/refresh, launch
│   │                               # safety net, per-game achievement stats, per-profile gamerscore, XConfig sync
│   ├── ScreenshotLibraryService.cs  # Recursive screenshot scan, extension filter, game-title matching, filename metadata
│   └── ServiceConfigurator.cs       # DI registration (singleton services + VMs, App.Services)
├── Constants/
│   ├── AppConstants.cs              # BaseAppExecutable, RecentGamesLimit, SettingsFileName, config option names
│   ├── TimingConstants.cs           # Polls, fade durations, splash stage/done/minimum timings
│   ├── SplashStages.cs              # Boot pipeline progress values (7 stages)
│   ├── FormatConstants.cs           # Clock/capture-date formats, screenshot file-name timestamp format, XUID format
│   ├── XboxConstants.cs             # ProfileContentTitleId (FFFE07D1)
│   └── LayoutConstants.cs           # Vignette step, gradient mixes, accent tint step, carousel fallbacks
├── Utilities/
│   ├── SelectionHelper.cs           # ISelectable + single-selection helpers (move/select/resort-preserving)
│   ├── EnumCycleHelper.cs           # Generic enum + colour palette cycling
│   ├── ScreenshotFileNameParser.cs  # Decodes game ID + capture timestamp from screenshot file names
│   ├── ImageFormats.cs              # Shared screenshot extensions + file-picker patterns
│   ├── ProfileRowsHelper.cs         # Shared profile row building (active-first + alphabetical) + async gamerscore
│   ├── ProfileImportExportHelper.cs # Import/export pickers + profile replacement
│   ├── ScreenshotGridLayout.cs      # Gallery grid layout math
│   ├── ScrollViewerHelper.cs        # Scroll-to-selected helpers
│   ├── GamertagValidator.cs         # Gamertag regex + length validation
│   ├── TaskUtilities.cs             # RunSafely<T>: logged fire-and-forget task execution
│   └── AccountInfoExtensions.cs     # PathXuidText() hex helper
├── Models/
│   ├── Settings/
│   │   ├── DashboardSettings.cs      # Persisted user-facing options (incl. primary_controller_guid, profile_xuid, time_format)
│   │   ├── BackgroundMode.cs / BackgroundModeOption.cs
│   │   ├── XConfigResolutionOption.cs # XConfig resolution dropdown option ("R" prefix stripped)
│   │   ├── LibraryViewMode.cs / LibraryViewModeOption.cs
│   │   ├── CardImageMode.cs / CardImageModeOption.cs
│   │   └── TimeFormat.cs / TimeFormatOption.cs
│   ├── NavigationCommand.cs         # Public command set (Move/Activate/Back/CycleSort/ToggleView/Start/Details)
│   ├── SettingsRowKind.cs           # Settings screen row kinds (fixed cards + gamepad rows + XConfig)
│   ├── ManageProfilesRowKind.cs     # Manage Profiles edit-panel row kinds
│   ├── GameModalPane.cs             # The six game modal panes (Achievements…Settings)
│   ├── AchievementSort.cs           # Achieved / GamerscoreAwarded / Alphabetical
│   ├── GameStatInfo.cs              # Achievement/gamerscore counters (unlocked / total)
│   ├── LibrarySort.cs               # Alphabetical / TimePlayed / LastPlayed
│   ├── GallerySort.cs               # NewestFirst / OldestFirst / ByGame
│   ├── NetworkStatus.cs             # Disconnected / Wifi / Ethernet (header network icon)
│   └── OverlayScreen.cs             # Dashboard / Library / Gallery / Settings
└── Resources/
    ├── BigScreenStyle.axaml         # Shared card/screen-title/hint-bar/empty-state styles
    ├── Language/en.axaml            # Full key set for every user-facing string (+ Core playtime keys)
    └── Themes/DarkGradient.axaml · Controls.axaml
```

**Navigation:** the shell hosts the dashboard as a `ContentControl` (`MainWindowViewModel.Dashboard`) and the three overlay screens as pre-instantiated `ContentControl`s whose content never changes (`MainWindowViewModel.Library` / `Gallery` / `Settings`, visibility flipped via `Is*Screen`). Views are created **once at startup** — opening a screen is a pure visibility flip (instant), and all boot-time work (profile, library, screenshot scan) happens behind the built-in splash. The screenshot viewer opens as a **modal on the modal stack** from both the gallery and the game modal's screenshots pane. The window interacts with the overlay views via live visual-tree lookups (`Find<T>()`) for focus/scroll requests raised by `DashboardNavigationController`.

**Boot:** `Program.Main` → `App` builds DI → `desktop.MainWindow = MainWindow` (an `FAAppWindow`) → FluentAvalonia shows its built-in splash (`AppSplashScreen`) → `RunTasks` dispatches `MainWindowViewModel.InitializeAsync` (staged loads, cancellable, per-stage dwell, progress via `IProgress`) onto the UI thread → splash closes on completion (~3s minimum) → fullscreen dashboard revealed, input un-gated (`IsInitialized`), launch fade runs.

**Data flow**
- DI: `App.Services` (built by `ServiceConfigurator.ConfigureServices()`) → singleton services (`IBackgroundService`, `IProfileService`, `IGameLibraryService`, `IScreenshotLibraryService`, `IModalService`, `IGamepadInputService` (from Core), `DashboardNavigationController`, `InputRouter`) + `MainWindowViewModel` + `MainWindow` (parameterless ctor resolving from `App.Services` — the XAML loader requires it).
- Input: keyboard (`OnWindowKeyDown`) and gamepad (`IGamepadInputService.ButtonPressed`, from Core) → `InputRouter` (key/button → `Command` → per-screen handler) → `DashboardNavigationController` actions (move/select/activate) → selection `IsSelected` → styled visuals; the controller raises focus/scroll requests the window fulfils. All input is gated until the boot pipeline completes.
- Navigation: `OptionsCardViewModel.TargetScreen` → `MainWindowViewModel.OpenScreen()` → `CurrentScreen` (a screen VM) → the matching overlay's `IsVisible` flips; null shows the dashboard.
- Selection: focus/click on a card → `IsSelected` → styled via `.selected` class / pseudo-class. **Dashboard (`DashboardViewModel.RecentGames`) and library (`LibraryViewModel.Games`) hold separate VM instances with independent selections.**
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
- `GamepadInputService` / `IGamepadInputService` — SDL3 gamepad polling: multi-gamepad tracking via `GamepadDeviceCollection` (all pads open, per-pad battery/GUID, primary selection, `Rescan`/`ReloadMappings`), pure mapping in `GamepadButtonMapper` + `StickTracker`; input flows from the primary pad only. **Hold-repeat:** navigation directions (D-pad + left stick via `StickTracker.HeldButton`) raise once on entry, then re-raise after 400ms and every 100ms while held (button-up, stick-centred and controller-removed all cancel); action buttons raise exactly once per press.
- `ReleaseDateFormatter.Format(...)` — ordinal release dates (+ unit tests).
- `TweenAvalonia` (0.3.0 NuGet) — the tween engine extracted from the original in-repo `Core/Tweening`; used for the art crossfade and dashboard reveal.

---

## 4. Design system

### Tokens (`DarkGradient.axaml`)
| Token                                                             | Purpose                                                                 |
|-------------------------------------------------------------------|-------------------------------------------------------------------------|
| `CardBackground` / `CardTitleBar`                                 | Card surfaces                                                           |
| `CardBorder`                                                      | Reserved borders on non-dashboard cards; dashboard cards reserve a transparent 5px stroke (accent at the same 5px on hover/selection — no layout shift) |
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

## 5. Build & run

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

## 6. Known limitations & unverified

- **Dual-disc selection** — implemented but untested: no multi-disc games were available to verify with.
- **Title Updates / Marketplace Content / Patches / Game Settings panes** — ported but never verified against real game data; the patches pane's toggle got a feedback fix (Aug 15) but no full walkthrough.
- **Translations** — English key set only; other languages deferred (the app is fully keyed, new locales are new files only).
- **Gallery sort** — stays at the three orders (Newest First / Oldest First / By Game); the desktop-parity sort expansion (Title, Time Played, Compatibility, TitleId, MediaId, XeniaVersion, Last Played) was **cancelled by owner decision**.
- **Icon** — BigScreen ships a unique icon (`Assets/icon.ico`); the maintainer suggested reusing the main app's icon instead — unresolved.
- **Full config editing** stays in the main app; BigScreen exposes the curated 18-option subset only.
- **Animation** — dashboard reveal is 1000ms (`TimingConstants.LaunchFadeDuration`); the artwork crossfade is 300ms legs with a latest-wins hard reset. Everything else (overlays, modals, viewer) opens instantly by design.

---

## 7. Appendix — Roadmap history (complete)

> All items implemented and shipped in PR #572. Kept for provenance.

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
- [x] Dashboard row capped at 8 (fewer games = fewer cards, no empty slots)

### 5.8 SDL3 gamepad input
- [x] `ppy.SDL3-CS` + `AllowUnsafeBlocks` for the pointer API
- [x] `GamepadInputService` (now in Core): graceful init failure, UI-thread poll, deadzone edge detection; D-pad, left stick and bumpers normalise onto the D-pad values; opens/adds/closes gamepads; raw input traced
- [x] D-pad + A/B mapped to the selection model
- [x] Dashboard **row-state driven** (not keyboard focus): Up/Down switches rows with a fixed column mapping; A acts on the active row only; options cleared when returning to games; no games = options row stays active
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
- [x] Compliance sweep against root `CONTRIBUTING.md` (naming, AXAML property order, XML docs, logger error handling, formatting, MVVM)

### 5.11 Post-sweep engineering
- [x] Command-driven `InputRouter` (key/button → `Command` → per-screen handler)
- [x] DI (`ServiceConfigurator` + `App.Services`); single `IGamepadInputService` singleton (double-instantiation was dropping ~half of gamepad input — fixed)
- [x] Logging density raised with level discipline
- [x] `Constants/` extraction — no magic values left in C#
- [x] Declaration order, expression bodies, explicit types (repo convention)
- [x] Performance: pre-instantiated overlays, boot-time screenshot scan, first-game pre-selection, pre-warmed background art, viewer↔gallery selection sync

### 5.12 Main app launch button + splash + localisation
- [x] Big Screen launch button (main app): nav item + `NavigationService` launch (side-by-side or repo sibling); missing exe → localised warning
- [x] Boot splash: FA built-in `AppWindow.SplashScreen`, deferred startup, seven staged statuses, tweened bar, 3s minimum, saved-colour background; input gated until `IsInitialized`
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
- [x] Up/Down navigates the list; scroll-to-selected handles both geometries

### 5.14 Card image mode + hint bars
- [x] `CardImageMode` (Box Art / Icon) persisted as `card_image_mode`, **default Icon**; Settings dropdown; dashboard cards swap live
- [x] Box Art mode keeps the disc icon as fallback when art is missing
- [x] Library hint bar: Back (B red) → Play (A green) → Sort (X blue) → Swap View (faded-white `CaretLeft`, `HintKeyBack` token)
- [x] Media hint bar: Back → Select (A) → Sort (X blue)

### 5.15 Post-review: housekeeping
- [x] **T1.** Formatting sweep (Rider Code Clean-up; `en.axaml` alignment in both apps)
- [x] **T2.** Delete test artwork (`Resources/Art/fr65z3.jpg`, `SL1qfH.jpg`)
- [x] **T3.** Move `ColorJsonConverter` → `Converters/`
- [x] **T4.** Boot splash via FA's built-in `AppWindow.SplashScreen`; delete `SplashWindow`; gradient math → `Factories/BackgroundBrushFactory.cs`

### 5.16 Controller input: primary controller + rescan (Core)
- [x] **T5.** All gamepads opened; `ConnectedGamepads` snapshots; `SetPrimary`/`SetPrimaryByGuid` (GUID persisted, restored at boot); `Rescan()`/`ReloadMappings()`; input from the primary pad only; refactored into `GamepadDeviceCollection` + `GamepadButtonMapper` + `StickTracker`

### 5.17 Profiles & identity
- [x] **T6.** Profile switching — active profile persisted (`profile_xuid`); header + per-game stats follow
- [x] **T7.** Manage Profiles modal — port desktop dialog (create/delete/import/export)

### 5.18 Game actions (game modal)
- [x] **T8.** Game modal — Y/right-click opens; options list with live panes; A/Right enters, B/Left returns; single-highlight contract
- [x] **T9.** Achievements pane — X-cycled sort (Achieved/Gamerscore/Alphabetical), scroll-to-selected, images only when unlocked
- [x] **T10.** Title Updates pane — rows + confirmed delete
- [x] **T11.** Marketplace Content pane — shared pane, per-menu init
- [x] **T12.** Screenshots pane — per-game grid; A opens the shared viewer
- [x] **T13.** Patches pane — toggle/remove + dedicated download modal (entry editing stays in the main app)
- [x] **T14.** Game Settings pane — 18 curated primary options (toggles/sliders/dropdowns) as main-settings-style cards; manual save (X) + Save/Discard/Cancel on exit; full config editor stays in the main app
- [x] **T15.** Disc selection modal on multi-disc launch — A launches, B cancels, missing discs skipped

### 5.19 Dashboard, header & settings
- [x] **T16.** Time format setting — 12h/24h persisted; clock + capture dates follow
- [x] **T17.** Controllers section — `GamepadCard` rows with primary/secondary status + battery
- [x] **T18.** Settings controller navigation — D-pad rows, A activates (primary controller, dropdowns, toggles, colours)
- [x] **T19.** Media → Gallery rename (screenshots only; installed content stays in the game modal)
- [x] **T20.** Boxart tile sizing — portrait, bottom-anchored, top ~12% cropped
- [x] **T21.** Header — ethernet-aware network icon; 10-stage battery
- [x] **T22.** Background default → Dynamic; settings dropdowns built in enum order
- [x] **T23.** Xenia version icon in the list-view details pane (Core converters, hover tooltip)
- [x] **T24.** Compatibility rating row in the details pane (dot + label, DB URL tooltip)
- [x] **T25.** Achievements unlocked-first sort + spoiler gating (secret achievements hidden while locked)
- [x] **T26.** XConfig section — resolution dropdown only (bottom of settings, hidden without XConfig)
- [x] **T27.** Input gating while a game runs (all window handlers)
- [x] **T35.** Launch games in fullscreen — Preferences toggle (default on, persisted); sets `Display.fullscreen` at launch, restores after

### 5.20 Desktop app integration
- [x] **T28.** Hide + disable the main window while BigScreen is open; restore on exit
- [x] **T29.** BigScreen start by default — `--bigscreen` CLI arg + "Start in Big Screen" toggle (persisted)

### 5.21 Gallery sort expansion (full desktop parity)
- [ ] **T30.** ~~Expand gallery sort with the desktop's full list: Title, Time Played, Compatibility, TitleId, MediaId, XeniaVersion, Last Played (alongside Newest/Oldest/By Game)~~ — **cancelled by owner decision: the gallery keeps its three orders (Newest First / Oldest First / By Game); no sort modifications.**

### 5.22 Screen animations
- [x] **T31.** Dashboard reveal fade — header + card rows fade in (1000ms) after launch; overlays/modals open instantly (no tweens outside the dashboard)

### 5.23 Input, animation & config follow-ups
- [x] **T32.** Gamepad hold-repeat (Core) — held buttons re-raise after a delay at a repeat rate
- [x] **T33.** Background fade hard reset — single cancellable fade instance, restarted on every art swap
- [x] **T34.** Trim the config editor — superseded by the T14 curated rewrite (Notification Sound/WinKey/Logging never shipped)
