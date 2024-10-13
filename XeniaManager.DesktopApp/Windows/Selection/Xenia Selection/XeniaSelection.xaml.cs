using System;
using System.Windows;
using System.Windows.Media.Animation;

// Imported
using XeniaManager.DesktopApp.Utilities.Animations;

namespace XeniaManager.DesktopApp.Windows
{
    /// <summary>
    /// Interaction logic for XeniaSelection.xaml
    /// </summary>
    public partial class XeniaSelection : Window
    {
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
        /// User wants to use Xenia Canary
        /// </summary>
        private void Canary_Click(object sender, RoutedEventArgs e)
        {
            UserSelection = EmulatorVersion.Canary;
            WindowAnimations.ClosingAnimation(this);
        }

        /// <summary>
        /// User wants to use Xenia Netplay
        /// </summary>
        private void Netplay_Click(object sender, RoutedEventArgs e)
        {
            UserSelection = EmulatorVersion.Netplay;
            WindowAnimations.ClosingAnimation(this);
        }
    }
}
