using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Media;

// Imported
using Serilog;

namespace XeniaManager.DesktopApp.CustomControls
{
    public partial class GameButton
    {
        /// <summary>
        /// Checks if the game boxart is cached
        /// <para>If the game boxart is not cached, it'll cache it</para>
        /// </summary>
        /// <param name="game">Game</param>
        /// <returns >BitmapImage - cached game boxart</returns>
        private BitmapImage LoadOrCacheBoxart(Game game)
        {
            string boxartFilePath =
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, game.Artwork.Boxart); // Path to the game boxart
            string cacheDirectory =
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Cache\"); // Path to the cached directory

            // Tries to find cached boxart
            game.ArtworkCache.Boxart = GameManager.Caching.FindFirstIdenticalFile(boxartFilePath, cacheDirectory);
            if (game.ArtworkCache.Boxart != null)
            {
                // If there is a cached boxart, return it
                Log.Information("Boxart has already been cached");
                return new BitmapImage(new Uri(game.ArtworkCache.Boxart));
            }

            // If there's no cached boxart, create a cached version and return it
            Log.Information("Creating new cached boxart for the game");
            string randomImageName = Path.GetRandomFileName().Replace(".", "").Substring(0, 8) + ".ico";
            game.ArtworkCache.Boxart = Path.Combine(cacheDirectory, randomImageName);

            File.Copy(boxartFilePath, game.ArtworkCache.Boxart, true);
            Log.Information($"Cached icon name: {randomImageName}");

            return new BitmapImage(new Uri(game.ArtworkCache.Boxart));
        }

        /// <summary>
        /// Creates a 20x20 image that shows the compatibility rating for the specific game
        /// </summary>
        /// <param name="game">Game itself</param>
        /// <returns>Image of the compatibility rating</returns>
        private Border CompatibilityRatingIcon(Game game)
        {
            // Border that contains the compatibility rating icon
            Border compatibilityRatingElement = new Border
            {
                Width = 22, // Width of the emoji
                Height = 22, // Height of the emoji
                Background = Brushes.White,
                BorderBrush = Brushes.Black,
                BorderThickness = new Thickness(1),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(1, 1, 0, 0),
                CornerRadius = new CornerRadius(16)
            };

            // Image of the compatibility rating
            Image compatibilityRatingIcon = new Image
            {
                Width = 20, // Width of the emoji
                Height = 20, // Height of the emoji
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            // Use appropriate compatibility rating icon based on the compatibility rating
            switch (game.CompatibilityRating)
            {
                case CompatibilityRating.Unplayable:
                    compatibilityRatingIcon.Source = new BitmapImage(new Uri(
                        "pack://application:,,,/XeniaManager.DesktopApp;component/Assets/Compatibility Icons/Unplayable.png"));
                    break;
                case CompatibilityRating.Loads:
                    compatibilityRatingIcon.Source = new BitmapImage(new Uri(
                        "pack://application:,,,/XeniaManager.DesktopApp;component/Assets/Compatibility Icons/Loads.png"));
                    break;
                case CompatibilityRating.Gameplay:
                    compatibilityRatingIcon.Source = new BitmapImage(new Uri(
                        "pack://application:,,,/XeniaManager.DesktopApp;component/Assets/Compatibility Icons/Gameplay.png"));
                    break;
                case CompatibilityRating.Playable:
                    compatibilityRatingIcon.Source = new BitmapImage(new Uri(
                        "pack://application:,,,/XeniaManager.DesktopApp;component/Assets/Compatibility Icons/Playable.png"));
                    break;
                default:
                    compatibilityRatingIcon.Source = new BitmapImage(new Uri(
                        "pack://application:,,,/XeniaManager.DesktopApp;component/Assets/Compatibility Icons/Unknown.png"));
                    break;
            }

            // Add the image to the main element
            compatibilityRatingElement.Child = compatibilityRatingIcon;

            return compatibilityRatingElement; // Return the main element aka border
        }

        /// <summary>
        /// Creates image for the game button
        /// </summary>
        /// <param name="game">Game itself</param>
        /// <returns>Border - Content of the game button</returns>
        private Border CreateButtonContent(Game game)
        {
            // Get the cached boxart
            BitmapImage boxart = LoadOrCacheBoxart(game);
            Image gameImage = new Image
            {
                Source = boxart,
                Stretch = Stretch.UniformToFill
            };

            // Create a Grid to hold the game image and any overlays
            Grid contentGrid = new Grid();
            // Add the image as the base layer
            contentGrid.Children.Add(gameImage);

            // Optionally add the compatibility rating overlay (if enabled)
            if (ConfigurationManager.AppConfig.CompatibilityIcons == true)
            {
                contentGrid.Children.Add(CompatibilityRatingIcon(game));
            }

            if (ConfigurationManager.AppConfig.DisplayGameTitle)
            {
                // Create an overlay for text at the bottom of the image
                Border textOverlay = new Border
                {
                    // Use a semi-transparent background to ensure readability
                    Background = new SolidColorBrush(Color.FromArgb(200, 0, 0, 0)),
                    CornerRadius = new CornerRadius(0,0,2,2),
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Bottom,
                    Padding = new Thickness(3) // Adjust padding as needed
                };

                // The text to display on the overlay
                TextBlock overlayText = new TextBlock
                {
                    Text = game.Title, // Replace with your dynamic text if needed
                    Foreground = Brushes.White,
                    FontSize = 12, // Adjust font size as needed
                    FontWeight = FontWeights.Bold,
                    FontFamily = (FontFamily)Application.Current.Resources["SegoeFluent"],
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    TextAlignment = TextAlignment.Center,
                    TextWrapping = TextWrapping.Wrap
                };

                // Place the text inside the overlay border
                textOverlay.Child = overlayText;
                // Add the overlay to the grid; later children render on top of earlier ones.
                contentGrid.Children.Add(textOverlay);
            }

            // Define the clipping geometry for rounded corners
            RectangleGeometry clipGeometry = new RectangleGeometry
            {
                Rect = new Rect(0, 0, 150, 207),
                RadiusX = 3,
                RadiusY = 3
            };

            // Return the final button content wrapped in a Border with rounded edges
            return new Border
            {
                CornerRadius = new CornerRadius(10),
                Background = Brushes.Black,
                Child = contentGrid,
                Clip = clipGeometry
            };
        }
    }
}