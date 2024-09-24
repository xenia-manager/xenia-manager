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

        // Loading of games into UI
        /// <summary>
        /// Checks if the game icon is cached
        /// <para>If the game icon is not cached, it'll cache it</para>
        /// </summary>
        /// <param name="game">Game</param>
        /// <returns >BitmapImage - cached game icon</returns>
        private async Task<BitmapImage> LoadOrCacheIcon(Game game)
        {
            await Task.Delay(1);
            string iconFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, game.Artwork.Boxart); // Path to the game icon
            string cacheDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Cache\"); // Path to the cached directory

            // Tries to find cached icon
            game.ArtworkCache.Boxart = GameManager.Caching.FindFirstIdenticalFile(iconFilePath, cacheDirectory);
            if (game.ArtworkCache.Boxart != null)
            {
                // If there is a cached icon, return it
                Log.Information("Icon has already been cached");
                return new BitmapImage(new Uri(game.ArtworkCache.Boxart));
            }

            // If there's no cached icon, create a cached version and return it
            Log.Information("Creating new cached icon for the game");
            string randomIconName = Path.GetRandomFileName().Replace(".", "").Substring(0, 8) + ".ico";
            game.ArtworkCache.Boxart = Path.Combine(cacheDirectory, randomIconName);

            File.Copy(iconFilePath, game.ArtworkCache.Boxart, true);
            Log.Information($"Cached icon name: {randomIconName}");

            return new BitmapImage(new Uri(game.ArtworkCache.Boxart));
        }

        /// <summary>
        /// Creates image for the game button
        /// </summary>
        /// <param name="game">Game itself</param>
        /// <returns>Border - Content of the game button</returns>
        private async Task<Border> CreateButtonContent(Game game)
        {
            // Cached game icon
            BitmapImage iconImage = await LoadOrCacheIcon(game);
            Image gameImage = new Image
            {
                Source = iconImage,
                Stretch = Stretch.UniformToFill
            };

            // Create a Grid to hold both the game image and the overlay symbol
            Grid contentGrid = new Grid();

            // Add the game image to the grid
            contentGrid.Children.Add(gameImage);

            // Compatibility Rating
            Border CompatibilityRatingImage = new Border
            {
                Width = 22, // Width of the emoji
                Height = 22, // Height of the emoji
                Background = Brushes.White,
                BorderBrush = Brushes.Black,
                BorderThickness = new Thickness(1),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(1, 1, 0, 0),
                CornerRadius = new CornerRadius(16)
            };

            Image CompatibilityRating = new Image
            {
                Width = 20, // Width of the emoji
                Height = 20, // Height of the emoji
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            /*
            switch (game.CompatibilityRating)
            {
                case "Unplayable":
                    CompatibilityRating.Source = new BitmapImage(new Uri("pack://application:,,,/Xenia Manager;component/Assets/Compatibility Icons/Unplayable.png"));
                    break;
                case "Loads":
                    CompatibilityRating.Source = new BitmapImage(new Uri("pack://application:,,,/Xenia Manager;component/Assets/Compatibility Icons/Loads.png"));
                    break;
                case "Gameplay":
                    CompatibilityRating.Source = new BitmapImage(new Uri("pack://application:,,,/Xenia Manager;component/Assets/Compatibility Icons/Gameplay.png"));
                    break;
                case "Playable":
                    CompatibilityRating.Source = new BitmapImage(new Uri("pack://application:,,,/Xenia Manager;component/Assets/Compatibility Icons/Playable.png"));
                    break;
                default:
                    CompatibilityRating.Source = new BitmapImage(new Uri("pack://application:,,,/Xenia Manager;component/Assets/Compatibility Icons/Unknown.png"));
                    break;
            }
            
            // Add the compatibility rating to the grid
            CompatibilityRatingImage.Child = CompatibilityRating;
            contentGrid.Children.Add(CompatibilityRatingImage);*/

            // Rounded edges of the game boxart
            RectangleGeometry clipGeometry = new RectangleGeometry
            {
                Rect = new Rect(0, 0, 150, 207),
                RadiusX = 3,
                RadiusY = 3
            };

            // Game button content with rounded corners
            return new Border
            {
                CornerRadius = new CornerRadius(10),
                Background = Brushes.Black,
                Child = contentGrid,
                Clip = clipGeometry
            };
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
                Button button = new Button();

                // Creating image for the game button
                button.Content = await CreateButtonContent(game);

                // When user clicks on the game, launch the game
                button.Click += async (sender, e) =>
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
                };

                // When button loads, create ContextMenu for it
                button.Loaded += (sender, e) =>
                {
                    button.ContextMenu = InitializeContextMenu(button, game);
                };

                button.Cursor = Cursors.Hand; // Change cursor to hand cursor
                button.Style = (Style)FindResource("GameCoverButtons"); // Styling of the game button

                // Tooltip
                TextBlock tooltip = new TextBlock { TextAlignment = TextAlignment.Center };
                tooltip.Inlines.Add(new Run(game.Title + "\n") { FontWeight = FontWeights.Bold }); // Adding game title to tooltip

                // Adding compatibility rating to the tooltip
                tooltip.Inlines.Add(new Run($"{game.CompatibilityRating}") { FontWeight = FontWeights.Bold, TextDecorations = TextDecorations.Underline });
                switch (game.CompatibilityRating)
                {
                    case CompatibilityRating.Unplayable:
                        tooltip.Inlines.Add(new Run(" (The game either doesn't start or it crashes a lot)"));
                        break;
                    case CompatibilityRating.Loads:
                        tooltip.Inlines.Add(new Run(" (The game loads, but crashes in the title screen or main menu)"));
                        break;
                    case CompatibilityRating.Gameplay:
                        tooltip.Inlines.Add(new Run(" (Gameplay loads, but it may be unplayable)"));
                        break;
                    case CompatibilityRating.Playable:
                        tooltip.Inlines.Add(new Run(" (The game can be reasonably played from start to finish with little to no issues)"));
                        break;
                    default:
                        break;
                }

                // Adding playtime to the tooltip
                if (game.Playtime != null)
                {
                    string FormattedPlaytime = "";
                    if (game.Playtime == 0)
                    {
                        FormattedPlaytime = "Never played";
                    }
                    else if (game.Playtime < 60)
                    {
                        FormattedPlaytime = $"{game.Playtime:N0} minutes";
                    }
                    else
                    {
                        FormattedPlaytime = $"{(game.Playtime / 60):N1} hours";
                    }
                    tooltip.Inlines.Add(new Run("\n" + "Time played:") { FontWeight = FontWeights.Bold, TextDecorations = TextDecorations.Underline });
                    tooltip.Inlines.Add(new Run($" {FormattedPlaytime}"));
                }
                else
                {
                    tooltip.Inlines.Add(new Run("\n" + "Time played:") { FontWeight = FontWeights.Bold, TextDecorations = TextDecorations.Underline });
                    tooltip.Inlines.Add(new Run(" Never played"));
                }
                button.ToolTip = tooltip; // Adding the tooltip to the game button

                // Adding game to WrapPanel
                GameLibrary.Children.Add(button);
            }

            Mouse.OverrideCursor = null;
        }

        /// <summary>
        /// Clears the WrapPanel of games and adds the games
        /// </summary>
        private async void LoadGames()
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
