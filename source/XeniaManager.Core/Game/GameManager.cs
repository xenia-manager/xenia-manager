using System.Text.Json;
using System.Text.Json.Serialization;

namespace XeniaManager.Core.Game;

public class Game
{
    /// <summary>
    /// The unique identifier for the game
    /// </summary>
    [JsonPropertyName("game_id")]
    public string? GameId { get; set; }
    
    /// <summary>
    /// Alternative game_id's that the game can use (Useful for searching for game compatibility)
    /// </summary>
    [JsonPropertyName("alternative_id")]
    public List<string> AlternativeIDs { get; set; } = new List<string>();

    /// <summary>
    /// The unique identifier for the game
    /// </summary>
    [JsonPropertyName("media_id")]
    public string? MediaId { get; set; }
    
    /// <summary>
    /// Game name
    /// </summary>
    [JsonPropertyName("title")]
    public string Title { get; set; }
    
    /// <summary>
    /// This tells the Xenia Manager which Xenia version (Custom/Canary/Mousehook/Netplay) the game wants to use
    /// </summary>
    [JsonPropertyName("xenia_version")]
    public XeniaVersion XeniaVersion { get; set; }
}

public static class GameManager
{
    // Variables
    /// <summary>
    /// All the currently installed games
    /// </summary>
    public static List<Game> Games { get; set; }

    private static readonly JsonSerializerOptions _jsonSerializerOptions = new JsonSerializerOptions
    {
        WriteIndented = true
    };
    
    // Functions
    /// <summary>
    /// Initializes new game library
    /// </summary>
    private static void InitializeGameLibrary()
    {
        Logger.Info("Creating new game library");
        Games = new List<Game>();
    }
    
    /// <summary>
    /// Loads all the games from a .JSON file
    /// </summary>
    public static void LoadLibrary()
    {
        if (!File.Exists(Constants.GameLibrary))
        {
            Logger.Warning("Couldn't find file that stores all of the installed games");
            InitializeGameLibrary();
            SaveLibrary();
            return;
        }

        Logger.Info("Loading game library");
        Games = JsonSerializer.Deserialize<List<Game>>(File.ReadAllText(Constants.GameLibrary));
    }
    
    /// <summary>
    /// Saves all the games into a .JSON file
    /// </summary>
    public static void SaveLibrary()
    {
        try
        {
            string gameLibrarySerialized = JsonSerializer.Serialize(Games, _jsonSerializerOptions);
            File.WriteAllText(Constants.GameLibrary, gameLibrarySerialized);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, ex.Message);
        }
    }
}