using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using XeniaManager.Core.Files;
using XeniaManager.Core.Logging;
using XeniaManager.Core.Manage;
using XeniaManager.Core.Models;
using XeniaManager.Core.Models.Files.Gpd;
using XeniaManager.Core.Models.Files.Stfs;
using XeniaManager.Core.Models.Game;
using XeniaManager.Core.Models.Items;
using XeniaManager.Core.Utilities;
using XeniaManager.Services;
using XeniaManager.ViewModels.Items;

namespace XeniaManager.ViewModels.Controls;

/// <summary>
/// ViewModel for the installed content dialog.
/// Manages content type selection and account selection for viewing installed content.
/// </summary>
public partial class ContentViewerDialogViewModel : ViewModelBase
{
    private readonly IMessageBoxService _messageBoxService;
    private SecretCodeListener? _secretCodeListener;

    [ObservableProperty] private Game _game;

    /// <summary>
    /// Available content types for display.
    /// </summary>
    public ObservableCollection<EnumDisplayItem<ContentType>> ContentTypes { get; }

    /// <summary>
    /// The currently selected content type.
    /// </summary>
    [ObservableProperty] private EnumDisplayItem<ContentType>? _selectedContentType;

    partial void OnSelectedContentTypeChanged(EnumDisplayItem<ContentType>? value)
    {
        UpdateAccountVisibility();
        UpdateContentList();
        OnPropertyChanged(nameof(IsSavedGameContentType));
    }

    /// <summary>
    /// Gets whether the selected content type is SavedGame.
    /// </summary>
    public bool IsSavedGameContentType => SelectedContentType?.Value == ContentType.SavedGame;

    /// <summary>
    /// Collection of account content items (excludes GameContent).
    /// Used for SavedGame and Achievements content types.
    /// </summary>
    [ObservableProperty] private ObservableCollection<AccountContent> _accountContents = [];

    partial void OnAccountContentsChanged(ObservableCollection<AccountContent> value)
    {
        UpdateAccountVisibility();
    }

    /// <summary>
    /// The currently selected account content.
    /// </summary>
    [ObservableProperty] private AccountContent? _selectedAccountContent;

    partial void OnSelectedAccountContentChanged(AccountContent? value)
    {
        UpdateContentList();
    }

    /// <summary>
    /// Collection of game content items (only GameContent).
    /// Used for MarketplaceContent and Installer content types.
    /// </summary>
    [ObservableProperty] private ObservableCollection<GameContent> _gameContents = [];

    partial void OnGameContentsChanged(ObservableCollection<GameContent> value)
    {
        UpdateAccountVisibility();
    }

    /// <summary>
    /// The currently selected game content.
    /// </summary>
    [ObservableProperty] private GameContent? _selectedGameContent;

    partial void OnSelectedGameContentChanged(GameContent? value)
    {
        UpdateContentList();
    }

    /// <summary>
    /// Indicates whether the account selector should be visible.
    /// True for SavedGame and Achievements content types when there are account contents.
    /// </summary>
    [ObservableProperty] private bool _isAccountSelectorVisible;

    partial void OnIsAccountSelectorVisibleChanged(bool value)
    {
        OnPropertyChanged(nameof(IsAccountSelectorVisible));
    }

    /// <summary>
    /// Collection of header files to display in the dialog.
    /// </summary>
    [ObservableProperty] private ObservableCollection<HeaderFileViewModel> _headerFiles = [];

    partial void OnHeaderFilesChanged(ObservableCollection<HeaderFileViewModel> value)
    {
        OnPropertyChanged(nameof(IsEmpty));
        // Subscribe to collection changes to update IsEmpty
        // Unsubscribe from an old collection if it exists
        HeaderFiles.CollectionChanged -= HeaderFiles_CollectionChanged;
        value.CollectionChanged += HeaderFiles_CollectionChanged;
    }

