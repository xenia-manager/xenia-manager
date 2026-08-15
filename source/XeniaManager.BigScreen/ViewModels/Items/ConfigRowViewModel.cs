using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using XeniaManager.BigScreen.Utilities;
using XeniaManager.Core.Models.Files.Config;

namespace XeniaManager.BigScreen.ViewModels.Items;

/// <summary>
/// A combo box option shown in a curated config row.
/// </summary>
public partial class ComboBoxOptionViewModel : ObservableObject
{
    /// <summary>
    /// The underlying config value.
    /// </summary>
    public object Value { get; }

    /// <summary>
    /// The human-readable option name.
    /// </summary>
    public string DisplayName { get; }

    public ComboBoxOptionViewModel(object value, string displayName)
    {
        Value = value;
        DisplayName = displayName;
    }
}

/// <summary>
/// A curated config row in the game settings pane: label, section header and
/// the config option it edits, rendered as a main-settings-style card
/// (toggle / slider / combo box). Values commit straight to the config
/// option; <see cref="ValueChanged"/> fires when a commit happened.
/// </summary>
public partial class ConfigRowViewModel : ObservableObject, ISelectable
{
    private readonly ConfigOption _option;
    private object? _savedValue;
    private object? _editorValue;

    /// <summary>
    /// Whether the current value differs from the last saved value.
    /// </summary>
    public bool IsDirty => !IsSameValue(_option.Value, _savedValue);

    /// <summary>
    /// Whether a section header shows above this row.
    /// </summary>
    public bool HasSectionTitle => SectionTitle != null;

    /// <summary>
    /// Whether the row renders a toggle switch.
    /// </summary>
    public bool IsToggle => ControlType == ConfigControlType.ToggleSwitch;

    /// <summary>
    /// Whether the row renders a slider.
    /// </summary>
    public bool IsSlider => ControlType == ConfigControlType.Slider;

    /// <summary>
    /// Whether the row renders a combo box.
    /// </summary>
    public bool IsComboBox => ControlType == ConfigControlType.ComboBox;

    /// <summary>
    /// The config option this row edits.
    /// </summary>
    public ConfigOption Option => _option;

    /// <summary>
    /// The row's display label (from the shared UI definitions).
    /// </summary>
    public string Label { get; }

    /// <summary>
    /// The section header text shown above the first row of each section, or null.
    /// </summary>
    public string? SectionTitle { get; }

    /// <summary>
    /// The resolved control type for this row.
    /// </summary>
    public ConfigControlType ControlType { get; }

    /// <summary>
    /// The slider range and step, when this row is a slider.
    /// </summary>
    public double? Minimum { get; }

    /// <summary>
    /// The slider range and step, when this row is a slider.
    /// </summary>
    public double? Maximum { get; }

    /// <summary>
    /// The slider range and step, when this row is a slider.
    /// </summary>
    public double? Step { get; }

    /// <summary>
    /// The slider's current value as display text (whole numbers for integer
    /// options, trimmed decimals for floats).
    /// </summary>
    public string SliderValueText => _option.Type == ConfigOptionType.Integer
        ? ((int)Math.Round(FloatValue)).ToString(CultureInfo.InvariantCulture)
        : FloatValue.ToString("0.##", CultureInfo.InvariantCulture);

    /// <summary>
    /// Whether the row is selected (controller focus).
    /// </summary>
    [ObservableProperty]
    public partial bool IsSelected { get; set; }

    /// <summary>
    /// The toggle's current value.
    /// </summary>
    [ObservableProperty]
    public partial bool BoolValue { get; set; }

    /// <summary>
    /// The slider's current value.
    /// </summary>
    [ObservableProperty]
    public partial double FloatValue { get; set; }

    /// <summary>
    /// The combo box's selected option index.
    /// </summary>
    [ObservableProperty]
    public partial int SelectedIndex { get; set; } = -1;

    /// <summary>
    /// The combo box options, when this row is a combo box.
    /// </summary>
    [ObservableProperty]
    public partial ObservableCollection<ComboBoxOptionViewModel>? ComboBoxOptions { get; set; }

    /// <summary>
    /// Raised after a value was committed to the config option (the pane saves
    /// the config file on this).
    /// </summary>
    public event Action? ValueChanged;

    /// <summary>
    /// Whether the incoming control value equals the option's stored value
    /// (string comparison so int/float/boxed values compare sensibly).
    /// </summary>
    private static bool IsSameValue(object? stored, object? incoming)
    {
        return string.Equals(Convert.ToString(stored, CultureInfo.InvariantCulture),
            Convert.ToString(incoming, CultureInfo.InvariantCulture), StringComparison.Ordinal);
    }

