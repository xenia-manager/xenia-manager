using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

// Imported
using XeniaManager.DesktopApp.Pages;

namespace XeniaManager.DesktopApp.CustomControls
{
    /// <summary>
    /// Custom Button that has the ability to store game title and titleid
    /// </summary>
    public partial class GameButton : Button
    {
        // Custom properties
        /// <summary>
        /// Game Title
        /// </summary>
        public string GameTitle { get; set; }

        /// <summary>
        /// Game titleid
        /// </summary>
        public string GameId { get; set; }

        /// <summary>
        /// Game
        /// </summary>
        private Game Game { get; set; }

        /// <summary>
        /// Library Page where this button is used
        /// </summary>
        private Library Library { get; set; }

        /// <summary>
        /// Constructor for this custom "GameButton"
        /// </summary>
        /// <param name="game"></param>
        /// <param name="library"></param>
        public GameButton(Game game, Library library)
        {
            GameTitle = game.Title;
            GameId = game.GameId;
            this.Game = game;
            this.Library = library;
            this.Cursor = Cursors.Hand;
            this.Style = (Style)FindResource("GameCoverButtons"); // Setting up button style
            this.ToolTip = CreateTooltip(Game); // Creating tooltip for the button
            this.Content = CreateButtonContent(Game); // Using game boxart for button content
            Click += ButtonClick; // Click event
            this.ContextMenu = CreateContextMenu(Game);
        }
    }
}