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
        /// Load selected values into the UI
        /// </summary>
        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            // Load the selected theme into the ui
            foreach (ComboBoxItem theme in ThemeSelector.Items)
            {
                if (theme.Content.ToString() == ConfigurationManager.AppConfig.SelectedTheme)
                {
                    ThemeSelector.SelectedItem = theme;
                    break;
                }
            }
        }
        /// <summary>
        /// Opens InstallXenia window where the user can install or uninstall different Xenia versions
        /// </summary>
        private void OpenXeniaInstaller_Click(object sender, RoutedEventArgs e)
        {
            InstallXenia installXenia = new InstallXenia();
            installXenia.ShowDialog();
        }

        /// <summary>
        /// Loads the selected theme
        /// </summary>
        private void ThemeSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Check if the selection is valid
            if (ThemeSelector.SelectedIndex < 0)
            {
                return;
            }

            // Switch the theme in the configuration
            switch (ThemeSelector.SelectedIndex)
            {
                case 1:
                    ConfigurationManager.AppConfig.SelectedTheme = "Light";
                    break;
                case 2:
                    ConfigurationManager.AppConfig.SelectedTheme = "Dark";
                    break;
                case 3:
                    ConfigurationManager.AppConfig.SelectedTheme = "AMOLED";
                    break;
                case 4:
                    ConfigurationManager.AppConfig.SelectedTheme = "Nord";
                    break;
                default:
                    ConfigurationManager.AppConfig.SelectedTheme = "System Default";
                    break;
            }

            // Load the theme and save changes to the file
            App.LoadTheme();
            ConfigurationManager.SaveConfigurationFile();
        }
    }
}
