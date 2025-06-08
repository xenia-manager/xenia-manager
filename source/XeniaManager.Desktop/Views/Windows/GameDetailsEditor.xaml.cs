using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

// Imported
using ImageMagick;
using Microsoft.Win32;
using Wpf.Ui.Controls;
using XeniaManager.Core;
using XeniaManager.Core.Game;
using XeniaManager.Desktop.Components;
using EventManager = XeniaManager.Desktop.Utilities.EventManager;

namespace XeniaManager.Desktop.Views.Windows;

/// <summary>
/// Represents a window used to edit the details of a game in the Xenia Manager desktop application.
/// </summary>
/// <remarks>
/// The GameDetailsEditor class extends the FluentWindow from the Wpf.Ui.Controls library. It provides functionality
/// to modify game details such as the title. It also ensures that the changes adhere to constraints such as avoiding
/// duplicate game titles, and triggers necessary updates to the UI when changes are made.
/// </remarks>
/// <example>
/// The editor is typically opened as a modal dialog whenever a user chooses to edit the details of a game.
/// Upon closing, it validates the changes to ensure there are no duplicates and applies the changes to the game information.
/// </example>
public partial class GameDetailsEditor : FluentWindow
{
    #region Variables

    /// <summary>
    /// Represents the game details being edited in the GameDetailsEditor.
    /// This property is used to load and bind data from the provided <see cref="Game"/> instance
    /// to the user interface components in the editor window.
    /// </summary>
    private Game _game { get; set; }

    #endregion

    #region Constructors

    /// <summary>
    /// Represents a window used for viewing and editing detailed information about a game in the Xenia Manager.
    /// </summary>
    public GameDetailsEditor(Game game)
    {
        InitializeComponent();
        this._game = game;
        TbTitle.Title = $"{_game.Title} Details Editor";
        try
        {
            TbTitleIcon.Source = ArtworkManager.CacheLoadArtwork(Path.Combine(Constants.DirectoryPaths.Base, _game.Artwork.Icon));
        }
        catch (Exception ex)
        {
            Logger.Error($"{ex.Message}\nFull Error:\n{ex}");
            TbTitleIcon.Source = new BitmapImage(new Uri("pack://application:,,,/Assets/1024.png", UriKind.Absolute));
        }
        LoadContent();
    }

    #endregion

    #region Functions

    /// Loads game content into the user interface components.
    /// This method initializes UI elements with data from the associated game object.
    /// It populates text boxes with game details such as Title ID, Media ID, and Title.
    /// Additionally, it sets up the display for graphical elements such as box art and icons
    /// using the artwork data stored in the game object.
    /// Logging information is output to aid debugging and provide insight into the data being loaded.
    /// Any exceptions encountered when loading images into the UI are logged.
    /// Dependencies:
    /// - The method uses the ArtworkManager to retrieve artwork assets.
    /// - Relies on properties of the Game object to populate data.
    /// Exceptions:
    /// - Catches and logs exceptions that occur during the image loading process.
    private void LoadContent()
    {
        Logger.Info("Loading content into the UI.");
        Logger.Debug($"Title ID: {_game.GameId}");
        TxtTitleId.Text = _game.GameId ?? "N/A";
        Logger.Debug($"Media ID: {_game.MediaId}");
        TxtMediaId.Text = _game.MediaId ?? "N/A";
        Logger.Debug($"Game Title: {_game.Title}");
        TxtGameTitle.Text = _game.Title ?? "N/A";

        try
        {
            BtnBoxart.Content = new Image
            {
                Source = ArtworkManager.CacheLoadArtwork(_game.Artwork.Boxart),
                Stretch = Stretch.Fill
            };
        }
        catch (Exception ex)
        {
            Logger.Error(ex);
        }
        try
        {
            BtnIcon.Content = new Image
            {
                Source = ArtworkManager.CacheLoadArtwork(_game.Artwork.Icon),
                Stretch = Stretch.Fill
            };
        }
        catch (Exception ex)
        {
            Logger.Error($"{ex.Message}\nFull Error:\n{ex}");
        }
    }

    /// <summary>
    /// Handles the closing event of the GameDetailsEditor window.
    /// It checks for changes to the game title, validates the new title,
    /// updates the game information if necessary, and triggers a UI refresh for the game library.
    /// </summary>
    protected override void OnClosing(CancelEventArgs e)
    {
        if (_game.Title != TxtGameTitle.Text)
        {
            string newTitle = GameManager.TitleCleanup(TxtGameTitle.Text);
            Logger.Info($"New Game Title: {newTitle}");
            if (GameManager.CheckForDuplicateTitle(newTitle))
            {
                Logger.Warning("Duplicate title found");
                CustomMessageBox.Show("Duplicate title", "This title is already taken by another game. Please change it.");
                return;
            }
            else
            {
                Logger.Info("Detected game title change");
                GameManager.AdjustGameInfo(_game, newTitle);
            }
        }
        EventManager.RequestLibraryUiRefresh();
        base.OnClosing(e);
    }

