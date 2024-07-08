using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;

// Imported
using Serilog;

namespace Xenia_Manager.Windows
{
    /// <summary>
    /// Interaction logic for XeniaSelection.xaml
    /// </summary>
    public partial class XeniaSelection : Window
    {
        // Used to send a signal that this window has been closed
        private TaskCompletionSource<bool> _closeTaskCompletionSource = new TaskCompletionSource<bool>();

        // This is used to know what option user selected
        public string UserSelection { get; private set; }

        public XeniaSelection()
        {
            InitializeComponent();
            InitializeAsync();
            Closed += (sender, args) => _closeTaskCompletionSource.TrySetResult(true);
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

        /// <summary>
        /// User wants to use Xenia Stable for the specific game
        /// </summary>
        private async void Stable_Click(object sender, RoutedEventArgs e)
        {
            UserSelection = "Stable";
            await ClosingAnimation();
        }

        /// <summary>
        /// User wants to use Xenia Canary for the specific game
        /// </summary>
        private async void Canary_Click(object sender, RoutedEventArgs e)
        {
            UserSelection = "Canary";
            await ClosingAnimation();
        }
    }
}
