using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using XeniaManager.ViewModels.Pages;

namespace XeniaManager.Views.Pages;

public partial class ManagePage : UserControl
{
    // Variables
    private ManagePageViewModel _viewModel { get; set; }
    
    // Constructor
    public ManagePage()
    {
        InitializeComponent();
        _viewModel = App.Services.GetRequiredService<ManagePageViewModel>();
        DataContext = _viewModel;
    }
}