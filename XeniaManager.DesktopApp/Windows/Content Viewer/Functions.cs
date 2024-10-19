using System;
using System.IO;
using System.Windows;
using System.Windows.Input;

// Imported
using Serilog;

namespace XeniaManager.DesktopApp.Windows
{
    public partial class ContentViewer : Window
    {
        /// <summary>
        /// Used to emulate a WaitForCloseAsync function that is similar to the one Process Class has
        /// </summary>
        /// <returns></returns>
        public Task WaitForCloseAsync()
        {
            return closeWindowCheck.Task;
        }

        /// <summary>
        /// Populates the ContentType combobox with items
        /// </summary>
        private void LoadContentTypes()
        {
            // Populate the ComboBox with the names of the ContentType enum, with underscores replaced by spaces
            var contentTypes = Enum.GetValues(typeof(ContentType)).Cast<ContentType>()
                .Select(ct => new { Value = ct, DisplayName = ct.ToString().Replace("_", " ") })
                .ToList();

            ContentTypeList.ItemsSource = contentTypes;
            ContentTypeList.DisplayMemberPath = "DisplayName";
            ContentTypeList.SelectedValuePath = "Value";
        }

        /// <summary>
        /// Function that executes other functions asynchronously
        /// </summary>
        private async void InitializeAsync()
        {
            try
            {
                await Dispatcher.InvokeAsync(() =>
                {
                    this.Visibility = Visibility.Hidden;
                    Mouse.OverrideCursor = Cursors.Wait;
                    LoadContentTypes();
                    ContentTypeList.SelectedIndex = 0;
                });
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message + "\nFull Error:\n" + ex);
                MessageBox.Show(ex.Message);
            }
            finally
            {
                await Dispatcher.InvokeAsync(() =>
                {
                    this.Visibility = Visibility.Visible;
                    Mouse.OverrideCursor = null;
                });

            }
        }

        // Call this method to load the root directory
        private void LoadDirectory(string mainDirectoryPath)
        {
            List<FileItem> children = LoadChildren(mainDirectoryPath);
            foreach (FileItem child in children)
            {
                Files.Add(child);
            }
        }

        // Recursive method to load child items (directories and files)
        private List<FileItem> LoadChildren(string directoryPath)
        {
            var items = new List<FileItem>();

            // Get all directories
            foreach (var directory in Directory.GetDirectories(directoryPath))
            {
                var directoryInfo = new DirectoryInfo(directory);
                var directoryItem = new FileItem
                {
                    Name = directoryInfo.Name,
                    FullPath = directoryInfo.FullName,
                    IsDirectory = true,
                    Children = LoadChildren(directory) // Load subdirectories and files recursively
                };
                items.Add(directoryItem);
            }

            // Get all files
            foreach (var file in Directory.GetFiles(directoryPath))
            {
                var fileInfo = new FileInfo(file);
                var fileItem = new FileItem
                {
                    Name = fileInfo.Name,
                    FullPath = fileInfo.FullName,
                    IsDirectory = false
                };
                items.Add(fileItem);
            }

            return items;
        }
    }
}