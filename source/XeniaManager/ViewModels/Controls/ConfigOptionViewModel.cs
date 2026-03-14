using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using XeniaManager.Core.Models.Files.Config;

namespace XeniaManager.ViewModels.Controls;

/// <summary>
/// ViewModel for a single configuration option in the config editor.
/// </summary>
public partial class ConfigOptionViewModel : ObservableObject, IDisposable
{
    private readonly ConfigOption _configOption;
    private readonly ConfigOptionDefinition? _definition;
    private object? _originalValue;
    private bool _disposed;
    private bool _isInitializing;

    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private string _displayName = string.Empty;
    [ObservableProperty] private string? _comment;
    [ObservableProperty] private ConfigOptionType _type;
    [ObservableProperty] private bool _isCommented;
    [ObservableProperty] private bool _isEditable = true;
    [ObservableProperty] private bool _isVisible = true;
    [ObservableProperty] private ConfigControlType _controlType = ConfigControlType.Auto;
    [ObservableProperty] private bool _hasUnsavedChanges;

    // Value properties for different types
    [ObservableProperty] private bool _boolValue;
    [ObservableProperty] private int _intValue;
    [ObservableProperty] private double _floatValue;
    [ObservableProperty] private string _stringValue = string.Empty;

    // Control-specific properties
    [ObservableProperty] private double? _minimum;
    [ObservableProperty] private double? _maximum;
    [ObservableProperty] private double? _step;
    [ObservableProperty] private string? _valueSuffix;
    [ObservableProperty] private string? _valueFormat;
    [ObservableProperty] private ObservableCollection<ComboBoxOptionViewModel>? _comboBoxOptions;
    [ObservableProperty] private int _selectedComboBoxIndex = -1;

    public ConfigOptionViewModel(ConfigOption configOption, ConfigOptionDefinition? definition = null)
    {
        _configOption = configOption;
        _definition = definition;
        _isInitializing = true;
        Name = configOption.Name;
        Comment = definition?.CustomComment ?? (definition?.HideComment == true ? null : configOption.Comment);
        Type = configOption.Type;
        IsCommented = configOption.IsCommented;

        // Store original value for change tracking
        _originalValue = configOption.Value;

        // Use definition settings if provided
        if (definition != null)
        {
            DisplayName = definition.DisplayName ?? FormatDisplayName(configOption.Name);
            ControlType = definition.ControlType;
            IsVisible = definition.IsVisible;
            IsEditable = definition.IsEditable;
            Minimum = definition.Minimum;
            Maximum = definition.Maximum;
            Step = definition.Step;
            ValueSuffix = definition.ValueSuffix;
            ValueFormat = definition.ValueFormat;
        }
        else
        {
            DisplayName = FormatDisplayName(configOption.Name);
        }

        // Initialize value based on type (do this BEFORE setting up ComboBox options)
        InitializeValue(configOption.Value);

        // If the control type is Auto, determine it from the value type
        if (ControlType == ConfigControlType.Auto)
        {
            ControlType = Type switch
            {
                ConfigOptionType.Boolean => ConfigControlType.ToggleSwitch,
                ConfigOptionType.Integer => ConfigControlType.Slider,
                ConfigOptionType.Float => ConfigControlType.Slider,
                ConfigOptionType.String => ConfigControlType.TextBox,
                _ => ConfigControlType.TextBox
            };
        }

        // Set up ComboBox options after InitializeValue
        if (definition?.ComboBoxOptions != null)
        {
            ComboBoxOptions = new ObservableCollection<ComboBoxOptionViewModel>();
            int selectedIndex = -1;
            bool isFirstItem = true;

            foreach (KeyValuePair<object, string> kvp in definition.ComboBoxOptions)
            {
                ComboBoxOptionViewModel optionVm = new ComboBoxOptionViewModel(kvp.Key, kvp.Value);
                ComboBoxOptions.Add(optionVm);

                // Select the option that matches the current value
                if (configOption.Value != null)
                {
                    string configValue = configOption.Value.ToString() ?? "";
                    string optionValue = kvp.Key.ToString() ?? "";

                    // If the config value is empty, select the first item
                    if (string.IsNullOrEmpty(configValue) && isFirstItem)
                    {
                        selectedIndex = 0;
                    }
                    // Otherwise, match by value (case-insensitive)
                    else if (!string.IsNullOrEmpty(configValue) && configValue.Equals(optionValue, StringComparison.OrdinalIgnoreCase))
                    {
                        selectedIndex = ComboBoxOptions.Count - 1;
                    }
                }

                isFirstItem = false;
            }

            // If the config value is empty and no selection was made, select the first item
            if (string.IsNullOrEmpty(configOption.Value?.ToString()) && selectedIndex == -1 && ComboBoxOptions.Count > 0)
            {
                selectedIndex = 0;
            }

            // Set the selected index after all options are added
            SelectedComboBoxIndex = selectedIndex;
        }

        // Only allow editing for supported types
        if (_definition == null || _definition.IsEditable)
        {
            IsEditable = Type == ConfigOptionType.Boolean ||
                         Type == ConfigOptionType.Integer ||
                         Type == ConfigOptionType.Float ||
                         Type == ConfigOptionType.String ||
                         ControlType == ConfigControlType.ComboBox;
        }

        _isInitializing = false;
    }

    /// <summary>
    /// Formats the option name for display (e.g., "apu_max_queued_frames" -> "Apu Max Queued Frames").
    /// </summary>
    private static string FormatDisplayName(string name)
    {
        string[] parts = name.Split('_');
        for (int i = 0; i < parts.Length; i++)
        {
            if (parts[i].Length > 0)
            {
                parts[i] = char.ToUpper(parts[i][0]) + parts[i][1..].ToLower();
            }
        }
        return string.Join(" ", parts);
    }

