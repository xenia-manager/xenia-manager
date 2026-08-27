using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using FluentAvalonia.Styling;
using XeniaManager.Logging;
using XeniaManager.Core.Models;
using XeniaManager.Core.Models.Items;
using XeniaManager.Core.Utilities;

namespace XeniaManager.Services;

/// <summary>
/// Applies and caches application themes on top of FluentAvalonia 3 (Avalonia 12).
/// </summary>
/// <remarks>
/// <para>
/// FluentAvalonia only ships Light/Dark palettes out of the box, so Dark, AMOLED and Steam
/// are implemented here as custom <see cref="ResourceDictionary"/> overrides merged into
/// <c>Application.Current.Resources</c>. <see cref="Theme.System"/> removes the override
/// entirely and falls back to FluentAvalonia's own palette via
/// <see cref="FluentAvaloniaTheme.PreferSystemTheme"/>.
/// </para>
/// <para>
/// Order of operations matters: the resource dictionary is swapped in <em>before</em>
/// <c>RequestedThemeVariant</c> is changed. Avalonia only re-queries <c>ThemeResource</c>
/// bindings on FluentAvalonia controls once <c>ActualThemeVariantChanged</c> fires, so the
/// override dictionary needs to already be present when that happens.
/// </para>
/// <para>
/// Parsed dictionaries are cached per-theme (<see cref="_loadedDictionaries"/>) so switching
/// back to a previously used theme skips XAML parsing. Only one dictionary is merged in at a
/// time (<see cref="_activeDictionary"/>), and it's added/removed as a single unit via
/// <c>MergedDictionaries</c> rather than key-by-key, so a theme switch triggers one
/// resource-changed pass instead of one per resource key.
/// </para>
/// </remarks>
public class ThemeService
{
    private Theme _currentTheme = Theme.Light;

    /// <summary>
    /// Cache of parsed theme resource dictionaries, keyed by theme. Populated lazily on first
    /// use so repeat switches to the same theme don't reparse XAML.
    /// </summary>
    private readonly Dictionary<Theme, ResourceDictionary> _loadedDictionaries = new Dictionary<Theme, ResourceDictionary>();

    /// <summary>
    /// The theme dictionary currently merged into <c>Application.Current.Resources</c>, or
    /// <see langword="null"/> when the current theme is <see cref="Theme.System"/>.
    /// </summary>
    private ResourceDictionary? _activeDictionary;

    private ReadOnlyObservableCollection<ThemeDisplayItem>? _themeDisplayItems;

    private FluentAvaloniaTheme? _faTheme;

    /// <summary>
    /// Available themes for display in UI dropdowns, sorted and localized.
    /// </summary>
    public ReadOnlyObservableCollection<ThemeDisplayItem> ThemeDisplayItems => _themeDisplayItems ??= CreateThemeDisplayItems();

    /// <summary>
    /// Locates and caches the app's <see cref="FluentAvaloniaTheme"/> style instance.
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

    /// <summary>
    /// Builds the localized, sorted list of themes exposed via <see cref="ThemeDisplayItems"/>.
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
    /// Applies <paramref name="theme"/> and remembers it as the current theme.
    /// </summary>
    /// <param name="theme">The theme to switch to.</param>
    public void SetTheme(Theme theme)
    {
        Logger.Info<ThemeService>($"Switching to {theme} theme");
        ApplyTheme(theme);
        _currentTheme = theme;
    }

    /// <summary>
    /// Swaps in the resource dictionary for <paramref name="theme"/>, then updates
    /// FluentAvalonia's theme variant / system-theme preference to match.
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

        // Resources first: ActualThemeVariantChanged (fired below) is what makes FluentAvalonia
        // controls re-query their ThemeResource bindings, so the override must already be merged
        // in by the time that happens.
        SwapDictionary(theme);

        if (theme == Theme.System)
        {
            faTheme.PreferSystemTheme = true;
        }
        else
        {
            faTheme.PreferSystemTheme = false;
            ThemeVariant targetVariant = theme == Theme.Light ? ThemeVariant.Light : ThemeVariant.Dark;

            // Avalonia only raises ActualThemeVariantChanged when RequestedThemeVariant actually
            // changes value. Dark, AMOLED and Steam all map to ThemeVariant.Dark, so switching
            // between them needs a toggle-away-and-back to force the event. A genuine variant
            // change (e.g. Light -> Dark) doesn't need the toggle, so it's skipped in that case.
            if (Application.Current!.RequestedThemeVariant == targetVariant)
            {
                Application.Current.RequestedThemeVariant = targetVariant == ThemeVariant.Light
                    ? ThemeVariant.Dark
                    : ThemeVariant.Light;
            }
            Application.Current.RequestedThemeVariant = targetVariant;
        }

        Logger.Debug<ThemeService>($"ApplyTheme complete, variant={Application.Current?.ActualThemeVariant}");
    }

    /// <summary>
    /// Detaches the active theme dictionary (if any) and merges in the dictionary for
    /// <paramref name="theme"/>, loading it from XAML and caching it on first use.
    /// </summary>
    /// <remarks>
    /// Detach and attach are each a single <c>MergedDictionaries</c> operation, so a theme
    /// switch fires one resource-changed pass total rather than one per resource key.
    /// </remarks>
    private void SwapDictionary(Theme theme)
    {
        if (_activeDictionary != null)
        {
            Application.Current!.Resources.MergedDictionaries.Remove(_activeDictionary);
            Logger.Debug<ThemeService>("Detached previously active theme dictionary");
            _activeDictionary = null;
        }

        if (theme == Theme.System)
        {
            return;
        }

        if (!_loadedDictionaries.TryGetValue(theme, out ResourceDictionary? dictionary))
        {
            string resourcePath = theme switch
            {
                Theme.Amoled => "avares://XeniaManager/Resources/Themes/Amoled.axaml",
                Theme.Dark => "avares://XeniaManager/Resources/Themes/Dark.axaml",
                Theme.Steam =>  "avares://XeniaManager/Resources/Themes/Steam.axaml",
                _ => "avares://XeniaManager/Resources/Themes/Light.axaml"
            };

            try
            {
                dictionary = (ResourceDictionary)AvaloniaXamlLoader.Load(new Uri(resourcePath));
                _loadedDictionaries[theme] = dictionary;
                Logger.Debug<ThemeService>($"Loaded and cached theme resources for {theme}");
            }
            catch (Exception ex)
            {
                Logger.Error<ThemeService>($"Failed to load theme resources for {theme}: {ex.Message}");
                return;
            }
        }
        else
        {
            Logger.Debug<ThemeService>($"Reusing cached theme resources for {theme}");
        }

        Application.Current!.Resources.MergedDictionaries.Add(dictionary);
        _activeDictionary = dictionary;
        Logger.Info<ThemeService>($"Applied theme resources for {theme}");
    }

    /// <summary>
    /// The currently applied theme.
    /// </summary>
    public Theme GetCurrentTheme() => _currentTheme;

    /// <summary>
    /// All themes defined on the <see cref="Theme"/> enum.
    /// </summary>
    public IEnumerable<Theme> GetAvailableThemes()
    {
        return (Theme[])Enum.GetValues(typeof(Theme));
    }
}