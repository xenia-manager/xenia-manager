using System.ComponentModel;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

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

    public static async Task<(string title, string game_id, string media_id)> GetGameDetailsWithXenia(string gamePath, XeniaVersion version)
    {
        Logger.Info($"Launching the game with Xenia {version} to find game title, game_id and media_id");
        Process xenia = new Process();
        switch (version)
        {
            case XeniaVersion.Canary:
                xenia.StartInfo.FileName = Path.Combine(Constants.BaseDir, Constants.Xenia.Canary.ExecutableLocation);
                xenia.StartInfo.WorkingDirectory = Path.Combine(Constants.BaseDir, Constants.Xenia.Canary.EmulatorDir);
                break;
            case XeniaVersion.Mousehook:
                throw new NotImplementedException();
                break;
            case XeniaVersion.Netplay:
                throw new NotImplementedException();
                break;
            default:
                throw new InvalidEnumArgumentException("Inavlid Xenia version.");
        }

        xenia.StartInfo.Arguments = $@"""{gamePath}"""; // Add game path to the arguments so it's launched through Xenia
        // Start Xenia and wait for input
        xenia.Start();
        xenia.WaitForInputIdle();

        // Things we're looking for
        string gameTitle = "Not found";
        string game_id = "Not found";
        string media_id = string.Empty;

        Process process = Process.GetProcessById(xenia.Id);
        Logger.Info("Trying to find the game title from Xenia Window Title");
        int NumberOfTries = 0;

        // Method 1 - Using Xenia Window Title
        // Repeats for 10 seconds before moving on
        while (gameTitle == "Not found")
        {
            // Regex used to scrape Title and TitleID from Xenia Window Title
            Regex titleRegex = new Regex(@"\]\s+([^<]+)\s+<");
            Regex idRegex = new Regex(@"\[(\w{8}) v[\d\.]+\]");

            // Grabbing Title from Xenia Window Title
            Match gameNameMatch = titleRegex.Match(process.MainWindowTitle);
            gameTitle = gameNameMatch.Success ? gameNameMatch.Groups[1].Value : "Not found";

            // Grabbing TitleID from Xenia Window Title
            Match versionMatch = idRegex.Match(process.MainWindowTitle);
            game_id = versionMatch.Success ? versionMatch.Groups[1].Value : "Not found";

            process = Process.GetProcessById(xenia.Id);

            NumberOfTries++;

            // Check if this reached the maximum number of attempts
            // If it did, break the while loop
            if (NumberOfTries > 1000)
            {
                gameTitle = "Not found";
                game_id = "Not found";
                break;
            }
            await Task.Delay(100); // Delay between repeating to ensure everything loads
        }
        
        xenia.Kill(); // Force close Xenia
        
        // Method 2 - Using Xenia.log (In case method 1 fails)
        // Checks if xenia.log exists and if it does, goes through it, trying to find Title, TitleID and MediaID
        if (File.Exists(Path.Combine(xenia.StartInfo.WorkingDirectory, "xenia.log")))
        {
            using (FileStream fs = new FileStream(Path.Combine(xenia.StartInfo.WorkingDirectory, "xenia.log"), FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (StreamReader sr = new StreamReader(fs))
            {
                // Goes through every line in xenia.log, trying to find lines that contain Title, TitleID and MediaID
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    switch (true)
                    {
                        case var _ when line.ToLower().Contains("title name"):
                        {
                            string[] split = line.Split(':');
                            if (gameTitle == "Not found")
                            {
                                gameTitle = split[1].TrimStart();
                            }
                            break;
                        }
                        case var _ when line.ToLower().Contains("title id"):
                        {
                            string[] split = line.Split(':');
                            game_id = split[1].TrimStart();
                            if (game_id == "Not found")
                            {
                                game_id = split[1].TrimStart();
                            }
                            break;
                        }
                        case var _ when line.ToLower().Contains("media id"):
                        {
                            string[] split = line.Split(':');
                            media_id = split[1].TrimStart();
                            break;
                        }
                    }
                }
            }
        }
        
        return (gameTitle, game_id, media_id);
    }
}