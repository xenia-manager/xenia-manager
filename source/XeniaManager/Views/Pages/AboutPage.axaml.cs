using Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;
using XeniaManager.ViewModels.Pages;

namespace XeniaManager.Views.Pages;

public partial class AboutPage : UserControl
{
    // Variables
    private AboutPageViewModel _viewModel { get; set; }

    // Constructor
    public AboutPage()
    {
        InitializeComponent();
        _viewModel = App.Services.GetRequiredService<AboutPageViewModel>();
        DataContext = _viewModel;
    }
}