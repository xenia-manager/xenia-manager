using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;
using XeniaManager.Desktop.Utilities;

namespace XeniaManager.Desktop.ViewModel.Pages;

public class LibraryPageViewModel : INotifyPropertyChanged
{
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

    public LibraryPageViewModel()
    {
        LoadSettings();
    }

    public void LoadSettings()
    {
        _showGameTitle = App.Settings.Ui.Library.GameTitle;
        _showCompatibilityRating = App.Settings.Ui.Library.CompatibilityRating;

        OnPropertyChanged(nameof(ShowGameTitle));
        OnPropertyChanged(nameof(ShowCompatibilityRating));
    }

    public event PropertyChangedEventHandler PropertyChanged;
    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}