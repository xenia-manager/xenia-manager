using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Wpf.Ui.Controls;

// Imported
using XeniaManager.Core.Game;
using XeniaManager.Desktop.Views.Pages;
using Button = Wpf.Ui.Controls.Button;

namespace XeniaManager.Desktop.Components;

public class LibraryGameButton : Button
{
    // Custom properties
    /// <summary>
    /// Game Title
    /// </summary>
    public string _gameTitle { get; set; }

    /// <summary>
    /// Game titleid
    /// </summary>
    public string _titleId { get; set; }

    /// <summary>
    /// Game
    /// </summary>
    private Game _game { get; set; }

    /// <summary>
    /// Library Page where this button is used
    /// </summary>
    private LibraryPage _library { get; set; }

    /// <summary>
    /// Constructor for this custom "GameButton"
    /// </summary>
    /// <param name="game"></param>
    /// <param name="library"></param>
    public LibraryGameButton(Game game, LibraryPage library)
    {
        _gameTitle = game.Title;
        _titleId = game.GameId;
        this._game = game;
        this._library = library;
        this.Cursor = Cursors.Hand;
        this.Style = (Style)FindResource("DefaultUiButtonStyle");
        this.BorderThickness = new Thickness(3);
        this.Margin = new Thickness(5);
        this.Content = $"{_gameTitle}\n({_titleId})";
        this.Width = 150;
        this.Height = 207;
    }
}