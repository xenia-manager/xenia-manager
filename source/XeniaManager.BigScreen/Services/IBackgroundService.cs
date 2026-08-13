using Avalonia.Media;
using Avalonia.Media.Imaging;
using XeniaManager.BigScreen.Models;

namespace XeniaManager.BigScreen.Services;

/// <summary>
/// Manages the dashboard styling: persistence, user-facing options, and brush construction.
/// </summary>
public interface IBackgroundService
{
    /// <summary>
    /// The currently loaded dashboard settings.
    /// </summary>
    DashboardSettings Settings { get; }

    /// <summary>
    /// Loads the persisted settings, falling back to defaults, then applies the style tokens.
    /// </summary>
    void Load();

    /// <summary>
    /// Saves the current settings to disk and re-applies the style tokens.
    /// </summary>
    void Save();

    /// <summary>
    /// Pushes the user-facing style tokens into the application resources.
    /// </summary>
    void ApplyResources();

    /// <summary>
    /// Builds the brush for the current settings and the optionally selected game artwork.
    /// </summary>
    IBrush? GetBackground(Bitmap? selectedGameArt);
}