using System.Windows.Controls;

// Imported
using XeniaManager.Desktop.ViewModel.Pages;

namespace XeniaManager.Desktop.Views.Pages;

/// <summary>
/// Interaction logic for SettingsPage.xaml
/// </summary>
public partial class SettingsPage : Page
{
    // Variables 
    private SettingsPageViewModel _viewModel { get; set; }
    // Constructor
    public SettingsPage()
    {
        InitializeComponent();
        _viewModel = new SettingsPageViewModel();
        DataContext = _viewModel;
    }
}