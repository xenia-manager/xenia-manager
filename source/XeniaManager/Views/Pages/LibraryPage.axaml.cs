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
        if (_viewModel.DoubleClickLaunch)
        {
            return;
        }
        if (sender is Button { DataContext: GameItemViewModel vm })
        {
            if (vm.LaunchCommand.CanExecute(null))
            {
                vm.LaunchCommand.Execute(null);
            }
        }
    }

    private void OnGameButtonDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (!_viewModel.DoubleClickLaunch)
        {
            return;
        }
        if (sender is Button { DataContext: GameItemViewModel vm })
        {
            if (vm.LaunchCommand.CanExecute(null))
            {
                vm.LaunchCommand.Execute(null);
            }
        }
    }
}