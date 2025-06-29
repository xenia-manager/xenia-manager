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
    private ContentViewerViewModel _viewModel { get; set; }

    #endregion

    #region Constructors

    public ContentViewer(Game game)
    {
        InitializeComponent();
        _viewModel = new ContentViewerViewModel(game);
        DataContext = _viewModel;
        CmbGamerProfiles.SelectedIndex = 0;
        CmbContentTypeList.SelectedIndex = 0;
    }

    #endregion

    #region Functions & Events

    private string GetContentFolder(string contentType, XeniaVersion xeniaVersion)
    {
        string emulatorLocation = xeniaVersion switch
        {
            XeniaVersion.Canary => Constants.Xenia.Canary.ContentFolderLocation,
            XeniaVersion.Mousehook => Constants.Xenia.Mousehook.ContentFolderLocation,
            _ => string.Empty
        };

        string profileFolder = string.Empty;
        if (CmbGamerProfiles.SelectedItem != null)
        {
            profileFolder = contentType == "00000001" ? CmbGamerProfiles.SelectedValue.ToString() : "0000000000000000";
        }

        return Path.Combine(emulatorLocation, profileFolder, _viewModel.Game.GameId, contentType);
    }

    private void CmbContentTypeList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CmbContentTypeList.SelectedIndex < 0)
        {
            _viewModel.Files = [];
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
            _viewModel.Files = [];
            CustomMessageBox.Show(ex);
        }
    }

    private void CmbGamerProfiles_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CmbGamerProfiles.SelectedIndex < 0)
        {
            _viewModel.Files = [];
            return;
        }
        Logger.Debug($"Currently selected profile: {CmbGamerProfiles.SelectedItem}");
        CmbContentTypeList_SelectionChanged(CmbContentTypeList, null);
    }

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

    private async Task DeleteContent()
    {
        Logger.Debug("Delete Content");
        MessageBoxResult result = await CustomMessageBox.YesNo(LocalizationHelper.GetUiText("MessageBox_DeleteGameContentTitle"), string.Format(LocalizationHelper.GetUiText("MessageBox_DeleteGameContentText"), _viewModel.Game.Title));
        if (result != MessageBoxResult.Primary)
        {
            Logger.Info($"Cancelling the deletion of save game for {_viewModel.Game.Title}");
            return;
        }
        try
        {
            Logger.Info($"Deleting save game for {_viewModel.Game.Title}");
            Mouse.OverrideCursor = Cursors.Wait;
            FileItem fileItem = TvwInstalledContentTree.SelectedItem as FileItem;
            string contentLocation = fileItem.FullPath;
            Logger.Debug($"Content Location: {contentLocation}");
            string deletedContentName = string.Empty;
            if (File.Exists(contentLocation))
            {
                Logger.Debug($"Deleted content file: {contentLocation}");
                deletedContentName = Path.GetFileName(contentLocation);
                File.Delete(contentLocation);
            }
            else if (Directory.Exists(contentLocation))
            {
                Logger.Debug($"Deleted content folder: {contentLocation}");
                deletedContentName = Path.GetFileName(contentLocation);
                Directory.Delete(contentLocation, true);
            }
            else
            {
                Logger.Warning($"Couldn't find content file: {contentLocation}");
            }
            CmbContentTypeList_SelectionChanged(CmbContentTypeList, null);
            Logger.Info($"Successful deletion of save game for {_viewModel.Game.Title}");
            Mouse.OverrideCursor = null;
            await CustomMessageBox.Show(LocalizationHelper.GetUiText("MessageBox_Success"), string.Format(LocalizationHelper.GetUiText("MessageBox_SuccessDeleteGameContentText"), deletedContentName, _viewModel.Game.Title));
        }
        catch (Exception ex)
        {
            Logger.Error($"{ex.Message}\nFull Error:\n{ex}");
            await CustomMessageBox.Show(ex);
        }
        finally
        {
            Mouse.OverrideCursor = null;
        }
    }

    private async Task DeleteSave()
    {
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
            await CustomMessageBox.Show(LocalizationHelper.GetUiText("MessageBox_Success"), string.Format(LocalizationHelper.GetUiText("MessageBox_SuccessDeleteSaveGameText"), _viewModel.Game.Title));
        }
        catch (Exception ex)
        {
            Logger.Error($"{ex.Message}\nFull Error:\n{ex}");
            await CustomMessageBox.Show(ex);
        }
        finally
        {
            Mouse.OverrideCursor = null;
        }
    }

    private async void BtnDelete_Click(object sender, RoutedEventArgs e)
    {
        if (TvwInstalledContentTree.Items.Count <= 0)
        {
            Logger.Error("No content to delete");
            return;
        }

        Logger.Debug(_viewModel.SelectedContentDisplayName);

        if (_viewModel.SelectedContentDisplayName == "Saved Game")
        {
            if (CmbGamerProfiles.SelectedIndex < 0)
            {
                Logger.Error("No profile selected");
                return;
            }
            await DeleteSave();
        }
        else
        {
            await DeleteContent();
        }
    }

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

    private async void BtnDeleteProfile_Click(object sender, RoutedEventArgs e)
    {
        if (CmbGamerProfiles.SelectedIndex < 0)
        {
            Logger.Error("No profile selected");
            return;
        }

        MessageBoxResult result = await CustomMessageBox.YesNo(LocalizationHelper.GetUiText("MessageBox_DeleteGamerProfileTitle"), string.Format(LocalizationHelper.GetUiText("MessageBox_DeleteGamerProfileText"), CmbGamerProfiles.SelectedItem.ToString()));

        if (result != MessageBoxResult.Primary)
        {
            return;
        }

        try
        {
            string profileLocation = _viewModel.Game.XeniaVersion switch
            {
                XeniaVersion.Canary => Path.Combine(Constants.DirectoryPaths.Base, Constants.Xenia.Canary.ContentFolderLocation, CmbGamerProfiles.SelectedValue.ToString()),
                XeniaVersion.Mousehook => Path.Combine(Constants.DirectoryPaths.Base, Constants.Xenia.Mousehook.ContentFolderLocation, CmbGamerProfiles.SelectedValue.ToString()),
                XeniaVersion.Netplay => throw new NotImplementedException(),
                XeniaVersion.Custom => throw new NotImplementedException(),
                _ => throw new NotImplementedException()
            };
            Logger.Debug($"Profile Location: {profileLocation}");
            if (Directory.Exists(profileLocation))
            {
                Directory.Delete(profileLocation, true);
            }

            _viewModel.LoadProfiles(_viewModel.Game.XeniaVersion);
            try
            {
                CmbGamerProfiles.SelectedIndex = 0;
            }
            catch (Exception)
            {
                CmbGamerProfiles.SelectedIndex = -1;
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"{ex.Message}\nFull Error:\n{ex}");
            CustomMessageBox.Show(ex);
        }
    }

    #endregion
}