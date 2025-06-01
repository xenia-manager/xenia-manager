using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;

// Imported Libraries
using Microsoft.Win32;
using Wpf.Ui.Controls;
using MessageBoxResult = Wpf.Ui.Controls.MessageBoxResult;
using XeniaManager.Core;
using XeniaManager.Core.Game;
using XeniaManager.Desktop.Components;
using XeniaManager.Desktop.Utilities;
using XeniaManager.Desktop.ViewModel.Windows;

namespace XeniaManager.Desktop.Views.Windows;

public partial class ContentViewer : FluentWindow
{
    #region Variables

    /// <summary>
    /// The view model that provides data binding and business logic for the content viewer.
    /// </summary>
    private ContentViewerViewModel _viewModel { get; set; }

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of the ContentViewer window for the specified game.
    /// </summary>
    /// <param name="game">The game object containing information about the game whose content will be viewed.</param>
    /// <remarks>
    /// This constructor sets up the window with the game's title and icon, initializes the view model,
    /// and sets default selections for the gamer profiles and content type combo boxes.
    /// If the game's icon cannot be loaded, it falls back to a default application icon.
    /// </remarks>
    public ContentViewer(Game game)
    {
        InitializeComponent();
        _viewModel = new ContentViewerViewModel(game);
        DataContext = _viewModel;
        TbTitle.Title = $"{_viewModel.Game.Title} Content";
        try
        {
            TbTitleIcon.Source = ArtworkManager.CacheLoadArtwork(Path.Combine(Constants.DirectoryPaths.Base, _viewModel.Game.Artwork.Icon));
        }
        catch (Exception ex)
        {
            Logger.Error($"{ex.Message}\nFull Error:\n{ex}");
            TbTitleIcon.Source = new BitmapImage(new Uri("pack://application:,,,/Assets/1024.png", UriKind.Absolute));
        }
        CmbGamerProfiles.SelectedIndex = 0;
        CmbContentTypeList.SelectedIndex = 0;
    }

    #endregion

    #region Functions & Events

    /// <summary>
    /// Constructs the file system path to a specific content folder based on content type and Xenia version.
    /// </summary>
    /// <param name="contentType">The type of content (e.g., "00000001" for save games).</param>
    /// <param name="xeniaVersion">The version of Xenia emulator being used.</param>
    /// <returns>The complete file system path to the content folder.</returns>
    /// <remarks>
    /// The path structure follows Xenia's content organization:
    /// [EmulatorLocation]/[ProfileFolder]/[GameId]/[ContentType]
    /// 
    /// For save games (contentType "00000001"), the selected gamer profile is used.
    /// For other content types, a default profile "0000000000000000" is used.
    /// Currently only supports the Xenia Canary version.
    /// </remarks>
    private string GetContentFolder(string contentType, XeniaVersion xeniaVersion)
    {
        string emulatorLocation = xeniaVersion switch
        {
            XeniaVersion.Canary => Constants.Xenia.Canary.ContentFolderLocation,
            _ => string.Empty
        };

        string profileFolder = string.Empty;
        if (CmbGamerProfiles.SelectedItem != null)
        {
            profileFolder = contentType == "00000001" ? CmbGamerProfiles.SelectedValue.ToString() : "0000000000000000";
        }
        else
        {
            profileFolder = "0000000000000000";
        }

        return Path.Combine(emulatorLocation, profileFolder, _viewModel.Game.GameId, contentType);
    }

