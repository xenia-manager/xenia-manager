using System.IO;
using System.Windows;
using System.Windows.Controls;
using TextBox = System.Windows.Controls.TextBox;
using System.Windows.Input;
using System.Windows.Media.Imaging;

// Imported Libraries
using Octokit;
using Wpf.Ui.Controls;
using XeniaManager.Core;
using XeniaManager.Core.Game;
using XeniaManager.Desktop.Components;
using XeniaManager.Core.Constants;

namespace XeniaManager.Desktop.Views.Windows;

public partial class GamePatchesDatabase : FluentWindow
{
    #region Variables

    /// <summary>
    /// The game object for which patches are being browsed.
    /// Contains game metadata including title and id
    /// used for patch filtering.
    /// </summary>
    private Game _game { get; set; }

    /// <summary>
    /// Collection of patch files available from the Canary repository.
    /// Contains repository content metadata including patch names, download URLs,
    /// and file information. Limited to 8 items for performance.
    /// </summary>
    private IReadOnlyList<RepositoryContent> _canaryPatches { get; set; }

    /// <summary>
    /// Collection of patch files available from the Netplay repository.
    /// Contains repository content metadata for multiplayer-specific patches.
    /// May be empty if no Netplay patches are available for the current game.
    /// Limited to 8 items for performance.
    /// </summary>
    private IReadOnlyList<RepositoryContent> _netplayPatches { get; set; }

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of the GamePatchesDatabase window.
    /// Sets up the window with game context, loads available patches from multiple sources,
    /// configures the UI elements, and initializes the search functionality.
    /// </summary>
    /// <param name="game">The game object containing information about the game</param>
    /// <param name="canaryPatches">Collection of patches from the Canary repository</param>
    /// <param name="netplayPatches">Collection of patches from the Netplay repository</param>
    /// <remarks>
    /// Initialization Process:
    /// 1. Store game reference and patch collections
    /// 2. Set window title with game name for context
    /// 3. Load and display game icon (with fallback)
    /// 4. Configure patch source dropdown based on available patches
    /// 5. Initialize search with game ID for relevant results
    /// 6. Limit initial display to 8 patches for performance
    /// 
    /// The Netplay source is automatically removed from the dropdown if no
    /// Netplay patches are available, streamlining the user interface.
    /// </remarks>
    public GamePatchesDatabase(Game game, IReadOnlyList<RepositoryContent> canaryPatches, IReadOnlyList<RepositoryContent> netplayPatches)
    {
        InitializeComponent();
        this._game = game;
        TbTitle.Title = $"{_game.Title} Patches";
        try
        {
            TbTitleIcon.Source = ArtworkManager.CacheLoadArtwork(Path.Combine(DirectoryPaths.Base, _game.Artwork.Icon));
        }
        catch (Exception ex)
        {
            Logger.Error($"{ex.Message}\nFull Error:\n{ex}");
            TbTitleIcon.Source = new BitmapImage(new Uri("pack://application:,,,/Assets/1024.png", UriKind.Absolute));
        }
        this._canaryPatches = canaryPatches;
        LstCanaryPatches.ItemsSource = _canaryPatches.Take(8);
        if (netplayPatches.Count > 0)
        {
            this._netplayPatches = netplayPatches;
            LstNetplayPatches.ItemsSource = _netplayPatches.Take(8);
        }
        else
        {
            CmbPatchSource.Items.RemoveAt(1);
            this._netplayPatches = netplayPatches;
        }
        CmbPatchSource.SelectedIndex = 0;
        TxtSearchBar.Text = _game.GameId;
    }

    #endregion

    #region Functions & Events

