using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Styling;
using FluentAvalonia.Styling;
using XeniaManager.Core.Constants;
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

    private readonly Dictionary<Theme, ThemeConfiguration> _themeConfigs = new Dictionary<Theme, ThemeConfiguration>();
    private readonly Dictionary<string, Theme> _customThemeFiles = new Dictionary<string, Theme>(StringComparer.OrdinalIgnoreCase);

    private class ThemeConfiguration
    {
        public ThemeVariant? BaseTheme { get; set; }
        public string? ResourcePath { get; set; }
        public Theme? FallbackTheme { get; set; }
        public string? DisplayName { get; set; }
        public bool IsCustom { get; set; }
    }

    private class CustomThemeMetadata
    {
        public string? BaseTheme { get; set; }
        public string? DisplayName { get; set; }
    }

    /// <summary>
    /// Initializes the ThemeService and configures all available themes
    /// </summary>
    public ThemeService()
    {
        InitializeDefaultThemes();
        DiscoverCustomThemes();

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

    private void InitializeDefaultThemes()
    {
        _themeConfigs[Theme.Light] = new ThemeConfiguration
        {
            BaseTheme = ThemeVariant.Light,
            ResourcePath = "avares://XeniaManager/Resources/Themes/Light.axaml",
            FallbackTheme = null,
            DisplayName = "Light",
            IsCustom = false
        };

        _themeConfigs[Theme.Dark] = new ThemeConfiguration
        {
            BaseTheme = ThemeVariant.Dark,
            ResourcePath = "avares://XeniaManager/Resources/Themes/Dark.axaml",
            FallbackTheme = null,
            DisplayName = "Dark",
            IsCustom = false
        };
    }

    private void DiscoverCustomThemes()
    {
        try
        {
            string themesDir = AppPaths.ThemesDirectory;
            if (!Directory.Exists(themesDir))
            {
                Logger.Debug<ThemeService>($"Themes directory does not exist: {themesDir}");
                return;
            }

            List<string> axamlFiles = Directory.GetFiles(themesDir, "*.axaml")
                .Concat(Directory.GetFiles(themesDir, "*.axamlc"))
                .ToList();

            Logger.Info<ThemeService>($"Discovered {axamlFiles.Count} custom theme files");

            foreach (string file in axamlFiles)
            {
                string fileName = Path.GetFileName(file);
                if (_customThemeFiles.ContainsKey(fileName))
                {
                    Logger.Warning<ThemeService>($"Duplicate theme file found: {fileName}");
                    continue;
                }

                _themeConfigs[Theme.Custom] = new ThemeConfiguration
                {
                    BaseTheme = null,
                    ResourcePath = null,
                    FallbackTheme = Theme.Dark,
                    DisplayName = fileName,
                    IsCustom = true
                };

                _customThemeFiles[fileName] = Theme.Custom;
                Logger.Info<ThemeService>($"Registered custom theme: {fileName}");
            }
        }
        catch (Exception ex)
        {
            Logger.Error<ThemeService>($"Failed to discover custom themes: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets the list of available custom theme file names
    /// </summary>
    public IEnumerable<string> GetCustomThemeFiles()
    {
        return _customThemeFiles.Keys.ToList();
    }

    /// <summary>
    /// Sets the current application theme
    /// </summary>
    /// <param name="theme">The theme to apply</param>
    /// <param name="customThemePath">Optional path to custom theme file (used when theme is Custom)</param>
    public void SetTheme(Theme theme, string? customThemePath = null)
    {
        if (theme == Theme.Custom && !string.IsNullOrEmpty(customThemePath))
        {
            ApplyCustomTheme(customThemePath);
            _currentTheme = theme;
            return;
        }

        if (!_themeConfigs.ContainsKey(theme))
        {
            Logger.Error<ThemeService>($"Theme {theme} is not configured");
            return;
        }

        Logger.Info<ThemeService>($"Switching to {theme} theme");

        RemoveCurrentThemeResources();
        ApplyTheme(theme);

        _currentTheme = theme;
    }

    private void ApplyCustomTheme(string customThemePath)
    {
        Logger.Info<ThemeService>($"Loading custom theme from: {customThemePath}");

        RemoveCurrentThemeResources();

        string fileName = Path.GetFileName(customThemePath);
        string themesDir = AppPaths.ThemesDirectory;
        string fullPath;

        fullPath = Path.IsPathRooted(customThemePath) ? customThemePath : Path.Combine(themesDir, customThemePath);

        if (!File.Exists(fullPath))
        {
            Logger.Error<ThemeService>($"Custom theme file not found: {fullPath}");
            if (_themeConfigs.TryGetValue(Theme.Dark, out ThemeConfiguration? fallback))
            {
                Logger.Warning<ThemeService>("Falling back to Dark theme");
                ApplyTheme(Theme.Dark);
            }
            return;
        }

        try
        {
            string xamlContent = File.ReadAllText(fullPath);
            object loadedResources = AvaloniaRuntimeXamlLoader.Parse(xamlContent, typeof(ThemeService).Assembly);
            if (loadedResources is IResourceProvider resourceProvider)
            {
                _themeResources[Theme.Custom] = null;
                Application.Current?.Resources.MergedDictionaries.Add(resourceProvider);
                Logger.Info<ThemeService>($"Custom theme resources loaded from {fileName}");
            }
            else
            {
                Logger.Error<ThemeService>($"Loaded resources from {fileName} is not IResourceProvider");
            }
        }
        catch (Exception loadEx)
        {
            Logger.Error<ThemeService>($"Failed to parse custom theme XAML: {loadEx.Message}");
            if (_themeConfigs.TryGetValue(Theme.Dark, out ThemeConfiguration? fallback))
            {
                Logger.Warning<ThemeService>("Falling back to Dark theme");
                ApplyTheme(Theme.Dark);
            }
            return;
        }

        ThemeVariant baseVariant = ThemeVariant.Dark;
        string baseThemeName = Path.GetFileNameWithoutExtension(fullPath).ToLowerInvariant();
        if (baseThemeName.Contains("light"))
        {
            baseVariant = ThemeVariant.Light;
        }
        else if (baseThemeName.Contains("dark"))
        {
            baseVariant = ThemeVariant.Dark;
        }
        else
        {
            string jsonPath = Path.ChangeExtension(fullPath, ".json");
            if (File.Exists(jsonPath))
            {
                try
                {
                    string jsonContent = File.ReadAllText(jsonPath);
                    CustomThemeMetadata? metadata = System.Text.Json.JsonSerializer.Deserialize<CustomThemeMetadata>(jsonContent);
                    if (metadata != null && !string.IsNullOrEmpty(metadata.BaseTheme))
                    {
                        if (metadata.BaseTheme.Equals("Light", StringComparison.OrdinalIgnoreCase))
                        {
                            baseVariant = ThemeVariant.Light;
                        }
                        else if (metadata.BaseTheme.Equals("Dark", StringComparison.OrdinalIgnoreCase))
                        {
                            baseVariant = ThemeVariant.Dark;
                        }
                    }
                }
                catch
                {
                    Logger.Warning<ThemeService>($"Failed to read theme metadata from {jsonPath}, using default Dark theme");
                }
            }
            else
            {
                Logger.Warning<ThemeService>($"Custom theme '{fileName}' does not specify base theme in filename or metadata. Using default Dark theme.");
                Logger.Info<ThemeService>($"To specify base theme, add 'Dark' or 'Light' to the filename, or create a {Path.GetFileNameWithoutExtension(fullPath)}.json sidecar file with '{{\"BaseTheme\": \"Dark\"}}'");
            }
        }

        if (Application.Current != null)
        {
            Application.Current.RequestedThemeVariant = baseVariant;
        }

        Logger.Info<ThemeService>($"Custom theme loaded successfully: {fileName} (base: {baseVariant})");
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