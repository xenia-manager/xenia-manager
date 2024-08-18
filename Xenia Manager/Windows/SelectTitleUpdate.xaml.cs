using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;

// Imported
using Serilog;
using Xenia_Manager.Classes;

namespace Xenia_Manager.Windows
{
    /// <summary>
    /// Interaction logic for SelectTitleUpdate.xaml
    /// </summary>
    public partial class SelectTitleUpdate : Window
    {
        // Instance of XboxUnityAPI
        private XboxUnity xboxUnity { get; set; }

        // Stores all of the available title updates
        private List<XboxUnityTitleUpdate> titleUpdates { get; set; }

        // Just a check to see if there are any updates
        private bool haveUpdates = false;

        // Location to the title update
        public string TitleUpdateLocation { get; set; }

        // Used to send a signal that this window has been closed
        private TaskCompletionSource<bool> _closeTaskCompletionSource = new TaskCompletionSource<bool>();

        public SelectTitleUpdate(string gameid, string mediaid)
        {
            InitializeComponent();
            xboxUnity = new XboxUnity(gameid, mediaid);
            InitializeAsync();
            Closed += (sender, args) => _closeTaskCompletionSource.TrySetResult(true);
        }

        /// <summary>
        /// Tries to find all of the available title updates and loads them into the UI
        /// </summary>
        private async Task ReadTitleUpdatesIntoUI()
        {
            try
            {
                // Get all of the title updates into the UI
                List<XboxUnityTitleUpdate> titleUpdates = await xboxUnity.GetTitleUpdates();
                if (titleUpdates != null && titleUpdates.Count > 0)
                {
                    haveUpdates = true;
                    foreach (XboxUnityTitleUpdate titleUpdate in titleUpdates)
                    {
                        Log.Information($"Version: {titleUpdate.Version}, TUID: {titleUpdate.id}");
                    }
                    TitleUpdatesList.ItemsSource = titleUpdates; // Load them into the UI
                }
                else
                {
                    MessageBox.Show("No Title Updates found");
                    await ClosingAnimation();
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message + "\nFull Error:\n" + ex);
                MessageBox.Show(ex.Message);
            }
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
                await ReadTitleUpdatesIntoUI();
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
                    if (haveUpdates)
                    {
                        this.Visibility = Visibility.Visible;
                    }
                    Mouse.OverrideCursor = null;
                });
            }
        }

        /// <summary>
        /// Used to execute fade in animation when loading is finished
        /// </summary>
        private void Window_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (this.Visibility == Visibility.Visible)
            {
                Storyboard fadeInStoryboard = ((Storyboard)Application.Current.FindResource("FadeInAnimation")).Clone();
                if (fadeInStoryboard != null)
                {
                    fadeInStoryboard.Begin(this);
                }
            }
        }

        /// <summary>
        /// Does fade out animation before closing the window
        /// </summary>
        private async Task ClosingAnimation()
        {
            Storyboard FadeOutClosingAnimation = ((Storyboard)Application.Current.FindResource("FadeOutAnimation")).Clone();

            FadeOutClosingAnimation.Completed += (sender, e) =>
            {
                Log.Information("Closing SelectTitleUpdate Window");
                this.Close();
            };

            FadeOutClosingAnimation.Begin(this);
            await Task.Delay(1);
        }

        /// <summary>
        /// Closes this window
        /// </summary>
        private async void Exit_Click(object sender, RoutedEventArgs e)
        {
            await ClosingAnimation();
        }

        /// <summary>
        /// Used to emulate a WaitForCloseAsync function that is similar to the one Process Class has
        /// </summary>
        /// <returns></returns>
        public Task WaitForCloseAsync()
        {
            return _closeTaskCompletionSource.Task;
        }

        /// <summary>
        /// When the user selects a title update from the list
        /// </summary>
        private async void TitleUpdatesList_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            try
            {
                // Checking to make sure everything is selected properly
                ListBox listBox = sender as ListBox;
                if (listBox == null || listBox.SelectedItem == null)
                {
                    return;
                }
                XboxUnityTitleUpdate selectedTitleUpdate = TitleUpdatesList.SelectedItem as XboxUnityTitleUpdate;
                if (selectedTitleUpdate == null)
                {
                    return;
                }

                // Checks if the folder for storing TU's exist
                // If it doesn't, create it
                if (!Directory.Exists(Path.Combine(App.baseDirectory, @"Downloads\")))
                {
                    Directory.CreateDirectory(Path.Combine(App.baseDirectory, @"Downloads\"));
                }
                Mouse.OverrideCursor = Cursors.Wait;
                Log.Information($"Downloading {selectedTitleUpdate.ToString()}");
                App.downloadManager.progressBar = Progress;
                string url = $"http://xboxunity.net/Resources/Lib/TitleUpdate.php?tuid={selectedTitleUpdate.id}";
                await App.downloadManager.DownloadFileAsync(url, Path.Combine(App.baseDirectory, $@"Downloads\{selectedTitleUpdate.ToString()}"));
                TitleUpdateLocation = Path.Combine(App.baseDirectory, $@"Downloads\{selectedTitleUpdate.ToString()}");

                Mouse.OverrideCursor = null;
                await ClosingAnimation();
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message + "\nFull Error:\n" + ex);
                MessageBox.Show(ex.Message);
            }
        }
    }
}
