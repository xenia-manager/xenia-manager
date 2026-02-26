using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using XeniaManager.Core.Files;
using XeniaManager.Core.Logging;
using XeniaManager.Core.Models.Files.Patches;
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
    [ObservableProperty] private bool _isValid = true;
    [ObservableProperty] private string _validationError = string.Empty;

    public PatchCommandViewModel()
    {
    }

    public PatchCommandViewModel(PatchCommand command)
    {
        Type = command.Type;
        Address = command.Address;
        string? rawValue = command.GetValueAsString();

        // Strip quotes from string values for display in the textbox
        if (Type is PatchType.String or PatchType.U16String && rawValue != null)
        {
            string trimmed = rawValue.Trim();
            if ((trimmed.StartsWith("\"") && trimmed.EndsWith("\"")) ||
                (trimmed.StartsWith("'") && trimmed.EndsWith("'")))
            {
                rawValue = trimmed.Substring(1, trimmed.Length - 2);
            }
        }

        Value = rawValue ?? "0x00";
        Validate();
    }

    /// <summary>
    /// Called when the Type property changes to re-validate the value.
    /// </summary>
    partial void OnTypeChanged(PatchType value)
    {
        Validate();
    }

    /// <summary>
    /// Called when the Value property changes to validate the new value.
    /// </summary>
    partial void OnValueChanged(string value)
    {
        Validate();
    }

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
        if (string.IsNullOrWhiteSpace(value))
        {
            return (false, "Value cannot be empty");
        }

        return type switch
        {
            PatchType.Be8 => ValidateHex(value, 1, "be8"),
            PatchType.Be16 => ValidateHex(value, 2, "be16"),
            PatchType.Be32 => ValidateHex(value, 4, "be32"),
            PatchType.Be64 => ValidateHex(value, 8, "be64"),
            PatchType.Array => ValidateArray(value),
            PatchType.F32 => ValidateFloat(value, "single"),
            PatchType.F64 => ValidateFloat(value, "double"),
            PatchType.String => ValidateString(value, "UTF-8"),
            PatchType.U16String => ValidateString(value, "UTF-16"),
            _ => (true, string.Empty)
        };
    }

    /// <summary>
    /// Validates a hexadecimal value for be8, be16, be32, and be64 types.
    /// </summary>
    /// <param name="value">The hex value to validate (e.g., "0x00", "0x0000", "0x00000000").</param>
    /// <param name="expectedBytes">The expected number of bytes for the type.</param>
    /// <param name="typeName">The type name for error messages.</param>
    /// <returns>A tuple containing (isValid, errorMessage).</returns>
    private static (bool IsValid, string ErrorMessage) ValidateHex(string value, int expectedBytes, string typeName)
    {
        string hexValue = value.Trim().StartsWith("0x", StringComparison.OrdinalIgnoreCase)
            ? value.Substring(2)
            : value;

        // Check if it's a valid hex string
        if (!Regex.IsMatch(hexValue, "^[0-9A-Fa-f]+$"))
        {
            return (false, $"Invalid hexadecimal format for {typeName}");
        }

        // Check if the hex value fits within the expected byte size
        int maxHexChars = expectedBytes * 2;
        if (hexValue.Length > maxHexChars)
        {
            return (false, $"Value exceeds maximum size for {typeName} (max {maxHexChars} hex characters)");
        }

        return (true, string.Empty);
    }

    /// <summary>
    /// Validates an array value (hex string of any size).
    /// </summary>
    /// <param name="value">The array value to validate (e.g., "0x01 0x02 0x03").</param>
    /// <returns>A tuple containing (isValid, errorMessage).</returns>
    private static (bool IsValid, string ErrorMessage) ValidateArray(string value)
    {
        string arrayValue = value.Trim();

        // Array can be specified as "0x##*" format or space-separated hex bytes
        if (arrayValue.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            string hexPart = arrayValue.Substring(2);
            // Allow space-separated hex bytes or continuous hex string
            string cleanedHex = hexPart.Replace(" ", "");
            if (!Regex.IsMatch(cleanedHex, "^[0-9A-Fa-f]*$"))
            {
                return (false, "Invalid array format. Use space-separated hex bytes (e.g., 0x01 0x02 0x03)");
            }
        }
        else
        {
            // Space-separated hex bytes without 0x prefix
            string[] bytes = arrayValue.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            foreach (string byteStr in bytes)
            {
                if (!Regex.IsMatch(byteStr, "^[0-9A-Fa-f]{1,2}$"))
                {
                    return (false, $"Invalid byte value in array: {byteStr}");
                }
            }
        }

        return (true, string.Empty);
    }

    /// <summary>
    /// Validates a floating-point value for f32 and f64 types.
    /// </summary>
    /// <param name="value">The float value to validate (e.g., "1.0").</param>
    /// <param name="precision">The precision type ("single" for f32, "double" for f64).</param>
    /// <returns>A tuple containing (isValid, errorMessage).</returns>
    private static (bool IsValid, string ErrorMessage) ValidateFloat(string value, string precision)
    {
        if (!float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out _))
        {
            return (false, $"Invalid {precision}-precision float value (e.g., 1.0, -3.14)");
        }

        return (true, string.Empty);
    }

    /// <summary>
    /// Validates a string value for string and u16string types.
    /// </summary>
    /// <param name="value">The string value to validate.</param>
    /// <param name="encoding">The expected encoding ("UTF-8" or "UTF-16").</param>
    /// <returns>A tuple containing (isValid, errorMessage).</returns>
    private static (bool IsValid, string ErrorMessage) ValidateString(string value, string encoding)
    {
        // Strings must be enclosed in quotes
        string trimmedValue = value.Trim();
        if ((trimmedValue.StartsWith("\"") && trimmedValue.EndsWith("\"")) ||
            (trimmedValue.StartsWith("'") && trimmedValue.EndsWith("'")))
        {
            return (true, string.Empty);
        }

        // Also allow unquoted strings (they will be treated as-is)
        return (true, string.Empty);
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
                PatchType.F32 => float.Parse(value, System.Globalization.CultureInfo.InvariantCulture),
                PatchType.F64 => double.Parse(value, System.Globalization.CultureInfo.InvariantCulture),
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
            string errorMessage = string.Format(LocalizationHelper.GetText("PatchConfigurationDialog.Save.InvalidValues.Header"), invalidCommands.Count) + "\n\n";

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