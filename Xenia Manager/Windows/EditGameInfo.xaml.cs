using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;

// Imported
using ImageMagick;
using Microsoft.Win32;
using Serilog;
using Xenia_Manager.Classes;

namespace Xenia_Manager.Windows
{
    /// <summary>
    /// Interaction logic for EditGameInfo.xaml
    /// </summary>
    public partial class EditGameInfo : Window
    {
        // Selected game
        private InstalledGame game = new InstalledGame();

        // Used to send a signal that this window has been closed
        private TaskCompletionSource<bool> _closeTaskCompletionSource = new TaskCompletionSource<bool>();

        public EditGameInfo(InstalledGame game)
        {
            InitializeComponent();
            this.game = game;
            InitializeAsync();
            Closed += (sender, args) => _closeTaskCompletionSource.TrySetResult(true);
        }

        /// <summary>
        /// Used for dragging the window around
        /// </summary>
        private void Window_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        /// <summary>
        /// Creates image for the button
        /// </summary>
        /// <param name="game">Game itself</param>
        /// <returns>Border - Content of the button</returns>
        private async Task<Border> CreateButtonContent()
        {
            await Task.Delay(1);
            // Cached game icon
            BitmapImage iconImage = new BitmapImage(new Uri(game.CachedIconPath));
            Image image = new Image
            {
                Source = iconImage,
                Stretch = Stretch.UniformToFill
            };

            // Rounded edges of the game icon
            RectangleGeometry clipGeometry = new RectangleGeometry
            {
                Rect = new Rect(0, 0, 150, 207),
                RadiusX = 3,
                RadiusY = 3
            };

            // Game button content
            return new Border
            {
                CornerRadius = new CornerRadius(10),
                Background = Brushes.Black,
                Child = image,
                Clip = clipGeometry
            };
        }

        /// <summary>
        /// Loads content into the UI
        /// </summary>
        private async Task LoadContentIntoUI()
        {
            try
            {
                GameID.Text = game.GameId;
                GameTitle.Text = game.Title;
                GameIcon.Content = await CreateButtonContent();
                await Task.Delay(1);
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message + "\nFull Error:\n" + ex);
                MessageBox.Show(ex.Message);
            }
        }

