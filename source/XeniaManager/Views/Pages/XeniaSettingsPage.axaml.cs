using Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;
using XeniaManager.ViewModels.Pages;

namespace XeniaManager.Views.Pages;

public partial class XeniaSettingsPage : UserControl
{
    // Variables
    private XeniaSettingsPageViewModel _viewModel { get; set; }
    
    // Constructor
    public XeniaSettingsPage()
    {
        InitializeComponent();
        _viewModel = App.Services.GetRequiredService<XeniaSettingsPageViewModel>();
        DataContext = _viewModel;
    }
}