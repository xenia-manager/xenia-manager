using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;

// Imported
using Serilog;
using XeniaManager.DesktopApp.Pages;
using XeniaManager.DesktopApp.Utilities;
using XeniaManager.DesktopApp.Utilities.Animations;
using XeniaManager.Installation;

namespace XeniaManager.DesktopApp.Windows
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        // UI Interactions
        // Window interactions
        /// <summary>
        /// When window loads, check for updates
        /// </summary>
        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            WindowAnimations.OpeningAnimation(this); // Run "Fade-In" animation
            Log.Information("Application has loaded");

            // Check if Xenia Manager needs to be launched in fullscreen mode
            if (ConfigurationManager.AppConfig.FullscreenMode == true)
            {
                this.WindowState = WindowState.Maximized;
                MainWindowBorder.CornerRadius = new CornerRadius(0);
            }

            // Check for Xenia Manager updates
            if ((ConfigurationManager.AppConfig.Manager.UpdateAvailable == null || ConfigurationManager.AppConfig.Manager.UpdateAvailable == false) && (DateTime.Now - ConfigurationManager.AppConfig.Manager.LastUpdateCheckDate.Value).TotalDays >= 1)
            {
                Log.Information("Checking for Xenia Manager updates");
                if (await InstallationManager.ManagerUpdateChecker())
                {
                    Log.Information("Found newer version of Xenia Manager");
                    Update.Visibility = Visibility.Visible;
                    ConfigurationManager.AppConfig.Manager.UpdateAvailable = true;
                    ConfigurationManager.SaveConfigurationFile();
                }
                else
                {
                    Log.Information("Latest version is already installed");
                    ConfigurationManager.AppConfig.Manager.UpdateAvailable = false;
                    ConfigurationManager.AppConfig.Manager.LastUpdateCheckDate = DateTime.Now;
                    ConfigurationManager.SaveConfigurationFile();
                }
            }
            else if (ConfigurationManager.AppConfig.Manager.UpdateAvailable == true)
            {
                Update.Visibility = Visibility.Visible;
            }
            else
            {
                ConfigurationManager.AppConfig.Manager.UpdateAvailable = false;
                ConfigurationManager.SaveConfigurationFile();
            }

            // Check if Xenia is installed and if it's not, open Welcome Screen
            if (!ConfigurationManager.AppConfig.IsXeniaInstalled())
            {
                Log.Information("No Xenia installed");
                InstallXenia welcome = new InstallXenia();
                welcome.ShowDialog();
            }
        }

        /// <summary>
        /// Enables dragging the window
        /// </summary>
        private void Window_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // Checks if Left Mouse is pressed and if it is, enable DragMove()
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        // TitleBar Button Interactions
        /// <summary>
        /// Opens the Xenia Manager repository page
        /// </summary>
        private void OpenRepository_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://www.github.com/xenia-manager/xenia-manager/",
                UseShellExecute = true,
            });
        }

        /// <summary>
        /// Maximizes/Minimizes Xenia Manager window
        /// </summary>
        private void Maximize_Click(object sender, RoutedEventArgs e)
        {
            if (this.WindowState == WindowState.Maximized)
            {
                this.WindowState = WindowState.Normal;
                ConfigurationManager.AppConfig.FullscreenMode = false;
                MainWindowBorder.CornerRadius = new CornerRadius(10);
            }
            else
            {
                this.WindowState = WindowState.Maximized;
                ConfigurationManager.AppConfig.FullscreenMode = true;
                MainWindowBorder.CornerRadius = new CornerRadius(0);
            }

            ConfigurationManager.SaveConfigurationFile();
        }

        /// <summary>
        /// What happens when Exit button is pressed
        /// </summary>
        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            // Run "Fade-Out" animation and then close the window
            WindowAnimations.ClosingAnimation(this, () => Environment.Exit(0));
        }

        // NavigationBar Button interactions
        /// <summary>
        /// Show Library page on NavigationFrame
        /// </summary>
        private void Home_Click(object sender, RoutedEventArgs e)
        {
            PageNavigationManager.NavigateToPage<Library>(NavigationFrame);
        }

        /// <summary>
        /// Show XeniaSettings page on NavigationFrame
        /// </summary>
        private void XeniaSettings_Click(object sender, RoutedEventArgs e)
        {
            if (GameManager.Games.Count > 0)
            {
                PageNavigationManager.NavigateToPage<XeniaSettings>(NavigationFrame);
            }
            else
            {
                MessageBox.Show("No games installed");
            }
        }

        /// <summary>
        /// Show Xenia Manager Settings page on NavigationFrame
        /// </summary>
        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            PageNavigationManager.NavigateToPage<Settings>(NavigationFrame);
        }
    }
}