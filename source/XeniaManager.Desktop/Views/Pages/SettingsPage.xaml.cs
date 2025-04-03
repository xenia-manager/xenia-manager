using System.Windows.Controls;

// Imported
using XeniaManager.Core;
using XeniaManager.Desktop.Utilities;

namespace XeniaManager.Desktop.Views.Pages;

/// <summary>
/// Interaction logic for SettingsPage.xaml
/// </summary>
public partial class SettingsPage : Page
{
    // Variables 
    /// <summary>
    /// Simple check so SelectionChanges events don't trigger on loading of this page
    /// </summary>
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
    /// <summary>
    /// Loads the supported languages into the Language setting
    /// </summary>
    private void LoadLanguages()
    {
        // Load languages
        CmbLanguage.ItemsSource = LocalizationHelper.GetSupportedLanguages();

        // Select current language
        int selectedIndex = LocalizationHelper.GetSupportedLanguages()
            .FindIndex(lang => lang.TwoLetterISOLanguageName.Equals(App.Settings.Ui.Language, StringComparison.OrdinalIgnoreCase));
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

    /// <summary>
    /// Loads the supported themes into the Theme setting
    /// </summary>
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

    /// <summary>
    /// Applies the selected Langauge to Xenia Manager UI
    /// </summary>
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
            LoadLanguages();
        }
    }

    /// <summary>
    /// Applies the selected theme to Xenia Manager UI
    /// </summary>
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