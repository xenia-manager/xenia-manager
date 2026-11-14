using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using XeniaManager.Files;
using XeniaManager.Files.Utilities;
using XeniaManager.Logging;
using XeniaManager.Files.Models.Patches;
using XeniaManager.Core.Utilities;
using XeniaManager.Services;

namespace XeniaManager.ViewModels.Controls;

/// <summary>
/// ViewModel for a single patch command in the configuration dialog.
/// </summary>
public partial class PatchCommandViewModel : ObservableObject
{
    [ObservableProperty] private PatchType _type;
    [ObservableProperty] private ulong _address;
    [ObservableProperty] private string _value = string.Empty;
    [ObservableProperty] private bool _isSelected;
    [ObservableProperty] private bool _isValid = true;
    [ObservableProperty] private string _validationError = string.Empty;
    [ObservableProperty] private PatchConfigurationViewModel? _parentViewModel;
    [ObservableProperty] private string? _typeComment;
    [ObservableProperty] private string? _addressComment;
    [ObservableProperty] private string? _valueComment;
    [ObservableProperty] private bool? _useArrayPrefix;
    private readonly PatchCommand? _originalCommand;
    private readonly string? _originalDisplayValue;
    private readonly string? _originalValueComment;
    private readonly bool? _originalUseArrayPrefix;

    public PatchCommandViewModel()
    {
    }

    public PatchCommandViewModel(PatchCommand command, PatchConfigurationViewModel? parentViewModel = null)
    {
        _originalCommand = command;
        _originalValueComment = command.ValueComment;
        _originalUseArrayPrefix = command.UseArrayPrefix;
        _parentViewModel = parentViewModel;
        Type = command.Type;
        Address = command.Address;
        _typeComment = command.TypeComment;
        _addressComment = command.AddressComment;
        _valueComment = command.ValueComment;
        _useArrayPrefix = command.UseArrayPrefix;

        string? rawValue;
        if (Type is PatchType.String or PatchType.U16String)
        {
            rawValue = command.Value as string ?? string.Empty;
        }
        else
        {
            rawValue = command.GetValueAsString();

            // Strip quotes from array values for display in the textbox
            if (Type is PatchType.Array && rawValue != null)
            {
                string trimmed = rawValue.Trim();
                if ((trimmed.StartsWith("\"") && trimmed.EndsWith("\"")) ||
                    (trimmed.StartsWith("'") && trimmed.EndsWith("'")))
                {
                    rawValue = trimmed.Substring(1, trimmed.Length - 2);
                }
            }
        }

        Value = rawValue ?? "0x00";
        _originalDisplayValue = Value;
        Validate();
    }

    /// <summary>
    /// Called when the Type property changes to re-validate the value.
    /// </summary>
    partial void OnTypeChanged(PatchType value) => Validate();

    /// <summary>
    /// Called when the Value property changes to validate the new value.
    /// </summary>
    partial void OnValueChanged(string value) => Validate();

    /// <summary>
    /// Validates the current value against the patch type format requirements.
    /// </summary>
    /// <returns>True if the value is valid for the current patch type, false otherwise.</returns>
    public bool Validate()
    {
        (IsValid, ValidationError) = ValidateValue(Value, Type);
        return IsValid;
    }

    /// <summary>
    /// Validates a value against the specified patch type format requirements.
    /// </summary>
    /// <param name="value">The value to validate.</param>
    /// <param name="type">The patch type to validate against.</param>
    /// <returns>A tuple containing (isValid, errorMessage).</returns>
    private static (bool IsValid, string ErrorMessage) ValidateValue(string value, PatchType type)
    {
        if (type is PatchType.String or PatchType.U16String)
        {
            return (true, string.Empty);
        }

        if (string.IsNullOrWhiteSpace(value))
        {
            return (false, "Value cannot be empty");
        }

        if (PatchValueParser.TryParse(value, type, out _))
        {
            return (true, string.Empty);
        }

        string error = type switch
        {
            PatchType.Array => "Invalid array format. Use hex bytes (e.g., 0x0102030405)",
            PatchType.F32 => "Invalid single-precision float value (e.g., 1.0, -3.14, inf, 0x60000000)",
            PatchType.F64 => "Invalid double-precision float value (e.g., 1.0, -3.14, inf, 0x60000000)",
            _ => "Invalid integer value (e.g., 0x60000000, 123, -1, 0b1010)"
        };
        return (false, error);
    }

    public PatchCommand ToPatchCommand()
    {
        PatchCommand command = new PatchCommand
        {
            Type = Type,
            Address = Address,
            Value = ParseValue(Value, Type),
            TypeComment = TypeComment,
            AddressComment = AddressComment,
            ValueComment = ValueComment,
            UseArrayPrefix = UseArrayPrefix
        };

        if (_originalCommand != null)
        {
            command.LeadingComments = [.. _originalCommand.LeadingComments];
            command.ValueLeadingComments = [.. _originalCommand.ValueLeadingComments];

            if (Type == _originalCommand.Type &&
                string.Equals(Value, _originalDisplayValue, StringComparison.Ordinal))
            {
                // Keep the original numeric literal even when only the comment
                // or one of the other fields changed; otherwise a comment edit
                // would silently round the value.
                command.RawValue = _originalCommand.RawValue;

                if (string.Equals(ValueComment, _originalValueComment, StringComparison.Ordinal) &&
                    UseArrayPrefix == _originalUseArrayPrefix)
                {
                    command.RawValueLineSuffix = _originalCommand.RawValueLineSuffix;
                }
            }
        }

        return command;
    }

    private static object? ParseValue(string value, PatchType type)
    {
        if (string.IsNullOrWhiteSpace(value) && type is not (PatchType.String or PatchType.U16String))
        {
            return null;
        }

