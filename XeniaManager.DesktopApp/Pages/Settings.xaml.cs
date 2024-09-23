using System;
using System.Windows;
using System.Windows.Controls;

// Imported
using XeniaManager.DesktopApp.Windows;

namespace XeniaManager.DesktopApp.Pages
{
    /// <summary>
    /// Interaction logic for Settings.xaml
    /// </summary>
    public partial class Settings : Page
    {
        public Settings()
        {
            InitializeComponent();
        }

        // UI Interactions
        /// <summary>
        /// Opens InstallXenia window where the user can install or uninstall different Xenia versions
        /// </summary>
        private void OpenXeniaInstaller_Click(object sender, RoutedEventArgs e)
        {
            InstallXenia installXenia = new InstallXenia();
            installXenia.ShowDialog();
        }
    }
}
