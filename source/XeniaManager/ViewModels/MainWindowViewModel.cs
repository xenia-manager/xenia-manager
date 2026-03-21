using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using XeniaManager.Core.Settings;
using XeniaManager.Core.Utilities;

namespace XeniaManager.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    public Settings Settings { get; set; }

    // MainWindow
    [ObservableProperty] private bool disableWindow;

    public MainWindowViewModel()
    {
        // Initialize DisableWindow
        DisableWindow = false;

        // Load version into the Window Title
        Settings = App.Services.GetRequiredService<Settings>();
    }
}