using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using FluentAvalonia.UI.Controls;
using Microsoft.Extensions.DependencyInjection;
using XeniaManager.Core.Settings;
using XeniaManager.Core.Utilities;
using XeniaManager.Views;

namespace XeniaManager.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    public Settings Settings { get; set; }

    // MainWindow
    [ObservableProperty] private string windowTitle;
    [ObservableProperty] private bool disableWindow;

    // InfoBar
    [ObservableProperty] private bool infoBarVisible = false;
    [ObservableProperty] private string infoBarMessage = string.Empty;
    [ObservableProperty] private InfoBarSeverity infoBarSeverity = InfoBarSeverity.Informational;

    public MainWindowViewModel()
    {
        // Initialize DisableWindow
        DisableWindow = false;

        // Load version into the Window Title
        Settings = App.Services.GetRequiredService<Settings>();
        WindowTitle = string.Format(LocalizationHelper.GetText("MainWindow.Title"), Settings.GetVersion());
    }

    /// <summary>
    /// Shows the InfoBar with a message and severity for a specified duration with slide animation.
    /// </summary>
    /// <param name="message">The message to display.</param>
    /// <param name="severity">The severity level of the message.</param>
    /// <param name="durationSeconds">How long to display the InfoBar in seconds.</param>
    public async void ShowInfoBar(string message, InfoBarSeverity severity, double durationSeconds = 5)
    {
        if (App.MainWindow is not MainWindow mainWindow)
        {
            return;
        }

        InfoBarMessage = message;
        InfoBarSeverity = severity;
        InfoBarVisible = true;

        // Animate in
        await mainWindow.SlideInInfoBar();

        await Task.Delay(TimeSpan.FromSeconds(durationSeconds));

        // Animate out
        await mainWindow.SlideOutInfoBar();

        InfoBarVisible = false;
    }
}