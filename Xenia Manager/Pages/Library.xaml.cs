using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Text.RegularExpressions;
using System.Security.Cryptography;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;

// Imported
using Microsoft.Win32;
using Newtonsoft.Json;
using Serilog;
using Xenia_Manager.Classes;
using Xenia_Manager.Windows;

namespace Xenia_Manager.Pages
{
    /// <summary>
    /// Interaction logic for Library.xaml
    /// </summary>
    public partial class Library : Page
    {
        // Holds all of the installed games
        public ObservableCollection<InstalledGame> Games = new ObservableCollection<InstalledGame>();

        public Library()
        {
            InitializeComponent();
            LoadGamesStartup();
        }

        /// <summary>
        /// Loads all of the games when this page is loaded
        /// </summary>
        private async void LoadGamesStartup()
        {
            try
            {
                if (System.IO.File.Exists(AppDomain.CurrentDomain.BaseDirectory + @"installedGames.json"))
                {
                    wrapPanel.Children.Clear();
                    string JSON = System.IO.File.ReadAllText(AppDomain.CurrentDomain.BaseDirectory + @"installedGames.json");
                    Games = JsonConvert.DeserializeObject<ObservableCollection<InstalledGame>>((JSON));
                    await LoadGamesIntoUI();
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message + "\nFull Error:\n" + ex);
                MessageBox.Show(ex.Message);
            }
        }

        /// <summary>
        /// Used to get game title from Xenia Window Title
        /// </summary>
        /// <param name="selectedFilePath">Where the selected game file is (.iso etc.)</param>
        /// /// <param name="XeniaVersion">What version of Xenia will be used by the game</param>
        private async Task GetGameTitle(string selectedFilePath, string XeniaVersion)
        {
            try
            {
                Log.Information("Launching game with Xenia to find the name of the game");
                Process xenia = new Process();
                if (XeniaVersion == "Stable")
                {
                    xenia.StartInfo.FileName = App.appConfiguration.XeniaStable.EmulatorLocation + @"xenia.exe";
                }
                else
                {
                    xenia.StartInfo.FileName = App.appConfiguration.XeniaCanary.EmulatorLocation + @"xenia_canary.exe";
                }
                xenia.StartInfo.Arguments = $@"""{selectedFilePath}""";
                xenia.Start();
                xenia.WaitForInputIdle();

                string gameTitle = "";
                string game_id = "";

                Process process = Process.GetProcessById(xenia.Id);
                Log.Information("Trying to find the game title from Xenia Window Title");
                int NumberOfTries = 0;
                while (gameTitle == "" || gameTitle == "Not found")
                {
                    Regex titleRegex = new Regex(@"\]\s+([^<]+)\s+<");
                    Regex idRegex = new Regex(@"\[(\w{8}) v[\d\.]+\]");

                    Match gameNameMatch = titleRegex.Match(process.MainWindowTitle);
                    gameTitle = gameNameMatch.Success ? gameNameMatch.Groups[1].Value : "Not found";
                    Match versionMatch = idRegex.Match(process.MainWindowTitle);
                    game_id = versionMatch.Success ? versionMatch.Groups[1].Value : "Not found";

                    process = Process.GetProcessById(xenia.Id);
                    NumberOfTries++;
                    if (NumberOfTries > 100)
                    {
                        gameTitle = "Not found";
                        game_id = "Not found";
                        break;
                    }
                    await Task.Delay(100);
                }

                xenia.Kill();

                Log.Information("Game found");
                Log.Information("Game Title: " + gameTitle);
                Log.Information("Game ID: " + game_id);

                EmulatorInfo emulator = new EmulatorInfo();
                if (XeniaVersion == "Stable")
                {
                    emulator = App.appConfiguration.XeniaStable;
                    SelectGame sd = new SelectGame(this, gameTitle, game_id, selectedFilePath, XeniaVersion, emulator);
                    sd.Show();
                    await sd.WaitForCloseAsync();
                }
                else
                {
                    emulator = App.appConfiguration.XeniaCanary;
                    SelectGame sd = new SelectGame(this, gameTitle, game_id, selectedFilePath, XeniaVersion, emulator);
                    sd.Show();
                    await sd.WaitForCloseAsync();
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message + "\nFull Error:\n" + ex);
                MessageBox.Show(ex.Message);
                return;
            }
        }

        /// <summary>
        /// Used to load games in general, mostly after importing another game or removing
        /// </summary>
        private async Task LoadGames()
        {
            try
            {
                wrapPanel.Children.Clear();
                await LoadGamesIntoUI();
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message + "\nFull Error:\n" + ex);
                MessageBox.Show(ex.Message);
                return;
            }
        }

