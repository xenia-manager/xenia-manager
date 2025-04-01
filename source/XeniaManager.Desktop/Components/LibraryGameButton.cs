using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

// Imported
using XeniaManager.Core;
using XeniaManager.Core.Game;
using XeniaManager.Desktop.Views.Pages;
using Button = Wpf.Ui.Controls.Button;
using MessageBoxResult = Wpf.Ui.Controls.MessageBoxResult;

namespace XeniaManager.Desktop.Components;

public class LibraryGameButton : Button
{
    // Variables
    // Game related variables
    private string _gameTitle { get; set; }
    private string _titleId { get; set; }
    private Game _game { get; set; }
    private LibraryPage _library { get; set; }
    private Style _buttonStyle { get; set; }

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
        Click += ButtonClick;
    }

    // Functions
    private Style CreateStyle()
    {
        _buttonStyle = new Style(typeof(LibraryGameButton)) { BasedOn = (Style)FindResource("DefaultUiButtonStyle") };
        _buttonStyle.Setters.Add(new Setter(BorderThicknessProperty, new Thickness(0)));
        _buttonStyle.Setters.Add(new Setter(PaddingProperty, new Thickness(0)));
        _buttonStyle.Setters.Add(new Setter(CursorProperty, Cursors.Hand));
        _buttonStyle.Setters.Add(new Setter(MarginProperty, new Thickness(5)));
        _buttonStyle.Setters.Add(new Setter(HorizontalContentAlignmentProperty, HorizontalAlignment.Stretch));
        _buttonStyle.Setters.Add(new Setter(VerticalContentAlignmentProperty, VerticalAlignment.Stretch));
        _buttonStyle.Setters.Add(new Setter(TextBlock.TextAlignmentProperty, TextAlignment.Center));
        _buttonStyle.Setters.Add(new Setter(WidthProperty, 150.0));
        _buttonStyle.Setters.Add(new Setter(HeightProperty, 207.0));
        return _buttonStyle;
    }

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
        if (File.Exists(boxartPath))
        {
            mainGrid.Children.Add(new Image
            {
                Source = ArtworkManager.CacheLoadArtwork(boxartPath),
                Stretch = Stretch.UniformToFill
            });
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

    private ContextMenu CreateContextMenu()
    {
        ContextMenu mainMenu = new ContextMenu();
        mainMenu.Items.Add(CreateContextMenuItem("Remove from Xenia Manager", null, async (_, _) =>
        {
            bool deleteGameContent = false;
            if (await CustomMessageBox.YesNo($"Remove {_game.Title}", $"Do you want to remove {_game.Title}?") != MessageBoxResult.Primary)
            {
                Logger.Info($"Cancelled removal of {_game.Title}");
                return;
            }

            if (await CustomMessageBox.YesNo($"Remove {_game.Title} content",
                    $"Do you want to remove {_game.Title} content folder?\nThis will get rid of all of the installed title updates, save games etc.") == MessageBoxResult.Primary)
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

    private async void ButtonClick(object sender, RoutedEventArgs args)
    {
        Launcher.LaunchGame(_game);
    }
}