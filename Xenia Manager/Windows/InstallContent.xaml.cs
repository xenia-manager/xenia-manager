using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;

// Imported
using Serilog;
using Xenia_Manager.Classes;

namespace Xenia_Manager.Windows
{
    /// <summary>
    /// Interaction logic for InstallContent.xaml
    /// </summary>
    public partial class InstallContent : Window
    {
        // Contains every selected content for installation
        List<GameContent> gameContent = new List<GameContent>();

        // Used to send a signal that this window has been closed
        private TaskCompletionSource<bool> _closeTaskCompletionSource = new TaskCompletionSource<bool>();

        public InstallContent(List<GameContent> gameContent)
        {
            InitializeComponent();
            this.gameContent = gameContent;
            InitializeAsync();
            Closed += (sender, args) => _closeTaskCompletionSource.TrySetResult(true);
        }

        /// <summary>
        /// Used to read the content from the list into the UI (ListBox)
        /// </summary>
        private async Task ReadContent()
        {
            try
            {
                foreach (GameContent content in gameContent)
                {
                    string test = "";
                    if (!content.ContentDisplayName.Contains(content.ContentTitle) && content.ContentTitle != "")
                    {
                        test += $"{content.ContentTitle} ";
                    }
                    test += $"{content.ContentDisplayName} ";
                    test += $"({content.ContentType})";
                    ListOfContentToInstall.Items.Add(test);
                }
                await Task.Delay(1);
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
                await ReadContent();
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
        /// Used to emulate a WaitForCloseAsync function that is similar to the one Process Class has
        /// </summary>
        /// <returns></returns>
        public Task WaitForCloseAsync()
        {
            return _closeTaskCompletionSource.Task;
        }

        /// <summary>
        /// Does fade out animation before closing the window
        /// </summary>
        private async Task ClosingAnimation()
        {
            Storyboard FadeOutClosingAnimation = ((Storyboard)Application.Current.FindResource("FadeOutAnimation")).Clone();

            FadeOutClosingAnimation.Completed += (sender, e) =>
            {
                Log.Information("Closing SelectGame window");
                this.Close();
            };

            FadeOutClosingAnimation.Begin(this);
            await Task.Delay(1);
        }

        // UI
        /// <summary>
        /// Closes this window
        /// </summary>
        private async void Exit_Click(object sender, RoutedEventArgs e)
        {
            await ClosingAnimation();
        }

        private void ListOfContentToInstall_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (ListOfContentToInstall.SelectedIndex >= 0)
            {
                Log.Information(gameContent[ListOfContentToInstall.SelectedIndex].ContentTitle);
                Log.Information(gameContent[ListOfContentToInstall.SelectedIndex].ContentDisplayName);
                Log.Information(gameContent[ListOfContentToInstall.SelectedIndex].ContentType);
                Log.Information(gameContent[ListOfContentToInstall.SelectedIndex].ContentTypeValue);
            }
        }
    }
}
