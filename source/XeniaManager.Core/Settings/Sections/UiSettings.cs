using System.Text.Json.Serialization;
using Avalonia.Controls;
using Avalonia.Media;

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
    }
}