using System;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using System.Windows.Input;
using Microsoft.Extensions.DependencyModel;
using Newtonsoft.Json.Linq;


// Imported
using Serilog;
using XeniaManager.Database;
using XeniaManager.Downloader;

namespace XeniaManager
{
    public static partial class GameManager
    {
        /// <summary>
        /// Grabs the URL to the compatibility page of the game
        /// </summary>
        private static async Task GetGameCompatibilityPage(Game newGame, string gameid)
        {
            try
            {
                Log.Information($"Trying to find the compatibility page for {newGame.Title}");
                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("User-Agent", "Xenia Manager (https://github.com/xenia-manager/xenia-manager)");
                    client.DefaultRequestHeaders.Add("Accept", "application/vnd.github.v3+json");

                    HttpResponseMessage response = await client.GetAsync($"https://api.github.com/search/issues?q={gameid}%20in%3Atitle%20repo%3Axenia-project%2Fgame-compatibility");

                    if (response.IsSuccessStatusCode)
                    {
                        string json = await response.Content.ReadAsStringAsync();
                        JObject jsonObject = JObject.Parse(json);
                        JArray searchResults = (JArray)jsonObject["items"];
                        switch (searchResults.Count)
                        {
                            case 0:
                                Log.Information($"The compatibility page for {newGame.Title} isn't found");
                                newGame.GameCompatibilityURL = null;
                                break;
                            case 1:
                                Log.Information($"Found the compatibility page for {newGame.Title}");
                                Log.Information($"URL: {searchResults[0]["html_url"].ToString()}");
                                newGame.GameCompatibilityURL = searchResults[0]["html_url"].ToString();
                                break;
                            default:
                                Log.Information($"Multiple compatibility pages found");
                                Log.Information($"Trying to parse them");
                                foreach (JToken result in searchResults)
                                {
                                    string originalResultTitle = result["title"].ToString();
                                    string[] parts = originalResultTitle.Split(new string[] { " - " }, StringSplitOptions.None);
                                    string resultTitle = parts[1];
                                    if (resultTitle == newGame.Title)
                                    {
                                        Log.Information($"Found the compatibility page for {newGame.Title}");
                                        Log.Information($"URL: {result["html_url"].ToString()}");
                                        newGame.GameCompatibilityURL = result["html_url"].ToString();
                                        break;
                                    }
                                }
                                break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message + "\nFull Error:\n" + ex);
            }
        }

        /// <summary>
        /// Checks for the compatibility of the game with the emulator
        /// </summary>
        private static async Task GetCompatibilityRating(Game newGame)
        {
            try
            {
                Log.Information($"Trying to find the compatibility page for {newGame.Title}");
                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("User-Agent", "Xenia Manager (https://github.com/xenia-manager/xenia-manager)");
                    client.DefaultRequestHeaders.Add("Accept", "application/vnd.github.v3+json");

                    HttpResponseMessage response = await client.GetAsync(newGame.GameCompatibilityURL.Replace("https://github.com/", "https://api.github.com/repos/"));

                    if (!response.IsSuccessStatusCode)
                    {
                        return;
                    }

                    string json = await response.Content.ReadAsStringAsync();
                    JObject jsonObject = JObject.Parse(json);
                    JArray labels = (JArray)jsonObject["labels"];
                    if (labels.Count > 0)
                    {
                        bool foundCompatibility = false;
                        foreach (JObject label in labels)
                        {
                            string labelName = (string)label["name"];
                            if (labelName.Contains("state-"))
                            {
                                foundCompatibility = true;
                                string[] split = labelName.Split('-');
                                switch (split[1].ToLower())
                                {
                                    case "nothing":
                                    case "crash":
                                        newGame.CompatibilityRating = CompatibilityRating.Unknown;
                                        break;
                                    case "intro":
                                    case "hang":
                                    case "load":
                                    case "title":
                                    case "menus":
                                        newGame.CompatibilityRating = CompatibilityRating.Loads;
                                        break;
                                    case "gameplay":
                                        newGame.CompatibilityRating = CompatibilityRating.Gameplay;
                                        break;
                                    case "playable":
                                        newGame.CompatibilityRating = CompatibilityRating.Playable;
                                        break;
                                    default:
                                        newGame.CompatibilityRating = CompatibilityRating.Unknown;
                                        break;
                                }
                                Log.Information($"Current compatibility: {newGame.CompatibilityRating}");
                                break;
                            }
                            if (!foundCompatibility)
                            {
                                newGame.CompatibilityRating = CompatibilityRating.Unknown;
                            }
                        }
                    }
                    else
                    {
                        newGame.CompatibilityRating = CompatibilityRating.Unknown;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message + "\nFull Error:\n" + ex);
                newGame.CompatibilityRating = CompatibilityRating.Unknown;
            }
        }

        /// <summary>
        /// Function that adds selected game to the library
        /// </summary>
        /// <param name="game">Game that has been selected by the user</param>
        /// <param name="gameid">Game's TitleID</param>
        /// <param name="mediaid">Game's MediaID</param>
        public static async Task AddGameToLibrary(GameInfo game, string gameid, string? mediaid, string gamePath, EmulatorVersion xeniaVersion)
        {
            // Adding the game to the library
            Log.Information($"Selected game: {game.Title} ({game.Id})");
            Game newGame = new Game();

            newGame.Title = game.Title.Replace(":", " -").Replace('\\', ' ').Replace('/', ' ');
            newGame.GameId = gameid;
            newGame.MediaId = mediaid;

            await GetGameCompatibilityPage(newGame, gameid); // Tries to find the game on Xenia Master's compatibility page

            // If it fails, try alternative id's
            if (newGame.GameCompatibilityURL == null)
            {
                foreach (string titleid in game.AlternativeId)
                {
                    await GetGameCompatibilityPage(newGame, titleid);
                    if (newGame.GameCompatibilityURL != null)
                    {
                        break;
                    }
                }
            }

            // Check if game has compatibility page
            if (newGame.GameCompatibilityURL != null)
            {
                await GetCompatibilityRating(newGame);
            }
            else
            {
                newGame.CompatibilityRating = CompatibilityRating.Unknown;
            }

            // Check for duplicates
            if (InstalledGames.Any(game => game.Title == newGame.Title))
            {
                Log.Information("This game title is already in use");
                Log.Information("Adding it as a duplicate");
                int counter = 1;
                string OriginalGameTitle = newGame.Title;
                while (InstalledGames.Any(game => game.Title == newGame.Title))
                {
                    newGame.Title = $"{OriginalGameTitle} ({counter})";
                    counter++;
                }
            }

            newGame.FileLocations.GameFilePath = gamePath;
            // Grabbing the correct emulator
            EmulatorInfo emulatorInfo = xeniaVersion switch
            {
                EmulatorVersion.Stable => ConfigurationManager.AppConfig.XeniaStable,
                EmulatorVersion.Canary => ConfigurationManager.AppConfig.XeniaCanary,
                EmulatorVersion.Netplay => ConfigurationManager.AppConfig.XeniaNetplay,
                _ => throw new InvalidOperationException("Unexpected build type")
            };
            Log.Information($"Creating a new configuration file for {newGame.Title}");
            if (File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, emulatorInfo.ConfigurationFileLocation)))
            {
                File.Copy(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, emulatorInfo.ConfigurationFileLocation), Path.Combine(AppDomain.CurrentDomain.BaseDirectory, emulatorInfo.EmulatorLocation, $@"config\{newGame.Title}.config.toml"), true);
            }
            newGame.FileLocations.ConfigFilePath = Path.Combine(emulatorInfo.EmulatorLocation, $@"config\{newGame.Title}.config.toml");
            newGame.EmulatorVersion = xeniaVersion;

