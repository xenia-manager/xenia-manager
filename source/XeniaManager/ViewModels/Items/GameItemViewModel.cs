using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using XeniaManager.Controls;
using XeniaManager.Core.Database;
using XeniaManager.Core.Files;
using XeniaManager.Core.Logging;
using XeniaManager.Core.Manage;
using XeniaManager.Core.Models.Database.Patches;
using XeniaManager.Core.Models.Game;
using XeniaManager.Core.Utilities;
using XeniaManager.Core.Utilities.Paths;
using XeniaManager.Services;
using XeniaManager.ViewModels.Pages;

namespace XeniaManager.ViewModels.Items;

public partial class GameItemViewModel : ViewModelBase
{
    [ObservableProperty] private Game _game;
    private readonly LibraryPageViewModel _library;
    private IMessageBoxService _messageBoxService { get; set; }

    public string Title => Game.Title;
    public GameArtwork Artwork => Game.Artwork;
    public bool HasBoxart => !string.IsNullOrEmpty(Artwork.Boxart) && Artwork.CachedBoxart != null;

    public bool InstalledPatches => !string.IsNullOrEmpty(Game.FileLocations.Patch);

    public GameItemViewModel(Game game, LibraryPageViewModel library)
    {
        Game = game;
        _library = library;
        _messageBoxService = App.Services.GetRequiredService<IMessageBoxService>();
    }

    [RelayCommand]
    private async Task Launch()
    {
        try
        {
            Logger.Info<GameItemViewModel>($"Launching {Game.Title}...");
            await Launcher.LaunchGameASync(Game);
        }
        catch (Exception ex)
        {
            Logger.Error<GameItemViewModel>($"Failed to launch {Game.Title}");
            Logger.LogExceptionDetails<GameItemViewModel>(ex);
        }
    }

    [RelayCommand]
    private async Task DownloadPatches()
    {
        Logger.Info<GameItemViewModel>($"Initializing patch download for: '{Game.Title}'");
        PatchInfo? selectedPatch;

        // Show the patch selection dialog
        try
        {
            // Load Patches database (Canary & Netplay)
            await PatchesDatabase.LoadCanaryAsync();
            await PatchesDatabase.LoadNetplayAsync();
            Logger.Debug<GameItemViewModel>($"Patches database loaded successfully");

            Logger.Debug<GameItemViewModel>($"Opening patch selection dialog for game ID: '{Game.GameId}'");
            selectedPatch = await PatchSelectionDialog.ShowAsync(Game.GameId);
            if (selectedPatch == null)
            {
                Logger.Info<GameItemViewModel>($"Patch selection cancelled by user for: '{Game.Title}'");
                return;
            }
        }
        catch (Exception ex)
        {
            Logger.Error<GameItemViewModel>($"Failed to open patch selection dialog for: '{Game.Title}'");
            Logger.LogExceptionDetails<GameItemViewModel>(ex);
            await _messageBoxService.ShowErrorAsync(LocalizationHelper.GetText("GameButton.ContextFlyout.Patches.Download.Failed.Title"),
                string.Format(LocalizationHelper.GetText("GameButton.ContextFlyout.Patches.Download.Failed.Message"),
                    ex));
            return;
        }

        // Download the selected patch
        try
        {
            Logger.Info<GameItemViewModel>($"Downloading patch: '{selectedPatch.Name}' for: '{Game.Title}'");
            await PatchManager.DownloadPatchAsync(Game,
                selectedPatch);
            Logger.Info<GameItemViewModel>($"Successfully installed patch: '{selectedPatch.Name}' for: '{Game.Title}'");
            await _messageBoxService.ShowInfoAsync(LocalizationHelper.GetText("GameButton.ContextFlyout.Patches.Download.Success.Title"),
                string.Format(LocalizationHelper.GetText("GameButton.ContextFlyout.Patches.Download.Success.Message"),
                    selectedPatch.Name));
        }
        catch (Exception ex)
        {
            Logger.Error<GameItemViewModel>($"Failed to download and install patch for: '{Game.Title}'");
            Logger.LogExceptionDetails<GameItemViewModel>(ex);
            await _messageBoxService.ShowErrorAsync(LocalizationHelper.GetText("GameButton.ContextFlyout.Patches.Download.Failed.Title"),
                string.Format(LocalizationHelper.GetText("GameButton.ContextFlyout.Patches.Download.Failed.Message"),
                    ex));
        }
    }

