using System.Windows.Controls;
using Wpf.Ui.Controls;
using XeniaManager.Core;


// Imported
using XeniaManager.Desktop.Utilities;

namespace XeniaManager.Desktop.Views.Pages;

/// <summary>
/// Interaction logic for SettingsPage.xaml
/// </summary>
public partial class SettingsPage : Page
{
    // Variables 
    private bool _isInitializing = true;

    // Constructor
    public SettingsPage()
    {
        InitializeComponent();

        // Load language into the UI
        LoadLanguages();

        // Select current theme
        LoadThemes();

        _isInitializing = false;
    }

    // Functions
    private void LoadLanguages()
    {
        // Load languages
        CmbLanguage.ItemsSource = LocalizationHelper.SupportedLanguages;

        // Select current language
        int selectedIndex = Array.FindIndex(LocalizationHelper.SupportedLanguages,
                                        lang => lang.TwoLetterISOLanguageName == App.Settings.Ui.Language);
        if (selectedIndex >= 0)
        {
            CmbLanguage.SelectedIndex = selectedIndex;
        }
        else
        {
            CmbLanguage.SelectedIndex = 0;
            if (CmbLanguage.SelectedValue is string selectedLanguageCode)
            {
                App.Settings.Ui.Language = selectedLanguageCode;
            }
        }
    }

    private void LoadThemes()
    {
        // Load themes
        CmbTheme.ItemsSource = Enum.GetValues(typeof(Theme)).Cast<Theme>();

        // Select the current theme
        if (CmbTheme.Items.Contains(App.Settings.Ui.Theme))
        {
            CmbTheme.SelectedItem = App.Settings.Ui.Theme;
        }
        else
        {
            CmbTheme.SelectedIndex = 0;
            App.Settings.Ui.Theme = (Theme)CmbTheme.SelectedItem;
        }
    }

    private void CmbLanguage_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isInitializing)
        {
            return;
        }

        if (CmbLanguage.SelectedValue is string selectedLanguageCode)
        {
            LocalizationHelper.LoadLanguage(selectedLanguageCode);
            App.Settings.Ui.Language = selectedLanguageCode;
        }
    }

    private void CmbTheme_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isInitializing)
        {
            return;
        }

        if (CmbTheme.SelectedValue is Theme selectedTheme)
        {
            ThemeManager.ApplyTheme(selectedTheme);
            App.Settings.Ui.Theme = selectedTheme;
        }
    }
}