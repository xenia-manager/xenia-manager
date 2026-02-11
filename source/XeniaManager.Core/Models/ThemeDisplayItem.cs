namespace XeniaManager.Core.Models;

/// <summary>
/// Represents a theme option that can be displayed in the UI
/// </summary>
public class ThemeDisplayItem
{
    /// <summary>
    /// The display name of the theme that will be shown to the user
    /// </summary>
    public string DisplayName { get; set; }

    /// <summary>
    /// The actual theme value associated with this display item
    /// </summary>
    public Theme ThemeValue { get; set; }

    /// <summary>
    /// Returns a string representation of this ThemeDisplayItem
    /// </summary>
    /// <returns>Display name of ThemeDisplayItem</returns>
    public override string ToString() => DisplayName;
}