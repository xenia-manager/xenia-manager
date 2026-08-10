using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using XeniaManager.BigScreen.ViewModels;
using XeniaManager.BigScreen.Views;
using XeniaManager.Core.Utilities;

namespace XeniaManager.BigScreen;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Default-language resources so Core's PlaytimeFormatter can localize
            LocalizationHelper.Initialize("avares://XeniaManager.BigScreen/Resources/Language/");

            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainWindowViewModel(),
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}