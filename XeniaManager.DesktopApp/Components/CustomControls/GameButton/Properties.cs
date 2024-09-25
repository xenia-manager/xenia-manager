using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace XeniaManager.DesktopApp.Components.CustomControls
{
    /// <summary>
    /// Custom Button that has the ability to store game title and titleid
    /// </summary>
    public partial class GameButton : Button
    {
        // Custom properties
        public string GameTitle { get; set; } // Game Title
        public string GameId { get; set; } // TitleID

        // Constructor
        public GameButton(Game game)
        {
            GameTitle = game.Title;
            GameId = game.GameId;
            this.Cursor = Cursors.Hand;
            this.Style = (Style)FindResource("GameCoverButtons"); // Setting up button style
            this.ToolTip = CreateTooltip(game); // Creating tooltip for the button
            this.Content = CreateButtonContent(game); // Using game boxart for button content
        }
    }
}
