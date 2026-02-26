using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using XeniaManager.Core.Logging;
using XeniaManager.Core.Models;
using XeniaManager.Core.Models.Items;
using XeniaManager.Core.Settings;
using XeniaManager.Core.Utilities;
using XeniaManager.Services;

namespace XeniaManager.ViewModels.Pages;

public partial class SettingsPageViewModel : ViewModelBase
{
    // Variables
    private Settings _settings { get; set; }
    private ThemeService _themeService { get; set; }
    private bool _firstStartup = true;
    private bool _suppressUpdates = false;

    // UI Settings
    // Language settings
    public ObservableCollection<LanguageItem> AppLanguages { get; set; } = [];
    [ObservableProperty] private int selectedLanguageIndex;

    // Theme Settings
    public ObservableCollection<ThemeDisplayItem> AppThemeOptions { get; set; } =
    [
        new ThemeDisplayItem
        {
            DisplayName = LocalizationHelper.GetText("SettingsPage.Ui.Theme.Option.Light"),
            ThemeValue = Theme.Light
        },

        new ThemeDisplayItem
        {
            DisplayName = LocalizationHelper.GetText("SettingsPage.Ui.Theme.Option.Dark"),
            ThemeValue = Theme.Dark
        }
    ];

    [ObservableProperty] private Theme selectedTheme;

    public int SelectedThemeIndex
    {
        get
        {
            for (int i = 0; i < AppThemeOptions.Count; i++)
            {
                if (AppThemeOptions[i].ThemeValue == SelectedTheme)
                {
                    return i;
                }
            }
            return 0; // Default to the first theme if not found
        }
        set
        {
            if (value < 0 || value >= AppThemeOptions.Count)
            {
                return;
            }
            if (SelectedTheme == AppThemeOptions[value].ThemeValue)
            {
                return;
            }
            SelectedTheme = AppThemeOptions[value].ThemeValue;

            // Update settings when the theme changes
            _settings.Settings.Ui.Theme = SelectedTheme;
            _settings.SaveSettings();

            _themeService.SetTheme(SelectedTheme);
        }
    }

    // Constructor
    public SettingsPageViewModel()
    {
        _settings = App.Services.GetRequiredService<Settings>();
        _themeService = App.Services.GetRequiredService<ThemeService>();
        LoadUISettings();
    }

    // Functions
    private void LoadUISettings()
    {
        Logger.Debug<SettingsPageViewModel>("Starting LoadUISettings method");

        // Load supported languages & selected language
        CultureInfo[] supportedCultures = LocalizationHelper.GetSupportedLanguages();
        List<LanguageItem> languageItems = supportedCultures.Select(c => new LanguageItem(c)).ToList();
        Logger.Info<SettingsPageViewModel>($"Loaded {supportedCultures.Length} supported cultures");

        // Only recreate the collection if it's empty or different
        if (AppLanguages.Count == 0 || AppLanguages.Count != languageItems.Count)
        {
            AppLanguages = new ObservableCollection<LanguageItem>(languageItems);
            Logger.Info<SettingsPageViewModel>($"Created new ObservableCollection with {languageItems.Count} language items");
        }
        else
        {
            // Update the existing collection to maintain bindings
            AppLanguages.Clear();
            foreach (LanguageItem item in languageItems)
            {
                AppLanguages.Add(item);
            }
            Logger.Info<SettingsPageViewModel>($"Updated existing collection with {languageItems.Count} language items");
        }

        // Get the stored language from settings
        string storedLanguage = _settings.Settings.Ui.Language;
        Logger.Debug<SettingsPageViewModel>($"Retrieved stored language from settings: {storedLanguage}");

        // Find the index of the stored language in the supported languages
        SelectedLanguageIndex = AppLanguages.ToList().FindIndex(c => c.Culture.Name == storedLanguage);
        Logger.Debug<SettingsPageViewModel>($"Calculated SelectedLanguageIndex: {SelectedLanguageIndex}");

        if (SelectedLanguageIndex == -1)
        {
            // If the stored language isn't in the supported list, default to English (en)
            LanguageItem? defaultLanguageItem = AppLanguages.FirstOrDefault(c => c.Culture.Name == "en") ?? AppLanguages.FirstOrDefault();
            if (defaultLanguageItem != null)
            {
                SelectedLanguageIndex = AppLanguages.IndexOf(defaultLanguageItem);
                Logger.Warning<SettingsPageViewModel>($"SelectedLanguageIndex defaulted to: {SelectedLanguageIndex} for language: {defaultLanguageItem.Culture.Name}");
            }
            else
            {
                Logger.Error<SettingsPageViewModel>("No default language could be found, SelectedLanguageIndex remains -1");
            }
        }
        Logger.Info<SettingsPageViewModel>($"Currently selected language: {(SelectedLanguageIndex >= 0 && SelectedLanguageIndex < AppLanguages.Count ? AppLanguages[SelectedLanguageIndex].Culture.DisplayName : "Unknown")}");

        // Load theme
        SelectedTheme = _settings.Settings.Ui.Theme;

        Logger.Debug<SettingsPageViewModel>("Completed LoadUISettings method");
    }

    /// <summary>
    /// Refreshes the UI settings to reflect current settings values
    /// </summary>
    public void RefreshSettings()
    {
        Logger.Debug<SettingsPageViewModel>("Starting RefreshSettings method");

        if (_firstStartup)
        {
            Logger.Info<SettingsPageViewModel>("Skipping refresh during first startup");
            _firstStartup = false;
            return;
        }

        Logger.Debug<SettingsPageViewModel>("Suppressing updates and calling LoadUISettings");
        _suppressUpdates = true;
        LoadUISettings();
        _suppressUpdates = false;
        Logger.Debug<SettingsPageViewModel>("Updates unsuppressed after LoadUISettings call");
        Logger.Debug<SettingsPageViewModel>("Completed RefreshSettings method");
    }

    partial void OnSelectedLanguageIndexChanged(int oldValue, int newValue)
    {
        if (_suppressUpdates)
        {
            Logger.Debug<SettingsPageViewModel>("Language change suppressed, skipping update");
            return;
        }

        if (newValue < 0 || newValue >= AppLanguages.Count || newValue == oldValue)
        {
            return;
        }
        Logger.Info<SettingsPageViewModel>($"Language changed from '{AppLanguages[oldValue].Culture.DisplayName}' to '{AppLanguages[newValue].Culture.DisplayName}'");
        // Refresh localization after language change
        LocalizationHelper.LoadLanguage(AppLanguages[newValue].Culture.Name);

        // Update the settings with the selected culture name
        _settings.Settings.Ui.Language = AppLanguages[newValue].Culture.Name;
        _settings.SaveSettings();
        Logger.Info<SettingsPageViewModel>($"Settings updated with new language: {AppLanguages[newValue].Culture.Name}");
    }

    partial void OnSelectedThemeChanged(Theme oldValue, Theme newValue)
    {
        if (oldValue == newValue)
        {
            return;
        }
        Logger.Info<SettingsPageViewModel>($"Theme changed from '{oldValue}' to '{newValue}'");
        OnPropertyChanged(nameof(SelectedThemeIndex));
    }
}