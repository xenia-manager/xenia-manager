using System;

// Imported
using Serilog;
using XeniaManager.Downloader;

namespace XeniaManager.Installation
{
    public static partial class InstallationManager
    {
        /// <summary>
        /// Downloads Xenia VFS Dump tool
        /// </summary>
        public static async void DownloadXeniaVFSDumper()
        {
            try
            {
                // Check if directory for Xenia VFS Dump Tool exists, if not, create it
                if (!Directory.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Tools\Xenia VFS Dump Tool\")))
                {
                    Directory.CreateDirectory(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Tools\Xenia VFS Dump Tool\"));
                }

                // Download and extract Xenia VFS Dump Tool to it's directory
                Log.Information("Downloading Xenia VFS Dump Tool");
                await DownloadManager.DownloadFileAsync("https://github.com/xenia-project/release-builds-windows/releases/latest/download/xenia-vfs-dump_master.zip", Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Downloads\xenia-vfs-dump.zip"));
                Log.Information("Extracting Xenia VFS Dump Tool");
                DownloadManager.ExtractZipFile(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Downloads\xenia-vfs-dump.zip"), Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Tools\Xenia VFS Dump Tool\"));
                Log.Information("Cleaning up");
                File.Delete(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Downloads\xenia-vfs-dump.zip"));
                ConfigurationManager.AppConfig.VfsDumpToolLocation = @"Tools\Xenia VFS Dump Tool\xenia-vfs-dump.exe";
                ConfigurationManager.SaveConfigurationFile();
                Log.Information("Xenia VFS Dump Tool is installed");
            }
            catch (Exception ex)
            {
                Log.Error($"An error occurred: {ex.Message}");
            }
        }

        /// <summary>
        /// Downloads Xenia Manager Updater
        /// </summary>
        public static async void DownloadXeniaManagerUpdater()
        {
            // Download and extract Xenia Manager Updater
            Log.Information("Downloading Xenia Manager Updater");
            await DownloadManager.DownloadFileAsync("https://github.com/xenia-manager/xenia-manager/releases/download/updater/Xenia.Manager.Updater.zip", Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Downloads\xenia-manager-updater.zip"));
            Log.Information("Extracting Xenia Manager Updater");
            DownloadManager.ExtractZipFile(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Downloads\xenia-manager-updater.zip"), AppDomain.CurrentDomain.BaseDirectory);
            Log.Information("Cleaning up");
            File.Delete(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Downloads\xenia-manager-updater.zip"));
            Log.Information("Xenia Manager Updater is installed");
        }
    }
}
