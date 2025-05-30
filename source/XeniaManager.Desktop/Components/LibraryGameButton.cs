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
using XeniaManager.Core;
using XeniaManager.Core.Game;
using XeniaManager.Desktop.Utilities;
using EventManager = XeniaManager.Desktop.Utilities.EventManager;
using XeniaManager.Desktop.Views.Pages;
using XeniaManager.Desktop.Views.Windows;
using Button = Wpf.Ui.Controls.Button;
using MessageBoxResult = Wpf.Ui.Controls.MessageBoxResult;

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
        ContextMenu = CreateContextMenu();
        ToolTip = CreateToolTip();
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
            boxartPath = Path.Combine(Constants.DirectoryPaths.Base, _game.Artwork.Boxart);
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
    /// Creates the Tooltip for the game button
    /// </summary>
    private TextBlock CreateToolTip()
    {
        TextBlock tooltip = new TextBlock { TextAlignment = TextAlignment.Center };
        tooltip.Inlines.Add(new Run(_game.Title) { FontWeight = FontWeights.Bold }); // Adding game title to tooltip

        // Compatibility rating to the tooltip
        switch (_game.Compatibility.Rating)
        {
            case CompatibilityRating.Unknown:
                tooltip.Inlines.Add(new Run($"\n{LocalizationHelper.GetUiText("CompatibilityRating_Unknown")}") { FontWeight = FontWeights.Bold, TextDecorations = TextDecorations.Underline });
                break;
            case CompatibilityRating.Unplayable:
                tooltip.Inlines.Add(new Run($"\n{LocalizationHelper.GetUiText("CompatibilityRating_Unplayable")}") { FontWeight = FontWeights.Bold, TextDecorations = TextDecorations.Underline });
                break;
            case CompatibilityRating.Loads:
                tooltip.Inlines.Add(new Run($"\n{LocalizationHelper.GetUiText("CompatibilityRating_Loads")}") { FontWeight = FontWeights.Bold, TextDecorations = TextDecorations.Underline });
                break;
            case CompatibilityRating.Gameplay:
                tooltip.Inlines.Add(new Run($"\n{LocalizationHelper.GetUiText("CompatibilityRating_Gameplay")}") { FontWeight = FontWeights.Bold, TextDecorations = TextDecorations.Underline });
                break;
            case CompatibilityRating.Playable:
                tooltip.Inlines.Add(new Run($"\n{LocalizationHelper.GetUiText("CompatibilityRating_Playable")}") { FontWeight = FontWeights.Bold, TextDecorations = TextDecorations.Underline });
                break;
        }
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
    /// Creates a contextmenu for the library game button
    /// </summary>
    /// <returns></returns>
    private ContextMenu CreateContextMenu()
    {
        ContextMenu mainMenu = new ContextMenu();
        // TODO: Option to configure controls (Mousehook Exclusive)
        if (_game.XeniaVersion != XeniaVersion.Custom)
        {
            // TODO: Content installation and manager
            MenuItem contentMenu = new MenuItem { Header = LocalizationHelper.GetUiText("LibraryGameButton_ContentMenuText") };
            // TODO: Install Content
            /*
            contentMenu.Items.Add(CreateContextMenuItem(LocalizationHelper.GetUiText("LibraryGameButton_InstallContent"), null, (_, _) =>
            {
                CustomMessageBox.Show("Not implemented yet", "This isn't implemented yet.");
            }));

            // TODO: View Installed Content
            contentMenu.Items.Add(CreateContextMenuItem(LocalizationHelper.GetUiText("LibraryGameButton_ViewInstalledContent"), null, (_, _) =>
            {
                //CustomMessageBox.Show("Not implemented yet", "This isn't implemented yet.");
            }));*/

            // TODO: Open Save Backup
            mainMenu.Items.Add(contentMenu);

            // Patch installer/downloader/configurator
            MenuItem patchesMenu = new MenuItem { Header = LocalizationHelper.GetUiText("LibraryGameButton_PatchesMenuText") };
            if (_game.FileLocations.Patch == null)
            {
                // Install Local Patches
                patchesMenu.Items.Add(CreateContextMenuItem(LocalizationHelper.GetUiText("LibraryGameButton_InstallPatches"), null, (_, _) =>
                {
                    //CustomMessageBox.Show("Not implemented yet", "This isn't implemented yet.");
                    Logger.Info("Opening file dialog");
                    OpenFileDialog openFileDialog = new OpenFileDialog
                    {
                        Title = LocalizationHelper.GetUiText("OpenFileDialog_SelectGamePatchTitle"),
                        Filter = "Supported Files|*.toml|All Files|*",
                        Multiselect = false
                    };
                    if (openFileDialog.ShowDialog() == false)
                    {
                        Logger.Info("Cancelling adding additional patches");
                        return;
                    }
                    Logger.Info($"Selected file: {openFileDialog.FileName}");
                    string patchesLocation = _game.XeniaVersion switch
                    {
                        XeniaVersion.Canary => Constants.Xenia.Canary.PatchFolderLocation,
                        XeniaVersion.Mousehook => throw new NotImplementedException("Xenia Mousehook is not implemented yet"),
                        XeniaVersion.Netplay => throw new NotImplementedException("Xenia Netplay is not implemented yet"),
                        _ => throw new NotSupportedException("Unexpected build type")
                    };
                    PatchManager.InstallLocalPatch(_game, patchesLocation, openFileDialog.FileName);
                    EventManager.RequestLibraryUiRefresh(); // Reload UI
                    CustomMessageBox.Show("Patches installed", $"Patches have been installed for {_game.Title}.");
                }));

                // Download Patches
                patchesMenu.Items.Add(CreateContextMenuItem(LocalizationHelper.GetUiText("LibraryGameButton_DownloadPatches"), null, async (_, _) =>
                {
                    //CustomMessageBox.Show("Not implemented yet", "This isn't implemented yet.");
                    GamePatchesDatabase patchesDatabase = null;
                    Mouse.OverrideCursor = Cursors.Wait;
                    using (new WindowDisabler(this))
                    {
                        patchesDatabase = new GamePatchesDatabase(_game, await Github.GetGamePatches(XeniaVersion.Canary), await Github.GetGamePatches(XeniaVersion.Netplay));
                    }
                    Mouse.OverrideCursor = null;
                    patchesDatabase.ShowDialog();
                    EventManager.RequestLibraryUiRefresh(); // Reload UI
                }));
            }
            else
            {
                // Add Additional Patches
                patchesMenu.Items.Add(CreateContextMenuItem(LocalizationHelper.GetUiText("LibraryGameButton_AddAdditionalPatches"), null, (_, _) =>
                {
                    //CustomMessageBox.Show("Not implemented yet", "This isn't implemented yet.");
                    Logger.Info("Opening file dialog");
                    OpenFileDialog openFileDialog = new OpenFileDialog
                    {
                        Title = LocalizationHelper.GetUiText("OpenFileDialog_SelectGamePatchTitle"),
                        Filter = "Supported Files|*.toml|All Files|*",
                        Multiselect = false
                    };
                    if (openFileDialog.ShowDialog() == false)
                    {
                        Logger.Info("Cancelling adding additional patches");
                        return;
                    }
                    string addedPatches = PatchManager.AddAdditionalPatches(_game.FileLocations.Patch, openFileDialog.FileName);
                    if (!string.IsNullOrEmpty(addedPatches))
                    {
                        EventManager.RequestLibraryUiRefresh(); // Reload UI
                        CustomMessageBox.Show("Patches added", $"{addedPatches}\nAdditional patches have been added for {_game.Title}.");
                    }
                }));

                // Configure Patches
                patchesMenu.Items.Add(CreateContextMenuItem(LocalizationHelper.GetUiText("LibraryGameButton_ConfigurePatches"), null, (_, _) =>
                {
                    Logger.Info($"Loading patches for {_game.Title}");
                    Logger.Debug($"Patch file location: {Path.Combine(Constants.DirectoryPaths.Base, _game.FileLocations.Patch)}");
                    GamePatchesSettings gamePatchesSettings = new GamePatchesSettings(_game.Title, Path.Combine(Constants.DirectoryPaths.Base, _game.FileLocations.Patch));
                    gamePatchesSettings.ShowDialog();
                }));

                // Remove Patches
                patchesMenu.Items.Add(CreateContextMenuItem(LocalizationHelper.GetUiText("LibraryGameButton_RemovePatches"), null, (_, _) =>
                {
                    PatchManager.RemoveGamePatches(_game);
                    EventManager.RequestLibraryUiRefresh(); // Reload UI
                    CustomMessageBox.Show("Patches removed", $"Patches have been removed for {_game.Title}.");
                }));
            }
            mainMenu.Items.Add(patchesMenu);
        }

        // Option to create shortcut
        MenuItem shortcutMenu = new MenuItem { Header = LocalizationHelper.GetUiText("LibraryGameButton_ShortcutMenuText") };

        // Desktop Shortcut
        shortcutMenu.Items.Add(CreateContextMenuItem(LocalizationHelper.GetUiText("LibraryGameButton_DesktopShortcut"), null, (_, _) =>
        {
            Shortcut.DesktopShortcut(_game);
        }));

        // Steam Shortcut
        if (!string.IsNullOrEmpty(Shortcut.FindSteamInstallPath()))
        {
            shortcutMenu.Items.Add(CreateContextMenuItem(LocalizationHelper.GetUiText("LibraryGameButton_SteamShortcut"), null, (_, _) =>
            {
                try
                {
                    Shortcut.SteamShortcut(_game);
                }
                catch (Exception ex)
                {
                    CustomMessageBox.Show(ex);
                }
            }));
        }
        mainMenu.Items.Add(shortcutMenu);

        // Switch emulator version and game location
        MenuItem locationMenu = new MenuItem { Header = LocalizationHelper.GetUiText("LibraryGameButton_LocationMenuText") };

        // Change game location
        locationMenu.Items.Add(CreateContextMenuItem(LocalizationHelper.GetUiText("LibraryGameButton_ChangeGamePath"), "", (_, _) =>
        {
            Logger.Info("Opening file dialog for changing game path.");
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Title = LocalizationHelper.GetUiText("OpenFileDialog_SelectGameTitle"),
                Filter = "All Files|*|Supported Files|*.iso;*.xex;*.zar",
                Multiselect = false
            };

            if (openFileDialog.ShowDialog() == false)
            {
                Logger.Info("Cancelling changing game path.");
                return;
            }

            Logger.Debug($"New game path: {openFileDialog.FileName}");
            _game.FileLocations.Game = openFileDialog.FileName;
            GameManager.SaveLibrary();
            EventManager.RequestLibraryUiRefresh(); // Reload UI
        }));
        // TODO: Option to switch to different Xenia version
        mainMenu.Items.Add(locationMenu);

        // Open Compatibility Page (If there is one)
        if (_game.Compatibility.Url != null)
        {
            mainMenu.Items.Add(CreateContextMenuItem(LocalizationHelper.GetUiText("LibraryGameButton_OpenCompatibilityPage"), null, (_, _) =>
            {
                Process.Start(new ProcessStartInfo(_game.Compatibility.Url) { UseShellExecute = true });
            }));
        }

        MenuItem editorMenu = new MenuItem { Header = LocalizationHelper.GetUiText("LibraryGameButton_EditMenuText") };
        
        // Edit Game Details (title, boxart, icon, background...)
        editorMenu.Items.Add(CreateContextMenuItem(LocalizationHelper.GetUiText("LibraryGameButton_EditGameDetails"), null, (_, _) =>
        {
            Logger.Info("Opening Game Details Editor.");
            GameDetailsEditor editor = new GameDetailsEditor(_game);
            editor.ShowDialog();
            GameManager.SaveLibrary();
        }));
        
        editorMenu.Items.Add(CreateContextMenuItem(LocalizationHelper.GetUiText("LibraryGameButton_EditGameSettings"), null, (_, _) =>
        {
            Logger.Info("Opening Game Settings Editor.");
            //CustomMessageBox.Show("Not implemented yet", "This isn't implemented yet.");
            GameSettingsEditor gameSettingsEditor = new GameSettingsEditor(_game);
            gameSettingsEditor.ShowDialog();
        }));

        mainMenu.Items.Add(editorMenu);

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
    private async void ButtonClick(object sender, RoutedEventArgs args)
    {
        await Launcher.LaunchGameASync(_game);
    }
}