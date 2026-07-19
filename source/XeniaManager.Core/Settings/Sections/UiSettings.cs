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

        /// <summary>
        /// Settings for the game library
        /// </summary>
        [JsonPropertyName("game_library")]
        public LibraryProperties Library { get; set; } = new LibraryProperties();

        /// <summary>
        /// Settings for the game library
        /// </summary>
        public class LibraryProperties
        {
            /// <summary>
            /// Current view mode for the game library
            /// </summary>
            [JsonPropertyName("is_grid_view")]
            public LibraryViewOption ViewOption { get; set; } = LibraryViewOption.Grid;

            /// <summary>
            /// Grid view specific settings
            /// </summary>
            [JsonPropertyName("grid_view")]
            public GridViewSettings GridView { get; set; } = new GridViewSettings();

            /// <summary>
            /// Settings for the grid view
            /// </summary>
            public class GridViewSettings
            {
                /// <summary>
                /// Show game title on game tile
                /// </summary>
                [JsonPropertyName("game_title")]
                public bool GameTitle { get; set; } = true;

                /// <summary>
                /// Show compatibility rating on game tile
                /// </summary>
                [JsonPropertyName("compatibility_rating")]
                public bool CompatibilityRating { get; set; } = true;

                /// <summary>
                /// Show Xenia version on game tile
                /// </summary>
                [JsonPropertyName("xenia_version")]
                public bool XeniaVersion { get; set; } = false;

                /// <summary>
                /// Zoom level for grid view (1.0 = 100%)
                /// </summary>
                [JsonPropertyName("zoom")]
                public double Zoom { get; set; } = 1.0;

                /// <summary>
                /// Launch game on double click
                /// </summary>
                [JsonPropertyName("double_click_open")]
                public bool DoubleClickLaunch { get; set; } = false;
            }

            /// <summary>
            /// List view specific settings
            /// </summary>
            [JsonPropertyName("list_view")]
            public ListViewSettings ListView { get; set; } = new ListViewSettings();

            /// <summary>
            /// Settings for the list view
            /// </summary>
            public class ListViewSettings
            {
                /// <summary>
                /// Show compatibility rating column
                /// </summary>
                [JsonPropertyName("compatibility_rating")]
                public bool CompatibilityRating { get; set; } = true;

                /// <summary>
                /// Show playtime column
                /// </summary>
                [JsonPropertyName("playtime")]
                public bool Playtime { get; set; } = true;

                /// <summary>
                /// Show Xenia version column
                /// </summary>
                [JsonPropertyName("xenia_version")]
                public bool XeniaVersion { get; set; } = true;

                /// <summary>
                /// Show last played column
                /// </summary>
                [JsonPropertyName("last_played")]
                public bool LastPlayed { get; set; } = true;

                /// <summary>
                /// Show game icon column
                /// </summary>
                [JsonPropertyName("show_icon")]
                public bool ShowIcon { get; set; } = true;
            }

            /// <summary>
            /// Sorting settings for the game library
            /// </summary>
            [JsonPropertyName("sort")]
            public SortSettings Sort { get; set; } = new SortSettings();

            /// <summary>
            /// Settings for sorting the game library
            /// </summary>
            public class SortSettings
            {
                /// <summary>
                /// Which property to sort by
                /// </summary>
                [JsonPropertyName("option")]
                public int Option { get; set; } = 0;

                /// <summary>
                /// Whether to sort in descending order
                /// </summary>
                [JsonPropertyName("descending")]
                public bool Descending { get; set; } = false;
            }
        }

        /// <summary>
        /// Show game loading screen when launching games
        /// </summary>
        [JsonPropertyName("loading_screen")]
        public bool LoadingScreen { get; set; } = true;
    }
}