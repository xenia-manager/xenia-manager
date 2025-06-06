using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;

// Imported Libraries
using Wpf.Ui;
using Wpf.Ui.Controls;
using XeniaManager.Core;
using XeniaManager.Core.Game;
using XeniaManager.Desktop.Components;
using XeniaManager.Desktop.Utilities;
using XeniaManager.Desktop.ViewModel.Windows;
using Image = System.Windows.Controls.Image;
using MenuItem = System.Windows.Controls.MenuItem;

namespace XeniaManager.Desktop.Views.Windows;

/// <summary>
/// Interaction logic for XeniaScreenshotsViewer.xaml
/// </summary>
public partial class XeniaScreenshotsViewer : FluentWindow
{
    private XeniaScreenshotViewerViewModel _viewModel { get; set; }
    private readonly SnackbarService _updateNotification = new SnackbarService();
    private DateTime _lastFullscreenClose = DateTime.MinValue;

    public XeniaScreenshotsViewer(Game game)
    {
        InitializeComponent();
        _viewModel = new XeniaScreenshotViewerViewModel(game);
        DataContext = _viewModel;
        _updateNotification.SetSnackbarPresenter(SbNotification);
    }

    private void Image_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if ((DateTime.Now - _lastFullscreenClose).TotalMilliseconds < 250)
        {
            return;
        }
        if (sender is Image img && img.Tag is Screenshot screenshot)
        {
            FullscreenImageWindow fullscreenWindow = new FullscreenImageWindow(screenshot.FilePath);
            fullscreenWindow.Closed += (s, args) => _lastFullscreenClose = DateTime.Now;
            fullscreenWindow.ShowDialog();
        }
    }

    private void OpenInFullscreen_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuItem item && item.Tag is Screenshot screenshot)
        {
            FullscreenImageWindow fullscreenWindow = new FullscreenImageWindow(screenshot.FilePath);
            fullscreenWindow.Closed += (s, args) => _lastFullscreenClose = DateTime.Now;
            fullscreenWindow.ShowDialog();
        }
    }

    private void CopyImage_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuItem { Tag: Screenshot screenshot })
        {
            try
            {
                BitmapImage bitmap = new BitmapImage(new Uri(screenshot.FilePath));
                Clipboard.SetImage(bitmap);
                _updateNotification.Show(LocalizationHelper.GetUiText("SnackbarPresenter_CopiedImageTitle"),
                    $"{LocalizationHelper.GetUiText("SnackbarPresenter_CopiedImageText")}",
                    ControlAppearance.Success, new SymbolIcon(SymbolRegular.ImageCopy20), TimeSpan.FromSeconds(5));
            }
            catch (Exception ex)
            {
                Logger.Error($"{ex.Message}\nFull Error:\n{ex}");
                CustomMessageBox.Show(ex);
            }
        }
    }
    private void ShowInExplorer_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuItem { Tag: Screenshot screenshot })
        {
            try
            {
                Process.Start("explorer.exe", $"/select,\"{screenshot.FilePath}\"");
            }
            catch (Exception ex)
            {
                Logger.Error($"{ex.Message}\nFull Error:\n{ex}");
                CustomMessageBox.Show(ex);
            }
        }
    }
    private void DeleteScreenshot_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuItem { Tag: Screenshot screenshot })
        {
            try
            {
                File.Delete(screenshot.FilePath);
                _viewModel.GameScreenshots.Remove(screenshot);
                _viewModel.LoadGameScreenshots();
            }
            catch (Exception ex)
            {
                Logger.Error($"{ex.Message}\nFull Error:\n{ex}");
                CustomMessageBox.Show(ex);
            }
        }
    }
}