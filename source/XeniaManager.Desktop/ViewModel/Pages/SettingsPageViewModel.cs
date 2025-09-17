using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics.Tracing;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows.Media;

// Imported Libraries
using XeniaManager.Core.Enum;
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
                EventManager.RequestXeniaSettingsRefresh();
                App.AppSettings.SaveSettings();
            }
        }
    }

    public ObservableCollection<Theme> SupportedThemes { get; } = new ObservableCollection<Theme>(Enum.GetValues<Theme>().Cast<Theme>());
    private Theme _selectedTheme = App.Settings.Ui.Theme;

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

            // Update app settings when theme changes
            if (App.Settings.Ui.Theme != _selectedTheme)
            {
                App.Settings.Ui.Theme = _selectedTheme;
                ThemeManager.ApplyTheme(_selectedTheme);
                App.AppSettings.SaveSettings();
            }
        }
    }

    public class NamedColor
    {
        public string Name { get; set; }
        public Color Color { get; set; }
    }

    public ObservableCollection<NamedColor> SupportedColors { get; } = new ObservableCollection<NamedColor>();
    private Color _selectedAccentColor = App.Settings.Ui.AccentColor;

    public Color SelectedAccentColor
    {
        get => _selectedAccentColor;
        set
        {
            if (Equals(value, _selectedAccentColor))
            {
                return;
            }

            _selectedAccentColor = value;
            OnPropertyChanged();

            // Update app settings when accent color changes
            if (App.Settings.Ui.AccentColor != _selectedAccentColor)
            {
                App.Settings.Ui.AccentColor = _selectedAccentColor;
                ThemeManager.ApplyAccent(_selectedAccentColor);
                App.AppSettings.SaveSettings();
            }
        }
    }

    private bool _automaticEmulatorUpdate = App.Settings.Emulator.Settings.AutomaticallyUpdateEmulator;

    public bool AutomaticEmulatorUpdate
    {
        get => _automaticEmulatorUpdate;
        set
        {
            if (value == _automaticEmulatorUpdate)
            {
                return;
            }
            _automaticEmulatorUpdate = value;
            OnPropertyChanged();

            if (value != null)
            {
                App.Settings.Emulator.Settings.AutomaticallyUpdateEmulator = value;
                App.AppSettings.SaveSettings();
            }
        }
    }

    private bool _automaticSaveBackup = App.Settings.Emulator.Settings.Profile.AutomaticSaveBackup;

    public bool AutomaticSaveBackup
    {
        get => _automaticSaveBackup;
        set
        {
            if (value == _automaticSaveBackup)
            {
                return;
            }
            _automaticSaveBackup = value;
            OnPropertyChanged();

            if (value != null)
            {
                App.Settings.Emulator.Settings.Profile.AutomaticSaveBackup = value;
                App.AppSettings.SaveSettings();
            }
        }
    }

    public static ObservableCollection<KeyValuePair<string, string>> ProfileSlots { get; } = new ObservableCollection<KeyValuePair<string, string>>
    {
        new KeyValuePair<string, string>("Slot 1", "0"),
        new KeyValuePair<string, string>("Slot 2", "1"),
        new KeyValuePair<string, string>("Slot 3", "2"),
        new KeyValuePair<string, string>("Slot 4", "3"),
    };

    private KeyValuePair<string, string> _selectedProfileSlot = ProfileSlots.FirstOrDefault(slot => slot.Value == App.Settings.Emulator.Settings.Profile.ProfileSlot);

    public KeyValuePair<string, string> SelectedProfileSlot
    {
        get => _selectedProfileSlot;
        set
        {
            if (Equals(value, _selectedProfileSlot))
            {
                return;
            }
            _selectedProfileSlot = value;
            OnPropertyChanged();

            // Save the integer value to settings
            App.Settings.Emulator.Settings.Profile.ProfileSlot = value.Value;
            App.AppSettings.SaveSettings();
        }
    }

    private bool _showLoadingScreen = App.Settings.Ui.ShowGameLoadingBackground;

    public bool ShowLoadingScreen
    {
        get => _showLoadingScreen;
        set
        {
            if (value == _showLoadingScreen)
            {
                return;
            }
            _showLoadingScreen = value;
            OnPropertyChanged();
            
            if (value != null)
            {
                App.Settings.Ui.ShowGameLoadingBackground = value;
                App.AppSettings.SaveSettings();
            }
        }
    }

    private bool _doubleClickOpen = App.Settings.Ui.Library.DoubleClickToOpenGame;
    
    public bool DoubleClickOpen
    {
        get => _doubleClickOpen;
        set
        {
            if (value == _doubleClickOpen)
            {
                return;
            }

            _doubleClickOpen = value;
            OnPropertyChanged();

            App.Settings.Ui.Library.DoubleClickToOpenGame = value;
            App.AppSettings.SaveSettings();
            EventManager.RequestLibraryUiRefresh();
        }
    }

    public SettingsPageViewModel()
    {
        LoadLanguages();
        LoadAllColors();
        _isInitializing = false;
    }

    private void LoadLanguages()
    {
        // Find and select current language
        CultureInfo? currentLanguage = SupportedLanguages.FirstOrDefault(lang => lang.Name.Equals(App.Settings.Ui.Language, StringComparison.OrdinalIgnoreCase));

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

    private void LoadAllColors()
    {
        PropertyInfo[] colorProperties = typeof(Colors).GetProperties(BindingFlags.Public | BindingFlags.Static);

        foreach (var prop in colorProperties)
        {
            if (prop.PropertyType == typeof(Color))
            {
                if (prop.GetValue(null) is Color color)
                {
                    SupportedColors.Add(new NamedColor
                    {
                        Name = prop.Name,
                        Color = color
                    });
                }
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}