    private void HeaderFiles_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        OnPropertyChanged(nameof(IsEmpty));
        OnPropertyChanged(nameof(IsHeaderFilesEmptyStateVisible));
    }

    /// <summary>
    /// Collection of selected header files.
    /// </summary>
    [ObservableProperty] private ObservableCollection<HeaderFileViewModel> _selectedHeaderFiles = [];

    /// <summary>
    /// Indicates whether the header files list is empty.
    /// </summary>
    public bool IsEmpty => HeaderFiles.Count == 0;

    /// <summary>
    /// Collection of achievements to display when the Achievements content type is selected.
    /// </summary>
    [ObservableProperty] private ObservableCollection<AchievementViewModel> _achievements = [];

    partial void OnAchievementsChanged(ObservableCollection<AchievementViewModel> value)
    {
        OnPropertyChanged(nameof(IsAchievementsEmpty));
        OnPropertyChanged(nameof(IsAchievementsEmptyStateVisible));
        OnPropertyChanged(nameof(CanSelectAllAchievements));
        OnPropertyChanged(nameof(CanUnlockAchievements));
        OnPropertyChanged(nameof(CanLockAchievements));

        // Unsubscribe from the old collection if it exists
        Achievements.CollectionChanged -= Achievements_CollectionChanged;
        foreach (AchievementViewModel achievement in Achievements)
        {
            achievement.PropertyChanged -= Achievement_PropertyChanged;
        }

        // Subscribe to collection changes to update IsAchievementsEmpty
        value.CollectionChanged += Achievements_CollectionChanged;
        foreach (AchievementViewModel achievement in value)
        {
            achievement.PropertyChanged += Achievement_PropertyChanged;
        }
    }

    private void Achievements_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        OnPropertyChanged(nameof(IsAchievementsEmpty));
        OnPropertyChanged(nameof(IsAchievementsEmptyStateVisible));
        OnPropertyChanged(nameof(CanSelectAllAchievements));
        OnPropertyChanged(nameof(CanUnlockAchievements));
        OnPropertyChanged(nameof(CanLockAchievements));

        // Subscribe/unsubscribe to individual achievement property changes
        if (e.OldItems != null)
        {
            foreach (AchievementViewModel achievement in e.OldItems)
            {
                achievement.PropertyChanged -= Achievement_PropertyChanged;
            }
        }
        if (e.NewItems != null)
        {
            foreach (AchievementViewModel achievement in e.NewItems)
            {
                achievement.PropertyChanged += Achievement_PropertyChanged;
            }
        }
    }

    private void Achievement_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(AchievementViewModel.IsSelected))
        {
            OnPropertyChanged(nameof(CanSelectAllAchievements));
            OnPropertyChanged(nameof(CanUnlockAchievements));
            OnPropertyChanged(nameof(CanLockAchievements));
            SelectAllAchievementsCommand.NotifyCanExecuteChanged();
            UnlockSelectedAchievementsCommand.NotifyCanExecuteChanged();
            LockSelectedAchievementsCommand.NotifyCanExecuteChanged();
        }
    }

    /// <summary>
    /// Gets or sets whether the advanced achievement features are enabled (hidden feature).
    /// When false, checkboxes and select/unselect/lock/unlock functionality are hidden.
    /// </summary>
    [ObservableProperty] private bool _areAchievementFeaturesEnabled = false;

    partial void OnAreAchievementFeaturesEnabledChanged(bool value)
    {
        foreach (AchievementViewModel achievement in _allAchievements)
        {
            achievement.AreAchievementFeaturesEnabled = value;
        }
    }

    /// <summary>
    /// Gets whether achievements can be unlocked (selected achievements that are locked).
    /// </summary>
    public bool CanUnlockAchievements => AreAchievementFeaturesEnabled && Achievements.Any(a => a.IsSelected && !a.IsUnlocked);

    /// <summary>
    /// Gets whether achievements can be locked (selected achievements that are unlocked).
    /// </summary>
    public bool CanLockAchievements => AreAchievementFeaturesEnabled && Achievements.Any(a => a.IsSelected && a.IsUnlocked);

    /// <summary>
    /// Gets whether achievements can be selected (not all achievements are selected).
    /// </summary>
    public bool CanSelectAllAchievements => AreAchievementFeaturesEnabled && Achievements.Any(a => !a.IsSelected);

    /// <summary>
    /// Selects all achievements.
    /// </summary>
    [RelayCommand]
    private void SelectAllAchievements()
    {
        foreach (AchievementViewModel achievement in Achievements)
        {
            achievement.IsSelected = true;
        }

        OnPropertyChanged(nameof(CanSelectAllAchievements));
        OnPropertyChanged(nameof(CanUnlockAchievements));
        OnPropertyChanged(nameof(CanLockAchievements));
    }

    /// <summary>
    /// Unselects all achievements.
    /// </summary>
    [RelayCommand]
    private void UnselectAllAchievements()
    {
        foreach (AchievementViewModel achievement in Achievements)
        {
            achievement.IsSelected = false;
        }

        OnPropertyChanged(nameof(CanSelectAllAchievements));
        OnPropertyChanged(nameof(CanUnlockAchievements));
        OnPropertyChanged(nameof(CanLockAchievements));
    }

    /// <summary>
    /// Indicates whether the achievements list is empty.
    /// </summary>
    public bool IsAchievementsEmpty => Achievements.Count == 0;

    /// <summary>
    /// Indicates whether the achievements empty state should be visible.
    /// True when the achievements list is visible AND empty.
    /// </summary>
    public bool IsAchievementsEmptyStateVisible => IsAchievementsListVisible && IsAchievementsEmpty;

    /// <summary>
    /// Gets the total achievement count.
    /// </summary>
    [ObservableProperty] private int _achievementCount;

    /// <summary>
    /// Gets the unlocked achievement count.
    /// </summary>
    [ObservableProperty] private int _achievementUnlockedCount;

    /// <summary>
    /// Gets the total gamerscore.
    /// </summary>
    [ObservableProperty] private int _gamerscoreTotal;

    /// <summary>
    /// Gets the unlocked gamerscore.
    /// </summary>
    [ObservableProperty] private int _gamerscoreUnlocked;

    /// <summary>
    /// Gets or sets the current achievement filter.
    /// </summary>
    [ObservableProperty] private string _achievementFilter = string.Empty;

    partial void OnAchievementFilterChanged(string value)
    {
        ApplyAchievementFilter();
    }

    /// <summary>
    /// Gets the list of available achievement filters.
    /// </summary>
    public string[] AchievementFilters =>
    [
        LocalizationHelper.GetText("InstalledContentDialog.AchievementFilter.All"),
        LocalizationHelper.GetText("InstalledContentDialog.AchievementFilter.Unlocked"),
        LocalizationHelper.GetText("InstalledContentDialog.AchievementFilter.Locked")
    ];

    /// <summary>
    /// All achievements loaded from the GPD file (unfiltered).
    /// </summary>
    private List<AchievementViewModel> _allAchievements = [];

    /// <summary>
    /// Indicates whether the header files empty state should be visible.
    /// True when the header files list is visible AND empty.
    /// </summary>
    public bool IsHeaderFilesEmptyStateVisible => IsHeaderFilesListVisible && IsEmpty;

    /// <summary>
    /// Indicates whether the achievements list is visible.
    /// True when Achievements content type is selected.
    /// </summary>
    [ObservableProperty] private bool _isAchievementsListVisible;

    partial void OnIsAchievementsListVisibleChanged(bool value)
    {
        OnPropertyChanged(nameof(IsHeaderFilesListVisible));
        OnPropertyChanged(nameof(IsHeaderFilesEmptyStateVisible));
        OnPropertyChanged(nameof(IsAchievementsEmptyStateVisible));
    }

    /// <summary>
    /// Indicates whether the header files list is visible.
    /// True for all content types except Achievements.
    /// </summary>
    [ObservableProperty] private bool _isHeaderFilesListVisible = true;

    partial void OnIsHeaderFilesListVisibleChanged(bool value)
    {
        OnPropertyChanged(nameof(IsHeaderFilesEmptyStateVisible));
        OnPropertyChanged(nameof(IsAchievementsEmptyStateVisible));
    }

    /// <summary>
    /// Unlocks the selected achievements.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanUnlockAchievements))]
    private void UnlockSelectedAchievements()
    {
        if (SelectedAccountContent?.GameAchievementGpdFile == null)
        {
            return;
        }

        GpdFile? gpdFile = SelectedAccountContent.GameAchievementGpdFile;
        int unlockedCount = 0;
        int gamerscoreGained = 0;
        List<string> failedAchievements = [];

        foreach (AchievementViewModel selectedAchievement in Achievements.Where(a => a is { IsSelected: true, IsUnlocked: false }))
        {
            if (gpdFile.UnlockAchievement(selectedAchievement.AchievementId))
            {
                selectedAchievement.Achievement.IsEarned = true;
                selectedAchievement.Achievement.UnlockTime = DateTime.Now.ToFileTime();
                selectedAchievement.Refresh(clearImageCache: true);
                selectedAchievement.IsSelected = false;
                unlockedCount++;
                gamerscoreGained += selectedAchievement.Gamerscore;
            }
            else
            {
                failedAchievements.Add(selectedAchievement.Name);
            }
        }

        if (unlockedCount > 0)
        {
            // Update ProfileGpd TitleEntry if it exists
            UpdateProfileGpdTitleEntry(unlockedCount, gamerscoreGained);

            gpdFile.Save(SelectedAccountContent.GameAchievementGpdPath);
            Logger.Info<ContentViewerDialogViewModel>($"Unlocked {unlockedCount} achievements");

            // Update achievement statistics
            LoadAchievementStatistics();

            OnPropertyChanged(nameof(CanSelectAllAchievements));
            OnPropertyChanged(nameof(CanUnlockAchievements));
            OnPropertyChanged(nameof(CanLockAchievements));
        }

        if (failedAchievements.Count > 0)
        {
            string errorMessage = string.Format(
                LocalizationHelper.GetText("InstalledContentDialog.Achievements.UnlockFailed.Message"),
                string.Join(", ", failedAchievements));

            _messageBoxService.ShowErrorAsync(
                LocalizationHelper.GetText("InstalledContentDialog.Achievements.UnlockFailed.Title"),
                errorMessage);
        }
    }

    /// <summary>
    /// Locks the selected achievements.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanLockAchievements))]
    private void LockSelectedAchievements()
    {
        if (SelectedAccountContent?.GameAchievementGpdFile == null)
        {
            return;
        }

        GpdFile? gpdFile = SelectedAccountContent.GameAchievementGpdFile;
        int lockedCount = 0;
        int gamerscoreLost = 0;
        List<string> failedAchievements = [];

        foreach (AchievementViewModel selectedAchievement in Achievements.Where(a => a is { IsSelected: true, IsUnlocked: true }))
        {
            if (gpdFile.LockAchievement(selectedAchievement.AchievementId))
            {
                selectedAchievement.Achievement.IsEarned = false;
                selectedAchievement.Achievement.UnlockTime = 0;
                selectedAchievement.Refresh(clearImageCache: true);
                selectedAchievement.IsSelected = false;
                lockedCount++;
                gamerscoreLost += selectedAchievement.Gamerscore;
            }
            else
            {
                failedAchievements.Add(selectedAchievement.Name);
            }
        }

        if (lockedCount > 0)
        {
            // Update ProfileGpd TitleEntry if it exists
            UpdateProfileGpdTitleEntry(-lockedCount, -gamerscoreLost);

            gpdFile.Save(SelectedAccountContent.GameAchievementGpdPath);
            Logger.Info<ContentViewerDialogViewModel>($"Locked {lockedCount} achievements");

            // Update achievement statistics
            LoadAchievementStatistics();

            OnPropertyChanged(nameof(CanSelectAllAchievements));
            OnPropertyChanged(nameof(CanUnlockAchievements));
            OnPropertyChanged(nameof(CanLockAchievements));
        }

        if (failedAchievements.Count > 0)
        {
            string errorMessage = string.Format(
                LocalizationHelper.GetText("InstalledContentDialog.Achievements.LockFailed.Message"),
                string.Join(", ", failedAchievements));

            _messageBoxService.ShowErrorAsync(
                LocalizationHelper.GetText("InstalledContentDialog.Achievements.LockFailed.Title"),
                errorMessage);
        }
    }

    /// <summary>
    /// Unlocks all achievements.
    /// </summary>
    [RelayCommand]
    private void UnlockAllAchievements()
    {
        if (SelectedAccountContent?.GameAchievementGpdFile == null)
        {
            return;
        }

        GpdFile? gpdFile = SelectedAccountContent.GameAchievementGpdFile;
        int unlockedCount = 0;
        int gamerscoreGained = 0;
        List<string> failedAchievements = [];

        foreach (AchievementViewModel achievement in Achievements.Where(a => !a.IsUnlocked))
        {
            if (gpdFile.UnlockAchievement(achievement.AchievementId))
            {
                achievement.Achievement.IsEarned = true;
                achievement.Achievement.UnlockTime = DateTime.Now.ToFileTime();
                unlockedCount++;
                gamerscoreGained += achievement.Gamerscore;
            }
            else
            {
                failedAchievements.Add(achievement.Name);
            }
        }

        if (unlockedCount > 0)
        {
            // Update ProfileGpd TitleEntry if it exists
            UpdateProfileGpdTitleEntry(unlockedCount, gamerscoreGained);

            gpdFile.Save(SelectedAccountContent.GameAchievementGpdPath);
            Logger.Info<ContentViewerDialogViewModel>($"Unlocked all {unlockedCount} achievements");

            // Refresh all achievement view models
            foreach (AchievementViewModel achievement in Achievements)
            {
                achievement.Refresh(clearImageCache: true);
                achievement.IsSelected = false;
            }

            // Update achievement statistics
            LoadAchievementStatistics();

            OnPropertyChanged(nameof(CanSelectAllAchievements));
            OnPropertyChanged(nameof(CanUnlockAchievements));
            OnPropertyChanged(nameof(CanLockAchievements));
        }

        if (failedAchievements.Count > 0)
        {
            string errorMessage = string.Format(
                LocalizationHelper.GetText("InstalledContentDialog.Achievements.UnlockFailed.Message"),
                string.Join(", ", failedAchievements));

            _messageBoxService.ShowErrorAsync(
                LocalizationHelper.GetText("InstalledContentDialog.Achievements.UnlockFailed.Title"),
                errorMessage);
        }
    }

    /// <summary>
    /// Locks all achievements.
    /// </summary>
    [RelayCommand]
    private void LockAllAchievements()
    {
        if (SelectedAccountContent?.GameAchievementGpdFile == null)
        {
            return;
        }

        GpdFile? gpdFile = SelectedAccountContent.GameAchievementGpdFile;
        int lockedCount = 0;
        int gamerscoreLost = 0;
        List<string> failedAchievements = [];

        foreach (AchievementViewModel achievement in Achievements.Where(a => a.IsUnlocked))
        {
            if (gpdFile.LockAchievement(achievement.AchievementId))
            {
                achievement.Achievement.IsEarned = false;
                achievement.Achievement.UnlockTime = 0;
                lockedCount++;
                gamerscoreLost += achievement.Gamerscore;
            }
            else
            {
                failedAchievements.Add(achievement.Name);
            }
        }

        if (lockedCount > 0)
        {
            // Update ProfileGpd TitleEntry if it exists
            UpdateProfileGpdTitleEntry(-lockedCount, -gamerscoreLost);

            gpdFile.Save(SelectedAccountContent.GameAchievementGpdPath);
            Logger.Info<ContentViewerDialogViewModel>($"Locked all {lockedCount} achievements");

            // Refresh all achievement view models
            foreach (AchievementViewModel achievement in Achievements)
            {
                achievement.Refresh(clearImageCache: true);
                achievement.IsSelected = false;
            }

            // Update achievement statistics
            LoadAchievementStatistics();

            OnPropertyChanged(nameof(CanSelectAllAchievements));
            OnPropertyChanged(nameof(CanUnlockAchievements));
            OnPropertyChanged(nameof(CanLockAchievements));
        }

        if (failedAchievements.Count > 0)
        {
            string errorMessage = string.Format(
                LocalizationHelper.GetText("InstalledContentDialog.Achievements.LockFailed.Message"),
                string.Join(", ", failedAchievements));

            _messageBoxService.ShowErrorAsync(
                LocalizationHelper.GetText("InstalledContentDialog.Achievements.LockFailed.Title"),
                errorMessage);
        }
    }

    /// <summary>
    /// Deletes the selected header files.
    /// </summary>
    [RelayCommand]
    private async Task DeleteSelectedFiles()
    {
        if (SelectedHeaderFiles.Count == 0)
        {
            return;
        }

        // Build a list of files to delete
        string fileList = string.Join("\n", SelectedHeaderFiles.Select(h => $"- {h.DisplayName}"));

        bool result = await _messageBoxService.ShowConfirmationAsync(
            LocalizationHelper.GetText("InstalledContentDialog.DeleteSelected.Confirmation.Title"),
            string.Format(LocalizationHelper.GetText("InstalledContentDialog.DeleteSelected.Confirmation.Message"), SelectedHeaderFiles.Count, fileList));

        if (!result)
        {
            return;
        }

        int deletedCount = 0;
        foreach (HeaderFileViewModel headerFile in SelectedHeaderFiles.ToList())
        {
            try
            {
                string filePath = headerFile.FilePath;
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    deletedCount++;
                    HeaderFiles.Remove(headerFile);
                }
                else if (Directory.Exists(filePath))
                {
                    Directory.Delete(filePath, true);
                    deletedCount++;
                    HeaderFiles.Remove(headerFile);
                }
                if (File.Exists(headerFile.HeaderFilePath))
                {
                    File.Delete(headerFile.HeaderFilePath);
                }
            }
            catch (Exception ex)
            {
                Logger.Error<ContentViewerDialogViewModel>($"Failed to delete {headerFile.FileName}");
                Logger.LogExceptionDetails<InstallContentDialogViewModel>(ex);
            }
        }

        if (deletedCount > 0)
        {
            Logger.Info<ContentViewerDialogViewModel>($"Deleted {deletedCount} header file(s)");
            OnPropertyChanged(nameof(IsEmpty));
            OnPropertyChanged(nameof(IsHeaderFilesEmptyStateVisible));
        }
    }

    /// <summary>
    /// Deletes all header files.
    /// </summary>
    [RelayCommand]
    private async Task DeleteAllFiles()
    {
        if (HeaderFiles.Count == 0)
        {
            return;
        }

        // Build a list of files to delete
        string fileList = string.Join("\n", HeaderFiles.Select(h => $"- {h.DisplayName}"));

        bool result = await _messageBoxService.ShowConfirmationAsync(
            LocalizationHelper.GetText("InstalledContentDialog.DeleteAll.Confirmation.Title"),
            string.Format(LocalizationHelper.GetText("InstalledContentDialog.DeleteAll.Confirmation.Message"), HeaderFiles.Count, fileList));

        if (!result)
        {
            return;
        }

        int deletedCount = 0;
        foreach (HeaderFileViewModel headerFile in HeaderFiles.ToList())
        {
            try
            {
                string filePath = headerFile.FilePath;
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    deletedCount++;
                }
                else if (Directory.Exists(filePath))
                {
                    Directory.Delete(filePath, true);
                    deletedCount++;
                    HeaderFiles.Remove(headerFile);
                }
                if (File.Exists(headerFile.HeaderFilePath))
                {
                    File.Delete(headerFile.HeaderFilePath);
                }
            }
            catch (Exception ex)
            {
                Logger.Error<ContentViewerDialogViewModel>($"Failed to delete {headerFile.FileName}");
                Logger.LogExceptionDetails<InstallContentDialogViewModel>(ex);
            }
        }

        if (deletedCount > 0)
        {
            HeaderFiles.Clear();
            Logger.Info<ContentViewerDialogViewModel>($"Deleted {deletedCount} header file(s)");
            OnPropertyChanged(nameof(IsEmpty));
            OnPropertyChanged(nameof(IsHeaderFilesEmptyStateVisible));
        }
    }

    /// <summary>
    /// Exports all save games to a zip file.
    /// </summary>
    [RelayCommand]
    private async Task ExportSaves()
    {
        if (SelectedAccountContent == null || HeaderFiles.Count == 0)
        {
            await _messageBoxService.ShowWarningAsync(
                LocalizationHelper.GetText("InstalledContentDialog.ExportSaves.NoSaves.Title"),
                LocalizationHelper.GetText("InstalledContentDialog.ExportSaves.NoSaves.Message"));
            return;
        }

        // Get the TitleId from the first header file
        string titleId = HeaderFiles.First().Header.TitleId.ToString("X8");
        string timeStamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string zipFileName = $"SaveGame_{titleId}_{timeStamp}.xsave";

        // Show save file picker
        Window? topLevel = App.MainWindow;
        if (topLevel == null)
        {
            await _messageBoxService.ShowErrorAsync(
                LocalizationHelper.GetText("InstalledContentDialog.ExportSaves.MainWindow.Error.Title"),
                LocalizationHelper.GetText("InstalledContentDialog.ExportSaves.MainWindow.Error.Message"));
            return;
        }

        IStorageProvider storageProvider = topLevel.StorageProvider;
        IStorageFile? file = await storageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = LocalizationHelper.GetText("InstalledContentDialog.ExportSaves.FilePicker.Title"),
            FileTypeChoices =
            [
                new FilePickerFileType("Xenia Save File")
                {
                    Patterns = ["*.xsave"]
                }
            ],
            SuggestedFileName = zipFileName,
            DefaultExtension = "xsave",
            ShowOverwritePrompt = true
        });

        if (file == null)
        {
            return; // User cancelled
        }

        try
        {
            string zipPath = file.Path.LocalPath;

            // Convert HeaderFileViewModels to HeaderFiles
            IEnumerable<HeaderFile> headerFiles = HeaderFiles.Select(h => h.Header);

            bool result = await SaveManager.ExportSave(headerFiles, zipPath);

            if (result)
            {
                await _messageBoxService.ShowInfoAsync(
                    LocalizationHelper.GetText("InstalledContentDialog.ExportSaves.Success.Title"),
                    string.Format(LocalizationHelper.GetText("InstalledContentDialog.ExportSaves.Success.Message"), HeaderFiles.Count, zipPath));
            }
            else
            {
                await _messageBoxService.ShowErrorAsync(
                    LocalizationHelper.GetText("InstalledContentDialog.ExportSaves.Failed.Title"),
                    LocalizationHelper.GetText("InstalledContentDialog.ExportSaves.Failed.Message"));
            }
        }
        catch (Exception ex)
        {
            Logger.Error<ContentViewerDialogViewModel>($"Failed to export save games: {ex.Message}");
            Logger.LogExceptionDetails<ContentViewerDialogViewModel>(ex);
            await _messageBoxService.ShowErrorAsync(
                LocalizationHelper.GetText("InstalledContentDialog.ExportSaves.Failed.Title"),
                string.Format(LocalizationHelper.GetText("InstalledContentDialog.ExportSaves.Failed.Message"), ex.Message));
        }
    }

    /// <summary>
    /// Imports save games from a zip file.
    /// </summary>
    [RelayCommand]
    private async Task ImportSaves()
    {
        if (SelectedAccountContent == null)
        {
            await _messageBoxService.ShowWarningAsync(
                LocalizationHelper.GetText("InstalledContentDialog.ImportSaves.NoAccountSelected.Title"),
                LocalizationHelper.GetText("InstalledContentDialog.ImportSaves.NoAccountSelected.Message"));
            return;
        }

        // Show a file picker for zip files
        Window? topLevel = App.MainWindow;
        if (topLevel == null)
        {
            await _messageBoxService.ShowErrorAsync(
                LocalizationHelper.GetText("InstalledContentDialog.ImportSaves.MainWindow.Error.Title"),
                LocalizationHelper.GetText("InstalledContentDialog.ImportSaves.MainWindow.Error.Message"));
            return;
        }

        IStorageProvider storageProvider = topLevel.StorageProvider;
        IReadOnlyList<IStorageFile> files = await storageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = LocalizationHelper.GetText("InstalledContentDialog.ImportSaves.FilePicker.Title"),
            FileTypeFilter =
            [
                new FilePickerFileType("Xenia Save File")
                {
                    Patterns = ["*.xsave", "*.zip"]
                }
            ],
            AllowMultiple = false
        });

        if (files.Count == 0)
        {
            return; // User cancelled
        }

        IStorageFile zipFile = files[0];

        try
        {
            string zipPath = zipFile.Path.LocalPath;

            // Get the destination path: XeniaContentFolder/XUID/TitleId/
            // Note: TitleId will be extracted from the zip file by SaveManager
            string destinationBase = Path.Combine(SelectedAccountContent.XeniaContentFolder, SelectedAccountContent.XuidHex);

            bool result = await SaveManager.ImportSave(zipPath, destinationBase);

            // Refresh the header files list
            UpdateContentList();

            if (result)
            {
                // Extract TitleId from the zip file name or show generic success
                string titleId = SelectedAccountContent.TitleId;
                await _messageBoxService.ShowInfoAsync(
                    LocalizationHelper.GetText("InstalledContentDialog.ImportSaves.Success.Title"),
                    string.Format(LocalizationHelper.GetText("InstalledContentDialog.ImportSaves.Success.Message"), titleId));
            }
            else
            {
                await _messageBoxService.ShowErrorAsync(
                    LocalizationHelper.GetText("InstalledContentDialog.ImportSaves.Failed.Title"),
                    LocalizationHelper.GetText("InstalledContentDialog.ImportSaves.Failed.Message"));
            }
        }
        catch (Exception ex)
        {
            Logger.Error<ContentViewerDialogViewModel>($"Failed to import save games: {ex.Message}");
            Logger.LogExceptionDetails<ContentViewerDialogViewModel>(ex);
            await _messageBoxService.ShowErrorAsync(
                LocalizationHelper.GetText("InstalledContentDialog.ImportSaves.Failed.Title"),
                string.Format(LocalizationHelper.GetText("InstalledContentDialog.ImportSaves.Failed.Message"), ex.Message));
        }
    }

    /// <summary>
    /// Updates the ProfileGpd TitleEntry with the new achievement count and gamerscore.
    /// </summary>
    /// <param name="achievementDelta">The change in unlocked achievement count (positive or negative).</param>
    /// <param name="gamerscoreDelta">The change in gamerscore (positive or negative).</param>
    private void UpdateProfileGpdTitleEntry(int achievementDelta, int gamerscoreDelta)
    {
        if (SelectedAccountContent?.ProfileGpd == null || SelectedAccountContent.GameAchievementGpdFile == null)
        {
            return;
        }

        // Get the TitleId from the game (stored in the GPD file's achievement entries)
        // The TitleId is the upper 16 bits of the AchievementId
        AchievementViewModel? firstAchievement = Achievements.FirstOrDefault();
        if (firstAchievement == null)
        {
            return;
        }

        // Parse TitleId from hex string to uint
        if (!uint.TryParse(SelectedAccountContent.TitleId, System.Globalization.NumberStyles.HexNumber, null, out uint titleId))
        {
            Logger.Warning<ContentViewerDialogViewModel>($"Failed to parse TitleId: {SelectedAccountContent.TitleId}");
            return;
        }

        // Find the matching TitleEntry in ProfileGpd
        TitleEntry? titleEntry = SelectedAccountContent.ProfileGpd.Titles.FirstOrDefault(t => t.TitleId == titleId);

        if (titleEntry != null)
        {
            titleEntry.AchievementUnlockedCount = Math.Max(0, titleEntry.AchievementUnlockedCount + achievementDelta);
            titleEntry.GamerscoreUnlocked = Math.Max(0, titleEntry.GamerscoreUnlocked + gamerscoreDelta);

            // Update the binary data in the GPD file
            SelectedAccountContent.ProfileGpd.UpdateTitleEntry(titleId, titleEntry);

            // Save the ProfileGpd file
            if (!string.IsNullOrEmpty(SelectedAccountContent.ProfileGpdPath))
            {
                SelectedAccountContent.ProfileGpd.Save(SelectedAccountContent.ProfileGpdPath);
                Logger.Info<ContentViewerDialogViewModel>($"Updated ProfileGpd TitleEntry for {SelectedAccountContent.TitleId}: {titleEntry.AchievementUnlockedCount} achievements, {titleEntry.GamerscoreUnlocked}G");
            }
        }
        else
        {
            Logger.Warning<ContentViewerDialogViewModel>($"TitleEntry not found in ProfileGpd for {SelectedAccountContent.TitleId}");
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ContentViewerDialogViewModel"/> class.
    /// </summary>
    public ContentViewerDialogViewModel()
    {
        _messageBoxService = App.Services.GetRequiredService<IMessageBoxService>();
        Game = new Game();

        ContentTypes = new ObservableCollection<EnumDisplayItem<ContentType>>([
            new EnumDisplayItem<ContentType>(ContentType.SavedGame),
            new EnumDisplayItem<ContentType>(ContentType.Achievements),
            new EnumDisplayItem<ContentType>(ContentType.TitleUpdates),
            new EnumDisplayItem<ContentType>(ContentType.MarketplaceContent)
        ]);

        AccountContents = [];
        GameContents = [];
        HeaderFiles = [];
        SelectedHeaderFiles = [];

        // Select the first content type by default
        if (ContentTypes.Count > 0)
        {
            SelectedContentType = ContentTypes[0];
        }

        // Initialize secret code listener for Konami code detection
        InitializeSecretCodeListener();
    }

    /// <summary>
    /// Initializes the dialog with the provided account contents.
    /// </summary>
    /// <param name="accountContents">List of all account contents (including GameContent).</param>
    /// <param name="game">Game whose content we're showing</param>
    public void Initialize(List<AccountContent> accountContents, Game game)
    {
        Game = game;
        AccountContents.Clear();
        GameContents.Clear();

        foreach (AccountContent content in accountContents)
        {
            if (content is GameContent gameContent)
            {
                GameContents.Add(gameContent);
            }
            else
            {
                AccountContents.Add(content);
            }
        }

        // Select the first account content if available
        if (AccountContents.Count > 0)
        {
            SelectedAccountContent = AccountContents[0];
        }

        // Select the first game content if available
        if (GameContents.Count > 0)
        {
            SelectedGameContent = GameContents[0];
        }

        UpdateAccountVisibility();
        UpdateContentList();
    }

    /// <summary>
    /// Updates the visibility of account and game content selectors based on the selected content type.
    /// </summary>
    private void UpdateAccountVisibility()
    {
        if (SelectedContentType == null)
        {
            IsAccountSelectorVisible = false;
            return;
        }

        ContentType contentType = SelectedContentType.Value;

        // Account selector is visible for SavedGame and Achievements (only if there are accounts)
        IsAccountSelectorVisible = (contentType == ContentType.SavedGame || contentType == ContentType.Achievements) &&
                                   AccountContents.Count > 0;

        // Auto-select the first (and only) GameContent for MarketplaceContent and Installer
        if ((contentType == ContentType.MarketplaceContent || contentType == ContentType.Installer) &&
            GameContents.Count > 0 && SelectedGameContent == null)
        {
            SelectedGameContent = GameContents[0];
        }
    }

    /// <summary>
    /// Updates the header files list based on the selected content type and selected account/game content.
    /// </summary>
    private void UpdateContentList()
    {
        HeaderFiles.Clear();
        Achievements.Clear();

        if (SelectedContentType == null)
        {
            return;
        }

        ContentType contentType = SelectedContentType.Value;

        // Update list visibility based on the content type
        IsAchievementsListVisible = contentType == ContentType.Achievements;
        IsHeaderFilesListVisible = contentType != ContentType.Achievements;

        // Explicitly notify empty state visibility changes
        OnPropertyChanged(nameof(IsAchievementsEmptyStateVisible));
        OnPropertyChanged(nameof(IsHeaderFilesEmptyStateVisible));

        if (contentType == ContentType.Achievements)
        {
            // Load achievements from GPD file
            LoadAchievements();
            return;
        }

        // Get header files based on the content type
        List<HeaderFile> headers = contentType switch
        {
            ContentType.SavedGame => SelectedAccountContent?.SavedGameHeaderFiles ?? [],
            ContentType.MarketplaceContent => SelectedGameContent?.MarketplaceContentHeaderFiles ?? [],
            ContentType.Installer => SelectedGameContent?.InstallerHeaderFiles ?? [],
            _ => []
        };

        // Convert to view models
        foreach (HeaderFile header in headers)
        {
            HeaderFiles.Add(new HeaderFileViewModel(header));
        }
    }

    /// <summary>
    /// Loads achievements from the selected account's GameAchievementGpdFile.
    /// </summary>
    private void LoadAchievements()
    {
        // Reset statistics first
        AchievementCount = 0;
        AchievementUnlockedCount = 0;
        GamerscoreTotal = 0;
        GamerscoreUnlocked = 0;

        // Reset filter to All (localized)
        AchievementFilter = LocalizationHelper.GetText("InstalledContentDialog.AchievementFilter.All");

        if (SelectedAccountContent?.GameAchievementGpdFile == null)
        {
            Logger.Warning<ContentViewerDialogViewModel>("No achievement GPD file found for selected account");
            OnPropertyChanged(nameof(IsAchievementsEmptyStateVisible));
            return;
        }

        try
        {
            List<AchievementEntry> achievements = SelectedAccountContent.GameAchievementGpdFile.Achievements.ToList();
            Logger.Info<ContentViewerDialogViewModel>($"Loaded {achievements.Count} achievements from GPD file");

            // Store all achievements in the unfiltered list
            _allAchievements = achievements
                .Select(a => new AchievementViewModel(a, SelectedAccountContent.GameAchievementGpdFile, AreAchievementFeaturesEnabled))
                .ToList();

            // Apply the current filter (default is "All")
            ApplyAchievementFilter();

            // Load achievement statistics from ProfileGpd TitleEntry
            LoadAchievementStatistics();

            // Notify empty state changed after loading
            OnPropertyChanged(nameof(IsAchievementsEmpty));
            OnPropertyChanged(nameof(IsAchievementsEmptyStateVisible));
        }
        catch (Exception ex)
        {
            Logger.Error<ContentViewerDialogViewModel>($"Failed to load achievements: {ex.Message}");
            Logger.LogExceptionDetails<ContentViewerDialogViewModel>(ex);
        }
    }

    /// <summary>
    /// Applies the current achievement filter to the displayed list.
    /// </summary>
    private void ApplyAchievementFilter()
    {
        Achievements.Clear();

        IEnumerable<AchievementViewModel> filteredAchievements = AchievementFilter switch
        {
            _ when AchievementFilter == LocalizationHelper.GetText("InstalledContentDialog.AchievementFilter.Unlocked") => _allAchievements.Where(a => a.IsUnlocked),
            _ when AchievementFilter == LocalizationHelper.GetText("InstalledContentDialog.AchievementFilter.Locked") => _allAchievements.Where(a => !a.IsUnlocked),
            _ => _allAchievements
        };

        foreach (AchievementViewModel achievement in filteredAchievements)
        {
            Achievements.Add(achievement);
        }

        // Notify empty state and selection commands changed after filtering
        OnPropertyChanged(nameof(IsAchievementsEmpty));
        OnPropertyChanged(nameof(IsAchievementsEmptyStateVisible));
        OnPropertyChanged(nameof(CanSelectAllAchievements));
    }

    /// <summary>
    /// Loads achievement statistics from the ProfileGpd TitleEntry.
    /// </summary>
    private void LoadAchievementStatistics()
    {
        if (SelectedAccountContent?.ProfileGpd == null)
        {
            // No ProfileGpd or TitleId, reset statistics
            AchievementCount = 0;
            AchievementUnlockedCount = 0;
            GamerscoreTotal = 0;
            GamerscoreUnlocked = 0;
            return;
        }

        // Find the TitleEntry for the current game
        TitleEntry? titleEntry = SelectedAccountContent.ProfileGpd.Titles.FirstOrDefault(t => t.TitleId.ToString("X8") == SelectedAccountContent.TitleId);

        if (titleEntry != null)
        {
            AchievementCount = titleEntry.AchievementCount;
            AchievementUnlockedCount = titleEntry.AchievementUnlockedCount;
            GamerscoreTotal = titleEntry.GamerscoreTotal;
            GamerscoreUnlocked = titleEntry.GamerscoreUnlocked;
        }
        else
        {
            // No TitleEntry found, calculate from achievements
            AchievementCount = Achievements.Count;
            AchievementUnlockedCount = Achievements.Count(a => a.IsUnlocked);
            GamerscoreTotal = Achievements.Sum(a => a.Gamerscore);
            GamerscoreUnlocked = Achievements.Sum(a => a.Gamerscore * (a.IsUnlocked ? 1 : 0));
        }
    }

    /// <summary>
    /// Initializes the secret code listener for Konami code detection.
    /// The listener will continue listening until the dialog is closed.
    /// </summary>
    private void InitializeSecretCodeListener()
    {
        if (_secretCodeListener != null)
        {
            Logger.Debug<ContentViewerDialogViewModel>("Secret code listener already initialized");
            return;
        }

        try
        {
            _secretCodeListener = new SecretCodeListener
            {
                AutoStopAfterSuccess = false // Keep listening after code is entered
            };
            _secretCodeListener.KonamiCodeEntered += OnKonamiCodeEntered;
            _secretCodeListener.Start();
            Logger.Info<ContentViewerDialogViewModel>("Secret code listener started for achievements view");
        }
        catch (Exception ex)
        {
            Logger.Error<ContentViewerDialogViewModel>($"Failed to initialize secret code listener: {ex.Message}");
            Logger.LogExceptionDetails<ContentViewerDialogViewModel>(ex);
        }
    }

    /// <summary>
    /// Disposes the secret code listener and stops the input listener.
    /// Called when the dialog is closed or after the Konami code is entered.
    /// </summary>
    public void DisposeSecretCodeListener()
    {
        if (_secretCodeListener == null)
        {
            return;
        }

        try
        {
            _secretCodeListener.KonamiCodeEntered -= OnKonamiCodeEntered;
            _secretCodeListener.Dispose();
            _secretCodeListener = null;
            Logger.Info<ContentViewerDialogViewModel>("Secret code listener and input listener disposed");
        }
        catch (Exception ex)
        {
            Logger.Error<ContentViewerDialogViewModel>($"Failed to dispose secret code listener: {ex.Message}");
            Logger.LogExceptionDetails<ContentViewerDialogViewModel>(ex);
        }
    }

    /// <summary>
    /// Event handler for when the Konami code is entered.
    /// Enables achievement features when the code is detected and stops both listeners.
    /// </summary>
    private void OnKonamiCodeEntered()
    {
        Logger.Info<ContentViewerDialogViewModel>("Konami code detected! Enabling achievement features and stopping listeners");

        AreAchievementFeaturesEnabled = true;

        // Stop and dispose of both SecretCodeListener and InputListener after a successful code entry
        DisposeSecretCodeListener();

        Dispatcher.UIThread.Post(() =>
        {
            _messageBoxService.ShowInfoAsync(
                LocalizationHelper.GetText("ContentViewerDialog.SecretFeature.Title"),
                LocalizationHelper.GetText("ContentViewerDialog.SecretFeature.Message"));
        });
    }
}