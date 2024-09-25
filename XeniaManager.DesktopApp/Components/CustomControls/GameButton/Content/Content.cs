using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Media;

// Imported
using Serilog;

namespace XeniaManager.DesktopApp.CustomControls
{
    public partial class GameButton : Button
    {
        /// <summary>
        /// Checks if the game boxart is cached
        /// <para>If the game boxart is not cached, it'll cache it</para>
        /// </summary>
        /// <param name="game">Game</param>
        /// <returns >BitmapImage - cached game boxart</returns>
        private BitmapImage LoadOrCacheBoxart(Game game)
        {
            string boxartFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, game.Artwork.Boxart); // Path to the game boxart
            string cacheDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Cache\"); // Path to the cached directory

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
        /// Creates image for the game button
        /// </summary>
        /// <param name="game">Game itself</param>
        /// <returns>Border - Content of the game button</returns>
        private Border CreateButtonContent(Game game)
        {
            // Cached game boxart
            BitmapImage boxart = LoadOrCacheBoxart(game);
            Image gameImage = new Image
            {
                Source = boxart,
                Stretch = Stretch.UniformToFill
            };

            // Create a Grid to hold both the game image and the overlay symbol
            Grid contentGrid = new Grid();

            // Add the game image to the grid
            contentGrid.Children.Add(gameImage);

            // Compatibility Rating
            Border CompatibilityRatingImage = new Border
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

            Image CompatibilityRating = new Image
            {
                Width = 20, // Width of the emoji
                Height = 20, // Height of the emoji
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            /*
            switch (game.CompatibilityRating)
            {
                case "Unplayable":
                    CompatibilityRating.Source = new BitmapImage(new Uri("pack://application:,,,/Xenia Manager;component/Assets/Compatibility Icons/Unplayable.png"));
                    break;
                case "Loads":
                    CompatibilityRating.Source = new BitmapImage(new Uri("pack://application:,,,/Xenia Manager;component/Assets/Compatibility Icons/Loads.png"));
                    break;
                case "Gameplay":
                    CompatibilityRating.Source = new BitmapImage(new Uri("pack://application:,,,/Xenia Manager;component/Assets/Compatibility Icons/Gameplay.png"));
                    break;
                case "Playable":
                    CompatibilityRating.Source = new BitmapImage(new Uri("pack://application:,,,/Xenia Manager;component/Assets/Compatibility Icons/Playable.png"));
                    break;
                default:
                    CompatibilityRating.Source = new BitmapImage(new Uri("pack://application:,,,/Xenia Manager;component/Assets/Compatibility Icons/Unknown.png"));
                    break;
            }
            
            // Add the compatibility rating to the grid
            CompatibilityRatingImage.Child = CompatibilityRating;
            contentGrid.Children.Add(CompatibilityRatingImage);*/

            // Rounded edges of the game boxart
            RectangleGeometry clipGeometry = new RectangleGeometry
            {
                Rect = new Rect(0, 0, 150, 207),
                RadiusX = 3,
                RadiusY = 3
            };

            // Game button content with rounded corners
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
