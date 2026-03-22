using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using SkiaSharp;
using XeniaManager.Core.Constants;
using XeniaManager.Core.Logging;
using XeniaManager.Core.Manage;
using XeniaManager.Core.Models;
using XeniaManager.Core.Models.Game;
using XeniaManager.Core.Utilities;
using XeniaManager.Core.Utilities.Paths;
using XeniaManager.Services;

namespace XeniaManager.ViewModels.Controls;

/// <summary>
/// ViewModel for the game details editor dialog.
/// Manages editing of game information including artwork, title, compatibility rating, and compatibility page URL.
/// </summary>
public partial class GameDetailsEditorViewModel : ObservableObject
{
    private readonly Game _game;
    private readonly IMessageBoxService _messageBoxService;
    private readonly Core.Settings.Settings _settings;

    [ObservableProperty] private string _titleId;
    [ObservableProperty] private string _mediaId;
    [ObservableProperty] private string _gameTitle;
    [ObservableProperty] private string _gamePath;
    [ObservableProperty] private string _compatibilityPageUrl;
    [ObservableProperty] private CompatibilityRatingItem _selectedCompatibilityRating;

    [ObservableProperty] private List<CompatibilityRatingItem> _compatibilityRatings =
    [
        new CompatibilityRatingItem(CompatibilityRating.Unknown, LocalizationHelper.GetText("CompatibilityRating.Unknown")),
        new CompatibilityRatingItem(CompatibilityRating.Unplayable, LocalizationHelper.GetText("CompatibilityRating.Unplayable")),
        new CompatibilityRatingItem(CompatibilityRating.Loads, LocalizationHelper.GetText("CompatibilityRating.Loads")),
        new CompatibilityRatingItem(CompatibilityRating.Gameplay, LocalizationHelper.GetText("CompatibilityRating.Gameplay")),
        new CompatibilityRatingItem(CompatibilityRating.Playable, LocalizationHelper.GetText("CompatibilityRating.Playable"))
    ];

    [ObservableProperty] private string _iconPath;
    [ObservableProperty] private string _boxartPath;
    [ObservableProperty] private string _backgroundPath;
    [ObservableProperty] private bool _hasChanges;

    // Xenia Version selection
    [ObservableProperty] private XeniaVersionItem _selectedXeniaVersion;
    [ObservableProperty] private List<XeniaVersionItem> _xeniaVersions;
    [ObservableProperty] private string _customExecutablePath;

    // Cached images for display
    [ObservableProperty] private Bitmap? _cachedIcon;
    [ObservableProperty] private Bitmap? _cachedBoxart;
    [ObservableProperty] private Bitmap? _cachedBackground;

    public GameDetailsEditorViewModel(Game game, IMessageBoxService messageBoxService)
    {
        _game = game;
        _messageBoxService = messageBoxService;
        _settings = App.Services.GetRequiredService<Core.Settings.Settings>();

        TitleId = game.GameId;
        MediaId = game.MediaId;
        GameTitle = game.Title;
        GamePath = game.FileLocations.Game;
        CompatibilityPageUrl = game.Compatibility.Url;
        SelectedCompatibilityRating = CompatibilityRatings.First(r => r.Rating == game.Compatibility.Rating);

        IconPath = game.Artwork.Icon;
        BoxartPath = game.Artwork.Boxart;
        BackgroundPath = game.Artwork.Background;

        // Initialize Xenia versions based on installed versions
        List<XeniaVersion> installedVersions = _settings.GetInstalledVersions(_settings);
        XeniaVersions = [];

        // Add installed versions
        if (installedVersions.Contains(XeniaVersion.Canary))
        {
            XeniaVersions.Add(new XeniaVersionItem(XeniaVersion.Canary, "Canary"));
        }
        if (installedVersions.Contains(XeniaVersion.Mousehook))
        {
            XeniaVersions.Add(new XeniaVersionItem(XeniaVersion.Mousehook, "Mousehook"));
        }
        if (installedVersions.Contains(XeniaVersion.Netplay))
        {
            XeniaVersions.Add(new XeniaVersionItem(XeniaVersion.Netplay, "Netplay"));
        }

        // Always add the Custom option
        XeniaVersions.Add(new XeniaVersionItem(XeniaVersion.Custom, "Custom"));

        // Select the current game's Xenia version (or default to first available)
        XeniaVersionItem? currentVersion = XeniaVersions.FirstOrDefault(v => v.Version == game.XeniaVersion);
        SelectedXeniaVersion = currentVersion ?? XeniaVersions.First();

        CustomExecutablePath = game.FileLocations.CustomEmulatorExecutable ?? string.Empty;

        HasChanges = false;

        // Load cached images
        LoadCachedImages();
    }