            // Fetch detailed game info for artwork
            XboxMarketplaceGameInfo gameInfo = await DownloadManager.DownloadGameInfo(gameid);
            if (gameInfo == null)
            {
                Log.Error("Couldn't fetch game information");
                return;
            }

            // Download Artwork
            // Download Boxart
            if (!Directory.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @$"Artwork\{newGame.Title}")))
            {
                Directory.CreateDirectory(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @$"Artwork\{newGame.Title}"));
            }
            Log.Information("Downloading boxart");
            Log.Information(gameInfo.Artwork.Boxart);
            if (gameInfo.Artwork.Boxart == null)
            {
                gameInfo.Artwork.Boxart = @"https://raw.githubusercontent.com/xenia-manager/Assets/main/Assets/Boxart.jpg";
                Log.Information("Using default boxart since the game doesn't have boxart");
                await DownloadManager.GetGameIcon(gameInfo.Artwork.Boxart, Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @$"Artwork\{newGame.Title}\boxart.ico"));
            }
            else
            {
                if (await DownloadManager.CheckIfURLWorks(gameInfo.Artwork.Boxart, "image/"))
                {
                    Log.Information("Using boxart from Xbox Marketplace");
                    await DownloadManager.GetGameIcon(gameInfo.Artwork.Boxart, Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @$"Artwork\{newGame.Title}\boxart.ico"));
                }
                else if (await DownloadManager.CheckIfURLWorks($"https://raw.githubusercontent.com/xenia-manager/Assets/main/Assets/Marketplace/Boxart/{gameid}.jpg", "image/"))
                {
                    Log.Information("Using boxart from Xbox Marketplace backup");
                    await DownloadManager.GetGameIcon($"https://raw.githubusercontent.com/xenia-manager/Assets/main/Assets/Marketplace/Boxart/{gameid}.jpg", Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @$"Artwork\{newGame.Title}\boxart.ico"));
                }
                else
                {
                    Log.Information("Using default boxart as the last option");
                    await DownloadManager.GetGameIcon($@"https://raw.githubusercontent.com/xenia-manager/Assets/main/Assets/Boxart.jpg", Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @$"Artwork\{newGame.Title}\boxart.ico"));
                }
            }
            newGame.Artwork.Boxart = @$"Artwork\{newGame.Title}\boxart.ico";

            // Download icon for shortcut
            Log.Information("Downloading icon for shortcuts");
            if (gameInfo.Artwork.Icon == null)
            {
                gameInfo.Artwork.Icon = @"https://raw.githubusercontent.com/xenia-manager/Assets/main/Assets/Disc.png";
                Log.Information("Using default disc image since the game doesn't have icon");
                await DownloadManager.GetGameIcon(gameInfo.Artwork.Icon, Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @$"Artwork\{newGame.Title}\icon.ico"), 64, 64);
            }
            else
            {
                if (await DownloadManager.CheckIfURLWorks(gameInfo.Artwork.Icon, "image/"))
                {
                    Log.Information("Using game icon for shortcut icons from Xbox Marketplace");
                    await DownloadManager.GetGameIcon(gameInfo.Artwork.Icon, Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @$"Artwork\{newGame.Title}\icon.ico"), 64, 64);
                }
                else if (await DownloadManager.CheckIfURLWorks($"https://raw.githubusercontent.com/xenia-manager/Assets/main/Assets/Marketplace/Icons/{gameid}.jpg", "image/"))
                {
                    Log.Information("Using game icon for shortcut icons from Xbox Marketplace backup");
                    await DownloadManager.GetGameIcon($"https://raw.githubusercontent.com/xenia-manager/Assets/main/Assets/Marketplace/Icons/{gameid}.jpg", Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @$"Icons\{newGame.Title}\icon.ico"), 64, 64);
                }
                else
                {
                    Log.Information("Using default disc image as the last option");
                    await DownloadManager.GetGameIcon($@"https://raw.githubusercontent.com/xenia-manager/Assets/main/Assets/Disc.png", Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @$"Icons\{newGame.Title}\icon.ico"), 64, 64);
                }
            }
            newGame.Artwork.Icon = @$"Icons\{newGame.Title}\icon.ico";
            Log.Information("Adding the game to the Xenia Manager");
            InstalledGames.Add(newGame);
            GameManager.SaveGames();
        }
    }
}
