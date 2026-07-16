using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using FluentAvalonia.Styling;
using XeniaManager.Core.Logging;
using XeniaManager.Core.Models;
using Logger = XeniaManager.Core.Logging.Logger;

namespace XeniaManager.Services;

/// <summary>
/// Applies custom theme resource overrides (AMOLED dark / Xbox-green light) into
/// FluentAvalonia 3's MergedDictionaries. System theme clears the override and follows
/// FluentAvalonia's built-in palette.
///
/// The resource dictionary is swapped BEFORE the theme variant is changed so that when
/// Avalonia fires ActualThemeVariantChanged (which makes FluentAvalonia controls re-query
/// their ThemeResource bindings), the correct override dictionary is already present.
/// </summary>
public class ThemeResourceLoader
{
    public static readonly ThemeResourceLoader Instance = new();

    private const string DarkResourcePath = "avares://XeniaManager/Resources/Themes/Dark.axaml";
    private const string LightResourcePath = "avares://XeniaManager/Resources/Themes/Light.axaml";

    private ResourceDictionary? _currentDictionary;

    /// <summary>
    /// Applies the full theme: swaps the custom resource dictionary and updates the
    /// FluentAvalonia theme variant / system-theme preference.
    /// </summary>
    public void ApplyTheme(Theme theme)
    {
        FluentAvaloniaTheme? faTheme = Application.Current?.Styles.OfType<FluentAvaloniaTheme>().FirstOrDefault();
        if (faTheme == null)
        {
            Logger.Error<ThemeResourceLoader>("FluentAvaloniaTheme not found, cannot apply theme");
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

    private void SwapDictionary(FluentAvaloniaTheme faTheme, Theme theme)
    {
        // Remove any previously loaded override
        if (_currentDictionary != null)
        {
            faTheme.MergedDictionaries.Remove(_currentDictionary);
            _currentDictionary = null;
        }

        // System theme has no custom override (uses FluentAvalonia defaults)
        if (theme == Theme.System)
        {
            return;
        }

        string path = theme == Theme.Dark ? DarkResourcePath : LightResourcePath;

        try
        {
            ResourceDictionary dictionary = (ResourceDictionary)AvaloniaXamlLoader.Load(new Uri(path));
            faTheme.MergedDictionaries.Add(dictionary);
            _currentDictionary = dictionary;
            Logger.Info<ThemeResourceLoader>($"Loaded theme resources for {theme}");
        }
        catch (Exception ex)
        {
            Logger.Error<ThemeResourceLoader>($"Failed to load theme resources for {theme}: {ex.Message}");
        }
    }
}
