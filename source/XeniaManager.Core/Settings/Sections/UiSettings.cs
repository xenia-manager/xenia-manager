using System.Text.Json.Serialization;
using Avalonia.Controls;
using XeniaManager.Core.Models;

namespace XeniaManager.Core.Settings.Sections;

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
    /// Window properties
    /// </summary>
    [JsonPropertyName("window")]
    public WindowProperties Window { get; set; } = new WindowProperties();

    public class WindowProperties
    {
        /// <summary>
        /// The position of the window on screen
        /// </summary>
        [JsonPropertyName("position")]
        public WindowPosition Position { get; set; } = new WindowPosition();

        /// <summary>
        /// Represents the X and Y coordinates of the window position
        /// </summary>
        public class WindowPosition
        {
            /// <summary>
            /// The X coordinate of the window position
            /// </summary>
            [JsonPropertyName("x")]
            public int X { get; set; } = 0;

            /// <summary>
            /// The Y coordinate of the window position
            /// </summary>
            [JsonPropertyName("y")]
            public int Y { get; set; } = 0;
        }

        /// <summary>
        /// The width of the window
        /// </summary>
        [JsonPropertyName("width")]
        public double Width { get; set; } = 885;

        /// <summary>
        /// The height of the window
        /// </summary>
        [JsonPropertyName("height")]
        public double Height { get; set; } = 720;

        /// <summary>
        /// The state of the window (normal, minimized, maximized)
        /// </summary>
        [JsonPropertyName("state")]
        public WindowState State { get; set; } = WindowState.Normal;

        [JsonPropertyName("game_library")]
        public LibraryProperties Library { get; set; } = new LibraryProperties();

        public class LibraryProperties
        {
            [JsonPropertyName("game_title")]
            public bool GameTitle { get; set; } = true;

            [JsonPropertyName("compatibility_rating")]
            public bool CompatibilityRating { get; set; } = true;

            [JsonPropertyName("xenia_version")]
            public bool XeniaVersion { get; set; } = false;

            [JsonPropertyName("zoom")]
            public double Zoom { get; set; } = 1.0;

            [JsonPropertyName("double_click_open")]
            public bool DoubleClickLaunch { get; set; } = false;

            [JsonPropertyName("is_grid_view")]
            public bool IsGridView { get; set; } = true;

            [JsonPropertyName("list_compatibility_rating")]
            public bool ListCompatibilityRating { get; set; } = true;

            [JsonPropertyName("list_playtime")]
            public bool ListPlaytime { get; set; } = true;

            [JsonPropertyName("list_xenia_version")]
            public bool ListXeniaVersion { get; set; } = true;

            [JsonPropertyName("list_last_played")]
            public bool ListLastPlayed { get; set; } = true;

            [JsonPropertyName("list_show_icon")]
            public bool ListShowIcon { get; set; } = true;

            [JsonPropertyName("sort_option")]
            public int SortOption { get; set; } = 0;

            [JsonPropertyName("sort_descending")]
            public bool SortDescending { get; set; } = false;
        }

        /// <summary>
        /// Show game loading screen when launching games
        /// </summary>
        [JsonPropertyName("loading_screen")]
        public bool LoadingScreen { get; set; } = true;
    }
}