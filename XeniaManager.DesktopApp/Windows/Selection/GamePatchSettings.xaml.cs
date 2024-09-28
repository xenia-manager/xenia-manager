using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Input;

// Imported
using Serilog;
using Tomlyn.Model;
using Tomlyn;
using XeniaManager.DesktopApp.Utilities.Animations;

namespace XeniaManager.DesktopApp.Windows
{
    /// <summary>
    /// Interaction logic for GamePatchSettings.xaml
    /// </summary>
    public partial class GamePatchSettings : Window
    {
        // Global variables
        /// <summary>
        /// Location to the game specific patch file
        /// </summary>
        private string patchLocation { get; set; }

        // Used to send a signal that this window has been closed
        private TaskCompletionSource<bool> closeWindowCheck = new TaskCompletionSource<bool>();

        /// <summary>
        /// Holds every patch as a Patch class
        /// </summary>
        public ObservableCollection<Patch> Patches = new ObservableCollection<Patch>();

        /// <summary>
        /// Initializes this window
        /// </summary>
        /// <param name="patchLocation">Location to the patch we're loading and editing</param>
        public GamePatchSettings(string gameTitle, string patchLocation)
        {
            InitializeComponent();
            GameTitle.Text = gameTitle;
            this.patchLocation = patchLocation;
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
                    Patches = GameManager.ReadPatchFile(patchLocation);
                    PatchesList.ItemsSource = Patches;
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
            Log.Information("Saving changes");
            GameManager.SavePatchFile(Patches, patchLocation);
            WindowAnimations.ClosingAnimation(this);
        }
    }
}
