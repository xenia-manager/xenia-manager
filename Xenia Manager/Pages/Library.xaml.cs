using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Text.RegularExpressions;
using System.Security.Cryptography;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
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
        public async void LoadGamesStartup()
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
        /// Checks if the game icon is cached
        /// <para>If the game icon is not cached, it'll cache it</para>
        /// </summary>
        /// <param name="game">Game</param>
        /// <returns >BitmapImage - cached game icon</returns>
        private async Task<BitmapImage> LoadOrCacheIcon(InstalledGame game)
        {
            await Task.Delay(1);
            string iconFilePath = Path.Combine(App.baseDirectory, game.IconFilePath); // Path to the game icon
            string cacheDirectory = Path.Combine(App.baseDirectory, @"Icons\Cache\"); // Path to the cached directory

            // Tries to find cached icon
            string identicalFilePath = FindFirstIdenticalFile(iconFilePath, cacheDirectory);
            if (identicalFilePath != null)
            {
                // If there is a cached icon, return it
                Log.Information("Icon has already been cached");
                return new BitmapImage(new Uri(identicalFilePath));
            }

            // If there's no cached icon, create a cached version and return it
            Log.Information("Creating new cached icon for the game");
            string randomIconName = Path.GetRandomFileName().Replace(".", "").Substring(0, 8) + ".ico";
            string cachedIconPath = Path.Combine(cacheDirectory, randomIconName);

            File.Copy(iconFilePath, cachedIconPath, true);
            Log.Information($"Cached icon name: {randomIconName}");

            return new BitmapImage(new Uri(cachedIconPath));
        }

        /// <summary>
        /// Creates image for the game button
        /// </summary>
        /// <param name="game">Game itself</param>
        /// <returns>Border - Content of the game button</returns>
        private async Task<Border> CreateButtonContent(InstalledGame game)
        {
            // Cached game icon
            BitmapImage iconImage = await LoadOrCacheIcon(game);
            Image image = new Image
            {
                Source = iconImage,
                Stretch = Stretch.UniformToFill
            };

            // Rounded edges of the game icon
            RectangleGeometry clipGeometry = new RectangleGeometry
            {
                Rect = new Rect(0, 0, 150, 207),
                RadiusX = 3,
                RadiusY = 3
            };

            // Game button content
            return new Border
            {
                CornerRadius = new CornerRadius(10),
                Background = Brushes.Black,
                Child = image,
                Clip = clipGeometry
            };
        }

        /// <summary>
        /// Launches the game
        /// </summary>
        /// <param name="game">The game user wants to launch</param>
        /// <param name="windowedMode">Check if he wants it to be in Windowed Mode</param>
        private async Task LaunchGame(InstalledGame game, bool windowedMode = false)
        {
            // Animations
            MainWindow mainWindow = Application.Current.MainWindow as MainWindow;
            DoubleAnimation fadeOutAnimation = new DoubleAnimation(1, 0, TimeSpan.FromSeconds(0.15));
            DoubleAnimation fadeInAnimation = new DoubleAnimation(0, 1, TimeSpan.FromSeconds(0.15));
            TaskCompletionSource<bool> animationCompleted = new TaskCompletionSource<bool>(); // Check for when animation is completed

            Log.Information($"Launching {game.Title}");
            Process xenia = new Process();

            // Checking what emulator the game uses
            if (game.EmulatorVersion == "Canary")
            {
                xenia.StartInfo.FileName = Path.Combine(App.baseDirectory, App.appConfiguration.XeniaCanary.ExecutableLocation);
            }
            else if (game.EmulatorVersion == "Stable")
            {
                xenia.StartInfo.FileName = Path.Combine(App.baseDirectory, App.appConfiguration.XeniaStable.ExecutableLocation);
            }

            // Adding default launch arguments
            xenia.StartInfo.Arguments = $@"""{game.GameFilePath}"" --config ""{Path.Combine(App.baseDirectory, game.ConfigFilePath)}""";

            // Checking if the game will be run in windowed mode
            if (windowedMode)
            {
                xenia.StartInfo.Arguments += " --fullscreen=false";
            }

            animationCompleted = new TaskCompletionSource<bool>();
            fadeOutAnimation.Completed += (s, e) =>
            {
                mainWindow.Visibility = Visibility.Collapsed; // Collapse the main window
                animationCompleted.SetResult(true); // Signal that the animation has completed
            };
            mainWindow.BeginAnimation(Window.OpacityProperty, fadeOutAnimation);
            await animationCompleted.Task; // Wait for animation to be completed

            // Starting the emulator
            xenia.Start();
            Log.Information("Emulator started");
            await xenia.WaitForExitAsync(); // Waiting for emulator to close
            Log.Information("Emulator closed");
            mainWindow.Visibility = Visibility.Visible;
            mainWindow.BeginAnimation(Window.OpacityProperty, fadeInAnimation);
        }

        /// <summary>
        /// Removes the game from Xenia Manager
        /// </summary>
        /// <param name="game">Game that we want to remove</param>
        private async Task RemoveGame(InstalledGame game)
        {
            MessageBoxResult result = MessageBox.Show($"Do you want to remove {game.Title}?", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                Log.Information($"Removing {game.Title}");

                // Remove game patch
                if (game.PatchFilePath != null && File.Exists(Path.Combine(App.baseDirectory, game.PatchFilePath)))
                {
                    File.Delete(Path.Combine(App.baseDirectory, game.PatchFilePath));
                    Log.Information($"Deleted file: {Path.Combine(App.baseDirectory, game.PatchFilePath)}");
                };

                // Remove game configuration file
                if (game.ConfigFilePath != null && File.Exists(Path.Combine(App.baseDirectory, game.ConfigFilePath)))
                {
                    File.Delete(Path.Combine(App.baseDirectory, game.ConfigFilePath));
                    Log.Information($"Deleted file: {Path.Combine(App.baseDirectory, game.ConfigFilePath)}");
                };

                // Remove game icon
                if (game.IconFilePath != null && File.Exists(Path.Combine(App.baseDirectory, game.IconFilePath)))
                {
                    File.Delete(Path.Combine(App.baseDirectory, game.IconFilePath));
                    Log.Information($"Deleted file: {Path.Combine(App.baseDirectory, game.IconFilePath)}");
                };

                // Remove game from Xenia Manager
                Games.Remove(game);
                Log.Information($"Removing {game.Title} from the Library");

                // Reload the UI and save changes to the JSON file
                await LoadGames();
                await SaveGames(); 
                Log.Information($"Saving the new library without {game.Title}");
            }
        }

        /// <summary>
        /// Grabs the path to the "content" folder of the emulator
        /// </summary>
        /// <param name="game">Game</param>
        /// <returns>Path to the content folder of the emulator</returns>
        private string GetSaveGamePath(InstalledGame game)
        {
            return game.EmulatorVersion == "Stable"
                ? Path.Combine(App.baseDirectory, App.appConfiguration.XeniaStable.EmulatorLocation, @"content\")
                : Path.Combine(App.baseDirectory, App.appConfiguration.XeniaCanary.EmulatorLocation, @"content\");
        }

        /// <summary>
        /// Removes game patch
        /// </summary>
        /// <param name="game">Game</param>
        private async Task RemoveGamePatch(InstalledGame game)
        {
            MessageBoxResult result = MessageBox.Show($"Do you want to remove {game.Title} patch?", "Confirmation", MessageBoxButton.YesNo);
            if (result == MessageBoxResult.Yes)
            {
                Log.Information($"Removing patch for {game.Title}");
                if (File.Exists(Path.Combine(App.baseDirectory, game.PatchFilePath)))
                {
                    File.Delete(Path.Combine(App.baseDirectory, game.PatchFilePath));
                }
                Log.Information($"Patch removed");
                game.PatchFilePath = null;
                await LoadGames();
                await SaveGames();
            }
        }

        /// <summary>
        /// Adds game patches to Xenia Canary
        /// </summary>
        /// <param name="game"></param>
        /// <returns></returns>
        private async Task AddGamePatch(InstalledGame game)
        {
            // Check if patches folder exists
            if (!Directory.Exists(Path.Combine(App.baseDirectory, App.appConfiguration.XeniaCanary.EmulatorLocation, @"patches\")))
            {
                Directory.CreateDirectory(Path.Combine(App.baseDirectory, App.appConfiguration.XeniaCanary.EmulatorLocation, @"patches\"));
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
                    System.IO.File.Copy(openFileDialog.FileName, Path.Combine(App.baseDirectory, App.appConfiguration.XeniaCanary.EmulatorLocation, @$"patches\{Path.GetFileName(openFileDialog.FileName)}"), true);
                    Log.Information("Copying the file to the patches folder.");
                    System.IO.File.Delete(openFileDialog.FileName);
                    Log.Information("Deleting the original file.");
                    game.PatchFilePath = Path.Combine(App.appConfiguration.XeniaCanary.EmulatorLocation, @$"patches\{Path.GetFileName(openFileDialog.FileName)}");
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
        }

        /// <summary>
        /// Opens File Dialog and allows user to select Title Updates, DLC's etc.
        /// <para>Checks every selected file and tries to determine what it is.</para>
        /// Opens 'InstallContent' window where all of the selected and supported items are shown with a 'Confirm' button below
        /// </summary>
        private async void InstallContent(InstalledGame game)
        {
            Log.Information("Open file dialog");
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Title = $"Select files for {game.Title}";
            openFileDialog.Filter = "All Files|*";
            openFileDialog.Multiselect = true;
            bool? result = openFileDialog.ShowDialog();
            if (result == true)
            {
                Mouse.OverrideCursor = Cursors.Wait;
                List<GameContent> gameContent = new List<GameContent>();
                foreach (string file in openFileDialog.FileNames)
                {
                    try
                    {
                        STFS stfs = new STFS(file);
                        if (stfs.SupportedFile)
                        {
                            stfs.ReadTitle();
                            stfs.ReadDisplayName();
                            stfs.ReadContentType();
                            var (contentType, contentTypeValue) = stfs.GetContentType();
                            GameContent content = new GameContent();
                            content.GameId = game.GameId;
                            content.ContentTitle = stfs.Title;
                            content.ContentDisplayName = stfs.DisplayName;
                            content.ContentType = contentType.ToString().Replace('_', ' ');
                            content.ContentTypeValue = $"{contentTypeValue:X8}";
                            content.ContentPath = file;
                            if (content.ContentType != null)
                            {
                                gameContent.Add(content);
                            }
                        }
                        else
                        {
                            Log.Information($"{Path.GetFileNameWithoutExtension(file)} is currently not supported");
                            MessageBox.Show($"{Path.GetFileNameWithoutExtension(file)} is currently not supported");
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Information($"Error: {ex.Message}");
                    }
                }
                Mouse.OverrideCursor = null;
                if (gameContent.Count > 0)
                {
                    InstallContent installContent = new InstallContent(gameContent);
                    await installContent.WaitForCloseAsync();
                }
            };
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
            if (!File.Exists(Path.Combine(App.baseDirectory, game.ConfigFilePath)))
            {
                Log.Information("Game configuration file not found");
                Log.Information("Creating a new configuration file from the default one");
                File.Copy(Path.Combine(App.baseDirectory, defaultConfigFileLocation), Path.Combine(App.baseDirectory, targetEmulatorLocation, $@"config\{game.Title}.config.toml"), true);
            }

            // Checking if there is some content installed that should be copied over
            if (Directory.Exists(Path.Combine(App.baseDirectory, @$"{sourceEmulatorLocation}content\{game.GameId}")))
            {
                Log.Information($"Copying all of the installed content and saves from Xenia {SourceVersion} to Xenia {TargetVersion}");
                // Create all of the necessary directories for content copy
                foreach (string dirPath in Directory.GetDirectories(Path.Combine(App.baseDirectory, @$"{sourceEmulatorLocation}content\{game.GameId}"), "*", SearchOption.AllDirectories))
                {
                    Directory.CreateDirectory(dirPath.Replace(Path.Combine(App.baseDirectory, @$"{sourceEmulatorLocation}content\{game.GameId}"), Path.Combine(App.baseDirectory, @$"{targetEmulatorLocation}content\{game.GameId}")));
                }

                // Copy all the files
                foreach (string newPath in Directory.GetFiles(Path.Combine(App.baseDirectory, @$"{sourceEmulatorLocation}content\{game.GameId}"), "*.*", SearchOption.AllDirectories))
                {
                    File.Copy(newPath, newPath.Replace(Path.Combine(App.baseDirectory, @$"{sourceEmulatorLocation}content\{game.GameId}"), Path.Combine(App.baseDirectory, $@"{targetEmulatorLocation}content\{game.GameId}")), true);
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
        /// Creates a ContextMenu Item for a option
        /// </summary>
        /// <param name="header">Text that is shown in the ContextMenu for this option</param>
        /// <param name="toolTip">Hovered description of the option</param>
        /// <param name="clickHandler">Event when the option is selected</param>
        /// <returns></returns>
        private MenuItem CreateMenuItem(string header, string? toolTipText, RoutedEventHandler clickHandler)
        {
            MenuItem menuItem = new MenuItem { Header = header };
            if (!string.IsNullOrEmpty(toolTipText))
            {
                if (!toolTipText.Contains("\nNOTE:"))
                {
                    menuItem.ToolTip = toolTipText;
                }
                else
                {
                    ToolTip toolTip = new ToolTip();
                    TextBlock textBlock = new TextBlock();

                    // Split the string into parts
                    string[] parts = toolTipText.Split(new string[] { "\nNOTE:" }, StringSplitOptions.None);

                    // Add the first part (before "NOTE:")
                    textBlock.Inlines.Add(new Run(parts[0]));

                    // Add "NOTE:" in bold
                    Run boldRun = new Run("\nNOTE:") { FontWeight = FontWeights.Bold };
                    textBlock.Inlines.Add(boldRun);

                    // Add the rest of the string (after "NOTE:")
                    if (parts.Length > 1)
                    {
                        textBlock.Inlines.Add(new Run(parts[1]));
                    }

                    // Assign TextBlock to ToolTip's content
                    toolTip.Content = textBlock;
                    menuItem.ToolTip = toolTip;
                }
            }
            menuItem.Click += clickHandler;
            return menuItem;
        }

        /// <summary>
        /// Creates ContextMenu for the button of the game
        /// </summary>
        /// <param name="button"></param>
        /// <param name="game"></param>
        private void InitializeContextMenu(Button button, InstalledGame game)
        {
            // Create new Context Menu
            ContextMenu contextMenu = new ContextMenu();

            // Add "Launch games in Windowed mode" option
            contextMenu.Items.Add(CreateMenuItem("Launch game in windowed mode", "Start the game in a window instead of fullscreen", async (sender, e) =>
            {
                await LaunchGame(game, true);
                await LoadGames();
            }));

            // Add "Add shortcut to desktop" option
            contextMenu.Items.Add(CreateMenuItem("Add shortcut to desktop", null, (sender, e) =>
            {
                if (game.EmulatorVersion == "Stable")
                {
                    ShortcutCreator.CreateShortcutOnDesktop(game.Title, Path.Combine(App.baseDirectory, App.appConfiguration.XeniaStable.ExecutableLocation), Path.Combine(App.baseDirectory, App.appConfiguration.XeniaStable.EmulatorLocation), $@"""{game.GameFilePath}"" --config ""{Path.Combine(App.baseDirectory, game.ConfigFilePath)}""", Path.Combine(App.baseDirectory, game.IconFilePath));
                }
                else if (game.EmulatorVersion == "Canary")
                {
                    ShortcutCreator.CreateShortcutOnDesktop(game.Title, Path.Combine(App.baseDirectory, App.appConfiguration.XeniaCanary.ExecutableLocation), Path.Combine(App.baseDirectory, App.appConfiguration.XeniaCanary.EmulatorLocation), $@"""{game.GameFilePath}"" --config ""{Path.Combine(App.baseDirectory, game.ConfigFilePath)}""", Path.Combine(App.baseDirectory, game.IconFilePath));
                }
            }));

            // Add "Open Compatibility Page" option
            if (game.GameCompatibilityURL != null)
            {
                contextMenu.Items.Add(CreateMenuItem("Open Compatibility Page", null, (sender, e) =>
                {
                    ProcessStartInfo compatibilityPageURL = new ProcessStartInfo(game.GameCompatibilityURL) { UseShellExecute = true };
                    Process.Start(compatibilityPageURL);
                }));
            }

            // Add "Delete game" option
            contextMenu.Items.Add(CreateMenuItem("Delete game", "Deletes the game from Xenia Manager", async (sender, e) => await RemoveGame(game)));

            // Check what version of Xenia the game uses
            switch (game.EmulatorVersion)
            {
                case "Stable":
                    // Check if Xenia Canary is installed
                    if (App.appConfiguration.XeniaCanary != null && Directory.Exists(App.appConfiguration.XeniaCanary.EmulatorLocation))
                    {
                        // Add "Switch to Xenia Canary" option
                        contextMenu.Items.Add(CreateMenuItem("Switch to Xenia Canary", $"Migrate '{game.Title}' content to Xenia Canary and set it to use Xenia Canary instead of Xenia Stable", async (sender, e) =>
                        {
                            await TransferGame(game, "Stable", "Canary", App.appConfiguration.XeniaStable.EmulatorLocation, App.appConfiguration.XeniaCanary.EmulatorLocation, App.appConfiguration.XeniaCanary.ConfigurationFileLocation);
                        }));
                    };
                    break;
                case "Canary":
                    // Check if the game has any game patches installed
                    if (game.PatchFilePath != null)
                    {
                        // Add "Patch Settings" option
                        contextMenu.Items.Add(CreateMenuItem("Patch Settings", "Enable or disable game patches", async (sender, e) =>
                        {
                            // Opens EditGamePatch window
                            EditGamePatch editGamePatch = new EditGamePatch(game);
                            editGamePatch.Show();
                            await editGamePatch.WaitForCloseAsync();
                        }));

                        // Add "Remove Game Patch" option
                        contextMenu.Items.Add(CreateMenuItem("Remove Game Patch", "Allows the user to remove the game patch from Xenia", async (sender, e) => await RemoveGamePatch(game)));
                    }
                    else
                    {
                        // Add "Add game patch" option
                        contextMenu.Items.Add(CreateMenuItem("Add Game Patch", "Downloads and installs a selected game patch from the game-patches repository", async (sender, e) => await AddGamePatch(game)));
                    }

                    // Add 'Install content' option
                    contextMenu.Items.Add(CreateMenuItem("Install Content", $"Install various game content like DLC, Title Updates etc.", (sender, e) => InstallContent(game)));

                    // Add 'Show installed content' option
                    contextMenu.Items.Add(CreateMenuItem("Show Installed Content", $"Allows the user to see what's installed in game content folder and to export save files", async (sender, e) =>
                    {
                        ShowInstalledContent showInstalledContent = new ShowInstalledContent(game);
                        await showInstalledContent.WaitForCloseAsync();
                    }));

                    // Check if Xenia Stable is installed
                    if (App.appConfiguration.XeniaStable != null && Directory.Exists(App.appConfiguration.XeniaStable.EmulatorLocation))
                    {
                        // Add "Switch to Xenia Stable" option
                        contextMenu.Items.Add(CreateMenuItem("Switch to Xenia Stable", $"Migrate '{game.Title}' content to Xenia Stable and set it to use Xenia Stable instead of Xenia Canary", async (sender, e) =>
                        {
                            await TransferGame(game, "Canary", "Stable", App.appConfiguration.XeniaCanary.EmulatorLocation, App.appConfiguration.XeniaStable.EmulatorLocation, App.appConfiguration.XeniaStable.ConfigurationFileLocation);
                        }));
                    };
                    break;
                default:
                    break;
            }

            // Add the new Context Menu to the game button
            button.ContextMenu = contextMenu;
        }

        /// <summary>
        /// Loads the games into the Wrappanel
        /// </summary>
        private async Task LoadGamesIntoUI()
        {
            try
            {
                // Check if there are any games installed
                if (Games == null && Games.Count == 0)
                {
                    return;
                };

                // Sort the games by name
                IOrderedEnumerable<InstalledGame> orderedGames = Games.OrderBy(game => game.Title);
                foreach (InstalledGame game in orderedGames)
                {
                    // Create a new button for the game
                    Button button = new Button();
                    Log.Information($"Adding {game.Title} to the Library");

                    // Creating image for the game button
                    button.Content = await CreateButtonContent(game);

                    // Animations
                    MainWindow mainWindow = Application.Current.MainWindow as MainWindow;
                    DoubleAnimation fadeOutAnimation = new DoubleAnimation(1, 0, TimeSpan.FromSeconds(0.15));
                    DoubleAnimation fadeInAnimation = new DoubleAnimation(0, 1, TimeSpan.FromSeconds(0.15));

                    // Check for when animation is completed
                    TaskCompletionSource<bool> animationCompleted = new TaskCompletionSource<bool>();

                    // When user clicks on the game, launch the game
                    button.Click += async (sender, e) =>
                    {
                        // Launch the game
                        await LaunchGame(game);

                        // When the user closes the game/emulator, reload the UI
                        await LoadGames();
                    };

                    button.Cursor = Cursors.Hand; // Change cursor to hand cursor
                    button.Style = (Style)FindResource("GameCoverButtons"); // Styling of the game button

                    // Tooltip
                    ToolTip toolTip = new ToolTip();
                    TextBlock textBlock = new TextBlock();
                    textBlock.Inlines.Add(new Run("Game Name:") { FontWeight = FontWeights.Bold });
                    textBlock.Inlines.Add(new Run(" " + game.Title + "\n"));
                    textBlock.Inlines.Add(new Run("Game ID:") { FontWeight = FontWeights.Bold });
                    textBlock.Inlines.Add(new Run(" " + game.GameId));
                    toolTip.Content = textBlock;
                    button.ToolTip = toolTip;

                    wrapPanel.Children.Add(button); // Add the game to the Warp Panel

                    // When button loads
                    button.Loaded += (sender, e) =>
                    {
                        // Button width and height
                        button.Width = 150;
                        button.Height = 207;
                        button.Margin = new Thickness(5);

                        InitializeContextMenu(button, game); // Creates ContextMenu
                    };
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
                    xenia.StartInfo.FileName = Path.Combine(App.baseDirectory, App.appConfiguration.XeniaStable.EmulatorLocation, @"xenia.exe");
                }
                else
                {
                    xenia.StartInfo.FileName = Path.Combine(App.baseDirectory, App.appConfiguration.XeniaCanary.EmulatorLocation, @"xenia_canary.exe");
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
                openFileDialog.Filter = "All Files|*|Supported Files|*.iso;*.xex;*.zar";
                openFileDialog.Multiselect = true;
                bool? result = openFileDialog.ShowDialog();
                if (result == true)
                {
                    foreach (string game in openFileDialog.FileNames)
                    {
                        Log.Information($"Selected file: {openFileDialog.FileName}");
                        if (App.appConfiguration.XeniaStable != null && App.appConfiguration.XeniaCanary != null)
                        {
                            Log.Information("Detected both Xenia installations");
                            Log.Information("Asking user what Xenia version will the game use");
                            XeniaSelection xs = new XeniaSelection();
                            await xs.WaitForCloseAsync();
                            Log.Information($"User selected Xenia {xs.UserSelection}");
                            await GetGameTitle(game, xs.UserSelection);
                        }
                        else if (App.appConfiguration.XeniaStable != null && App.appConfiguration.XeniaCanary == null)
                        {
                            Log.Information("Only Xenia Stable is installed");
                            await GetGameTitle(game, "Stable");
                        }
                        else
                        {
                            Log.Information("Only Xenia Canary is installed");
                            await GetGameTitle(game, "Canary");
                        }
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
