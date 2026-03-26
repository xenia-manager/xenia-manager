using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using XeniaManager.Core.Logging;

namespace XeniaManager.Core.Utilities;

/// <summary>
/// Manages localization and internationalization. Loads languages, retrieves localized strings, and manages supported languages.
/// Untranslated entries (marked with <see cref="UntranslatedMarker"/> or left empty) are automatically
/// stripped from non-default dictionaries so that Avalonia's MergedDictionaries resolution falls back
/// to the default (English) value — for both code-behind lookups and {DynamicResource} bindings.
/// Supports both simple language codes (e.g., "en", "fr", "hr") and full culture codes with region (e.g., "zh-Hans-CN", "zh-Hant-HK", "pt-BR").
/// For full culture codes, name the translation file using the complete culture code (e.g., "zh-Hans-CN.axaml").
/// </summary>
public class LocalizationHelper
{
    private const string DefaultLanguageCode = "en";

    /// <summary>
    /// Sentinel value that translators place in resource files to indicate a string has not been translated yet.
    /// The entry is removed at load time so the default language value is used instead.
    /// </summary>
    public const string UntranslatedMarker = "#NOTTRANSLATED#";

    private static ResourceDictionary? _defaultLanguage;
    private static ResourceDictionary? _currentOverlay;
    private static string _baseUri = string.Empty;

    // Add languages using full culture codes for regional variants
    // Simple codes: new CultureInfo("en"), new CultureInfo("fr")
    // Full culture codes with region: new CultureInfo("zh-Hans-CN"), new CultureInfo("zh-Hant-HK"), new CultureInfo("pt-BR")
    private static readonly CultureInfo[] SupportedLanguages =
    [
        new CultureInfo(DefaultLanguageCode), // English
        new CultureInfo("hr"), // Hrvatski
        // Add more languages here
    ];

    /// <summary>
    /// Initializes the LocalizationHelper and loads the default language as the permanent fallback.
    /// </summary>
    /// <param name="baseUri">The base URI where language resource files are located</param>
    /// <exception cref="ArgumentException">Thrown when baseUri is null or empty</exception>
    /// <exception cref="InvalidOperationException">Thrown when Application.Current is null</exception>
    public static void Initialize(string baseUri)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(baseUri);

        if (Application.Current is null)
        {
            throw new InvalidOperationException("Cannot initialize localization before Application is created.");
        }

        _baseUri = baseUri;
        _defaultLanguage = LoadDictionary(DefaultLanguageCode);
        Application.Current.Resources.MergedDictionaries.Add(_defaultLanguage);

        CultureInfo.CurrentUICulture = new CultureInfo(DefaultLanguageCode);
    }

    /// <summary>
    /// Switches the app language. The default language always remains loaded as a fallback,
    /// so any missing or untranslated keys in the selected language resolve to English automatically.
    /// Every {DynamicResource} in the application updates immediately.
    /// </summary>
    /// <param name="langCode">The language code to switch to (e.g., "en", "de", "fr"). Defaults to "en".</param>
    public static void LoadLanguage(string langCode = DefaultLanguageCode)
    {
        if (Application.Current is null)
        {
            return;
        }

        EnsureInitialized();

        IList<IResourceProvider> merged = Application.Current.Resources.MergedDictionaries;

        if (_currentOverlay is not null)
        {
            merged.Remove(_currentOverlay);
            _currentOverlay = null;
        }

        if (!IsDefaultLanguage(langCode))
        {
            try
            {
                _currentOverlay = LoadDictionary(langCode);

                int removed = RemoveUntranslatedEntries(_currentOverlay);

                if (removed > 0)
                {
                    Logger.Warning<LocalizationHelper>($"Language '{langCode}': {removed} untranslated string(s) will fall back to '{DefaultLanguageCode}'");
                }

                merged.Add(_currentOverlay);
            }
            catch (Exception ex)
            {
                Logger.Error<LocalizationHelper>($"Failed to load language '{langCode}', falling back to '{DefaultLanguageCode}': {ex.Message}");
                langCode = DefaultLanguageCode;
            }
        }

        CultureInfo.CurrentUICulture = new CultureInfo(langCode);
    }

    /// <summary>
    /// Gets a localized string from the current language resources.
    /// Falls back to the default language automatically via Avalonia's MergedDictionaries resolution.
    /// </summary>
    /// <param name="key">The resource key to look up</param>
    /// <returns>The localized string, or the key in square brackets if not found</returns>
    public static string GetText(string key)
    {
        // Use dispatcher if called from a background thread
        if (Application.Current is not null)
        {
            if (!Dispatcher.UIThread.CheckAccess())
            {
                return Dispatcher.UIThread.Invoke(() => GetText(key));
            }

            if (Application.Current.TryGetResource(key, Application.Current.ActualThemeVariant, out object? value)
                && value is string text)
            {
                return text;
            }
        }

        return $"[{key}]";
    }

    /// <summary>
    /// Gets all supported cultures.
    /// </summary>
    public static CultureInfo[] GetSupportedLanguages() => SupportedLanguages;

    private static bool IsDefaultLanguage(string langCode) => langCode.Equals(DefaultLanguageCode, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Loads a resource dictionary for the specified language code.
    /// Supports full culture codes (e.g., "zh-Hans-CN", "zh-Hant-HK") as well as simple codes (e.g., "en", "fr").
    /// </summary>
    /// <param name="langCode">The language/culture code (e.g., "en", "zh-Hans-CN")</param>
    /// <returns>The loaded ResourceDictionary</returns>
    private static ResourceDictionary LoadDictionary(string langCode)
    {
        // For full culture codes like "zh-Hans-CN", the file should be named "zh-Hans-CN.axaml"
        // For simple codes like "en", the file should be named "en.axaml"
        string resourceUri = $"{_baseUri}{langCode}.axaml";
        return (ResourceDictionary)AvaloniaXamlLoader.Load(new Uri(resourceUri));
    }

    /// <summary>
    /// Removes untranslated entries (marked with <see cref="UntranslatedMarker"/> or empty/whitespace) from the dictionary.
    /// This allows Avalonia's resource lookup to fall through to the default dictionary.
    /// </summary>
    /// <returns>The number of entries removed.</returns>
    private static int RemoveUntranslatedEntries(ResourceDictionary dictionary)
    {
        List<object> keysToRemove = [];

        foreach (KeyValuePair<object, object?> entry in dictionary)
        {
            if (entry.Value is not string s || IsUntranslated(s))
            {
                keysToRemove.Add(entry.Key);
            }
        }

        foreach (object key in keysToRemove)
        {
            dictionary.Remove(key);
        }

        return keysToRemove.Count;
    }

    /// <summary>
    /// Determines whether a string value should be considered untranslated.
    /// </summary>
    /// <param name="value">The string value to check</param>
    /// <returns>True if the value is untranslated, otherwise false</returns>
    private static bool IsUntranslated(string value) => string.IsNullOrWhiteSpace(value) || value.Equals(UntranslatedMarker, StringComparison.OrdinalIgnoreCase);

    private static void EnsureInitialized()
    {
        if (_defaultLanguage is null || string.IsNullOrEmpty(_baseUri))
        {
            throw new InvalidOperationException("LocalizationHelper has not been initialized. Call Initialize() first.");
        }
    }
}