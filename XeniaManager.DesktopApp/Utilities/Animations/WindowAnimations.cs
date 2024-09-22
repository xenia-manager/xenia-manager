using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace XeniaManager.DesktopApp.Utilities.Animations
{
    public static class WindowAnimations
    {
        /// <summary>
        /// Do "Fade Out" animation and close the window
        /// </summary>
        /// <param name="window">The window to animate and close</param>
        /// <param name="customAction">Optional action to run after the animation, like closing the app or just the window</param>
        public static void ClosingAnimation(Window window, Action? customAction = null)
        {
            // Fetch "Fade Out" animation
            Storyboard fadeOutClosingAnimation = ((Storyboard)Application.Current.FindResource("FadeOutAnimation")).Clone();

            // Once it's completed, do an action
            fadeOutClosingAnimation.Completed += (sender, e) =>
            {
                //Log.Information($"Closing {window.Title} window");
                if (customAction != null)
                {
                    customAction(); // Custom Action
                }
                else
                {
                    window.Close(); // Default behavior: close the window
                }
            };

            fadeOutClosingAnimation.Begin(window); // Run the "Fade Out" animation
        }

        /// <summary>
        /// Do "Fade In" animation and then show the window
        /// </summary>
        /// <param name="window">The window to animate and close</param>
        public static void OpeningAnimation(Window window)
        {
            // Fetch "Fade In" animation
            Storyboard fadeInStoryboard = ((Storyboard)Application.Current.FindResource("FadeInAnimation")).Clone();
            fadeInStoryboard.Begin(window); // Run the "Fade In" animation
        }

        /// <summary>
        /// NavigationFrame "Fade In" animation
        /// </summary>
        /// <param name="page"></param>
        public static void NavigatedAnimation(Page page)
        {
            // Ensure the page's opacity is set to 0 before starting the animation
            page.Opacity = 0;

            // Do a "Fade In" Animation
            DoubleAnimation fadeInAnimation = new DoubleAnimation(0, 1, TimeSpan.FromSeconds(0.15));
            page.BeginAnimation(Page.OpacityProperty, fadeInAnimation);
        }
    }
}
