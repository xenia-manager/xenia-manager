using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Resources;
using System.Windows;

using XeniaManager.Core;

namespace XeniaManager.Desktop.Utilities;


public static class LocalizationHelper
{
    // Variables
    /// <summary>
    /// ResourceManager used to grab UI text and it's localization
    /// </summary>
    private static ResourceManager _resourceManager { get; set; } = new ResourceManager("XeniaManager.Desktop.Resources.Resource", Assembly.GetExecutingAssembly());
    
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
    // TODO: Add more supported languages
    public static readonly CultureInfo[] SupportedLanguages = [
        _defaultLanguage,
        new CultureInfo("hr")];

    // Functions
    /// <summary>
    /// Returns the supported languages with their display names and ISO codes.
    /// </summary>
    public static List<(string Name, string Code)> GetSupportedLanguages()
    {
        return SupportedLanguages
            .Select(language => (Name: language.DisplayName, Code: language.TwoLetterISOLanguageName))
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
    /// <param name="languageCode">The ISO language code (default "en").</param>
    public static void LoadLanguage(string languageCode = "en")
    {
        CultureInfo? selectedLanguage = SupportedLanguages
            .FirstOrDefault(lang => lang.TwoLetterISOLanguageName.Equals(languageCode, StringComparison.OrdinalIgnoreCase));

        if (selectedLanguage == null)
        {
            throw new InvalidOperationException("Unsupported language");
        }

        LoadLanguage(selectedLanguage);
    }

    /// <summary>
    /// Helper method that loads a language resource dictionary based on the provided CultureInfo.
    /// </summary>
    /// <param name="language">The CultureInfo representing the language to load.</param>
    private static void LoadLanguage(CultureInfo language)
    {
        // Remove previously loaded language dictionary if it exists.
        if (_currentLanguage != null)
        {
            Logger.Info("Unloading current language");
            Application.Current.Resources.MergedDictionaries.Remove(_currentLanguage);
        }
        Logger.Info($"Loading {language.DisplayName} language");
        ResourceManager rm = XeniaManager.Desktop.Resources.Resource.ResourceManager;
        ResourceSet resourceSet = rm.GetResourceSet(language, true, true)
            ?? throw new InvalidOperationException("Language could not be loaded. (Maybe it's missing)");

        ResourceDictionary resourceDictionary = new ResourceDictionary();
        foreach (DictionaryEntry entry in resourceSet)
        {
            resourceDictionary.Add(entry.Key, entry.Value);
        }

        _currentLanguage = resourceDictionary;
        Application.Current.Resources.MergedDictionaries.Add(_currentLanguage);
        CultureInfo.CurrentUICulture = language;
    }

    /// <summary>
    /// Returns the string for a specific key in our Resources file
    /// </summary>
    /// <param name="key">Key we're looking for</param>
    /// <returns>UI text for specific key</returns>
    public static String GetUIText(string key)
    {
        return _resourceManager.GetString(key, CultureInfo.CurrentUICulture);
    }
}