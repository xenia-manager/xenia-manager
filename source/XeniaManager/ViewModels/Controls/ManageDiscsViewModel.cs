using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using XeniaManager.Core.Logging;
using XeniaManager.Core.Models.Game;
using XeniaManager.Core.Utilities;
using XeniaManager.Services;

namespace XeniaManager.ViewModels.Controls;

/// <summary>
/// Represents a single disc row shown in the Manage Discs dialog.
/// Disc 1 (the main game file) is shown read-only alongside the editable additional discs.
/// </summary>
public partial class DiscRowViewModel : ObservableObject
{
    /// <summary>1-based disc number.</summary>
    [ObservableProperty] private int _discNumber;

    /// <summary>Display label (custom label if set, otherwise "Disc N").</summary>
    [ObservableProperty] private string _label = string.Empty;

    /// <summary>Full path to this disc's game file.</summary>
    [ObservableProperty] private string _path = string.Empty;

    /// <summary>Whether the file this disc points to currently exists.</summary>
    [ObservableProperty] private bool _isPathValid;

    /// <summary>Disc 1 is the main game file and can't be removed or relabeled from this dialog.</summary>
    public bool IsRemovable => DiscNumber > 1;
}

/// <summary>
/// ViewModel for the Manage Discs dialog, which lets users view, add, rename, and remove
/// the discs associated with a multi-disc game.
/// </summary>
public partial class ManageDiscsViewModel : ObservableObject
{
    private readonly Game _game;
    private readonly IMessageBoxService _messageBoxService;

    /// <summary>The discs currently shown in the dialog (Disc 1 + all additional discs).</summary>
    [ObservableProperty] private ObservableCollection<DiscRowViewModel> _discs = [];

    public string GameTitle => _game.Title;

    public ManageDiscsViewModel(Game game)
    {
        _game = game;
        _messageBoxService = App.Services.GetRequiredService<IMessageBoxService>();
        LoadDiscs();
    }

    /// <summary>
    /// Rebuilds the <see cref="Discs"/> collection from the current state of the game's file locations.
    /// </summary>
    private void LoadDiscs()
    {
        Discs.Clear();

        Discs.Add(new DiscRowViewModel
        {
            DiscNumber = 1,
            Label = LocalizationHelper.GetText("ManageDiscsDialog.Disc1Label"),
            Path = _game.FileLocations.Game,
            IsPathValid = _game.FileLocations.IsGamePathValid
        });

        foreach (GameDisc disc in _game.FileLocations.AdditionalDiscs)
        {
            Discs.Add(new DiscRowViewModel
            {
                DiscNumber = disc.DiscNumber,
                Label = string.IsNullOrWhiteSpace(disc.Label)
                    ? string.Format(LocalizationHelper.GetText("ManageDiscsDialog.DiscNLabel"), disc.DiscNumber)
                    : disc.Label!,
                Path = disc.Path,
                IsPathValid = disc.IsPathValid
            });
        }
    }

    /// <summary>
    /// Opens a file picker and adds the selected file as a new additional disc.
    /// </summary>
    [RelayCommand]
    private async Task AddDiscAsync()
    {
        IStorageProvider? storageProvider = App.MainWindow?.StorageProvider;
        if (storageProvider == null)
        {
            Logger.Warning<ManageDiscsViewModel>("Storage provider is not available");
            await _messageBoxService.ShowErrorAsync(
                LocalizationHelper.GetText("ManageDiscsDialog.MissingStorageProvider.Title"),
                LocalizationHelper.GetText("ManageDiscsDialog.MissingStorageProvider.Message"));
            return;
        }

        FilePickerOpenOptions options = new FilePickerOpenOptions
        {
            Title = LocalizationHelper.GetText("ManageDiscsDialog.FilePicker.Title"),
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
            Logger.Debug<ManageDiscsViewModel>("Add disc selection canceled by user");
            return;
        }

        string selectedPath = files[0].Path.LocalPath;

        // Avoid adding the same file twice (as Disc 1 or as an existing additional disc)
        bool alreadyAdded = _game.FileLocations.Game == selectedPath
                            || _game.FileLocations.AdditionalDiscs.Exists(d => d.Path == selectedPath);
        if (alreadyAdded)
        {
            Logger.Warning<ManageDiscsViewModel>($"'{selectedPath}' is already associated with '{_game.Title}'");
            await _messageBoxService.ShowWarningAsync(
                LocalizationHelper.GetText("ManageDiscsDialog.DuplicateDisc.Title"),
                LocalizationHelper.GetText("ManageDiscsDialog.DuplicateDisc.Message"));
            return;
        }

        int newDiscNumber = _game.FileLocations.DiscCount + 1;
        Logger.Info<ManageDiscsViewModel>($"Adding Disc {newDiscNumber} for '{_game.Title}': {selectedPath}");

        _game.FileLocations.AdditionalDiscs.Add(new GameDisc
        {
            DiscNumber = newDiscNumber,
            Path = selectedPath
        });

        LoadDiscs();
    }

    /// <summary>
    /// Removes the given disc from the game. Disc 1 can't be removed this way.
    /// Renumbers the remaining additional discs to stay contiguous (2, 3, 4, ...).
    /// </summary>
    [RelayCommand]
    private void RemoveDisc(DiscRowViewModel disc)
    {
        if (disc.DiscNumber <= 1)
        {
            Logger.Warning<ManageDiscsViewModel>("Attempted to remove Disc 1, which is not allowed");
            return;
        }

        Logger.Info<ManageDiscsViewModel>($"Removing Disc {disc.DiscNumber} from '{_game.Title}'");

        _game.FileLocations.AdditionalDiscs.RemoveAll(d => d.Path == disc.Path);

        // Renumber remaining additional discs so they stay contiguous
        for (int i = 0; i < _game.FileLocations.AdditionalDiscs.Count; i++)
        {
            _game.FileLocations.AdditionalDiscs[i].DiscNumber = i + 2;
        }

        // Reset LastPlayedDisc if it no longer exists
        if (_game.LastPlayedDisc > _game.FileLocations.DiscCount)
        {
            _game.LastPlayedDisc = 1;
        }

        LoadDiscs();
    }

    /// <summary>
    /// Updates the custom label for an additional disc (Disc 2+).
    /// </summary>
    /// <param name="discNumber">1-based disc number being relabeled.</param>
    /// <param name="newLabel">The new label text.</param>
    public void UpdateDiscLabel(int discNumber, string newLabel)
    {
        if (discNumber <= 1)
        {
            return;
        }

        int index = discNumber - 2;
        if (index < 0 || index >= _game.FileLocations.AdditionalDiscs.Count)
        {
            return;
        }

        _game.FileLocations.AdditionalDiscs[index].Label = string.IsNullOrWhiteSpace(newLabel) ? null : newLabel;
    }
}