    /// <summary>
    /// Loads cached images from the game artwork.
    /// </summary>
    private void LoadCachedImages()
    {
        CachedIcon = _game.Artwork.CachedIcon;
        CachedBoxart = _game.Artwork.CachedBoxart;
        CachedBackground = _game.Artwork.CachedBackground;
    }

    /// <summary>
    /// Refreshes cached images after artwork changes.
    /// Forces the cached values to null so they reload from the disk.
    /// </summary>
    private void RefreshCachedImages()
    {
        // Force clear the cached values in the GameArtwork class
        _game.Artwork.ClearCachedImages();

        // Then reload from the artwork (which will reload from disk)
        CachedIcon = _game.Artwork.CachedIcon;
        CachedBoxart = _game.Artwork.CachedBoxart;
        CachedBackground = _game.Artwork.CachedBackground;
    }

    /// <summary>
    /// Validates the game title for duplicates and proper formatting.
    /// </summary>
    /// <param name="title">The title to validate.</param>
    /// <returns>A tuple containing (isValid, errorMessage).</returns>
    private (bool IsValid, string ErrorMessage) ValidateGameTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return (false, LocalizationHelper.GetText("GameDetailsEditor.Validation.Title.Empty"));
        }

        if (title.Length > 100)
        {
            return (false, LocalizationHelper.GetText("GameDetailsEditor.Validation.Title.TooLong"));
        }

        // Check for duplicates (excluding the current game)
        if (GameManager.Games.Any(g => g.Title == title && g != _game))
        {
            return (false, LocalizationHelper.GetText("GameDetailsEditor.Validation.Title.Duplicate"));
        }

        return (true, string.Empty);
    }

    /// <summary>
    /// Filters the game title to fit the criteria.
    /// </summary>
    /// <param name="title">The title to filter.</param>
    /// <returns>The filtered title.</returns>
    private string FilterGameTitle(string title)
    {
        return title.Replace(":", " -").Replace('\\', ' ').Replace('/', ' ');
    }

    /// <summary>
    /// Handles the game title property change to validate and filter it.
    /// </summary>
    partial void OnGameTitleChanged(string value)
    {
        HasChanges = true;
    }

    /// <summary>
    /// Handles the compatibility page URL property change.
    /// </summary>
    partial void OnCompatibilityPageUrlChanged(string value)
    {
        HasChanges = true;
    }

    /// <summary>
    /// Handles the selected compatibility rating property change.
    /// </summary>
    partial void OnSelectedCompatibilityRatingChanged(CompatibilityRatingItem value)
    {
        HasChanges = true;
    }

    /// <summary>
    /// Handles the selected Xenia version property change.
    /// </summary>
    partial void OnSelectedXeniaVersionChanged(XeniaVersionItem value)
    {
        HasChanges = true;
    }

    /// <summary>
    /// Handles the custom executable path property change.
    /// </summary>
    partial void OnCustomExecutablePathChanged(string value)
    {
        HasChanges = true;
    }

    /// <summary>
    /// Opens a file picker to select a new icon image.
    /// </summary>
    [RelayCommand]
    private async Task SelectIconAsync()
    {
        await SelectArtworkAsync("Icon");
    }

    /// <summary>
    /// Opens a file picker to select a new boxart image.
    /// </summary>
    [RelayCommand]
    private async Task SelectBoxartAsync()
    {
        await SelectArtworkAsync("Boxart");
    }

    /// <summary>
    /// Opens a file picker to select a new background image.
    /// </summary>
    [RelayCommand]
    private async Task SelectBackgroundAsync()
    {
        await SelectArtworkAsync("Background");
    }

    /// <summary>
    /// Opens a file picker to select an artwork image.
    /// </summary>
    /// <param name="artworkType">The type of artwork (Icon, Boxart, Background).</param>
    private async Task SelectArtworkAsync(string artworkType)
    {
        IStorageProvider? storageProvider = App.MainWindow?.StorageProvider;
        if (storageProvider == null)
        {
            Logger.Warning<GameDetailsEditorViewModel>("Storage provider is not available");
            await _messageBoxService.ShowErrorAsync(LocalizationHelper.GetText("GameDetailsEditor.Artwork.MissingStorageProvider.Title"),
                LocalizationHelper.GetText("GameDetailsEditor.Artwork.MissingStorageProvider.Message"));
            return;
        }

        FilePickerOpenOptions options = new FilePickerOpenOptions
        {
            Title = string.Format(LocalizationHelper.GetText("GameDetailsEditor.Artwork.FilePicker.Title"), artworkType),
            AllowMultiple = false,
            FileTypeFilter = new List<FilePickerFileType>
            {
                new FilePickerFileType("Image Files")
                {
                    Patterns = ["*.png", "*.jpg", "*.jpeg", ".ico"]
                }
            }
        };

        IReadOnlyList<IStorageFile> files = await storageProvider.OpenFilePickerAsync(options);
        if (files.Count == 0)
        {
            Logger.Debug<GameDetailsEditorViewModel>($"{artworkType} selection canceled by user");
            return;
        }

        string selectedPath = files[0].Path.LocalPath;
        Logger.Info<GameDetailsEditorViewModel>($"Selected {artworkType.ToLower()} image: {selectedPath}");

        try
        {
            // Copy the image to the game's artwork directory
            string gameDataDirectory = Path.Combine(AppPaths.GameDataDirectory, _game.Title, "Artwork");
            Directory.CreateDirectory(gameDataDirectory);

            // Determine the correct format and filename based on the artwork type
            string artworkFileName;
            string destinationPath;
            SKEncodedImageFormat targetFormat;

            switch (artworkType)
            {
                case "Icon":
                    artworkFileName = "Icon.png";
                    targetFormat = SKEncodedImageFormat.Png;
                    break;
                case "Boxart":
                    artworkFileName = "Boxart.png";
                    targetFormat = SKEncodedImageFormat.Png;
                    break;
                case "Background":
                    artworkFileName = "Background.jpg";
                    targetFormat = SKEncodedImageFormat.Jpeg;
                    break;
                default:
                    Logger.Warning<GameDetailsEditorViewModel>($"Unknown artwork type: {artworkType}");
                    return;
            }

            destinationPath = Path.Combine(gameDataDirectory, artworkFileName);

            // Convert the artwork to the proper format
            ArtworkManager.ConvertArtwork(selectedPath, destinationPath, targetFormat);
            Logger.Info<GameDetailsEditorViewModel>($"Converted and saved {artworkType.ToLower()} to: {destinationPath}");

            // Update the game's artwork path
            string relativePath = Path.Combine("GameData", _game.Title, "Artwork", artworkFileName);

            switch (artworkType)
            {
                case "Icon":
                    IconPath = relativePath;
                    _game.Artwork.Icon = relativePath;
                    break;
                case "Boxart":
                    BoxartPath = relativePath;
                    _game.Artwork.Boxart = relativePath;
                    break;
                case "Background":
                    BackgroundPath = relativePath;
                    _game.Artwork.Background = relativePath;
                    break;
            }

            HasChanges = true;
            Logger.Info<GameDetailsEditorViewModel>($"{artworkType} updated successfully");

            // Refresh cached images
            RefreshCachedImages();
        }
        catch (Exception ex)
        {
            Logger.Error<GameDetailsEditorViewModel>($"Failed to update {artworkType.ToLower()}");
            Logger.LogExceptionDetails<GameDetailsEditorViewModel>(ex);
            await _messageBoxService.ShowErrorAsync(string.Format(LocalizationHelper.GetText("GameDetailsEditor.Artwork.Update.Error.Title"), artworkType),
                string.Format(LocalizationHelper.GetText("GameDetailsEditor.Artwork.Update.Error.Message"), artworkType.ToLower(), ex));
        }
    }

    /// <summary>
    /// Clears the current icon.
    /// </summary>
    [RelayCommand]
    private async Task ClearIconAsync()
    {
        await ClearArtworkAsync("Icon");
    }

    /// <summary>
    /// Clears the current boxart.
    /// </summary>
    [RelayCommand]
    private async Task ClearBoxartAsync()
    {
        await ClearArtworkAsync("Boxart");
    }

    /// <summary>
    /// Clears the current background.
    /// </summary>
    [RelayCommand]
    private async Task ClearBackgroundAsync()
    {
        await ClearArtworkAsync("Background");
    }

    /// <summary>
    /// Opens a file picker to select a new game path.
    /// </summary>
    [RelayCommand]
    private async Task ChangeGamePathAsync()
    {
        IStorageProvider? storageProvider = App.MainWindow?.StorageProvider;
        if (storageProvider == null)
        {
            Logger.Warning<GameDetailsEditorViewModel>("Storage provider is not available");
            await _messageBoxService.ShowErrorAsync(LocalizationHelper.GetText("GameDetailsEditor.GamePath.MissingStorageProvider.Title"),
                LocalizationHelper.GetText("GameDetailsEditor.GamePath.MissingStorageProvider.Message"));
            return;
        }

        FilePickerOpenOptions options = new FilePickerOpenOptions
        {
            Title = LocalizationHelper.GetText("GameDetailsEditor.GamePath.FilePicker.Title"),
            AllowMultiple = false,
            FileTypeFilter = new List<FilePickerFileType>
            {
                new FilePickerFileType("Game Files")
                {
                    Patterns = ["*.iso", "*.xex", "*.zar"]
                },
                new FilePickerFileType("All Files")
                {
                    Patterns = ["*.*"]
                }
            }
        };

        IReadOnlyList<IStorageFile> files = await storageProvider.OpenFilePickerAsync(options);
        if (files.Count == 0)
        {
            Logger.Debug<GameDetailsEditorViewModel>("Game path selection canceled by user");
            return;
        }

        string selectedPath = files[0].Path.LocalPath;
        Logger.Info<GameDetailsEditorViewModel>($"Selected game path: {selectedPath}");

        GamePath = selectedPath;
        HasChanges = true;
    }

    /// <summary>
    /// Opens a file picker to select a custom Xenia executable.
    /// </summary>
    [RelayCommand]
    private async Task SelectCustomExecutableAsync()
    {
        IStorageProvider? storageProvider = App.MainWindow?.StorageProvider;
        if (storageProvider == null)
        {
            Logger.Warning<GameDetailsEditorViewModel>("Storage provider is not available");
            await _messageBoxService.ShowErrorAsync(LocalizationHelper.GetText("GameDetailsEditor.CustomExecutable.MissingStorageProvider.Title"),
                LocalizationHelper.GetText("GameDetailsEditor.CustomExecutable.MissingStorageProvider.Message"));
            return;
        }

        FilePickerOpenOptions options = new FilePickerOpenOptions
        {
            Title = LocalizationHelper.GetText("GameDetailsEditor.CustomExecutable.FilePicker.Title"),
            AllowMultiple = false,
            FileTypeFilter = new List<FilePickerFileType>
            {
                new FilePickerFileType("Executable Files")
                {
                    Patterns = ["*.exe"]
                },
                new FilePickerFileType("All Files")
                {
                    Patterns = ["*.*"]
                }
            }
        };

        IReadOnlyList<IStorageFile> files = await storageProvider.OpenFilePickerAsync(options);
        if (files.Count == 0)
        {
            Logger.Debug<GameDetailsEditorViewModel>("Custom executable selection canceled by user");
            return;
        }

        string selectedPath = files[0].Path.LocalPath;
        Logger.Info<GameDetailsEditorViewModel>($"Selected custom executable: {selectedPath}");

        CustomExecutablePath = selectedPath;
        HasChanges = true;
    }

    /// <summary>
    /// Clears the specified artwork.
    /// </summary>
    /// <param name="artworkType">The type of artwork to clear.</param>
    private async Task ClearArtworkAsync(string artworkType)
    {
        bool confirmed = await _messageBoxService.ShowConfirmationAsync(string.Format(LocalizationHelper.GetText("GameDetailsEditor.Artwork.Clear.Confirmation.Title"), artworkType),
            string.Format(LocalizationHelper.GetText("GameDetailsEditor.Artwork.Clear.Confirmation.Message"), artworkType.ToLower()));

        if (!confirmed)
        {
            return;
        }

        switch (artworkType)
        {
            case "Icon":
                IconPath = string.Empty;
                _game.Artwork.Icon = string.Empty;
                break;
            case "Boxart":
                BoxartPath = string.Empty;
                _game.Artwork.Boxart = string.Empty;
                break;
            case "Background":
                BackgroundPath = string.Empty;
                _game.Artwork.Background = string.Empty;
                break;
        }

        HasChanges = true;
        Logger.Info<GameDetailsEditorViewModel>($"{artworkType} cleared successfully");

        // Refresh cached images
        RefreshCachedImages();
    }

    /// <summary>
    /// Saves all changes to the game.
    /// </summary>
    public async Task<bool> SaveAsync()
    {
        // Validate game title
        (bool isValid, string errorMessage) = ValidateGameTitle(GameTitle);
        if (!isValid)
        {
            await _messageBoxService.ShowErrorAsync(LocalizationHelper.GetText("GameDetailsEditor.Save.ValidationFailed.Title"),
                errorMessage);
            return false;
        }

        // Validate the custom executable path if a Custom version is selected
        if (SelectedXeniaVersion.Version == XeniaVersion.Custom)
        {
            if (string.IsNullOrWhiteSpace(CustomExecutablePath))
            {
                await _messageBoxService.ShowErrorAsync(LocalizationHelper.GetText("GameDetailsEditor.Save.CustomExecutableRequired.Title"),
                    LocalizationHelper.GetText("GameDetailsEditor.Save.CustomExecutableRequired.Message"));
                return false;
            }

            if (!File.Exists(CustomExecutablePath))
            {
                await _messageBoxService.ShowErrorAsync(LocalizationHelper.GetText("GameDetailsEditor.Save.CustomExecutableNotFound.Title"),
                    LocalizationHelper.GetText("GameDetailsEditor.Save.CustomExecutableNotFound.Message"));
                return false;
            }
        }

        try
        {
            // Filter and apply the game title
            string filteredTitle = FilterGameTitle(GameTitle);

            // Handle title change (update directories and config paths)
            if (filteredTitle != _game.Title)
            {
                string oldTitle = _game.Title;
                Logger.Info<GameDetailsEditorViewModel>($"Game title changed from '{oldTitle}' to '{filteredTitle}'");

                // Update artwork directory
                string oldArtworkPath = Path.Combine(AppPaths.GameDataDirectory, oldTitle, "Artwork");
                string newArtworkPath = Path.Combine(AppPaths.GameDataDirectory, filteredTitle, "Artwork");

                if (Directory.Exists(oldArtworkPath))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(newArtworkPath)!);
                    Directory.Move(oldArtworkPath, newArtworkPath);
                    Logger.Info<GameDetailsEditorViewModel>($"Moved artwork directory from '{oldArtworkPath}' to '{newArtworkPath}'");
                }

                // Update config file path
                string oldConfigPath = _game.FileLocations.Config;
                string newConfigPath = Path.Combine(XeniaVersionInfo.GetXeniaVersionInfo(_game.XeniaVersion).ConfigFolderLocation, $"{filteredTitle}.config.toml");

                if (File.Exists(AppPathResolver.GetFullPath(oldConfigPath)))
                {
                    File.Move(AppPathResolver.GetFullPath(oldConfigPath), AppPathResolver.GetFullPath(newConfigPath), true);
                    Logger.Info<GameDetailsEditorViewModel>($"Moved config file from '{oldConfigPath}' to '{newConfigPath}'");
                }

                // Update patch file path (If it exists)
                if (_game.FileLocations.Patch != null)
                {
                    string oldPatchesPath = _game.FileLocations.Patch;
                    string newPatchPath = Path.Combine(XeniaVersionInfo.GetXeniaVersionInfo(_game.XeniaVersion).ConfigFolderLocation, $"{_game.GameId} - {filteredTitle}.patch.toml");

                    if (oldPatchesPath != null && File.Exists(AppPathResolver.GetFullPath(oldPatchesPath)))
                    {
                        File.Move(AppPathResolver.GetFullPath(oldPatchesPath), AppPathResolver.GetFullPath(newPatchPath), true);
                        Logger.Info<GameDetailsEditorViewModel>($"Moved patches file from '{oldPatchesPath}' to '{newPatchPath}'");
                    }
                    _game.FileLocations.Patch = newPatchPath;
                }

                // Update game paths
                _game.FileLocations.Config = newConfigPath;

                // Update artwork paths
                if (!string.IsNullOrEmpty(IconPath))
                {
                    IconPath = Path.Combine("GameData", filteredTitle, "Artwork", Path.GetFileName(IconPath));
                }
                if (!string.IsNullOrEmpty(BoxartPath))
                {
                    BoxartPath = Path.Combine("GameData", filteredTitle, "Artwork", Path.GetFileName(BoxartPath));
                }
                if (!string.IsNullOrEmpty(BackgroundPath))
                {
                    BackgroundPath = Path.Combine("GameData", filteredTitle, "Artwork", Path.GetFileName(BackgroundPath));
                }
            }

            // Handle Xenia version change (move config and patch files to the new emulator location)
            XeniaVersion newVersion = SelectedXeniaVersion.Version;
            if (newVersion != _game.XeniaVersion && newVersion != XeniaVersion.Custom)
            {
                Logger.Info<GameDetailsEditorViewModel>($"Xenia version changed from '{_game.XeniaVersion}' to '{newVersion}'");

                // Get the new emulator paths
                XeniaVersionInfo newVersionInfo = XeniaVersionInfo.GetXeniaVersionInfo(newVersion);

                // Migrate config file to new Xenia version
                string oldConfigPath = _game.FileLocations.Config;
                string newConfigPath = Path.Combine(newVersionInfo.ConfigFolderLocation, $"{filteredTitle}.config.toml");

                if (!string.IsNullOrEmpty(oldConfigPath) && File.Exists(AppPathResolver.GetFullPath(oldConfigPath)))
                {
                    bool migrated = ConfigManager.MigrateConfigurationFile(oldConfigPath, newConfigPath, newVersion);
                    if (migrated)
                    {
                        Logger.Info<GameDetailsEditorViewModel>($"Successfully migrated config file to new Xenia version");
                    }
                }

                // Move the patch file to the new emulator's patch folder (if it exists)
                if (!string.IsNullOrEmpty(_game.FileLocations.Patch))
                {
                    string oldPatchPath = _game.FileLocations.Patch;
                    string newPatchPath = Path.Combine(newVersionInfo.PatchFolderLocation, $"{_game.GameId} - {filteredTitle}.patch.toml");

                    if (!string.IsNullOrEmpty(oldPatchPath) && File.Exists(AppPathResolver.GetFullPath(oldPatchPath)))
                    {
                        // Only move if the paths are different
                        if (oldPatchPath != newPatchPath)
                        {
                            Directory.CreateDirectory(Path.GetDirectoryName(AppPathResolver.GetFullPath(newPatchPath))!);
                            File.Move(AppPathResolver.GetFullPath(oldPatchPath), AppPathResolver.GetFullPath(newPatchPath), true);
                            Logger.Info<GameDetailsEditorViewModel>($"Moved patch file from '{oldPatchPath}' to '{newPatchPath}'");
                        }
                    }

                    _game.FileLocations.Patch = newPatchPath;
                }

                _game.FileLocations.Config = newConfigPath;
            }

            // Apply all changes
            _game.Title = filteredTitle;
            _game.Compatibility.Url = CompatibilityPageUrl;
            _game.Compatibility.Rating = SelectedCompatibilityRating.Rating;
            _game.Artwork.Icon = IconPath;
            _game.Artwork.Boxart = BoxartPath;
            _game.Artwork.Background = BackgroundPath;
            _game.FileLocations.Game = GamePath;
            _game.XeniaVersion = SelectedXeniaVersion.Version;
            _game.FileLocations.CustomEmulatorExecutable = SelectedXeniaVersion.Version == XeniaVersion.Custom ? CustomExecutablePath : null;

            // Clear cached images so they reload with new paths/files
            _game.Artwork.ClearCachedImages();

            // Save the game library
            GameManager.SaveLibrary();

            Logger.Info<GameDetailsEditorViewModel>($"Successfully saved game details for: '{_game.Title}'");
            return true;
        }
        catch (Exception ex)
        {
            Logger.Error<GameDetailsEditorViewModel>($"Failed to save game details");
            Logger.LogExceptionDetails<GameDetailsEditorViewModel>(ex);
            await _messageBoxService.ShowErrorAsync(LocalizationHelper.GetText("GameDetailsEditor.Save.Failed.Title"),
                string.Format(LocalizationHelper.GetText("GameDetailsEditor.Save.Failed.Message"), ex));
            return false;
        }
    }

    /// <summary>
    /// Cancels the edit and discards changes.
    /// </summary>
    [RelayCommand]
    private void Cancel()
    {
        // Reset to original values
        GameTitle = _game.Title;
        CompatibilityPageUrl = _game.Compatibility.Url;
        SelectedCompatibilityRating = CompatibilityRatings.First(r => r.Rating == _game.Compatibility.Rating);
        IconPath = _game.Artwork.Icon;
        BoxartPath = _game.Artwork.Boxart;
        BackgroundPath = _game.Artwork.Background;

        // Reset Xenia version (find it in the list or use first available)
        XeniaVersionItem? currentVersion = XeniaVersions.FirstOrDefault(v => v.Version == _game.XeniaVersion);
        SelectedXeniaVersion = currentVersion ?? XeniaVersions.First();

        CustomExecutablePath = _game.FileLocations.CustomEmulatorExecutable ?? string.Empty;
        HasChanges = false;
    }

    /// <summary>
    /// Saves all changes to the game.
    /// </summary>
    [RelayCommand]
    private async Task DoSaveAsync()
    {
        await SaveAsync();
    }
}

/// <summary>
/// Represents a compatibility rating item with its enum value and localized display name.
/// </summary>
/// <param name="Rating">The compatibility rating enum value.</param>
/// <param name="DisplayName">The localized display name for the rating.</param>
public record CompatibilityRatingItem(CompatibilityRating Rating, string DisplayName);

/// <summary>
/// Represents a Xenia version item with its enum value and localized display name.
/// </summary>
/// <param name="Version">The Xenia version enum value.</param>
/// <param name="DisplayName">The localized display name for the version.</param>
public record XeniaVersionItem(XeniaVersion Version, string DisplayName);