    [RelayCommand]
    private async Task InstallLocalPatches()
    {
        Logger.Info<GameItemViewModel>($"Initializing local patch installation for: '{Game.Title}'");

        IStorageProvider? storageProvider;
        // Create a file picker
        FilePickerOpenOptions options = new FilePickerOpenOptions
        {
            Title = LocalizationHelper.GetText("GameButton.ContextFlyout.Patches.InstallLocal.FilePicker.Title"),
            AllowMultiple = false,
            FileTypeFilter = new List<FilePickerFileType>
            {
                new FilePickerFileType("Supported Files")
                {
                    Patterns = ["*.toml"]
                }
            }
        };

        // Check if StorageProvider is available
        try
        {
            // Check if we have StorageProvider
            storageProvider = App.MainWindow?.StorageProvider;
            if (storageProvider == null)
            {
                Logger.Warning<LibraryPageViewModel>("Storage provider is not available");
                // TODO: Custom Exception
                throw new Exception();
            }
        }
        catch (Exception ex)
        {
            Logger.Error<LibraryPageViewModel>("Storage provider is not available");
            Logger.LogExceptionDetails<LibraryPageViewModel>(ex);
            await _messageBoxService.ShowErrorAsync(LocalizationHelper.GetText("GameButton.ContextFlyout.Patches.InstallLocal.FilePicker.MissingStorageProvider.Title"),
                string.Format(LocalizationHelper.GetText("GameButton.ContextFlyout.Patches.InstallLocal.FilePicker.MissingStorageProvider.Message"), ex));
            return;
        }

        // Open the file picker and check if a file was selected
        IReadOnlyList<IStorageFile> files = await storageProvider.OpenFilePickerAsync(options);
        if (files.Count == 0)
        {
            // User canceled the file picker
            Logger.Debug<GameItemViewModel>($"Local patch installation canceled by user for: '{Game.Title}'");
            return;
        }

        // Get the selected file path
        string filePath = files[0].Path.LocalPath;
        Logger.Info<GameItemViewModel>($"Installing local patch from: '{filePath}' for: '{Game.Title}'");

        try
        {
            // Load the patch file to get TitleId and TitleName for display purposes
            PatchFile patchFile = PatchFile.Load(filePath);

            // Show confirmation if TitleId doesn't match
            if (!patchFile.TitleId.Equals(Game.GameId, StringComparison.OrdinalIgnoreCase) &&
                !Game.AlternativeIDs.Any(id => id.Equals(patchFile.TitleId, StringComparison.OrdinalIgnoreCase)))
            {
                string title = LocalizationHelper.GetText("GameButton.ContextFlyout.Patches.InstallLocal.MismatchConfirmation.Title");
                string message = string.Format(LocalizationHelper.GetText("GameButton.ContextFlyout.Patches.InstallLocal.MismatchConfirmation.Message"),
                    patchFile.TitleId,
                    patchFile.TitleName,
                    Game.GameId);

                bool confirmed = await _messageBoxService.ShowConfirmationAsync(title, message);

                if (!confirmed)
                {
                    Logger.Info<GameItemViewModel>($"User canceled installation due to TitleId mismatch for: '{Game.Title}'");
                    return;
                }
            }

            // Install the patch with the already loaded patch file (confirmation already handled above)
            await PatchManager.InstallLocalPatchAsync(Game, patchFile, filePath, (patchTitleId, gameTitleId, patchTitleName) => Task.FromResult(true));

            Logger.Info<GameItemViewModel>($"Successfully installed local patch: '{patchFile.TitleId} - {patchFile.TitleName}' for: '{Game.Title}'");
            await _messageBoxService.ShowInfoAsync(LocalizationHelper.GetText("GameButton.ContextFlyout.Patches.InstallLocal.Success.Title"),
                string.Format(LocalizationHelper.GetText("GameButton.ContextFlyout.Patches.InstallLocal.Success.Message"),
                    $"{patchFile.TitleId} - {patchFile.TitleName}"));
        }
        catch (OperationCanceledException)
        {
            // User canceled due to TitleId mismatch, no error message needed
            Logger.Info<GameItemViewModel>($"Local patch installation canceled due to TitleId mismatch for: '{Game.Title}'");
        }
        catch (Exception ex)
        {
            Logger.Error<GameItemViewModel>($"Failed to install local patch for: '{Game.Title}'");
            Logger.LogExceptionDetails<GameItemViewModel>(ex);
            await _messageBoxService.ShowErrorAsync(
                LocalizationHelper.GetText("GameButton.ContextFlyout.Patches.InstallLocal.Error.Title"),
                string.Format(LocalizationHelper.GetText("GameButton.ContextFlyout.Patches.InstallLocal.Error.Message"),
                    ex));
        }
    }