    /// <summary>
    /// Handles patch source selection changes in the dropdown menu.
    /// Switches the visible patch list between Canary and Netplay sources
    /// and refreshes the search results for the newly selected source.
    /// </summary>
    /// <remarks>
    /// This method manages the visibility of different patch lists based on
    /// the selected source. Only one patch list is visible at a time to
    /// maintain a clean interface. The search is automatically refreshed
    /// to show relevant results for the newly selected source.
    /// </remarks>
    private void CmbPatchSource_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CmbPatchSource.SelectedIndex < 0)
        {
            return;
        }
        if (CmbPatchSource.SelectedItem is ComboBoxItem cmbItem)
        {
            switch (cmbItem.Content.ToString())
            {
                case "Canary Patches":
                    LstCanaryPatches.Visibility = Visibility.Visible;
                    LstNetplayPatches.Visibility = Visibility.Collapsed;
                    break;
                case "Netplay Patches":
                    LstCanaryPatches.Visibility = Visibility.Collapsed;
                    LstNetplayPatches.Visibility = Visibility.Visible;
                    break;
            }
            TxtSearchBar_TextChanged(TxtSearchBar, new TextChangedEventArgs(TextBox.TextChangedEvent, UndoAction.None));
        }
    }

    /// <summary>
    /// Handles real-time search functionality as the user types in the search bar.
    /// Filters the currently visible patch list based on the search query,
    /// providing immediate feedback and improved patch discovery experience.
    /// </summary>
    /// <remarks>
    /// Search Features:
    /// <para>
    /// - Case-insensitive matching against patch names
    /// </para>
    /// - Real-time filtering as the user types
    /// <para>
    /// - Maintains 8-item limit for performance
    /// </para>
    /// - Works with both Canary and Netplay patch sources
    /// <para>
    /// The search is performed on the patch file names, allowing users to
    /// quickly find specific patches or browse patches containing certain keywords.
    /// </para>
    /// </remarks>
    private async void TxtSearchBar_TextChanged(object sender, TextChangedEventArgs e)
    {
        try
        {
            if (CmbPatchSource.SelectedItem is not ComboBoxItem cmbItem)
            {
                return;
            }
            string searchQuery = TxtSearchBar.Text.ToLower();

            switch (cmbItem.Content.ToString())
            {
                case "Canary Patches":
                    LstCanaryPatches.ItemsSource = _canaryPatches.Where(patch => patch.Name.ToLower().Contains(searchQuery)).ToList().Take(8);
                    break;
                case "Netplay Patches":
                    LstNetplayPatches.ItemsSource = _netplayPatches.Where(patch => patch.Name.ToLower().Contains(searchQuery)).ToList().Take(8);
                    break;
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"{ex.Message}\nFull Error:\n{ex}");
            await CustomMessageBox.Show(ex);
            return;
        }
    }

    /// <summary>
    /// Handles patch selection and download when a user clicks on a patch in the list.
    /// Downloads the selected patch, installs it for the current game, and provides
    /// user feedback throughout the process including loading indicators and success messages.
    /// </summary>
    /// <remarks>
    /// Download Process:
    /// <para>
    /// 1. Validate the selection and sender
    /// </para>
    /// 2. Show loading cursor to indicate processing
    /// <para>
    /// 3. Download and install the patch via PatchManager
    /// </para>
    /// 4. Display a success message to user
    /// <para>
    /// 5. Close the window automatically after successful installation
    /// </para>
    /// Error Handling:
    /// <para>
    /// - Network errors during download
    /// </para>
    /// - File system errors during installation
    /// <para>
    /// - Invalid patch file formats
    /// </para>
    /// The loading cursor is managed in a try-finally block to ensure it's
    /// always reset, even if an error occurs during the download process.
    /// </remarks>
    private async void LstPatches_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        try
        {
            ListBox patchesList = (ListBox)sender;
            if (patchesList.SelectedItem == null || patchesList == null)
            {
                return;
            }
            Mouse.OverrideCursor = Cursors.Wait;
            await PatchManager.DownloadPatch(_game, (RepositoryContent)patchesList.SelectedItem);
            Mouse.OverrideCursor = null;
            CustomMessageBox.Show("Success", $"{_game.Title} patch has been installed.");
            this.Close();
        }
        catch (Exception ex)
        {
            Logger.Error($"{ex.Message}\nFull Error:\n{ex}");
            await CustomMessageBox.Show(ex);
            return;
        }
        finally
        {
            Mouse.OverrideCursor = null;
        }
    }

    #endregion
}