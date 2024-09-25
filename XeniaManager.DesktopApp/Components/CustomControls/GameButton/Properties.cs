using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

// Imported
using Serilog;
using XeniaManager.DesktopApp.Pages;

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
        private Game Game { get; set; } // Game itself
        private Library Library { get; set; } // Library Page where this button is used

        // Constructor
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
        }
    }
}
