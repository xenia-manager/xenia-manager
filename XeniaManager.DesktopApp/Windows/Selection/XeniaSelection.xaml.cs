using System;
using System.Windows;
using System.Windows.Media.Animation;

// Imported
using Serilog;
using XeniaManager.DesktopApp.Utilities.Animations;

namespace XeniaManager.DesktopApp.Windows
{
    /// <summary>
    /// Interaction logic for XeniaSelection.xaml
    /// </summary>
    public partial class XeniaSelection : Window
    {
        // Globar variables
        // This is used to know what option user selected
        public EmulatorVersion UserSelection { get; private set; }
        public XeniaSelection()
        {
            InitializeComponent();
            CheckInstalledXeniaVersions();
        }

        // Functions
        /// <summary>
        /// Check to see what Xenia version is installed
        /// </summary>
        private void CheckInstalledXeniaVersions()
        {
            try
            {
                /*
                // Checking if Xenia Stable is installed
                if (ConfigurationManager.AppConfig.XeniaStable != null)
                {
                    Stable.Visibility = Visibility.Visible;
                }
                else
                {
                    Stable.Visibility = Visibility.Collapsed;
                }*/

                // Checking if Xenia Canary is installed
                if (ConfigurationManager.AppConfig.XeniaCanary != null)
                {
                    Canary.Visibility = Visibility.Visible;
                }
                else
                {
                    Canary.Visibility = Visibility.Collapsed;
                }

                // Checking if Xenia Netplay is installed
                if (ConfigurationManager.AppConfig.XeniaNetplay != null)
                {
                    Netplay.Visibility = Visibility.Visible;
                }
                else
                {
                    Netplay.Visibility = Visibility.Collapsed;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message + "\nFull Error:\n" + ex);
                MessageBox.Show(ex.Message);
            }
        }

        // UI Interactions
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

        /*
        /// <summary>
        /// User wants to use Xenia Stable
        /// </summary>
        private void Stable_Click(object sender, RoutedEventArgs e)
        {
            UserSelection = EmulatorVersion.Stable;
            WindowAnimations.ClosingAnimation(this);
        }
        */

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
