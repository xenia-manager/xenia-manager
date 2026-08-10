using Avalonia.Markup.Xaml;
using FluentAvalonia.UI.Windowing;

namespace XeniaManager.BigScreen.Views;

/// <summary>
/// Fullscreen boot splash: logo, live status text and a progress bar.
/// Shown before the main window, closed when the boot pipeline completes.
/// </summary>
public partial class SplashWindow : FAAppWindow
{
    public SplashWindow()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Updates the status text and progress bar (0-1).
    /// </summary>
    public void SetProgress(string status, double progress) => ContentHost.SetProgress(status, progress);
}
