// Imported Libraries
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Wpf.Ui.Controls;
using XeniaManager.Core;
using XeniaManager.Core.Constants;
using XeniaManager.Core.Constants.Emulators;
using XeniaManager.Core.Enum;
using XeniaManager.Core.Game;
using XeniaManager.Core.Mousehook;
using XeniaManager.Core.Profile;
using XeniaManager.Desktop.Components;
using XeniaManager.Desktop.Utilities;
using XeniaManager.Desktop.ViewModel.Windows;
using MessageBoxResult = Wpf.Ui.Controls.MessageBoxResult;

namespace XeniaManager.Desktop.Views.Windows;

public partial class ContentViewer : FluentWindow
{
    #region Variables
    private ContentViewerViewModel _viewModel { get; set; }
    private readonly SecretCodeListener _konamiListener = new();
    #endregion

    #region Constructors

    public ContentViewer(Game game)
    {
        InitializeComponent();
        _viewModel = new ContentViewerViewModel(game);
        DataContext = _viewModel;
        CmbGamerProfiles.SelectedIndex = 0;
        CmbContentTypeList.SelectedIndex = 0;

        // Subscribe to Konami code event
        _konamiListener.KonamiCodeEntered += OnKonamiCodeEntered;

        // Subscribe to InputListener only when Achievements are visible
        InputListener.KeyPressed += OnKeyPressed;
    }

    #endregion

    #region Functions & Events
    private void OnKeyPressed(object? sender, InputListener.KeyEventArgs e)
    {
        // Only listen if Achievements are visible
        if (_viewModel.IsAchievementsVisible == Visibility.Visible)
        {
            _konamiListener.OnKeyPressed(sender, e);
        }
    }

    private void OnKonamiCodeEntered()
    {
        Dispatcher.Invoke(() =>
        {
            _viewModel.IsAchievementEditingEnabled = true;
            InputListener.Stop();
            CustomMessageBox.Show("Konami Code!", "Achievement editing enabled.");
        });
    }
    private string GetContentFolder(string contentType, XeniaVersion xeniaVersion)
    {
        string emulatorLocation = xeniaVersion switch
        {
            XeniaVersion.Canary => XeniaCanary.ContentFolderLocation,
            XeniaVersion.Mousehook => XeniaMousehook.ContentFolderLocation,
            XeniaVersion.Netplay => XeniaNetplay.ContentFolderLocation,
            _ => string.Empty
        };

        string profileFolder = string.Empty;
        if (CmbGamerProfiles.SelectedItem != null)
        {
            profileFolder = contentType switch
            {
                "00000001" or "GPD" or "ProfileGpdFile" => CmbGamerProfiles.SelectedValue.ToString(),
                _ => "0000000000000000"
            };
        }

        if (contentType == "GPD")
        {
            return Path.Combine(emulatorLocation, profileFolder, "FFFE07D1", "00010000", profileFolder, _viewModel.Game.GameId + ".gpd");
        }
        else if (contentType == "ProfileGpdFile")
        {
            return Path.Combine(emulatorLocation, profileFolder, "FFFE07D1", "00010000", profileFolder, "FFFE07D1.gpd");
        }

        return Path.Combine(emulatorLocation, profileFolder, _viewModel.Game.GameId, contentType);
    }

