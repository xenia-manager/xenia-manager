using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;

// Imported
using Microsoft.Win32;
using Serilog;
using XeniaManager.DesktopApp.Components.CustomControls;
using XeniaManager.DesktopApp.Windows;

namespace XeniaManager.DesktopApp.Pages
{
    /// <summary>
    /// Interaction logic for Library.xaml
    /// </summary>
    public partial class Library : Page
    {
        public Library()
        {
            InitializeComponent();
            LoadGames();
        }

        // Functions
        // Game ContextMenu
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
        private ContextMenu InitializeContextMenu(Button button, Game game)
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
                LoadGames();
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
                LoadGames();
            }));
            contextMenu.Items.Add(launchOptions);

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

        /// <summary>
        /// Loads the games into the Wrappanel
        /// </summary>
        private async Task LoadGamesIntoUI()
        {
            // Check if there are any games installed
            if (GameManager.Games == null && GameManager.Games.Count <= 0)
            {
                return;
            }

            // Sort the games by name
            IOrderedEnumerable<Game> orderedGames = GameManager.Games.OrderBy(game => game.Title);
            Mouse.OverrideCursor = Cursors.Wait;

            // Go through every game in the list
            foreach (Game game in orderedGames)
            {
                Log.Information($"Adding {game.Title} to the Library");

                // Create a new button for the game
                GameButton button = new GameButton(game, this);

                // Create ContextMenu for the game
                button.ContextMenu = InitializeContextMenu(button, game);

                // Adding game to WrapPanel
                GameLibrary.Children.Add(button);
            }

            Mouse.OverrideCursor = null;
        }

        /// <summary>
        /// Clears the WrapPanel of games and adds the games
        /// </summary>
        public async void LoadGames()
        {
            GameLibrary.Children.Clear();
            await LoadGamesIntoUI();
        }

        // Adding games into Xenia Manager
        /// <summary>
        /// Goes through every game in the array, calls the function that grabs their TitleID and MediaID and opens a new window where the user selects the game
        /// </summary>
        /// <param name="newGames">Array of game ISOs/xex files</param>
        /// <param name="emulatorVersion">Tells us what Xenia version to use for this game</param>
        private async void AddGames(string[] newGames, EmulatorVersion xeniaVersion)
        {
            // Go through every game in the array
            foreach (string gamePath in newGames)
            {
                Log.Information($"File Name: {Path.GetFileName(gamePath)}");
                (string gameTitle, string gameId, string mediaId) = GameManager.GetGameDetails(gamePath, xeniaVersion); // Get Title, TitleID and MediaID
                Log.Information($"Title: {gameTitle}, Game ID: {gameId}, Media ID: {mediaId}");
                SelectGame selectGame = new SelectGame(this, gameTitle, gameId, mediaId, gamePath, xeniaVersion);
                selectGame.Show();
                await selectGame.WaitForCloseAsync();
            }
            LoadGames();
        }

        // UI Interactions
        /// <summary>
        /// Opens FileDialog where user selects the game/games they want to add to Xenia Manager
        /// </summary>
        private void AddGame_Click(object sender, RoutedEventArgs e)
        {
            Log.Information("Opening file dialog");
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Title = "Select a game";
            openFileDialog.Filter = "All Files|*|Supported Files|*.iso;*.xex;*.zar";
            openFileDialog.Multiselect = true;
            bool? result = openFileDialog.ShowDialog();
            if (result == false)
            {
                Log.Information("Cancelling adding of games");
                return;
            }

            // Calls for the function that adds the game into Xenia Manager
            AddGames(openFileDialog.FileNames, EmulatorVersion.Canary);
        }
    }
}
