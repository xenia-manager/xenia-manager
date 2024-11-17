using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Windows;

namespace XeniaManager.Updater;

public partial class MainWindow : Window
{
    /// <summary>
    /// Download new version of Xenia Manager
    /// </summary>
    private async Task DownloadNewVersion()
    {
        try
        {
            using (HttpClient client = new HttpClient())
            {
                using (var response = await client.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead))
                {
                    response.EnsureSuccessStatusCode();
                    Directory.CreateDirectory(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Downloads"));
                    using (var stream = await response.Content.ReadAsStreamAsync())
                    using (var fileStream = new FileStream(
                               Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Downloads", "xenia_manager.zip"),
                               FileMode.Create,
                               FileAccess.Write, FileShare.None))
                    {
                        var totalBytes = response.Content.Headers.ContentLength ?? -1;
                        var buffer = new byte[8192];
                        var bytesRead = 0;
                        var totalRead = 0;
                        do
                        {
                            bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                            if (bytesRead > 0)
                            {
                                await fileStream.WriteAsync(buffer, 0, bytesRead);

                                totalRead += bytesRead;

                                // Calculate progress percentage
                                var progressPercentage =
                                    totalBytes == -1 ? 0 : (int)((double)totalRead / totalBytes * 100);
                                pbProgress.Value = progressPercentage;
                            }
                        } while (bytesRead > 0);
                    }
                }
            }

            await Task.Delay(1);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message + "\n" + ex);
            return;
        }
    }

    /// <summary>
    /// Delete old version of Xenia Manager
    /// </summary>
    /// <returns></returns>
    private void DeleteOldVersion()
    {
        try
        {
            if (File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "XeniaManager.DesktopApp.exe")))
            {
                File.Delete(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "XeniaManager.DesktopApp.exe"));
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message + "\n" + ex);
            return;
        }
    }

    /// <summary>
    /// Installation of new Xenia Manager version
    /// </summary>
    /// <returns></returns>
    private async Task Installation()
    {
        try
        {
            Directory.CreateDirectory(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Update"));

            Extract(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Downloads", "xenia_manager.zip"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Update"));
            Move();
            Cleanup();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message);
            return;
        }
    }

    /// <summary>
    /// Extracts the zip file
    /// </summary>
    /// <param name="fullPath">Path to the zip file</param>
    /// <param name="directory">Path to the extraction directory</param>
    private void Extract(string fullPath, string directory)
    {
        try
        {
            ZipFile.ExtractToDirectory(fullPath, directory, true);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message);
            return;
        }
    }

    /// <summary>
    /// Moves all of the updated files to the correct location
    /// </summary>
    private void Move()
    {
        try
        {
            if (Directory.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Update")))
            {
                foreach (string file in Directory.GetFiles(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Update")))
                {
                    if (Path.GetFileName(file) != "XeniaManager.Updater.exe")
                    {
                        File.Move(file, Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Path.GetFileName(file)),
                            true);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message);
            return;
        }
    }

    /// <summary>
    /// Cleans up after updating to the latest version
    /// </summary>
    /// <returns></returns>
    private void Cleanup()
    {
        try
        {
            if (File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Downloads", "xenia_manager.zip")))
            {
                File.Delete(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Downloads","xenia_manager.zip"));
            }

            if (Directory.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Update")))
            {
                Directory.Delete(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Update"), true);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message);
            return;
        }
    }

    /// <summary>
    /// Launches Xenia Manager and closes the updater
    /// </summary>
    /// <returns></returns>
    private void LaunchXeniaManager()
    {
        try
        {
            Process Launcher = new Process();
            Launcher.StartInfo.WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory;
            Launcher.StartInfo.FileName = "XeniaManager.DesktopApp.exe";
            Launcher.StartInfo.UseShellExecute = true;
            Launcher.Start();
            Environment.Exit(0);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message + "\n" + ex);
        }
    }
}