using System;
using System.IO;
using System.Net.Http;
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
using XeniaManager.DesktopApp.Utilities.Animations;

namespace XeniaManager.DesktopApp.Windows
{
    /// <summary>
    /// Interaction logic for GameDetails.xaml
    /// </summary>
    public partial class GameDetails : Window
    {
        // Global variables
        // Selected game
        private Game game { get; set; }
        // Used to send a signal that this window has been closed
        private TaskCompletionSource<bool> closeTaskCompletionSource = new TaskCompletionSource<bool>();

        public GameDetails(Game game)
        {
            InitializeComponent();
            this.game = game;
            InitializeAsync();
            Closed += (sender, args) => closeTaskCompletionSource.TrySetResult(true);
        }

        // Functions
        /// <summary>
        /// Used to emulate a WaitForCloseAsync function that is similar to the one Process Class has
        /// </summary>
        public Task WaitForCloseAsync()
        {
            return closeTaskCompletionSource.Task;
        }

        /// <summary>
        /// Checks if the game icon is cached
        /// <para>If the game icon is not cached, it'll cache it</para>
        /// </summary>
        /// <param name="imagePath">Path to the image that needs caching</param>
        /// <returns >BitmapImage - cached game icon</returns>
        public async Task CacheImage(string imagePath, string image)
        {
            await Task.Delay(1);
            string iconFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, imagePath); // Path to the game icon
            string cacheDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Cache\"); // Path to the cached directory

            Log.Information("Creating new cached image for the game");
            string randomIconName = Path.GetRandomFileName().Replace(".", "").Substring(0, 8) + ".ico";
            switch (image)
            {
                case "boxart":
                    game.ArtworkCache.Boxart = Path.Combine(cacheDirectory, randomIconName);
                    File.Copy(iconFilePath, game.ArtworkCache.Boxart, true);
                    break;
                case "icon":
                    game.ArtworkCache.Icon = Path.Combine(cacheDirectory, randomIconName);
                    File.Copy(iconFilePath, game.ArtworkCache.Icon, true);
                    break;
                default:
                    break;
            }
            Log.Information($"Cached image name: {randomIconName}");
        }

