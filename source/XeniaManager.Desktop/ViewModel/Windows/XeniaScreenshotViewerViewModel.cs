using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Media;
using System.Windows.Media.Imaging;

// Imported Libraries
using XeniaManager.Core;
using XeniaManager.Core.Constants;
using XeniaManager.Core.Constants.Emulators;
using XeniaManager.Core.Enum;
using XeniaManager.Core.Game;

namespace XeniaManager.Desktop.ViewModel.Windows;

public class Screenshot
{
    public string FilePath { get; set; }
    public string FileName { get; set; }
    public BitmapImage Thumbnail { get; set; }
}

public class XeniaScreenshotViewerViewModel : INotifyPropertyChanged
{
    #region Variables

    private string _screenshotDirectory;
    private Game _game;
    private string _windowTitle;

    public string WindowTitle
    {
        get => _windowTitle;
        set
        {
            _windowTitle = value;
            OnPropertyChanged();
        }
    }
    
    private ImageSource _windowIcon;

    public ImageSource WindowIcon
    {
        get => _windowIcon;
        set
        {
            _windowIcon = value;
            OnPropertyChanged();
        }
    }

    private string[] _supportedExtensions = { ".png" };
    public ObservableCollection<Screenshot> GameScreenshots { get; set; } = new ObservableCollection<Screenshot>();

    #endregion

    #region Constructors

    public XeniaScreenshotViewerViewModel(Game game)
    {
        _game = game;
        WindowTitle = $"{_game.Title} Screenshots";
        try
        {
            _windowIcon = ArtworkManager.CacheLoadArtwork(Path.Combine(DirectoryPaths.Base, _game.Artwork.Icon));
        }
        catch (Exception ex)
        {
            Logger.Error($"{ex.Message}\nFull Error:\n{ex}");
            try
            {
                _windowIcon = new BitmapImage(new Uri("pack://application:,,,/Assets/64.png", UriKind.Absolute));
            }
            catch (Exception)
            {
                _windowIcon = null;
            }
        }
        _screenshotDirectory = _game.XeniaVersion switch
        {
            XeniaVersion.Canary => Path.Combine(DirectoryPaths.Base, XeniaCanary.ScreenshotsFolderLocation, _game.GameId),
            XeniaVersion.Mousehook => Path.Combine(DirectoryPaths.Base, XeniaMousehook.ScreenshotsFolderLocation, _game.GameId),
            XeniaVersion.Netplay => Path.Combine(DirectoryPaths.Base, XeniaNetplay.ScreenshotsFolderLocation, _game.GameId),
            XeniaVersion.Custom => throw new NotSupportedException($"Unsupported version: {_game.XeniaVersion}"),
            _ => throw new NotSupportedException($"Unsupported version: {_game.XeniaVersion}")
        };

        LoadGameScreenshots();
    }

    #endregion

    #region Functions

    public void LoadGameScreenshots()
    {
        try
        {
            if (!Directory.Exists(_screenshotDirectory))
            {
                Logger.Error("Game Screenshots directory doesn't exist");
                return;
            }
            Logger.Info($"Loading game screenshots for {_game.Title}");
            GameScreenshots.Clear();
            string[] imageFiles = Directory.GetFiles(_screenshotDirectory)
                .Where(file => _supportedExtensions.Contains(Path.GetExtension(file).ToLower())).ToArray();

            foreach (string imageFile in imageFiles)
            {
                try
                {
                    BitmapImage bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(imageFile);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();
                    bitmap.Freeze();
                    GameScreenshots.Add(new Screenshot()
                    {
                        FileName = Path.GetFileName(imageFile),
                        FilePath = imageFile,
                        Thumbnail = bitmap
                    });
                    Logger.Debug($"Added {GameScreenshots.Last().FileName} to gallery");
                }
                catch (Exception e)
                {
                    Logger.Error($"Failed loading {imageFile}: {e.Message}\n{e}");
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"{ex.Message}\nFull Error:\n{ex}");
        }
    }
    public event PropertyChangedEventHandler? PropertyChanged;
    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    #endregion
}