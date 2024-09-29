using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media.Animation;

// Imported
using Serilog;
using XeniaManager.DesktopApp.Pages;
using XeniaManager.DesktopApp.Windows;

namespace XeniaManager.DesktopApp.CustomControls
{
    public partial class GameButton : Button
    {
        /// <summary>
        /// Creates a ContextMenu Item for a option
        /// </summary>
        /// <param name="header">Text that is shown in the ContextMenu for this option</param>
        /// <param name="toolTip">Hovered description of the option</param>
        /// <param name="clickHandler">Event when the option is selected</param>
        /// <returns></returns>
        private MenuItem CreateMenuItem(string header, string? toolTipText, RoutedEventHandler clickHandler)
        {
            MenuItem menuItem = new MenuItem { Header = header };
            if (!string.IsNullOrEmpty(toolTipText))
            {
                if (!toolTipText.Contains("\nNOTE:"))
                {
                    menuItem.ToolTip = toolTipText;
                }
                else
                {
                    ToolTip toolTip = new ToolTip();
                    TextBlock textBlock = new TextBlock();

                    // Split the string into parts
                    string[] parts = toolTipText.Split(new string[] { "\nNOTE:" }, StringSplitOptions.None);

                    // Add the first part (before "NOTE:")
                    textBlock.Inlines.Add(new Run(parts[0]));

                    // Add "NOTE:" in bold
                    Run boldRun = new Run("\nNOTE:") { FontWeight = FontWeights.Bold };
                    textBlock.Inlines.Add(boldRun);

                    // Add the rest of the string (after "NOTE:")
                    if (parts.Length > 1)
                    {
                        textBlock.Inlines.Add(new Run(parts[1]));
                    }

                    // Assign TextBlock to ToolTip's content
                    toolTip.Content = textBlock;
                    menuItem.ToolTip = toolTip;
                }
            }
            menuItem.Click += clickHandler;
            return menuItem;
        }

