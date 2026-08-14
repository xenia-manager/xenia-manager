using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using XeniaManager.BigScreen.Factories;
using XeniaManager.BigScreen.Services;

namespace XeniaManager.BigScreen.ViewModels.Screens;

/// <summary>
/// Base for the full-screen overlay screens (Library, Gallery, Settings).
/// </summary>
public abstract partial class ScreenViewModel : ViewModelBase
{
    private readonly SettingsViewModel _settings;

    /// <summary>
    /// Whether this screen's hint bar is visible - hidden while any modal is
    /// open, so only the top modal's hints show.
    /// </summary>
    [ObservableProperty]
    public partial bool IsHintBarVisible { get; set; } = true;

    protected ScreenViewModel(SettingsViewModel settings, IModalService modalService)
    {
        _settings = settings;
        modalService.StackChanged += () => IsHintBarVisible = !modalService.IsOpen;
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
    public IBrush ScreenBackground => BackgroundBrushFactory.CreateSolid(_settings.PrimaryColor);
}