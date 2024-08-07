﻿using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
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
            switch (game.EmulatorVersion)
            {
                case "Stable":
                    xenia.StartInfo.FileName = Path.Combine(App.baseDirectory, App.appConfiguration.XeniaStable.ExecutableLocation);
                    break;
                case "Canary":
                    xenia.StartInfo.FileName = Path.Combine(App.baseDirectory, App.appConfiguration.XeniaCanary.ExecutableLocation);
                    break;
                case "Netplay":
                    xenia.StartInfo.FileName = Path.Combine(App.baseDirectory, App.appConfiguration.XeniaNetplay.ExecutableLocation);
                    break;
                default:
                    break;
            }
            Log.Information($"Xenia Executable Location: {xenia.StartInfo.FileName}");

            // Adding default launch arguments
            xenia.StartInfo.Arguments = $@"""{game.GameFilePath}"" --config ""{Path.Combine(App.baseDirectory, game.ConfigFilePath)}""";
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
            xenia.Start();
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
                    System.IO.File.Delete(openFileDialog.FileName);
                    Log.Information("Deleting the original file.");
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
        /// Opens File Dialog and allows user to select Title Updates, DLC's etc.
        /// <para>Checks every selected file and tries to determine what it is.</para>
        /// Opens 'InstallContent' window where all of the selected and supported items are shown with a 'Confirm' button below
        /// </summary>
        private async void InstallContent(InstalledGame game)
        {
            Log.Information("Open file dialog so user can select the content that he wants to install");
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
                        Log.Information($"Checking if {Path.GetFileNameWithoutExtension(file)} is supported");
                        STFS stfs = new STFS(file);
                        if (stfs.SupportedFile)
                        {
                            Log.Information($"{Path.GetFileNameWithoutExtension(file)} is supported");
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
                    Log.Information("Opening window for installing content");
                    InstallContent installContent = new InstallContent(game.EmulatorVersion, gameContent);
                    await installContent.WaitForCloseAsync();
                }
            };
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

            // Add 'Install content' option
            contextMenu.Items.Add(CreateMenuItem("Install Content", $"Install various game content like DLC, Title Updates etc.", (sender, e) => InstallContent(game)));

            // Add 'Show installed content' option
            contextMenu.Items.Add(CreateMenuItem("Show Installed Content", $"Allows the user to see what's installed in game content folder and to export save files", async (sender, e) =>
            {
                Log.Information("Opening 'ShowInstalledContent' window");
                ShowInstalledContent showInstalledContent = new ShowInstalledContent(game);
                await showInstalledContent.WaitForCloseAsync();
            }));

            // Check what version of Xenia the game uses
            switch (game.EmulatorVersion)
            {
                case "Stable":
                    break;
                case "Canary":
                    // Check if the game has any game patches installed
                    if (game.PatchFilePath != null)
                    {
                        // Add "Add Additional Patches" option
                        contextMenu.Items.Add(CreateMenuItem("Add Additional Patches", "Add additional patches to the existing patch file from another local file\nNOTE: Useful if you have a patch file that is not in game-patches repository", (sender, e) =>
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
                    break;
                case "Netplay":
                    // Check if the game has any game patches installed
                    if (game.PatchFilePath != null)
                    {
                        // Add "Add Additional Patches" option
                        contextMenu.Items.Add(CreateMenuItem("Add Additional Patches", "Add additional patches to the existing patch file from another local file\nNOTE: Useful if you have a patch file that is not in game-patches repository", (sender, e) =>
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
                    break;
                default:
                    break;
            }

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

            // Add "Edit Game" option
            contextMenu.Items.Add(CreateMenuItem("Edit Game", "Opens a window where you can edit game name and icon", async (sender, e) =>
            {
                Log.Information("Opening 'EditGameInfo' window");
                EditGameInfo editGameInfo = new EditGameInfo(game);
                editGameInfo.Show();
                await editGameInfo.WaitForCloseAsync();
                await LoadGames();
                await SaveGames();
            }));

            // Add "Delete game" option
            contextMenu.Items.Add(CreateMenuItem("Delete Game", "Deletes the game from Xenia Manager", async (sender, e) => await RemoveGame(game)));

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
                if (gameTitle == "Not found" || game_id == "Not found")   
                {
                    if (File.Exists(xenia.StartInfo.WorkingDirectory + "xenia.log"))
                    {
                        using (FileStream fs = new FileStream(xenia.StartInfo.WorkingDirectory + "xenia.log", FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                        using (StreamReader sr = new StreamReader(fs))
                        {
                            string line;
                            while ((line = sr.ReadLine()) != null)
                            {
                                if (line.Contains("Title name"))
                                {
                                    string[] split = line.Split(':');
                                    Log.Information($"Title: {split[1].TrimStart()}");
                                    gameTitle = split[1].TrimStart();
                                }
                                else if (line.Contains("Title ID"))
                                {
                                    string[] split = line.Split(':');
                                    Log.Information($"ID: {split[1].TrimStart()}");
                                    game_id = split[1].TrimStart();
                                }
                            }
                        }
                    }
                }

                Log.Information("Game Title: " + gameTitle);
                Log.Information("Game ID: " + game_id);

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
                SelectGame sd = new SelectGame(this, gameTitle, game_id, selectedFilePath, XeniaVersion, emulator);
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
                                await GetGameTitle(game, availableXeniaVersions[0]);
                                break;
                            default:
                                Log.Information("Detected multiple Xenia installations");
                                Log.Information("Asking user what Xenia version will the game use");
                                XeniaSelection xs = new XeniaSelection();
                                await xs.WaitForCloseAsync();
                                Log.Information($"User selected Xenia {xs.UserSelection}");
                                await GetGameTitle(game, xs.UserSelection);
                                break;
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
