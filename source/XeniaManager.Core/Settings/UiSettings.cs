using System.Text.Json.Serialization;
using System.Windows;
using System.Windows.Media;

// Imported Libraries
using XeniaManager.Core.Enum;

namespace XeniaManager.Core.Settings;

public class DataGridColumnSettings
{
    [JsonPropertyName("display_index")]
    public int DisplayIndex { get; set; }

    [JsonPropertyName("width")]
    public double Width { get; set; }

    [JsonPropertyName("actual_width")]
    public double ActualWidth { get; set; }
}

public class DataGridViewSettings
{
    [JsonPropertyName("columns")]
    public Dictionary<string, DataGridColumnSettings> Columns { get; set; } = new Dictionary<string, DataGridColumnSettings>();
}

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

    /// <summary>
    /// <para>Accent Color used by Xenia Manager UI</para>
    /// Default Accent = DarkGreen
    /// </summary>
    [JsonPropertyName("accent_color")]
    public Color AccentColor { get; set; } = Colors.DarkGreen;


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

        [JsonPropertyName("list_view_settings")]
        public DataGridViewSettings ListViewSettings { get; set; } = new DataGridViewSettings();

        [JsonPropertyName("zoom")]
        public double Zoom { get; set; } = 1.0;

        [JsonPropertyName("double_click_open")]
        public bool DoubleClickToOpenGame { get; set; } = false;
    }

    [JsonPropertyName("game_loading_screen")]
    public bool ShowGameLoadingBackground { get; set; } = true;
}