using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Resources;
using System.Windows;

// Imported Libraries
using XeniaManager.Core;

namespace XeniaManager.Desktop.Utilities;

public static class LocalizationHelper
{
    // Variables
    /// <summary>
    /// ResourceManager used to grab UI text and it's localization
    /// </summary>
    private static ResourceManager _resourceManager { get; set; } = new ResourceManager("XeniaManager.Desktop.Resources.Language.Resource", Assembly.GetExecutingAssembly());

    /// <summary>
    /// Currently selected language
    /// </summary>
    private static ResourceDictionary? _currentLanguage { get; set; }

    /// <summary>
    /// Default language (English)
    /// </summary>
    private static readonly CultureInfo _defaultLanguage = new CultureInfo("en");

    /// <summary>
    /// Array of all the supported languages
    /// </summary>
    private static readonly CultureInfo[] _supportedLanguages =
    [
        _defaultLanguage, // English
        new CultureInfo("hr-HR"), // Croatian
        //new CultureInfo("ja-JP"), // Japanese/日本語
        new CultureInfo("de-DE"), // Deutsche
        //new CultureInfo("fr-FR"), // Français
        new CultureInfo("es-ES"), // Español
        new CultureInfo("it-IT"), // Italiano
        new CultureInfo("ko-KR"), // 한국어
        //new CultureInfo("zh-TW"), // 繁體中文 (Traditional Chinese)
        //new CultureInfo("pt-PT"), // Português
        //new CultureInfo("pl-PL"), // Polski
        //new CultureInfo("ru-RU"), // русский
        //new CultureInfo("sv-SE"), // Svenska
        //new CultureInfo("tr-TR"), // Türk
        //new CultureInfo("no-NO"), // Norsk
        //new CultureInfo("nl-NL"), // Nederlands
        //new CultureInfo("zh-CN") // 简体中文 (Simplified Chinese)
    ];

    // Functions
    /// <summary>
    /// Returns the supported languages as CultureInfo
    /// </summary>
    public static List<CultureInfo> GetSupportedLanguages()
    {
        return _supportedLanguages
            // if it's specific (hr-HR), grab the neutral parent (hr)
            .Select(ci => ci.IsNeutralCulture ? ci : ci.Parent)
            // remove duplicates if you accidentally listed both hr and hr-HR
            .Distinct()
            .ToList();
    }

    /// <summary>
    /// Loads the default language (English) resources.
    /// </summary>
    public static void LoadDefaultLanguage()
    {
        LoadLanguage(_defaultLanguage);
    }

    /// <summary>
    /// Loads the language resources for the specified language code.
    /// Throws an InvalidOperationException if the language is not supported.
    /// </summary>
    /// <param name="languageCode">The language code (default "en"). Can be either two-letter code or full locale.</param>
    public static void LoadLanguage(string languageCode = "en")
    {
        CultureInfo? selectedLanguage = null;

        // Try to match with the full locale first (e.g., "hr-HR")
        selectedLanguage = _supportedLanguages
            .FirstOrDefault(lang => lang.Name.Equals(languageCode, StringComparison.OrdinalIgnoreCase));

        // If not found, try to match with two-letter code (e.g., "hr")
        if (selectedLanguage == null)
        {
            selectedLanguage = _supportedLanguages
                .FirstOrDefault(lang => lang.TwoLetterISOLanguageName.Equals(languageCode, StringComparison.OrdinalIgnoreCase));
        }

        if (selectedLanguage == null)
        {
            throw new InvalidOperationException("Unsupported language");
        }

        LoadLanguage(selectedLanguage);
    }

    /// <summary>
    /// Helper method that loads a language resource dictionary based on the provided CultureInfo.
    /// Always ensures fallback to English for missing keys.
    /// </summary>
    /// <param name="language">The CultureInfo representing the language to load.</param>
    private static void LoadLanguage(CultureInfo language)
    {
        // Remove a previously loaded language dictionary if it exists.
        if (_currentLanguage != null)
        {
            Logger.Info("Unloading current language");
            Application.Current.Resources.MergedDictionaries.Remove(_currentLanguage);
        }

        Logger.Info($"Loading {language.DisplayName} language");

        ResourceDictionary resourceDictionary = new ResourceDictionary();

        // Always load the default language resources first as a fallback
        ResourceSet? defaultResourceSet = _resourceManager.GetResourceSet(_defaultLanguage, true, true)
                                          ?? throw new InvalidOperationException("Default language resources are missing");

        // Load all default (English) entries first
        foreach (DictionaryEntry entry in defaultResourceSet)
        {
            resourceDictionary[entry.Key] = entry.Value;
        }

        // If we're not loading the default language, try to load the target language and override
        if (!language.Equals(_defaultLanguage))
        {
            try
            {
                ResourceSet? languageResourceSet = _resourceManager.GetResourceSet(language, true, false);

                // If the specific culture doesn't exist, try the neutral culture
                if (languageResourceSet == null && !language.IsNeutralCulture)
                {
                    Logger.Warning($"Specific culture {language.Name} not found, trying neutral culture {language.Parent.Name}");
                    languageResourceSet = _resourceManager.GetResourceSet(language.Parent, true, false);
                }

                // Override default entries with localized ones if they exist
                if (languageResourceSet != null)
                {
                    Logger.Debug($"Found resource set for {language.DisplayName}, applying localized strings");
                    foreach (DictionaryEntry entry in languageResourceSet)
                    {
                        if (entry.Value != null && !string.IsNullOrEmpty(entry.Value.ToString()))
                        {
                            resourceDictionary[entry.Key] = entry.Value;
                        }
                    }
                }
                else
                {
                    Logger.Warning($"No resource set found for {language.DisplayName}, using default language only");
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error loading resources for {language.DisplayName}: {ex.Message}");
                Logger.Info("Falling back to default language resources only");
            }
        }

        _currentLanguage = resourceDictionary;
        Application.Current.Resources.MergedDictionaries.Add(_currentLanguage);
        CultureInfo.CurrentUICulture = language;

        Logger.Info($"Successfully loaded language resources. Total entries: {resourceDictionary.Count}");
    }

    /// <summary>
    /// Returns the string for a specific key in our Resources file
    /// </summary>
    /// <param name="key">Key we're looking for</param>
    /// <returns>UI text for a specific key</returns>
    public static String GetUiText(string key)
    {
        // First try to get from the currently loaded resource dictionary
        if (_currentLanguage != null && _currentLanguage.Contains(key))
        {
            string? value = _currentLanguage[key]?.ToString();
            if (!string.IsNullOrEmpty(value))
            {
                return value;
            }
        }

        // First try to get from current UI culture
        string? localizedUiText = _resourceManager.GetString(key, CultureInfo.CurrentUICulture);
        if (!string.IsNullOrEmpty(localizedUiText))
        {
            return localizedUiText;
        }

        // If not found and current culture is not neutral, try the neutral culture
        if (!CultureInfo.CurrentUICulture.IsNeutralCulture)
        {
            localizedUiText = _resourceManager.GetString(key, CultureInfo.CurrentUICulture.Parent);
            if (!string.IsNullOrEmpty(localizedUiText))
            {
                return localizedUiText;
            }
        }

        // Finally fallback to default language
        string? defaultText = _resourceManager.GetString(key, _defaultLanguage);
        if (!string.IsNullOrEmpty(defaultText))
        {
            return defaultText;
        }

        // If all else fails, return the key itself so you know what's missing
        Logger.Warning($"Missing localization key: {key}");
        return $"[{key}]";
    }
}