using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using XeniaManager.Core.Settings;
using XeniaManager.Core.Utilities;

namespace XeniaManager.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    [ObservableProperty] private string windowTitle;
    [ObservableProperty] private bool disableWindow;

    public MainWindowViewModel()
    {
        // Initialize DisableWindow
        DisableWindow = false;
        
        // Load version into the Window Title
        Settings settings = App.Services.GetRequiredService<Settings>();
        WindowTitle = string.Format(LocalizationHelper.GetText("MainWindow.Title"), settings.GetVersion());
    }
}