        /// <summary>
        /// Creates image for the button
        /// </summary>
        /// <param name="imagePath">Path to the image that will be shown</param>
        /// <returns>Border - Content of the button</returns>
        private async Task<Border> CreateButtonContent(string imagePath, uint width = 150, uint height = 207)
        {
            // Cached game icon
            BitmapImage iconImage = new BitmapImage();

            // Load local file synchronously
            iconImage.BeginInit();
            iconImage.UriSource = new Uri(imagePath);
            iconImage.EndInit();
            iconImage.Freeze(); // Freeze for cross-thread operations

            // Create Image control with loaded BitmapImage
            Image image = new Image
            {
                Source = iconImage,
                Stretch = Stretch.UniformToFill
            };

            // Rounded edges of the game icon
            RectangleGeometry clipGeometry = new RectangleGeometry
            {
                Rect = new Rect(0, 0, width, height),
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
        /// Loads all of the images and text into the UI
        /// </summary>
        private async Task LoadContentIntoUI()
        {
            // Load game info into the UI
            TitleID.Text = game.GameId;
            if (game.MediaId != null)
            {
                MediaID.Text = game.MediaId;
            }
            else
            {
                MediaID.Text = "N/A";
            }
            GameTitle.Text = game.Title;
            // Load boxart
            // Check if it's cached and if it's not cache it
            if (game.ArtworkCache.Boxart == null || !File.Exists(game.ArtworkCache.Boxart))
            {
                await CacheImage(game.Artwork.Boxart, "boxart");
            }
            // Create Boxart button content from the cached boxart image
            GameBoxart.Content = await CreateButtonContent(game.ArtworkCache.Boxart);
            // Load icon
            // Check if it's cached and if it's not cache it
            if (game.ArtworkCache.Icon == null || !File.Exists(game.ArtworkCache.Icon))
            {
                await CacheImage(game.Artwork.Icon, "icon");
            }
            // Create Icon button content from the cached boxart image
            GameIcon.Content = await CreateButtonContent(game.ArtworkCache.Icon, 64, 64);
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
        /// Function that grabs the game box art from the PC and converts it to .ico
        /// </summary>
        /// <param name="filePath">Where the file is</param>
        /// <param name="outputPath">Where the file will be stored after conversion</param>
        /// <param name="width">Width of the box art. Default is 150</param>
        /// <param name="height">Height of the box art. Default is 207</param>
        /// <param name="newFormat">In what format we want the final image. Default is .ico</param>
        private void GetIconFromFile(string filePath, string outputPath, uint width = 150, uint height = 207, MagickFormat newFormat = MagickFormat.Ico)
        {
            // Checking what format the loaded icon is
            MagickFormat currentFormat = Path.GetExtension(filePath).ToLower() switch
            {
                ".jpg" or ".jpeg" => MagickFormat.Jpeg,
                ".png" => MagickFormat.Png,
                ".ico" => MagickFormat.Ico,
                _ => throw new NotSupportedException($"Unsupported file extension: {Path.GetExtension(filePath)}")
            };
            Log.Information($"Selected file format: {currentFormat}");

            // Converting it to the proper size
            using (FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                using (MagickImage magickImage = new MagickImage(fileStream, currentFormat))
                {
                    // Resize the image to the specified dimensions (this will stretch the image)
                    magickImage.Resize(width, height);

                    // Convert to the correct format
                    magickImage.Format = currentFormat;
                    magickImage.Write(outputPath);
                }
            }
        }

        // UI Interactions
        // Window
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
        /// Closes this window
        /// </summary>
        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            WindowAnimations.ClosingAnimation(this);
        }

        /// <summary>
        /// Opens the file dialog and waits for user to select a new boxart for the game
        /// <para>Afterwards it'll apply the new boxart to the game</para>
        /// </summary>
        private async void GameBoxart_Click(object sender, RoutedEventArgs e)
        {
            // Create OpenFileDialog
            OpenFileDialog openFileDialog = new OpenFileDialog();

            // Set filter for image files
            openFileDialog.Filter = "Image Files|*.jpg;*.jpeg;*.png;*.ico";
            openFileDialog.Title = $"Select new boxart for {game.Title}";

            // Allow the user to only select 1 file
            openFileDialog.Multiselect = false;

            // Show the dialog and get result
            if (openFileDialog.ShowDialog() == false)
            {
                Log.Information("Boxart selection cancelled");
                return;
            }

            Log.Information($"Selected file: {Path.GetFileName(openFileDialog.FileName)}");
            if (game.Title != GameTitle.Text)
            {
                // Adjust game title before moving forwards
            }

            // Trying to convert the file to a proper format and move it into the right location
            try
            {
                GetIconFromFile(openFileDialog.FileName, Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @$"GameData\{game.Title}\Artwork\boxart.png"), 150, 207, MagickFormat.Png);
            }
            catch (NotSupportedException notsex)
            {
                Log.Error(notsex.Message);
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message + "\n" + ex);
            }
            Log.Information("New boxart is added");
            await CacheImage(game.Artwork.Boxart, "boxart");

            Log.Information("Changing boxart showed on the button to the new one");
            GameBoxart.Content = await CreateButtonContent(game.ArtworkCache.Boxart);
        }

        /// <summary>
        /// Opens the file dialog and waits for user to select a new icon for the game
        /// <para>Afterwards it'll apply the new icon to the game</para>
        /// </summary>
        private async void GameIcon_Click(object sender, RoutedEventArgs e)
        {
            // Create OpenFileDialog
            OpenFileDialog openFileDialog = new OpenFileDialog();

            // Set filter for image files
            openFileDialog.Filter = "Image Files|*.jpg;*.jpeg;*.png;*.ico";
            openFileDialog.Title = $"Select new icon for {game.Title}";

            // Allow the user to only select 1 file
            openFileDialog.Multiselect = false;

            // Show the dialog and get result
            if (openFileDialog.ShowDialog() == false)
            {
                Log.Information("Icon selection cancelled");
                return;
            }

            Log.Information($"Selected file: {Path.GetFileName(openFileDialog.FileName)}");
            if (game.Title != GameTitle.Text)
            {
                // Adjust game title before moving forwards
            }

            // Trying to convert the file to a proper format and move it into the right location
            try
            {
                GetIconFromFile(openFileDialog.FileName, Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @$"GameData\{game.Title}\Artwork\icon.ico"), 64, 64);
            }
            catch (NotSupportedException notsex)
            {
                Log.Error(notsex.Message);
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message + "\n" + ex);
            }
            Log.Information("New icon is added");
            await CacheImage(game.Artwork.Icon, "icon");

            Log.Information("Changing icon showed on the button to the new one");
            GameIcon.Content = await CreateButtonContent(game.ArtworkCache.Icon);
        }
    }
}
