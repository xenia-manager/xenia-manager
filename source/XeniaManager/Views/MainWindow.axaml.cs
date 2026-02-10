using FluentAvalonia.UI.Windowing;
using Microsoft.Extensions.DependencyInjection;
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
    }
}