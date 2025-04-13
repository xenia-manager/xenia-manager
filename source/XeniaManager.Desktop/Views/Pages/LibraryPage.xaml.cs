using System.IO;
using System.Runtime.InteropServices.JavaScript;
using System.Windows;
using System.Windows.Input;

// Imported
using Microsoft.Win32;
using Octokit;
using Wpf.Ui;
using Wpf.Ui.Controls;
using XeniaManager.Core;
using XeniaManager.Core.Game;
using XeniaManager.Core.Installation;
using XeniaManager.Desktop.Components;
using XeniaManager.Desktop.Utilities;
using XeniaManager.Desktop.Views.Windows;
using Page = System.Windows.Controls.Page;

namespace XeniaManager.Desktop.Views.Pages;

/// <summary>
/// Interaction logic for LibraryPage.xaml
/// </summary>
public partial class LibraryPage : Page
{
    // Variables
    /// <summary>
    /// Check to only have update notification appear once per Xenia Manager launch
    /// </summary>
    private bool _showUpdateNotification = true;

    /// <summary>
    /// 
    /// </summary>
    private readonly SnackbarService _updateNotification = new SnackbarService();

    /// <summary>
    /// Contains all the games being displayed in the WrapPanel
    /// </summary>
    private IOrderedEnumerable<Game> _games { get; set; }

    // Constructor
    public LibraryPage()
    {
        InitializeComponent();
        Loaded += (sender, args) =>
        {
            UpdateUI();
            LoadGames();
            App.Settings.ClearCache(); // Clear cache after loading the games
        };
        UpdateCompatibilityRatings();
        CheckForXeniaUpdates();
    }

    // Functions
    /// <summary>
    /// Updates the UI based on the selected settings in the configuration file
    /// </summary>
    private void UpdateUI()
    {
        // Update the "Display Game Title" Button Icon
        if (App.Settings.Ui.DisplayGameTitle)
        {
            BtnDisplayGameTitle.Icon = new SymbolIcon { Symbol = SymbolRegular.Eye24 };
        }
        else
        {
            BtnDisplayGameTitle.Icon = new SymbolIcon { Symbol = SymbolRegular.EyeOff24 };
        }
    }

    /// <summary>
    /// Loads the games into the WrapPanel
    /// </summary>
    public void LoadGames()
    {
        WpGameLibrary.Children.Clear();
        Logger.Info("Loading games into the UI");
        if (GameManager.Games.Count <= 0)
        {
            Logger.Info("No games found.");
            return;
        }

        Mouse.OverrideCursor = Cursors.Wait;
        _games = GameManager.Games.OrderBy(game => game.Title);
        foreach (Game game in _games)
        {
            Logger.Info($"Adding {game.Title} to the library");
            try
            {
                LibraryGameButton gameButton = new LibraryGameButton(game, this);
                WpGameLibrary.Children.Add(gameButton);
            }
            catch (Exception ex)
            {
                Logger.Error($"{ex.Message}\n{ex.StackTrace}");
                CustomMessageBox.Show(ex);
            }
        }

        Mouse.OverrideCursor = null;
    }

    /// <summary>
    /// Checks for Xenia emulator updates
    /// </summary>
    private async void CheckForXeniaUpdates()
    {
        try
        {
            bool updateAvailable = false;
            string xeniaVersionUpdateAvailable = string.Empty;

            // Xenia Canary
            // Checking if it's installed
            if (App.Settings.Emulator.Canary != null)
            {
                if (App.Settings.Emulator.Canary.UpdateAvailable)
                {
                    // Show Snackbar
                    updateAvailable = true;
                    xeniaVersionUpdateAvailable += XeniaVersion.Canary;
                }
                // Check if we need to do an update check
                else if ((DateTime.Now - App.Settings.Emulator.Canary.LastUpdateCheckDate).TotalDays >= 1)
                {
                    Logger.Info("Checking for Xenia Canary updates.");
                    (bool, Release) canaryUpdate = await Xenia.CheckForUpdates(App.Settings.Emulator.Canary, XeniaVersion.Canary);
                    if (canaryUpdate.Item1)
                    {
                        // Show Snackbar
                        updateAvailable = true;
                        xeniaVersionUpdateAvailable += XeniaVersion.Canary;
                    }
                }
            }

            // TODO: Add checking for updates for Mousehook and Netplay


            // Show update notification
            if (updateAvailable && _showUpdateNotification)
            {
                _updateNotification.SetSnackbarPresenter(SbXeniaUpdateNotification);
                _updateNotification.Show(LocalizationHelper.GetUiText("SnackbarPresenter_XeniaUpdateAvailableTitle"),
                    $"{LocalizationHelper.GetUiText("SnackbarPresenter_XeniaUpdateAvailableText")} {xeniaVersionUpdateAvailable}",
                    ControlAppearance.Info, null, TimeSpan.FromSeconds(5));
                _showUpdateNotification = false;
            }

            App.AppSettings.SaveSettings();
        }
        catch (Exception ex)
        {
            Logger.Error(ex);
            await CustomMessageBox.Show(ex);
        }
    }

