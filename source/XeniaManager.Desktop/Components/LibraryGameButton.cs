using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

// Imported
using XeniaManager.Core;
using XeniaManager.Core.Game;
using XeniaManager.Desktop.Utilities;
using XeniaManager.Desktop.Views.Pages;
using Button = Wpf.Ui.Controls.Button;
using MessageBoxResult = Wpf.Ui.Controls.MessageBoxResult;
using Path = System.IO.Path;

namespace XeniaManager.Desktop.Components;

/// <summary>
/// Customized Button used to show games on the Library page
/// </summary>
public class LibraryGameButton : Button
{
    // Variables
    // Game related variables
    private string _gameTitle { get; set; }
    private string _titleId { get; set; }
    private Game _game { get; set; }
    
    private LibraryPage _library { get; set; }

    // Constructors
    public LibraryGameButton(Game game, LibraryPage library)
    {
        _gameTitle = game.Title;
        _titleId = game.GameId;
        this._game = game;
        this._library = library;
        this.Style = CreateStyle();
        this.Content = CreateContent();
        this.ContextMenu = CreateContextMenu();
        this.ToolTip = CreateToolTip();
        Click += ButtonClick;
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
            boxartPath = Path.Combine(Constants.BaseDir, _game.Artwork.Boxart);
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
            if (App.Settings.Ui.DisplayGameTitle)
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
                Text = $"{_gameTitle}\n({_titleId})",
                TextAlignment = TextAlignment.Center
            });
        }

        // Game Compatibility
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

        switch (_game.Compatibility.Rating)
        {
            case CompatibilityRating.Unknown:
                compatibilityStatus.Fill = new SolidColorBrush(Colors.DarkGray);
                break;
            case CompatibilityRating.Unplayable:
                compatibilityStatus.Fill = new SolidColorBrush(Colors.Red);
                break;
            case CompatibilityRating.Loads:
                compatibilityStatus.Fill = new SolidColorBrush(Colors.Yellow);
                break;
            case CompatibilityRating.Gameplay:
                compatibilityStatus.Fill = new SolidColorBrush(Colors.GreenYellow);
                break;
            case CompatibilityRating.Playable:
                compatibilityStatus.Fill = new SolidColorBrush(Colors.ForestGreen);
                break;
        }
        
        mainGrid.Children.Add(compatibilityStatus);
        
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

    /// <summary>
    /// Creates the Tooltip for the game button
    /// </summary>
    private TextBlock CreateToolTip()
    {
        TextBlock tooltip = new TextBlock { TextAlignment = TextAlignment.Center };
        tooltip.Inlines.Add(new Run(_game.Title) { FontWeight = FontWeights.Bold }); // Adding game title to tooltip
        return tooltip;
    }

    /// <summary>
    /// Creates a ContextMenuItem for game button
    /// </summary>
    /// <param name="header">Text that is shown in the ContextMenu for this option</param>
    /// <param name="toolTipText">Hovered description of the option</param>
    /// <param name="clickHandler">Event when the option is selected</param>
    /// <returns>MenuItem</returns>
    private MenuItem CreateContextMenuItem(string headerText, string? tooltipText, RoutedEventHandler clickHandler)
    {
        // Create ContextMenuItem
        MenuItem menuItem = new MenuItem { Header = headerText };

        // Add Tooltip text to it (if it exists)
        if (!string.IsNullOrEmpty(tooltipText))
        {
            // Check if it has a NOTE to properly format it
            if (tooltipText.Contains("\nNOTE:"))
            {
                ToolTip tooltip = new ToolTip();
                TextBlock textBlock = new TextBlock();

                // Split the string into parts
                string[] split = tooltipText.Split(["\nNOTE:"], StringSplitOptions.None);

                // Adding the first part before \nNOTE:
                textBlock.Inlines.Add(new Run(split[0]));

                // "NOTE:" bolded
                textBlock.Inlines.Add(new Run("\nNOTE:") { FontWeight = FontWeights.Bold });

                // Add the rest of the string that comes after "\nNOTE:"
                if (split.Length > 1)
                {
                    textBlock.Inlines.Add(new Run(split[1]));
                }

                // Assign the finished tooltip to the ContextMenuItem
                tooltip.Content = textBlock;
                menuItem.ToolTip = tooltip;
            }
            // If it doesn't, just add the text to the tooltip
            else
            {
                menuItem.ToolTip = tooltipText;
            }
        }

        menuItem.Click += clickHandler;
        return menuItem;
    }

    /// <summary>
    /// Creates contextmenu for the library game button
    /// </summary>
    /// <returns></returns>
    private ContextMenu CreateContextMenu()
    {
        ContextMenu mainMenu = new ContextMenu();
        // TODO: Option to configure controls (Mousehook Exclusive)
        // TODO: Content installation and manager
        // TODO: Patch installer/downloader/configurator
        // TODO: Option to create shortcut
        // TODO: Option to change game location
        // TODO: Option to switch to different Xenia version
        // TODO: Option to open compatibility page of the game (If there is one)
        // TODO: Option to edit game details (title, boxart, icon, background...)
        // Option to remove the game from Xenia Manager
        mainMenu.Items.Add(CreateContextMenuItem(LocalizationHelper.GetUiText("LibraryGameButton_RemoveGameHeaderText"), null, async (_, _) =>
        {
            bool deleteGameContent = false;
            if (await CustomMessageBox.YesNo($"{LocalizationHelper.GetUiText("MessageBox_Remove")} {_game.Title}", 
                    $"{string.Format(LocalizationHelper.GetUiText("MessageBox_RemoveGameText"), _game.Title)}") != MessageBoxResult.Primary)
            {
                Logger.Info($"Cancelled removal of {_game.Title}");
                return;
            }

            if (await CustomMessageBox.YesNo(string.Format(LocalizationHelper.GetUiText("MessageBox_RemoveGameContentTitle"), _game.Title),
                    string.Format(LocalizationHelper.GetUiText("MessageBox_RemoveGameContentText"), _game.Title)) == MessageBoxResult.Primary)
            {
                deleteGameContent = true;
            }

            Logger.Info($"Removing {_game.Title}");
            GameManager.RemoveGame(_game, deleteGameContent);
            // Reload Library UI
            _library.LoadGames();
        }));
        return mainMenu;
    }

    /// <summary>
    /// Clicking on the button launches the game
    /// </summary>
    private void ButtonClick(object sender, RoutedEventArgs args)
    {
        Launcher.LaunchGame(_game);
    }
}