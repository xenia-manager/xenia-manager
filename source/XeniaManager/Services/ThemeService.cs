using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Styling;
using FluentAvalonia.Styling;
using XeniaManager.Core.Logging;
using XeniaManager.Core.Models;

namespace XeniaManager.Services;

/// <summary>
/// Service responsible for managing and applying themes to the application
/// </summary>
public class ThemeService
{
    private Theme _currentTheme = Theme.Dark;
    private FluentAvaloniaTheme? _faTheme;
    private readonly Dictionary<Theme, ResourceInclude?> _themeResources = new Dictionary<Theme, ResourceInclude?>();

    private readonly Dictionary<Theme, ThemeConfiguration> _themeConfigs = new Dictionary<Theme, ThemeConfiguration>
    {
        [Theme.Light] = new ThemeConfiguration
        {
            BaseTheme = ThemeVariant.Light,
            ResourcePath = "avares://XeniaManager/Resources/Themes/Light.axaml",
            FallbackTheme = null
        },
        [Theme.Dark] = new ThemeConfiguration
        {
            BaseTheme = ThemeVariant.Dark,
            ResourcePath = "avares://XeniaManager/Resources/Themes/Dark.axaml",
            FallbackTheme = null
        }
        // New themes need to be added here
    };

    private class ThemeConfiguration
    {
        public ThemeVariant? BaseTheme { get; set; }
        public string? ResourcePath { get; set; }
        public Theme? FallbackTheme { get; set; }
    }

    /// <summary>
    /// Initializes the ThemeService and configures all available themes
    /// </summary>
    public ThemeService()
    {
        // Find FluentAvalonia Theme in Application Styles
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

    /// <summary>
    /// Sets the current application theme
    /// </summary>
    /// <param name="theme">The theme to apply</param>
    public void SetTheme(Theme theme)
    {
        if (!_themeConfigs.ContainsKey(theme))
        {
            Logger.Error<ThemeService>($"Theme {theme} is not configured");
            return;
        }

        Logger.Info<ThemeService>($"Switching to {theme} theme");

        // Remove the previously loaded theme and apply the new theme
        RemoveCurrentThemeResources();
        ApplyTheme(theme);

        _currentTheme = theme;
    }

    /// <summary>
    /// Applies the specified theme by setting the base theme variant and loading theme resources
    /// </summary>
    /// <param name="theme">The theme to apply</param>
    private void ApplyTheme(Theme theme)
    {
        if (!_themeConfigs.TryGetValue(theme, out ThemeConfiguration? config))
        {
            Logger.Error<ThemeService>($"No configuration found for theme {theme}");
            return;
        }

        if (config.BaseTheme != null && Application.Current != null)
        {
            Application.Current.RequestedThemeVariant = config.BaseTheme;
        }

        if (!string.IsNullOrEmpty(config.ResourcePath))
        {
            LoadThemeResources(theme, config.ResourcePath, config.FallbackTheme);
        }
    }

    /// <summary>
    /// Loads the theme resources from the specified resource path
    /// </summary>
    /// <param name="theme">The theme to load resources for</param>
    /// <param name="resourcePath">The path to the theme resource file</param>
    /// <param name="fallbackTheme">The fallback theme to use if loading fails</param>
    private void LoadThemeResources(Theme theme, string resourcePath, Theme? fallbackTheme)
    {
        if (Application.Current == null)
        {
            return;
        }

        try
        {
            Uri uri = new Uri(resourcePath);
            ResourceInclude resourceInclude = new ResourceInclude(uri)
            {
                Source = uri
            };

            _themeResources[theme] = resourceInclude;
            Application.Current.Resources.MergedDictionaries.Add(resourceInclude);
            Logger.Info<ThemeService>($"Theme resources loaded for {theme}");
        }
        catch (Exception ex)
        {
            Logger.Error<ThemeService>($"Failed to load theme resources for {theme}: {ex.Message}");
            if (fallbackTheme != null)
            {
                Logger.Warning<ThemeService>($"Falling back to {fallbackTheme.Value} theme");
                ApplyTheme(fallbackTheme.Value);
            }
        }
    }

    /// <summary>
    /// Removes all currently loaded theme resources from the application
    /// </summary>
    private void RemoveCurrentThemeResources()
    {
        if (Application.Current == null)
        {
            return;
        }

        foreach (KeyValuePair<Theme, ResourceInclude?> kvp in _themeResources)
        {
            if (kvp.Value == null)
            {
                continue;
            }
            Application.Current.Resources.MergedDictionaries.Remove(kvp.Value);
            Logger.Debug<ThemeService>($"Removed theme resources for {kvp.Key}");
        }

        _themeResources.Clear();
    }

    /// <summary>
    /// Gets the currently applied theme
    /// </summary>
    /// <returns>The current theme</returns>
    public Theme GetCurrentTheme() => _currentTheme;

    /// <summary>
    /// Gets a collection of all available themes
    /// </summary>
    /// <returns>An enumerable collection of available themes</returns>
    public IEnumerable<Theme> GetAvailableThemes()
    {
        return _themeConfigs.Keys;
    }
}