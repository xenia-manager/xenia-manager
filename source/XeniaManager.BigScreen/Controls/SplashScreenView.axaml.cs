using System;
using System.IO;
using System.Text.Json;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Threading;
using XeniaManager.BigScreen.Constants;
using XeniaManager.BigScreen.Factories;
using XeniaManager.Core.Logging;

namespace XeniaManager.BigScreen.Controls;

/// <summary>
/// Splash screen content: logo, live boot status text and a progress bar
/// over the dashboard's radial background. Hosted in FluentAvalonia's
/// built-in splash window, which is forced fullscreen on attach.
/// </summary>
public partial class SplashScreenView : UserControl
{
    public SplashScreenView()
    {
        InitializeComponent();
        Background = BackgroundBrushFactory.CreateRadial(
            LoadSavedColor("primary_color", Color.FromRgb(0x1C, 0x1F, 0x25)));

        // Use the saved BigScreen accent (not the theme default) so the splash
        // matches the dashboard once the settings load
        Color accent = LoadAccentColor();
        LogoIcon.Foreground = new SolidColorBrush(accent);
        LoadBar.Foreground = new SolidColorBrush(accent);
    }

    /// <summary>
    /// FluentAvalonia shows the splash in a centered window by default;
    /// force it fullscreen (and borderless) so it covers the screen.
    /// </summary>
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        if (TopLevel.GetTopLevel(this) is Window window)
        {
            window.WindowState = WindowState.FullScreen;
            window.WindowDecorations = WindowDecorations.None;
        }
    }

    /// <summary>
    /// Updates the status text and progress bar (0-1). Safe from any thread.
    /// </summary>
    public void SetProgress(string status, double progress)
    {
        if (Dispatcher.UIThread.CheckAccess())
        {
            StatusText.Text = status;
            LoadBar.Value = progress * 100;
        }
        else
        {
            Dispatcher.UIThread.Post(() => SetProgress(status, progress));
        }
    }

    /// <summary>
    /// The saved BigScreen accent color, falling back to the theme default.
    /// </summary>
    private static Color LoadAccentColor() => LoadSavedColor("accent_color", Color.FromRgb(0x10, 0x7C, 0x41));

    /// <summary>
    /// Reads a saved color from the dashboard settings file, falling back to
    /// <paramref name="fallback"/> when unavailable.
    /// </summary>
    private static Color LoadSavedColor(string propertyName, Color fallback)
    {
        try
        {
            string path = Path.Combine(AppContext.BaseDirectory, AppConstants.SettingsFileName);
            if (File.Exists(path))
            {
                using JsonDocument document = JsonDocument.Parse(File.ReadAllText(path));
                if (document.RootElement.TryGetProperty(propertyName, out JsonElement element)
                    && element.ValueKind == JsonValueKind.String
                    && Color.TryParse(element.GetString(), out Color color))
                {
                    return color;
                }
            }
        }
        catch (Exception ex)
        {
            // Unreadable settings - fall back to the default
            Logger.Warning<SplashScreenView>("Failed to read saved BigScreen colors, falling back to defaults");
            Logger.LogExceptionDetails<SplashScreenView>(ex);
        }

        return fallback;
    }
}
