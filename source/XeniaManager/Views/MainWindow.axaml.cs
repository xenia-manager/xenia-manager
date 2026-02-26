using FluentAvalonia.UI.Windowing;
using Microsoft.Extensions.DependencyInjection;
using XeniaManager.Core.Services;
using XeniaManager.ViewModels;

namespace XeniaManager.Views;

public partial class MainWindow : AppWindow
{
    // Properties
    private MainWindowViewModel _viewModel { get; set; }

    // Constructor
    public MainWindow()
    {
        InitializeComponent();
        _viewModel = App.Services.GetRequiredService<MainWindowViewModel>();
        DataContext = _viewModel;

        // Subscribe to EventManager for window state changes
        EventManager.Instance.WindowDisabled += OnWindowDisabled;
    }

    private void OnWindowDisabled(bool isDisabled)
    {
        _viewModel.DisableWindow = isDisabled;
    }
}