    /// <summary>
    /// Whether the incoming selection index is valid for the combo box:
    /// non-negative and within the current options.
    /// </summary>
    private bool IsValidSelectionIndex(int value) =>
        value >= 0 && ComboBoxOptions != null && value < ComboBoxOptions.Count;

    /// <summary>
    /// Resolves the row's control type, applying the Auto default from the
    /// option's type (Boolean → toggle, numeric → slider).
    /// </summary>
    private static ConfigControlType ResolveControlType(ConfigOption option, ConfigOptionDefinition definition)
    {
        if (definition.ControlType != ConfigControlType.Auto)
        {
            return definition.ControlType;
        }

        return option.Type switch
        {
            ConfigOptionType.Boolean => ConfigControlType.ToggleSwitch,
            ConfigOptionType.Integer or ConfigOptionType.Float => ConfigControlType.Slider,
            _ => ConfigControlType.TextBox
        };
    }

    /// <summary>
    /// Pushes the config option's current value into the bound control values.
    /// </summary>
    private void InitializeValues()
    {
        switch (_option.Type)
        {
            case ConfigOptionType.Boolean:
                BoolValue = _option.Value is true;
                break;
            case ConfigOptionType.Integer:
                FloatValue = _option.Value is int integer ? integer : 0;
                break;
            case ConfigOptionType.Float:
                FloatValue = _option.Value switch
                {
                    float single => single,
                    double @double => @double,
                    _ => 0.0
                };
                break;
        }
    }

    /// <summary>
    /// Builds the combo options from the definition, selecting the option
    /// matching the config value (first option as fallback).
    /// </summary>
    private void BuildComboBoxOptions(ConfigOption option, Dictionary<object, string> options)
    {
        ComboBoxOptions = new ObservableCollection<ComboBoxOptionViewModel>();
        int matchIndex = 0;
        foreach (KeyValuePair<object, string> pair in options)
        {
            ComboBoxOptions.Add(new ComboBoxOptionViewModel(pair.Key, pair.Value));
            if (IsSameValue(option.Value, pair.Key))
            {
                matchIndex = ComboBoxOptions.Count - 1;
            }
        }

        SelectedIndex = ComboBoxOptions.Count > 0 ? matchIndex : -1;
    }

    /// <summary>
    /// Captures the current config value so a cancelled edit can restore it.
    /// </summary>
    public void StartEdit()
    {
        _editorValue = _option.Value;
    }

    /// <summary>
    /// Restores the config option to the value captured by <see cref="StartEdit"/>
    /// and pushes it back into the controls.
    /// </summary>
    public void CancelEdit()
    {
        _option.Value = _editorValue;
        InitializeValues();
        if (ControlType == ConfigControlType.ComboBox && ComboBoxOptions != null)
        {
            int index = 0;
            for (int i = 0; i < ComboBoxOptions.Count; i++)
            {
                if (IsSameValue(_option.Value, ComboBoxOptions[i].Value))
                {
                    index = i;
                    break;
                }
            }

            SelectedIndex = ComboBoxOptions.Count > 0 ? index : -1;
        }

        ValueChanged?.Invoke();
    }

    /// <summary>
    /// Marks the current value as the saved baseline (after a manual save).
    /// </summary>
    public void MarkAsSaved()
    {
        _savedValue = _option.Value;
    }

    partial void OnBoolValueChanged(bool value)
    {
        if (IsSameValue(_option.Value, value))
        {
            return;
        }

        _option.Value = value;
        ValueChanged?.Invoke();
    }

    partial void OnFloatValueChanged(double value)
    {
        OnPropertyChanged(nameof(SliderValueText));
        if (IsSameValue(_option.Value, value))
        {
            return;
        }

        _option.Value = _option.Type == ConfigOptionType.Float
            ? (float)value
            : (int)Math.Round(value);
        ValueChanged?.Invoke();
    }

    partial void OnSelectedIndexChanged(int value)
    {
        if (!IsValidSelectionIndex(value))
        {
            return;
        }

        object selected = ComboBoxOptions![value].Value;
        if (IsSameValue(_option.Value, selected))
        {
            return;
        }

        _option.Value = selected;
        ValueChanged?.Invoke();
    }

    public ConfigRowViewModel(ConfigOption option, ConfigOptionDefinition definition, string label,
        string? sectionTitle)
    {
        _option = option;
        _savedValue = option.Value;
        Label = label;
        SectionTitle = sectionTitle;
        Minimum = definition.Minimum;
        Maximum = definition.Maximum;
        Step = definition.Step;
        ControlType = ResolveControlType(option, definition);
        InitializeValues();
        if (definition.ComboBoxOptions != null)
        {
            BuildComboBoxOptions(option, definition.ComboBoxOptions);
        }
    }
}