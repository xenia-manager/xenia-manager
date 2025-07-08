using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows.Media;

// Imported Libraries
using XeniaManager.Core;
using XeniaManager.Core.Enum;
using XeniaManager.Core.Game;
using XeniaManager.Desktop.Utilities;

namespace XeniaManager.Desktop.ViewModel.Pages;

public class LibraryPageViewModel : INotifyPropertyChanged
{
    // Library View
    private LibraryViewType _currentView = App.Settings.Ui.Library.View;

    public LibraryViewType CurrentView
    {
        get => _currentView;
        set
        {
            if (_currentView == value)
            {
                return;
            }

            _currentView = value;
            OnPropertyChanged();

            App.Settings.Ui.Library.View = _currentView;
            App.AppSettings.SaveSettings();
        }
    }

    private void ToggleView()
    {
        CurrentView = CurrentView == LibraryViewType.Grid
            ? LibraryViewType.List
            : LibraryViewType.Grid;
    }

    public ICommand ToggleViewTypeCommand { get; }

    // "Display Game Title"
    private bool _showGameTitle;

    public bool ShowGameTitle
    {
        get => _showGameTitle;
        set
        {
            if (_showGameTitle == value)
            {
                return;
            }
            _showGameTitle = value;
            OnPropertyChanged();
            App.Settings.Ui.Library.GameTitle = value;
            App.AppSettings.SaveSettings();
            EventManager.RequestLibraryUiRefresh();
        }
    }

    // "Display Compatibility Rating"
    private bool _showCompatibilityRating;

    public bool ShowCompatibilityRating
    {
        get => _showCompatibilityRating;
        set
        {
            if (_showCompatibilityRating == value)
            {
                return;
            }
            _showCompatibilityRating = value;
            OnPropertyChanged();
            App.Settings.Ui.Library.CompatibilityRating = value;
            App.AppSettings.SaveSettings();
            EventManager.RequestLibraryUiRefresh();
        }
    }

    // "Grid Library Zoom"
    private double _zoomValue;

    public double ZoomValue
    {
        get => _zoomValue;
        set
        {
            if (Math.Abs(_zoomValue - value) < 0.01) // Use small epsilon for double comparison
            {
                return;
            }
            _zoomValue = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(ZoomToolTip));
            OnPropertyChanged(nameof(ZoomTransform));
            App.Settings.Ui.Library.Zoom = value;
            App.AppSettings.SaveSettings();
        }
    }

    public string ZoomToolTip => $"{Math.Round(_zoomValue, 1)}x";

    public ScaleTransform ZoomTransform => new ScaleTransform(_zoomValue, _zoomValue);

    // Zoom constants
    public double ZoomMinimum => 0.5;
    public double ZoomMaximum => 2.0;
    public double ZoomTickFrequency => 0.1;
    // Commands
    public ICommand ZoomInCommand { get; }
    public ICommand ZoomOutCommand { get; }
    public ICommand HandleMouseWheelCommand { get; }

    public ObservableCollection<Game> Games { get; set; } = new ObservableCollection<Game>(GameManager.Games);

    public LibraryPageViewModel()
    {
        ToggleViewTypeCommand = new RelayCommand(ToggleView);
        ZoomInCommand = new RelayCommand(ZoomIn);
        ZoomOutCommand = new RelayCommand(ZoomOut);
        HandleMouseWheelCommand = new RelayCommand<MouseWheelEventArgs>(HandleMouseWheel);
        LoadSettings();
    }

    public async Task UpdateCompatibilityRatings()
    {
        if ((DateTime.Now - App.Settings.UpdateCheckChecks.CompatibilityCheck).TotalDays <= 1)
        {
            return;
        }

        Logger.Info("Updating compatibility ratings");
        try
        {
            await CompatibilityManager.UpdateCompatibility();
        }
        catch (Exception) { }
        App.Settings.UpdateCheckChecks.CompatibilityCheck = DateTime.Now;

        // Save changes
        GameManager.SaveLibrary();
        App.AppSettings.SaveSettings();
    }

    public void PrecacheGameIcons()
    {
        Task.Run(() =>
        {
            foreach (Game game in Games)
            {
                try
                {
                    ArtworkManager.CacheLoadArtwork(game.Artwork.Icon);
                }
                catch (Exception ex)
                {
                    Logger.Error($"{ex.Message}\nFull Error:\n{ex}");
                }
            }
        });
    }

    public void LoadSettings()
    {
        _showGameTitle = App.Settings.Ui.Library.GameTitle;
        _showCompatibilityRating = App.Settings.Ui.Library.CompatibilityRating;
        _zoomValue = App.Settings.Ui.Library.Zoom;
        
        OnPropertyChanged(nameof(ShowGameTitle));
        OnPropertyChanged(nameof(ShowCompatibilityRating));
        OnPropertyChanged(nameof(ZoomValue));
    }

    public void RefreshGames()
    {
        Games = new ObservableCollection<Game>(GameManager.Games);
        OnPropertyChanged(nameof(Games));
    }

    private void ZoomIn()
    {
        ZoomValue = Math.Min(ZoomValue + ZoomTickFrequency, ZoomMaximum);
    }

    private void ZoomOut()
    {
        ZoomValue = Math.Max(ZoomValue - ZoomTickFrequency, ZoomMinimum);
    }

    private void HandleMouseWheel(MouseWheelEventArgs e)
    {
        // Check if the Ctrl key is pressed
        if (Keyboard.Modifiers == ModifierKeys.Control)
        {
            // Zoom in or out based on mouse wheel direction
            if (e.Delta > 0)
            {
                ZoomIn();
            }
            else
            {
                ZoomOut();
            }

            // Mark the event as handled so it doesn't also scroll
            e.Handled = true;
        }
    }

    public event PropertyChangedEventHandler PropertyChanged;
    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}