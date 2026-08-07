using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using XeniaManager.BigScreen.ViewModels;
using XeniaManager.BigScreen.ViewModels.Items;

namespace XeniaManager.BigScreen.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        GamesRow.AddHandler(GotFocusEvent, OnCardGotFocus, RoutingStrategies.Bubble, true);
        OptionsRow.AddHandler(GotFocusEvent, OnCardGotFocus, RoutingStrategies.Bubble, true);
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        // Start with the first card selected
        if (DataContext is MainWindowViewModel vm && vm.Games.Count > 0)
        {
            vm.Games[0].IsSelected = true;
        }
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
