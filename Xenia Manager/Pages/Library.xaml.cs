using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.IO;


// Imported
using Microsoft.Win32;
using Serilog;
using Xenia_Manager.Classes;
using Xenia_Manager.Windows;
using Newtonsoft.Json;
using System.Runtime.InteropServices;

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
        private async Task GetGameTitle(string selectedFilePath)
        {
            try
            {
                Log.Information("Launching game with Xenia to find the name of the game");
                Process xenia = new Process();
                xenia.StartInfo.FileName = App.appConfiguration.EmulatorLocation + "xenia_canary.exe";
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

                xenia.CloseMainWindow();
                xenia.Close();
                xenia.Dispose();

                Log.Information("Game found");
                Log.Information("Game Title: " + gameTitle);
                Log.Information("Game ID: " + game_id);

                SelectGame sd = new SelectGame(this, gameTitle, game_id, selectedFilePath);
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

                        // Box art of the game
                        Image image = new Image
                        {
                            Source = new BitmapImage(new Uri(game.IconFilePath)),
                            Stretch = Stretch.UniformToFill
                        };

                        // Black border for rounded edges
                        Border border = new Border
                        {
                            CornerRadius = new CornerRadius(10),
                            Child = image
                        };

                        button.Content = border;

                        // When user clicks on the game, launch the game
                        button.Click += async (sender, e) =>
                        {
                            MainWindow mainWindow = Application.Current.MainWindow as MainWindow;
                            mainWindow.FadeOutAnimation();
                            Log.Information($"Launching {game.Title} in fullscreen mode");
                            Process xenia = new Process();
                            xenia.StartInfo.FileName = App.appConfiguration.EmulatorLocation + @"xenia_canary.exe";
                            xenia.StartInfo.Arguments = $@"""{game.GameFilePath}"" --config ""{game.ConfigFilePath}""";
                            xenia.Start();
                            Log.Information("Emulator started");
                            await xenia.WaitForExitAsync();
                            Log.Information("Emulator closed");
                            mainWindow.FadeInAnimation();
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
                                MainWindow mainWindow = Application.Current.MainWindow as MainWindow;
                                mainWindow.FadeOutAnimation();
                                Log.Information($"Launching {game.Title} in windowed mode");
                                Process xenia = new Process();
                                xenia.StartInfo.FileName = App.appConfiguration.EmulatorLocation + @"xenia_canary.exe";
                                xenia.StartInfo.Arguments = $@"""{game.GameFilePath}"" --config ""{game.ConfigFilePath}"" --fullscreen=false";
                                xenia.Start();
                                Log.Information("Emulator started");
                                await xenia.WaitForExitAsync();
                                Log.Information("Emulator closed");
                                mainWindow.FadeInAnimation();
                            };
                            contextMenu.Items.Add(WindowedMode); // Add the item to the ContextMenu

                            // Create a Desktop Shortcut
                            MenuItem CreateShortcut = new MenuItem();
                            CreateShortcut.Header = "Create shortcut on desktop"; // Text that shows in the context menu

                            // If this is selected, Create a shortcut of the game on desktop
                            CreateShortcut.Click += (sender, e) => 
                            {                                
                                ShortcutCreator.CreateShortcutOnDesktop(game.Title, Path.Combine(App.appConfiguration.EmulatorLocation, "xenia_canary.exe"), App.appConfiguration.EmulatorLocation, $@"""{game.GameFilePath}"" --config ""{game.ConfigFilePath}""", game.IconFilePath);
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
                    await GetGameTitle(openFileDialog.FileName);
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
