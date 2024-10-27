using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;

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
                    Log.Information($"Currently selected content type: {selectedContentType}");
                    if (selectedContentType == ContentType.Saved_Game)
                    {
                        cmbGamerProfiles.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        cmbGamerProfiles.Visibility = Visibility.Collapsed;
                    }
                    
                    // Get the folder path based on the selected ContentType enum value
                    string folderPath = GetContentFolder(selectedContentType, game.EmulatorVersion);
                    
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
                    string directoryPath = GetContentFolder(contentType, game.EmulatorVersion);

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

        private void cmbGamerProfiles_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbGamerProfiles.SelectedIndex < 0)
            {
                return;
            }
            Log.Information($"Currently selected profile: {cmbGamerProfiles.SelectedItem.ToString()}");
            ContentTypeList_SelectionChanged(ContentTypeList, null);
        }
    }
}
