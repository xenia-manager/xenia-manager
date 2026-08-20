using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using FluentAvalonia.UI.Controls;
using Microsoft.Extensions.DependencyInjection;
using XeniaManager.Core.Services;
using XeniaManager.Services;
using XeniaManager.ViewModels.Items;
using XeniaManager.ViewModels.Pages;

namespace XeniaManager.Views.Pages;

public partial class LibraryPage : UserControl
{
    // Variables
    private LibraryPageViewModel _viewModel { get; set; }
    private GameItemViewModel? _lastSelectedGame;
    private GamepadService? _gamepadService;
    private Control? _infoPopupControl;

    // Constructor
    public LibraryPage()
    {
        InitializeComponent();
        _viewModel = App.Services.GetRequiredService<LibraryPageViewModel>();
        DataContext = _viewModel;
        KeyDown += OnKeyDown;

        // Experimental: controller navigation. Only subscribes while this page is
        // actually attached to the visual tree, to avoid driving focus changes
        // while the user is on a different page (Settings, Manage, etc).
        AttachedToVisualTree += OnAttachedToVisualTree;
        DetachedFromVisualTree += OnDetachedFromVisualTree;
    }

    private void OnAttachedToVisualTree(object? sender, Avalonia.VisualTreeAttachmentEventArgs e)
    {
        _gamepadService = App.Services.GetRequiredService<GamepadService>();
        _gamepadService.NavigationActionTriggered += OnControllerNavigationAction;
        _gamepadService.PushNavigationContext(this);
        _viewModel.RefreshControllerNavigationVisibility();
    }

    private void OnDetachedFromVisualTree(object? sender, Avalonia.VisualTreeAttachmentEventArgs e)
    {
        if (_gamepadService != null)
        {
            _gamepadService.NavigationActionTriggered -= OnControllerNavigationAction;
            _gamepadService.PopNavigationContext(this);
            _gamepadService = null;
        }
        _viewModel.ClearControllerFocus();
    }

    /// <summary>
    /// Handles a navigation action from the controller. Runs on the Timer's thread pool
    /// thread, so all UI/ViewModel interaction is marshalled to the UI thread.
    /// </summary>
    private void OnControllerNavigationAction(object? sender, ControllerNavigationAction action)
    {
        Dispatcher.UIThread.Post(() =>
        {
            // Only react while this page is the active navigation context. Opening a modal
            // dialog (e.g. DiscSelectionDialog) pushes its own context on top of ours, so
            // this check prevents the grid from also moving underneath the dialog.
            if (_gamepadService == null || !_gamepadService.IsActiveNavigationContext(this))
            {
                return;
            }

            // ToggleView (the controller's View button) works regardless of which view is
            // currently active
            if (action == ControllerNavigationAction.ToggleView)
            {
                CloseInfoPopup();
                if (_viewModel.ToggleGridViewCommand.CanExecute(null))
                {
                    _viewModel.ToggleGridViewCommand.Execute(null);
                }
                return;
            }

            switch (action)
            {
                case ControllerNavigationAction.Confirm:
                    CloseInfoPopup();
                    _viewModel.LaunchControllerFocusedGame();
                    break;
                case ControllerNavigationAction.Back:
                    // If the info popup is open, B closes it first rather than immediately
                    // backing out to the side menu - matches the "B closes what's on top"
                    // expectation from the disc selection dialog
                    if (_infoPopupControl != null)
                    {
                        CloseInfoPopup();
                        break;
                    }
                    // Return focus to the side navigation menu, same as pressing B on a
                    // console dashboard to back out of a screen
                    _viewModel.ClearControllerFocus();
                    App.Services.GetRequiredService<NavigationService>().FocusNavigationMenu();
                    break;
                case ControllerNavigationAction.Info:
                    ToggleFocusedGameInfoPopup();
                    break;
                case ControllerNavigationAction.Menu:
                    CloseInfoPopup();
                    OpenFocusedGameContextMenu();
                    break;
                default:
                    // Any directional movement dismisses the info popup, since it was
                    // anchored to the game that's no longer focused
                    CloseInfoPopup();
                    // Grid view has multiple columns per row; List view is a single column
                    // (each row is one game), so Left/Right behave like Up/Down there
                    int columns = _viewModel.IsGridView ? CalculateVisibleColumns() : 1;
                    _viewModel.MoveControllerFocus(action, columns);
                    break;
            }
        });
    }

