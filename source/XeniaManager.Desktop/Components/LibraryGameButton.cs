using System.Diagnostics;
using System.IO;
using Path = System.IO.Path;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

// Imported
using Microsoft.Win32;
using Octokit;
using XeniaManager.Core;
using XeniaManager.Core.Game;
using XeniaManager.Desktop.Utilities;
using EventManager = XeniaManager.Desktop.Utilities.EventManager;
using XeniaManager.Desktop.Views.Pages;
using XeniaManager.Desktop.Views.Windows;
using Button = Wpf.Ui.Controls.Button;
using MessageBoxResult = Wpf.Ui.Controls.MessageBoxResult;
using XeniaManager.Core.Constants;

namespace XeniaManager.Desktop.Components;

/// <summary>
/// Customized Button used to show games on the Library page
/// </summary>
public class LibraryGameButton : Button
{
    // Variables
    // Game related variables
    private Game _game { get; set; }

    // Search
    public string GameTitle { get; set; }
    public string TitleId { get; set; }

    private LibraryPage _library { get; set; }

    // Constructors
    public LibraryGameButton(Game game, LibraryPage library)
    {
        GameTitle = game.Title;
        TitleId = game.GameId;
        _game = game;
        _library = library;
        Style = CreateStyle();
        Content = CreateContent();
        ContextMenu = GameUIHelper.CreateContextMenu(_game, this);
        ToolTip = GameUIHelper.CreateTooltip(_game);
        Click += (sender, args) => GameUIHelper.Game_Click(_game, sender, args);
    }

    // Functions
    /// <summary>
    /// Creates a style for the Library Game Button
    /// </summary>
    private Style CreateStyle()
    {
        Style buttonStyle = new Style(typeof(LibraryGameButton)) { BasedOn = (Style)FindResource("DefaultUiButtonStyle") };
        buttonStyle.Setters.Add(new Setter(BorderThicknessProperty, new Thickness(2)));
        buttonStyle.Setters.Add(new Setter(PaddingProperty, new Thickness(0)));
        buttonStyle.Setters.Add(new Setter(CursorProperty, Cursors.Hand));
        buttonStyle.Setters.Add(new Setter(MarginProperty, new Thickness(5)));
        buttonStyle.Setters.Add(new Setter(HorizontalContentAlignmentProperty, HorizontalAlignment.Stretch));
        buttonStyle.Setters.Add(new Setter(VerticalContentAlignmentProperty, VerticalAlignment.Stretch));
        buttonStyle.Setters.Add(new Setter(TextBlock.TextAlignmentProperty, TextAlignment.Center));
        buttonStyle.Setters.Add(new Setter(WidthProperty, 150.0));
        buttonStyle.Setters.Add(new Setter(HeightProperty, 207.0));
        return buttonStyle;
    }

    /// <summary>
    /// Adds the boxart/game title and title_id to the button
    /// </summary>
    /// <returns></returns>
    private Border CreateContent()
    {
        Grid mainGrid = new Grid();
        string boxartPath = string.Empty;
        try
        {
            boxartPath = Path.Combine(DirectoryPaths.Base, _game.Artwork.Boxart);
        }
        catch (Exception ex)
        {
            Logger.Error($"There was an error reading boxart location: {ex.Message}");
            boxartPath = string.Empty;
        }

        // Check if the boxart exists, if it doesn't add game title and title id as a backup solution instead of crashing
        if (File.Exists(boxartPath))
        {
            mainGrid.Children.Add(new Image
            {
                Source = ArtworkManager.CacheLoadArtwork(boxartPath),
                Stretch = Stretch.UniformToFill
            });

            // Checks if it needs to display game title at the bottom of the button
            if (App.Settings.Ui.Library.GameTitle)
            {
                Border textOverlay = new Border
                {
                    // Use a semi-transparent background to ensure readability
                    Background = new SolidColorBrush(Color.FromArgb(200, 0, 0, 0)),
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Bottom,
                    Padding = new Thickness(3) // Adjust padding as needed
                };

                // The text to display on the overlay
                TextBlock gameTitleText = new TextBlock
                {
                    Text = _game.Title,
                    Foreground = Brushes.White,
                    FontSize = 12,
                    FontWeight = FontWeights.Bold,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    TextAlignment = TextAlignment.Center,
                    TextWrapping = TextWrapping.Wrap
                };

                // Place the text inside the overlay border
                textOverlay.Child = gameTitleText;
                // Add the overlay to the grid
                mainGrid.Children.Add(textOverlay);
            }
        }
        else
        {
            mainGrid.Children.Add(new TextBlock
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Text = $"{GameTitle}\n({TitleId})",
                TextAlignment = TextAlignment.Center
            });
        }

        // Game Compatibility
        if (App.Settings.Ui.Library.CompatibilityRating)
        {
            Ellipse compatibilityStatus = new Ellipse
            {
                Width = 15,
                Height = 15,
                Stroke = new SolidColorBrush(Colors.Black),
                StrokeThickness = 1,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(4, 4, 0, 0)
            };

            compatibilityStatus.Fill = GameUIHelper.CompatibilityRatingColor(_game.Compatibility.Rating);

            mainGrid.Children.Add(compatibilityStatus);
        }

        return new Border
        {
            Child = mainGrid,
            Clip = new RectangleGeometry
            {
                Rect = new Rect(0, 0, 150, 207),
                RadiusX = 3,
                RadiusY = 3
            }
        };
    }
}