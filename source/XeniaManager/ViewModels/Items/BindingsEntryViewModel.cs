using CommunityToolkit.Mvvm.ComponentModel;
using XeniaManager.Core.Logging;
using XeniaManager.Core.Models.Files.Bindings;
using XeniaManager.ViewModels.Controls;

namespace XeniaManager.ViewModels.Items;

/// <summary>
/// ViewModel for a single bindings entry in the mousehook controls editor.
/// </summary>
public partial class BindingsEntryViewModel : ObservableObject
{
    private readonly BindingsEntry _entry;
    public MousehookControlsEditorDialogViewModel Parent { get; }

    [ObservableProperty] private int _index;
    [ObservableProperty] private string _key;
    [ObservableProperty] private string _value;
    [ObservableProperty] private string? _comment;
    [ObservableProperty] private bool _isCommented;

    /// <summary>
    /// Gets the underlying bindings entry.
    /// </summary>
    public BindingsEntry Entry => _entry;

    public BindingsEntryViewModel(BindingsEntry entry, int index, MousehookControlsEditorDialogViewModel parent)
    {
        Parent = parent;
        _entry = entry;
        _index = index;
        _key = entry.Key;
        _value = entry.Value?.ToString() ?? string.Empty;
        _comment = entry.Comment;
        _isCommented = entry.IsCommented;
        Logger.Debug<BindingsEntryViewModel>($"Created entry: {_key} = {_value}");
    }

    /// <summary>
    /// Applies the current ViewModel state back to the underlying entry.
    /// </summary>
    public void ApplyChanges()
    {
        Logger.Debug<BindingsEntryViewModel>($"Applying changes: {Key} = {Value} (was: {_entry.Key} = {_entry.Value})");
        _entry.Key = Key;
        _entry.Value = Value;
        _entry.Comment = Comment;
        _entry.IsCommented = IsCommented;
    }

    /// <summary>
    /// Refreshes the ViewModel from the underlying entry.
    /// </summary>
    public void Refresh()
    {
        Key = _entry.Key;
        Value = _entry.Value?.ToString() ?? string.Empty;
        Comment = _entry.Comment;
        IsCommented = _entry.IsCommented;
    }
}