    /// <summary>
    /// Shows/hides the info popup (title, compatibility, playtime, disc count - the same
    /// content shown on mouse hover) for the game currently focused via controller.
    /// Grid view only: the game's card doesn't show these details inline the way List
    /// view's columns already do, so the popup is most useful there.
    /// Forces placement relative to the game's card (Placement=Bottom) instead of the
    /// default Pointer placement, which is unreliable when opened programmatically without
    /// a recent real mouse movement (a known Avalonia quirk - the tooltip can appear
    /// anywhere on screen based on a stale last-known pointer position).
    /// </summary>
    private void ToggleFocusedGameInfoPopup()
    {
        if (!_viewModel.IsGridView)
        {
            return;
        }

        Control? control = GetFocusedGameControl();
        if (control == null)
        {
            return;
        }

        bool isOpen = ToolTip.GetIsOpen(control);
        if (isOpen)
        {
            ToolTip.SetIsOpen(control, false);
            _infoPopupControl = null;
            return;
        }

        // Close any other open info popup first (e.g. if the focus moved without closing it)
        CloseInfoPopup();

        ToolTip.SetPlacement(control, PlacementMode.Bottom);
        ToolTip.SetIsOpen(control, true);
        _infoPopupControl = control;
    }

    /// <summary>
    /// Closes the currently-open controller info popup, if any. Called when the controller
    /// focus moves to a different game, when B is pressed, or when the page loses the
    /// controller navigation context, so the popup never lingers pointing at a game that's
    /// no longer selected.
    /// </summary>
    private void CloseInfoPopup()
    {
        if (_infoPopupControl != null)
        {
            ToolTip.SetIsOpen(_infoPopupControl, false);
            _infoPopupControl = null;
        }
    }

    /// <summary>
    /// Opens the context menu (same content as right-click) for the game currently focused
    /// via controller, and makes its top-level items navigable with the D-Pad/A/B while open.
    ///
    /// Note: only top-level menu items (Content, Patches, Create Shortcut, Compatibility Page,
    /// Edit Game, Remove Game) are controller-navigable in this first pass. Their submenus
    /// (e.g. Content > Installed Content) still require mouse/keyboard - simulating keyboard
    /// events to drive Avalonia's built-in menu keyboard navigation turned out to be
    /// unreliable in testing (several open Avalonia issues/discussions about RaiseEvent not
    /// reliably reaching menu popups), so this uses the same manual cursor approach as
    /// DiscSelectionDialog instead, which is confirmed to work.
    /// </summary>
    /// <summary>
    /// Opens the context menu (same content as right-click) for the game currently focused
    /// via controller, and makes it fully navigable with the D-Pad/A/B, including nested
    /// submenus (e.g. Content > Installed Content).
    ///
    /// Uses a manual cursor (highlighting the currently-selected MenuItem via a style class)
    /// rather than simulating keyboard events to drive Avalonia's built-in menu keyboard
    /// navigation, since that turned out to be unreliable in testing. A stack of "menu levels"
    /// tracks which submenu is currently being navigated: pressing Confirm on an item with
    /// children opens its submenu and pushes a new level; pressing Back pops back up one level
    /// (or closes the whole menu if already at the top level).
    /// </summary>
    private void OpenFocusedGameContextMenu()
    {
        Control? control = GetFocusedGameControl();
        if (control?.ContextFlyout is not MenuFlyout menuFlyout || _gamepadService == null)
        {
            return;
        }

        List<MenuItem> rootItems = menuFlyout.Items.OfType<MenuItem>().Where(m => m.IsEnabled).ToList();
        if (rootItems.Count == 0)
        {
            return;
        }

        object navigationOwner = new object();

        // Each entry is one level of the menu currently being navigated: its list of
        // (enabled) sibling items, the currently-highlighted index within them, and (for
        // every level except the root) the parent MenuItem whose submenu this list belongs to.
        List<(List<MenuItem> Items, int CursorIndex, MenuItem? Parent)> levelStack = [(rootItems, -1, null)];

        void SetCursor(int levelIndex, int newIndex)
        {
            (List<MenuItem> items, int oldIndex, MenuItem? parent) = levelStack[levelIndex];
            if (oldIndex >= 0 && oldIndex < items.Count)
            {
                items[oldIndex].Classes.Remove("controllerCursor");
            }
            if (newIndex >= 0 && newIndex < items.Count)
            {
                items[newIndex].Classes.Add("controllerCursor");
            }
            levelStack[levelIndex] = (items, newIndex, parent);
        }

        EventHandler<ControllerNavigationAction>? controllerHandler = null;
        controllerHandler = (_, action) =>
        {
            Dispatcher.UIThread.Post(() =>
            {
                if (!_gamepadService.IsActiveNavigationContext(navigationOwner))
                {
                    return;
                }

                int currentLevel = levelStack.Count - 1;
                (List<MenuItem> items, int cursorIndex, MenuItem? parent) = levelStack[currentLevel];

                switch (action)
                {
                    case ControllerNavigationAction.Up:
                        SetCursor(currentLevel, cursorIndex <= 0 ? items.Count - 1 : cursorIndex - 1);
                        break;
                    case ControllerNavigationAction.Down:
                        SetCursor(currentLevel, cursorIndex < 0 ? 0 : (cursorIndex + 1) % items.Count);
                        break;
                    case ControllerNavigationAction.Confirm:
                    case ControllerNavigationAction.Right:
                        if (cursorIndex < 0 || cursorIndex >= items.Count)
                        {
                            break;
                        }

                        MenuItem selected = items[cursorIndex];
                        List<MenuItem> children = selected.Items.OfType<MenuItem>().Where(m => m.IsEnabled).ToList();
                        if (children.Count > 0)
                        {
                            // Descend into the submenu: open it and push a new navigation level
                            selected.Open();
                            levelStack.Add((children, -1, selected));
                            SetCursor(levelStack.Count - 1, 0);
                        }
                        else if (action == ControllerNavigationAction.Confirm)
                        {
                            // Leaf item: only Confirm (A) actually activates it, Right just
                            // "enters" a submenu if there is one and does nothing otherwise
                            selected.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(MenuItem.ClickEvent));
                            menuFlyout.Hide();
                        }
                        break;
                    case ControllerNavigationAction.Back:
                    case ControllerNavigationAction.Left:
                        if (levelStack.Count > 1)
                        {
                            // Go back up one submenu level instead of closing the whole menu
                            SetCursor(currentLevel, -1);
                            levelStack.RemoveAt(currentLevel);
                            parent?.Close();
                        }
                        else
                        {
                            menuFlyout.Hide();
                        }
                        break;
                    case ControllerNavigationAction.Menu:
                        menuFlyout.Hide();
                        break;
                }
            });
        };

