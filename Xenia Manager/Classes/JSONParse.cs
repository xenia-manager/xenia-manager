using System;
using System.IO;

// Imported
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
        /// The unique identifier for the game
        /// </summary>
        [JsonProperty("media_id")]
        public string? MediaId { get; set; }

        /// <summary>
        /// URL to the github issues page for the game
        /// </summary>
        [JsonProperty("gamecompatibility_url")]
        public string? GameCompatibilityURL { get; set; }

        /// <summary>
        /// Tells the current compatibility of the game (Unknown, Unplayable, Loads, Gameplay, Playable)
        /// </summary>
        [JsonProperty("compatibility_rating")]
        public string? CompatibilityRating { get; set; }

        /// <summary>
        /// The file path to the game's boxart
        /// </summary>
        [JsonProperty("icon")]
        public string? BoxartFilePath { get; set; }

        /// <summary>
        /// The file path to the game's cached boxart
        /// </summary>
        [JsonProperty("cached_icon")]
        public string? CachedIconPath { get; set; }

        /// <summary>
        /// The file path to the game's shortcut icon
        /// </summary>
        [JsonProperty("shortcut_icon")]
        public string? ShortcutIconFilePath { get; set; }

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
        /// The file path to the game's configuration file
        /// null if it doesn't exist
        /// </summary>
        [JsonProperty("config_location")]
        public string? ConfigFilePath { get; set; }

        /// <summary>
        /// This tells the Xenia Manager which Xenia version (Stable/Canary/Netplay/Custom) the game wants to use
        /// null if it doesn't exist
        /// </summary>
        [JsonProperty("emulator_version")]
        public string? EmulatorVersion { get; set; }

        /// <summary>
        /// This is mostly to store the location to the executable of the Custom version of Xenia
        /// null if it doesn't exist or not needed
        /// </summary>
        [JsonProperty("emulator_executable_location")]
        public string? EmulatorExecutableLocation { get; set; }
    }

    /// <summary>
    /// This is used to parse games list that are stored as .JSON files
    /// </summary>
    public class GameInfo
    {
        [JsonProperty("id")]
        public string? Id { get; set; }

        [JsonProperty("alternative_id")]
        public List<string> AlternativeId { get; set; }

        [JsonProperty("title")]
        public string? Title { get; set; }

        [JsonProperty("url")]
        public string? Link { get; set; }

        [JsonProperty("artwork")]
        public Artwork? Artwork { get; set; }
    }

    /// <summary>
    /// This is used to parse the "artwork" section of .JSON file
    /// </summary>
    public class Artwork
    {
        // Universal
        [JsonProperty("boxart")]
        public string Boxart { get; set; }

        // Launchbox DB specific
        [JsonProperty("disc")]
        public string? Disc { get; set; }

        [JsonProperty("logo")]
        public string? Logo { get; set; }

        // Xbox Marketplace specific
        [JsonProperty("background")]
        public string? Background { get; set; }

        [JsonProperty("banner")]
        public string? Banner { get; set; }

        [JsonProperty("icon")]
        public string? Icon { get; set; }

        [JsonProperty("gallery")]
        public List<string>? Gallery { get; set; }
    }

    /// <summary>
    /// Xbox Marketplace game info
    /// </summary>
    public class Title
    {
        [JsonProperty("full")]
        public string Full { get; set; }

        [JsonProperty("reduced")]
        public string Reduced { get; set; }
    }

    public class Description
    {
        [JsonProperty("full")]
        public string Full { get; set; }

        [JsonProperty("short")]
        public string Short { get; set; }
    }

    public class Media
    {
        [JsonProperty("media_id")]
        public string Id { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("edition")]
        public string Edition { get; set; }

        [JsonProperty("region")]
        public string Region { get; set; }
    }

    public class Parent
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }
    }

    public class Products
    {
        [JsonProperty("parent")]
        public List<Parent> Parent { get; set; }

        [JsonProperty("related")]
        public List<object> Related { get; set; }
    }

    /// <summary>
    /// Used to parse specific game details when it has been selected in Xbox Marketplace source
    /// </summary>
    public class XboxMarketplaceGameInfo
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("title")]
        public Title Title { get; set; }

        [JsonProperty("genre")]
        public List<string> Genres { get; set; }

        [JsonProperty("developer")]
        public string Developer { get; set; }

        [JsonProperty("publisher")]
        public string Publisher { get; set; }

        [JsonProperty("release_date")]
        public string ReleaseDate { get; set; }

        [JsonProperty("user_rating")]
        public string UserRating { get; set; }

        [JsonProperty("description")]
        public Description Description { get; set; }

        [JsonProperty("media")]
        public List<Media> Media { get; set; }

        [JsonProperty("artwork")]
        public Artwork? Artwork { get; set; }

        [JsonProperty("products")]
        public Products products { get; set; }
    }

    /// <summary>
    /// This is used to parse a JSON file that holds all of the Game Patches for Xenia
    /// </summary>
    public class GamePatch
    {
        [JsonProperty("name")]
        public string gameName { get; set; }

        [JsonProperty("download_url")]
        public string url { get; set; }
    }

    /// <summary>
    /// Patch class used to read and edit patches
    /// </summary>
    public class Patch
    {
        /// <summary>
        /// Name of the patch
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Disabled/Enabled patch
        /// </summary>
        public bool IsEnabled { get; set; }

        /// <summary>
        /// Explains what patch does, can be null
        /// </summary>
        public string? Description { get; set; }
    }

    /// <summary>
    /// Class that contains everything about content that is going to be installed
    /// </summary>
    public class GameContent
    {
        public string GameId { get; set; }

        public string ContentTitle { get; set; }

        public string ContentDisplayName { get; set; }

        public string ContentType { get; set; }

        public string ContentTypeValue { get; set; }

        public string ContentPath { get; set; }
    }

    /// <summary>
    /// This is used for parsing Xenia Manager settings which are stored in a .JSON file
    /// </summary>
    public class Configuration
    {
        /// <summary>
        /// <para>This stores the location where the Xenia VFS Dump is stored</para>
        /// </summary>
        [JsonProperty("xeniavfsdumptool_location")]
        public string? VFSDumpToolLocation { get; set; }

        /// <summary>
        /// <para>This stores the selected theme for Xenia Manager</para>
        /// </summary>
        [JsonProperty("theme_selected")]
        public string? ThemeSelected { get; set; }

        /// <summary>
        /// <para>This tells Xenia Manager to launch in normal or fullscreen mode on startup</para>
        /// </summary>
        [JsonProperty("fullscreen")]
        public bool? FullscreenMode { get; set; }

        /// <summary>
        /// This is to store Xenia Manager update checks
        /// </summary>
        [JsonProperty("manager")]
        public UpdateInfo Manager { get; set; }

        /// <summary>
        /// This is to store Xenia Manager's Xenia update checks
        /// </summary>
        [JsonProperty("xenia_stable")]
        public EmulatorInfo XeniaStable { get; set; }

        /// <summary>
        /// This is to store Xenia Manager's Xenia Canary update checks
        /// </summary>
        [JsonProperty("xenia_canary")]
        public EmulatorInfo XeniaCanary { get; set; }

        /// <summary>
        /// This is to store Xenia Manager's Xenia Netplay update checks
        /// </summary>
        [JsonProperty("xenia_netplay")]
        public EmulatorInfo XeniaNetplay { get; set; }

        /// <summary>
        /// Saves the configuration object to a JSON file asynchronously
        /// </summary>
        /// <param name="filePath">The file path where the JSON file will be saved.</param>
        public async Task SaveAsync(string filePath)
        {
            // Serialize the object to JSON
            string json = JsonConvert.SerializeObject(this, Formatting.Indented);

            // Write JSON to file asynchronously
            await File.WriteAllTextAsync(filePath, json);
        }
    }

    /// <summary>
    /// All options regarding Xenia Manager
    /// </summary>
    public class UpdateInfo
    {
        /// <summary>
        /// <para>"id" property from this JSON file</para>
        /// <para>Used to update the emulator</para>
        /// </summary>
        [JsonProperty("version")]
        public string? Version { get; set; }

        /// <summary>
        /// <para>"id" property from this JSON file</para>
        /// <para>Used to update the emulator</para>
        /// </summary>
        [JsonProperty("update_available")]
        public bool? UpdateAvailable { get; set; }

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
    }

    /// <summary>
    /// All options regarding Xenia
    /// </summary>
    public class EmulatorInfo
    {
        /// <summary>
        /// <para>This stores the location where the emulator is installed</para>
        /// </summary>
        [JsonProperty("emulator_location")]
        public string? EmulatorLocation { get; set; }

        /// <summary>
        /// <para>This stores the location where the emulator executable is</para>
        /// </summary>
        [JsonProperty("executable_location")]
        public string? ExecutableLocation { get; set; }

        /// <summary>
        /// <para>This stores the location where the emulator configuration file is</para>
        /// </summary>
        [JsonProperty("configurationfile_location")]
        public string? ConfigurationFileLocation { get; set; }

        /// <summary>
        /// <para>"id" property from this JSON file</para>
        /// <para>Used to update the emulator</para>
        /// </summary>
        [JsonProperty("version")]
        public string? Version { get; set; }

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
    }
}