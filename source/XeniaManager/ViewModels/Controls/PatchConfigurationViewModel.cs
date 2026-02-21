using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using XeniaManager.Core.Files;
using XeniaManager.Core.Logging;
using XeniaManager.Core.Models.Patches;
using XeniaManager.Core.Utilities;
using XeniaManager.Services;

namespace XeniaManager.ViewModels.Controls;

/// <summary>
/// ViewModel for a single patch command in the configuration dialog.
/// </summary>
public partial class PatchCommandViewModel : ObservableObject
{
    [ObservableProperty] private PatchType _type;
    [ObservableProperty] private uint _address;
    [ObservableProperty] private string _value = string.Empty;
    [ObservableProperty] private bool _isSelected;

    public PatchCommandViewModel()
    {
    }

    public PatchCommandViewModel(PatchCommand command)
    {
        Type = command.Type;
        Address = command.Address;
        Value = command.GetValueAsString() ?? "0x00";
    }

    public PatchCommand ToPatchCommand()
    {
        return new PatchCommand
        {
            Type = Type,
            Address = Address,
            Value = ParseValue(Value, Type)
        };
    }

    private static object? ParseValue(string value, PatchType type)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        try
        {
            return type switch
            {
                PatchType.Be8 => Convert.ToByte(value.StartsWith("0x") ? value.Substring(2) : value, 16),
                PatchType.Be16 => Convert.ToUInt16(value.StartsWith("0x") ? value.Substring(2) : value, 16),
                PatchType.Be32 => Convert.ToUInt32(value.StartsWith("0x") ? value.Substring(2) : value, 16),
                PatchType.Be64 => Convert.ToUInt64(value.StartsWith("0x") ? value.Substring(2) : value, 16),
                PatchType.F32 => float.Parse(value),
                PatchType.F64 => double.Parse(value),
                PatchType.String or PatchType.U16String => value,
                PatchType.Array => value,
                _ => value
            };
        }
        catch
        {
            return value;
        }
    }
}

/// <summary>
/// ViewModel for a single patch entry in the configuration dialog.
/// </summary>
public partial class PatchEntryViewModel : ObservableObject
{
    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private string _author = string.Empty;
    [ObservableProperty] private string? _description;
    [ObservableProperty] private bool _isEnabled;
    [ObservableProperty] private bool _isExpanded;
    [ObservableProperty] private ObservableCollection<PatchCommandViewModel> _commands = [];
    [ObservableProperty] private PatchEntry _originalEntry;

    public PatchEntryViewModel(PatchEntry entry)
    {
        _originalEntry = entry;
        Name = entry.Name;
        Author = entry.Author;
        Description = entry.Description;
        IsEnabled = entry.IsEnabled;

        foreach (PatchCommand command in entry.Commands)
        {
            Commands.Add(new PatchCommandViewModel(command));
        }
    }

    public PatchEntry ToPatchEntry()
    {
        PatchEntry entry = new PatchEntry
        {
            Name = Name,
            Author = Author,
            Description = Description,
            IsEnabled = IsEnabled
        };

        foreach (PatchCommandViewModel commandVm in Commands)
        {
            entry.Commands.Add(commandVm.ToPatchCommand());
        }

        return entry;
    }

    [RelayCommand]
    private void AddCommand()
    {
        Commands.Add(new PatchCommandViewModel
        {
            Type = PatchType.Be32,
            Address = 0,
            Value = "0x00000000"
        });
    }

    [RelayCommand]
    public void RemoveCommand(PatchCommandViewModel? command)
    {
        if (command != null)
        {
            Commands.Remove(command);
        }
    }
}

/// <summary>
/// ViewModel for the patch configuration dialog.
/// Manages the collection of patches and handles user interactions for editing.
/// </summary>
public partial class PatchConfigurationViewModel : ObservableObject
{
    private readonly PatchFile _patchFile;
    private readonly string _patchFilePath;
    private readonly IMessageBoxService _messageBoxService;

    [ObservableProperty] private string _title = string.Empty;
    [ObservableProperty] private string _titleId = string.Empty;
    [ObservableProperty] private string _titleName = string.Empty;
    [ObservableProperty] private ObservableCollection<PatchEntryViewModel> _patches = [];
    [ObservableProperty] private PatchEntryViewModel? _selectedPatch;
    [ObservableProperty] private bool _hasUnsavedChanges;

    public PatchConfigurationViewModel(PatchFile patchFile, string patchFilePath, IMessageBoxService messageBoxService)
    {
        _patchFile = patchFile;
        _patchFilePath = patchFilePath;
        _messageBoxService = messageBoxService;

        TitleId = patchFile.TitleId;
        TitleName = patchFile.TitleName;
        Title = $"{TitleId} - {TitleName}";

        LoadPatches();
    }

    /// <summary>
    /// Loads all patches from the patch file into the ViewModel.
    /// </summary>
    private void LoadPatches()
    {
        Patches.Clear();

        foreach (PatchEntry entry in _patchFile.Patches)
        {
            Patches.Add(new PatchEntryViewModel(entry));
        }
    }

    /// <summary>
    /// Saves all changes to the patch file.
    /// </summary>
    public async Task<bool> SaveAsync()
    {
        try
        {
            // Clear existing patches and rebuild from ViewModels
            _patchFile.Document.Patches.Clear();

            foreach (PatchEntryViewModel patchVm in Patches)
            {
                _patchFile.Document.Patches.Add(patchVm.ToPatchEntry());
            }

            // Save the file
            _patchFile.Save(_patchFilePath);

            HasUnsavedChanges = false;

            Logger.Info<PatchConfigurationViewModel>($"Successfully saved patch configuration: {_patchFilePath}");
            return true;
        }
        catch (Exception ex)
        {
            Logger.Error<PatchConfigurationViewModel>($"Failed to save patch configuration");
            Logger.LogExceptionDetails<PatchConfigurationViewModel>(ex);

            await _messageBoxService.ShowErrorAsync(LocalizationHelper.GetText("PatchConfigurationDialog.Save.Failed.Title"),
                string.Format(LocalizationHelper.GetText("PatchConfigurationDialog.Save.Failed.Message"), ex));

            return false;
        }
    }

    /// <summary>
    /// Adds a new patch entry.
    /// </summary>
    [RelayCommand]
    private void AddPatch()
    {
        PatchEntryViewModel newPatch = new PatchEntryViewModel(new PatchEntry("New Patch", "Unknown", false, ""))
        {
            IsExpanded = true
        };

        Patches.Add(newPatch);
        SelectedPatch = newPatch;
        HasUnsavedChanges = true;
    }

    /// <summary>
    /// Removes the selected patch entry.
    /// </summary>
    [RelayCommand]
    public async Task RemovePatchAsync(PatchEntryViewModel? patch)
    {
        if (patch == null)
        {
            return;
        }

        bool confirmed = await _messageBoxService.ShowConfirmationAsync(LocalizationHelper.GetText("PatchConfigurationDialog.DeleteCommand.Confirmation.Title"),
            string.Format(LocalizationHelper.GetText("PatchConfigurationDialog.DeleteCommand.Confirmation.Message"), patch.Name));

        if (confirmed)
        {
            Patches.Remove(patch);
            HasUnsavedChanges = true;
        }
    }

    /// <summary>
    /// Marks the configuration as changed when any patch is modified.
    /// </summary>
    partial void OnSelectedPatchChanged(PatchEntryViewModel? value)
    {
        HasUnsavedChanges = true;
    }

    /// <summary>
    /// Saves the patch configuration.
    /// </summary>
    [RelayCommand]
    private async Task DoSaveAsync()
    {
        await SaveAsync();
    }
}