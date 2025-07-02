using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Input;
using System.Windows.Shell;

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
    public partial class MainWindow
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        // UI Interactions
        // Window interactions
        /// <summary>
        /// When window loads, check if Xenia Manager launched in fullscreen mode to remove the rounded corners and to check for updates
        /// </summary>
        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            WindowAnimations.OpeningAnimation(this); // Run "Fade-In" animation
            Log.Information("Application has loaded");

            // Check if Xenia Manager needs to be launched in fullscreen mode
            if (ConfigurationManager.AppConfig.FullscreenMode)
            {
                this.WindowState = WindowState.Maximized;
                BrdMainWindow.CornerRadius = new CornerRadius(0);
            }

            // Check for Xenia Manager updates
            /*
            if ((ConfigurationManager.AppConfig.Manager.UpdateAvailable == null ||
                 ConfigurationManager.AppConfig.Manager.UpdateAvailable == false) &&
                (DateTime.Now - ConfigurationManager.AppConfig.Manager.LastUpdateCheckDate).TotalDays >= 1)
            {
                Log.Information("Checking for Xenia Manager updates");
                if (await InstallationManager.ManagerUpdateChecker())
                {
                    Log.Information("Found newer version of Xenia Manager");
                    BtnUpdate.Visibility = Visibility.Visible;
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
                BtnUpdate.Visibility = Visibility.Visible;
            }
            else
            {
                ConfigurationManager.AppConfig.Manager.UpdateAvailable = false;
                ConfigurationManager.SaveConfigurationFile();
            }*/

            // Check if Xenia is installed and if it's not, open Welcome Screen
            if (!ConfigurationManager.AppConfig.IsXeniaInstalled())
            {
                Log.Information("No Xenia installed");
                InstallXenia welcome = new InstallXenia();
                welcome.ShowDialog();
            }
        }

        /// <summary>
        /// Enables dragging of the window
        /// </summary>
        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Checks if Left Mouse is pressed and if it is, enable DragMove()
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }
        
        /// <summary>
        /// Changes the main window border thickness depending on if the app is in fullscreen mode or not
        /// </summary>
        private void Window_StateChanged(object sender, EventArgs e)
        {
            if (WindowState == WindowState.Maximized)
            {
                BrdMainWindow.BorderThickness = new Thickness(10);
            }
            else
            {
                BrdMainWindow.BorderThickness = new Thickness(2);
            }
        }

        // TitleBar Button Interactions
        /// <summary>
        /// Opens the Xenia Manager repository page
        /// </summary>
        private void BtnRepository_Click(object sender, RoutedEventArgs e)
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
        private void BtnMaximize_Click(object sender, RoutedEventArgs e)
        {
            if (this.WindowState == WindowState.Maximized)
            {
                this.WindowState = WindowState.Normal;
                ConfigurationManager.AppConfig.FullscreenMode = false;
                BrdMainWindow.CornerRadius = new CornerRadius(10);
            }
            else
            {
                this.WindowState = WindowState.Maximized;
                ConfigurationManager.AppConfig.FullscreenMode = true;
                BrdMainWindow.CornerRadius = new CornerRadius(0);
            }

            ConfigurationManager.SaveConfigurationFile();
        }

        /// <summary>
        /// Runs the "Fade-Out" animation before closing the Xenia Manager
        /// </summary>
        private void BtnExit_Click(object sender, RoutedEventArgs e)
        {
            WindowAnimations.ClosingAnimation(this, () => Environment.Exit(0));
        }

        // NavigationBar Button interactions
        /// <summary>
        /// Navigates to game library
        /// </summary>
        private void BtnHome_Click(object sender, RoutedEventArgs e)
        {
            TblkWindowTitle.Text = "Xenia Manager";
            PageNavigationManager.NavigateToPage<Library>(FrmNavigation);
        }

        /// <summary>
        /// Navigates to Xenia settings
        /// </summary>
        private void BtnXeniaSettings_Click(object sender, RoutedEventArgs e)
        {
            // Checking what emulator versions are installed
            List<EmulatorVersion> installedXeniaVersions = new List<EmulatorVersion>();
            if (ConfigurationManager.AppConfig.XeniaCanary != null)
                installedXeniaVersions.Add(EmulatorVersion.Canary);
            if (ConfigurationManager.AppConfig.XeniaMousehook != null)
                installedXeniaVersions.Add(EmulatorVersion.Mousehook);
            if (ConfigurationManager.AppConfig.XeniaNetplay != null)
                installedXeniaVersions.Add(EmulatorVersion.Netplay);
            if (GameManager.Games.Count > 0 || installedXeniaVersions.Count > 0)
            {
                TblkWindowTitle.Text = "Xenia Manager";
                PageNavigationManager.NavigateToPage<XeniaSettings>(FrmNavigation);
            }
            else
            {
                MessageBox.Show("No games installed");
            }
        }

        /// <summary>
        /// Navigates to Xenia Manager settings
        /// </summary>
        private void BtnSettings_Click(object sender, RoutedEventArgs e)
        {
            TblkWindowTitle.Text = $"Xenia Manager v{Assembly.GetExecutingAssembly().GetName().Version.Major}.{Assembly.GetExecutingAssembly().GetName().Version.Minor}.{Assembly.GetExecutingAssembly().GetName().Version.Build}";
            PageNavigationManager.NavigateToPage<Settings>(FrmNavigation);
        }

        /// <summary>
        /// Updates the Xenia Manager information in the config.json and opens Xenia Manager Updater
        /// </summary>
        private async void BtnUpdate_Click(object sender, RoutedEventArgs e)
        {
            // Updating Xenia Manager info
            Log.Information(InstallationManager.LatestXeniaManagerRelease.Version);
            if (InstallationManager.LatestXeniaManagerRelease.Version == null)
            {
                await InstallationManager.ManagerUpdateChecker();
            }

            ConfigurationManager.AppConfig.Manager.Version = InstallationManager.LatestXeniaManagerRelease.Version;
            ConfigurationManager.AppConfig.Manager.ReleaseDate =
                InstallationManager.LatestXeniaManagerRelease.ReleaseDate;
            ConfigurationManager.AppConfig.Manager.UpdateAvailable = false;
            ConfigurationManager.AppConfig.Manager.LastUpdateCheckDate = DateTime.Now;
            ConfigurationManager.SaveConfigurationFile();

            // Opening the latest page to show changes
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://www.github.com/xenia-manager/xenia-manager/releases/latest",
                UseShellExecute = true,
            });

            // Launching Xenia Manager Updater
            using (Process process = new Process())
            {
                process.StartInfo.WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory;
                process.StartInfo.FileName = "XeniaManager.Updater.exe";
                process.StartInfo.UseShellExecute = true;
                process.Start();
            }

            Log.Information("Closing Xenia Manager for update");
            Environment.Exit(0);
        }
    }
}