        /// <summary>
        /// Compares arrays between game icon bytes and cached icon bytes
        /// </summary>
        /// <param name="OriginalIconBytes">Array of bytes of the original icon</param>
        /// <param name="CachedIconBytes">Array of bytes of the cached icon</param>
        /// <returns>True if they match, otherwise false</returns>
        public static bool ByteArraysAreEqual(byte[] OriginalIconBytes, byte[] CachedIconBytes)
        {
            // Compare lengths
            if (OriginalIconBytes.Length != CachedIconBytes.Length)
            {
                return false;
            }

            // Compare each byte
            for (int i = 0; i < OriginalIconBytes.Length; i++)
            {
                if (OriginalIconBytes[i] != CachedIconBytes[i])
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Tries to find cached icon of the game
        /// </summary>
        /// <param name="iconFilePath">Location to the original icon</param>
        /// <param name="directoryPath">Location of the cached icons directory</param>
        /// <returns>Cached Icon file path or null</returns>
        public static string FindFirstIdenticalFile(string iconFilePath, string directoryPath)
        {
            // Read the icon file once
            byte[] iconFileBytes = File.ReadAllBytes(iconFilePath);

            // Compute hash for the icon file
            byte[] iconFileHash;
            using (var md5 = MD5.Create())
            {
                iconFileHash = md5.ComputeHash(iconFileBytes);
            }

            // Get all files in the directory
            string[] files = Directory.GetFiles(directoryPath);

            foreach (var filePath in files)
            {
                // Skip comparing the icon file against itself
                if (string.Equals(filePath, iconFilePath, StringComparison.OrdinalIgnoreCase))
                    continue;

                // Read the current file
                byte[] currentFileBytes = File.ReadAllBytes(filePath);

                if (ByteArraysAreEqual(iconFileBytes, currentFileBytes))
                {
                    return filePath;
                }
            }

            // If no identical file is found, return null or handle as needed
            return null;
        }

        /// <summary>
        /// Launches the game
        /// </summary>
        /// <param name="game">The game user wants to launch</param>
        /// <param name="windowedMode">Check if he wants it to be in Windowed Mode</param>
        private async Task LaunchGame(InstalledGame game, bool windowedMode = false)
        {
            Log.Information($"Launching {game.Title}");
            Process xenia = new Process();

            // Checking what emulator the game uses
            if (game.EmulatorVersion == "Canary")
            {
                xenia.StartInfo.FileName = App.appConfiguration.XeniaCanary.EmulatorLocation + @"xenia_canary.exe";
            }
            else if (game.EmulatorVersion == "Stable")
            {
                xenia.StartInfo.FileName = App.appConfiguration.XeniaStable.EmulatorLocation + @"xenia.exe";
            }

            // Adding default launch arguments
            xenia.StartInfo.Arguments = $@"""{game.GameFilePath}"" --config ""{game.ConfigFilePath}""";

            // Checking if the game will be run in windowed mode
            if (windowedMode)
            {
                xenia.StartInfo.Arguments += " --fullscreen=false";
            }
            // Starting the emulator
            xenia.Start();
            Log.Information("Emulator started");
            await xenia.WaitForExitAsync();
            Log.Information("Emulator closed");
        }

        /// <summary>
        /// Adds all files to the zip
        /// </summary>
        /// <param name="archive">Instance of ZipArchive used for zipping</param>
        /// <param name="sourceDir">Source directory</param>
        /// <param name="basePath">Base Path (gameid/00000001)</param>
        public static void AddDirectoryToZip(ZipArchive archive, string sourceDir, string basePath)
        {
            foreach (string filePath in Directory.GetFiles(sourceDir, "*.*", SearchOption.AllDirectories))
            {
                string entryName = Path.Combine(basePath, filePath.Substring(sourceDir.Length + 1).Replace('\\', '/'));
                archive.CreateEntryFromFile(filePath, entryName);
            }
        }

        /// <summary>
        /// Function to handle the game transfer between emulators
        /// </summary>
        /// <param name="game">Game to tranasfer</param>
        /// <param name="SourceVersion">Original Xenia version that the game uses</param>
        /// <param name="TargetVersion">New Xenia version that the game will use</param>
        /// <param name="sourceEmulatorLocation">Original Xenia version location</param>
        /// <param name="targetEmulatorLocation">New Xenia version location</param>
        /// <param name="defaultConfigFileLocation">Location to the default configuration file of the new Xenia version</param>
        private async Task TransferGame(InstalledGame game, string SourceVersion, string TargetVersion, string sourceEmulatorLocation, string targetEmulatorLocation, string defaultConfigFileLocation)
        {
            Log.Information($"Moving the game to Xenia {TargetVersion}");
            game.EmulatorVersion = TargetVersion; // Set the emulator version

            game.ConfigFilePath = @$"{targetEmulatorLocation}config\{game.Title}.config.toml";
            if (!File.Exists(game.ConfigFilePath))
            {
                Log.Information("Game configuration file not found");
                Log.Information("Creating a new configuration file from the default one");
                File.Copy(defaultConfigFileLocation, targetEmulatorLocation + $@"config\{game.Title}.config.toml", true);
            }

            // Checking if there is some content installed that should be copied over
            if (Directory.Exists(@$"{sourceEmulatorLocation}content\{game.GameId}"))
            {
                Log.Information($"Copying all of the installed content and saves from Xenia {SourceVersion} to Xenia {TargetVersion}");
                // Create all of the necessary directories for content copy
                foreach (string dirPath in Directory.GetDirectories($@"{sourceEmulatorLocation}content\{game.GameId}", "*", SearchOption.AllDirectories))
                {
                    Directory.CreateDirectory(dirPath.Replace($@"{sourceEmulatorLocation}content\{game.GameId}", $@"{targetEmulatorLocation}content\{game.GameId}"));
                }

                // Copy all the files
                foreach (string newPath in Directory.GetFiles($@"{sourceEmulatorLocation}content\{game.GameId}", "*.*", SearchOption.AllDirectories))
                {
                    File.Copy(newPath, newPath.Replace($@"{sourceEmulatorLocation}content\{game.GameId}", $@"{targetEmulatorLocation}content\{game.GameId}"), true);
                }
            }
            else
            {
                Log.Information("No installed content or saves found");
            }

            Log.Information("Reloading the UI and saving changes");

            // Reload UI and save changes
            await LoadGames();
            await SaveGames();
            MessageBox.Show($"{game.Title} transfer is complete. Now the game will use Xenia {TargetVersion}.");
        }

        /// <summary>
        /// Loads the games into the Wrappanel
        /// </summary>
        private async Task LoadGamesIntoUI()
        {
            try
            {
                // Check if there are any games installed
                if (Games != null && Games.Count > 0)
                {
                    // Sort the games by name
                    IOrderedEnumerable<InstalledGame> orderedGames = Games.OrderBy(game => game.Title);
                    foreach (InstalledGame game in orderedGames)
                    {
                        // Create a new button for the game
                        Button button = new Button();
                        Log.Information($"Adding {game.Title} to the Library");

                        Log.Information("Checking if the game icon has already been cached");
                        BitmapImage iconImage = new BitmapImage();
                        string identicalFilePath = FindFirstIdenticalFile(game.IconFilePath, $@"{AppDomain.CurrentDomain.BaseDirectory}Icons\Cache\");
                        if (identicalFilePath != null)
                        {
                            Log.Information("Icon has already been cached");
                            Log.Information("Loading cached icon");
                            iconImage = new BitmapImage(new Uri(identicalFilePath));
                        }
                        else
                        {
                            // Creating a cached image of the icon
                            Log.Information("Couldn't find cached version of the game icon");
                            Log.Information("Creating new cached icon for the game");
                            string randomIconName;
                            while (true)
                            {
                                randomIconName = Path.GetRandomFileName().Replace(".", "").Substring(0, 8);
                                if (File.Exists($@"{AppDomain.CurrentDomain.BaseDirectory}Icons\Cache\{randomIconName}.ico"))
                                {
                                    randomIconName = Path.GetRandomFileName().Replace(".", "").Substring(0, 8);
                                }
                                else
                                {
                                    File.Copy(game.IconFilePath, $@"{AppDomain.CurrentDomain.BaseDirectory}Icons\Cache\{randomIconName}.ico", true);
                                    break;
                                }
                            }
                            Log.Information($"Cached icon name: {randomIconName}.ico");
                            iconImage = new BitmapImage(new Uri($@"{AppDomain.CurrentDomain.BaseDirectory}Icons\Cache\{randomIconName}.ico"));
                        }

                        // Box art of the game
                        Image image = new Image
                        {
                            Source = iconImage,
                            Stretch = Stretch.UniformToFill
                        };

                        RectangleGeometry clipGeometry = new RectangleGeometry
                        {
                            Rect = new Rect(0, 0, 150, 207),
                            RadiusX = 3,
                            RadiusY = 3 
                        };

                        // Black border for rounded edges
                        Border border = new Border
                        {
                            CornerRadius = new CornerRadius(10),
                            Background = Brushes.Black,
                            Child = image,
                            Clip = clipGeometry
                        };

                        button.Content = border;

                        // Animations
                        MainWindow mainWindow = Application.Current.MainWindow as MainWindow;
                        DoubleAnimation fadeOutAnimation = new DoubleAnimation(1, 0, TimeSpan.FromSeconds(0.15));
                        DoubleAnimation fadeInAnimation = new DoubleAnimation(0, 1, TimeSpan.FromSeconds(0.15));

                        // Check for when animation is completed
                        TaskCompletionSource<bool> animationCompleted = new TaskCompletionSource<bool>();

                        // When user clicks on the game, launch the game
                        button.Click += async (sender, e) =>
                        {
                            // Run the animation
                            animationCompleted = new TaskCompletionSource<bool>();
                            fadeOutAnimation.Completed += (s, e) =>
                            {
                                mainWindow.Visibility = Visibility.Collapsed; // Collapse the main window
                                animationCompleted.SetResult(true); // Signal that the animation has completed
                            };
                            mainWindow.BeginAnimation(Window.OpacityProperty, fadeOutAnimation);
                            await animationCompleted.Task; // Wait for animation to be completed

                            // Launch the game
                            await LaunchGame(game);

                            // When the user closes the game/emulator, reload the UI and show the main window again
                            await LoadGames();
                            mainWindow.Visibility = Visibility.Visible;
                            mainWindow.BeginAnimation(Window.OpacityProperty, fadeInAnimation);
                        };

                        button.Cursor = Cursors.Hand; // Change cursor to hand cursor
                        button.Style = (Style)FindResource("GameCoverButtons"); // Styling of the game button
                        button.ToolTip = game.Title; // Hovering shows game name
                        
                        wrapPanel.Children.Add(button); // Add the game to the Warp Panel

                        // When button loads
                        button.Loaded += (sender, e) =>
                        {
                            // Button width and height
                            button.Width = 150;
                            button.Height = 207;
                            button.Margin = new Thickness(5);

                            // Context Menu
                            ContextMenu contextMenu = new ContextMenu();

                            // Adding options to ContextMenu
                            // Windowed mode
                            MenuItem WindowedMode = new MenuItem
                            {
                                Header = "Play game in windowed mode", // Text that shows in the context menu
                                ToolTip = "Opens the game in the windowed mode", // Hovering showing more detail about this option
                            };

                            // Action when this option is pressed
                            WindowedMode.Click += async (sender, e) =>
                            {
                                // Run the animation
                                animationCompleted = new TaskCompletionSource<bool>();
                                fadeOutAnimation.Completed += (s, e) =>
                                {
                                    mainWindow.Visibility = Visibility.Collapsed; // Collapse the main window
                                    animationCompleted.SetResult(true); // Signal that the animation has completed
                                };
                                mainWindow.BeginAnimation(Window.OpacityProperty, fadeOutAnimation);
                                await animationCompleted.Task; // Wait for animation to be completed

                                // Launch the game
                                await LaunchGame(game, true);

                                // When the user closes the game/emulator, reload the UI and show the main window again
                                await LoadGames();
                                mainWindow.Visibility = Visibility.Visible;
                                mainWindow.BeginAnimation(Window.OpacityProperty, fadeInAnimation);
                            };
                            contextMenu.Items.Add(WindowedMode); // Add the item to the ContextMenu

                            // Create a Desktop Shortcut
                            MenuItem CreateShortcut = new MenuItem
                            {
                                Header = "Create shortcut on desktop", // Text that shows in the context menu
                            };

                            // Action when this option is pressed
                            CreateShortcut.Click += (sender, e) =>
                            {
                                if (game.EmulatorVersion == "Stable")
                                {
                                    ShortcutCreator.CreateShortcutOnDesktop(game.Title, Path.Combine(App.appConfiguration.XeniaStable.EmulatorLocation), App.appConfiguration.XeniaStable.EmulatorLocation, $@"""{game.GameFilePath}"" --config ""{game.ConfigFilePath}""", game.IconFilePath);
                                }
                                else if (game.EmulatorVersion == "Canary")
                                {
                                    ShortcutCreator.CreateShortcutOnDesktop(game.Title, Path.Combine(App.appConfiguration.XeniaCanary.EmulatorLocation), App.appConfiguration.XeniaCanary.EmulatorLocation, $@"""{game.GameFilePath}"" --config ""{game.ConfigFilePath}""", game.IconFilePath);
                                }
                            };
                            contextMenu.Items.Add(CreateShortcut); // Add the item to the ContextMenu

                            // Open compatibility page
                            if (game.GameCompatibilityURL != null)
                            {
                                MenuItem OpenCompatibilityPage = new MenuItem
                                {
                                    Header = "Open Compatibility Page", // Text that shows in the context menu
                                };

                                // Action when this option is pressed
                                OpenCompatibilityPage.Click += (sender, e) =>
                                {
                                    ProcessStartInfo compatibilityPageURL = new ProcessStartInfo(game.GameCompatibilityURL) { UseShellExecute = true };
                                    Process.Start(compatibilityPageURL);
                                };
                                contextMenu.Items.Add(OpenCompatibilityPage); // Add the item to the ContextMenu
                            }

                            // Remove game from Xenia Manager
                            MenuItem RemoveGame = new MenuItem
                            {
                                Header = "Remove game", // Text that shows in the context menu
                                ToolTip = "Removes the game from Xenia Manager", // Hovering showing more detail about this option
                            };

                            // Action when this option is pressed
                            RemoveGame.Click += async (sender, e) => 
                            {
                                MessageBoxResult result = MessageBox.Show($"Do you want to remove {game.Title}?", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question);
                                if (result == MessageBoxResult.Yes)
                                {
                                    Log.Information($"Removing {game.Title}");

                                    // Remove game patch
                                    if (System.IO.File.Exists(game.PatchFilePath))
                                    {
                                        System.IO.File.Delete(game.PatchFilePath);
                                        Log.Information($"Deleted {game.Title} patch");
                                    }

                                    // Remove game configuration file
                                    if (System.IO.File.Exists(game.ConfigFilePath))
                                    {
                                        System.IO.File.Delete(game.ConfigFilePath);
                                        Log.Information($"Deleted {game.Title} configuration");
                                    }

                                    // Remove game icon
                                    if (System.IO.File.Exists(game.IconFilePath))
                                    {
                                        System.IO.File.Delete(game.IconFilePath);
                                        Log.Information($"Deleted {game.Title} icon");
                                    }

                                    // Removing the game
                                    Games.Remove(game);
                                    Log.Information($"Removing the {game.Title} from the Library");
                                    await LoadGames();
                                    Log.Information("Reloading the library");
                                    await SaveGames();
                                    Log.Information($"Saving the new library without {game.Title}");
                                }
                            };
                            contextMenu.Items.Add(RemoveGame); // Add the item to the ContextMenu

                            // Backup save game
                            string saveGamePath = "";
                            if (game.EmulatorVersion == "Stable")
                            {
                                saveGamePath = App.appConfiguration.XeniaStable.EmulatorLocation + @"content\";
                            }
                            else if (game.EmulatorVersion == "Canary")
                            {
                                saveGamePath = App.appConfiguration.XeniaCanary.EmulatorLocation + @"content\";
                            }

                            // Import Save File
                            MenuItem ImportSaveFile = new MenuItem
                            {
                                Header = "Import save file", // Text that shows in the context menu
                                ToolTip = "Imports the save file to Xenia Emulator used by the game\nNOTE: This can overwrite existing save", // Hovering showing more detail about this option
                            };

                            // Action when this option is pressed
                            ImportSaveFile.Click += async (sender, e) =>
                            {
                                Mouse.OverrideCursor = Cursors.Wait; // This is to indicate an action is happening

                                Log.Information("Open file dialog");
                                OpenFileDialog openFileDialog = new OpenFileDialog();
                                openFileDialog.Title = "Select a save file";
                                openFileDialog.Filter = "All Files|*";
                                bool? result = openFileDialog.ShowDialog();
                                if (result == true)
                                {
                                    // If there is no directory for storing save files, create it
                                    Log.Information($"Selected file: {openFileDialog.FileName}");
                                    if (!Directory.Exists(saveGamePath + @$"{game.GameId}\00000001"))
                                    {
                                        Log.Information($"Creating a content folder for {game.Title}");
                                        Directory.CreateDirectory(saveGamePath + @$"{game.GameId}\00000001");
                                    }

                                    // Try to extract the save file to the directory
                                    try
                                    {
                                        ZipFile.ExtractToDirectory(openFileDialog.FileName, saveGamePath, true);
                                    }
                                    catch (Exception ex)
                                    {
                                        Log.Error(ex.Message + "\nFull Error:\n" + ex);
                                        MessageBox.Show(ex.Message);
                                        return;
                                    }
                                    await LoadGames();
                                    Mouse.OverrideCursor = null; // Indicating the action is over

                                    MessageBox.Show($"The save file for '{game.Title}' has been successfully imported.");
                                }
                                Mouse.OverrideCursor = null; // Indicating the action is over
                                await Task.Delay(1);
                            };
                            contextMenu.Items.Add(ImportSaveFile);

                            // Checks if the save file is there
                            if (Directory.Exists(saveGamePath + @$"{game.GameId}\00000001"))
                            {
                                // Export Save File
                                MenuItem ExportSaveFile = new MenuItem
                                {
                                    Header = "Export the save file", // Text that shows in the context menu
                                    ToolTip = "Exports the save file as a .zip to the desktop", // Hovering showing more detail about this option
                                };

                                // Action when this option is pressed
                                ExportSaveFile.Click += async (sender, e) =>
                                {
                                    Mouse.OverrideCursor = Cursors.Wait; // Indicating the action is happeing

                                    Log.Information("Ziping the save file and saving it to the Desktop");
                                    using (FileStream zipToOpen = new FileStream(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), $"{DateTime.Now.ToString("yyyyMMdd_HHmmss")} - {game.Title} Save File.zip"), FileMode.Create))
                                    {
                                        using (ZipArchive archive = new ZipArchive(zipToOpen, ZipArchiveMode.Create))
                                        {
                                            // Add files to the archive with the specified structure
                                            AddDirectoryToZip(archive, saveGamePath + @$"{game.GameId}\00000001", $"{game.GameId}/00000001");
                                        }
                                    }

                                    Mouse.OverrideCursor = null; // Indicating the action is over
                                    Log.Information($"The save file for '{game.Title}' has been successfully exported to the desktop");
                                    MessageBox.Show($"The save file for '{game.Title}' has been successfully exported to the desktop");
                                    await Task.Delay(1);
                                };
                                contextMenu.Items.Add(ExportSaveFile);
                            }

                            // Check if the game is using Xenia Canary (for game patches since Stable doesn't support them)
                            if (game.EmulatorVersion == "Stable")
                            {
                                // Switch to Canary
                                if (App.appConfiguration.XeniaCanary != null && App.appConfiguration.XeniaCanary.EmulatorLocation != null)
                                {
                                    MenuItem UseXeniaCanary = new MenuItem
                                    {
                                        Header = "Switch to Xenia Canary", // Text that shows in the context menu
                                        ToolTip = $"Transfer '{game.Title}' content to Xenia Canary and make it use Xenia Canary instead of Xenia Stable", // Hovering showing more detail about this option
                                    };

                                    // Action when this option is pressed
                                    UseXeniaCanary.Click += async (sender, e) => await TransferGame(game, "Stable", "Canary", App.appConfiguration.XeniaStable.EmulatorLocation, App.appConfiguration.XeniaCanary.EmulatorLocation, App.appConfiguration.XeniaCanary.ConfigurationFileLocation);
                                    contextMenu.Items.Add(UseXeniaCanary); // Add the item to the ContextMenu
                                }
                            }
                            else if (game.EmulatorVersion == "Canary")
                            {
                                // Check if the game has any patches installed
                                if (game.PatchFilePath != null)
                                {
                                    // If it does, add "Edit game patch" and "Remove game patch" to the ContextMenu
                                    // Enable/Disable game patches
                                    MenuItem EditGamePatch = new MenuItem
                                    {
                                        Header = "Game Patch options", // Text that shows in the context menu
                                        ToolTip = "Allows the user to enable/disable patches", // Hovering showing more detail about this option
                                    };

                                    // Action when this option is pressed
                                    EditGamePatch.Click += async (sender, e) =>
                                    {
                                        // Opens EditGamePatch window
                                        EditGamePatch editGamePatch = new EditGamePatch(game);
                                        editGamePatch.Show();
                                        await editGamePatch.WaitForCloseAsync();
                                    };
                                    contextMenu.Items.Add(EditGamePatch); // Add the item to the ContextMenu

                                    // Remove gamepatch from Xenia Emulator
                                    MenuItem RemoveGamePatch = new MenuItem
                                    {
                                        Header = "Remove game patch", // Text that shows in the context menu
                                        ToolTip = "Allows the user to remove the game patch from Xenia", // Hovering showing more detail about this option
                                    };

                                    // Action when this option is pressed
                                    RemoveGamePatch.Click += async (sender, e) =>
                                    {
                                        MessageBoxResult result = MessageBox.Show($"Do you want to remove {game.Title} patch?", "Confirmation", MessageBoxButton.YesNo);
                                        if (result == MessageBoxResult.Yes)
                                        {
                                            Log.Information($"Removing patch for {game.Title}");
                                            if (File.Exists(game.PatchFilePath))
                                            {
                                                File.Delete(game.PatchFilePath);
                                            }
                                            Log.Information($"Patch removed");
                                            game.PatchFilePath = null;
                                            await LoadGames();
                                            await SaveGames();
                                        }
                                    };
                                    contextMenu.Items.Add(RemoveGamePatch); // Add the item to the ContextMenu
                                }
                                else
                                {
                                    // If it doesn't, add "Add game patch" to the ContextMenu
                                    MenuItem AddGamePatch = new MenuItem
                                    {
                                        Header = "Add game patch", // Text that shows in the context menu
                                        ToolTip = "Downloads and installs the game patch user selects from game-patches repository" // Hovering showing more detail about this option
                                    };

                                    // Action when this option is pressed
                                    AddGamePatch.Click += async (sender, e) =>
                                    {
                                        // Check if patches folder exists
                                        if (!Directory.Exists(App.appConfiguration.XeniaCanary.EmulatorLocation + @"patches\"))
                                        {
                                            Directory.CreateDirectory(App.appConfiguration.XeniaCanary.EmulatorLocation + @"patches\");
                                        }
                                        Log.Information($"Adding {game.Title} patch file.");
                                        MessageBoxResult result = MessageBox.Show("Do you have the patch locally downloaded?", "Confirmation", MessageBoxButton.YesNo);
                                        if (result == MessageBoxResult.Yes)
                                        {
                                            // If user has the patch locally, install it
                                            OpenFileDialog openFileDialog = new OpenFileDialog();
                                            openFileDialog.Title = "Select a game patch";
                                            openFileDialog.Filter = "Supported Files|*.toml";
                                            if (openFileDialog.ShowDialog() == true)
                                            {
                                                Log.Information($"Selected file: {openFileDialog.FileName}");
                                                System.IO.File.Copy(openFileDialog.FileName, App.appConfiguration.XeniaCanary.EmulatorLocation + @$"patches\{Path.GetFileName(openFileDialog.FileName)}", true);
                                                Log.Information("Copying the file to the patches folder.");
                                                System.IO.File.Delete(openFileDialog.FileName);
                                                Log.Information("Deleting the original file.");
                                                game.PatchFilePath = App.appConfiguration.XeniaCanary.EmulatorLocation + @$"patches\{Path.GetFileName(openFileDialog.FileName)}";
                                                MessageBox.Show($"{game.Title} patch has been installed");
                                            }
                                        }
                                        else
                                        {
                                            // If user doesn't have the patch locally, check on Xenia Canary patches list if the game has any patches
                                            SelectGamePatch selectGamePatch = new SelectGamePatch(game);
                                            selectGamePatch.Show();
                                            await selectGamePatch.WaitForCloseAsync();
                                        }

                                        // Reload the UI
                                        await LoadGames();
                                        await SaveGames(); // Save changes in the .JSON file
                                    };
                                    contextMenu.Items.Add(AddGamePatch); // Add the item to the ContextMenu
                                }


                                // Install/Uninstall Title Updates
                                if (Directory.Exists($@"{App.appConfiguration.XeniaCanary.EmulatorLocation}content\{game.GameId}\000B0000\"))
                                {
                                    // Remove Title Update
                                    MenuItem RemoveTitleUpdate = new MenuItem
                                    {
                                        Header = "Remove Title updates", // Text that shows in the context menu
                                        ToolTip = $"Allows the user to remove every title update for {game.Title}", // Hovering showing more detail about this option
                                    };

                                    // Action when this option is pressed
                                    RemoveTitleUpdate.Click += async (sender, e) =>
                                    {
                                        Directory.Delete($@"{App.appConfiguration.XeniaCanary.EmulatorLocation}content\{game.GameId}\000B0000\", true);
                                        await LoadGames();
                                    };
                                    contextMenu.Items.Add(RemoveTitleUpdate); // Add the item to the ContextMenu
                                }
                                else
                                {
                                    // Install Title Update
                                    MenuItem InstallTitleUpdate = new MenuItem
                                    {
                                        Header = "Install Title updates", // Text that shows in the context menu
                                        ToolTip = $"Allows the user to install game updates for {game.Title}", // Hovering showing more detail about this option
                                    };

                                    // Action when this option is pressed
                                    InstallTitleUpdate.Click += async (sender, e) =>
                                    {
                                        // Open FileDialog where the user selects the TU file
                                        OpenFileDialog openFileDialog = new OpenFileDialog();
                                        openFileDialog.Title = "Select a game update";
                                        openFileDialog.Filter = "All Files|*";
                                        if (openFileDialog.ShowDialog() == true)
                                        {
                                            Log.Information($"Selected file: {openFileDialog.FileName}");

                                            // Use VFSDumpTool to install title update
                                            Process XeniaVFSDumpTool = new Process();
                                            XeniaVFSDumpTool.StartInfo.FileName = App.appConfiguration.VFSDumpToolLocation;
                                            XeniaVFSDumpTool.StartInfo.CreateNoWindow = true;
                                            XeniaVFSDumpTool.StartInfo.UseShellExecute = false;
                                            XeniaVFSDumpTool.StartInfo.Arguments = $@"""{openFileDialog.FileName}"" ""{App.appConfiguration.XeniaCanary.EmulatorLocation}content\{game.GameId}\000B0000\{Path.GetFileName(openFileDialog.FileName)}""";
                                            XeniaVFSDumpTool.Start();
                                            await XeniaVFSDumpTool.WaitForExitAsync();

                                            // Reload UI and show success mesage
                                            await LoadGames();
                                            MessageBox.Show($"{game.Title} has been updated.");
                                        }
                                    };
                                    contextMenu.Items.Add(InstallTitleUpdate); // Add the item to the ContextMenu
                                }

                                // Switch to Stable
                                if (App.appConfiguration.XeniaStable != null && App.appConfiguration.XeniaStable.EmulatorLocation != null)
                                {
                                    MenuItem UseXeniaStable = new MenuItem
                                    {
                                        Header = "Switch to Xenia Stable", // Text that shows in the context menu
                                        ToolTip = $"Transfer '{game.Title}' content to Xenia Stable and make it use Xenia Stable instead of Xenia Canary", // Hovering showing more detail about this option
                                    };

                                    // Action when this option is pressed
                                    UseXeniaStable.Click += async (sender, e) => await TransferGame(game, "Canary", "Stable", App.appConfiguration.XeniaCanary.EmulatorLocation, App.appConfiguration.XeniaStable.EmulatorLocation, App.appConfiguration.XeniaStable.ConfigurationFileLocation);
                                    contextMenu.Items.Add(UseXeniaStable); // Add the item to the ContextMenu
                                }
                            }
                            button.ContextMenu = contextMenu; // Add the ContextMenu to the actual button
                        };
                    }
                }
                await Task.Delay(1);
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message + "\nFull Error:\n" + ex);
                MessageBox.Show(ex.Message);
                return;
            }
        }

        /// <summary>
        /// Saves the installed games into installedGames.json
        /// </summary>
        private async Task SaveGames()
        {
            try
            {
                string JSON = JsonConvert.SerializeObject(Games, Formatting.Indented);
                System.IO.File.WriteAllText(AppDomain.CurrentDomain.BaseDirectory + @"installedGames.json", JSON);
                await Task.Delay(1);
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message + "\nFull Error:\n" + ex);
                MessageBox.Show(ex.Message);
                return;
            }
        }

        /// <summary>
        /// Opens FileDialog where user selects the game
        /// </summary>
        private async void AddGame_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Open file dialog
                Log.Information("Open file dialog");
                OpenFileDialog openFileDialog = new OpenFileDialog();
                openFileDialog.Title = "Select a game";
                openFileDialog.Filter = "Supported Files|*.iso;*.xex;*.zar|ISO Files (*.iso)|*.iso|XEX Files (*.xex)|*.xex|ZAR Files (*.zar)|*.zar|All Files|*";
                bool? result = openFileDialog.ShowDialog();
                if (result == true)
                {
                    Log.Information($"Selected file: {openFileDialog.FileName}");
                    if (App.appConfiguration.XeniaStable != null && App.appConfiguration.XeniaCanary != null)
                    {
                        Log.Information("Detected both Xenia installations");
                        Log.Information("Asking user what Xenia version will the game use");
                        XeniaSelection xs = new XeniaSelection();
                        await xs.WaitForCloseAsync();
                        Log.Information($"User selected Xenia {xs.UserSelection}");
                        await GetGameTitle(openFileDialog.FileName, xs.UserSelection);
                    }
                    else if (App.appConfiguration.XeniaStable != null && App.appConfiguration.XeniaCanary == null)
                    {
                        Log.Information("Only Xenia Stable is installed");
                        await GetGameTitle(openFileDialog.FileName, "Stable");
                    }
                    else
                    {
                        Log.Information("Only Xenia Canary is installed");
                        await GetGameTitle(openFileDialog.FileName, "Canary");
                    }
                }
                await LoadGames();
                await SaveGames();
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message + "\nFull Error:\n" + ex);
                MessageBox.Show(ex.Message);
                return;
            }
        }
    }
}
