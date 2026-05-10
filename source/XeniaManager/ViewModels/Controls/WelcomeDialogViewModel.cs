using System.Collections.ObjectModel;
using Microsoft.Extensions.DependencyInjection;
using CommunityToolkit.Mvvm.ComponentModel;
using XeniaManager.Core.Logging;
using XeniaManager.Core.Models;
using XeniaManager.Core.Models.Items;
using XeniaManager.Core.Settings;
using XeniaManager.Core.Utilities;
using XeniaManager.Services;

namespace XeniaManager.ViewModels.Controls;

/// <summary>
/// ViewModel for the welcome dialog shown on the first startup.
/// Handles theme selection and applies the chosen theme.
/// </summary>
public partial class WelcomeDialogViewModel : ViewModelBase
{
    /// <summary>
    /// The theme service used to apply theme changes.
    /// </summary>
    private readonly ThemeService _themeService;

    /// <summary>
    /// The settings used to load the saved theme preference.
    /// </summary>
    private readonly Settings _settings;

    /// <summary>
    /// The list of available themes for selection.
    /// </summary>
    public ObservableCollection<ThemeDisplayItem> ThemeOptions { get; set; } =
    [
        new ThemeDisplayItem
        {
            DisplayName = LocalizationHelper.GetText("SettingsPage.Ui.Theme.Option.Light"),
            ThemeValue = Theme.Light
        },
        new ThemeDisplayItem
        {
            DisplayName = LocalizationHelper.GetText("SettingsPage.Ui.Theme.Option.Dark"),
            ThemeValue = Theme.Dark
        }
    ];

    /// <summary>
    /// The currently selected theme.
    /// </summary>
    [ObservableProperty] private Theme _selectedTheme;

    /// <summary>
    /// The selected index for the theme combobox.
    /// </summary>
    [ObservableProperty] private int _selectedThemeIndex;

    partial void OnSelectedThemeIndexChanged(int oldValue, int newValue)
    {
        if (newValue < 0 || newValue >= ThemeOptions.Count || newValue == oldValue)
        {
            return;
        }

        SelectedTheme = ThemeOptions[newValue].ThemeValue;
        _themeService.SetTheme(SelectedTheme);
        Logger.Info<WelcomeDialogViewModel>($"Theme preview changed to {SelectedTheme}");
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="WelcomeDialogViewModel"/> class.
    /// </summary>
    public WelcomeDialogViewModel()
    {
        _themeService = App.Services.GetRequiredService<ThemeService>();
        _settings = App.Services.GetRequiredService<Settings>();

        SelectedTheme = _settings.Settings.Ui.Theme;
        for (int i = 0; i < ThemeOptions.Count; i++)
        {
            if (ThemeOptions[i].ThemeValue == SelectedTheme)
            {
                SelectedThemeIndex = i;
                break;
            }
        }
    }
}