    /// <summary>
    /// Handles the selection change event for the content type combo box.
    /// </summary>
    /// <remarks>
    /// When the content type selection changes, this method:
    /// <para>
    /// 1. Determines the appropriate content folder path
    /// </para>
    /// 2. Checks if the folder exists
    /// <para>
    /// 3. Loads the directory contents into the view model if it exists
    /// </para>
    /// 4. Clears the file list if the directory doesn't exist
    /// <para>
    /// All operations are logged and any exceptions are caught and displayed to the user.
    /// </para>
    /// </remarks>
    private void CmbContentTypeList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CmbContentTypeList.SelectedIndex < 0)
        {
            return;
        }

        try
        {
            Logger.Info($"Currently selected content: {_viewModel.ContentFolders.FirstOrDefault(key => key.Value == CmbContentTypeList.SelectedValue.ToString()).Key}");
            string folderPath = Path.Combine(Constants.DirectoryPaths.Base, GetContentFolder(CmbContentTypeList.SelectedValue.ToString(), _viewModel.Game.XeniaVersion));
            Logger.Debug($"Current content path: {folderPath}");
            if (Directory.Exists(folderPath))
            {
                _viewModel.LoadDirectory(folderPath);
            }
            else
            {
                _viewModel.Files = [];
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"{ex.Message}\nFull Error:\n{ex}");
            CustomMessageBox.Show(ex);
        }
    }

    /// <summary>
    /// Handles the selection change event for the gamer profiles combo box.
    /// </summary>
    /// <remarks>
    /// When a different gamer profile is selected, this method triggers a refresh
    /// of the content type selection to update the displayed files for the new profile.
    /// The selected profile affects which save games and content are visible.
    /// </remarks>
    private void CmbGamerProfiles_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CmbGamerProfiles.SelectedIndex < 0)
        {
            return;
        }
        Logger.Debug($"Currently selected profile: {CmbGamerProfiles.SelectedItem}");
        CmbContentTypeList_SelectionChanged(CmbContentTypeList, null);
    }

    /// <summary>
    /// Handles the click event for the "Open Folder" button to launch Windows Explorer.
    /// </summary>
    /// <remarks>
    /// This method opens the current content directory in Windows Explorer, allowing users
    /// to manually browse and manage files. If the directory doesn't exist, it displays
    /// an informative message to the user instead of opening Explorer.
    /// 
    /// Requires a valid content type selection to determine which folder to open.
    /// </remarks>
    private void BtnOpenFolder_Click(object sender, RoutedEventArgs e)
    {
        if (CmbContentTypeList.SelectedIndex < 0)
        {
            Logger.Error("No profile selected");
            return;
        }

        try
        {
            Process process = new Process();
            process.StartInfo.FileName = "explorer.exe";
            string directoryPath = GetContentFolder(CmbContentTypeList.SelectedValue.ToString(), _viewModel.Game.XeniaVersion);

            if (Directory.Exists(directoryPath))
            {
                process.StartInfo.Arguments = directoryPath;
                process.Start();
            }
            else
            {
                CustomMessageBox.Show("Missing directory", $"This game has no directory called '{_viewModel.ContentFolders.FirstOrDefault(key => key.Value == CmbContentTypeList.SelectedValue.ToString()).Key}'");
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"{ex.Message}\nFull Error:\n{ex}");
            CustomMessageBox.Show(ex);
        }
    }

    /// <summary>
    /// Handles the click event for the "Delete Save" button to remove save game data.
    /// </summary>
    /// <remarks>
    /// This method performs a complete save game deletion including:
    /// <para>
    /// 1. User confirmation dialog before deletion
    /// </para>
    /// 2. Deletion of save data files
    /// <para>
    /// 3. Deletion of associated header files
    /// </para>
    /// 4. UI refresh to reflect changes
    /// <para>
    /// 5. Success confirmation message
    /// </para>
    /// The operation requires a selected gamer profile and existing save games.
    /// The mouse cursor changes to indicate the operation is in progress.
    /// All operations are logged and any errors are displayed to the user.
    /// </remarks>
    private async void BtnDeleteSave_Click(object sender, RoutedEventArgs e)
    {
        if (CmbGamerProfiles.SelectedIndex < 0)
        {
            Logger.Error("No profile selected");
            return;
        }

        if (TvwInstalledContentTree.Items.Count <= 0)
        {
            Logger.Error("No save games to delete");
            return;
        }

        MessageBoxResult result = await CustomMessageBox.YesNo(LocalizationHelper.GetUiText("MessageBox_DeleteSaveGameTitle"), string.Format(LocalizationHelper.GetUiText("MessageBox_DeleteSaveGameText"), _viewModel.Game.Title));
        if (result != MessageBoxResult.Primary)
        {
            Logger.Info($"Cancelling the deletion of save game for {_viewModel.Game.Title}");
            return;
        }
        try
        {
            Logger.Info($"Deleting save game for {_viewModel.Game.Title}");
            Mouse.OverrideCursor = Cursors.Wait;
            string saveLocation = Path.Combine(Constants.DirectoryPaths.Base, GetContentFolder(CmbContentTypeList.SelectedValue.ToString(), _viewModel.Game.XeniaVersion));
            string headersLocation = Path.Combine(Constants.DirectoryPaths.Base, Path.GetDirectoryName(GetContentFolder(CmbContentTypeList.SelectedValue.ToString(), _viewModel.Game.XeniaVersion)), "Headers", "00000001");
            SaveManager.DeleteSave(saveLocation, headersLocation);
            CmbContentTypeList_SelectionChanged(CmbContentTypeList, null);
            Logger.Info($"Successful deletion of save game for {_viewModel.Game.Title}");
            Mouse.OverrideCursor = null;
            CustomMessageBox.Show(LocalizationHelper.GetUiText("MessageBox_Success"), string.Format(LocalizationHelper.GetUiText("MessageBox_SuccessDeleteSaveGameText"), _viewModel.Game.Title));
        }
        catch (Exception ex)
        {
            Logger.Error($"{ex.Message}\nFull Error:\n{ex}");
            CustomMessageBox.Show(ex);
        }
        finally
        {
            Mouse.OverrideCursor = null;
        }
    }

    /// <summary>
    /// Handles the click event for the "Export Save" button to create a backup of save game data.
    /// </summary>
    /// <remarks>
    /// This method exports the current save game to the user's desktop as a ZIP file.
    /// The export includes both the save data files and associated header files.
    /// 
    /// Requirements:
    /// <para>
    /// - A gamer profile must be selected
    /// </para>
    /// - Save games must exist for the current game
    /// <para>
    /// The exported file can later be imported using the Import Save functionality.
    /// The mouse cursor changes to indicate the operation is in progress.
    /// </para>
    /// </remarks>
    private void BtnExportSave_Click(object sender, RoutedEventArgs e)
    {
        if (CmbGamerProfiles.SelectedIndex < 0)
        {
            Logger.Error("No profile selected");
            return;
        }

        if (TvwInstalledContentTree.Items.Count <= 0)
        {
            Logger.Error("No save games to export");
            return;
        }

        try
        {
            Mouse.OverrideCursor = Cursors.Wait;
            string saveLocation = Path.Combine(Constants.DirectoryPaths.Base, GetContentFolder(CmbContentTypeList.SelectedValue.ToString(), _viewModel.Game.XeniaVersion));
            Logger.Debug($"Save Location: {saveLocation}");

            string headersLocation = Path.Combine(Constants.DirectoryPaths.Base, Path.GetDirectoryName(GetContentFolder(CmbContentTypeList.SelectedValue.ToString(), _viewModel.Game.XeniaVersion)), "Headers", "00000001");
            Logger.Debug($"Headers Location: {headersLocation}");

            SaveManager.ExportSave(_viewModel.Game, saveLocation, headersLocation);
            Mouse.OverrideCursor = null;
            Logger.Info($"The save file for {_viewModel.Game.Title} has been exported to desktop");
            CustomMessageBox.Show("Success", $"The save file for {_viewModel.Game.Title} has been exported to desktop");
        }
        catch (Exception ex)
        {
            Logger.Error($"{ex.Message}\nFull Error:\n{ex}");
            CustomMessageBox.Show(ex);
        }
        finally
        {
            Mouse.OverrideCursor = null;
        }
    }

    /// <summary>
    /// Handles the click event for the "Import Save" button to restore save game data from a backup file.
    /// </summary>
    /// <remarks>
    /// This method allows users to import previously exported save games from ZIP files.
    /// The process includes:
    /// <para>
    /// 1. File selection dialog filtered to ZIP files only
    /// </para>
    /// 2. Extraction and installation of save data and headers
    /// <para>
    /// 3. UI refresh to show the imported content
    /// </para>
    /// Requirements:
    /// <para>
    /// - A gamer profile must be selected
    /// </para>
    /// - User must select a valid ZIP file containing save data
    /// <para>
    /// The import destination is automatically determined based on the current game and profile.
    /// The mouse cursor changes to indicate the operation is in progress.
    /// </para>
    /// </remarks>
    private void BtnImportSave_Click(object sender, RoutedEventArgs e)
    {
        if (CmbGamerProfiles.SelectedIndex < 0)
        {
            Logger.Error("No profile selected");
            return;
        }
        OpenFileDialog openFileDialog = new OpenFileDialog
        {
            Title = "Select a save file",
            Filter = "Supported files|*.zip",
            Multiselect = false
        };

        if (openFileDialog.ShowDialog() != true)
        {
            Logger.Info("Cancelled the import of save game");
            return;
        }

        try
        {
            Mouse.OverrideCursor = Cursors.Wait;
            string saveDestination = new DirectoryInfo(Path.Combine(Constants.DirectoryPaths.Base, GetContentFolder(CmbContentTypeList.SelectedValue.ToString(), _viewModel.Game.XeniaVersion)))
                .Parent?
                .Parent?
                .FullName;
            SaveManager.ImportSave(openFileDialog.FileName, saveDestination);
            Logger.Info($"Successful import of save game for {_viewModel.Game.Title}");
            CmbContentTypeList_SelectionChanged(CmbContentTypeList, null);
            ;
        }
        catch (Exception ex)
        {
            Logger.Error($"{ex.Message}\nFull Error:\n{ex}");
            CustomMessageBox.Show(ex);
        }
        finally
        {
            Mouse.OverrideCursor = null;
        }
    }

    #endregion
}