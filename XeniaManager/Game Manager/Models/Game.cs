using System;

// Imported
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace XeniaManager
{
    /// <summary>
    /// Represents a game installed in the system.
    /// </summary>
    public class Game
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
        /// The unique identifier for the game
        /// </summary>
        [JsonProperty("media_id")]
        public string? MediaId { get; set; }

        /// <summary>
        /// This tells the Xenia Manager which Xenia version (Stable/Canary/Netplay/Custom) the game wants to use
        /// null if it doesn't exist
        /// </summary>
        [JsonProperty("emulator_version")]
        public EmulatorVersion EmulatorVersion { get; set; }

        /// <summary>
        /// Holds how much time user spent on playing this game
        /// </summary>
        [JsonProperty("playtime")]
        public double? Playtime { get; set; }

        /// <summary>
        /// URL to the github issues page for the game
        /// </summary>
        [JsonProperty("gamecompatibility_url")]
        public string? GameCompatibilityURL { get; set; }

        /// <summary>
        /// Tells the current compatibility of the game 
        /// <para>(Unknown, Unplayable, Loads, Gameplay, Playable)</para>
        /// </summary>
        [JsonProperty("compatibility_rating")]
        public CompatibilityRating CompatibilityRating { get; set; }

        /// <summary>
        /// Holds all of the paths towards different artworks for the game
        /// </summary>
        [JsonProperty("artwork")]
        public GameArtwork Artwork { get; set; } = new GameArtwork();

        /// <summary>
        /// Holds all of the paths towards cached versions of the artworks for the game
        /// </summary>
        [JsonProperty("artwork_cache")]
        public GameArtwork? ArtworkCache { get; set; } = new GameArtwork();

        /// <summary>
        /// Grouping of all file paths related to the game
        /// </summary>
        [JsonProperty("file_locations")]
        public GameFiles FileLocations { get; set; } = new GameFiles();
    }

    /// <summary>
    /// Enum representing the compatibility rating of the game.
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum CompatibilityRating
    {
        Unknown,
        Unplayable,
        Loads,
        Gameplay,
        Playable
    }

    /// <summary>
    /// Enum representing the Xenia version the game wants to use.
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum EmulatorVersion
    {
        // Stable,
        Canary,
        Netplay,
        Custom
    }

    /// <summary>
    /// All of the game artwork used by Xenia Manager
    /// </summary>
    public class GameArtwork
    {
        /// <summary>
        /// The file path to the game's background
        /// </summary>
        [JsonProperty("background")]
        public string? Background { get; set; }

        /// <summary>
        /// The file path to the game's boxart
        /// </summary>
        [JsonProperty("boxart")]
        public string? Boxart { get; set; }

        /// <summary>
        /// The file path to the game's shortcut icon
        /// </summary>
        [JsonProperty("icon")]
        public string? Icon { get; set; }
    }

    /// <summary>
    /// Grouping of all file locations related to the game (ISO, patch, configuration, and emulator)
    /// </summary>
    public class GameFiles
    {
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

        /// <summary>
        /// The file path to the game's configuration file (null if it doesn't exist)
        /// </summary>
        [JsonProperty("config_location")]
        public string? ConfigFilePath { get; set; }

        /// <summary>
        /// The location of the custom Xenia executable (null if not applicable)
        /// </summary>
        [JsonProperty("emulator_executable_location")]
        public string? EmulatorExecutableLocation { get; set; }
    }
}