    [RelayCommand]
    private async Task AddAdditionalPatches()
    {
        Logger.Info<GameItemViewModel>($"Initializing additional patches installation for: '{Game.Title}'");

        // Check if there's a patch file already installed
        if (string.IsNullOrEmpty(Game.FileLocations.Patch))
        {
            Logger.Warning<GameItemViewModel>($"No patch file installed for: '{Game.Title}'");
            await _messageBoxService.ShowWarningAsync(
                LocalizationHelper.GetText("GameButton.ContextFlyout.Patches.AddAdditional.NoPatchInstalled.Title"),
                LocalizationHelper.GetText("GameButton.ContextFlyout.Patches.AddAdditional.NoPatchInstalled.Message"));
            return;
        }

        IStorageProvider? storageProvider;
        // Create a file picker
        FilePickerOpenOptions options = new FilePickerOpenOptions
        {
            Title = LocalizationHelper.GetText("GameButton.ContextFlyout.Patches.AddAdditional.FilePicker.Title"),
            AllowMultiple = false,
            FileTypeFilter = new List<FilePickerFileType>
            {
                new FilePickerFileType("Supported Files")
                {
                    Patterns = ["*.toml"]
                }
            }
        };

        // Check if StorageProvider is available
        try
        {
            storageProvider = App.MainWindow?.StorageProvider;
            if (storageProvider == null)
            {
                Logger.Warning<LibraryPageViewModel>("Storage provider is not available");
                throw new Exception();
            }
        }
        catch (Exception ex)
        {
            Logger.Error<LibraryPageViewModel>("Storage provider is not available");
            Logger.LogExceptionDetails<LibraryPageViewModel>(ex);
            await _messageBoxService.ShowErrorAsync(
                LocalizationHelper.GetText("GameButton.ContextFlyout.Patches.AddAdditional.FilePicker.MissingStorageProvider.Title"),
                string.Format(LocalizationHelper.GetText("GameButton.ContextFlyout.Patches.AddAdditional.FilePicker.MissingStorageProvider.Message"), ex));
            return;
        }

        // Open the file picker and check if a file was selected
        IReadOnlyList<IStorageFile> files = await storageProvider.OpenFilePickerAsync(options);
        if (files.Count == 0)
        {
            // User canceled the file picker
            Logger.Debug<GameItemViewModel>($"Additional patches installation canceled by user for: '{Game.Title}'");
            return;
        }

        // Get the selected file path
        string filePath = files[0].Path.LocalPath;
        Logger.Info<GameItemViewModel>($"Adding additional patches from: '{filePath}' for: '{Game.Title}'");

        try
        {
            // Load the currently installed patch file
            string currentPatchPath = AppPathResolver.GetFullPath(Game.FileLocations.Patch);
            PatchFile currentPatchFile = PatchFile.Load(currentPatchPath);
            Logger.Info<GameItemViewModel>($"Loaded current patch file: TitleId='{currentPatchFile.TitleId}', Patches={currentPatchFile.Patches.Count}");

            // Add additional patches from the selected file
            List<string> addedPatches = await PatchManager.AddAdditionalPatchesAsync(Game, currentPatchFile, filePath);

            Logger.Info<GameItemViewModel>($"Successfully added additional patches from: '{filePath}' for: '{Game.Title}'");

            // Format the list of added patches for display
            string patchList = addedPatches.Count > 0
                ? $"{filePath}\n\nAdded patches:\n" + string.Join("\n", addedPatches.Select(p => $"• {p}"))
                : filePath;

            await _messageBoxService.ShowInfoAsync(
                LocalizationHelper.GetText("GameButton.ContextFlyout.Patches.AddAdditional.Success.Title"),
                string.Format(LocalizationHelper.GetText("GameButton.ContextFlyout.Patches.AddAdditional.Success.Message"), patchList));
        }
        catch (ArgumentException argEx)
        {
            // Hash mismatch or other argument errors
            Logger.Error<GameItemViewModel>($"Incompatible patch file: {argEx.Message}");
            await _messageBoxService.ShowErrorAsync(
                LocalizationHelper.GetText("GameButton.ContextFlyout.Patches.AddAdditional.Incompatible.Title"),
                string.Format(LocalizationHelper.GetText("GameButton.ContextFlyout.Patches.AddAdditional.Incompatible.Message"), argEx.Message));
        }
        catch (FileNotFoundException fnfEx)
        {
            Logger.Error<GameItemViewModel>($"Patch file not found: {fnfEx.Message}");
            await _messageBoxService.ShowErrorAsync(
                LocalizationHelper.GetText("GameButton.ContextFlyout.Patches.AddAdditional.Error.Title"),
                LocalizationHelper.GetText("GameButton.ContextFlyout.Patches.AddAdditional.FileNotFound.Message"));
        }
        catch (Exception ex)
        {
            Logger.Error<GameItemViewModel>($"Failed to add additional patches for: '{Game.Title}'");
            Logger.LogExceptionDetails<GameItemViewModel>(ex);
            await _messageBoxService.ShowErrorAsync(
                LocalizationHelper.GetText("GameButton.ContextFlyout.Patches.AddAdditional.Error.Title"),
                string.Format(LocalizationHelper.GetText("GameButton.ContextFlyout.Patches.AddAdditional.Error.Message"), ex));
        }
    }

