using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Wpf.Ui.Controls;
using XeniaManager.Core;
using XeniaManager.Core.Game;
using XeniaManager.Desktop.Components;
using XeniaManager.Desktop.ViewModel.Windows;

namespace XeniaManager.Desktop.Views.Windows;

public partial class ContentViewer : FluentWindow
{
    private ContentViewerViewModel _viewModel { get; set; }
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

    private void CmbGamerProfiles_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CmbGamerProfiles.SelectedIndex < 0)
        {
            return;
        }
        Logger.Debug($"Currently selected profile: {CmbGamerProfiles.SelectedItem}");
        CmbContentTypeList_SelectionChanged(CmbContentTypeList, null);
    }
    private void BtnOpenFolder_Click(object sender, RoutedEventArgs e)
    {
        if (CmbContentTypeList.SelectedIndex < 0)
        {
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
}