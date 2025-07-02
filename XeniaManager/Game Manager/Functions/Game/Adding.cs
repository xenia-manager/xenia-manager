using System;

// Imported
using ImageMagick;
using Serilog;
using XeniaManager.Database;
using XeniaManager.Downloader;

namespace XeniaManager
{
    public static partial class GameManager
    {
        /// <summary>
        /// Function that adds selected game to the library
        /// </summary>
        /// <param name="game">Game that has been selected by the user</param>
        /// <param name="gameid">Game's TitleID</param>
        /// <param name="mediaid">Game's MediaID</param>
        public static async Task AddGameToLibrary(GameInfo game, string gameid, string? mediaid, string gamePath, EmulatorVersion xeniaVersion)
        {
            // Fetch detailed game info for artwork
            XboxMarketplaceGameInfo gameInfo = await DownloadManager.DownloadGameInfo(gameid);
            if (gameInfo == null)
            {
                Log.Error("Couldn't fetch game information");
                return;
            }

            // Adding the game to the library
            Log.Information($"Selected game: {gameInfo.Title.Full} ({game.Id})");
            Game newGame = new Game();

            newGame.Title = gameInfo.Title.Full.Replace(":", " -").Replace('\\', ' ').Replace('/', ' ');
            newGame.GameId = gameid;
            newGame.AlternativeIDs = game.AlternativeId;
            newGame.MediaId = mediaid;

            await GetGameCompatibility(newGame, gameid); // Tries to find the game on Xenia Canary's compatibility page

            // If it fails, try alternative id's
            if (newGame.GameCompatibilityUrl == null)
            {
                foreach (string titleid in game.AlternativeId)
                {
                    await GetGameCompatibility(newGame, titleid);
                    if (newGame.GameCompatibilityUrl != null)
                    {
                        break;
                    }
                }
            }

            // Check for duplicates
            if (Games.Any(game => game.Title == newGame.Title))
            {
                Log.Information("This game title is already in use");
                Log.Information("Adding it as a duplicate");
                int counter = 1;
                string OriginalGameTitle = gameInfo.Title.Full.Replace(":", " -").Replace('\\', ' ').Replace('/', ' ');
                while (Games.Any(game => game.Title == newGame.Title))
                {
                    newGame.Title = $"{OriginalGameTitle} ({counter})";
                    counter++;
                }
            }

            newGame.FileLocations.GameFilePath = gamePath;
            // Grabbing the correct emulator
            EmulatorInfo emulatorInfo = xeniaVersion switch
            {
                EmulatorVersion.Canary => ConfigurationManager.AppConfig.XeniaCanary,
                EmulatorVersion.Mousehook => ConfigurationManager.AppConfig.XeniaMousehook,
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

            // Download Artwork
            Directory.CreateDirectory(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @$"GameData\{newGame.Title}\Artwork"));

            // Download Background
            Log.Information("Downloading background");
            if (gameInfo.Artwork.Background == null)
            {
                Log.Information("Using default background since the game doesn't have it");
                UseLocalArtwork("XeniaManager.Assets.Default_Artwork.Background.jpg", Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GameData", newGame.Title, "Artwork", "background.png"), MagickFormat.Png, 1280, 720);
            }
            else
            {
                if (await DownloadManager.CheckIfUrlWorks(gameInfo.Artwork.Background, "image/"))
                {
                    Log.Information("Using background from Xbox Marketplace");
                    await DownloadManager.GetGameIcon(gameInfo.Artwork.Background, Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @$"GameData\{newGame.Title}\Artwork\background.png"), MagickFormat.Png, 1280, 720);
                }
                else if (await DownloadManager.CheckIfUrlWorks($"https://raw.githubusercontent.com/xenia-manager/Assets/main/Artwork/{gameid}/background.jpg", "image/"))
                {
                    Log.Information("Using background from Xbox Marketplace backup");
                    await DownloadManager.GetGameIcon($"https://raw.githubusercontent.com/xenia-manager/Assets/main/Artwork/{gameid}/background.jpg", Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @$"GameData\{newGame.Title}\Artwork\background.png"), MagickFormat.Png, 1280, 720);
                }
                else
                {
                    // Using template background
                    Log.Information("Using default background as the last option");
                    UseLocalArtwork("XeniaManager.Assets.Default_Artwork.Background.jpg", Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GameData", newGame.Title, "Artwork","background.png"), MagickFormat.Png, 1280, 720);
                }
            }
            newGame.Artwork.Background = @$"GameData\{newGame.Title}\Artwork\background.png";

            // Download Boxart
            Log.Information("Downloading boxart");
            if (gameInfo.Artwork.Boxart == null)
            {
                Log.Information("Using default boxart since the game doesn't have boxart");
                UseLocalArtwork("XeniaManager.Assets.Default_Artwork.Boxart.jpg", Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GameData", newGame.Title, "Artwork", "boxart.png"), MagickFormat.Png);
            }
            else
            {
                if (await DownloadManager.CheckIfUrlWorks(gameInfo.Artwork.Boxart, "image/"))
                {
                    Log.Information("Using boxart from Xbox Marketplace");
                    await DownloadManager.GetGameIcon(gameInfo.Artwork.Boxart, Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @$"GameData\{newGame.Title}\Artwork\boxart.png"), MagickFormat.Png);
                }
                else if (await DownloadManager.CheckIfUrlWorks($"https://raw.githubusercontent.com/xenia-manager/Assets/main/Artwork/{gameid}/boxart.jpg", "image/"))
                {
                    Log.Information("Using boxart from Xbox Marketplace backup");
                    await DownloadManager.GetGameIcon($"https://raw.githubusercontent.com/xenia-manager/Assets/main/Artwork/{gameid}/boxart.jpg", Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @$"GameData\{newGame.Title}\Artwork\boxart.png"), MagickFormat.Png);
                }
                else
                {
                    // Using template boxart
                    Log.Information("Using default boxart as the last option");
                    UseLocalArtwork("XeniaManager.Assets.Default_Artwork.Boxart.jpg", Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GameData", newGame.Title, "Artwork", "boxart.png"), MagickFormat.Png);
                }
            }
            newGame.Artwork.Boxart = @$"GameData\{newGame.Title}\Artwork\boxart.png";

            // Download icon for shortcut
            Log.Information("Downloading icon for shortcuts");
            if (gameInfo.Artwork.Icon == null)
            {
                Log.Information("Using default disc image since the game doesn't have icon");
                UseLocalArtwork("XeniaManager.Assets.Default_Artwork.Icon.png", Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GameData", newGame.Title, "Artwork", "icon.ico"), MagickFormat.Ico, 64, 64);
            }
            else
            {
                if (await DownloadManager.CheckIfUrlWorks(gameInfo.Artwork.Icon, "image/"))
                {
                    Log.Information("Using game icon for shortcut icons from Xbox Marketplace");
                    await DownloadManager.GetGameIcon(gameInfo.Artwork.Icon, Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @$"GameData\{newGame.Title}\Artwork\icon.ico"), MagickFormat.Ico, 64, 64);
                }
                else if (await DownloadManager.CheckIfUrlWorks($"https://raw.githubusercontent.com/xenia-manager/Assets/main/Artwork/{gameid}/icon.png", "image/"))
                {
                    Log.Information("Using game icon for shortcut icons from Xbox Marketplace backup");
                    await DownloadManager.GetGameIcon($"https://raw.githubusercontent.com/xenia-manager/Assets/main/Artwork/{gameid}/icon.png", Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @$"GameData\{newGame.Title}\Artwork\icon.ico"), MagickFormat.Ico, 64, 64);
                }
                else
                {
                    // Using template icon
                    UseLocalArtwork("XeniaManager.Assets.Default_Artwork.Icon.png", Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GameData", newGame.Title, "Artwork", "icon.ico"), MagickFormat.Ico, 64, 64);
                }
            }
            newGame.Artwork.Icon = @$"GameData\{newGame.Title}\Artwork\icon.ico";
            Log.Information("Adding the game to the Xenia Manager");
            Games.Add(newGame);
            GameManager.Save();
        }