    [RelayCommand]
    private async Task ConfigurePatches()
    {
        Logger.Info<GameItemViewModel>($"Initializing patch configuration for: '{Game.Title}'");

        // Check if there's a patch file already installed
        if (string.IsNullOrEmpty(Game.FileLocations.Patch))
        {
            Logger.Warning<GameItemViewModel>($"No patch file installed for: '{Game.Title}'");
            await _messageBoxService.ShowWarningAsync(
                LocalizationHelper.GetText("GameButton.ContextFlyout.Patches.Configure.NoPatchInstalled.Title"),
                LocalizationHelper.GetText("GameButton.ContextFlyout.Patches.Configure.NoPatchInstalled.Message"));
            return;
        }

        try
        {
            // Load the currently installed patch file
            string currentPatchPath = AppPathResolver.GetFullPath(Game.FileLocations.Patch);
            Logger.Info<GameItemViewModel>($"Loading patch file for configuration: '{currentPatchPath}'");

            PatchFile patchFile = PatchFile.Load(currentPatchPath);
            Logger.Info<GameItemViewModel>($"Loaded patch file: TitleId='{patchFile.TitleId}', Patches={patchFile.Patches.Count}");

            // Show the configuration dialog
            bool saved = await PatchConfigurationDialog.ShowAsync(Game.Title, patchFile, currentPatchPath);

            if (saved)
            {
                Logger.Info<GameItemViewModel>($"Patch configuration saved for: '{Game.Title}'");
                await _messageBoxService.ShowInfoAsync(
                    LocalizationHelper.GetText("GameButton.ContextFlyout.Patches.Configure.Success.Title"),
                    LocalizationHelper.GetText("GameButton.ContextFlyout.Patches.Configure.Success.Message"));
            }
            else
            {
                Logger.Info<GameItemViewModel>($"Patch configuration canceled for: '{Game.Title}'");
            }
        }
        catch (Exception ex)
        {
            Logger.Error<GameItemViewModel>($"Failed to configure patches for: '{Game.Title}'");
            Logger.LogExceptionDetails<GameItemViewModel>(ex);
            await _messageBoxService.ShowErrorAsync(
                LocalizationHelper.GetText("GameButton.ContextFlyout.Patches.Configure.Error.Title"),
                string.Format(LocalizationHelper.GetText("GameButton.ContextFlyout.Patches.Configure.Error.Message"), ex));
        }
    }

    [RelayCommand]
    private async Task RemovePatches()
    {
        Logger.Info<GameItemViewModel>($"Initializing patch removal for: '{Game.Title}'");

        try
        {
            await PatchManager.RemovePatchAsync(Game);
            Logger.Info<GameItemViewModel>($"Successfully removed patch for: '{Game.Title}'");
            await _messageBoxService.ShowInfoAsync(
                LocalizationHelper.GetText("GameButton.ContextFlyout.Patches.Remove.Success.Title"),
                LocalizationHelper.GetText("GameButton.ContextFlyout.Patches.Remove.Success.Message"));
        }
        catch (Exception ex)
        {
            Logger.Error<GameItemViewModel>($"Failed to remove patch for: '{Game.Title}'");
            Logger.LogExceptionDetails<GameItemViewModel>(ex);
            await _messageBoxService.ShowErrorAsync(
                LocalizationHelper.GetText("GameButton.ContextFlyout.Patches.Remove.Error.Title"),
                string.Format(LocalizationHelper.GetText("GameButton.ContextFlyout.Patches.Remove.Error.Message"),
                    ex));
        }
    }

    [RelayCommand]
    private async Task RemoveGame()
    {
        if (await _messageBoxService.ShowConfirmationAsync(LocalizationHelper.GetText("GameButton.ContextFlyout.RemoveGame.Confirmation.Title"),
                string.Format(LocalizationHelper.GetText("GameButton.ContextFlyout.RemoveGame.Confirmation.Message"),
                    Game.Title)))
        {
            bool deleteGameContent = await _messageBoxService.ShowConfirmationAsync(LocalizationHelper.GetText("GameButton.ContextFlyout.RemoveGame.Content.Confirmation.Title"),
                string.Format(LocalizationHelper.GetText("GameButton.ContextFlyout.RemoveGame.Content.Confirmation.Message"),
                    Game.Title));
            await Task.Run(() =>
            {
                try
                {
                    Logger.Info<GameItemViewModel>($"Removing {Game.Title}...");
                    GameManager.RemoveGame(Game,
                        deleteGameContent);
                    _library.RefreshLibrary();
                }
                catch (Exception ex)
                {
                    Logger.Error<GameItemViewModel>($"Failed to remove {Game.Title}");
                    Logger.LogExceptionDetails<GameItemViewModel>(ex);
                    // TODO: MessageBox
                }
            });
        }
    }
}