        /// <summary>
        /// Checks what version of Xenia the game uses and adjusts the UI according to it
        /// </summary>
        private async Task CheckXeniaVersion()
        {
            try
            {
                // Check if Xenia Stable is installed
                if (App.appConfiguration.XeniaStable != null && Directory.Exists(App.appConfiguration.XeniaStable.EmulatorLocation))
                {
                    SwitchToXeniaStableOption.Visibility = Visibility.Visible;
                }
                else
                {
                    SwitchToXeniaStableOption.Visibility = Visibility.Collapsed;
                }

                // Check if Xenia Canary is installed
                if (App.appConfiguration.XeniaCanary != null && Directory.Exists(App.appConfiguration.XeniaCanary.EmulatorLocation))
                {
                    SwitchToXeniaCanaryOption.Visibility = Visibility.Visible;
                }
                else
                {
                    SwitchToXeniaCanaryOption.Visibility = Visibility.Collapsed;
                }

                // Check if Xenia Netplay is installed
                if (App.appConfiguration.XeniaNetplay != null && Directory.Exists(App.appConfiguration.XeniaNetplay.EmulatorLocation))
                {
                    SwitchToXeniaNetplayOption.Visibility = Visibility.Visible;
                }
                else
                {
                    SwitchToXeniaNetplayOption.Visibility = Visibility.Collapsed;
                }

                // Check what version Xenia uses
                switch (game.EmulatorVersion)
                {
                    case "Stable":
                        SwitchToXeniaStableOption.Visibility = Visibility.Collapsed;
                        break;
                    case "Canary":
                        SwitchToXeniaCanaryOption.Visibility = Visibility.Collapsed;
                        break;
                    case "Netplay":
                        SwitchToXeniaNetplayOption.Visibility = Visibility.Collapsed;
                        break;
                    default:
                        break;
                }
                await Task.Delay(1);
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message + "\nFull Error:\n" + ex);
                MessageBox.Show(ex.Message);
            }
        }

        /// <summary>
        /// Function that executes other functions asynchronously
        /// </summary>
        private async void InitializeAsync()
        {
            try
            {
                await Dispatcher.InvokeAsync(() =>
                {
                    this.Visibility = Visibility.Hidden;
                    Mouse.OverrideCursor = Cursors.Wait;
                });
                await LoadContentIntoUI();
                await CheckXeniaVersion();
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message + "\nFull Error:\n" + ex);
                MessageBox.Show(ex.Message);
            }
            finally
            {
                await Dispatcher.InvokeAsync(() =>
                {
                    this.Visibility = Visibility.Visible;
                    Mouse.OverrideCursor = null;
                });
            }
        }

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
        /// Used to emulate a WaitForCloseAsync function that is similar to the one Process Class has
        /// </summary>
        /// <returns></returns>
        public Task WaitForCloseAsync()
        {
            return _closeTaskCompletionSource.Task;
        }

        /// <summary>
        /// Does fade out animation before closing the window
        /// </summary>
        private async Task ClosingAnimation()
        {
            Storyboard FadeOutClosingAnimation = ((Storyboard)Application.Current.FindResource("FadeOutAnimation")).Clone();

            FadeOutClosingAnimation.Completed += (sender, e) =>
            {
                Log.Information("Closing EditGameInfo window");
                this.Close();
            };

            FadeOutClosingAnimation.Begin(this);
            await Task.Delay(1);
        }

        // UI
        // Buttons
        /// <summary>
        /// Function that grabs the game box art from the PC and converts it to .ico
        /// </summary>
        /// <param name="filePath">Where the file is</param>
        /// <param name="outputPath">Where the file will be stored after conversion</param>
        /// <param name="width">Width of the box art. Default is 150</param>
        /// <param name="height">Height of the box art. Default is 207</param>
        /// <returns></returns>
        private async Task GetGameBoxArtFromFile(string filePath, string outputPath, int width = 150, int height = 207)
        {
            try
            {
                // Checking what format the loaded icon is
                MagickFormat format = Path.GetExtension(filePath).ToLower() switch
                {
                    ".jpg" or ".jpeg" => MagickFormat.Jpeg,
                    ".png" => MagickFormat.Png,
                    ".ico" => MagickFormat.Ico,
                    _ => throw new NotSupportedException($"Unsupported file extension: {Path.GetExtension(filePath)}")
                };
                Log.Information($"Selected file format: {format}");

                // Converting it to the proper size
                using (FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    using (MagickImage magickImage = new MagickImage(fileStream, format))
                    {
                        // Resize the image to the specified dimensions (this will stretch the image)
                        magickImage.Resize(width, height);

                        // Convert to ICO format
                        magickImage.Format = MagickFormat.Ico;
                        magickImage.Write(outputPath);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message + "\nFull Error:\n" + ex);
                MessageBox.Show(ex.Message);
            }
        }

        /// <summary>
        /// Checks if the game icon is cached
        /// <para>If the game icon is not cached, it'll cache it</para>
        /// </summary>
        /// <param name="game">Game</param>
        /// <returns >BitmapImage - cached game icon</returns>
        public async Task CacheIcon()
        {
            await Task.Delay(1);
            string iconFilePath = Path.Combine(App.baseDirectory, game.BoxartFilePath); // Path to the game icon
            string cacheDirectory = Path.Combine(App.baseDirectory, @"Icons\Cache\"); // Path to the cached directory

            Log.Information("Creating new cached icon for the game");
            string randomIconName = Path.GetRandomFileName().Replace(".", "").Substring(0, 8) + ".ico";
            game.CachedIconPath = Path.Combine(cacheDirectory, randomIconName);

            File.Copy(iconFilePath, game.CachedIconPath, true);
            Log.Information($"Cached icon name: {randomIconName}");
        }

        /// <summary>
        /// Opens the file dialog and waits for user to select a new icon for the game
        /// <para>Afterwards it'll apply the new icon to the game</para>
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void GameIcon_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Create OpenFileDialog
                OpenFileDialog openFileDialog = new OpenFileDialog();

                // Set filter for image files
                openFileDialog.Filter = "Image Files|*.jpg;*.jpeg;*.png;*.ico|All Files|*.*";
                openFileDialog.Title = $"Select a new icon for {game.Title}";

                // Allow the user to only select 1 file
                openFileDialog.Multiselect = false;

                // Show the dialog and get result
                bool? result = openFileDialog.ShowDialog();

                // Process open file dialog results
                if (result == true)
                {
                    Log.Information($"Selected file: {Path.GetFileName(openFileDialog.FileName)}");
                    if (game.Title == GameTitle.Text)
                    {
                        await GetGameBoxArtFromFile(openFileDialog.FileName, Path.Combine(App.baseDirectory, @$"Icons\{game.Title}.ico"));
                    }
                    else
                    {
                        await GetGameBoxArtFromFile(openFileDialog.FileName, Path.Combine(App.baseDirectory, @$"Icons\{game.Title}.ico"));
                        AdjustGameTitle();
                    }
                    Log.Information("New icon is added to Icons folder");

                    Log.Information("Changing icon showed on the button to the new one");
                    await CacheIcon();
                    GameIcon.Content = await CreateButtonContent();
                }
                await Task.Delay(1);
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message + "\nFull Error:\n" + ex);
                MessageBox.Show(ex.Message);
            }
        }

        /// <summary>
        /// Removes unsupported characters from the game title
        /// </summary>
        /// <param name="input">Game title</param>
        /// <returns>Sanitized game title</returns>
        private string RemoveUnsupportedCharacters(string input)
        {
            // Define the set of invalid characters
            char[] invalidChars = Path.GetInvalidFileNameChars();

            // Remove invalid characters
            string sanitized = new string(input.Where(ch => !invalidChars.Contains(ch)).ToArray());

            // Condense multiple spaces into a single space
            return Regex.Replace(sanitized, @"\s+", " ").Trim();
        }

        /// <summary>
        /// This is used to adjust the game
        /// </summary>
        private void AdjustGameTitle()
        {
            try
            {
                if (game.ConfigFilePath.Contains(game.Title))
                {
                    Log.Information("Renaming the configuration file to fit the new title");
                    // Rename the configuration file to fit the new title
                    File.Move(Path.Combine(App.baseDirectory, game.ConfigFilePath), Path.Combine(App.baseDirectory, Path.GetDirectoryName(game.ConfigFilePath), $"{RemoveUnsupportedCharacters(GameTitle.Text)}.config.toml"), true);

                    // Construct the new full path with the new file name
                    game.ConfigFilePath = Path.Combine(game.ConfigFilePath.Substring(0, game.ConfigFilePath.LastIndexOf('\\') + 1), $"{RemoveUnsupportedCharacters(GameTitle.Text)}.config.toml");
                }
                Log.Information("Changing the game title in the library");
                game.Title = RemoveUnsupportedCharacters(GameTitle.Text);

                Log.Information("Changing the name of icon");
                File.Move(Path.Combine(App.baseDirectory, game.BoxartFilePath), Path.Combine(App.baseDirectory, $"Icons\\{game.Title}.ico"), true);
                game.BoxartFilePath = $"Icons\\{game.Title}.ico";
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message + "\nFull Error:\n" + ex);
                MessageBox.Show(ex.Message);
            }
        }

        /// <summary>
        /// Closes this window
        /// </summary>
        private async void Exit_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Check if there was a change to game title
                if (game.Title != GameTitle.Text)
                {
                    Log.Information("There is a change in game title");
                    AdjustGameTitle();
                }
                await ClosingAnimation();
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message + "\nFull Error:\n" + ex);
                MessageBox.Show(ex.Message);
            }
        }

        /// <summary>
        /// Function to handle the game transfer between emulators
        /// </summary>
        /// <param name="game">Game to tranasfer</param>
        /// <param name="SourceVersion">Original Xenia version that the game uses</param>
        /// <param name="TargetVersion">New Xenia version that the game will use</param>
        /// <param name="sourceEmulatorLocation">Original Xenia version location</param>
        /// <param name="targetEmulatorLocation">New Xenia version location</param>
        /// <param name="defaultConfigFileLocation">Location to the default configuration file of the new Xenia version</param>
        private async Task TransferGame(InstalledGame game, string SourceVersion, string TargetVersion, string sourceEmulatorLocation, string targetEmulatorLocation, string defaultConfigFileLocation)
        {
            if (SourceVersion == "Custom")
            {
                game.EmulatorExecutableLocation = null;
            }
            Log.Information($"Moving the game to Xenia {TargetVersion}");
            game.EmulatorVersion = TargetVersion; // Set the emulator version

            game.ConfigFilePath = @$"{targetEmulatorLocation}config\{game.Title}.config.toml";
            if (!File.Exists(Path.Combine(App.baseDirectory, game.ConfigFilePath)))
            {
                Log.Information("Game configuration file not found");
                Log.Information("Creating a new configuration file from the default one");
                File.Copy(Path.Combine(App.baseDirectory, defaultConfigFileLocation), Path.Combine(App.baseDirectory, targetEmulatorLocation, $@"config\{game.Title}.config.toml"), true);
            }

            // Checking if patch file exists and should be moved
            if (game.PatchFilePath != null)
            {
                if ((SourceVersion == "Canary" || SourceVersion == "Netplay") && (TargetVersion == "Canary" || TargetVersion == "Netplay"))
                {
                    string destination = TargetVersion switch
                    {
                        "Canary" => App.appConfiguration.XeniaCanary.EmulatorLocation,
                        "Netplay" => App.appConfiguration.XeniaNetplay.EmulatorLocation,
                        _ => throw new InvalidOperationException("Unexpected build type")
                    };

                    // Check if the patches folder exists, if it doesn't, create it
                    if (!Directory.Exists(Path.Combine(App.baseDirectory, destination, @$"patches")))
                    {
                        Directory.CreateDirectory(Path.Combine(App.baseDirectory, destination, @$"patches"));
                    }
                    // Moving patch file
                    File.Move(Path.Combine(App.baseDirectory, game.PatchFilePath), Path.Combine(App.baseDirectory, destination, @$"patches\{Path.GetFileName(game.PatchFilePath)}"));

                    game.PatchFilePath = Path.Combine(destination, @$"patches\{Path.GetFileName(game.PatchFilePath)}");
                }
            }

            // Checking if there is some content installed that should be copied over
            if (Directory.Exists(Path.Combine(App.baseDirectory, @$"{sourceEmulatorLocation}content\{game.GameId}")))
            {
                Log.Information($"Copying all of the installed content and saves from Xenia {SourceVersion} to Xenia {TargetVersion}");
                // Create all of the necessary directories for content copy
                foreach (string dirPath in Directory.GetDirectories(Path.Combine(App.baseDirectory, @$"{sourceEmulatorLocation}content\{game.GameId}"), "*", SearchOption.AllDirectories))
                {
                    Directory.CreateDirectory(dirPath.Replace(Path.Combine(App.baseDirectory, @$"{sourceEmulatorLocation}content\{game.GameId}"), Path.Combine(App.baseDirectory, @$"{targetEmulatorLocation}content\{game.GameId}")));
                }

                // Copy all the files
                foreach (string newPath in Directory.GetFiles(Path.Combine(App.baseDirectory, @$"{sourceEmulatorLocation}content\{game.GameId}"), "*.*", SearchOption.AllDirectories))
                {
                    File.Copy(newPath, newPath.Replace(Path.Combine(App.baseDirectory, @$"{sourceEmulatorLocation}content\{game.GameId}"), Path.Combine(App.baseDirectory, $@"{targetEmulatorLocation}content\{game.GameId}")), true);
                }
            }
            else
            {
                Log.Information("No installed content or saves found");
            }

            Log.Information("Reloading the UI and saving changes");
            await Task.Delay(1);
        }

        /// <summary>
        /// Moves the game to another location
        /// </summary>
        private async void MoveGame_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await Task.Delay(1);

                if (game.Title != GameTitle.Text)
                {
                    Log.Information("There is a change in game title");
                    AdjustGameTitle();
                }

                // Open file dialog
                Log.Information("Open file dialog");
                OpenFileDialog openFileDialog = new OpenFileDialog();
                openFileDialog.Title = "Select a game";
                openFileDialog.Filter = "All Files|*|Supported Files|*.iso;*.xex;*.zar";
                openFileDialog.Multiselect = true;
                bool? result = openFileDialog.ShowDialog();
                if (result == true)
                {
                    game.GameFilePath = openFileDialog.FileName;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message + "\nFull Error:\n" + ex);
                MessageBox.Show(ex.Message);
            }
        }

        /// <summary>
        /// Makes the game use Xenia Canary
        /// </summary>
        private async void SwitchXeniaCanary_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (game.Title != GameTitle.Text)
                {
                    Log.Information("There is a change in game title");
                    AdjustGameTitle();
                }
                string sourceEmulatorLocation = game.EmulatorVersion switch
                {
                    "Stable" => App.appConfiguration.XeniaStable.EmulatorLocation,
                    "Netplay" => App.appConfiguration.XeniaNetplay.EmulatorLocation,
                    "Custom" => "",
                    _ => throw new InvalidOperationException("Unexpected build type")
                };
                await TransferGame(game, game.EmulatorVersion, "Canary", sourceEmulatorLocation, App.appConfiguration.XeniaCanary.EmulatorLocation, App.appConfiguration.XeniaCanary.ConfigurationFileLocation);
                await CheckXeniaVersion();
                MessageBox.Show($"{game.Title} transfer is complete. Now the game will use Xenia {game.EmulatorVersion}.");
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message + "\nFull Error:\n" + ex);
                MessageBox.Show(ex.Message);
            }
        }

        /// <summary>
        /// Makes the game use Xenia Stable
        /// </summary>
        private async void SwitchXeniaStable_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (game.Title != GameTitle.Text)
                {
                    Log.Information("There is a change in game title");
                    AdjustGameTitle();
                }
                string sourceEmulatorLocation = game.EmulatorVersion switch
                {
                    "Canary" => App.appConfiguration.XeniaCanary.EmulatorLocation,
                    "Netplay" => App.appConfiguration.XeniaNetplay.EmulatorLocation,
                    "Custom" => "",
                    _ => throw new InvalidOperationException("Unexpected build type")
                };
                await TransferGame(game, game.EmulatorVersion, "Stable", sourceEmulatorLocation, App.appConfiguration.XeniaStable.EmulatorLocation, App.appConfiguration.XeniaStable.ConfigurationFileLocation);
                await CheckXeniaVersion();
                MessageBox.Show($"{game.Title} transfer is complete. Now the game will use Xenia {game.EmulatorVersion}.");
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message + "\nFull Error:\n" + ex);
                MessageBox.Show(ex.Message);
            }
        }

        /// <summary>
        /// Makes the game use Xenia Netplay
        /// </summary>
        private async void SwitchXeniaNetplay_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (game.Title != GameTitle.Text)
                {
                    Log.Information("There is a change in game title");
                    AdjustGameTitle();
                }
                string sourceEmulatorLocation = game.EmulatorVersion switch
                {
                    "Canary" => App.appConfiguration.XeniaCanary.EmulatorLocation,
                    "Stable" => App.appConfiguration.XeniaStable.EmulatorLocation,
                    "Custom" => "",
                    _ => throw new InvalidOperationException("Unexpected build type")
                };
                await TransferGame(game, game.EmulatorVersion, "Netplay", sourceEmulatorLocation, App.appConfiguration.XeniaNetplay.EmulatorLocation, App.appConfiguration.XeniaNetplay.ConfigurationFileLocation);
                await CheckXeniaVersion();
                MessageBox.Show($"{game.Title} transfer is complete. Now the game will use Xenia {game.EmulatorVersion}.");
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message + "\nFull Error:\n" + ex);
                MessageBox.Show(ex.Message);
            }
        }

        /// <summary>
        /// Makes the game use custom version of Xenia
        /// </summary>
        private async void SwitchXeniaCustom_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (game.Title != GameTitle.Text)
                {
                    Log.Information("There is a change in game title");
                    AdjustGameTitle();
                }

                // OpenFileDialog to select custom Xenia executable
                OpenFileDialog CustomXeniaExecutableSelector = new OpenFileDialog();
                CustomXeniaExecutableSelector.Title = "Select Xenia executable";
                CustomXeniaExecutableSelector.Filter = "Supported Files|*.exe";
                bool? CustomXeniaExecutableSelectorResult = CustomXeniaExecutableSelector.ShowDialog();
                if (CustomXeniaExecutableSelectorResult == true)
                {
                    Log.Information($"Selected Xenia executable: {CustomXeniaExecutableSelector.FileName}");
                    game.EmulatorVersion = "Custom";
                    game.EmulatorExecutableLocation = CustomXeniaExecutableSelector.FileName;

                    // Trying to find the appropriate configuration file next to the emulator executable
                    Log.Information("Trying to find the configuration file");
                    if (File.Exists(Path.Combine(Path.GetDirectoryName(CustomXeniaExecutableSelector.FileName), $"{Path.GetFileNameWithoutExtension(CustomXeniaExecutableSelector.FileName).Replace('_','-')}.config.toml")))
                    {
                        Log.Information($"Found configuration file: {Path.Combine(Path.GetDirectoryName(CustomXeniaExecutableSelector.FileName), $"{Path.GetFileNameWithoutExtension(CustomXeniaExecutableSelector.FileName).Replace('_', '-')}.config.toml")}");
                        game.ConfigFilePath = Path.Combine(Path.GetDirectoryName(CustomXeniaExecutableSelector.FileName), $"{Path.GetFileNameWithoutExtension(CustomXeniaExecutableSelector.FileName).Replace('_', '-')}.config.toml");
                    }
                    else
                    {
                        // Incase it can't find it, ask user to find it himself
                        Log.Information($"Not able to find a configuration file");
                        OpenFileDialog CustomXeniaConfigurationSelector = new OpenFileDialog();
                        CustomXeniaConfigurationSelector.Title = "Select Xenia configuration file";
                        CustomXeniaConfigurationSelector.Filter = "Supported Files|*.toml";
                        bool? CustomXeniaConfigurationSelectorResult = CustomXeniaConfigurationSelector.ShowDialog();
                        if (CustomXeniaConfigurationSelectorResult == true)
                        {
                            Log.Information($"Selected configuration file: {CustomXeniaConfigurationSelector.FileName}");
                            game.ConfigFilePath = CustomXeniaConfigurationSelector.FileName;
                        }
                        else
                        {
                            game.ConfigFilePath = null;
                        }
                    }
                }
                //await TransferGame(game, game.EmulatorVersion, "Custom", sourceEmulatorLocation, App.appConfiguration.XeniaNetplay.EmulatorLocation, App.appConfiguration.XeniaNetplay.ConfigurationFileLocation);
                await CheckXeniaVersion();
                MessageBox.Show($"{game.Title} transfer is complete. Now the game will use Xenia {game.EmulatorVersion}.");
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message + "\nFull Error:\n" + ex);
                MessageBox.Show(ex.Message);
            }
        }
    }
}
