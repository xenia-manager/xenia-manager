using System;
using System.IO;
using System.Windows;
using System.Windows.Input;

// Imported
using Serilog;
using XeniaManager;
using XeniaManager.DesktopApp.Utilities.Animations;
using XeniaManager.Downloader;

namespace XeniaManager.DesktopApp.Windows
{
    /// <summary>
    /// Interaction logic for SelectTitleUpdate.xaml
    /// </summary>
    public partial class SelectTitleUpdate : Window
    {
        // Global variables
        // Game that we're searching title updates for
        private Game game { get; set; }

        // Just a check to see if there are updates available
        private bool updatesAvailable = false;

        // Location to the title update
        public string TitleUpdateLocation { get; set; }

        // Used to send a signal that this window has been closed
        private TaskCompletionSource<bool> closeWindowCheck = new TaskCompletionSource<bool>();

        public SelectTitleUpdate(Game game)
        {
            InitializeComponent();
            this.game = game;
            Closed += (s, args) => closeWindowCheck.TrySetResult(true);
            InitializeAsync();
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
        /// Tries to find all of the available title updates and loads them into the UI
        /// </summary>
        private async Task LoadTitleUpdatesIntoUI()
        {
            // Get all of the title updates into the UI
            List<XboxUnityTitleUpdate> titleUpdates = await XboxUnity.GetTitleUpdates(game.GameId, game.MediaId);
            if (titleUpdates == null || titleUpdates.Count <= 0)
            {
                MessageBox.Show("No Title Updates found");
                WindowAnimations.ClosingAnimation(this);
                return;
            }
            updatesAvailable = true;
            foreach (XboxUnityTitleUpdate titleUpdate in titleUpdates)
            {
                Log.Information($"Version: {titleUpdate.Version}, TUID: {titleUpdate.Id}");
            }
            TitleUpdatesList.ItemsSource = titleUpdates; // Load them into the UI
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
                });

                // Load all of the Title Updates into the UI
                await LoadTitleUpdatesIntoUI();
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
                    if (updatesAvailable)
                    {
                        this.Visibility = Visibility.Visible;
                    }
                    Mouse.OverrideCursor = null;
                });
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
        /// When the user selects a title update from the list
        /// </summary>
        private async void TitleUpdatesList_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            // Check if the selection is valid
            if (TitleUpdatesList.SelectedIndex < 0)
            {
                return;
            }

            XboxUnityTitleUpdate selectedTitleUpdate = TitleUpdatesList.SelectedItem as XboxUnityTitleUpdate;
            if (selectedTitleUpdate == null)
            {
                return;
            }

            // Download selected title update
            Log.Information($"Downloading {selectedTitleUpdate.ToString()}");
            Mouse.OverrideCursor = Cursors.Wait;
            DownloadManager.ProgressChanged += (progress) =>
            {
                Progress.Value = progress;
            };
            string url = $"http://xboxunity.net/Resources/Lib/TitleUpdate.php?tuid={selectedTitleUpdate.Id}";
            await DownloadManager.DownloadFileAsync(url, Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $@"Downloads\{selectedTitleUpdate.ToString()}"));
            TitleUpdateLocation = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $@"Downloads\{selectedTitleUpdate.ToString()}");

            Mouse.OverrideCursor = null;
            WindowAnimations.ClosingAnimation(this);
        }
    }
}
