using System.Windows;
using System.Windows.Media.Animation;

// Imported
using XeniaManager.DesktopApp.Windows;

namespace XeniaManager.DesktopApp.CustomControls
{
    public partial class GameButton
    {
        /// <summary>
        /// Event handler for clicking the game button
        /// </summary>
        private async void ButtonClick(object sender, RoutedEventArgs e)
        {
            // Logic for launching the game and animations
            MainWindow mainWindow = Application.Current.MainWindow as MainWindow;
            DoubleAnimation fadeOutAnimation = new DoubleAnimation(1, 0, TimeSpan.FromSeconds(0.15));
            TaskCompletionSource<bool> animationCompleted = new TaskCompletionSource<bool>();

            fadeOutAnimation.Completed += (_, _) =>
            {
                mainWindow.Visibility = Visibility.Hidden;
                animationCompleted.SetResult(true);
            };

            mainWindow.BeginAnimation(Window.OpacityProperty, fadeOutAnimation);
            await animationCompleted.Task;

            // Launch the game
            GameManager.LaunchGame(Game);

            // Restore the main window
            DoubleAnimation fadeInAnimation = new DoubleAnimation(0, 1, TimeSpan.FromSeconds(0.15));
            mainWindow.Visibility = Visibility.Visible;
            mainWindow.BeginAnimation(Window.OpacityProperty, fadeInAnimation);

            // Save changes and reload games
            GameManager.Save();

            // When the user closes the game/emulator, reload the UI
            Library.LoadGames();
        }
    }
}