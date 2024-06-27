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
                //await SaveGames();
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
