using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using FluentAvalonia.Styling;
using XeniaManager.Core.Logging;
using XeniaManager.Core.Models;
using XeniaManager.Core.Models.Items;
using XeniaManager.Core.Utilities;

namespace XeniaManager.Services;

/// <summary>
/// Service responsible for managing and applying themes to the application.
///
/// Custom theme resource overrides (AMOLED dark / Xbox-green light) are loaded into
/// FluentAvalonia 3's MergedDictionaries. The <see cref="Theme.System"/> option clears the
/// override and follows FluentAvalonia's built-in palette.
///
/// The resource dictionary is swapped BEFORE the theme variant is changed so that when
/// Avalonia fires ActualThemeVariantChanged (which makes FluentAvalonia controls re-query
/// their ThemeResource bindings), the correct override dictionary is already present.
/// </summary>
public class ThemeService
{
    private Theme _currentTheme = Theme.Light;
    private readonly Dictionary<Theme, ResourceDictionary?> _themeResources = new Dictionary<Theme, ResourceDictionary?>();
    private ReadOnlyObservableCollection<ThemeDisplayItem>? _themeDisplayItems;

    /// <summary>
    /// The list of available themes for selection, exposed to UI dropdowns.
    /// </summary>
    public ReadOnlyObservableCollection<ThemeDisplayItem> ThemeDisplayItems => _themeDisplayItems ??= CreateThemeDisplayItems();

    /// <summary>
    /// Initializes the ThemeService and applies the provided theme.
    /// </summary>
    public ThemeService()
    {
        if (Application.Current == null)
        {
            return;
        }

        foreach (IStyle style in Application.Current.Styles)
        {
            if (style is not FluentAvaloniaTheme faTheme)
            {
                continue;
            }

            _faTheme = faTheme;
            break;
        }
    }

    private FluentAvaloniaTheme? _faTheme;

    /// <summary>
    /// Create the observable collection accessed by dropdown menus for theme selection.
    /// </summary>
    private ReadOnlyObservableCollection<ThemeDisplayItem> CreateThemeDisplayItems()
    {
        ObservableCollection<ThemeDisplayItem> items = new ObservableCollection<ThemeDisplayItem>();
        List<Theme> themes = new List<Theme>((Theme[])Enum.GetValues(typeof(Theme)));
        themes.Sort();

        foreach (Theme theme in themes)
        {
            items.Add(new ThemeDisplayItem
            {
                DisplayName = LocalizationHelper.GetText($"SettingsPage.Ui.Theme.Option.{theme}"),
                ThemeValue = theme
            });
        }

        _themeDisplayItems = new ReadOnlyObservableCollection<ThemeDisplayItem>(items);
        return _themeDisplayItems;
    }

    /// <summary>
    /// Sets the current application theme.
    /// </summary>
    /// <param name="theme">The theme to apply</param>
    public void SetTheme(Theme theme)
    {
        Logger.Info<ThemeService>($"Switching to {theme} theme");
        ApplyTheme(theme);
        _currentTheme = theme;
    }

    /// <summary>
    /// Applies the full theme: swaps the custom resource dictionary and updates the
    /// FluentAvalonia theme variant / system-theme preference.
    /// </summary>
    private void ApplyTheme(Theme theme)
    {
        FluentAvaloniaTheme? faTheme = _faTheme
            ?? Application.Current?.Styles.OfType<FluentAvaloniaTheme>().FirstOrDefault();

        if (faTheme == null)
        {
            Logger.Error<ThemeService>("FluentAvaloniaTheme not found, cannot apply theme");
            return;
        }

        // 1. Swap the resource dictionary FIRST so any subsequent re-query sees the new colors.
        SwapDictionary(faTheme, theme);

        // 2. Update the variant / system preference. Changing RequestedThemeVariant fires
        //    ActualThemeVariantChanged, which re-resolves FluentAvalonia's ThemeResource bindings.
        if (theme == Theme.System)
        {
            faTheme.PreferSystemTheme = true;
        }
        else
        {
            faTheme.PreferSystemTheme = false;
            Application.Current!.RequestedThemeVariant = theme == Theme.Light ? ThemeVariant.Light : ThemeVariant.Dark;
        }
    }

    /// <summary>
    /// Swaps the custom override dictionary for the given theme.
    /// </summary>
    private void SwapDictionary(FluentAvaloniaTheme faTheme, Theme theme)
    {
        // Remove any previously loaded override
        if (_themeResources.TryGetValue(_currentTheme, out ResourceDictionary? current) && current != null)
        {
            faTheme.MergedDictionaries.Remove(current);
            _themeResources[_currentTheme] = null;
        }

        // System theme has no custom override (uses FluentAvalonia defaults)
        if (theme == Theme.System)
        {
            return;
        }

        string resourcePath = theme == Theme.Dark
            ? "avares://XeniaManager/Resources/Themes/Dark.axaml"
            : "avares://XeniaManager/Resources/Themes/Light.axaml";

        try
        {
            ResourceDictionary dictionary = (ResourceDictionary)AvaloniaXamlLoader.Load(new Uri(resourcePath));
            faTheme.MergedDictionaries.Add(dictionary);
            _themeResources[theme] = dictionary;
            Logger.Info<ThemeService>($"Loaded theme resources for {theme}");
        }
        catch (Exception ex)
        {
            Logger.Error<ThemeService>($"Failed to load theme resources for {theme}: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets the currently applied theme.
    /// </summary>
    /// <returns>The current theme</returns>
    public Theme GetCurrentTheme() => _currentTheme;

    /// <summary>
    /// Gets a collection of all available themes.
    /// </summary>
    /// <returns>An enumerable collection of available themes</returns>
    public IEnumerable<Theme> GetAvailableThemes()
    {
        return (Theme[])Enum.GetValues(typeof(Theme));
    }
}
