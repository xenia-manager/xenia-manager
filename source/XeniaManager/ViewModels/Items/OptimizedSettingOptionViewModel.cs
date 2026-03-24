using CommunityToolkit.Mvvm.ComponentModel;
using XeniaManager.Core.Models.Files.Config;

namespace XeniaManager.ViewModels.Items;

/// <summary>
/// ViewModel for a single optimized setting option.
/// Displays setting name, current value, new value, and allows removal.
/// </summary>
public partial class OptimizedSettingOptionViewModel : ObservableObject
{
    private readonly ConfigOption _optimizedOption;
    private readonly ConfigOption? _currentOption;

    [ObservableProperty] private string _sectionName = string.Empty;
    [ObservableProperty] private string _optionName = string.Empty;
    [ObservableProperty] private string _displayName = string.Empty;
    [ObservableProperty] private string? _comment;
    [ObservableProperty] private string _currentValue = string.Empty;
    [ObservableProperty] private string _newValue = string.Empty;
    [ObservableProperty] private bool _isModified;
    [ObservableProperty] private bool _isNew;
    [ObservableProperty] private bool _isRemoved;

    public OptimizedSettingOptionViewModel(string sectionName, ConfigOption optimizedOption, ConfigOption? currentOption = null)
    {
        _optimizedOption = optimizedOption;
        _currentOption = currentOption;

        SectionName = sectionName;
        OptionName = optimizedOption.Name;
        DisplayName = FormatDisplayName(optimizedOption.Name);
        Comment = optimizedOption.Comment;

        // Get current value
        CurrentValue = currentOption?.Value?.ToString() ?? "Not set";

        // Get new value
        NewValue = optimizedOption.Value?.ToString() ?? string.Empty;

        // Determine if modified or new
        IsModified = currentOption != null && !Equals(currentOption.Value, optimizedOption.Value);
        IsNew = currentOption == null;
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
    /// Gets the optimized option to apply.
    /// </summary>
    public ConfigOption OptimizedOption => _optimizedOption;

    /// <summary>
    /// Gets the section name.
    /// </summary>
    public string SectionNameValue => SectionName;
}