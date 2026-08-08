using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.VisualTree;
using XeniaManager.BigScreen.Controls;
using XeniaManager.BigScreen.Models;
using XeniaManager.BigScreen.ViewModels;
using XeniaManager.BigScreen.ViewModels.Items;

namespace XeniaManager.BigScreen.Views;

public partial class MainWindow : Window
{
    private SettingsOverlay? _settingsOverlay;

    public MainWindow()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        KeyDown += OnWindowKeyDown;
        GamesRow.AddHandler(GotFocusEvent, OnCardGotFocus, RoutingStrategies.Bubble, true);
        OptionsRow.AddHandler(GotFocusEvent, OnCardGotFocus, RoutingStrategies.Bubble, true);
        OptionsRow.AddHandler(PointerPressedEvent, OnOptionCardPressed, RoutingStrategies.Bubble, true);
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        // Start with the first card selected
        if (DataContext is MainWindowViewModel vm)
        {
            vm.QuitRequested += OnQuitRequested;
            if (vm.Games.Count > 0)
            {
                vm.Games[0].IsSelected = true;
            }

            _settingsOverlay = this.GetVisualDescendants().OfType<SettingsOverlay>().FirstOrDefault();
            if (_settingsOverlay != null)
            {
                _settingsOverlay.PickImageRequested += async (_, _) => await PickBackgroundImageAsync();
            }
        }
    }

    private void OnQuitRequested(object? sender, System.EventArgs e)
    {
        Close();
    }

    /// <summary>
    /// Handles Enter to activate an option card and B/Escape to close overlays.
    /// </summary>
    private void OnWindowKeyDown(object? sender, KeyEventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm)
        {
            return;
        }

        if (vm.IsOverlayOpen)
        {
            if (e.Key == Key.B || e.Key == Key.Escape)
            {
                vm.CloseOverlay();
                RestoreOptionFocus();
                e.Handled = true;
            }
            return;
        }

        if (e.Key is Key.Enter or Key.Space)
        {
            if (FocusManager?.GetFocusedElement() is Control { DataContext: OptionsCardViewModel option })
            {
                _lastActivationWasMouse = false;
                ActivateOption(option);
                e.Handled = true;
            }
        }
    }

    /// <summary>
    /// Opens the screen for the given option card, or quits for the Quit card.
    /// </summary>
    private void ActivateOption(OptionsCardViewModel option)
    {
        if (DataContext is not MainWindowViewModel vm)
        {
            return;
        }

        if (option.TargetScreen == OverlayScreen.None)
        {
            vm.Quit();
            return;
        }

        vm.OpenScreen(option.TargetScreen);
        FocusOverlay();
    }

    private bool _lastActivationWasMouse;

    private void OnOptionCardPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.Source is not Control control)
        {
            return;
        }

        if (control.GetSelfAndVisualAncestors().OfType<OptionsCard>().FirstOrDefault()
            is { DataContext: OptionsCardViewModel option })
        {
            _lastActivationWasMouse = true;
            ActivateOption(option);

            // A mouse click must not leave the card focused/selected - only the
            // controller (IsSelected via keyboard focus) or hover should show it
            if (DataContext is MainWindowViewModel vm)
            {
                foreach (OptionsCardViewModel o in vm.Options)
                {
                    o.IsSelected = false;
                }
            }
        }
    }

    /// <summary>
    /// Moves focus into the open overlay (first focusable element).
    /// </summary>
    private void FocusOverlay()
    {
        if (DataContext is MainWindowViewModel vm && vm.IsSettingsScreen && _settingsOverlay != null)
        {
            _settingsOverlay.FocusFirst();
        }
        else
        {
            // Focus the overlay panel itself so keys route through the window handler
            Focus();
        }
    }

    /// <summary>
    /// Restores focus to the previously selected option card after closing an overlay.
    /// Skipped when the overlay was opened with a mouse click - the card stays unfocused.
    /// </summary>
    private void RestoreOptionFocus()
    {
        if (_lastActivationWasMouse)
        {
            return;
        }

        if (DataContext is not MainWindowViewModel vm)
        {
            return;
        }

        OptionsCardViewModel? selected = vm.Options.FirstOrDefault(o => o.IsSelected);
        if (selected == null)
        {
            return;
        }

        OptionsCard? card = OptionsRow.GetVisualDescendants().OfType<OptionsCard>()
            .FirstOrDefault(c => ReferenceEquals(c.DataContext, selected));
        card?.Focus();
    }

    /// <summary>
    /// Opens a file picker and applies the chosen image as the dashboard background.
    /// </summary>
    public async Task PickBackgroundImageAsync()
    {
        FilePickerOpenOptions options = new()
        {
            Title = "Select Background Image",
            AllowMultiple = false,
            FileTypeFilter =
            [
                new FilePickerFileType("Images")
                {
                    Patterns = ["*.png", "*.jpg", "*.jpeg", "*.bmp", "*.gif", "*.webp"],
                },
            ],
        };

        IReadOnlyList<IStorageFile> files = await StorageProvider.OpenFilePickerAsync(options);
        if (files.Count == 0 || DataContext is not MainWindowViewModel vm)
        {
            return;
        }

        vm.SetBackgroundImage(files[0].Path.LocalPath);
    }

    private void OnCardGotFocus(object? sender, FocusChangedEventArgs e)
    {
        if (e.Source is not Control { DataContext: GameCardViewModel or OptionsCardViewModel } control)
        {
            return;
        }

        if (DataContext is not MainWindowViewModel vm)
        {
            return;
        }

        // Each row keeps its own independent selection; focus/click on one row
        // never clears the selection of the other
        switch (control.DataContext)
        {
            case GameCardViewModel focusedGame:
                foreach (GameCardViewModel game in vm.Games)
                {
                    game.IsSelected = ReferenceEquals(game, focusedGame);
                }
                break;
            case OptionsCardViewModel focusedOption:
                foreach (OptionsCardViewModel option in vm.Options)
                {
                    option.IsSelected = ReferenceEquals(option, focusedOption);
                }
                break;
        }
    }
}
