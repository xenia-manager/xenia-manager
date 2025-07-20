using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Media.Imaging;
using XeniaManager.Core;
using XeniaManager.Core.Constants;
using XeniaManager.Core.Database;
// Imported Libraries
using XeniaManager.Core.Enum;
using XeniaManager.Core.Game;

namespace XeniaManager.Desktop.ViewModel.Windows;

class GameDetailsEditorViewModel : INotifyPropertyChanged
{
    #region Variables
    public string WindowTitle { get; set; }
    public BitmapImage? WindowIcon { get; set; }

    private Game _game;
    public Game Game
    {
        get => _game;
        set
        {
            if (value == null || value == _game)
            {
                return;
            }

            _game = value;
            OnPropertyChanged();
        }
    }

    public string TitleId { get; set; }
    public string MediaId { get; set; }

    private string _gameTitle;
    public string GameTitle
    {
        get => _gameTitle;
        set
        {
            if (value == null || value == _gameTitle)
            {
                return;
            }

            _gameTitle = value;
            OnPropertyChanged();
        }
    }

    public CompatibilityRating[] CompatibilityRatings { get; set; } = Enum.GetValues<CompatibilityRating>();

    private CompatibilityRating _selectedCompatibilityRating;
    public CompatibilityRating SelectedCompatibilityRating
    {
        get => _selectedCompatibilityRating;
        set
        {
            if (value == null || value == _selectedCompatibilityRating)
            {
                return;
            }

            _selectedCompatibilityRating = value;
            Game.Compatibility.Rating = _selectedCompatibilityRating;
            OnPropertyChanged();
        }
    }

    private string _compatibilityPageUrl;
    public string CompatibilityPageUrl
    {
        get => _compatibilityPageUrl;
        set
        {
            if (value == null || value == _compatibilityPageUrl)
            {
                return;
            }
            _compatibilityPageUrl = value;
            Game.Compatibility.Url = _compatibilityPageUrl;
            OnPropertyChanged();
        }
    }

    #endregion

    #region Constructors
    public GameDetailsEditorViewModel(Game game)
    {
        _game = game;
        TitleId = game.GameId;
        MediaId = game.MediaId;
        _gameTitle = game.Title;
        _selectedCompatibilityRating = game.Compatibility.Rating;
        _compatibilityPageUrl = game.Compatibility.Url ?? string.Empty;
        WindowTitle = $"{Game.Title} Details Editor";
        try
        {
            WindowIcon = ArtworkManager.CacheLoadArtwork(Path.Combine(DirectoryPaths.Base, Game.Artwork.Icon));
        }
        catch (Exception ex)
        {
            Logger.Error($"{ex.Message}\nFull Error:\n{ex}");
            try
            {
                WindowIcon = new BitmapImage(new Uri("pack://application:,,,/Assets/64.png", UriKind.Absolute));
            }
            catch (Exception)
            {
                WindowIcon = null;
            }
        }
    }
    #endregion

    #region Functions
    public event PropertyChangedEventHandler? PropertyChanged;
    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
    #endregion
}