        return PatchValueParser.TryParse(value, type, out object? parsed) ? parsed : value;
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
    [ObservableProperty] private string? _note;
    [ObservableProperty] private bool _isEnabled;
    [ObservableProperty] private bool _isExpanded;
    [ObservableProperty] private string? _headerComment;
    [ObservableProperty] private ObservableCollection<PatchCommandViewModel> _commands = [];
    [ObservableProperty] private PatchEntry _originalEntry;
    [ObservableProperty] private PatchConfigurationViewModel? _parentViewModel;

    public PatchEntryViewModel(PatchEntry entry, PatchConfigurationViewModel? parentViewModel = null)
    {
        _originalEntry = entry;
        _parentViewModel = parentViewModel;
        Name = entry.Name;
        Author = entry.Author;
        Description = entry.Description;
        Note = entry.Note;
        IsEnabled = entry.IsEnabled;
        HeaderComment = entry.HeaderComment;

        foreach (PatchCommand command in entry.Commands)
        {
            Commands.Add(new PatchCommandViewModel(command, parentViewModel));
        }
    }

    public PatchEntry ToPatchEntry()
    {
        PatchEntry entry = new PatchEntry
        {
            Name = Name,
            Author = Author,
            Description = Description,
            Note = Note,
            IsEnabled = IsEnabled,
            HeaderComment = HeaderComment,
            LeadingComments = [.. OriginalEntry.LeadingComments],
            PostMetadataComments = [.. OriginalEntry.PostMetadataComments],
            SourceIsEnabledLineIndex = OriginalEntry.SourceIsEnabledLineIndex
        };

        foreach (PatchCommandViewModel commandVm in Commands)
        {
            entry.Commands.Add(commandVm.ToPatchCommand());
        }

        if (string.Equals(Name, OriginalEntry.Name, StringComparison.Ordinal))
        {
            entry.NameLineSuffix = OriginalEntry.NameLineSuffix;
        }

        if (string.Equals(Author, OriginalEntry.Author, StringComparison.Ordinal))
        {
            entry.AuthorLineSuffix = OriginalEntry.AuthorLineSuffix;
        }

        if (string.Equals(Description, OriginalEntry.Description, StringComparison.Ordinal))
        {
            entry.DescriptionLineSuffix = OriginalEntry.DescriptionLineSuffix;
        }

        if (string.Equals(Note, OriginalEntry.Note, StringComparison.Ordinal))
        {
            entry.NoteLineSuffix = OriginalEntry.NoteLineSuffix;
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
            Value = "0x00000000",
            ParentViewModel = ParentViewModel
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
    [ObservableProperty] private bool _isEditingEnabled;

    public PatchConfigurationViewModel(PatchFile patchFile, string patchFilePath, IMessageBoxService messageBoxService)
    {
        _patchFile = patchFile;
        _patchFilePath = patchFilePath;
        _messageBoxService = messageBoxService;

        TitleId = patchFile.TitleId;
        TitleName = patchFile.TitleName;
        Title = $"{TitleId} - {TitleName}";

        IsEditingEnabled = false;

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
            Patches.Add(new PatchEntryViewModel(entry, this));
        }
    }

    /// <summary>
    /// Saves all changes to the patch file.
    /// </summary>
    public async Task<bool> SaveAsync()
    {
        // Validate all patch commands before saving
        List<(string PatchName, string CommandType, string Address)> invalidCommands = [];

        foreach (PatchEntryViewModel patchVm in Patches)
        {
            foreach (PatchCommandViewModel commandVm in patchVm.Commands)
            {
                if (!commandVm.Validate())
                {
                    invalidCommands.Add((patchVm.Name, commandVm.Type.ToString(), $"0x{commandVm.Address:X8}"));
                }
            }
        }

        if (invalidCommands.Count > 0)
        {
            // Build the error message using localized strings
            string errorMessage = string.Format(LocalizationHelper.GetText("PatchConfigurationDialog.Save.InvalidValues.Header"), invalidCommands.Count) +
                                  "\n\n";

            foreach ((string PatchName, string CommandType, string Address) invalid in invalidCommands.Take(5))
            {
                errorMessage += string.Format(LocalizationHelper.GetText("PatchConfigurationDialog.Save.InvalidValues.Entry"),
                    invalid.PatchName, invalid.CommandType, invalid.Address) + "\n";
            }

            if (invalidCommands.Count > 5)
            {
                errorMessage += "\n" + string.Format(LocalizationHelper.GetText("PatchConfigurationDialog.Save.InvalidValues.More"),
                    invalidCommands.Count - 5);
            }

            await _messageBoxService.ShowErrorAsync(LocalizationHelper.GetText("PatchConfigurationDialog.Save.InvalidValues.Title"),
                errorMessage);

            return false;
        }

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
        PatchEntryViewModel newPatch = new PatchEntryViewModel(new PatchEntry("New Patch", "Unknown", false, ""), this)
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

        bool confirmed = await _messageBoxService.ShowConfirmationAsync(LocalizationHelper.GetText("PatchConfigurationDialog.RemovePatch.Title"),
            string.Format(LocalizationHelper.GetText("PatchConfigurationDialog.RemovePatch.Message"), patch.Name));

        if (confirmed)
        {
            Patches.Remove(patch);
            HasUnsavedChanges = true;
        }
    }

    /// <summary>
    /// Marks the configuration as changed when any patch is modified.
    /// </summary>
    partial void OnSelectedPatchChanged(PatchEntryViewModel? value) => HasUnsavedChanges = true;

    /// <summary>
    /// Saves the patch configuration.
    /// </summary>
    [RelayCommand]
    private async Task DoSaveAsync() => await SaveAsync();
}