    /// <summary>
    /// Initializes the value properties based on the config option's value and type.
    /// </summary>
    private void InitializeValue(object? value)
    {
        switch (Type)
        {
            case ConfigOptionType.Boolean:
                BoolValue = value is bool b && b;
                break;
            case ConfigOptionType.Integer:
                IntValue = value switch
                {
                    int i => i,
                    long l => (int)l,
                    uint u => (int)u,
                    _ => 0
                };
                FloatValue = IntValue; // Also set FloatValue for slider binding
                break;
            case ConfigOptionType.Float:
                FloatValue = value switch
                {
                    float f => f,
                    double d => d,
                    _ => 0.0
                };
                break;
            case ConfigOptionType.String:
                StringValue = value?.ToString() ?? string.Empty;
                break;
            case ConfigOptionType.Array:
                StringValue = value?.ToString() ?? string.Empty;
                IsEditable = false;
                IsVisible = false;
                break;
            default:
                StringValue = value?.ToString() ?? string.Empty;
                IsEditable = false;
                IsVisible = false;
                break;
        }
    }

    /// <summary>
    /// Applies the current ViewModel values back to the underlying ConfigOption.
    /// </summary>
    public void ApplyChanges()
    {
        _configOption.IsCommented = IsCommented;

        // For ComboBox, always use the selected option's value regardless of type
        if (ControlType == ConfigControlType.ComboBox && ComboBoxOptions != null &&
            SelectedComboBoxIndex >= 0 && SelectedComboBoxIndex < ComboBoxOptions.Count)
        {
            _configOption.Value = ComboBoxOptions[SelectedComboBoxIndex].Value;
        }
        else
        {
            // Apply value based on type for non-ComboBox controls
            switch (Type)
            {
                case ConfigOptionType.Boolean:
                    _configOption.Value = BoolValue;
                    break;
                case ConfigOptionType.Integer:
                    // For integer type with slider control and decimal step, save as float
                    if (ControlType is ConfigControlType.Slider or ConfigControlType.NumberBox && Step is < 1.0)
                    {
                        _configOption.Value = (float)FloatValue;
                        _configOption.Type = ConfigOptionType.Float;
                    }
                    else
                    {
                        // Use FloatValue and round to int (slider binds to FloatValue, not IntValue)
                        _configOption.Value = (int)Math.Round(FloatValue);
                    }
                    break;
                case ConfigOptionType.Float:
                    _configOption.Value = (float)FloatValue;
                    break;
                case ConfigOptionType.String:
                    _configOption.Value = StringValue;
                    break;
            }
        }

        MarkAsSaved();
    }

    /// <summary>
    /// Marks this option as saved (no pending changes).
    /// </summary>
    public void MarkAsSaved()
    {
        _originalValue = _configOption.Value;
        HasUnsavedChanges = false;
    }

    partial void OnBoolValueChanged(bool value)
    {
        if (Type == ConfigOptionType.Boolean && !_isInitializing)
        {
            _configOption.Value = value;
            CheckForChanges(value);
        }
    }

    partial void OnIntValueChanged(int value)
    {
        if (Type == ConfigOptionType.Integer && !_isInitializing)
        {
            _configOption.Value = value;
            CheckForChanges(value);
        }
    }

    partial void OnFloatValueChanged(double value)
    {
        // Handle both Float and Integer types (slider uses FloatValue for both)
        if (_isInitializing)
        {
            return;
        }

        if (Type == ConfigOptionType.Float)
        {
            _configOption.Value = (float)value;
            CheckForChanges(value);
        }
        else if (Type == ConfigOptionType.Integer && ControlType is ConfigControlType.Slider or ConfigControlType.NumberBox)
        {
            // Check if the step indicates decimal precision (e.g., 0.01)
            if (Step is < 1.0)
            {
                // Save as float for decimal sliders
                _configOption.Value = (float)value;
                _configOption.Type = ConfigOptionType.Float;
                CheckForChanges(value);
            }
            else
            {
                // Save as int for integer sliders
                _configOption.Value = (int)Math.Round(value);
                CheckForChanges((int)Math.Round(value));
            }
        }
    }

    partial void OnSelectedComboBoxIndexChanged(int value)
    {
        if (_isInitializing)
        {
            return;
        }

        if (value >= 0 && ComboBoxOptions != null && value < ComboBoxOptions.Count)
        {
            ComboBoxOptionViewModel selectedOption = ComboBoxOptions[value];
            _configOption.Value = selectedOption.Value;
            StringValue = selectedOption.Value?.ToString() ?? string.Empty;
            CheckForChanges(selectedOption.Value);
        }
    }

    partial void OnStringValueChanged(string value)
    {
        if (Type == ConfigOptionType.String && ControlType != ConfigControlType.ComboBox && !_isInitializing)
        {
            _configOption.Value = value;
            CheckForChanges(value);
        }
    }

    /// <summary>
    /// Checks if the current value differs from the original and updates HasUnsavedChanges.
    /// </summary>
    private void CheckForChanges(object? newValue)
    {
        HasUnsavedChanges = !Equals(newValue, _originalValue);
    }

    /// <summary>
    /// Disposes of resources used by this ViewModel.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        ComboBoxOptions?.Clear();
        _disposed = true;
    }
}

/// <summary>
/// ViewModel for a combo box option.
/// </summary>
public partial class ComboBoxOptionViewModel : ObservableObject
{
    [ObservableProperty] private object _value;
    [ObservableProperty] private string _displayName;

    public ComboBoxOptionViewModel(object value, string displayName)
    {
        Value = value;
        DisplayName = displayName;
    }
}