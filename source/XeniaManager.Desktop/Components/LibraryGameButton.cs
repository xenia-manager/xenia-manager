using System.Windows;
using System.Windows.Input;

// Imported
using XeniaManager.Core.Game;
using XeniaManager.Desktop.Views.Pages;
using Button = Wpf.Ui.Controls.Button;

namespace XeniaManager.Desktop.Components;

public class LibraryGameButton : Button
{
    // Variables
    // Game related variables
    private string _gameTitle { get; set; }
    private string _titleId { get; set; }
    private Game _game { get; set; }
    private LibraryPage _library { get; set; }
    private Style _buttonStyle { get; set; }

    // Constructors
    public LibraryGameButton(Game game, LibraryPage library)
    {
        _gameTitle = game.Title;
        _titleId = game.GameId;
        this._game = game;
        this._library = library;
        this.Style = CreateStyle();
        this.Content = $"{_gameTitle}\n({_titleId})";
        Click += ButtonClick;
    }

    // Functions
    private Style CreateStyle()
    {
        _buttonStyle = new Style(typeof(LibraryGameButton)) { BasedOn = (Style)FindResource("DefaultUiButtonStyle") };
        _buttonStyle.Setters.Add(new Setter(BorderThicknessProperty, new Thickness(3)));
        _buttonStyle.Setters.Add(new Setter(CursorProperty, Cursors.Hand));
        _buttonStyle.Setters.Add(new Setter(MarginProperty, new Thickness(5)));
        _buttonStyle.Setters.Add(new Setter(WidthProperty, 150.0));
        _buttonStyle.Setters.Add(new Setter(HeightProperty, 207.0));
        return _buttonStyle;
    }

    private void ButtonClick(object sender, RoutedEventArgs args)
    {
        Launcher.LaunchGame(_game);
    }
}