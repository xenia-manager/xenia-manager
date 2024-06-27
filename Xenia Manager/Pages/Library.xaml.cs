using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;

using System.Windows.Media;


// Imported
using Microsoft.Win32;
using Serilog;
using Xenia_Manager.Classes;
using Xenia_Manager.Windows;
using Newtonsoft.Json;

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
                    Regex titleRegex = new Regex(@"\]\s+(.+)\s+<");
                    Regex idRegex = new Regex(@"\[([A-Z0-9]+)\s+v\d+\.\d+\]");

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
                if (Games != null && Games.Count > 0)
                {
                    var orderedGames = Games.OrderBy(game => game.Title);
                    foreach (var game in orderedGames)
                    {
                        var button = new Button();
                        var image = new Image
                        {
                            Source = new BitmapImage(new Uri(game.IconFilePath)),
                            Stretch = Stretch.UniformToFill
                        };

                        var border = new Border
                        {
                            CornerRadius = new CornerRadius(20),
                            Child = image
                        };

                        button.Content = border;
                        button.Click += async (sender, e) =>
                        {
                            Process xenia = new Process();
                            xenia.StartInfo.FileName = App.appConfiguration.EmulatorLocation + @"xenia_canary.exe";
                            xenia.StartInfo.Arguments = $@"""{game.GameFilePath}"" --fullscreen";
                            xenia.Start();
                            Log.Information("Emulator started.");
                            await xenia.WaitForExitAsync();
                            Log.Information("Emulator closed.");
                        };
                        button.Cursor = Cursors.Hand;
                        button.Style = (Style)FindResource("GameCoverButtons");
                        button.ToolTip = game.Title;
                        wrapPanel.Children.Add(button);
                        button.Loaded += (sender, e) =>
                        {
                            button.Width = 150;
                            button.Height = 207;
                            button.Margin = new Thickness(5);

                            ContextMenu contextMenu = new ContextMenu();

                            MenuItem WindowedMode = new MenuItem();
                            WindowedMode.Header = "Play game in windowed mode";
                            WindowedMode.Click += async (sender, e) =>
                            {
                                Process xenia = new Process();
                                xenia.StartInfo.FileName = App.appConfiguration.EmulatorLocation + @"xenia_canary.exe";
                                xenia.StartInfo.Arguments = $@"""{game.GameFilePath}""";
                                xenia.Start();
                                Log.Information("Emulator started.");
                                await xenia.WaitForExitAsync();
                                Log.Information("Emulator closed.");
                            };
                            contextMenu.Items.Add(WindowedMode);

                            MenuItem CreateShortcut = new MenuItem();
                            CreateShortcut.Header = "Create shortcut on desktop";
                            //CreateShortcut.Click += (sender, e) => CreateShortcut_Click(game);
                            contextMenu.Items.Add(CreateShortcut);

                            MenuItem RemoveGame = new MenuItem();
                            RemoveGame.Header = "Remove game";
                            RemoveGame.Click += async (sender, e) => 
                            {
                                MessageBoxResult result = MessageBox.Show($"Do you want to remove {game.Title}?", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question);
                                if (result == MessageBoxResult.Yes)
                                {
                                    Log.Information($"Removing {game.Title}.");
                                    // Remove game patch
                                    if (System.IO.File.Exists(game.PatchFilePath))
                                    {
                                        System.IO.File.Delete(game.PatchFilePath);
                                        Log.Information($"Deleted {game.Title} patch.");
                                    }

                                    // Removing the game
                                    Games.Remove(game);
                                    Log.Information($"Removing the {game.Title} from the Library.");
                                    await LoadGames();
                                    Log.Information("Reloading the library.");
                                    await SaveGames();
                                    Log.Information($"Saving the new library without {game.Title}.");
                                }
                            };
                            contextMenu.Items.Add(RemoveGame);

                            button.ContextMenu = contextMenu;
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
