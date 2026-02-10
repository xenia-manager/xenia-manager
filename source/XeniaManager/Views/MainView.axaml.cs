using Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;
using XeniaManager.ViewModels;

namespace XeniaManager.Views;

public partial class MainView : UserControl
{
    // Properties
    private MainViewModel _viewModel { get; set; }

    // Constructor
    public MainView()
    {
        InitializeComponent();
        _viewModel = App.Services.GetRequiredService<MainViewModel>();
        DataContext = _viewModel;
    }
}