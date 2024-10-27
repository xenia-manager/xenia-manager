using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

// Imported
using Microsoft.Win32;
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
                        SavedGamesButtons.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        cmbGamerProfiles.Visibility = Visibility.Collapsed;
                        SavedGamesButtons.Visibility = Visibility.Hidden;
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

        /// <summary>
        /// Updates the "Saved Games" content folder display
        /// </summary>
        private void cmbGamerProfiles_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbGamerProfiles.SelectedIndex < 0)
            {
                return;
            }
            Log.Information($"Currently selected profile: {cmbGamerProfiles.SelectedItem.ToString()}");
            ContentTypeList_SelectionChanged(ContentTypeList, null);
        }
        
        /// <summary>
        /// Opens file dialog and imports the save games (Has to follow the correct format
        /// </summary>
        private void btnImport_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Title = "Select a save file",
                Filter = "Supported files|*.zip"
            };
            if (openFileDialog.ShowDialog() != true)
            {
                return;
            }
            Mouse.OverrideCursor = Cursors.Wait;
            
            // Where the actual save file should be
            string saveFileLocation = new DirectoryInfo(GetContentFolder(ContentType.Saved_Game, game.EmulatorVersion))?
                .Parent?
                .Parent?
                .FullName;
            Log.Information($"Save file location: {saveFileLocation}");
            
            // Creating the directory in case it's missing
            if (!Directory.Exists(saveFileLocation))
            {
                Directory.CreateDirectory(saveFileLocation);
            }
            
            // Extract the save file to the correct folder
            try
            {
                ZipFile.ExtractToDirectory(openFileDialog.FileName, saveFileLocation, true);

                // Reload UI
                ContentTypeList_SelectionChanged(ContentTypeList, null);
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message + "\nFull Error:\n" + ex);
                Mouse.OverrideCursor = null;
                MessageBox.Show(ex.Message);
            }
            
            Mouse.OverrideCursor = null;
        }

        /// <summary>
        /// Exports saves to the desktop
        /// </summary>
        private void btnExport_Click(object sender, RoutedEventArgs e)
        {
            Mouse.OverrideCursor = Cursors.Wait;
            // Export path
            string destination = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), $"{DateTime.Now:yyyyMMdd_HHmmss} - {game.Title} Save File.zip");
            Log.Information($"Destination: {destination}");
            
            // Where the actual save file is
            string saveFileLocation = Path.Combine(GetContentFolder(ContentType.Saved_Game, game.EmulatorVersion));
            Log.Information($"Save file location: {saveFileLocation}");
            
            // Where the headers for the save file are (Useful for some games to have)
            string headersLocation = Path.Combine(Path.GetDirectoryName(GetContentFolder(ContentType.Saved_Game, game.EmulatorVersion)), @"Headers\00000001");
            Log.Information($"Headers location: {headersLocation}");
            
            GameManager.ExportSaveGames(game, destination, saveFileLocation, headersLocation);
            
            Mouse.OverrideCursor = null;
            Log.Information($"The save file for '{game.Title}' has been successfully exported to the desktop");
            MessageBox.Show($"The save file for '{game.Title}' has been successfully exported to the desktop");
        }
    }
}
