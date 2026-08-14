using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using XeniaManager.BigScreen.Utilities;
using XeniaManager.Core.Models.Files.Config;

namespace XeniaManager.BigScreen.ViewModels.Modals;

/// <summary>
/// ViewModel for a single configuration option in the game settings pane.
/// Port of the desktop's ConfigOptionViewModel with row selection for the
/// controller navigation.
/// </summary>
public partial class ConfigOptionViewModel : ObservableObject, ISelectable, IDisposable
{
    private readonly ConfigOption _configOption;
    private readonly ConfigOptionDefinition? _definition;
    private object? _originalValue;
    private bool _disposed;
    private readonly bool _isInitializing;

    /// <summary>
    /// Whether this option row currently has selection (controller navigation).
    /// </summary>
    [ObservableProperty]
    public partial bool IsSelected { get; set; }

    [ObservableProperty] public partial string Name { get; set; }

    [ObservableProperty] public partial string DisplayName { get; set; }

    [ObservableProperty] public partial string? Comment { get; set; }

    [ObservableProperty] public partial ConfigOptionType Type { get; set; }

    [ObservableProperty] public partial bool IsCommented { get; set; }

    [ObservableProperty] public partial bool IsEditable { get; set; } = true;

    [ObservableProperty] public partial bool IsVisible { get; set; } = true;

    [ObservableProperty] public partial ConfigControlType ControlType { get; set; } = ConfigControlType.Auto;

    [ObservableProperty] public partial bool HasUnsavedChanges { get; set; }

    [ObservableProperty] public partial bool BoolValue { get; set; }

    [ObservableProperty] public partial int IntValue { get; set; }

    [ObservableProperty] public partial double FloatValue { get; set; }

    [ObservableProperty] public partial string StringValue { get; set; } = string.Empty;

    [ObservableProperty] public partial double? Minimum { get; set; }

    [ObservableProperty] public partial double? Maximum { get; set; }

    [ObservableProperty] public partial double? Step { get; set; }

    [ObservableProperty] public partial string? ValueSuffix { get; set; }

    [ObservableProperty] public partial string? ValueFormat { get; set; }

    [ObservableProperty] public partial ObservableCollection<ComboBoxOptionViewModel>? ComboBoxOptions { get; set; }

    [ObservableProperty] public partial int SelectedComboBoxIndex { get; set; } = -1;

    /// <summary>
    /// Whether the toggle-switch control is shown for this option.
    /// </summary>
    public bool IsToggle => ControlType == ConfigControlType.ToggleSwitch;

    /// <summary>
    /// Whether the slider control is shown for this option.
    /// </summary>
    public bool IsSlider => ControlType == ConfigControlType.Slider;

    /// <summary>
    /// Whether the number box control is shown for this option.
    /// </summary>
    public bool IsNumberBox => ControlType == ConfigControlType.NumberBox;

    /// <summary>
    /// Whether the combo box control is shown for this option.
    /// </summary>
    public bool IsComboBox => ControlType == ConfigControlType.ComboBox;

    /// <summary>
    /// Whether the text box control is shown for this option.
    /// </summary>
    public bool IsTextBox => ControlType == ConfigControlType.TextBox;

    partial void OnControlTypeChanged(ConfigControlType value)
    {
        OnPropertyChanged(nameof(IsToggle));
        OnPropertyChanged(nameof(IsSlider));
        OnPropertyChanged(nameof(IsNumberBox));
        OnPropertyChanged(nameof(IsComboBox));
        OnPropertyChanged(nameof(IsTextBox));
    }

    public ConfigOptionViewModel(ConfigOption configOption, ConfigOptionDefinition? definition = null)
    {
        _configOption = configOption;
        _definition = definition;
        _isInitializing = true;
        Name = configOption.Name;
        Comment = definition?.CustomComment ?? (definition?.HideComment == true ? null : configOption.Comment);
        Type = configOption.Type;
        IsCommented = configOption.IsCommented;
        _originalValue = configOption.Value;

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

        InitializeValue(configOption.Value);

        if (ControlType == ConfigControlType.Auto)
        {
            ControlType = Type switch
            {
                ConfigOptionType.Boolean => ConfigControlType.ToggleSwitch,
                ConfigOptionType.Integer => ConfigControlType.Slider,
                ConfigOptionType.Float => ConfigControlType.Slider,
                _ => ConfigControlType.TextBox
            };
        }

        if (definition?.ComboBoxOptions != null)
        {
            ComboBoxOptions = new ObservableCollection<ComboBoxOptionViewModel>();
            int selectedIndex = -1;
            bool isFirstItem = true;

            foreach (KeyValuePair<object, string> pair in definition.ComboBoxOptions)
            {
                ComboBoxOptions.Add(new ComboBoxOptionViewModel(pair.Key, pair.Value));

                if (configOption.Value != null)
                {
                    string configValue = configOption.Value.ToString() ?? string.Empty;
                    string optionValue = pair.Key.ToString() ?? string.Empty;

                    if (string.IsNullOrEmpty(configValue) && isFirstItem)
                    {
                        selectedIndex = 0;
                    }
                    else if (!string.IsNullOrEmpty(configValue) &&
                             configValue.Equals(optionValue, StringComparison.OrdinalIgnoreCase))
                    {
                        selectedIndex = ComboBoxOptions.Count - 1;
                    }
                }

                isFirstItem = false;
            }

            if (string.IsNullOrEmpty(configOption.Value?.ToString()) && selectedIndex == -1 &&
                ComboBoxOptions.Count > 0)
            {
                selectedIndex = 0;
            }

            SelectedComboBoxIndex = selectedIndex;
        }

        if (_definition == null || _definition.IsEditable)
        {
            IsEditable = Type is ConfigOptionType.Boolean or ConfigOptionType.Integer or ConfigOptionType.Float
                             or ConfigOptionType.String
                         || ControlType == ConfigControlType.ComboBox;
        }

        _isInitializing = false;
    }

