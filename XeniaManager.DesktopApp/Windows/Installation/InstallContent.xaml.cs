using System;
using System.IO;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;

// Imported
using Microsoft.Win32;
using Serilog;
using XeniaManager.DesktopApp.Utilities.Animations;
using XeniaManager.VFS;

namespace XeniaManager.DesktopApp.Windows
{
    /// <summary>
    /// Interaction logic for InstallContent.xaml
    /// </summary>
    public partial class InstallContent : Window
    {
        // Global variables
        // List of content that will be installed
        List<GameContent> selectedContent = new List<GameContent>();

        // Game whose content we're installing
        private Game game;

        // Used to send a signal that this window has been closed
        private TaskCompletionSource<bool> closeWindowCheck = new TaskCompletionSource<bool>();

        public InstallContent(Game game)
        {
            InitializeComponent();
            this.game = game;
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
        /// Checks if the file is supported and if it is, adds it to the ContentList
        /// </summary>
        /// <param name="contentFile"></param>
        private void AddContentFile(string contentFile)
        {
            try
            {
                // Read the necessary info from the file
                Log.Information($"{Path.GetFileName(contentFile)} is currently supported");
                var (contentType, contentTypeValue) = STFS.GetContentType();

                // Add it to the list of content
                GameContent newContent = new GameContent
                {
                    GameId = game.GameId,
                    Title = STFS.GetTitle(),
                    DisplayName = STFS.GetDisplayName(),
                    ContentType = contentType.ToString().Replace('_', ' '),
                    ContentTypeValue = $"{contentTypeValue:X8}",
                    Location = contentFile
                };

                // Checking for duplicates and if it has valid ContentType
                if (newContent.ContentType != null && !selectedContent.Any(content => content.Location == newContent.Location))
                {
                    selectedContent.Add(newContent);
                }
            }
            catch (InvalidOperationException IOex)
            {
                Log.Error($"Error: {IOex}");
            }
        }

        /// <summary>
        /// Loads the selected content into the ListBox
        /// </summary>
        private void LoadContentIntoUI()
        {
            ContentList.Items.Clear();
            foreach (GameContent content in selectedContent)
            {
                string contentDisplayName = "";
                if (!content.DisplayName.Contains(content.Title) && content.Title != "")
                {
                    contentDisplayName += $"{content.Title} ";
                }
                contentDisplayName += $"{content.DisplayName} ";
                contentDisplayName += $"({content.ContentType})";
                ContentList.Items.Add(contentDisplayName);
            }
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

        // Buttons
        /// <summary>
        /// Closes this window
        /// </summary>
        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            WindowAnimations.ClosingAnimation(this);
        }

        /// <summary>
        /// Goes through the selected items and installs them
        /// </summary>
        private void Confirm_Click(object sender, RoutedEventArgs e)
        {
            // Check if there's something to install
            if (selectedContent.Count <= 0)
            {
                Log.Information("There's nothing to install");
                return;
            }

            string installedItems = "";
            Mouse.OverrideCursor = Cursors.Wait;
            foreach (GameContent content in selectedContent)
            {
                GameManager.InstallContent(game, content);
                installedItems += $"{content.DisplayName}\n";
            }
            Mouse.OverrideCursor = null;
            MessageBox.Show($"Installed content:\n{installedItems}");

            // Close the window
            WindowAnimations.ClosingAnimation(this);
        }

        /// <summary>
        /// Opens file dialog where user selects content he wants to install and then adds it to the list
        /// </summary>
        private void AddLocalContent_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Open File Dialog so the user can select the content he wants to install
                Log.Information("Open file dialog so user can select the content that he wants to install");
                OpenFileDialog openFileDialog = new OpenFileDialog();
                openFileDialog.Title = $"Select files for {game.Title}";
                openFileDialog.Filter = "All Files|*";
                openFileDialog.Multiselect = true;
                bool? result = openFileDialog.ShowDialog();
                // Check if user cancelled file dialog selection
                if (result != true)
                {
                    return;
                }

                // Check every file selected if it's a supported file
                Mouse.OverrideCursor = Cursors.Wait;
                foreach (string file in openFileDialog.FileNames)
                {
                    // Check if the selected file is supported
                    Log.Information($"Checking if {Path.GetFileName(file)} is supported");
                    STFS.Open(file);
                    if (!STFS.SupportedFile())
                    {
                        Log.Information($"{Path.GetFileName(file)} is currently not supported");
                        MessageBox.Show($"{Path.GetFileName(file)} is currently not supported");
                        continue;
                    }

                    AddContentFile(file);
                }

                // Load the content into the UI
                LoadContentIntoUI();
                Mouse.OverrideCursor = null;
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message + "\nFull Error:\n" + ex);
                MessageBox.Show(ex.Message);
                return;
            }
        }

        /// <summary>
        /// Opens a new window and searches for title updates on XboxUnity
        /// </summary>
        private async void XboxUnitySearch_Click(object sender, RoutedEventArgs e)
        {
            // Check if the game has TitleID and MediaID
            if (game.GameId == null || game.MediaId == null)
            {
                // Something is wrong
                if (game.GameId == null && game.MediaId == null)
                {
                    MessageBox.Show("Game ID and Media ID are missing.");
                }
                else if (game.GameId != null && game.MediaId == null)
                {
                    MessageBox.Show("Media ID is missing.");
                }
                else
                {
                    MessageBox.Show("Game ID is missing.");
                }
                return;
            }

            // Open window for searching for Title Updates on XboxUnity
            SelectTitleUpdate selectTitleUpdate = new SelectTitleUpdate(game);
            selectTitleUpdate.ShowDialog();
            await selectTitleUpdate.WaitForCloseAsync();
            if (selectTitleUpdate.TitleUpdateLocation == null)
            {
                Log.Information("No content file to add");
                return;
            }

            // Check if the selected file is supported
            Log.Information($"Checking if {Path.GetFileName(selectTitleUpdate.TitleUpdateLocation)} is supported");
            STFS.Open(selectTitleUpdate.TitleUpdateLocation);
            if (!STFS.SupportedFile())
            {
                Log.Information($"{Path.GetFileName(selectTitleUpdate.TitleUpdateLocation)} is currently not supported");
                MessageBox.Show($"{Path.GetFileName(selectTitleUpdate.TitleUpdateLocation)} is currently not supported");
                return;
            }

            AddContentFile(selectTitleUpdate.TitleUpdateLocation);
            // Load the content into the UI
            LoadContentIntoUI();
        }

        /// <summary>
        /// If there's a selected item in the ListBox, it will remove it from the list
        /// </summary>
        private void RemoveContent_Click(object sender, RoutedEventArgs e)
        {
            // Checking if something is selected
            if (ContentList.SelectedIndex < 0)
            {
                Log.Information("Nothing is selected to remove");
                return;
            }

            // Removing selected content
            Log.Information($"Removing {selectedContent[ContentList.SelectedIndex].DisplayName}");
            selectedContent.RemoveAt(ContentList.SelectedIndex);
            ContentList.Items.RemoveAt(ContentList.SelectedIndex);

            // Reseting the selection in the ContentList
            ContentList.SelectedIndex = -1;
        }
    }
}
