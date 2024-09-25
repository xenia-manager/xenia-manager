using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media.Animation;

// Imported
using XeniaManager.DesktopApp.Windows;

namespace XeniaManager.DesktopApp.Components.CustomControls
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
                await GameManager.LaunchGame(game);
                mainWindow.Visibility = Visibility.Visible;
                mainWindow.BeginAnimation(Window.OpacityProperty, fadeInAnimation);

                // Save changes (Play time)
                GameManager.SaveGames();

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
                GameManager.SaveGames();

                // When the user closes the game/emulator, reload the UI
                Library.LoadGames();
            }));
            contextMenu.Items.Add(launchOptions);

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
                    EmulatorVersion.Stable => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ConfigurationManager.AppConfig.XeniaStable.EmulatorLocation),
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

            return contextMenu;
        }
    }
}
