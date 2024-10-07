using System;
using System.Windows;
using System.Windows.Input;

// Imported
using Serilog;
using XeniaManager.DesktopApp.Utilities.Animations;

namespace XeniaManager.DesktopApp.Windows
{
    /// <summary>
    /// Interaction logic for SelectTitleUpdate.xaml
    /// </summary>
    public partial class SelectTitleUpdate : Window
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
    }
}
