using System;
using System.IO;
using System.Windows;
using System.Windows.Media.Animation;

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
            // Checking if there have been made some changes to game title
            if (game.Title != GameTitle.Text)
            {
                if (!CheckForDuplicateTitle())
                {
                    // Adjust game title before moving forwards
                    Log.Information("Detected game title change");
                    AdjustGameTitle();
                }
                else
                {
                    Log.Warning("Duplicate title found");
                    MessageBox.Show("This title is already taken by another game. Please change it");
                    return;
                }
            }
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
            // Checking if there have been made some changes to game title
            if (game.Title != GameTitle.Text)
            {
                // Adjust game title before moving forwards
                Log.Information("Detected game title change");
                AdjustGameTitle();
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
            GameBoxart.Content = CreateButtonContent(game.ArtworkCache.Boxart);
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
            // Checking if there have been made some changes to game title
            if (game.Title != GameTitle.Text)
            {
                // Adjust game title before moving forwards
                Log.Information("Detected game title change");
                AdjustGameTitle();
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
            GameIcon.Content = CreateButtonContent(game.ArtworkCache.Icon);
        }
    }
}
