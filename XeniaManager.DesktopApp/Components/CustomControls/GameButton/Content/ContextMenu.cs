using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media.Animation;

// Imported
using Microsoft.Win32;
using Serilog;
using XeniaManager.DesktopApp.Windows;
using XeniaManager.Downloader;

namespace XeniaManager.DesktopApp.CustomControls
{
    public partial class GameButton
    {
        /// <summary>
        /// Creates a ContextMenu Item for an option
        /// </summary>
        /// <param name="header">Text that is shown in the ContextMenu for this option</param>
        /// <param name="toolTipText">Hovered description of the option</param>
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
                    string[] parts = toolTipText.Split(["\nNOTE:"], StringSplitOptions.None);

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
        /// <param name="game">Game itself</param>
        private ContextMenu CreateContextMenu(Game game)
        {
            // Create new Context Menu
            ContextMenu contextMenu = new ContextMenu();

            // Launch options
            MenuItem launchOptions = new MenuItem { Header = "Launch" };
            // Add "Launch games in Windowed mode" option
            launchOptions.Items.Add(CreateMenuItem("Launch in Windowed Mode", "Start the game in windowed mode",
                async void (_, _) =>
                {
                    // Animations
                    MainWindow mainWindow = Application.Current.MainWindow as MainWindow;
                    DoubleAnimation fadeOutAnimation = new DoubleAnimation(1, 0, TimeSpan.FromSeconds(0.15));
                    DoubleAnimation fadeInAnimation = new DoubleAnimation(0, 1, TimeSpan.FromSeconds(0.15));
                    TaskCompletionSource<bool> animationCompleted = new TaskCompletionSource<bool>();
                    fadeOutAnimation.Completed += (_, _) =>
                    {
                        mainWindow.Visibility = Visibility.Hidden; // Collapse the main window
                        animationCompleted.SetResult(true); // Signal that the animation has completed
                    };
                    mainWindow.BeginAnimation(Window.OpacityProperty, fadeOutAnimation);
                    await animationCompleted.Task; // Wait for animation to be completed

                    // Launch the game
                    GameManager.LaunchGame(game, true);
                    mainWindow.Visibility = Visibility.Visible;
                    mainWindow.BeginAnimation(Window.OpacityProperty, fadeInAnimation);

                    // Save changes (Play time)
                    GameManager.Save();

                    // When the user closes the game/emulator, reload the UI
                    Library.LoadGames();
                }));
            // Add "Launch game's emulator" option
            launchOptions.Items.Add(CreateMenuItem("Launch Xenia", "Start the Xenia emulator that the game uses",
                async void (_, _) =>
                {
                    // Animations
                    MainWindow mainWindow = Application.Current.MainWindow as MainWindow;
                    DoubleAnimation fadeOutAnimation = new DoubleAnimation(1, 0, TimeSpan.FromSeconds(0.15));
                    DoubleAnimation fadeInAnimation = new DoubleAnimation(0, 1, TimeSpan.FromSeconds(0.15));
                    TaskCompletionSource<bool> animationCompleted = new TaskCompletionSource<bool>();
                    fadeOutAnimation.Completed += (_, _) =>
                    {
                        mainWindow.Visibility = Visibility.Hidden; // Collapse the main window
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

            if (game.EmulatorVersion == EmulatorVersion.Mousehook)
            {
                contextMenu.Items.Add(CreateMenuItem("Configure Controls",
                    "Configure key bindings used by the game in Xenia Mousehook", (_, _) =>
                    {
                        // Check if the bindings.ini has been loaded into the UI
                        if (ConfigurationManager.MousehookBindings.Bindings == null)
                        {
                            Log.Information("Loading the bindings.ini file into the app");
                            ConfigurationManager.MousehookBindings.LoadBindings(
                                Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                                    ConfigurationManager.AppConfig.XeniaMousehook.EmulatorLocation, "bindings.ini"));
                        }

                        // Try to find supported bindings by gameid
                        bool foundGame = false;
                        List<GameBinding> selectedGameKeyBindings = new List<GameBinding>();
                        foreach (GameBinding gameKeyBindings in ConfigurationManager.MousehookBindings.Bindings)
                        {
                            if (gameKeyBindings.TitleId.ToUpper() == game.GameId.ToUpper())
                            {
                                selectedGameKeyBindings.Add(gameKeyBindings);
                                foundGame = true;
                            }
                        }

                        // Check if it found something, otherwise try to search with alternativeids
                        if (!foundGame)
                        {
                            foreach (GameBinding gameKeyBindings in ConfigurationManager.MousehookBindings.Bindings)
                            {
                                if (game.AlternativeIDs.Contains(gameKeyBindings.TitleId.ToUpper()))
                                {
                                    selectedGameKeyBindings.Add(gameKeyBindings);
                                }
                            }
                        }

                        // If nothing is found, show default keybindings for non-supported games
                        if (selectedGameKeyBindings.Count == 0)
                        {
                            selectedGameKeyBindings.Add(ConfigurationManager.MousehookBindings.Bindings[0]);
                        }

                        MousehookControlsEditor mousehookControlsEditor =
                            new MousehookControlsEditor(selectedGameKeyBindings);
                        mousehookControlsEditor.ShowDialog();
                    }));
            }

            // Check if emulator version is not custom
            if (game.EmulatorVersion != EmulatorVersion.Custom)
            {
                // Content options
                MenuItem contentOptions = new MenuItem { Header = "Content" };
                // Add "Install Content" option
                contentOptions.Items.Add(CreateMenuItem("Install DLC/Updates",
                    "Install various game content like DLC, Title Updates etc.", async void (_, _) =>
                    {
                        Log.Information("Install content");
                        InstallContent installContent = new InstallContent(game);
                        installContent.ShowDialog();
                        await installContent.WaitForCloseAsync();
                    }));

                // Add "View installed content" option
                contentOptions.Items.Add(CreateMenuItem("View Installed Content",
                    "Allows the user to see what's installed in game content folder and to export save files",
                    async void (_, _) =>
                    {
                        Log.Information("Opening 'ContentViewer' window");
                        ContentViewer contentViewer = new ContentViewer(game);
                        contentViewer.ShowDialog();
                        await contentViewer.WaitForCloseAsync();
                    }));

                // Add "Open save backup" option
                contentOptions.Items.Add(CreateMenuItem("Open Save Backup",
                    "Opens the folder containing all of the save game backups", (_, _) =>
                    {
                        Log.Information("Opening folder containing all of the save game backups");
                        string backupFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Backup", game.Title);
                        if (!Directory.Exists(backupFolder))
                        {
                            Log.Information("Couldn't find the backup folder");
                            MessageBox.Show("This game has no backups");
                            return;
                        }

                        Process.Start(new ProcessStartInfo
                        {
                            FileName = backupFolder,
                            UseShellExecute = true,
                            Verb = "open"
                        });
                    }));

                contextMenu.Items.Add(contentOptions); // Add "Content" options to the main ContextMenu

                // "Patches" option
                MenuItem patchOptions = new MenuItem { Header = "Patches" };
                // Check if the game has any game patches installed
                if (game.FileLocations.PatchFilePath == null)
                {
                    // Add "Install Patches" option
                    patchOptions.Items.Add(CreateMenuItem("Install Patches",
                        "Allows the user to install locally available patch file", (_, _) =>
                        {
                            Log.Information("Opening file dialog");
                            OpenFileDialog openFileDialog = new OpenFileDialog
                            {
                                Title = "Select a patch file",
                                Filter = "Supported Files|*.toml|All Files|*",
                                Multiselect = false
                            };
                            if (openFileDialog.ShowDialog() != true)
                            {
                                return;
                            }

                            GameManager.InstallLocalPatch(game,
                                openFileDialog.FileName); // Install the local patch file
                            Library.LoadGames(); // Reload UI
                            MessageBox.Show($"{game.Title} patch has been installed");
                        }));

                    // Add "Download Patches" option
                    patchOptions.Items.Add(CreateMenuItem("Download Patches",
                        "Downloads and installs a patch file from the game-patches repository", async void (_, _) =>
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
                    patchOptions.Items.Add(CreateMenuItem("Install additional patches",
                        "Add additional patches to the existing patch file from another local file\nNOTE: Useful if you have a patch file that is not in game-patches repository",
                        (_, _) =>
                        {
                            Log.Information("Opening file dialog");
                            OpenFileDialog openFileDialog = new OpenFileDialog
                            {
                                Title = "Select a patch file",
                                Filter = "Supported Files|*.toml|All Files|*",
                                Multiselect = true
                            };
                            if (openFileDialog.ShowDialog() != true)
                            {
                                return;
                            }

                            foreach (string file in openFileDialog.FileNames)
                            {
                                GameManager.AddAdditionalPatches(game.FileLocations.PatchFilePath, file);
                            }
                        }));

                    // Add "Manage Patches" option
                    patchOptions.Items.Add(CreateMenuItem("Manage Patches", "Enable or disable game patches",
                        async void (_, _) =>
                        {
                            // Opens GamePatchSettings window
                            Log.Information("Opening window for enabling/disabling game patches");
                            GamePatchSettings gamePatchSettings =
                                new GamePatchSettings(game.Title, game.FileLocations.PatchFilePath);
                            gamePatchSettings.ShowDialog();
                            await gamePatchSettings.WaitForCloseAsync();
                        }));

                    // Add "Update Patches" option
                    patchOptions.Items.Add(CreateMenuItem("Update Patches",
                        "Allows the user to update the currently installed patches to the latest version\nNOTE: This will disable all of the enabled patches",
                        async void (_, _) =>
                        {
                            try
                            {
                                // Downloading the patch file
                                string patchUrl =
                                    @$"https://raw.githubusercontent.com/xenia-canary/game-patches/main/patches/{Path.GetFileName(game.FileLocations.PatchFilePath)}";
                                Log.Information($"Patch URL: {patchUrl}");
                                Log.Information(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                                    game.FileLocations.PatchFilePath));
                                await DownloadManager.DownloadFileAsync(patchUrl,
                                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                                        game.FileLocations.PatchFilePath));

                                Log.Information($"{game.Title} patch has been updated");
                                MessageBox.Show($"{game.Title} patch has been updated");
                            }
                            catch (Exception ex)
                            {
                                Log.Error($"An error occurred: {ex.Message}");
                            }
                        }));

                    // Add "Remove Patches" option
                    patchOptions.Items.Add(CreateMenuItem("Remove Patches",
                        "Allows the user to remove the game patch from Xenia", (_, _) =>
                        {
                            MessageBoxResult result = MessageBox.Show($"Do you want to remove {game.Title} patch?",
                                "Confirmation", MessageBoxButton.YesNo);
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
            shortcutOptions.Items.Add(CreateMenuItem("Create Desktop Shortcut", null, (_, _) =>
            {
                // Grab icon location
                string iconLocation = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, game.Artwork.Icon ?? game.Artwork.Boxart);

                // Grab working directory
                string workingDirectory = game.EmulatorVersion switch
                {
                    EmulatorVersion.Canary => Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                        ConfigurationManager.AppConfig.XeniaCanary.EmulatorLocation),
                    EmulatorVersion.Mousehook => Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                        ConfigurationManager.AppConfig.XeniaMousehook.EmulatorLocation),
                    EmulatorVersion.Netplay => Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                        ConfigurationManager.AppConfig.XeniaNetplay.EmulatorLocation),
                    _ => AppDomain.CurrentDomain.BaseDirectory
                };
                GameManager.Shortcut.DesktopShortcut(game.Title,
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "XeniaManager.DesktopApp.exe"),
                    workingDirectory, $@"""{game.Title}""", iconLocation);
            }));
            // TODO: Add support for adding shortcuts to Steam

            contextMenu.Items.Add(shortcutOptions);

            // Moving game files/switching emulator versions
            MenuItem changeGameOptions = new MenuItem { Header = "Emulator & Game Path" };

            // Add "Change Game Location" option
            changeGameOptions.Items.Add(CreateMenuItem("Change Game Location",
                "Allows the user to change the game location", (_, _) =>
                {
                    // Open file dialog
                    Log.Information("Open file dialog");
                    OpenFileDialog openFileDialog = new OpenFileDialog
                    {
                        Title = "Select a game",
                        Filter = "All Files|*|Supported Files|*.iso;*.xex;*.zar",
                        Multiselect = false
                    };
                    if (openFileDialog.ShowDialog() == false)
                    {
                        return;
                    }

                    game.FileLocations.GameFilePath =
                        openFileDialog.FileName; // Change the game file path to the new one
                    GameManager.Save(); // Save Changes
                    Library.LoadGames(); // Reload UI
                }));

            // "Switch to Xenia Canary" option
            MenuItem switchXeniaCanary = CreateMenuItem("Switch to Xenia Canary",
                "Changes the Xenia version used by the game to Xenia Canary", (_, _) =>
                {
                    string sourceEmulatorLocation = game.EmulatorVersion switch
                    {
                        EmulatorVersion.Canary => ConfigurationManager.AppConfig.XeniaCanary.EmulatorLocation,
                        EmulatorVersion.Mousehook => ConfigurationManager.AppConfig.XeniaMousehook.EmulatorLocation,
                        EmulatorVersion.Netplay => ConfigurationManager.AppConfig.XeniaNetplay.EmulatorLocation,
                        EmulatorVersion.Custom => "",
                        _ => throw new InvalidOperationException("Unexpected build type")
                    };

                    GameManager.SwitchXeniaVersion(game, game.EmulatorVersion, EmulatorVersion.Canary,
                        sourceEmulatorLocation, ConfigurationManager.AppConfig.XeniaCanary.EmulatorLocation,
                        ConfigurationManager.AppConfig.XeniaCanary.ConfigurationFileLocation);
                    Library.LoadGames(); // Reload UI
                    MessageBox.Show($"{game.Title} is now using Xenia Canary.");
                });

            // "Switch to Xenia Mousehook" option
            MenuItem switchXeniaMousehook = CreateMenuItem("Switch to Xenia Mousehook",
                "Changes the Xenia version used by the game to Xenia Mousehook", (_, _) =>
                {
                    string sourceEmulatorLocation = game.EmulatorVersion switch
                    {
                        EmulatorVersion.Canary => ConfigurationManager.AppConfig.XeniaCanary.EmulatorLocation,
                        EmulatorVersion.Mousehook => ConfigurationManager.AppConfig.XeniaMousehook.EmulatorLocation,
                        EmulatorVersion.Netplay => ConfigurationManager.AppConfig.XeniaNetplay.EmulatorLocation,
                        EmulatorVersion.Custom => "",
                        _ => throw new InvalidOperationException("Unexpected build type")
                    };

                    GameManager.SwitchXeniaVersion(game, game.EmulatorVersion, EmulatorVersion.Mousehook,
                        sourceEmulatorLocation, ConfigurationManager.AppConfig.XeniaMousehook.EmulatorLocation,
                        ConfigurationManager.AppConfig.XeniaMousehook.ConfigurationFileLocation);
                    Library.LoadGames(); // Reload UI
                    MessageBox.Show($"{game.Title} is now using Xenia Mousehook.");
                });

            // "Switch to Xenia Netplay" option
            MenuItem switchXeniaNetplay = CreateMenuItem("Switch to Xenia Netplay",
                "Changes the Xenia version used by the game to Xenia Netplay", (_, _) =>
                {
                    string sourceEmulatorLocation = game.EmulatorVersion switch
                    {
                        EmulatorVersion.Canary => ConfigurationManager.AppConfig.XeniaCanary.EmulatorLocation,
                        EmulatorVersion.Mousehook => ConfigurationManager.AppConfig.XeniaMousehook.EmulatorLocation,
                        EmulatorVersion.Netplay => ConfigurationManager.AppConfig.XeniaNetplay.EmulatorLocation,
                        EmulatorVersion.Custom => "",
                        _ => throw new InvalidOperationException("Unexpected build type")
                    };

                    GameManager.SwitchXeniaVersion(game, game.EmulatorVersion, EmulatorVersion.Netplay,
                        sourceEmulatorLocation, ConfigurationManager.AppConfig.XeniaNetplay.EmulatorLocation,
                        ConfigurationManager.AppConfig.XeniaNetplay.ConfigurationFileLocation);
                    Library.LoadGames(); // Reload UI
                    MessageBox.Show($"{game.Title} is now using Xenia Netplay.");
                });

            switch (game.EmulatorVersion)
            {
                case EmulatorVersion.Canary:
                    // Check if Xenia Netplay is installed and show the option to switch to it
                    if (ConfigurationManager.AppConfig.XeniaNetplay != null)
                    {
                        // Add "Switch to Xenia Netplay" option
                        changeGameOptions.Items.Add(switchXeniaNetplay);
                    }

                    // Check if Xenia Mousehook is installed and show the option to switch to it
                    if (ConfigurationManager.AppConfig.XeniaMousehook != null)
                    {
                        // Add "Switch to Xenia Mousehook" option
                        changeGameOptions.Items.Add(switchXeniaMousehook);
                    }

                    break;
                case EmulatorVersion.Mousehook:
                    // Check if Xenia Canary is installed and show the option to switch to it
                    if (ConfigurationManager.AppConfig.XeniaCanary != null)
                    {
                        // Add "Switch to Xenia Canary" option
                        changeGameOptions.Items.Add(switchXeniaCanary);
                    }

                    // Check if Xenia Netplay is installed and show the option to switch to it
                    if (ConfigurationManager.AppConfig.XeniaNetplay != null)
                    {
                        // Add "Switch to Xenia Netplay" option
                        changeGameOptions.Items.Add(switchXeniaNetplay);
                    }

                    break;
                case EmulatorVersion.Netplay:
                    // Check if Xenia Canary is installed and show the option to switch to it
                    if (ConfigurationManager.AppConfig.XeniaCanary != null)
                    {
                        // Add "Switch to Xenia Canary" option
                        changeGameOptions.Items.Add(switchXeniaCanary);
                    }

                    // Check if Xenia Mousehook is installed and show the option to switch to it
                    if (ConfigurationManager.AppConfig.XeniaMousehook != null)
                    {
                        // Add "Switch to Xenia Mousehook" option
                        changeGameOptions.Items.Add(switchXeniaMousehook);
                    }

                    break;
                case EmulatorVersion.Custom:
                    // Check if Xenia Canary is installed and show the option to switch to it
                    if (ConfigurationManager.AppConfig.XeniaCanary != null)
                    {
                        // Add "Switch to Xenia Canary" option
                        changeGameOptions.Items.Add(switchXeniaCanary);
                    }

                    // Check if Xenia Mousehook is installed and show the option to switch to it
                    if (ConfigurationManager.AppConfig.XeniaMousehook != null)
                    {
                        // Add "Switch to Xenia Mousehook" option
                        changeGameOptions.Items.Add(switchXeniaMousehook);
                    }

                    // Check if Xenia Netplay is installed and show the option to switch to it
                    if (ConfigurationManager.AppConfig.XeniaNetplay != null)
                    {
                        // Add "Switch to Xenia Netplay" option
                        changeGameOptions.Items.Add(switchXeniaNetplay);
                    }

                    break;
                default:
                    break;
            }

            // Add "Switch to Xenia Custom" option
            changeGameOptions.Items.Add(CreateMenuItem("Switch to Xenia Custom",
                "Changes the Xenia version used by the game to Xenia Custom", (_, _) =>
                {
                    // OpenFileDialog to select custom Xenia executable
                    OpenFileDialog openFileDialog = new OpenFileDialog
                    {
                        Title = "Select Xenia executable",
                        Filter = "Supported Files|*.exe"
                    };
                    if (openFileDialog.ShowDialog() == false)
                    {
                        return;
                    }

                    Log.Information($"Selected Xenia executable: {openFileDialog.FileName}");
                    game.EmulatorVersion = EmulatorVersion.Custom;
                    game.FileLocations.EmulatorExecutableLocation = openFileDialog.FileName;

                    // Trying to find the appropriate configuration file next to the emulator executable
                    Log.Information("Trying to find the configuration file");
                    string[] configurationFiles =
                        Directory.GetFiles(Path.GetDirectoryName(openFileDialog.FileName), "*.config.toml");
                    switch (configurationFiles.Length)
                    {
                        case 1:
                            Log.Information($"Found configuration file: {configurationFiles[0]}");
                            game.FileLocations.ConfigFilePath = configurationFiles[0];
                            break;
                        default:
                            // In case it can't find it, ask user to find it himself
                            Log.Information($"Couldn't find a configuration file");
                            OpenFileDialog customXeniaConfigurationSelector = new OpenFileDialog
                            {
                                Title = "Select Xenia configuration file",
                                Filter = "Supported Files|*.toml"
                            };
                            if (customXeniaConfigurationSelector.ShowDialog() == true)
                            {
                                Log.Information(
                                    $"Selected configuration file: {customXeniaConfigurationSelector.FileName}");
                                game.FileLocations.ConfigFilePath = customXeniaConfigurationSelector.FileName;
                            }
                            else
                            {
                                game.FileLocations.ConfigFilePath = null;
                            }

                            break;
                    }

                    Library.LoadGames(); // Reload UI
                    MessageBox.Show($"{game.Title} is now using Xenia Custom.");
                }));

            contextMenu.Items.Add(changeGameOptions);

            // Add "Open Compatibility Page" option
            if (game.GameCompatibilityUrl != null)
            {
                contextMenu.Items.Add(CreateMenuItem("Check Compatibility Info", null, (_, _) =>
                {
                    ProcessStartInfo compatibilityPageUrl = new ProcessStartInfo(game.GameCompatibilityUrl)
                        { UseShellExecute = true };
                    Process.Start(compatibilityPageUrl);
                }));
            }

            // Add "Game Details" option
            contextMenu.Items.Add(CreateMenuItem("Game Details", "Opens a window where you can edit game name and icon",
                async void (_, _) =>
                {
                    Log.Information("Showing 'GameDetails' window");
                    GameDetails gameDetails = new GameDetails(game);
                    gameDetails.ShowDialog();
                    await gameDetails.WaitForCloseAsync();
                    GameManager.Save(); // Save changes to the games file
                    Library.LoadGames(); // Reload UI
                }));

            // Add "Remove from Xenia Manager" option
            contextMenu.Items.Add(CreateMenuItem("Remove from Xenia Manager", "Deletes the game from Xenia Manager",
                (_, _) =>
                {
                    MessageBoxResult result = MessageBox.Show($"Do you want to remove {game.Title}?", "Confirmation",
                        MessageBoxButton.YesNo, MessageBoxImage.Question);
                    if (result != MessageBoxResult.Yes)
                    {
                        Log.Information("Game removal cancelled");
                        return;
                    }

                    // Remove game from game library
                    GameManager.RemoveGame(game);

                    // Check if there is any content
                    string gameContentFolder = game.EmulatorVersion switch
                    {
                        EmulatorVersion.Canary => Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                            ConfigurationManager.AppConfig.XeniaCanary.EmulatorLocation, @$"content\{game.GameId}"),
                        EmulatorVersion.Mousehook => Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                            ConfigurationManager.AppConfig.XeniaMousehook.EmulatorLocation, @$"content\{game.GameId}"),
                        EmulatorVersion.Netplay => Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                            ConfigurationManager.AppConfig.XeniaNetplay.EmulatorLocation, @$"content\{game.GameId}"),
                        _ => ""
                    };

                    // Checking if game content directory exists
                    if (Directory.Exists(gameContentFolder))
                    {
                        // Checking if there is something in it
                        if (Directory.EnumerateFileSystemEntries(gameContentFolder).Any())
                        {
                            MessageBoxResult contentDeletionResult = MessageBox.Show(
                                $"Do you want to remove {game.Title} content folder?\nThis will get rid of all of the installed title updates, save games etc.",
                                "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question);
                            if (contentDeletionResult == MessageBoxResult.Yes)
                            {
                                Log.Information($"Deleting content folder of {game.Title}");
                                Directory.Delete(gameContentFolder, true);
                            }
                        }
                    }

                    // Remove installed game patch
                    if (game.FileLocations.PatchFilePath != null && File.Exists(
                            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, game.FileLocations.PatchFilePath)))
                    {
                        File.Delete(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                            game.FileLocations.PatchFilePath));
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