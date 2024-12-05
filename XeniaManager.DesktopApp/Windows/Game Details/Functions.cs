using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Media;

// Imported
using ImageMagick;
using Serilog;

namespace XeniaManager.DesktopApp.Windows
{
    /// <summary>
    /// Interaction logic for GameDetails.xaml
    /// </summary>
    public partial class GameDetails
    {
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
        /// <param name="image">What type of image it is (Boxart, Icon…)</param>
        /// <returns >BitmapImage - cached game icon</returns>
        private async Task CacheImage(string imagePath, string image)
        {
            await Task.Delay(1);
            string iconFilePath =
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, imagePath); // Path to the game icon
            string cacheDirectory =
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Cache\"); // Path to the cached directory

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
        /// <param name="width">Width of the image (Default = 150)</param>
        /// <param name="height">Height of the image (Default = 207)</param>
        /// <returns>Border - Content of the button</returns>
        private Border CreateButtonContent(string imagePath, uint width = 150, uint height = 207)
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
        /// Loads all the images and text into the UI
        /// </summary>
        private async Task LoadContentIntoUi()
        {
            // Load game info into the UI
            TxtTitleId.Text = game.GameId;
            TxtMediaId.Text = game.MediaId ?? "N/A";
            TxtGameTitle.Text = game.Title;
            // Load boxart
            // Check if it's cached and if it's not cache it
            if (game.ArtworkCache.Boxart == null || !File.Exists(game.ArtworkCache.Boxart))
            {
                await CacheImage(game.Artwork.Boxart, "boxart");
            }

            // Create Boxart button content from the cached boxart image
            BtnBoxart.Content = CreateButtonContent(game.ArtworkCache.Boxart);
            // Load icon
            // Check if it's cached and if it's not cache it
            if (game.ArtworkCache.Icon == null || !File.Exists(game.ArtworkCache.Icon))
            {
                await CacheImage(game.Artwork.Icon, "icon");
            }

            // Create Icon button content from the cached boxart image
            BtnIcon.Content = CreateButtonContent(game.ArtworkCache.Icon, 64, 64);
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
                await LoadContentIntoUi();
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
        private void GetIconFromFile(string filePath, string outputPath, uint width = 150, uint height = 207)
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
            using FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            using MagickImage magickImage = new MagickImage(fileStream, currentFormat);
            // Resize the image to the specified dimensions (this will stretch the image)
            magickImage.Resize(width, height);

            // Convert to the correct format
            magickImage.Format = currentFormat;
            magickImage.Write(outputPath);
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
        /// Checks if the current title is already in Xenia Manager before saving
        /// </summary>
        private bool CheckForDuplicateTitle()
        {
            foreach (Game game in GameManager.Games)
            {
                if (game.Title == TxtGameTitle.Text.Trim())
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// This is used to adjust the game
        /// </summary>
        private void AdjustGameTitle()
        {
            try
            {
                if (game.FileLocations.ConfigFilePath.Contains(game.Title))
                {
                    Log.Information("Renaming the configuration file to fit the new title");
                    // Rename the configuration file to fit the new title
                    File.Move(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, game.FileLocations.ConfigFilePath),
                        Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                            Path.GetDirectoryName(game.FileLocations.ConfigFilePath),
                            $"{RemoveUnsupportedCharacters(TxtGameTitle.Text)}.config.toml"), true);

                    // Construct the new full path with the new file name
                    game.FileLocations.ConfigFilePath = Path.Combine(
                        game.FileLocations.ConfigFilePath.Substring(0,
                            game.FileLocations.ConfigFilePath.LastIndexOf('\\') + 1),
                        $"{RemoveUnsupportedCharacters(TxtGameTitle.Text)}.config.toml");
                }

                Log.Information("Moving the game related data to a new folder");
                if (@$"GameData\{game.Title}" != @$"GameData\{RemoveUnsupportedCharacters(TxtGameTitle.Text)}")
                {
                    Directory.Move(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @$"GameData\{game.Title}"),
                        Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                            @$"GameData\{RemoveUnsupportedCharacters(TxtGameTitle.Text)}"));
                }

                // This is to move all the backups to the new name
                if (Directory.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Backup", game.Title)))
                {
                    Directory.Move(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Backup", game.Title),
                        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Backup", TxtGameTitle.Text));
                }

                Log.Information("Changing the game title in the library");
                game.Title = RemoveUnsupportedCharacters(TxtGameTitle.Text);

                // Adjust artwork paths
                game.Artwork.Background = @$"GameData\{game.Title}\Artwork\background.png";
                game.Artwork.Boxart = @$"GameData\{game.Title}\Artwork\boxart.png";
                game.Artwork.Icon = @$"GameData\{game.Title}\Artwork\icon.ico";
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message + "\nFull Error:\n" + ex);
                MessageBox.Show(ex.Message);
            }
        }
    }
}