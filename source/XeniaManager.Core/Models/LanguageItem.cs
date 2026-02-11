using System.Globalization;
using XeniaManager.Core.Logging;

namespace XeniaManager.Core.Models;

/// <summary>
/// Represents a language item that combines CultureInfo with its display name for UI purposes
/// </summary>
public class LanguageItem
{
    /// <summary>
    /// Gets the CultureInfo object associated with this language item
    /// </summary>
    public CultureInfo Culture { get; }

    /// <summary>
    /// Gets or sets the display name for this language item
    /// This can be customized if needed, otherwise it defaults to the culture's display name
    /// </summary>
    public string DisplayName { get; set; }

    /// <summary>
    /// Initializes a new instance of the LanguageItem class
    /// </summary>
    /// <param name="culture">The CultureInfo to associate with this language item</param>
    public LanguageItem(CultureInfo culture)
    {
        Culture = culture;
        DisplayName = culture.DisplayName;
    }

    /// <summary>
    /// Returns a string representation of this language item
    /// The display name is capitalized using the associated culture's text info
    /// </summary>
    /// <returns>The capitalized display name of the language</returns>
    public override string ToString() => CapitalizeFirst(DisplayName, Culture);

    /// <summary>
    /// Capitalizes the first character of the given text using the specified culture's text info
    /// This method properly handles surrogate pairs (non-BMP characters) to ensure correct capitalization
    /// </summary>
    /// <param name="text">The text to capitalize the first character of</param>
    /// <param name="culture">The culture to use for capitalization rules</param>
    /// <returns>The text with the first character capitalized, or the original text if it's null or empty</returns>
    private static string CapitalizeFirst(string text, CultureInfo culture)
    {
        Logger.Trace<LanguageItem>($"Capitalizing first character of text: '{text}' for culture: {culture.Name}");
        
        if (string.IsNullOrEmpty(text))
        {
            Logger.Trace<LanguageItem>($"Text is null or empty, returning as-is: '{text}'");
            return text;
        }

        // Handle surrogate pairs / non-BMP characters correctly
        TextInfo textInfo = culture.TextInfo;
        int firstCharLength = char.IsSurrogatePair(text, 0) ? 2 : 1;
        
        string firstChar = text.Substring(0, firstCharLength);
        string remainingText = text.Substring(firstCharLength);
        string capitalizedText = textInfo.ToUpper(firstChar) + remainingText;
        
        Logger.Trace<LanguageItem>($"Capitalized text: '{capitalizedText}' from original: '{text}' for culture: {culture.Name}");
        return capitalizedText;
    }

    /// <summary>
    /// Determines whether this LanguageItem is equal to another object
    /// Two LanguageItems are considered equal if they have the same Culture
    /// </summary>
    /// <param name="obj">The object to compare with this LanguageItem</param>
    /// <returns>True if the objects are equal, false otherwise</returns>
    public override bool Equals(object? obj)
    {
        if (obj is LanguageItem other)
        {
            return Culture.Equals(other.Culture);
        }
        return false;
    }

    /// <summary>
    /// Returns the hash code for this LanguageItem
    /// The hash code is based on the underlying CultureInfo
    /// </summary>
    /// <returns>The hash code of the associated CultureInfo</returns>
    public override int GetHashCode()
    {
        return Culture.GetHashCode();
    }
}