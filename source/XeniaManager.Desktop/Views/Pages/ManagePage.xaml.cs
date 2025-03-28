using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

// Imported
using Octokit;
using XeniaManager.Core;
using XeniaManager.Core.Downloader;
using Page = System.Windows.Controls.Page;

namespace XeniaManager.Desktop.Views.Pages
{
    /// <summary>
    /// Interaction logic for ManagePage.xaml
    /// </summary>
    public partial class ManagePage : Page
    {
        public ManagePage()
        {
            InitializeComponent();
        }

        private async void BtnInstallCanary_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Mouse.OverrideCursor = Cursors.Wait;
                // Fetch latest Xenia Canary release
                Release canaryRelease = await Github.GetLatestRelease(Xenia.Canary);
                ReleaseAsset canaryAsset = canaryRelease.Assets.FirstOrDefault(a => a.Name.Contains("windows", StringComparison.OrdinalIgnoreCase));
                if (canaryAsset == null)
                {
                    throw new Exception("Windows build asset missing in the release");
                }
                Logger.Info("Downloading the latest Xenia Canary build");
                DownloadManager downloadManager = new DownloadManager();
                downloadManager.ProgressChanged += (progress) => { PbDownloadProgress.Value = progress; };
                
                // Download Xenia Canary
                await downloadManager.DownloadAndExtractAsync(canaryAsset.BrowserDownloadUrl, "xenia.zip", Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Emulators\Xenia Canary\"));
                
                // Download "gamecontrollerdb.txt" for SDL Input System
                Logger.Info("Downloading gamecontrollerdb.txt for SDL Input System");
                await downloadManager.DownloadFileAsync(
                    "https://raw.githubusercontent.com/mdqinc/SDL_GameControllerDB/master/gamecontrollerdb.txt",
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Emulators\Xenia Canary\gamecontrollerdb.txt"));
                
                // Reset the ProgressBar and mouse
                PbDownloadProgress.Value = 0;
                Mouse.OverrideCursor = null;
            }
            catch (Exception exception)
            {
                Logger.Error(exception);
                PbDownloadProgress.Value = 0;
                Mouse.OverrideCursor = null;
                return;
            }
        }
    }
}