using System.Collections;
using System.Globalization;
using System.Resources;
using System.Windows;

using XeniaManager.Core;

namespace XeniaManager.Desktop.Utilities
{
    public static class LocalizationHelper
    {
        // Variables
        /// <summary>
        /// Currently selected language
        /// </summary>
        private static ResourceDictionary? _currentLanguage { get; set; }

        /// <summary>
        /// Default language (English)
        /// </summary>
        private static readonly CultureInfo _defaultLanguage = new CultureInfo("en");

        /// <summary>
        /// Array of all of the supported languages
        /// </summary>
        private static readonly CultureInfo[] _supportedLanguages = [
            _defaultLanguage,
            new CultureInfo("hr")];

        // Functions
        /// <summary>
        /// Returns the supported languages with their display names and ISO codes.
        /// </summary>
        public static List<(string Name, string Code)> GetSupportedLanguages()
        {
            return _supportedLanguages
                .Select(culture => (Name: culture.DisplayName, Code: culture.TwoLetterISOLanguageName))
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
            CultureInfo? selectedCulture = _supportedLanguages
                .FirstOrDefault(lang => lang.TwoLetterISOLanguageName.Equals(languageCode, StringComparison.OrdinalIgnoreCase));

            if (selectedCulture == null)
            {
                throw new InvalidOperationException("Unsupported language");
            }

            LoadLanguage(selectedCulture);
        }

        /// <summary>
        /// Helper method that loads a language resource dictionary based on the provided CultureInfo.
        /// </summary>
        /// <param name="culture">The CultureInfo representing the language to load.</param>
        private static void LoadLanguage(CultureInfo culture)
        {
            // Remove previously loaded language dictionary if it exists.
            if (_currentLanguage != null)
            {
                Logger.Info("Unloading current language");
                Application.Current.Resources.MergedDictionaries.Remove(_currentLanguage);
            }
            Logger.Info($"Loading {culture.DisplayName} language");
            ResourceManager rm = XeniaManager.Desktop.Resources.Resource.ResourceManager;
            ResourceSet resourceSet = rm.GetResourceSet(culture, true, true)
                ?? throw new InvalidOperationException("Resources for the specified language could not be loaded.");

            ResourceDictionary resourceDictionary = new ResourceDictionary();
            foreach (DictionaryEntry entry in resourceSet)
            {
                resourceDictionary.Add(entry.Key, entry.Value);
            }

            _currentLanguage = resourceDictionary;
            Application.Current.Resources.MergedDictionaries.Add(_currentLanguage);
        }
    }
}