    /// <summary>
    /// Updates Compatibility ratings
    /// </summary>
    private async void UpdateCompatibilityRatings()
    {
        if ((DateTime.Now - App.Settings.UpdateCheckChecks.CompatibilityCheck).TotalDays <= 1)
        {
            return;
        }
        
        Logger.Info("Updating compatibility ratings");
        await CompatibilityManager.UpdateCompatibility();
        App.Settings.UpdateCheckChecks.CompatibilityCheck = DateTime.Now;
    }

    /// <summary>
    /// Shows/Hides game title on the boxart
    /// </summary>
    private void BtnDisplayGameTitle_Click(object sender, RoutedEventArgs e)
    {
        // Invert the option
        App.Settings.Ui.DisplayGameTitle = !App.Settings.Ui.DisplayGameTitle;

        // Reload UI
        UpdateUI();
        LoadGames();

        // Save changes
        App.AppSettings.SaveSettings();
    }

    /// <summary>
    /// Adds the games
    /// </summary>
    private async void BtnAddGame_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            List<XeniaVersion> availableVersions = App.Settings.GetInstalledVersions();
            XeniaVersion xeniaVersion = XeniaVersion.Canary;
            switch (availableVersions.Count)
            {
                case 0:
                    throw new Exception("No Xenia version installed.\nInstall Xenia before continuing."); 
                case 1:
                    Logger.Info($"There is only 1 Xenia version installed: {availableVersions[0]}");
                    xeniaVersion = availableVersions[0];
                    break;
                default:
                    // TODO: Add the ability to choose what version of Xenia the game will use
                    throw new NotImplementedException();
                    
            }
            using (new WindowDisabler(this))
            {
                Logger.Info("Opening file dialog");
                OpenFileDialog openFileDialog = new OpenFileDialog
                {
                    Title = LocalizationHelper.GetUiText("OpenFileDialog_SelectGameTitle"),
                    Filter = "All Files|*|Supported Files|*.iso;*.xex;*.zar",
                    Multiselect = true
                };

                bool? result = openFileDialog.ShowDialog();
                if (result == false)
                {
                    Logger.Info("Cancelling adding of games");
                    return;
                }

                foreach (string gamePath in openFileDialog.FileNames)
                {
                    Logger.Debug($"File Name: {Path.GetFileName(gamePath)}");
                    (string gameTitle, string gameId, string mediaId) = ("Not found", "Not found", "");
                    // TODO: Add getting game details without Xenia
                    (gameTitle, gameId, mediaId) = await GameManager.GetGameDetailsWithXenia(gamePath, xeniaVersion);
                    Logger.Info($"Title: {gameTitle}, Game ID: {gameId}, Media ID: {mediaId}");
                    GameDatabaseWindow gameDatabaseWindow = new GameDatabaseWindow(gameTitle, gameId, mediaId, gamePath, xeniaVersion);
                    gameDatabaseWindow.ShowDialog();
                }
            }

            // Reload the UI to show added game
            LoadGames();
        }
        catch (Exception ex)
        {
            Logger.Error(ex);
            await CustomMessageBox.Show(ex);
        }
    }
}