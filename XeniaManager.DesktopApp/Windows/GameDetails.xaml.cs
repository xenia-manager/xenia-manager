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

            // Load image asynchronously if it's a web resource
            if (Uri.IsWellFormedUriString(imagePath, UriKind.Absolute))
            {
                using (HttpClient client = new HttpClient())
                {
                    var imageData = await client.GetByteArrayAsync(imagePath);
                    using (var stream = new MemoryStream(imageData))
                    {
                        stream.Position = 0;
                        iconImage.BeginInit();
                        iconImage.CacheOption = BitmapCacheOption.OnLoad;
                        iconImage.StreamSource = stream;
                        iconImage.EndInit();
                        iconImage.Freeze(); // Freeze for cross-thread operations
                    }
                }
            }
            else
            {
                // Load local file synchronously
                iconImage.BeginInit();
                iconImage.UriSource = new Uri(imagePath);
                iconImage.CacheOption = BitmapCacheOption.OnLoad;
                iconImage.EndInit();
                iconImage.Freeze(); // Freeze for cross-thread operations
            }

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
            if (game.ArtworkCache.Boxart == null)
            {
                await CacheImage(game.Artwork.Boxart, "boxart");
            }
            // Create Boxart button content from the cached boxart image
            GameBoxart.Content = await CreateButtonContent(game.ArtworkCache.Boxart);
            // Load icon
            // Check if it's cached and if it's not cache it
            if (game.ArtworkCache.Icon == null)
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
    }
}
