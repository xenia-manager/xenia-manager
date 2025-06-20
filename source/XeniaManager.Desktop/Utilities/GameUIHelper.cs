using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;


// Imported Libraries
using Microsoft.Win32;
using MessageBoxResult = Wpf.Ui.Controls.MessageBoxResult;
using XeniaManager.Core;
using XeniaManager.Core.Game;
using XeniaManager.Desktop.Components;
using XeniaManager.Desktop.Converters;
using XeniaManager.Desktop.Views.Windows;
using XeniaManager.Core.Installation;

namespace XeniaManager.Desktop.Utilities;
public static class GameUIHelper
{
    /// <summary>
    /// Creates a ContextMenuItem for the game button
    /// </summary>
    /// <param name="header">Text that is shown in the ContextMenu for this option</param>
    /// <param name="toolTipText">Hovered description of the option</param>
    /// <param name="clickHandler">Event when the option is selected</param>
    /// <returns>MenuItem</returns>
    private static MenuItem CreateContextMenuItem(string headerText, string? tooltipText, RoutedEventHandler clickHandler)
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
    public static ContextMenu CreateContextMenu(Game game, FrameworkElement element)
    {
        ContextMenu mainMenu = new ContextMenu();
        // TODO: Option to configure controls (Mousehook Exclusive)
        if (game.XeniaVersion != XeniaVersion.Custom)
        {
            // Content installation and manager
            MenuItem contentMenu = new MenuItem { Header = LocalizationHelper.GetUiText("LibraryGameButton_ContentMenuText") };
            // TODO: Install Content
            /*
            contentMenu.Items.Add(CreateContextMenuItem(LocalizationHelper.GetUiText("LibraryGameButton_InstallContent"), null, (_, _) =>
            {
                CustomMessageBox.Show("Not implemented yet", "This isn't implemented yet.");
            }));*/

            // View Installed Content
            contentMenu.Items.Add(CreateContextMenuItem(LocalizationHelper.GetUiText("LibraryGameButton_ViewInstalledContent"), null, (_, _) =>
            {
                //CustomMessageBox.Show("Not implemented yet", "This isn't implemented yet.");
                Logger.Info("Launching Content Viewer window");
                ContentViewer contentViewer = new ContentViewer(game);
                contentViewer.ShowDialog();
            }));

            // View Screenshots
            contentMenu.Items.Add(CreateContextMenuItem(LocalizationHelper.GetUiText("LibraryGameButton_GameScreenshots"), null, (_, _) =>
            {
                Logger.Info("Launching Xenia Screenshot Viewer window");
                XeniaScreenshotsViewer xeniaScreenshotsViewer = new XeniaScreenshotsViewer(game);
                xeniaScreenshotsViewer.ShowDialog();
            }));

            // Open Save Backup
            contentMenu.Items.Add(CreateContextMenuItem(LocalizationHelper.GetUiText("LibraryGameButton_OpenSaveBackup"), null, (_, _) =>
            {
                //CustomMessageBox.Show("Not implemented yet", "This isn't implemented yet.");
                Logger.Info("Opening folder containing all of the save game backups");
                string backupFolder = System.IO.Path.Combine(Constants.DirectoryPaths.Backup, game.Title);
                if (!Directory.Exists(backupFolder))
                {
                    Logger.Error($"{game.Title} doesn't have any backups");
                    CustomMessageBox.Show(LocalizationHelper.GetUiText("MessageBox_MissingGameSaveBackupsTitle"), string.Format(LocalizationHelper.GetUiText("MessageBox_MissingGameSaveBackupsText"), game.Title));
                    return;
                }

                Process.Start(new ProcessStartInfo
                {
                    FileName = backupFolder,
                    UseShellExecute = true,
                    Verb = "Open"
                });
            }));
            mainMenu.Items.Add(contentMenu);

            // Patch installer/downloader/configurator
            MenuItem patchesMenu = new MenuItem { Header = LocalizationHelper.GetUiText("LibraryGameButton_PatchesMenuText") };
            if (game.FileLocations.Patch == null)
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
                    string patchesLocation = game.XeniaVersion switch
                    {
                        XeniaVersion.Canary => Constants.Xenia.Canary.PatchFolderLocation,
                        XeniaVersion.Mousehook => Constants.Xenia.Mousehook.PatchFolderLocation,
                        XeniaVersion.Netplay => throw new NotImplementedException("Xenia Netplay is not implemented yet"),
                        _ => throw new NotSupportedException("Unexpected build type")
                    };
                    PatchManager.InstallLocalPatch(game, patchesLocation, openFileDialog.FileName);
                    EventManager.RequestLibraryUiRefresh(); // Reload UI
                    CustomMessageBox.Show("Patches installed", $"Patches have been installed for {game.Title}.");
                }));

                // Download Patches
                patchesMenu.Items.Add(CreateContextMenuItem(LocalizationHelper.GetUiText("LibraryGameButton_DownloadPatches"), null, async (_, _) =>
                {
                    //CustomMessageBox.Show("Not implemented yet", "This isn't implemented yet.");
                    GamePatchesDatabase patchesDatabase = null;
                    Mouse.OverrideCursor = Cursors.Wait;
                    using (new WindowDisabler(element))
                    {
                        if (game.XeniaVersion == XeniaVersion.Netplay)
                        {
                            patchesDatabase = new GamePatchesDatabase(game, await Github.GetGamePatches(XeniaVersion.Canary), await Github.GetGamePatches(XeniaVersion.Netplay));
                        }
                        else
                        {
                            patchesDatabase = new GamePatchesDatabase(game, await Github.GetGamePatches(XeniaVersion.Canary), []);
                        }
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
                    string addedPatches = PatchManager.AddAdditionalPatches(game.FileLocations.Patch, openFileDialog.FileName);
                    if (!string.IsNullOrEmpty(addedPatches))
                    {
                        EventManager.RequestLibraryUiRefresh(); // Reload UI
                        CustomMessageBox.Show("Patches added", $"{addedPatches}\nAdditional patches have been added for {game.Title}.");
                    }
                }));

                // Configure Patches
                patchesMenu.Items.Add(CreateContextMenuItem(LocalizationHelper.GetUiText("LibraryGameButton_ConfigurePatches"), null, (_, _) =>
                {
                    Logger.Info($"Loading patches for {game.Title}");
                    Logger.Debug($"Patch file location: {System.IO.Path.Combine(Constants.DirectoryPaths.Base, game.FileLocations.Patch)}");
                    GamePatchesSettings gamePatchesSettings = new GamePatchesSettings(game, System.IO.Path.Combine(Constants.DirectoryPaths.Base, game.FileLocations.Patch));
                    gamePatchesSettings.ShowDialog();
                }));

                // Remove Patches
                patchesMenu.Items.Add(CreateContextMenuItem(LocalizationHelper.GetUiText("LibraryGameButton_RemovePatches"), null, (_, _) =>
                {
                    PatchManager.RemoveGamePatches(game);
                    EventManager.RequestLibraryUiRefresh(); // Reload UI
                    CustomMessageBox.Show("Patches removed", $"Patches have been removed for {game.Title}.");
                }));
            }
            mainMenu.Items.Add(patchesMenu);
        }

        // Option to create shortcut
        MenuItem shortcutMenu = new MenuItem { Header = LocalizationHelper.GetUiText("LibraryGameButton_ShortcutMenuText") };

        // Desktop Shortcut
        shortcutMenu.Items.Add(CreateContextMenuItem(LocalizationHelper.GetUiText("LibraryGameButton_DesktopShortcut"), null, (_, _) =>
        {
            Shortcut.DesktopShortcut(game);
        }));

        // Steam Shortcut
        if (!string.IsNullOrEmpty(Shortcut.FindSteamInstallPath()))
        {
            shortcutMenu.Items.Add(CreateContextMenuItem(LocalizationHelper.GetUiText("LibraryGameButton_SteamShortcut"), null, (_, _) =>
            {
                try
                {
                    Shortcut.SteamShortcut(game);
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
            game.FileLocations.Game = openFileDialog.FileName;
            GameManager.SaveLibrary();
            EventManager.RequestLibraryUiRefresh(); // Reload UI
        }));

        // "Switch to Xenia Canary" option
        MenuItem switchXeniaCanary = CreateContextMenuItem(LocalizationHelper.GetUiText("LibraryGameButton_SwitchToXeniaCanary"), LocalizationHelper.GetUiText("LibraryGameButton_SwitchToXeniaCanaryTooltip"), (_, _) =>
        {
            try
            {
                Xenia.SwitchXeniaVersion(game, XeniaVersion.Canary);

                GameManager.SaveLibrary();
                EventManager.RequestLibraryUiRefresh(); // Reload UI
                CustomMessageBox.Show(LocalizationHelper.GetUiText("MessageBox_Success"), string.Format(LocalizationHelper.GetUiText("MessageBox_SwitchXeniaVersion"), game.Title, game.XeniaVersion));
            }
            catch (Exception ex)
            {
                Logger.Error($"{ex.Message}\nFull Error:\n{ex}");
                CustomMessageBox.Show(ex);
            }
        }); 
        MenuItem switchXeniaMousehook = CreateContextMenuItem(LocalizationHelper.GetUiText("LibraryGameButton_SwitchToXeniaMousehook"), LocalizationHelper.GetUiText("LibraryGameButton_SwitchToXeniaMousehookTooltip"), (_, _) =>
        {
            try
            {
                Xenia.SwitchXeniaVersion(game, XeniaVersion.Mousehook);

                GameManager.SaveLibrary();
                EventManager.RequestLibraryUiRefresh(); // Reload UI
                CustomMessageBox.Show(LocalizationHelper.GetUiText("MessageBox_Success"), string.Format(LocalizationHelper.GetUiText("MessageBox_SwitchXeniaVersion"), game.Title, game.XeniaVersion));
            }
            catch (Exception ex)
            {
                Logger.Error($"{ex.Message}\nFull Error:\n{ex}");
                CustomMessageBox.Show(ex);
            }
        }); 
        MenuItem switchXeniaNetplay = CreateContextMenuItem("Switch to Xenia Netplay", "Changes the Xenia version used by the game to Xenia Canary", (_, _) =>
        {

        });

        switch (game.XeniaVersion)
        {
            case XeniaVersion.Canary:
                if (App.Settings.IsXeniaInstalled(XeniaVersion.Mousehook))
                {
                    locationMenu.Items.Add(switchXeniaMousehook);
                }

                if (App.Settings.IsXeniaInstalled(XeniaVersion.Netplay))
                {
                    locationMenu.Items.Add(switchXeniaNetplay);
                }
                break;
            case XeniaVersion.Mousehook:
                if (App.Settings.IsXeniaInstalled(XeniaVersion.Canary))
                {
                    locationMenu.Items.Add(switchXeniaCanary);
                }

                if (App.Settings.IsXeniaInstalled(XeniaVersion.Netplay))
                {
                    locationMenu.Items.Add(switchXeniaNetplay);
                }
                break;
            case XeniaVersion.Netplay:
                if (App.Settings.IsXeniaInstalled(XeniaVersion.Canary))
                {
                    locationMenu.Items.Add(switchXeniaCanary);
                }

                if (App.Settings.IsXeniaInstalled(XeniaVersion.Mousehook))
                {
                    locationMenu.Items.Add(switchXeniaMousehook);
                }
                break;
            case XeniaVersion.Custom:
                if (App.Settings.IsXeniaInstalled(XeniaVersion.Canary))
                {
                    locationMenu.Items.Add(switchXeniaCanary);
                }

                if (App.Settings.IsXeniaInstalled(XeniaVersion.Mousehook))
                {
                    locationMenu.Items.Add(switchXeniaMousehook);
                }

                if (App.Settings.IsXeniaInstalled(XeniaVersion.Netplay))
                {
                    locationMenu.Items.Add(switchXeniaNetplay);
                }
                break;
            default:
                break;
        }

        locationMenu.Items.Add(CreateContextMenuItem(LocalizationHelper.GetUiText("LibraryGameButton_SwitchToXeniaCustom"), "", (_, _) =>
        {
            Logger.Info("Opening file dialog for changing game path.");
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Title = LocalizationHelper.GetUiText("OpenFileDialog_SelectXeniaExecutable"),
                Filter = "Xenia Executable|xenia*.exe|All Executables (*.exe)|*.exe",
                Multiselect = false,
                DefaultExt = ".exe"
            };
            if (openFileDialog.ShowDialog() != true)
            {
                return;
            }

            Logger.Debug($"Selected executable: {openFileDialog.FileName}");
            try
            {
                Xenia.SwitchXeniaVersion(game, XeniaVersion.Custom, openFileDialog.FileName);

                GameManager.SaveLibrary();
                EventManager.RequestLibraryUiRefresh(); // Reload UI
                CustomMessageBox.Show(LocalizationHelper.GetUiText("MessageBox_Success"), string.Format(LocalizationHelper.GetUiText("MessageBox_SwitchXeniaVersion"), game.Title, game.XeniaVersion));
            }
            catch (Exception ex)
            {
                Logger.Error($"{ex.Message}\nFull Error:\n{ex}");
                CustomMessageBox.Show(ex);
            }
        }));
        // TODO: Option to switch to different Xenia version
        mainMenu.Items.Add(locationMenu);

        // Open Compatibility Page (If there is one)
        if (!string.IsNullOrEmpty(game.Compatibility.Url) && !string.IsNullOrWhiteSpace(game.Compatibility.Url))
        {
            mainMenu.Items.Add(CreateContextMenuItem(LocalizationHelper.GetUiText("LibraryGameButton_OpenCompatibilityPage"), null, (_, _) =>
            {
                Process.Start(new ProcessStartInfo(game.Compatibility.Url) { UseShellExecute = true });
            }));
        }

        MenuItem editorMenu = new MenuItem { Header = LocalizationHelper.GetUiText("LibraryGameButton_EditMenuText") };

        // Edit Game Details (title, boxart, icon, background...)
        editorMenu.Items.Add(CreateContextMenuItem(LocalizationHelper.GetUiText("LibraryGameButton_EditGameDetails"), null, (_, _) =>
        {
            Logger.Info("Opening Game Details Editor.");
            GameDetailsEditor editor = new GameDetailsEditor(game);
            editor.ShowDialog();
            EventManager.RequestLibraryUiRefresh();
            GameManager.SaveLibrary();
        }));

        editorMenu.Items.Add(CreateContextMenuItem(LocalizationHelper.GetUiText("LibraryGameButton_EditGameSettings"), null, (_, _) =>
        {
            Logger.Info("Opening Game Settings Editor.");
            //CustomMessageBox.Show("Not implemented yet", "This isn't implemented yet.");
            GameSettingsEditor gameSettingsEditor = new GameSettingsEditor(game);
            gameSettingsEditor.ShowDialog();
        }));

        mainMenu.Items.Add(editorMenu);

        // Option to remove the game from Xenia Manager
        mainMenu.Items.Add(CreateContextMenuItem(LocalizationHelper.GetUiText("LibraryGameButton_RemoveGameHeaderText"), null, async (_, _) =>
        {
            bool deleteGameContent = false;
            if (await CustomMessageBox.YesNo($"{LocalizationHelper.GetUiText("MessageBox_Remove")} {game.Title}",
                    $"{string.Format(LocalizationHelper.GetUiText("MessageBox_RemoveGameText"), game.Title)}") != MessageBoxResult.Primary)
            {
                Logger.Info($"Cancelled removal of {game.Title}");
                return;
            }

            if (await CustomMessageBox.YesNo(string.Format(LocalizationHelper.GetUiText("MessageBox_RemoveGameContentTitle"), game.Title),
                    string.Format(LocalizationHelper.GetUiText("MessageBox_RemoveGameContentText"), game.Title)) == MessageBoxResult.Primary)
            {
                deleteGameContent = true;
            }

            Logger.Info($"Removing {game.Title}");
            GameManager.RemoveGame(game, deleteGameContent);
            // Reload Library UI
            EventManager.RequestLibraryUiRefresh();
        }));
        return mainMenu;
    }

    public static SolidColorBrush CompatibilityRatingColor(CompatibilityRating rating)
    {
        switch (rating)
        {
            case CompatibilityRating.Unknown:
                return Brushes.DarkGray;
            case CompatibilityRating.Unplayable:
                return Brushes.Red;
            case CompatibilityRating.Loads:
                return Brushes.Yellow;
            case CompatibilityRating.Gameplay:
                return Brushes.GreenYellow;
            case CompatibilityRating.Playable:
                return Brushes.ForestGreen;
            default:
                throw new NotImplementedException("Not supported compatibility rating");
        }
    }

    public static ToolTip CreateTooltip(Game game)
    {
        StackPanel panel = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Margin = new Thickness(4),
            MaxWidth = 250
        };
        TextBlock titleTb = new TextBlock
        {
            Text = game.Title,
            FontWeight = FontWeights.Bold,
            FontSize = 14,
            TextAlignment = TextAlignment.Center,
            TextWrapping = TextWrapping.Wrap
        };
        panel.Children.Add(titleTb);
        StackPanel compPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 2, 0, 2)
        };
        Ellipse compatibilityStatus = new Ellipse
        {
            Width = 15,
            Height = 15,
            Stroke = Brushes.Black,
            StrokeThickness = 1,
            VerticalAlignment = VerticalAlignment.Center
        };
        compatibilityStatus.Fill = CompatibilityRatingColor(game.Compatibility.Rating);
        compPanel.Children.Add(compatibilityStatus);

        TextBlock compText = new TextBlock
        {
            Text = " " + LocalizationHelper.GetUiText($"CompatibilityRating_{game.Compatibility.Rating}"),
            FontWeight = FontWeights.Bold,
            VerticalAlignment = VerticalAlignment.Center,
            TextWrapping = TextWrapping.Wrap
        };
        compPanel.Children.Add(compText);

        panel.Children.Add(compPanel);

        if (game.Playtime != null)
        {
            StackPanel playPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 4, 0, 0)
            };
            TextBlock labelTb = new TextBlock
            {
                Text = $"{LocalizationHelper.GetUiText("LibraryGameButton_PlaytimeTimePlayed")} ",
                FontWeight = FontWeights.Bold,
                TextWrapping = TextWrapping.Wrap
            };
            playPanel.Children.Add(labelTb);
            string formattedPlaytime = PlaytimeFormatter.Format(game.Playtime.Value, CultureInfo.CurrentCulture);
            TextBlock valueTb = new TextBlock
            {
                Text = formattedPlaytime,
                TextWrapping = TextWrapping.Wrap
            };
            playPanel.Children.Add(valueTb);

            panel.Children.Add(playPanel);
        }

        return new ToolTip
        {
            Content = panel
        };
    }


    public async static void Game_Click(Game game, object sender, RoutedEventArgs args)
    {
        try
        {
            if (App.Settings.Ui.ShowGameLoadingBackground && !Launcher.XeniaUpdating)
            {
                FullscreenImageWindow fullscreenImageWindow = new FullscreenImageWindow(System.IO.Path.Combine(Constants.DirectoryPaths.Base, game.Artwork.Background), true);
                fullscreenImageWindow.Show();
                _ = Task.Run(async () =>
                {
                    await Task.Delay(2000);
                    fullscreenImageWindow.Dispatcher.Invoke(() => fullscreenImageWindow.Close());
                });
            }
            await Launcher.LaunchGameASync(game, App.AppSettings.Settings.Emulator.Settings.Profile.AutomaticSaveBackup, App.AppSettings.Settings.Emulator.Settings.Profile.ProfileSlot);
            GameManager.SaveLibrary();
            EventManager.RequestLibraryUiRefresh();
        }
        catch (Exception ex)
        {
            Logger.Error($"{ex.Message}\nFull Error:\n{ex}");
            await CustomMessageBox.Show(ex);
        }
    }
}