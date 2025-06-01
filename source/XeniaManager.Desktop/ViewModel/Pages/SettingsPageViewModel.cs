using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using XeniaManager.Core;
using XeniaManager.Desktop.Utilities;

namespace XeniaManager.Desktop.ViewModel.Pages;

public class SettingsPageViewModel : INotifyPropertyChanged
{
    private bool _isInitializing = true;
    public ObservableCollection<CultureInfo> SupportedLanguages { get; } = new ObservableCollection<CultureInfo>(LocalizationHelper.GetSupportedLanguages());
    private CultureInfo _selectedLanguage;

    public CultureInfo SelectedLanguage
    {
        get => _selectedLanguage;
        set
        {
            if (Equals(value, _selectedLanguage))
            {
                return;
            }

            _selectedLanguage = value;
            OnPropertyChanged();

            // Update app settings when language changes
            if (value != null && !_isInitializing)
            {
                App.Settings.Ui.Language = value.Name;
                LocalizationHelper.LoadLanguage(value.Name);
                EventManager.RequestLibraryUiRefresh();
                App.AppSettings.SaveSettings();
            }
        }
    }

    public ObservableCollection<Theme> SupportedThemes { get; } = new ObservableCollection<Theme>(Enum.GetValues<Theme>().Cast<Theme>());
    private Theme _selectedTheme;

    public Theme SelectedTheme
    {
        get => _selectedTheme;
        set
        {
            if (Equals(value, _selectedTheme))
            {
                return;
            }

            _selectedTheme = value;
            OnPropertyChanged();

            // Update app settings when language changes
            if (value != null)
            {
                App.Settings.Ui.Theme = value;
                ThemeManager.ApplyTheme(value);
                App.AppSettings.SaveSettings();
            }
        }
    }

    public SettingsPageViewModel()
    {
        LoadLanguages();
        LoadThemes();
        _isInitializing = false;
    }

    private void LoadLanguages()
    {
        // Find and select current language
        CultureInfo currentLanguage = SupportedLanguages.FirstOrDefault(lang => lang.Name.Equals(App.Settings.Ui.Language, StringComparison.OrdinalIgnoreCase));

        if (currentLanguage != null)
        {
            SelectedLanguage = currentLanguage;
        }
        else
        {
            // Default to first language if current setting not found
            SelectedLanguage = SupportedLanguages.FirstOrDefault();
            if (SelectedLanguage != null)
            {
                App.Settings.Ui.Language = SelectedLanguage.Name;
            }
        }
    }

    private void LoadThemes()
    {
        if (SupportedThemes.Contains(App.Settings.Ui.Theme))
        {
            SelectedTheme = App.Settings.Ui.Theme;
        }
        else
        {
            // Default to first theme if current setting not found
            SelectedTheme = SupportedThemes.FirstOrDefault();
            if (SelectedTheme != default(Theme))
            {
                App.Settings.Ui.Theme = SelectedTheme;
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}