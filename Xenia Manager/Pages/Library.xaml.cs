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

                        // Black border for rounded edges
                        Border border = new Border
                        {
                            CornerRadius = new CornerRadius(10),
                            Child = image
                        };

                        button.Content = border;

                        // Animations
                        MainWindow mainWindow = Application.Current.MainWindow as MainWindow;
                        DoubleAnimation fadeOutAnimation = new DoubleAnimation(1, 0, TimeSpan.FromSeconds(0.2));
                        DoubleAnimation fadeInAnimation = new DoubleAnimation(0, 1, TimeSpan.FromSeconds(0.2));

                        // When user clicks on the game, launch the game
                        button.Click += async (sender, e) =>
                        {
                            mainWindow.BeginAnimation(Window.OpacityProperty, fadeOutAnimation);
                            Log.Information($"Launching {game.Title}");
                            Process xenia = new Process();
                            if (game.EmulatorVersion == "Stable")
                            {
                                xenia.StartInfo.FileName = App.appConfiguration.XeniaStable.EmulatorLocation + @"xenia.exe";
                            }
                            else
                            {
                                xenia.StartInfo.FileName = App.appConfiguration.XeniaCanary.EmulatorLocation + @"xenia_canary.exe";
                            }
                            xenia.StartInfo.Arguments = $@"""{game.GameFilePath}"" --config ""{game.ConfigFilePath}""";
                            xenia.Start();
                            Log.Information("Emulator started");
                            await xenia.WaitForExitAsync();
                            Log.Information("Emulator closed");
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

                            // Windowed mode
                            MenuItem WindowedMode = new MenuItem();
                            WindowedMode.Header = "Play game in windowed mode"; // Text that shows in the context menu
                            
                            // If this is selected, open the game in windowed mode
                            WindowedMode.Click += async (sender, e) =>
                            {
                                mainWindow.BeginAnimation(Window.OpacityProperty, fadeOutAnimation);
                                Log.Information($"Launching {game.Title} in windowed mode");
                                Process xenia = new Process();
                                if (game.EmulatorVersion == "Stable")
                                {
                                    xenia.StartInfo.FileName = App.appConfiguration.XeniaStable.EmulatorLocation + @"xenia.exe";
                                }
                                else
                                {
                                    xenia.StartInfo.FileName = App.appConfiguration.XeniaCanary.EmulatorLocation + @"xenia_canary.exe";
                                }
                                xenia.StartInfo.Arguments = $@"""{game.GameFilePath}"" --config ""{game.ConfigFilePath}"" --fullscreen=false";
                                xenia.Start();
                                Log.Information("Emulator started");
                                await xenia.WaitForExitAsync();
                                Log.Information("Emulator closed");
                                mainWindow.BeginAnimation(Window.OpacityProperty, fadeInAnimation);
                            };
                            contextMenu.Items.Add(WindowedMode); // Add the item to the ContextMenu

                            // Create a Desktop Shortcut
                            MenuItem CreateShortcut = new MenuItem();
                            CreateShortcut.Header = "Create shortcut on desktop"; // Text that shows in the context menu

                            // If this is selected, Create a shortcut of the game on desktop
                            CreateShortcut.Click += (sender, e) => 
                            {
                                if (game.EmulatorVersion == "Stable")
                                {
                                    ShortcutCreator.CreateShortcutOnDesktop(game.Title, Path.Combine(App.appConfiguration.XeniaStable.EmulatorLocation), App.appConfiguration.XeniaStable.EmulatorLocation, $@"""{game.GameFilePath}"" --config ""{game.ConfigFilePath}""", game.IconFilePath);
                                }
                                else
                                {
                                    ShortcutCreator.CreateShortcutOnDesktop(game.Title, Path.Combine(App.appConfiguration.XeniaCanary.EmulatorLocation), App.appConfiguration.XeniaCanary.EmulatorLocation, $@"""{game.GameFilePath}"" --config ""{game.ConfigFilePath}""", game.IconFilePath);
                                }
                            };
                            contextMenu.Items.Add(CreateShortcut); // Add the item to the ContextMenu

                            // Remove game from Xenia Manager
                            MenuItem RemoveGame = new MenuItem();
                            RemoveGame.Header = "Remove game"; // Text that shows in the context menu

                            // If this is selected, ask the user if he really wants to remove the game from the Xenia Manager 
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
                            string saveGamePath;
                            if (game.EmulatorVersion == "Canary")
                            {
                                saveGamePath = App.appConfiguration.XeniaCanary.EmulatorLocation + $@"content\{game.GameId}\00000001";
                            }
                            else
                            {
                                saveGamePath = App.appConfiguration.XeniaStable.EmulatorLocation + $@"content\{game.GameId}\00000001";
                            }
                            // Checks if the save file is there
                            if (Directory.Exists(saveGamePath))
                            {
                                MenuItem BackupSaveFile = new MenuItem();
                                BackupSaveFile.Header = "Export the save file";
                                BackupSaveFile.ToolTip = "Exports the game's save file as a .zip to the desktop";
                                BackupSaveFile.Click += async (sender, e) =>
                                {
                                    Mouse.OverrideCursor = Cursors.Wait;
                                    ZipFile.CreateFromDirectory(saveGamePath, Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), $"{DateTime.Now.ToString("yyyyMMdd_HHmmss")} - {game.Title} Save File.zip"));
                                    Mouse.OverrideCursor = null;
                                    Log.Information($"The save file for '{game.Title}' has been successfully exported to the desktop");
                                    MessageBox.Show($"The save file for '{game.Title}' has been successfully exported to the desktop");
                                    await Task.Delay(1);
                                };

                                contextMenu.Items.Add(BackupSaveFile);
                            }

                            // Check if the game is using Xenia Canary (for game patches since Stable doesn't support them)
                            if (game.EmulatorVersion == "Canary")
                            {
                                // Check if the game has any patches installed
                                if (game.PatchFilePath != null)
                                {
                                    // If it does, add "Edit game patch" and "Remove game patch" to the ContextMenu
                                    // Remove gamepatch from Xenia Emulator
                                    MenuItem EditGamePatch = new MenuItem();
                                    EditGamePatch.Header = "Edit game patch"; // Text that shows in the context menu

                                    // If this is selected, open the window with all of the patch settings loaded
                                    EditGamePatch.Click += async (sender, e) =>
                                    {
                                        EditGamePatch editGamePatch = new EditGamePatch(game);
                                        editGamePatch.Show();
                                        await editGamePatch.WaitForCloseAsync();
                                    };
                                    contextMenu.Items.Add(EditGamePatch); // Add the item to the ContextMenu

                                    // Remove gamepatch from Xenia Emulator
                                    MenuItem RemoveGamePatch = new MenuItem();
                                    RemoveGamePatch.Header = "Remove game patch"; // Text that shows in the context menu

                                    // If this is selected, ask the user if he really wants to remove the game patch from Xenia Emulator
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
                                    MenuItem AddGamePatch = new MenuItem();
                                    AddGamePatch.Header = "Add game patch";// Text that shows in the context menu

                                    // If this is selected, ask the user if he already downloaded the game patch
                                    AddGamePatch.Click += async (sender, e) =>
                                    {
                                        if (!Directory.Exists(App.appConfiguration.EmulatorLocation + @"patches\"))
                                        {
                                            Directory.CreateDirectory(App.appConfiguration.EmulatorLocation + @"patches\");
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
                                                System.IO.File.Copy(openFileDialog.FileName, App.appConfiguration.EmulatorLocation + @$"patches\{Path.GetFileName(openFileDialog.FileName)}", true);
                                                Log.Information("Copying the file to the patches folder.");
                                                System.IO.File.Delete(openFileDialog.FileName);
                                                Log.Information("Deleting the original file.");
                                                game.PatchFilePath = App.appConfiguration.EmulatorLocation + @$"patches\{Path.GetFileName(openFileDialog.FileName)}";
                                                MessageBox.Show($"{game.Title} patch has been installed");
                                            }
                                        }
                                        else
                                        {
                                            // If user doesn't have the patch locally, check on Xenia Canary patches list if the game has any patches
                                            SelectGamePatch selectGamePatch = new SelectGamePatch(game);
                                            selectGamePatch.Show();
                                            await selectGamePatch.WaitForCloseAsync();
                                            MessageBox.Show($"{game.Title} patch has been installed");
                                        }

                                        // Reload the UI
                                        await LoadGames();
                                        await SaveGames(); // Save changes in the .JSON file
                                    };
                                    contextMenu.Items.Add(AddGamePatch); // Add the item to the ContextMenu
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