        /// <summary>
        /// Creates ContextMenu for the button of the game
        /// </summary>
        /// <param name="button">Button of the game</param>
        /// <param name="game">Game itself</param>
        private ContextMenu CreateContextMenu(Game game)
        {
            // Create new Context Menu
            ContextMenu contextMenu = new ContextMenu();

            // Launch options
            MenuItem launchOptions = new MenuItem { Header = "Launch" };
            // Add "Launch games in Windowed mode" option
            launchOptions.Items.Add(CreateMenuItem("Launch in Windowed Mode", "Start the game in windowed mode", async (sender, e) =>
            {
                // Animations
                MainWindow mainWindow = Application.Current.MainWindow as MainWindow;
                DoubleAnimation fadeOutAnimation = new DoubleAnimation(1, 0, TimeSpan.FromSeconds(0.15));
                DoubleAnimation fadeInAnimation = new DoubleAnimation(0, 1, TimeSpan.FromSeconds(0.15));
                TaskCompletionSource<bool> animationCompleted = new TaskCompletionSource<bool>();
                animationCompleted = new TaskCompletionSource<bool>();
                fadeOutAnimation.Completed += (s, e) =>
                {
                    mainWindow.Visibility = Visibility.Collapsed; // Collapse the main window
                    animationCompleted.SetResult(true); // Signal that the animation has completed
                };
                mainWindow.BeginAnimation(Window.OpacityProperty, fadeOutAnimation);
                await animationCompleted.Task; // Wait for animation to be completed

                // Launch the game
                GameManager.LaunchGame(game);
                mainWindow.Visibility = Visibility.Visible;
                mainWindow.BeginAnimation(Window.OpacityProperty, fadeInAnimation);

                // Save changes (Play time)
                GameManager.Save();

                // When the user closes the game/emulator, reload the UI
                Library.LoadGames();
            }));
            // Add "Launch game's emulator" option
            launchOptions.Items.Add(CreateMenuItem("Launch Xenia", "Start the Xenia emulator that the game uses", async (sender, e) =>
            {
                // Animations
                MainWindow mainWindow = Application.Current.MainWindow as MainWindow;
                DoubleAnimation fadeOutAnimation = new DoubleAnimation(1, 0, TimeSpan.FromSeconds(0.15));
                DoubleAnimation fadeInAnimation = new DoubleAnimation(0, 1, TimeSpan.FromSeconds(0.15));
                TaskCompletionSource<bool> animationCompleted = new TaskCompletionSource<bool>();
                animationCompleted = new TaskCompletionSource<bool>();
                fadeOutAnimation.Completed += (s, e) =>
                {
                    mainWindow.Visibility = Visibility.Collapsed; // Collapse the main window
                    animationCompleted.SetResult(true); // Signal that the animation has completed
                };
                mainWindow.BeginAnimation(Window.OpacityProperty, fadeOutAnimation);
                await animationCompleted.Task; // Wait for animation to be completed

                // Launch the emulator
                await GameManager.LaunchEmulator(game);
                mainWindow.Visibility = Visibility.Visible;
                mainWindow.BeginAnimation(Window.OpacityProperty, fadeInAnimation);

                // Save changes (Play time)
                GameManager.Save();

                // When the user closes the game/emulator, reload the UI
                Library.LoadGames();
            }));
            contextMenu.Items.Add(launchOptions);

            // Check if emulator version is not custom
            if (game.EmulatorVersion != EmulatorVersion.Custom)
            {
                // Content options
                MenuItem contentOptions = new MenuItem { Header = "Content" };
                // Add "Install Content" option
                contentOptions.Items.Add(CreateMenuItem("Install DLC/Updates", "Install various game content like DLC, Title Updates etc.", async (sender, e) =>
                {
                    Log.Information("Install content");
                    InstallContent installContent = new InstallContent(game);
                    installContent.ShowDialog();
                    await installContent.WaitForCloseAsync();
                }));
                // Add "View installed content" option
                contentOptions.Items.Add(CreateMenuItem("View Installed Content", "Allows the user to see what's installed in game content folder and to export save files", (sender, e) =>
                {
                    Log.Information("Opening 'ShowInstalledContent' window");
                }));

                contextMenu.Items.Add(contentOptions); // Add "Content" options to the main ContextMenu

                // "Patches" option
                MenuItem patchOptions = new MenuItem { Header = "Patches" };
                // Check if the game has any game patches installed
                if (game.FileLocations.PatchFilePath == null)
                {
                    // Add "Download Patches" option
                    patchOptions.Items.Add(CreateMenuItem("Download Patches", "Downloads and installs a patch file from the game-patches repository", async (sender, e) =>
                    {
                        Log.Information("Show window for installing game patches");
                        SelectGamePatch selectGamePatch = new SelectGamePatch(game);
                        selectGamePatch.ShowDialog();
                        await selectGamePatch.WaitForCloseAsync();
                        Library.LoadGames(); // Reload UI
                    }));
                }
                else
                {
                    // Add "Add additional patches" option

                    // Add "Manage Patches" option
                    patchOptions.Items.Add(CreateMenuItem("Manage Patches", "Enable or disable game patches", async (sender, e) =>
                    {
                        // Opens GamePatchSettings window
                        Log.Information("Opening window for enabling/disabling game patches");
                        GamePatchSettings gamePatchSettings = new GamePatchSettings(game.Title, game.FileLocations.PatchFilePath);
                        gamePatchSettings.ShowDialog();
                        await gamePatchSettings.WaitForCloseAsync();
                    }));
                    // Add "Remove Patches" option
                    patchOptions.Items.Add(CreateMenuItem("Remove Patches", "Allows the user to remove the game patch from Xenia", (sender, e) =>
                    {
                        MessageBoxResult result = MessageBox.Show($"Do you want to remove {game.Title} patch?", "Confirmation", MessageBoxButton.YesNo);
                        if (result == MessageBoxResult.Yes)
                        {
                            GameManager.RemoveGamePatch(game);
                            Library.LoadGames(); // Reload UI
                        }
                    }));
                }

                contextMenu.Items.Add(patchOptions); // Add "Game Patch" options to the main ContextMenu
            }

            // Shortcut options
            MenuItem shortcutOptions = new MenuItem { Header = "Shortcut" };
            // "Add Create Desktop Shortcut" option
            shortcutOptions.Items.Add(CreateMenuItem("Create Desktop Shortcut", null, (sender, e) =>
            {
                // Grab icon location
                string iconLocation;
                if (game.Artwork.Icon != null)
                {
                    iconLocation = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, game.Artwork.Icon);
                }
                else
                {
                    iconLocation = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, game.Artwork.Boxart);
                }

                // Grab working directory
                string workingDirectory = game.EmulatorVersion switch
                {
                    // EmulatorVersion.Stable => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ConfigurationManager.AppConfig.XeniaStable.EmulatorLocation),
                    EmulatorVersion.Canary => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ConfigurationManager.AppConfig.XeniaCanary.EmulatorLocation),
                    EmulatorVersion.Netplay => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ConfigurationManager.AppConfig.XeniaNetplay.EmulatorLocation),
                    _ => AppDomain.CurrentDomain.BaseDirectory
                };
                GameManager.CreateShortcutOnDesktop(game.Title, Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "XeniaManager.DesktopApp.exe"), workingDirectory, $@"""{game.Title}""", iconLocation);
            }));
            // TODO: Add support for adding shortcuts to Steam

            contextMenu.Items.Add(shortcutOptions);

            // Add "Open Compatibility Page" option
            if (game.GameCompatibilityURL != null)
            {
                contextMenu.Items.Add(CreateMenuItem("Check Compatibility Info", null, (sender, e) =>
                {
                    ProcessStartInfo compatibilityPageURL = new ProcessStartInfo(game.GameCompatibilityURL) { UseShellExecute = true };
                    Process.Start(compatibilityPageURL);
                }));
            }

            // Add "Remove from Xenia Manager" option
            contextMenu.Items.Add(CreateMenuItem("Remove from Xenia Manager", "Deletes the game from Xenia Manager", (sender, e) =>
            {
                MessageBoxResult result = MessageBox.Show($"Do you want to remove {game.Title}?", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result != MessageBoxResult.Yes) 
                {
                    Log.Information("Game removal cancelled");
                    return;
                }

                // Remove game from game library
                GameManager.RemoveGame(game);

                // Check if there is any content
                string GameContentFolder = game.EmulatorVersion switch
                {
                    // EmulatorVersion.Stable => $@"{ConfigurationManager.AppConfig.XeniaStable.EmulatorLocation}\content\{game.GameId}",
                    EmulatorVersion.Canary => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ConfigurationManager.AppConfig.XeniaCanary.EmulatorLocation, @$"content\{game.GameId}"),
                    EmulatorVersion.Netplay => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ConfigurationManager.AppConfig.XeniaNetplay.EmulatorLocation, @$"content\{game.GameId}"),
                    _ => ""
                };

                // Checking if game content directory exists
                if (Directory.Exists(GameContentFolder))
                {
                    // Checking if there is something in it
                    if (Directory.EnumerateFileSystemEntries(GameContentFolder).Any())
                    {
                        MessageBoxResult ContentDeletionResult = MessageBox.Show($"Do you want to remove {game.Title} content folder?\nThis will get rid of all of the installed title updates, save games etc.", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question);
                        if (ContentDeletionResult == MessageBoxResult.Yes)
                        {
                            Log.Information($"Deleting content folder of {game.Title}");
                            Directory.Delete(GameContentFolder, true);
                        }
                    }
                }

                // Remove installed game patch
                if (File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, game.FileLocations.PatchFilePath)))
                {
                    File.Delete(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, game.FileLocations.PatchFilePath));
                }

                // Reload the UI and save changes to the JSON file
                Log.Information($"Saving the new library without {game.Title}");
                Library.LoadGames();
                GameManager.Save();
            }));

            return contextMenu;
        }
    }
}
