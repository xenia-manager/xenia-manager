using System;
using System.Globalization;
using System.Text.RegularExpressions;
using CommunityToolkit.Mvvm.ComponentModel;
using XeniaManager.BigScreen.Utilities;
using XeniaManager.Core.Models.Files.Patches;

namespace XeniaManager.BigScreen.ViewModels.Items;

/// <summary>
/// ViewModel for a single patch command: type, address and value with
/// per-type validation, plus the conversion back to a Core command.
/// </summary>
public partial class PatchCommandItemViewModel : ObservableObject, ISelectable
{
    private readonly string? _typeComment;
    private readonly string? _addressComment;
    private readonly string? _valueComment;
    private readonly bool? _useArrayPrefix;

    /// <summary>
    /// The command's patch type (be8 … string/array).
    /// </summary>
    [ObservableProperty] private PatchType _type;

    /// <summary>
    /// The command's memory address.
    /// </summary>
    [ObservableProperty] private ulong _address;

    /// <summary>
    /// The command's value as a display string (hex, float, quoted string…).
    /// </summary>
    [ObservableProperty] private string _value = string.Empty;

    /// <summary>
    /// Whether this row currently has selection in the command list.
    /// </summary>
    [ObservableProperty] private bool _isSelected;

    /// <summary>
    /// Whether the current value passes validation for the selected type.
    /// </summary>
    [ObservableProperty] private bool _isValid = true;

    /// <summary>
    /// The validation error for the current value, or empty when valid.
    /// </summary>
    [ObservableProperty] private string _validationError = string.Empty;

    /// <summary>
    /// The address formatted as a hex literal for the editor.
    /// </summary>
    public string AddressText
    {
        get => $"0x{Address:X8}";
        set
        {
            string hex = value.Trim().StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? value[2..] : value;
            if (ulong.TryParse(hex, NumberStyles.HexNumber, null, out ulong parsed))
            {
                Address = parsed;
            }
        }
    }

    public PatchCommandItemViewModel()
    {
        Type = PatchType.Be32;
        Address = 0;
        Value = "0x00000000";
        Validate();
    }

    public PatchCommandItemViewModel(PatchCommand command)
    {
        _typeComment = command.TypeComment;
        _addressComment = command.AddressComment;
        _valueComment = command.ValueComment;
        _useArrayPrefix = command.UseArrayPrefix;
        Type = command.Type;
        Address = command.Address;

        string? rawValue = command.GetValueAsString();
        if (Type is PatchType.String or PatchType.U16String or PatchType.Array && rawValue != null)
        {
            string trimmed = rawValue.Trim();
            if ((trimmed.StartsWith("\"") && trimmed.EndsWith("\"")) ||
                (trimmed.StartsWith("'") && trimmed.EndsWith("'")))
            {
                rawValue = trimmed[1..^1];
            }
        }

        Value = rawValue ?? "0x00";
        Validate();
    }

    partial void OnTypeChanged(PatchType value) => Validate();

    partial void OnValueChanged(string value) => Validate();

    /// <summary>
    /// Validates the current value against the patch type format requirements.
    /// </summary>
    public bool Validate()
    {
        (IsValid, ValidationError) = ValidateValue(Value, Type);
        return IsValid;
    }

    /// <summary>
    /// Converts this view model back to a Core patch command, preserving the
    /// untouched comment and array-prefix values.
    /// </summary>
    public PatchCommand ToPatchCommand()
    {
        return new PatchCommand
        {
            Type = Type,
            Address = Address,
            Value = ParseValue(Value, Type),
            TypeComment = _typeComment,
            AddressComment = _addressComment,
            ValueComment = _valueComment,
            UseArrayPrefix = _useArrayPrefix,
        };
    }

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
            _ => (true, string.Empty),
        };
    }

    private static (bool IsValid, string ErrorMessage) ValidateHex(string value, int expectedBytes, string typeName)
    {
        string hexValue = value.Trim().StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? value[2..] : value;
        if (!Regex.IsMatch(hexValue, "^[0-9A-Fa-f]+$"))
        {
            return (false, $"Invalid hexadecimal format for {typeName}");
        }

        int maxHexChars = expectedBytes * 2;
        if (hexValue.Length > maxHexChars)
        {
            return (false, $"Value exceeds maximum size for {typeName} (max {maxHexChars} hex characters)");
        }

        return (true, string.Empty);
    }

    private static (bool IsValid, string ErrorMessage) ValidateArray(string value)
    {
        string arrayValue = value.Trim();
        if (arrayValue.StartsWith("\"") && arrayValue.EndsWith("\""))
        {
            arrayValue = arrayValue[1..^1];
        }

        if (arrayValue.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            string cleanedHex = arrayValue[2..].Replace(" ", "");
            if (!Regex.IsMatch(cleanedHex, "^[0-9A-Fa-f]*$"))
            {
                return (false, "Invalid array format. Use hex bytes (e.g., 0x0102030405)");
            }

            if (cleanedHex.Length % 2 != 0)
            {
                return (false, "Array hex must have even number of characters");
            }
        }
        else
        {
            string cleanedHex = arrayValue.Replace(" ", "");
            if (cleanedHex.Length == 0)
            {
                return (true, string.Empty);
            }

            if (!Regex.IsMatch(cleanedHex, "^[0-9A-Fa-f]+$"))
            {
                return (false, "Invalid array format. Use hex bytes (e.g., 0102030405)");
            }

            if (cleanedHex.Length % 2 != 0)
            {
                return (false, "Array hex must have even number of characters");
            }
        }

        return (true, string.Empty);
    }

    private static (bool IsValid, string ErrorMessage) ValidateFloat(string value, string precision)
    {
        if (!float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out _))
        {
            return (false, $"Invalid {precision}-precision float value (e.g., 1.0, -3.14)");
        }

        return (true, string.Empty);
    }

    private static byte[] ParseArrayString(string value)
    {
        string input = value.Trim();
        if (input.StartsWith("\"") && input.EndsWith("\""))
        {
            input = input[1..^1];
        }

        string hex = input.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? input[2..] : input;
        hex = hex.Replace(" ", "");
        if (hex.Length == 0 || hex.Length % 2 != 0)
        {
            return [];
        }

        byte[] bytes = new byte[hex.Length / 2];
        for (int i = 0; i < bytes.Length; i++)
        {
            if (!byte.TryParse(hex.Substring(i * 2, 2), NumberStyles.HexNumber, null, out bytes[i]))
            {
                return [];
            }
        }

        return bytes;
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
                PatchType.Be8 => Convert.ToByte(value.StartsWith("0x") ? value[2..] : value, 16),
                PatchType.Be16 => Convert.ToUInt16(value.StartsWith("0x") ? value[2..] : value, 16),
                PatchType.Be32 => Convert.ToUInt32(value.StartsWith("0x") ? value[2..] : value, 16),
                PatchType.Be64 => Convert.ToUInt64(value.StartsWith("0x") ? value[2..] : value, 16),
                PatchType.F32 => float.Parse(value, CultureInfo.InvariantCulture),
                PatchType.F64 => double.Parse(value, CultureInfo.InvariantCulture),
                PatchType.String or PatchType.U16String => value,
                PatchType.Array => ParseArrayString(value),
                _ => value,
            };
        }
        catch
        {
            return value;
        }
    }
}
