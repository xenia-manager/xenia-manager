using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Microsoft.Extensions.DependencyInjection;
using XeniaManager.ViewModels.Items;
using XeniaManager.ViewModels.Pages;

namespace XeniaManager.Views.Pages;

public partial class LibraryPage : UserControl
{
    // Variables
    private LibraryPageViewModel _viewModel { get; set; }
    private GameItemViewModel? _lastSelectedGame;

    // Constructor
    public LibraryPage()
    {
        InitializeComponent();
        _viewModel = App.Services.GetRequiredService<LibraryPageViewModel>();
        DataContext = _viewModel;
    }

    // Events
    private void OnGameButtonTapped(object? sender, TappedEventArgs e)
    {
        if (sender is Button { DataContext: GameItemViewModel vm })
        {
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
    }

    private void OnGameButtonDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (_viewModel.DoubleClickLaunch && sender is Button { DataContext: GameItemViewModel vm })
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
            // Clear all selections
            foreach (GameItemViewModel game in _viewModel.Games)
            {
                game.IsSelected = false;
            }
            _lastSelectedGame = null;
        }
    }
}