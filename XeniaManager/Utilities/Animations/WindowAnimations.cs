using System;
using System.Windows;
using System.Windows.Media.Animation;

// Imported
using Serilog;

namespace XeniaManager.Utilities.Animations
{
    public static class WindowAnimations
    {
        /// <summary>
        /// Do "fade-out" animation and close the window
        /// </summary>
        /// <param name="window">The window to animate and close</param>
        /// <param name="customAction">Optional action to run after the animation, like closing the app or just the window</param>
        public static void ClosingAnimation(Window window, Action? customAction = null)
        {
            // Fetch fadeout animation
            Storyboard fadeOutClosingAnimation = ((Storyboard)Application.Current.FindResource("FadeOutAnimation")).Clone();

            // Once it's completed, do an action
            fadeOutClosingAnimation.Completed += (sender, e) =>
            {
                Log.Information($"Closing {window.Title} window");
                if (customAction != null)
                {
                    customAction(); // Custom Action
                }
                else
                {
                    window.Close(); // Default behavior: close the window
                }
            };

            fadeOutClosingAnimation.Begin(window); // Run the "fade-out" animation
        }
    }
}
