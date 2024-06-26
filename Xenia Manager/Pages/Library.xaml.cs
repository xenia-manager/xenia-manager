using System;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;

// Imported
using Microsoft.Win32;
using Serilog;
using Xenia_Manager.Windows;

namespace Xenia_Manager.Pages
{
    /// <summary>
    /// Interaction logic for Library.xaml
    /// </summary>
    public partial class Library : Page
    {
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

                SelectGame sd = new SelectGame();
                sd.ShowDialog();
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
                //await LoadGames();
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
