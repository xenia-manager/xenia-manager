using System;

// Imported via NuGet
using Newtonsoft.Json;

namespace Xenia_Manager.Classes
{
    /// <summary>
    /// Represents a game installed in the system.
    /// </summary>
    public class InstalledGame
    {
        /// <summary>
        /// The title of the game
        /// </summary>
        [JsonProperty("title")]
        public string? Title { get; set; }

        /// <summary>
        /// The unique identifier for the game
        /// </summary>
        [JsonProperty("game_id")]
        public string? GameId { get; set; }

        /// <summary>
        /// The file path to the game's icon
        /// </summary>
        [JsonProperty("icon")]
        public string? IconFilePath { get; set; }

        /// <summary>
        /// The file path to the game's ISO file
        /// </summary>
        [JsonProperty("game_location")]
        public string? GameFilePath { get; set; }

        /// <summary>
        /// The file path to the game's patch file
        /// </summary>
        [JsonProperty("patch_location")]
        public string? PatchFilePath { get; set; }
    }

    /// <summary>
    /// This is used to parse JSON files that have game names and their box arts
    /// </summary>
    public class GameInfo
    {
        // This is for Andy Declari's JSON file
        [JsonProperty("Name")]
        public string? Name { get; set; }

        [JsonProperty("Front")]
        public CoverDetails? Front { get; set; }

        [JsonProperty("Back")]
        public CoverDetails? Back { get; set; }

        // This is for Wikipedia JSON file
        [JsonProperty("Title")]
        public string? Title { get; set; }

        [JsonProperty("Link")]
        public string? Link { get; set; }

        [JsonProperty("Image URL")]
        public string? ImageUrl { get; set; }
    }

    /// <summary>
    /// This is for CoverDetails (Andy Declari's JSON file)
    /// </summary>
    public class CoverDetails
    {
        [JsonProperty("Full Size")]
        public string? FullSize { get; set; }

        [JsonProperty("Thumbnail")]
        public string? Thumbnail { get; set; }
    }

    /// <summary>
    /// This is used to parse a JSON file that holds all of the Game Patches for Xenia
    /// </summary>
    public class GamePatch
    {
        [JsonProperty("name")]
        public required string gameName { get; set; }

        [JsonProperty("download_url")]
        public required string url { get; set; }
    }

    /// <summary>
    /// This is used for parsing Xenia Manager settings which are stored in a .JSON file
    /// </summary>
    public class Configuration
    {
        /// <summary>
        /// <para>"id" property from this JSON file</para>
        /// <para>Used to update the emulator</para>
        /// </summary>
        [JsonProperty("version")]
        public int? Version { get; set; }

        /// <summary>
        /// <para>Date of publishing of the installed build</para>
        /// </summary>
        [JsonProperty("release_date")]
        public DateTime? ReleaseDate { get; set; }

        /// <summary>
        /// <para>Date when the last check for updates was</para>
        /// </summary>
        [JsonProperty("last_update_check_date")]
        public DateTime? LastUpdateCheckDate { get; set; }

        /// <summary>
        /// <para>This stores the location where the emulator is installed</para>
        /// </summary>
        [JsonProperty("emulator_location")]
        public string? EmulatorLocation { get; set; }
    }
}
