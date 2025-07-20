using System.ComponentModel;
using System.IO;
using System.Windows.Media.Imaging;

// Imported Libraries
using Wpf.Ui.Controls;
using XeniaManager.Core;
using XeniaManager.Core.Constants;
using XeniaManager.Core.Game;
using XeniaManager.Desktop.Components;

namespace XeniaManager.Desktop.Views.Windows;

public partial class GamePatchesSettings : FluentWindow
{
    #region Variables

    /// <summary>
    /// File system path to the patch configuration file for the current game.
    /// This path is used for both loading patches on initialization and
    /// saving changes when the window closes. The patch file contains
    /// patch definitions, enabled states, and configuration parameters.
    /// </summary>
    /// <remarks>
    /// This field is set during construction and remains immutable throughout
    /// the window's lifetime to ensure data consistency.
    /// </remarks>
    private string _patchLocation { get; set; }

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of the GamePatchesSettings window.
    /// Sets up the window with game-specific information, loads the game's icon,
    /// configures the window title, and populates the patches list from the patch file.
    /// </summary>
    /// <param name="game">The game object containing metadata such as title and artwork paths</param>
    /// <param name="patchLocation">File system path to the patch configuration file for this game</param>
    /// <remarks>
    /// Initialization Process:
    /// <para>
    /// 1. Store the patch file location for later use
    /// </para>
    /// 2. Set window title to include the game name
    /// <para>
    /// 3. Attempt to load and display the game's icon
    /// </para>
    /// 4. Load patch data from the patch file
    /// <para>
    /// 5. Bind patch data to the UI controls
    /// </para>
    /// If the game icon cannot be loaded (missing file, corrupt image, etc.),
    /// a default application icon is used as fallback to maintain UI consistency.
    /// </remarks>
    public GamePatchesSettings(Game game, string patchLocation)
    {
        InitializeComponent();
        _patchLocation = patchLocation;
        TbTitle.Title = $"{game.Title} Patches";
        try
        {
            TbTitleIcon.Source = ArtworkManager.CacheLoadArtwork(Path.Combine(DirectoryPaths.Base, game.Artwork.Icon));
        }
        catch (Exception ex)
        {
            Logger.Error($"{ex.Message}\nFull Error:\n{ex}");
            TbTitleIcon.Source = new BitmapImage(new Uri("pack://application:,,,/Assets/64.png", UriKind.Absolute));
        }
        IcPatchesList.ItemsSource = PatchManager.ReadPatchFile(_patchLocation);
    }

    #endregion

    #region Functions & Events

    /// <summary>
    /// Handles the window closing event to automatically save patch configuration changes.
    /// Ensures that all user modifications to patch settings are persisted to the patch file
    /// before the window closes, preventing data loss and maintaining configuration integrity.
    /// </summary>
    /// <remarks>
    /// Save Process:
    /// 1. Log the save operation for debugging/audit purposes
    /// 2. Extract current patch data from the UI controls
    /// 3. Write the updated patch configuration to the file system
    /// 4. Handle any errors that occur during the save process
    /// 5. Continue with normal window closure
    /// 
    /// If saving fails due to file system issues (permissions, disk space, etc.),
    /// an error message is displayed to the user, but the window closure is not
    /// prevented to avoid trapping the user in an unusable state.
    /// 
    /// Error Handling Strategy:
    /// - Log detailed error information for debugging
    /// - Display user-friendly error message
    /// - Allow window closure to continue (graceful degradation)
    /// </remarks>
    protected override void OnClosing(CancelEventArgs e)
    {
        Logger.Info("Saving changes");
        try
        {
            PatchManager.SavePatchFile(IcPatchesList.ItemsSource, _patchLocation);
        }
        catch (Exception ex)
        {
            Logger.Error($"{ex.Message}\nFull Error:\n{ex}");
            CustomMessageBox.ShowAsync(ex);
        }
        base.OnClosing(e);
    }

    #endregion
}