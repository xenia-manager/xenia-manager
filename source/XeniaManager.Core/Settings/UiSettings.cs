using System.Text.Json.Serialization;
using System.Windows;

namespace XeniaManager.Core.Settings;

/// <summary>
/// Subsection for UI settings
/// </summary>
public class UiSettings
{
    /// <summary>
    /// <para>Language used by Xenia Manager UI</para>
    /// Default Language = English
    /// </summary>
    [JsonPropertyName("language")]
    public string Language { get; set; } = "en";

    /// <summary>
    /// <para>Theme used by Xenia Manager UI</para>
    /// Default Theme = Light
    /// </summary>
    [JsonPropertyName("theme")]
    public Theme Theme { get; set; } = Theme.Light;

    [JsonPropertyName("window")]
    public WindowProperties Window { get; set; } = new WindowProperties();

    public class WindowProperties
    {
        [JsonPropertyName("top")]
        public double Top { get; set; } = 0;

        [JsonPropertyName("left")]
        public double Left { get; set; } = 0;

        [JsonPropertyName("width")]
        public double Width { get; set; } = 885;

        [JsonPropertyName("height")]
        public double Height { get; set; } = 720;

        [JsonPropertyName("state")]
        public WindowState State { get; set; } = WindowState.Normal;
    }

    [JsonPropertyName("game_library")]
    public LibraryProperties Library { get; set; } = new LibraryProperties();

    public class LibraryProperties
    {
        [JsonPropertyName("game_title")]
        public bool GameTitle { get; set; } = true;

        [JsonPropertyName("compatibility_rating")]
        public bool CompatibilityRating { get; set; } = true;

        [JsonPropertyName("view")]
        public LibraryViewType View { get; set; } = LibraryViewType.Grid;

        [JsonPropertyName("zoom")]
        public double Zoom { get; set; } = 1.0;
    }
    
    [JsonPropertyName("game_loading_screen")]
    public bool ShowGameLoadingBackground { get; set; } = true;
}