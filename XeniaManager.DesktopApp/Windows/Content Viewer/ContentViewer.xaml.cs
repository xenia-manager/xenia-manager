using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;

// Imported
using Serilog;
using XeniaManager.DesktopApp.Utilities.Animations;

namespace XeniaManager.DesktopApp.Windows
{
    /// <summary>
    /// Interaction logic for ContentViewer.xaml
    /// </summary>
    public partial class ContentViewer : Window
    {
        /// <summary>
        /// Used to execute fade in animation when loading is finished
        /// </summary>
        private void Window_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (this.Visibility == Visibility.Visible)
            {
                WindowAnimations.OpeningAnimation(this);
            }
        }

        /// <summary>
        /// Saves changes to the patch file and closes this window
        /// </summary>
        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            WindowAnimations.ClosingAnimation(this);
        }

        /// <summary>
        /// Executes when user changes selected ContentType
        /// </summary>
        private void ContentTypeList_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            // Checking if the selection is valid
            if (ContentTypeList.SelectedIndex < 0 )
            {
                return;
            }
            try
            {
                if (ContentTypeList.SelectedValue is ContentType selectedContentType)
                {
                    // Get the folder path based on the selected ContentType enum value
                    string folderPath = "";
                    switch (game.EmulatorVersion)
                    {
                        case EmulatorVersion.Canary:
                            folderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ConfigurationManager.AppConfig.XeniaCanary.EmulatorLocation, $@"content\0000000000000000\{game.GameId}\{((uint)selectedContentType).ToString("X8")}");
                            break;
                        case EmulatorVersion.Netplay:
                            folderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ConfigurationManager.AppConfig.XeniaNetplay.EmulatorLocation, $@"content\{game.GameId}\{((uint)selectedContentType).ToString("X8")}");
                            break;
                        default:
                            break;
                    }

                    // Check if the folder exists
                    if (Directory.Exists(folderPath))
                    {
                        // Load everything into the ObservableCollection
                        Files = new ObservableCollection<FileItem>();
                        LoadDirectory(folderPath);
                        InstalledContentTree.ItemsSource = Files;
                    }
                    else
                    {
                        InstalledContentTree.ItemsSource = null;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message + "\nFull Error:\n" + ex);
                MessageBox.Show(ex.Message);
            }
        }

        /// <summary>
        /// Opens the selected storage folder
        /// </summary>
        private void OpenFolder_Click(object sender, RoutedEventArgs e)
        {
            if (ContentTypeList.SelectedIndex < 0)
            {
                return;
            }

            try
            {
                if (ContentTypeList.SelectedValue is ContentType contentType)
                {
                    Process process = new Process();
                    process.StartInfo.FileName = "explorer.exe";
                    string directoryPath = "";
                    switch (game.EmulatorVersion)
                    {
                        case EmulatorVersion.Canary:
                            directoryPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ConfigurationManager.AppConfig.XeniaCanary.EmulatorLocation, $@"content\0000000000000000\{game.GameId}\{((uint)contentType).ToString("X8")}");
                            break;
                        case EmulatorVersion.Netplay:
                            directoryPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ConfigurationManager.AppConfig.XeniaNetplay.EmulatorLocation, $@"content\{game.GameId}\{((uint)contentType).ToString("X8")}");
                            break;
                        default:
                            break;
                    }

                    if (Directory.Exists(directoryPath))
                    {
                        process.StartInfo.Arguments = directoryPath;
                        process.Start();
                    }
                    else
                    {
                        MessageBox.Show($"This game has no directory called '{contentType.ToString().Replace("_", " ")}'");
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message + "\nFull Error:\n" + ex);
                MessageBox.Show(ex.Message);
            }
        }
    }
}
