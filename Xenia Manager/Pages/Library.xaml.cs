using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
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
using Newtonsoft.Json.Linq;
using Serilog;
using Tomlyn;
using Tomlyn.Model;
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
                if (!File.Exists(Path.Combine(App.baseDirectory, "patches.json")))
                {
                    await App.GrabGamePatches();
                }
                else
                {
                    string json = File.ReadAllText(Path.Combine(App.baseDirectory, "patches.json"));
                    App.gamePatches = JsonConvert.DeserializeObject<List<GamePatch>>(json);
                }
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
            string iconFilePath = Path.Combine(App.baseDirectory, game.BoxartFilePath); // Path to the game icon
            string cacheDirectory = Path.Combine(App.baseDirectory, @"Icons\Cache\"); // Path to the cached directory

            // Tries to find cached icon
            game.CachedIconPath = FindFirstIdenticalFile(iconFilePath, cacheDirectory);
            if (game.CachedIconPath != null)
            {
                // If there is a cached icon, return it
                Log.Information("Icon has already been cached");
                return new BitmapImage(new Uri(game.CachedIconPath));
            }

            // If there's no cached icon, create a cached version and return it
            Log.Information("Creating new cached icon for the game");
            string randomIconName = Path.GetRandomFileName().Replace(".", "").Substring(0, 8) + ".ico";
            game.CachedIconPath = Path.Combine(cacheDirectory, randomIconName);

            File.Copy(iconFilePath, game.CachedIconPath, true);
            Log.Information($"Cached icon name: {randomIconName}");
            game.CachedIconPath = game.CachedIconPath;

            return new BitmapImage(new Uri(game.CachedIconPath));
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
            Image gameImage = new Image
            {
                Source = iconImage,
                Stretch = Stretch.UniformToFill
            };

            // Create a Grid to hold both the game image and the overlay symbol
            Grid contentGrid = new Grid();

            // Add the game image to the grid
            contentGrid.Children.Add(gameImage);

            // Compatibility Rating
            Border CompatibilityRatingImage = new Border
            {
                Width = 22, // Width of the emoji
                Height = 22, // Height of the emoji
                Background = Brushes.White,
                BorderBrush = Brushes.Black,
                BorderThickness = new Thickness(1),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(1, 1, 0, 0),
                CornerRadius = new CornerRadius(16)
            };

            Image CompatibilityRating = new Image
            {
                Width = 20, // Width of the emoji
                Height = 20, // Height of the emoji
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            switch (game.CompatibilityRating)
            {
                case "Unplayable":
                    CompatibilityRating.Source = new BitmapImage(new Uri("pack://application:,,,/Xenia Manager;component/Assets/Compatibility Icons/Unplayable.png"));
                    break;
                case "Loads":
                    CompatibilityRating.Source = new BitmapImage(new Uri("pack://application:,,,/Xenia Manager;component/Assets/Compatibility Icons/Loads.png"));
                    break;
                case "Gameplay":
                    CompatibilityRating.Source = new BitmapImage(new Uri("pack://application:,,,/Xenia Manager;component/Assets/Compatibility Icons/Gameplay.png"));
                    break;
                case "Playable":
                    CompatibilityRating.Source = new BitmapImage(new Uri("pack://application:,,,/Xenia Manager;component/Assets/Compatibility Icons/Playable.png"));
                    break;
                default:
                    CompatibilityRating.Source = new BitmapImage(new Uri("pack://application:,,,/Xenia Manager;component/Assets/Compatibility Icons/Unknown.png"));
                    break;
            }

            // Add the compatibility rating to the grid
            CompatibilityRatingImage.Child = CompatibilityRating;
            contentGrid.Children.Add(CompatibilityRatingImage);

            // Rounded edges of the game boxart
            RectangleGeometry clipGeometry = new RectangleGeometry
            {
                Rect = new Rect(0, 0, 150, 207),
                RadiusX = 3,
                RadiusY = 3
            };

            // Game button content with rounded corners
            return new Border
            {
                CornerRadius = new CornerRadius(10),
                Background = Brushes.Black,
                Child = contentGrid,
                Clip = clipGeometry
            };
        }

        /// <summary>
        /// Checks for the compatibility of the game with the emulator
        /// </summary>
        private async Task GetCompatibilityRating(InstalledGame game)
        {
            try
            {
                Log.Information($"Trying to find the compatibility page for {game.Title}");
                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("User-Agent", "Xenia Manager (https://github.com/xenia-manager/xenia-manager)");
                    client.DefaultRequestHeaders.Add("Accept", "application/vnd.github.v3+json");

                    HttpResponseMessage response = await client.GetAsync(game.GameCompatibilityURL.Replace("https://github.com/", "https://api.github.com/repos/"));

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
                                        game.CompatibilityRating = "Unplayable";
                                        break;
                                    case "intro":
                                    case "hang":
                                    case "load":
                                    case "title":
                                    case "menus":
                                        game.CompatibilityRating = "Loads";
                                        break;
                                    case "gameplay":
                                        game.CompatibilityRating = "Gameplay";
                                        break;
                                    case "playable":
                                        game.CompatibilityRating = "Playable";
                                        break;
                                    default:
                                        game.CompatibilityRating = "Unknown";
                                        break;
                                }
                                Log.Information($"Current compatibility: {game.CompatibilityRating}");
                                break;
                            }
                            if (!foundCompatibility)
                            {
                                game.CompatibilityRating = "Unknown";
                            }
                        }
                    }
                    else
                    {
                        game.CompatibilityRating = "Unknown";
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message + "\nFull Error:\n" + ex);
                game.CompatibilityRating = "Unknown";
            }
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
            switch (game.EmulatorVersion)
            {
                case "Stable":
                    xenia.StartInfo.FileName = Path.Combine(App.baseDirectory, App.appConfiguration.XeniaStable.ExecutableLocation);
                    xenia.StartInfo.WorkingDirectory = Path.Combine(App.baseDirectory, App.appConfiguration.XeniaStable.EmulatorLocation);
                    break;
                case "Canary":
                    xenia.StartInfo.FileName = Path.Combine(App.baseDirectory, App.appConfiguration.XeniaCanary.ExecutableLocation);
                    xenia.StartInfo.WorkingDirectory = Path.Combine(App.baseDirectory, App.appConfiguration.XeniaCanary.EmulatorLocation);
                    break;
                case "Netplay":
                    xenia.StartInfo.FileName = Path.Combine(App.baseDirectory, App.appConfiguration.XeniaNetplay.ExecutableLocation);
                    xenia.StartInfo.WorkingDirectory = Path.Combine(App.baseDirectory, App.appConfiguration.XeniaNetplay.EmulatorLocation);
                    break;
                case "Custom":
                    xenia.StartInfo.FileName = game.EmulatorExecutableLocation;
                    xenia.StartInfo.WorkingDirectory = Path.GetDirectoryName(game.EmulatorExecutableLocation);
                    break;
                default:
                    break;
            }
            Log.Information($"Xenia Executable Location: {xenia.StartInfo.FileName}");

            // Adding default launch arguments
            if (game.EmulatorVersion != "Custom" && game.ConfigFilePath != null)
            {
                xenia.StartInfo.Arguments = $@"""{game.GameFilePath}"" --config ""{Path.Combine(App.baseDirectory, game.ConfigFilePath)}""";
            }
            else if (game.ConfigFilePath != null)
            {
                xenia.StartInfo.Arguments = $@"""{game.GameFilePath}"" --config ""{game.ConfigFilePath}""";
            }
            //xenia.StartInfo.ArgumentList.Add(game.GameFilePath);
            //xenia.StartInfo.ArgumentList.Add("--config");
            //xenia.StartInfo.ArgumentList.Add(game.ConfigFilePath);

            // Checking if the game will be run in windowed mode
            if (windowedMode)
            {
                xenia.StartInfo.Arguments += " --fullscreen=false";
            }

            Log.Information($"Xenia Arguments: {xenia.StartInfo.Arguments}");

            animationCompleted = new TaskCompletionSource<bool>();
            fadeOutAnimation.Completed += (s, e) =>
            {
                mainWindow.Visibility = Visibility.Collapsed; // Collapse the main window
                animationCompleted.SetResult(true); // Signal that the animation has completed
            };
            mainWindow.BeginAnimation(Window.OpacityProperty, fadeOutAnimation);
            await animationCompleted.Task; // Wait for animation to be completed

            // Starting the emulator
            DateTime TimeBeforeLaunch = DateTime.Now;
            xenia.Start();
            xenia.Exited += async (s, args) =>
            {
                TimeSpan PlayTime = DateTime.Now - TimeBeforeLaunch;
                //TimeSpan PlayTime = TimeSpan.FromMinutes(10.5); // For testing purposes
                Log.Information($"Current session playtime: {PlayTime.Minutes} minutes");
                if (game.Playtime != null)
                {
                    game.Playtime += PlayTime.TotalMinutes;
                }
                else
                {
                    game.Playtime = PlayTime.TotalMinutes;
                }
                await SaveGames();
            };
            Log.Information("Emulator started");
            Log.Information("Waiting for emulator to be closed");
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
                    Log.Information($"Deleted patch: {Path.Combine(App.baseDirectory, game.PatchFilePath)}");
                };

                // Remove game configuration file
                if (game.ConfigFilePath != null && File.Exists(Path.Combine(App.baseDirectory, game.ConfigFilePath)))
                {
                    File.Delete(Path.Combine(App.baseDirectory, game.ConfigFilePath));
                    Log.Information($"Deleted configuration file: {Path.Combine(App.baseDirectory, game.ConfigFilePath)}");
                };

                // Remove game boxart
                if (game.BoxartFilePath != null && File.Exists(Path.Combine(App.baseDirectory, game.BoxartFilePath)))
                {
                    File.Delete(Path.Combine(App.baseDirectory, game.BoxartFilePath));
                    Log.Information($"Deleted boxart: {Path.GetFileName(Path.Combine(App.baseDirectory, game.BoxartFilePath))}");
                };

                // Remove game icon
                if (game.ShortcutIconFilePath != null && File.Exists(Path.Combine(App.baseDirectory, game.ShortcutIconFilePath)))
                {
                    File.Delete(Path.Combine(App.baseDirectory, game.ShortcutIconFilePath));
                    Log.Information($"Deleted icon: {Path.GetFileName(Path.Combine(App.baseDirectory, game.ShortcutIconFilePath))}");
                }

                // Check if there is any content
                string GameContentFolder = game.EmulatorVersion switch
                {
                    "Stable" => $@"{App.appConfiguration.XeniaStable.EmulatorLocation}\content\{game.GameId}",
                    "Canary" => $@"{App.appConfiguration.XeniaCanary.EmulatorLocation}\content\{game.GameId}",
                    "Netplay" => $@"{App.appConfiguration.XeniaNetplay.EmulatorLocation}\content\{game.GameId}",
                    _ => ""
                };

                // Checking if directory exists
                if (Directory.Exists(GameContentFolder))
                {
                    // Checking if there is something in it
                    if (Directory.EnumerateFileSystemEntries(GameContentFolder).Any())
                    {
                        MessageBoxResult ContentDeletionResult = MessageBox.Show($"Do you want to remove {game.Title} content folder?\nThis will get rid of all of the installed title updates, save games etc.", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question);
                        if (ContentDeletionResult == MessageBoxResult.Yes)
                        {
                            Log.Information($"Deleting content folder of {game.Title}");
                            Directory.Delete(GameContentFolder, true);
                        }
                    }
                }

                // Remove game from Xenia Manager
                Log.Information($"Removing {game.Title} from the Library");
                Games.Remove(game);

                // Reload the UI and save changes to the JSON file
                Log.Information($"Saving the new library without {game.Title}");
                await LoadGames();
                await SaveGames(); 
            }
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
                
                // Reload UI
                Log.Information("Reloading the UI");
                await LoadGames();
                await SaveGames();
            }
        }

        /// <summary>
        /// Used to add additional patches to the already existing patch file
        /// </summary>
        /// <param name="game"></param>
        /// <returns></returns>
        private void AddAdditionalPatches(string gamePatchFileLocation, string newPatchFileLocation)
        {
            try
            {
                Mouse.OverrideCursor = Cursors.Wait;
                // Reading .toml files as TomlTable
                TomlTable originalPatchFile = Toml.ToModel(File.ReadAllText(gamePatchFileLocation));
                TomlTable newPatchFile = Toml.ToModel(File.ReadAllText(newPatchFileLocation));
                if (originalPatchFile["hash"].ToString() == newPatchFile["hash"].ToString())
                {
                    Log.Information("These patches match");
                    TomlTableArray originalPatches = originalPatchFile["patch"] as TomlTableArray;
                    TomlTableArray newPatches = newPatchFile["patch"] as TomlTableArray;

                    Log.Information("Looking for any new patches");
                    foreach (TomlTable patch in newPatches)
                    {
                        bool patchExists = originalPatches.Any(p => p["name"].ToString() == patch["name"].ToString());
                        if (!patchExists)
                        {
                            Log.Information($"{patch["name"].ToString()} is being added to the game patch file");
                            originalPatches.Add(patch);
                        }
                    }

                    Log.Information("Saving changes");
                    string updatedPatchFile = Toml.FromModel(originalPatchFile);
                    File.WriteAllText(gamePatchFileLocation, updatedPatchFile);
                    Log.Information("Additional patches have been added");
                }
                else
                {
                    Log.Error("Patches do not match");
                    MessageBox.Show("Hashes do not match.\nThis patch file is not supported.");
                }
                Mouse.OverrideCursor = null;
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message + "\nFull Error:\n" + ex);
                MessageBox.Show(ex.Message);
                return;
            }
        }

        /// <summary>
        /// Adds game patches to Xenia Canary
        /// </summary>
        /// <param name="game"></param>
        /// <returns></returns>
        private async Task AddGamePatch(InstalledGame game)
        {
            // Checking emulator version
            string EmulatorLocation = game.EmulatorVersion switch
            {
                "Canary" => App.appConfiguration.XeniaCanary.EmulatorLocation,
                "Netplay" => App.appConfiguration.XeniaNetplay.EmulatorLocation,
                _ => throw new InvalidOperationException("Unexpected build type")
            };

            // Check if patches folder exists
            if (!Directory.Exists(Path.Combine(App.baseDirectory, EmulatorLocation, @"patches\")))
            {
                Directory.CreateDirectory(Path.Combine(App.baseDirectory, EmulatorLocation, @"patches\"));
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
                    System.IO.File.Copy(openFileDialog.FileName, Path.Combine(App.baseDirectory, EmulatorLocation, @$"patches\{Path.GetFileName(openFileDialog.FileName)}"), true);
                    Log.Information("Copying the file to the patches folder.");
                    game.PatchFilePath = Path.Combine(EmulatorLocation, @$"patches\{Path.GetFileName(openFileDialog.FileName)}");
                    MessageBox.Show($"{game.Title} patch has been installed");
                }
            }
            else
            {
                // If user doesn't have the patch locally, check on Xenia Canary patches list if the game has any patches
                Log.Information("Opening window for selecting game patches");
                SelectGamePatch selectGamePatch = new SelectGamePatch(game);
                selectGamePatch.Show();
                await selectGamePatch.WaitForCloseAsync();
            }

            // Reload the UI
            Log.Information("Reloading the UI");
            await LoadGames();
            await SaveGames(); // Save changes in the .JSON file
        }

        /// <summary>
        /// Updates game patch to the latest version
        /// </summary>
        /// <param name="url">URL to the latest version of patch</param>
        /// <param name="savePath">Where to save the patch</param>
        private async Task UpdateGamePatch(string url, string savePath)
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    try
                    {
                        client.DefaultRequestHeaders.Add("User-Agent", "Xenia Manager (https://github.com/xenia-manager/xenia-manager)");
                        HttpResponseMessage response = await client.GetAsync(url);

                        if (response.IsSuccessStatusCode)
                        {
                            byte[] content = await response.Content.ReadAsByteArrayAsync();
                            await System.IO.File.WriteAllBytesAsync(savePath, content);
                            Log.Information("Patch successfully downloaded");
                        }
                        else
                        {
                            Log.Error($"Failed to download file. Status code: {response.StatusCode}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"An error occurred: {ex.Message}");
                    }
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
        /// Opens File Dialog and allows user to select Title Updates, DLC's etc.
        /// <para>Checks every selected file and tries to determine what it is.</para>
        /// Opens 'InstallContent' window where all of the selected and supported items are shown with a 'Confirm' button below
        /// </summary>
        private async void InstallContent(InstalledGame game)
        {
            Log.Information("Opening window for installing content");
            InstallContent installContent = new InstallContent(game);
            await installContent.WaitForCloseAsync();
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
            contextMenu.Items.Add(CreateMenuItem("Launch in Windowed Mode", "Start the game in a window instead of fullscreen", async (sender, e) =>
            {
                await LaunchGame(game, true);
                await LoadGames();
            }));

            if (game.EmulatorVersion != "Custom")
            {
                // 'Content' option
                MenuItem contentMenu = new MenuItem { Header = "Content"};
                // Add 'Install content' option
                contentMenu.Items.Add(CreateMenuItem("Install DLC/Updates", $"Install various game content like DLC, Title Updates etc.", (sender, e) => InstallContent(game)));

                // Add 'Show installed content' option
                contentMenu.Items.Add(CreateMenuItem("View Installed Content", $"Allows the user to see what's installed in game content folder and to export save files", async (sender, e) =>
                {
                    Log.Information("Opening 'ShowInstalledContent' window");
                    ShowInstalledContent showInstalledContent = new ShowInstalledContent(game);
                    await showInstalledContent.WaitForCloseAsync();
                }));

                // Add 'Content' MenuItem to the ContextMenu
                contextMenu.Items.Add(contentMenu);
            }

            // Check what version of Xenia the game uses
            switch (game.EmulatorVersion)
            {
                case "Stable":
                    break;
                case "Canary":
                case "Netplay":
                    // 'Game Patch' option
                    MenuItem gamePatchOptions = new MenuItem { Header = "Game Patch" };
                    // Check if the game has any game patches installed
                    if (game.PatchFilePath != null)
                    {
                        // Add "Add Additional Patches" option
                        gamePatchOptions.Items.Add(CreateMenuItem("Add More Patches", "Add additional patches to the existing patch file from another local file\nNOTE: Useful if you have a patch file that is not in game-patches repository", (sender, e) =>
                        {
                            Log.Information("Open file dialog");
                            OpenFileDialog openFileDialog = new OpenFileDialog();
                            openFileDialog.Title = "Select a game";
                            openFileDialog.Filter = "Supported Files|*.toml";
                            openFileDialog.Multiselect = true;
                            bool? result = openFileDialog.ShowDialog();
                            if (result == true)
                            {
                                foreach (string file in openFileDialog.FileNames)
                                {
                                    AddAdditionalPatches(game.PatchFilePath, file);
                                }
                            }
                        }));

                        // Add "Patch Settings" option
                        gamePatchOptions.Items.Add(CreateMenuItem("Manage Patches", "Enable or disable game patches", async (sender, e) =>
                        {
                            // Opens EditGamePatch window
                            EditGamePatch editGamePatch = new EditGamePatch(game);
                            editGamePatch.Show();
                            await editGamePatch.WaitForCloseAsync();
                        }));

                        // Check if current patch is outdated and if it is add "Update Patch" option
                        string[] split = Path.GetFileName(game.PatchFilePath).Split('-');
                        string patchHash = App.ComputeGitSha1(game.PatchFilePath);
                        foreach (GamePatch patch in App.gamePatches)
                        {
                            if (patch.gameName == Path.GetFileName(game.PatchFilePath))
                            {
                                // Add "Update Patch" option
                                gamePatchOptions.Items.Add(CreateMenuItem("Update patch", "Allows the user to update the currently installed patches to the latest version\nNOTE: This will disable all of the enabled patches", async (sender, e) =>
                                {
                                    await UpdateGamePatch(patch.url, Path.Combine(App.baseDirectory, game.PatchFilePath));
                                    await LoadGames();
                                    MessageBox.Show($"{game.Title} patch has been updated.");
                                }));
                            }
                        }

                        // Add "Remove Game Patch" option
                        gamePatchOptions.Items.Add(CreateMenuItem("Remove Current Patch", "Allows the user to remove the game patch from Xenia", async (sender, e) => await RemoveGamePatch(game)));

                    }
                    else
                    {
                        // Add "Add game patch" option
                        gamePatchOptions.Items.Add(CreateMenuItem("Download & Apply Patch", "Downloads and installs a selected game patch from the game-patches repository", async (sender, e) => await AddGamePatch(game)));
                    }
                    contextMenu.Items.Add(gamePatchOptions);
                    break;
                default:
                    break;
            }

            // Add "Add shortcut to desktop" option
            contextMenu.Items.Add(CreateMenuItem("Create Desktop Shortcut", null, (sender, e) =>
            {
                string IconLocation;
                string workingDirectory;
                if (game.ShortcutIconFilePath != null)
                {
                    IconLocation = game.ShortcutIconFilePath;
                }
                else
                {
                    IconLocation = game.BoxartFilePath;
                }
                // Checking what emulator the game uses
                switch (game.EmulatorVersion)
                {
                    case "Stable":
                        workingDirectory = Path.Combine(App.baseDirectory, App.appConfiguration.XeniaStable.EmulatorLocation);
                        break;
                    case "Canary":
                        workingDirectory = Path.Combine(App.baseDirectory, App.appConfiguration.XeniaCanary.EmulatorLocation);
                        break;
                    case "Netplay":
                        workingDirectory = Path.Combine(App.baseDirectory, App.appConfiguration.XeniaNetplay.EmulatorLocation);
                        break;
                    case "Custom":
                        workingDirectory = Path.GetDirectoryName(game.EmulatorExecutableLocation);
                        break;
                    default:
                        break;
                }
                ShortcutCreator.CreateShortcutOnDesktop(game.Title, Path.Combine(App.baseDirectory, "Xenia Manager.exe"), App.baseDirectory, $@"""{game.Title}""", Path.Combine(App.baseDirectory, IconLocation));
            }));

            // Add "Open Compatibility Page" option
            if (game.GameCompatibilityURL != null)
            {
                contextMenu.Items.Add(CreateMenuItem("Check Compatibility Info", null, (sender, e) =>
                {
                    ProcessStartInfo compatibilityPageURL = new ProcessStartInfo(game.GameCompatibilityURL) { UseShellExecute = true };
                    Process.Start(compatibilityPageURL);
                }));
            }

            // Add "Edit Game" option
            contextMenu.Items.Add(CreateMenuItem("Edit Game Details", "Opens a window where you can edit game name and icon", async (sender, e) =>
            {
                Log.Information("Opening 'EditGameInfo' window");
                EditGameInfo editGameInfo = new EditGameInfo(game);
                editGameInfo.Show();
                await editGameInfo.WaitForCloseAsync();
                await LoadGames();
                await SaveGames();
            }));

            // Add "Delete game" option
            contextMenu.Items.Add(CreateMenuItem("Remove from Xenia Manager", "Deletes the game from Xenia Manager", async (sender, e) => await RemoveGame(game)));

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
                Mouse.OverrideCursor = Cursors.Wait;
                foreach (InstalledGame game in orderedGames)
                {
                    // Create a new button for the game
                    Button button = new Button();
                    Log.Information($"Adding {game.Title} to the Library");

                    // Checking if the game has compatibility rating
                    if (game.CompatibilityRating == null && game.GameCompatibilityURL != null && (DateTime.Now - App.appConfiguration.Manager.LastUpdateCheckDate.Value).TotalDays >= 1)
                    {
                        await GetCompatibilityRating(game);
                        await SaveGames();
                    }
                    else
                    {
                        if (game.GameCompatibilityURL == null)
                        {
                            game.CompatibilityRating = "Unknown";
                        }
                    }

                    // Creating image for the game button
                    button.Content = await CreateButtonContent(game);

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
                    // Applying the tooltip
                    TextBlock tooltip = new TextBlock{TextAlignment = TextAlignment.Center};
                    //tooltip.Inlines.Add(new Run("Game Name: ") { FontWeight = FontWeights.Bold });
                    tooltip.Inlines.Add(new Run(game.Title + "\n") { FontWeight = FontWeights.Bold});
                    tooltip.Inlines.Add(new Run(game.CompatibilityRating) { FontWeight = FontWeights.Bold, TextDecorations = TextDecorations.Underline});
                    switch (game.CompatibilityRating)
                    {
                        case "Unplayable":
                            tooltip.Inlines.Add(new Run(" (The game either doesn't start or it crashes a lot)"));
                            break;
                        case "Loads":
                            tooltip.Inlines.Add(new Run(" (The game loads, but crashes in the title screen or main menu)"));
                            break;
                        case "Gameplay":
                            tooltip.Inlines.Add(new Run(" (Gameplay loads, but it may be unplayable)"));
                            break;
                        case "Playable":
                            tooltip.Inlines.Add(new Run(" (The game can be reasonably played from start to finish with little to no issues)"));
                            break;
                        default:
                            break;
                    }
                    if (game.Playtime != null)
                    {
                        string FormattedPlaytime = "";
                        if (game.Playtime == 0)
                        {
                            FormattedPlaytime = "Never played";
                        }
                        else if (game.Playtime < 60)
                        {
                            FormattedPlaytime = $"{game.Playtime:N0} minutes";
                        }
                        else
                        {
                            FormattedPlaytime = $"{(game.Playtime/60):N1} hours";
                        }
                        tooltip.Inlines.Add(new Run("\n" + "Time played:") { FontWeight = FontWeights.Bold, TextDecorations = TextDecorations.Underline });
                        tooltip.Inlines.Add(new Run($" {FormattedPlaytime}"));
                    }
                    else
                    {
                        tooltip.Inlines.Add(new Run("\n" + "Time played:") { FontWeight = FontWeights.Bold, TextDecorations = TextDecorations.Underline });
                        tooltip.Inlines.Add(new Run(" Never played"));
                    }
                    button.ToolTip = tooltip;

                    wrapPanel.Children.Add(button); // Add the game to the Warp Panel

                    // When button loads
                    button.Loaded += (sender, e) =>
                    {
                        InitializeContextMenu(button, game); // Creates ContextMenu
                    };
                }
                Mouse.OverrideCursor = null;
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
                switch (XeniaVersion)
                {
                    case "Stable":
                        xenia.StartInfo.FileName = Path.Combine(App.baseDirectory, App.appConfiguration.XeniaStable.ExecutableLocation);
                        xenia.StartInfo.WorkingDirectory = Path.Combine(App.baseDirectory, App.appConfiguration.XeniaStable.EmulatorLocation);
                        break;
                    case "Canary":
                        xenia.StartInfo.FileName = Path.Combine(App.baseDirectory, App.appConfiguration.XeniaCanary.ExecutableLocation);
                        xenia.StartInfo.WorkingDirectory = Path.Combine(App.baseDirectory, App.appConfiguration.XeniaCanary.EmulatorLocation);
                        break;
                    case "Netplay":
                        xenia.StartInfo.FileName = Path.Combine(App.baseDirectory, App.appConfiguration.XeniaNetplay.ExecutableLocation);
                        xenia.StartInfo.WorkingDirectory = Path.Combine(App.baseDirectory, App.appConfiguration.XeniaNetplay.EmulatorLocation);
                        break;
                    default:
                        break;
                }
                xenia.StartInfo.Arguments = $@"""{selectedFilePath}""";
                xenia.Start();
                xenia.WaitForInputIdle();

                string gameTitle = "";
                string game_id = "";
                string media_id = "";

                Process process = Process.GetProcessById(xenia.Id);
                Log.Information("Trying to find the game title from Xenia Window Title");
                int NumberOfTries = 0;

                // Method 1 using Xenia Window Title
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

                // Method 2 using Xenia.log (In case method 1 fails)
                if (File.Exists(xenia.StartInfo.WorkingDirectory + "xenia.log"))
                {
                    using (FileStream fs = new FileStream(xenia.StartInfo.WorkingDirectory + "xenia.log", FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    using (StreamReader sr = new StreamReader(fs))
                    {
                        string line;
                        while ((line = sr.ReadLine()) != null)
                        {
                            switch (true)
                            {
                                case var _ when line.Contains("Title name"):
                                    {
                                        string[] split = line.Split(':');
                                        Log.Information($"Title: {split[1].TrimStart()}");
                                        if (gameTitle == "Not found")
                                        {
                                            gameTitle = split[1].TrimStart();
                                        }
                                        break;
                                    }
                                case var _ when line.Contains("Title ID"):
                                    {
                                        string[] split = line.Split(':');
                                        Log.Information($"Title ID: {split[1].TrimStart()}");
                                        game_id = split[1].TrimStart();
                                        if (game_id == "Not found")
                                        {
                                            game_id = split[1].TrimStart();
                                        }
                                        break;
                                    }
                                case var _ when line.Contains("Media ID"):
                                    {
                                        string[] split = line.Split(':');
                                        Log.Information($"Media ID: {split[1].TrimStart()}");
                                        media_id = split[1].TrimStart();
                                        break;
                                    }
                            }
                        }
                    }
                }

                Log.Information("Game Title: " + gameTitle);
                Log.Information("Game ID: " + game_id);
                Log.Information("Media ID: " + media_id);

                EmulatorInfo emulator = new EmulatorInfo();
                switch (XeniaVersion)
                {
                    case "Stable":
                        emulator = App.appConfiguration.XeniaStable;
                        break;
                    case "Canary":
                        emulator = App.appConfiguration.XeniaCanary;
                        break;
                    case "Netplay":
                        emulator = App.appConfiguration.XeniaNetplay;
                        break;
                    default:
                        break;
                }

                SelectGame sd = new SelectGame(this, gameTitle, game_id, media_id, selectedFilePath, XeniaVersion, emulator);
                sd.Show();
                await sd.WaitForCloseAsync();
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message + "\nFull Error:\n" + ex);
                MessageBox.Show(ex.Message);
                return;
            }
        }

        /// <summary>
        /// Opens <see cref="OpenFileDialog"> where user selects the game
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
                        await TryAddGame(game, false, true);
                    }
                }

                RefreshList();
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message + "\nFull Error:\n" + ex);
                MessageBox.Show(ex.Message);
                return;
            }
        }

        /// <summary>
        /// Opens <see cref="OpenFolderDialog"> where user selects a path containing multiple games
        /// </summary>
        private async void ScanGames_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Open file dialog
                Log.Information("Open directory dialog");

                OpenFolderDialog openFolderDialog = new OpenFolderDialog();
                openFolderDialog.Title = "Select a directory to scan";
                bool? result = openFolderDialog.ShowDialog();
                if (result == true)
                {
                    foreach (var folder in Directory.EnumerateDirectories(openFolderDialog.FolderName, "*", new EnumerationOptions() { MaxRecursionDepth = 3, RecurseSubdirectories = true}))
                    {
                        string[] systemEntries = Directory.GetFileSystemEntries(folder);

                        //Stock console method
                        if (systemEntries.Length != 2) //A game path contains only 1 folder (the game content) & 1 file (the game)
                            continue;

                        string[] directories = Directory.GetDirectories(folder);
                        string[] files = Directory.GetFiles(folder);
                        if (directories.Length != 1 || files.Length != 1)
                            continue;

                        await TryAddGame(files[0], false, false);
                    }
                }

                RefreshList();
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message + "\nFull Error:\n" + ex);
                MessageBox.Show(ex.Message);
                return;
            }
        }

        /// <summary>
        /// Tries to add a game from a file path. Does nothing if the file path is invalid/not a game
        /// </summary>
        private async Task TryAddGame(string filePath, bool refreshList = true, bool allowDuplicates = true)
        {
            if (!allowDuplicates)
            {
                foreach (var game in Games)
                {
                    if (filePath == game.GameFilePath)
                        return;
                }
            }

            List<string> availableXeniaVersions = new List<string>();

            if (App.appConfiguration.XeniaStable != null) availableXeniaVersions.Add("Stable");
            if (App.appConfiguration.XeniaCanary != null) availableXeniaVersions.Add("Canary");
            if (App.appConfiguration.XeniaNetplay != null) availableXeniaVersions.Add("Netplay");

            switch (availableXeniaVersions.Count)
            {
                case 0:
                    Log.Information("No Xenia installations detected");
                    break;
                case 1:
                    Log.Information($"Only Xenia {availableXeniaVersions[0]} is installed");
                                await GetGameTitle(filePath, availableXeniaVersions[0]);
                    break;
                default:
                    Log.Information("Detected multiple Xenia installations");
                    Log.Information("Asking user what Xenia version will the game use");
                    XeniaSelection xs = new XeniaSelection();
                    await xs.WaitForCloseAsync();
                    Log.Information($"User selected Xenia {xs.UserSelection}");
                                await GetGameTitle(filePath, xs.UserSelection);
                    break;
            }

            if (refreshList)
                RefreshList();
        }

        /// <summary>
        /// Refreshes the list of games
        /// </summary>
        private async void RefreshList()
        {
            await LoadGames();
            await SaveGames();
        }
    }
}
