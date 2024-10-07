using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Input;

// Imported
using Serilog;
using XeniaManager.DesktopApp.Utilities.Animations;

namespace XeniaManager.DesktopApp.Windows
{
    /// <summary>
    /// Used for showing directories and their files in the TreeView
    /// </summary>
    internal class FileItem
    {
        /// <summary>
        /// Name of the folder/file
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Path to the folder/file
        /// </summary>
        public string FullPath { get; set; }

        /// <summary>
        /// Check that tells us if it's a folder or a file
        /// </summary>
        public bool IsDirectory { get; set; }

        /// <summary>
        /// List of files inside of the folder
        /// </summary>
        public List<FileItem> Children { get; set; }

        public FileItem()
        {
            Children = new List<FileItem>();
        }
    }

    /// <summary>
    /// Enumeration of all supported content types by Xenia according to their FAQ.
    /// </summary>
    internal enum ContentType : uint
    {
        /// <summary>
        /// Saved game data.
        /// </summary>
        Saved_Game = 0x0000001,

        /// <summary>
        /// Content available on the marketplace.
        /// </summary>
        Downloadable_Content = 0x0000002,

        /// <summary>
        /// Content published by a third party.
        /// </summary>
        //Publisher = 0x0000003,

        /// <summary>
        /// Xbox 360 title.
        /// </summary>
        Xbox360_Title = 0x0001000,

        /// <summary>
        /// Installed game.
        /// </summary>
        Installed_Game = 0x0004000,

        /// <summary>
        /// Xbox Original game.
        /// </summary>
        //XboxOriginalGame = 0x0005000,

        /// <summary>
        /// Xbox Title, also used for Xbox Original games.
        /// </summary>
        //XboxTitle = 0x0005000,

        /// <summary>
        /// Game on Demand content.
        /// </summary>
        Game_On_Demand = 0x0007000,

        /// <summary>
        /// Avatar item.
        /// </summary>
        //AvatarItem = 0x0009000,

        /// <summary>
        /// User profile data.
        /// </summary>
        //Profile = 0x0010000,

        /// <summary>
        /// Gamer picture.
        /// </summary>
        //GamerPicture = 0x0020000,

        /// <summary>
        /// Theme for Xbox dashboard or games.
        /// </summary>
        //Theme = 0x0030000,

        /// <summary>
        /// Storage download, typically for storage devices.
        /// </summary>
        //StorageDownload = 0x0050000,

        /// <summary>
        /// Xbox saved game data.
        /// </summary>
        //XboxSavedGame = 0x0060000,

        /// <summary>
        /// Downloadable content for Xbox.
        /// </summary>
        //XboxDownload = 0x0070000,

        /// <summary>
        /// Game demo content.
        /// </summary>
        //GameDemo = 0x0080000,

        /// <summary>
        /// Full game title.
        /// </summary>
        //GameTitle = 0x00A0000,

        /// <summary>
        /// Installer for games or applications.
        /// </summary>
        Installer = 0x00B0000,

        /// <summary>
        /// Arcade title, typically a game from the Xbox Live Arcade.
        /// </summary>
        Arcade_Title = 0x00D0000,
    }

    /// <summary>
    /// Interaction logic for ContentViewer.xaml
    /// </summary>
    public partial class ContentViewer : Window
    {
        // Global variables 
        // Game whose content we're installing
        private Game game { get; set; }

        // Used to send a signal that this window has been closed
        private TaskCompletionSource<bool> closeWindowCheck = new TaskCompletionSource<bool>();

        // Files will be the source of the TreeView
        private ObservableCollection<FileItem> Files { get; set; } = new ObservableCollection<FileItem>();

        public ContentViewer(Game game)
        {
            InitializeComponent();
            this.game = game;
            InitializeAsync();
            Closed += (s, args) => closeWindowCheck.TrySetResult(true);
        }

        // Functions
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

        // UI Interactions
        // Window
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

        // Button
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
    }
}