    public static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T t)
                return t;
            var result = FindVisualChild<T>(child);
            if (result != null)
                return result;
        }
        return null;
    }

    private void CmbContentTypeList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        InputListener.Stop();
        if (CmbContentTypeList.SelectedIndex < 0)
        {
            _viewModel.Files = [];
            return;
        }

        try
        {
            string selectedType = CmbContentTypeList.SelectedValue.ToString();
            if (selectedType == "GPD")
            {
                InputListener.Start();
                // Load Achievements
                string achievementGpdFilePath = Path.Combine(DirectoryPaths.Base, GetContentFolder(selectedType, _viewModel.Game.XeniaVersion));
                string profileGpdFilePath = Path.Combine(DirectoryPaths.Base, GetContentFolder("ProfileGpdFile", _viewModel.Game.XeniaVersion));
                _viewModel.Files = [];
                _viewModel.Achievements = [];
                if (File.Exists(achievementGpdFilePath))
                {
                    _viewModel.achievementFile.Load(achievementGpdFilePath);
                    _viewModel.Achievements.Clear();
                    foreach (var ach in Achievement.ParseAchievements(_viewModel.achievementFile))
                    {
                        _viewModel.Achievements.Add(ach);
                    }
                    _viewModel.profileGpdFile.Load(profileGpdFilePath);
                }
                IcAchievementsList.Focus();
                return;
            }
            else
            {
                Logger.Info($"Currently selected content: {_viewModel.ContentFolders.FirstOrDefault(key => key.Value == selectedType).Key}");
                string folderPath = Path.Combine(DirectoryPaths.Base, GetContentFolder(selectedType, _viewModel.Game.XeniaVersion));
                Logger.Debug($"Current content path: {folderPath}");
                if (Directory.Exists(folderPath))
                {
                    _viewModel.LoadDirectory(folderPath);
                }
                else
                {
                    _viewModel.Files = [];
                }
                TvwInstalledContentTree.Focus();
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"{ex.Message}\nFull Error:\n{ex}");
            _viewModel.Files = [];
            CustomMessageBox.ShowAsync(ex);
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
                CustomMessageBox.Show(LocalizationHelper.GetUiText("MessageBox_MissingDirectory"), string.Format(LocalizationHelper.GetUiText("MessageBox_MissingDirectoryText"), _viewModel.ContentFolders.FirstOrDefault(key => key.Value == CmbContentTypeList.SelectedValue.ToString()).Key));
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"{ex.Message}\nFull Error:\n{ex}");
            CustomMessageBox.ShowAsync(ex);
        }
    }

    private async Task DeleteContent()
    {
        Logger.Debug("Delete Content");
        MessageBoxResult result = await CustomMessageBox.YesNoAsync(LocalizationHelper.GetUiText("MessageBox_DeleteGameContentTitle"), string.Format(LocalizationHelper.GetUiText("MessageBox_DeleteGameContentText"), _viewModel.Game.Title));
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
            await CustomMessageBox.ShowAsync(LocalizationHelper.GetUiText("MessageBox_Success"), string.Format(LocalizationHelper.GetUiText("MessageBox_SuccessDeleteGameContentText"), deletedContentName, _viewModel.Game.Title));
        }
        catch (Exception ex)
        {
            Logger.Error($"{ex.Message}\nFull Error:\n{ex}");
            await CustomMessageBox.ShowAsync(ex);
        }
        finally
        {
            Mouse.OverrideCursor = null;
        }
    }

    private async Task DeleteSave()
    {
        MessageBoxResult result = await CustomMessageBox.YesNoAsync(LocalizationHelper.GetUiText("MessageBox_DeleteSaveGameTitle"), string.Format(LocalizationHelper.GetUiText("MessageBox_DeleteSaveGameText"), _viewModel.Game.Title));
        if (result != MessageBoxResult.Primary)
        {
            Logger.Info($"Cancelling the deletion of save game for {_viewModel.Game.Title}");
            return;
        }
        try
        {
            Logger.Info($"Deleting save game for {_viewModel.Game.Title}");
            Mouse.OverrideCursor = Cursors.Wait;
            string saveLocation = Path.Combine(DirectoryPaths.Base, GetContentFolder(CmbContentTypeList.SelectedValue.ToString(), _viewModel.Game.XeniaVersion));
            string headersLocation = Path.Combine(DirectoryPaths.Base, Path.GetDirectoryName(GetContentFolder(CmbContentTypeList.SelectedValue.ToString(), _viewModel.Game.XeniaVersion)), "Headers", "00000001");
            SaveManager.DeleteSave(saveLocation, headersLocation);
            CmbContentTypeList_SelectionChanged(CmbContentTypeList, null);
            Logger.Info($"Successful deletion of save game for {_viewModel.Game.Title}");
            Mouse.OverrideCursor = null;
            await CustomMessageBox.ShowAsync(LocalizationHelper.GetUiText("MessageBox_Success"), string.Format(LocalizationHelper.GetUiText("MessageBox_SuccessDeleteSaveGameText"), _viewModel.Game.Title));
        }
        catch (Exception ex)
        {
            Logger.Error($"{ex.Message}\nFull Error:\n{ex}");
            await CustomMessageBox.ShowAsync(ex);
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
            string saveLocation = Path.Combine(DirectoryPaths.Base, GetContentFolder(CmbContentTypeList.SelectedValue.ToString(), _viewModel.Game.XeniaVersion));
            Logger.Debug($"Save Location: {saveLocation}");

            string headersLocation = Path.Combine(DirectoryPaths.Base, Path.GetDirectoryName(GetContentFolder(CmbContentTypeList.SelectedValue.ToString(), _viewModel.Game.XeniaVersion)), "Headers", "00000001");
            Logger.Debug($"Headers Location: {headersLocation}");

            SaveManager.ExportSave(_viewModel.Game, saveLocation, headersLocation);
            Mouse.OverrideCursor = null;
            Logger.Info($"The save file for {_viewModel.Game.Title} has been exported to desktop");
            CustomMessageBox.Show(LocalizationHelper.GetUiText("MessageBox_Success"), string.Format(LocalizationHelper.GetUiText("MessageBox_SaveExportedToDesktop"), _viewModel.Game.Title));
        }
        catch (Exception ex)
        {
            Logger.Error($"{ex.Message}\nFull Error:\n{ex}");
            CustomMessageBox.ShowAsync(ex);
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
            string saveDestination = new DirectoryInfo(Path.Combine(DirectoryPaths.Base, GetContentFolder(CmbContentTypeList.SelectedValue.ToString(), _viewModel.Game.XeniaVersion)))
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
            CustomMessageBox.ShowAsync(ex);
        }
        finally
        {
            Mouse.OverrideCursor = null;
        }
    }

    private void BtnEditProfileInfo_Click(object sender, RoutedEventArgs e)
    {
        if (CmbGamerProfiles.SelectedIndex < 0)
        {
            Logger.Error("No profile selected");
            return;
        }

        string emulatorContentLocation = _viewModel.Game.XeniaVersion switch
        {
            XeniaVersion.Canary => Path.Combine(DirectoryPaths.Base, XeniaCanary.ContentFolderLocation),
            XeniaVersion.Mousehook => Path.Combine(DirectoryPaths.Base, XeniaMousehook.ContentFolderLocation),
            XeniaVersion.Netplay => Path.Combine(DirectoryPaths.Base, XeniaNetplay.ContentFolderLocation),
            _ => string.Empty
        };

        string profileLocation = Path.Combine(emulatorContentLocation, _viewModel.SelectedProfile.OfflineXuid, "FFFE07D1", "00010000", _viewModel.SelectedProfile.OfflineXuid, "Account");

        ProfileEditorWindow profileEditor = new ProfileEditorWindow(_viewModel.SelectedProfile, profileLocation);
        profileEditor.ShowDialog();

        if (profileEditor.ViewModel.GamertagChanged)
        {
            _viewModel.LoadProfiles(_viewModel.Game.XeniaVersion);
        }
    }

    private async void BtnDeleteProfile_Click(object sender, RoutedEventArgs e)
    {
        if (CmbGamerProfiles.SelectedIndex < 0)
        {
            Logger.Error("No profile selected");
            return;
        }

        MessageBoxResult result = await CustomMessageBox.YesNoAsync(LocalizationHelper.GetUiText("MessageBox_DeleteGamerProfileTitle"), string.Format(LocalizationHelper.GetUiText("MessageBox_DeleteGamerProfileText"), CmbGamerProfiles.SelectedItem.ToString()));

        if (result != MessageBoxResult.Primary)
        {
            return;
        }

        try
        {
            string profileLocation = _viewModel.Game.XeniaVersion switch
            {
                XeniaVersion.Canary => Path.Combine(DirectoryPaths.Base, XeniaCanary.ContentFolderLocation, CmbGamerProfiles.SelectedValue.ToString()),
                XeniaVersion.Mousehook => Path.Combine(DirectoryPaths.Base, XeniaMousehook.ContentFolderLocation, CmbGamerProfiles.SelectedValue.ToString()),
                XeniaVersion.Netplay => Path.Combine(DirectoryPaths.Base, XeniaNetplay.ContentFolderLocation, CmbGamerProfiles.SelectedValue.ToString()),
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
            CustomMessageBox.ShowAsync(ex);
        }
    }

    private void ChkAchievement_Click(object sender, RoutedEventArgs e)
    {
        if (sender is CheckBox checkBox && checkBox.DataContext is Achievement achievement)
        {
            if (checkBox.IsChecked == true)
            {
                Logger.Info($"Unlocking {achievement.Name}");
                achievement.Unlock();
            }
            else
            {
                Logger.Info($"Locking {achievement.Name}");
                achievement.Lock();
            }
        }
    }

    private void BtnUnlockAllAchievements_Click(object sender, RoutedEventArgs e)
    {
        foreach (Achievement achievement in _viewModel.Achievements)
        {
            Logger.Info($"Unlocking {achievement.Name}");
            achievement.Unlock();
        }
        _viewModel.Achievements = new ObservableCollection<Achievement>(_viewModel.Achievements);
    }

    private void BtnLockAllAchievements_Click(object sender, RoutedEventArgs e)
    {
        foreach (Achievement achievement in _viewModel.Achievements)
        {
            Logger.Info($"Locking {achievement.Name}");
            achievement.Lock();
        }
        _viewModel.Achievements = new ObservableCollection<Achievement>(_viewModel.Achievements);
    }

    private void BtnSaveAchievementChanges_Click(object sender, RoutedEventArgs e)
    {
        string achievementGpdFilePath = Path.Combine(DirectoryPaths.Base, GetContentFolder(CmbContentTypeList.SelectedValue.ToString(), _viewModel.Game.XeniaVersion));
        string profileGpdFilePath = Path.Combine(DirectoryPaths.Base, GetContentFolder("ProfileGpdFile", _viewModel.Game.XeniaVersion));

        bool success = _viewModel.SaveAchievementChanges(achievementGpdFilePath, profileGpdFilePath);
        if (success)
        {
            CustomMessageBox.Show(LocalizationHelper.GetUiText("MessageBox_Success"), "Changes have been saved.");
        }
        else
        {
            CustomMessageBox.Show(LocalizationHelper.GetUiText("MessageBox_Error"), "There was an error while saving achievements.");
        }
    }

    #endregion
}