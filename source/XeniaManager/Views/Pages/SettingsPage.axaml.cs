using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using XeniaManager.ViewModels.Pages;

namespace XeniaManager.Views.Pages;

public partial class SettingsPage : UserControl
{
    // Variables
    private SettingsPageViewModel _viewModel { get; set; }

    // Constructor
    public SettingsPage()
    {
        InitializeComponent();
        _viewModel = App.Services.GetRequiredService<SettingsPageViewModel>();
        DataContext = _viewModel;
    }
    
    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        // Refresh settings to ensure the UI reflects current values
        _viewModel.RefreshSettings();
    }
}