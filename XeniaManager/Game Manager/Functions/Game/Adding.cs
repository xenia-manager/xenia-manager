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
            // Adding the game to the library
            Log.Information($"Selected game: {game.Title} ({game.Id})");
            Game newGame = new Game();

            newGame.Title = game.Title.Replace(":", " -").Replace('\\', ' ').Replace('/', ' ');
            newGame.GameId = gameid;
            newGame.AlternativeIDs = game.AlternativeId;
            newGame.MediaId = mediaid;

            await GetGameCompatibility(newGame, gameid); // Tries to find the game on Xenia Master's compatibility page

            // If it fails, try alternative id's
            if (newGame.GameCompatibilityURL == null)
            {
                foreach (string titleid in game.AlternativeId)
                {
                    await GetGameCompatibility(newGame, titleid);
                    if (newGame.GameCompatibilityURL != null)
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
            Directory.CreateDirectory(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @$"GameData\{newGame.Title}\Artwork"));

            // Download Background
            Log.Information("Downloading background");
            Log.Information(gameInfo.Artwork.Background);
            if (gameInfo.Artwork.Background == null)
            {
                gameInfo.Artwork.Background = @"https://raw.githubusercontent.com/xenia-manager/Assets/refs/heads/v2/Artwork/00000000/background.jpg";
                Log.Information("Using default background since the game doesn't have it");
                await DownloadManager.GetGameIcon(gameInfo.Artwork.Background, Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @$"GameData\{newGame.Title}\Artwork\background.png"), MagickFormat.Png, 1280, 720);
            }
            else
            {
                if (await DownloadManager.CheckIfURLWorks(gameInfo.Artwork.Background, "image/"))
                {
                    Log.Information("Using background from Xbox Marketplace");
                    await DownloadManager.GetGameIcon(gameInfo.Artwork.Background, Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @$"GameData\{newGame.Title}\Artwork\background.png"), MagickFormat.Png, 1280, 720);
                }
                else
                {
                    Log.Information("Using default background as the last option");
                    await DownloadManager.GetGameIcon($@"https://raw.githubusercontent.com/xenia-manager/Assets/refs/heads/v2/Artwork/00000000/background.jpg", Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @$"GameData\{newGame.Title}\Artwork\background.png"), MagickFormat.Png, 1280, 720);
                }
            }
            newGame.Artwork.Background = @$"GameData\{newGame.Title}\Artwork\background.png";

            // Download Boxart
            Log.Information("Downloading boxart");
            Log.Information(gameInfo.Artwork.Boxart);
            if (gameInfo.Artwork.Boxart == null)
            {
                gameInfo.Artwork.Boxart = @"https://raw.githubusercontent.com/xenia-manager/Assets/refs/heads/v2/Artwork/00000000/boxart.jpg";
                Log.Information("Using default boxart since the game doesn't have boxart");
                await DownloadManager.GetGameIcon(gameInfo.Artwork.Boxart, Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @$"GameData\{newGame.Title}\Artwork\boxart.png"), MagickFormat.Png);
            }
            else
            {
                if (await DownloadManager.CheckIfURLWorks(gameInfo.Artwork.Boxart, "image/"))
                {
                    Log.Information("Using boxart from Xbox Marketplace");
                    await DownloadManager.GetGameIcon(gameInfo.Artwork.Boxart, Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @$"GameData\{newGame.Title}\Artwork\boxart.png"), MagickFormat.Png);
                }
                else if (await DownloadManager.CheckIfURLWorks($"https://raw.githubusercontent.com/xenia-manager/Assets/main/Assets/Marketplace/Boxart/{gameid}.jpg", "image/"))
                {
                    Log.Information("Using boxart from Xbox Marketplace backup");
                    await DownloadManager.GetGameIcon($"https://raw.githubusercontent.com/xenia-manager/Assets/main/Assets/Marketplace/Boxart/{gameid}.jpg", Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @$"GameData\{newGame.Title}\Artwork\boxart.png"), MagickFormat.Png);
                }
                else
                {
                    Log.Information("Using default boxart as the last option");
                    await DownloadManager.GetGameIcon($@"https://raw.githubusercontent.com/xenia-manager/Assets/refs/heads/v2/Artwork/00000000/boxart.jpg", Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @$"GameData\{newGame.Title}\Artwork\boxart.png"), MagickFormat.Png);
                }
            }
            newGame.Artwork.Boxart = @$"GameData\{newGame.Title}\Artwork\boxart.png";

            // Download icon for shortcut
            Log.Information("Downloading icon for shortcuts");
            if (gameInfo.Artwork.Icon == null)
            {
                gameInfo.Artwork.Icon = @"https://raw.githubusercontent.com/xenia-manager/Assets/refs/heads/v2/Artwork/00000000/icon.png";
                Log.Information("Using default disc image since the game doesn't have icon");
                await DownloadManager.GetGameIcon(gameInfo.Artwork.Icon, Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @$"GameData\{newGame.Title}\Artwork\icon.ico"), MagickFormat.Ico, 64, 64);
            }
            else
            {
                if (await DownloadManager.CheckIfURLWorks(gameInfo.Artwork.Icon, "image/"))
                {
                    Log.Information("Using game icon for shortcut icons from Xbox Marketplace");
                    await DownloadManager.GetGameIcon(gameInfo.Artwork.Icon, Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @$"GameData\{newGame.Title}\Artwork\icon.ico"), MagickFormat.Ico, 64, 64);
                }
                else if (await DownloadManager.CheckIfURLWorks($"https://raw.githubusercontent.com/xenia-manager/Assets/main/Assets/Marketplace/Icons/{gameid}.jpg", "image/"))
                {
                    Log.Information("Using game icon for shortcut icons from Xbox Marketplace backup");
                    await DownloadManager.GetGameIcon($"https://raw.githubusercontent.com/xenia-manager/Assets/main/Assets/Marketplace/Icons/{gameid}.jpg", Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @$"GameData\{newGame.Title}\Artwork\icon.ico"), MagickFormat.Ico, 64, 64);
                }
                else
                {
                    Log.Information("Using default disc image as the last option");
                    await DownloadManager.GetGameIcon($@"https://raw.githubusercontent.com/xenia-manager/Assets/refs/heads/v2/Artwork/00000000/icon.png", Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @$"GameData\{newGame.Title}\Artwork\icon.ico"), MagickFormat.Ico, 64, 64);
                }
            }
            newGame.Artwork.Icon = @$"GameData\{newGame.Title}\Artwork\icon.ico";
            Log.Information("Adding the game to the Xenia Manager");
            Games.Add(newGame);
            GameManager.Save();
        }
    }
}
