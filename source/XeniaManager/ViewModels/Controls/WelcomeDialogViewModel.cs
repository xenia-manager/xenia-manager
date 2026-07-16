using Avalonia;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.Linq;
using XeniaManager.Core.Logging;
using XeniaManager.Core.Models;
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
    /// The settings used to load the saved theme preference.
    /// </summary>
    private readonly Settings _settings;

    /// <summary>
    /// The list of available themes for selection.
    /// </summary>
    public IReadOnlyList<Theme> AppThemeOptions { get; } = [Theme.System, Theme.Light, Theme.Dark];

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
        if (newValue < 0 || newValue >= AppThemeOptions.Count || newValue == oldValue)
        {
            return;
        }

        SelectedTheme = AppThemeOptions[newValue];
        ThemeResourceLoader.Instance.ApplyTheme(SelectedTheme);
        Logger.Info<WelcomeDialogViewModel>($"Theme preview changed to {SelectedTheme}");
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="WelcomeDialogViewModel"/> class.
    /// </summary>
    public WelcomeDialogViewModel()
    {
        _settings = App.Services.GetRequiredService<Settings>();

        SelectedTheme = _settings!.Settings.Ui.Theme;
        for (int i = 0; i < AppThemeOptions.Count; i++)
        {
            if (AppThemeOptions[i] == SelectedTheme)
            {
                SelectedThemeIndex = i;
                break;
            }
        }
    }
}