        /// <summary>
        /// Function that adds unknown game to the library by using default artwork
        /// </summary>
        /// <param name="game">Game that has been selected by the user</param>
        /// <param name="gameid">Game's TitleID</param>
        /// <param name="mediaid">Game's MediaID</param>
        public static async Task AddUnknownGameToLibrary(string gameTitle, string gameid, string? mediaid, string gamePath, EmulatorVersion xeniaVersion)
        {
            // Adding the game to the library
            Log.Information($"Selected game: {gameTitle} ({gameid})");
            Game newGame = new Game();

            newGame.Title = gameTitle.Replace(":", " -").Replace('\\', ' ').Replace('/', ' ');
            newGame.GameId = gameid;
            newGame.MediaId = mediaid;

            await GetGameCompatibility(newGame, gameid); // Tries to find the game on Xenia Canary's compatibility page

            // Check for duplicates
            if (Games.Any(game => game.Title == newGame.Title))
            {
                Log.Information("This game title is already in use");
                Log.Information("Adding it as a duplicate");
                int counter = 1;
                string OriginalGameTitle = newGame.Title;
                while (Games.Any(game => game.Title == newGame.Title))
                {
                    newGame.Title = $"{OriginalGameTitle} ({counter})";
                    counter++;
                }
            }

            newGame.FileLocations.GameFilePath = gamePath;
            // Grabbing the correct emulator
            EmulatorInfo emulatorInfo = xeniaVersion switch
            {
                EmulatorVersion.Canary => ConfigurationManager.AppConfig.XeniaCanary,
                EmulatorVersion.Mousehook => ConfigurationManager.AppConfig.XeniaMousehook,
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

            // Download Artwork
            Directory.CreateDirectory(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @$"GameData\{newGame.Title}\Artwork"));

            // Using template background
            UseLocalArtwork("XeniaManager.Assets.Default_Artwork.Background.jpg", Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GameData", newGame.Title, "Artwork","background.png"), MagickFormat.Png, 1280, 720);
            newGame.Artwork.Background = @$"GameData\{newGame.Title}\Artwork\background.png";

            // Using template boxart
            UseLocalArtwork("XeniaManager.Assets.Default_Artwork.Boxart.jpg", Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GameData", newGame.Title, "Artwork", "boxart.png"), MagickFormat.Png);
            newGame.Artwork.Boxart = @$"GameData\{newGame.Title}\Artwork\boxart.png";

            // Using template icon
            UseLocalArtwork("XeniaManager.Assets.Default_Artwork.Icon.png", Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GameData", newGame.Title, "Artwork", "icon.ico"), MagickFormat.Ico, 64, 64);
            newGame.Artwork.Icon = @$"GameData\{newGame.Title}\Artwork\icon.ico";

            Log.Information("Adding the game to the Xenia Manager");
            Games.Add(newGame);
            GameManager.Save();
        }
    }
}