    /// <summary>
    /// Formats the option name for display (e.g. "apu_max_queued_frames" → "Apu Max Queued Frames").
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

    private void InitializeValue(object? value)
    {
        switch (Type)
        {
            case ConfigOptionType.Boolean:
                BoolValue = value is bool and true;
                break;
            case ConfigOptionType.Integer:
                IntValue = value switch
                {
                    int i => i,
                    long l => (int)l,
                    uint u => (int)u,
                    _ => 0
                };
                FloatValue = IntValue;
                break;
            case ConfigOptionType.Float:
                FloatValue = value switch
                {
                    float f => f,
                    double d => d,
                    _ => 0.0
                };
                break;
            default:
                StringValue = value?.ToString() ?? string.Empty;
                if (Type != ConfigOptionType.String)
                {
                    IsEditable = false;
                    IsVisible = false;
                }

                break;
        }
    }

    /// <summary>
    /// Applies the current ViewModel values back to the underlying ConfigOption.
    /// </summary>
    public void ApplyChanges()
    {
        _configOption.IsCommented = IsCommented;

        if (ControlType == ConfigControlType.ComboBox && ComboBoxOptions != null &&
            SelectedComboBoxIndex >= 0 && SelectedComboBoxIndex < ComboBoxOptions.Count)
        {
            _configOption.Value = ComboBoxOptions[SelectedComboBoxIndex].Value;
        }
        else
        {
            switch (Type)
            {
                case ConfigOptionType.Boolean:
                    _configOption.Value = BoolValue;
                    break;
                case ConfigOptionType.Integer:
                    if (ControlType is ConfigControlType.Slider or ConfigControlType.NumberBox && Step is < 1.0)
                    {
                        _configOption.Value = (float)FloatValue;
                        _configOption.Type = ConfigOptionType.Float;
                    }
                    else
                    {
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
        if (Type != ConfigOptionType.Boolean || _isInitializing || IsSameValue(_configOption.Value, value))
        {
            return;
        }

        _configOption.Value = value;
        CheckForChanges(value);
    }

    partial void OnIntValueChanged(int value)
    {
        if (Type != ConfigOptionType.Integer || _isInitializing || IsSameValue(_configOption.Value, value))
        {
            return;
        }

        _configOption.Value = value;
        CheckForChanges(value);
    }

    partial void OnFloatValueChanged(double value)
    {
        if (_isInitializing || IsSameValue(_configOption.Value, value))
        {
            return;
        }

        if (Type == ConfigOptionType.Float)
        {
            _configOption.Value = (float)value;
            CheckForChanges(value);
        }
        else if (Type == ConfigOptionType.Integer &&
                 ControlType is ConfigControlType.Slider or ConfigControlType.NumberBox)
        {
            if (Step is < 1.0)
            {
                _configOption.Value = (float)value;
                _configOption.Type = ConfigOptionType.Float;
                CheckForChanges(value);
            }
            else
            {
                _configOption.Value = (int)Math.Round(value);
                CheckForChanges((int)Math.Round(value));
            }
        }
    }

    partial void OnSelectedComboBoxIndexChanged(int value)
    {
        if (_isInitializing || value < 0 || ComboBoxOptions == null || value >= ComboBoxOptions.Count)
        {
            return;
        }

        ComboBoxOptionViewModel selectedOption = ComboBoxOptions[value];
        if (IsSameValue(_configOption.Value, selectedOption.Value))
        {
            return;
        }

        _configOption.Value = selectedOption.Value;
        StringValue = selectedOption.Value?.ToString() ?? string.Empty;
        CheckForChanges(selectedOption.Value);
    }

    partial void OnStringValueChanged(string value)
    {
        if (Type != ConfigOptionType.String || ControlType == ConfigControlType.ComboBox || _isInitializing
            || IsSameValue(_configOption.Value, value))
        {
            return;
        }

        _configOption.Value = value;
        CheckForChanges(value);
    }

    /// <summary>
    /// Whether the incoming control value is the same as the option's stored
    /// value (string comparison so int/float/boxed values compare sensibly).
    /// </summary>
    private static bool IsSameValue(object? stored, object? incoming)
    {
        return string.Equals(Convert.ToString(stored, CultureInfo.InvariantCulture),
            Convert.ToString(incoming, CultureInfo.InvariantCulture), StringComparison.Ordinal);
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
    [ObservableProperty] public partial object Value { get; set; }

    [ObservableProperty] public partial string DisplayName { get; set; }

    public ComboBoxOptionViewModel(object value, string displayName)
    {
        Value = value;
        DisplayName = displayName;
    }
}