        void OnOpened(object? s, EventArgs e)
        {
            _gamepadService.PushNavigationContext(navigationOwner);
            _gamepadService.NavigationActionTriggered += controllerHandler;
            SetCursor(0, 0);
        }

        void OnClosed(object? s, EventArgs e)
        {
            // Clear any cursor highlight left on whichever level was active when closed
            foreach ((List<MenuItem> items, int cursorIndex, _) in levelStack)
            {
                if (cursorIndex >= 0 && cursorIndex < items.Count)
                {
                    items[cursorIndex].Classes.Remove("controllerCursor");
                }
            }
            _gamepadService.NavigationActionTriggered -= controllerHandler;
            _gamepadService.PopNavigationContext(navigationOwner);
            menuFlyout.Opened -= OnOpened;
            menuFlyout.Closed -= OnClosed;
        }

        menuFlyout.Opened += OnOpened;
        menuFlyout.Closed += OnClosed;
        menuFlyout.ShowAt(control);
    }

    /// <summary>
    /// Finds the realized container control (Button in Grid view, Border in List view)
    /// corresponding to the game currently focused via controller navigation, if any.
    /// Returns null if nothing is focused or the container isn't currently realized
    /// (e.g. scrolled out of view).
    /// </summary>
    private Control? GetFocusedGameControl()
    {
        int index = _viewModel.ControllerFocusIndex;
        if (index < 0)
        {
            return null;
        }

        FAItemsRepeater repeater = _viewModel.IsGridView ? GamesGridRepeater : GamesListRepeater;
        return repeater.TryGetElement(index) as Control;
    }

    /// <summary>
    /// Calculates how many game cards fit per row in the grid view, based on the actual
    /// rendered width of the items repeater and the current item width/spacing settings.
    /// This mirrors the sizing logic of FAUniformGridLayout closely enough for navigation
    /// purposes (doesn't need to be pixel-perfect, just consistent).
    /// </summary>
    private int CalculateVisibleColumns()
    {
        double availableWidth = GamesGridRepeater.Bounds.Width;
        double itemWidth = _viewModel.MinItemWidth;
        double spacing = _viewModel.ItemSpacing;

        if (availableWidth <= 0 || itemWidth <= 0)
        {
            return 1;
        }

        int columns = (int)Math.Floor((availableWidth + spacing) / (itemWidth + spacing));
        return Math.Max(1, columns);
    }

    // Events
    // TODO: Find a better solution for this in case we want to have keybindings
    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.A && e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            // Let the search box handle Ctrl+A when it has focus
            if (SearchTextBox.IsFocused)
            {
                return;
            }

            // Otherwise, select all games
            if (_viewModel.SelectAllGamesCommand.CanExecute(null))
            {
                _viewModel.SelectAllGamesCommand.Execute(null);
            }
            e.Handled = true;
        }
    }

    private void OnGameButtonTapped(object? sender, TappedEventArgs e)
    {
        if (sender is Button { DataContext: GameItemViewModel vm })
        {
            HandleGameTapped(vm, e);
        }
    }

    private void OnGameButtonDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (sender is Button { DataContext: GameItemViewModel vm })
        {
            HandleGameDoubleTapped(vm, e);
        }
    }

    private void OnListItemTapped(object? sender, TappedEventArgs e)
    {
        if (sender is Border { DataContext: GameItemViewModel vm })
        {
            HandleGameTapped(vm, e);
        }
    }

    private void OnListItemDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (sender is Border { DataContext: GameItemViewModel vm })
        {
            HandleGameDoubleTapped(vm, e);
        }
    }

    private void HandleGameTapped(GameItemViewModel vm, TappedEventArgs e)
    {
        // Mouse interaction takes over from controller navigation
        _viewModel.ClearControllerFocus();

        // Handle multiselect with modifiers
        if (IsMultiselectModifierPressed(e))
        {
            HandleGameSelection(vm, e);
        }
        // Launch game if not in multiselect mode and no selection active
        else if (!_viewModel.DoubleClickLaunch && !_viewModel.HasSelectedGames)
        {
            if (vm.LaunchCommand.CanExecute(null))
            {
                vm.LaunchCommand.Execute(null);
            }
        }
        // If there are selected games, single click adds to selection
        else if (_viewModel.HasSelectedGames)
        {
            HandleGameSelection(vm, e);
        }
    }

    private void HandleGameDoubleTapped(GameItemViewModel vm, TappedEventArgs e)
    {
        if (_viewModel.DoubleClickLaunch)
        {
            // Don't launch on double tap if multiselect modifier is pressed
            if (!IsMultiselectModifierPressed(e) && !_viewModel.HasSelectedGames)
            {
                if (vm.LaunchCommand.CanExecute(null))
                {
                    vm.LaunchCommand.Execute(null);
                }
            }
        }
    }

    private bool IsMultiselectModifierPressed(TappedEventArgs e)
    {
        // Check for Ctrl (multi-add) or Shift (range select)
        return e.KeyModifiers.HasFlag(KeyModifiers.Control) ||
               e.KeyModifiers.HasFlag(KeyModifiers.Shift);
    }

    private void HandleGameSelection(GameItemViewModel clickedGame, TappedEventArgs e)
    {
        List<GameItemViewModel> games = _viewModel.Games.ToList();
        int clickedIndex = games.IndexOf(clickedGame);
        if (clickedIndex < 0) return;

        if (e.KeyModifiers.HasFlag(KeyModifiers.Shift) && _lastSelectedGame != null)
        {
            // Shift+Click: Range select
            int lastIndex = games.IndexOf(_lastSelectedGame);
            if (lastIndex >= 0)
            {
                int start = Math.Min(lastIndex, clickedIndex);
                int end = Math.Max(lastIndex, clickedIndex);
                for (int i = start; i <= end; i++)
                {
                    games[i].IsSelected = true;
                }
            }
        }
        else if (e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            // Ctrl+Click: Toggle selection
            clickedGame.IsSelected = !clickedGame.IsSelected;
            _lastSelectedGame = clickedGame;
        }
        else
        {
            // Normal click with selection active: clear others and select only this one
            foreach (GameItemViewModel game in games)
            {
                game.IsSelected = false;
            }
            clickedGame.IsSelected = true;
            _lastSelectedGame = clickedGame;
        }
    }

    private void OnScrollViewerPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        // Check if the click was on the ScrollViewer itself (empty area)
        if (e.InitialPressMouseButton == MouseButton.Left)
        {
            ClearAllSelections();
        }
    }

    private void OnListPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (e.InitialPressMouseButton == MouseButton.Left)
        {
            ClearAllSelections();
        }
    }

    private void ClearAllSelections()
    {
        foreach (GameItemViewModel game in _viewModel.Games)
        {
            game.IsSelected = false;
        }
        _lastSelectedGame = null;
    }

    // Drag & Drop Support
    private void OnDragEnter(object? sender, DragEventArgs e)
    {
        e.DragEffects = DragDropEffects.Copy;
        _viewModel.IsDragOverlayVisible = true;
    }

    private void OnDragOver(object? sender, DragEventArgs e)
    {
        e.DragEffects = DragDropEffects.Copy;
    }

    private void OnDragLeave(object? sender, DragEventArgs e)
    {
        _viewModel.IsDragOverlayVisible = false;
    }

    private async void OnDrop(object? sender, DragEventArgs e)
    {
        _viewModel.IsDragOverlayVisible = false;

        IStorageItem[]? files = e.DataTransfer.TryGetFiles();
        if (files == null)
        {
            return;
        }

        List<string> supportedFiles = files
            .Select(item => item.Path.LocalPath)
            .ToList();

        if (supportedFiles.Count == 0)
        {
            return;
        }

        await _viewModel.AddDroppedFilesAsync(supportedFiles);
    }
}