    /// <summary>
    /// Handles the click event for the box art selection button.
    /// Opens a file dialog allowing the user to select an image file and processes the selected file.
    /// </summary>
    private void BtnBoxart_Click(object sender, RoutedEventArgs e)
    {
        // Create OpenFileDialog
        OpenFileDialog openFileDialog = new OpenFileDialog
        {
            // Set filter for image files
            Filter = "Image Files|*.jpg;*.jpeg;*.png;*.ico",
            Title = $"Select new boxart for {_game.Title}",
            // Allow the user to only select 1 file
            Multiselect = false
        };

        // Show the dialog and get a result
        if (openFileDialog.ShowDialog() == false)
        {
            Logger.Info("Boxart selection cancelled");
            return;
        }

        Logger.Debug($"Selected file: {Path.GetFileName(openFileDialog.FileName)}");
        if (_game.Title != TxtGameTitle.Text)
        {
            string newTitle = GameManager.TitleCleanup(TxtGameTitle.Text);
            Logger.Info("Detected game title change");
            GameManager.AdjustGameInfo(_game, newTitle);
        }

        try
        {
            ArtworkManager.ConvertArtwork(openFileDialog.FileName, Path.Combine(Constants.DirectoryPaths.Base, _game.Artwork.Boxart), MagickFormat.Png);
        }
        catch (NotSupportedException notSupportedEx)
        {
            Logger.Error($"{notSupportedEx.Message}\nFull Error:\n{notSupportedEx}");
            CustomMessageBox.Show(notSupportedEx);
            return;
        }
        catch (Exception ex)
        {
            Logger.Error($"{ex.Message}\nFull Error:\n{ex}");
            CustomMessageBox.Show(ex);
            return;
        }
        Logger.Info("New boxart is added");
        try
        {
            BtnBoxart.Content = new Image
            {
                Source = ArtworkManager.CacheLoadArtwork(Path.Combine(Constants.DirectoryPaths.Base, _game.Artwork.Boxart)),
                Stretch = Stretch.Fill
            };
        }
        catch (Exception ex)
        {
            Logger.Error($"{ex.Message}\nFull Error:\n{ex}");
        }
    }

    /// <summary>
    /// Handles the click event for the BtnIcon button, allowing the user to select a new icon for a game.
    /// </summary>
    private void BtnIcon_Click(object sender, RoutedEventArgs e)
    {
        // Create OpenFileDialog
        OpenFileDialog openFileDialog = new OpenFileDialog
        {
            // Set filter for image files
            Filter = "Image Files|*.jpg;*.jpeg;*.png;*.ico",
            Title = $"Select new icon for {_game.Title}",
            // Allow the user to only select 1 file
            Multiselect = false
        };

        // Show the dialog and get a result
        if (openFileDialog.ShowDialog() == false)
        {
            Logger.Info("Icon selection cancelled");
            return;
        }

        Logger.Debug($"Selected file: {Path.GetFileName(openFileDialog.FileName)}");
        if (_game.Title != TxtGameTitle.Text)
        {
            string newTitle = GameManager.TitleCleanup(TxtGameTitle.Text);
            Logger.Info("Detected game title change");
            GameManager.AdjustGameInfo(_game, newTitle);
        }

        try
        {
            ArtworkManager.ConvertArtwork(openFileDialog.FileName, Path.Combine(Constants.DirectoryPaths.Base, _game.Artwork.Icon), MagickFormat.Ico);
        }
        catch (NotSupportedException notSupportedEx)
        {
            Logger.Error($"{notSupportedEx.Message}\nFull Error:\n{notSupportedEx}");
            CustomMessageBox.Show(notSupportedEx);
            return;
        }
        catch (Exception ex)
        {
            Logger.Error($"{ex.Message}\nFull Error:\n{ex}");
            CustomMessageBox.Show(ex);
            return;
        }
        Logger.Info("New icon is added");
        try
        {
            BtnIcon.Content = new Image
            {
                Source = ArtworkManager.CacheLoadArtwork(Path.Combine(Constants.DirectoryPaths.Base, _game.Artwork.Icon)),
                Stretch = Stretch.Fill
            };
        }
        catch (Exception ex)
        {
            Logger.Error($"{ex.Message}\nFull Error:\n{ex}");
        }
    }

    #endregion
}