using System;
using System.IO;
using System.Windows;

// Imported
using Microsoft.Win32;
using Newtonsoft.Json.Linq;
using Serilog;
using XeniaManager;
using XeniaManager.DesktopApp.Windows;
using XeniaManager.Installation;
using XeniaManager.Logging;

namespace XeniaManager.DesktopApp
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        // Functions
        /// <summary>
        /// Just checks if the necessary folders exist and if they don't, create them
        /// </summary>
        private void CheckIfFoldersExist()
        {
            // Check if Config folder exists
            if (!Directory.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config")))
            {
                Directory.CreateDirectory(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config"));
            }

            // Check if Cache folder exists
            if (!Directory.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Cache")))
            {
                Directory.CreateDirectory(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Cache"));
            }

            // Check if Downloads folder exists
            if (!Directory.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Downloads")))
            {
                Directory.CreateDirectory(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Downloads"));
            }

            // Check if Tools folder exists
            if (!Directory.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Tools")))
            {
                Directory.CreateDirectory(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Tools"));
            }
        }

        /// <summary>
        /// Replaces only the resource dictionary responsible for theming.
        /// </summary>
        /// <param name="themeUri">Uri of the new theme to load</param>
        private static void ReplaceThemeResourceDictionary(Uri themeUri)
        {
            // Identify the application resources
            ResourceDictionary appResources = Application.Current.Resources;

            // Find the existing theme resource dictionary (optional step to identify by key, if you have one)
            ResourceDictionary existingThemeDictionary = appResources.MergedDictionaries
                .FirstOrDefault(d => d.Source != null && d.Source.ToString().Contains("/Resources/Themes/"));

            // If a theme dictionary exists, remove it
            if (existingThemeDictionary != null)
            {
                Log.Information($"Removing the current theme: {existingThemeDictionary.Source}");
                appResources.MergedDictionaries.Remove(existingThemeDictionary);
            }

            // Add the new theme resource dictionary
            ResourceDictionary newThemeDictionary = new ResourceDictionary() { Source = themeUri };
            appResources.MergedDictionaries.Add(newThemeDictionary);

            Log.Information($"Applied new theme: {ConfigurationManager.AppConfig.SelectedTheme}");
        }

        /// <summary>
        /// Loads the selected theme into the UI, replacing only the resource dictionary used for theming.
        /// </summary>
        public static void LoadTheme()
        {
            try
            {
                Log.Information("Checking which theme is currently selected");

                // Define the theme Uri based on the selected theme
                Uri themeUri = null;
                switch (ConfigurationManager.AppConfig.SelectedTheme)
                {
                    case "Light":
                        Log.Information("Applying Light theme");
                        themeUri = new Uri("pack://application:,,,/Resources/Themes/Light.xaml", UriKind.Absolute);
                        break;

                    case "Dark":
                        Log.Information("Applying Dark theme");
                        themeUri = new Uri("pack://application:,,,/Resources/Themes/Dark.xaml", UriKind.Absolute);
                        break;

                    case "AMOLED":
                        Log.Information("Applying Dark (AMOLED) theme");
                        themeUri = new Uri("pack://application:,,,/Resources/Themes/AMOLED.xaml", UriKind.Absolute);
                        break;

                    case "Nord":
                        Log.Information("Applying Nord theme");
                        themeUri = new Uri("pack://application:,,,/Resources/Themes/Nord.xaml", UriKind.Absolute);
                        break;

                    case "System Default":
                        // Check the Windows system theme
                        Log.Information("Checking the selected theme in Windows");
                        using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize"))
                        {
                            if (key != null)
                            {
                                object value = key.GetValue("AppsUseLightTheme");
                                if (value != null && int.TryParse(value.ToString(), out int appsUseLightTheme))
                                {
                                    themeUri = appsUseLightTheme == 0
                                        ? new Uri("pack://application:,,,/Resources/Themes/Dark.xaml", UriKind.Absolute)
                                        : new Uri("pack://application:,,,/Resources/Themes/Light.xaml", UriKind.Absolute);
                                    Log.Information(appsUseLightTheme == 0 ? "Dark theme detected in Windows" : "Light theme detected in Windows");
                                }
                            }

                            if (themeUri == null)
                            {
                                Log.Information("Couldn't detect the selected theme in Windows, applying default Light theme");
                                themeUri = new Uri("pack://application:,,,/Resources/Themes/Light.xaml", UriKind.Absolute);
                            }
                        }
                        break;

                    default:
                        Log.Information("No theme selected, applying default Light theme");
                        themeUri = new Uri("pack://application:,,,/Resources/Themes/Light.xaml", UriKind.Absolute);
                        break;
                }

                // Replace the current theme resource dictionary
                if (themeUri != null)
                {
                    ReplaceThemeResourceDictionary(themeUri);
                }
            }
            catch (Exception ex)
            {
                Log.Error($"An error occurred: {ex.Message}");
                MessageBox.Show($"An error occurred: {ex.Message}");
            }
        }

        /// <summary>
        /// Checks launch arguments and starts a game if it's found
        /// </summary>
        private static void CheckLaunchArguments(string[] arguments)
        {
            // Check if there are launch arguments and if there are games in Xenia Manager
            if (arguments.Length < 1 || GameManager.Games.Count == 0)
            {
                return;
            }

            Log.Information("Checking launch arguments");
            foreach (string argument in arguments)
            {
                // Skipping "-console" argument since it's already checked
                if (argument != "-console")
                {
                    Log.Information($"Current launch argument: {argument}");
                    Game game = GameManager.Games.FirstOrDefault(game => string.Equals(game.Title, argument, StringComparison.OrdinalIgnoreCase));
                    if (game != null)
                    {
                        GameManager.LaunchGame(game);
                        GameManager.Save();
                        MainWindow mainWindow = new MainWindow();
                        mainWindow.Visibility = Visibility.Collapsed;
                        mainWindow.Show();
                        Application.Current.Shutdown();
                    }
                }
            }
        }

        /// <summary>
        /// Checks if necessary tools are installed, if missing, install them
        /// </summary>
        private static void CheckTools()
        {
            // Checking if Xenia VFS Dump Tool is installed
            Log.Information("Checking if Xenia VFS Dump Tool is installed");
            if (ConfigurationManager.AppConfig.VFSDumpToolLocation == null)
            {
                Log.Warning("Xenia VFS Dump Tool is missing. Installing it now");
                InstallationManager.DownloadXeniaVFSDumper();
                Log.Information("Xenia VFS Dump Tool is installed");
            }
        }

        /// <summary>
        /// Checks for updates for Xenia
        /// </summary>
        private static async void CheckForXeniaUpdates()
        {
            // Check if Xenia Canary is installed
            if (ConfigurationManager.AppConfig.XeniaCanary != null && (ConfigurationManager.AppConfig.XeniaCanary.LastUpdateCheckDate == null || (DateTime.Now - ConfigurationManager.AppConfig.XeniaCanary.LastUpdateCheckDate.Value).TotalDays >= 1))
            {
                (bool updateAvailable, JObject latestRelease) = await InstallationManager.Xenia.CheckForUpdates(EmulatorVersion.Canary);
                // Check for updates for Xenia Canary
                if (updateAvailable)
                {
                    Log.Information("There is an update for Xenia Canary");
                    // Ask the user if he wants to update Xenia Canary
                    MessageBoxResult result = MessageBox.Show($"Found a new version of Xenia {EmulatorVersion.Canary}. Do you want to update it?", "Confirmation",MessageBoxButton.YesNo);
                    if (result == MessageBoxResult.Yes)
                    {
                        await InstallationManager.Xenia.UpdateCanary(latestRelease);
                        MessageBox.Show($"Xenia {EmulatorVersion.Canary} has been updated to the latest build.");
                    }
                }
                else
                {
                    Log.Information("No updates available for Xenia Canary");
                }
            }
        }

        /// <summary>
        /// Before startup, check if console should be enabled and initialize logger and cleanup of old log files
        /// <para>Afterwards, continue with startup</para>
        /// </summary>
        /// <param name="e"></param>
        protected override void OnStartup(StartupEventArgs e)
        {
            // Check if "-console" argument is present
            if (e.Args.Contains("-console"))
            {
                // Show Console if the argument is present
                Logger.AllocConsole();
            }
            Logger.InitializeLogger(); // Initialize Logger
            Logger.Cleanup(); // Check if there are any log files that should be deleted (Older than 7 days)
            CheckIfFoldersExist();
            ConfigurationManager.LoadConfigurationFile(); // Loading configuration file
            // Check if configuration file is "null" and if it is, initialize new configuration file
            if (ConfigurationManager.AppConfig == null)
            {
                ConfigurationManager.InitializeNewConfiguration();
                ConfigurationManager.SaveConfigurationFile();
            }
            GameManager.Load(); // Loads installed games
            CheckTools(); // Check if all necessary tools are installed
            CheckForXeniaUpdates();
            LoadTheme(); // Loading theme
            CheckLaunchArguments(e.Args); // Checking for launching games via launch arguments

            // Continue doing base startup function
            base.OnStartup(e);
        }

        /// <summary>
        /// While the application is starting up this executes
        /// </summary>
        private void Application_Startup(object sender, StartupEventArgs e)
        {

        }
    }
}