using Avalonia.Media;

namespace XeniaManager.BigScreen.ViewModels;

/// <summary>
/// Base for the full-screen overlay screens (Library, Media, Settings).
/// </summary>
public abstract class ScreenViewModel : ViewModelBase
{
    private readonly SettingsViewModel _settings;

    protected ScreenViewModel(SettingsViewModel settings)
    {
        _settings = settings;
        _settings.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(SettingsViewModel.PrimaryColor))
            {
                OnPropertyChanged(nameof(ScreenBackground));
            }
        };
    }

    /// <summary>
    /// Brush used as the overlay/menu background, derived from the primary colour
    /// so menus match the dashboard instead of being pitch black.
    /// </summary>
    public IBrush ScreenBackground => new SolidColorBrush(_settings.PrimaryColor);
}
