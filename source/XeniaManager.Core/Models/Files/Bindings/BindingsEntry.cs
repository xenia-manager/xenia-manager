using XeniaManager.Core.Extensions;

namespace XeniaManager.Core.Models.Files.Bindings;

/// <summary>
/// Represents a single entry (key-value pair) in a bindings file section.
/// Each entry maps a keyboard key to a controller input.
/// </summary>
public class BindingsEntry
{
    /// <summary>
    /// Gets or sets the keyboard key of the bindings entry.
    /// </summary>
    public string Key { get; set; }

    /// <summary>
    /// Gets or sets the controller input value of the bindings entry.
    /// </summary>
    public object? Value { get; set; }

    /// <summary>
    /// Gets or sets the comment for this entry.
    /// </summary>
    public string? Comment { get; set; }

    /// <summary>
    /// Gets or sets whether this entry is commented out.
    /// </summary>
    public bool IsCommented { get; set; }

    /// <summary>
    /// Creates a new bindings entry.
    /// </summary>
    public BindingsEntry(string key, object? value, string? comment = null, bool isCommented = false)
    {
        Key = key;
        Value = value;
        Comment = comment;
        IsCommented = isCommented;
    }

    /// <summary>
    /// Gets the keyboard key as a VirtualKeyCode enum value.
    /// </summary>
    /// <returns>The VirtualKeyCode if the key can be parsed, null otherwise.</returns>
    public VirtualKeyCode? GetVirtualKeyCode()
    {
        return BindingExtensions.ParseFromBindingString<VirtualKeyCode>(Key);
    }

    /// <summary>
    /// Sets the keyboard key from a VirtualKeyCode enum value.
    /// </summary>
    /// <param name="keyCode">The virtual key code.</param>
    public void SetVirtualKeyCode(VirtualKeyCode keyCode)
    {
        Key = keyCode.ToBindingString();
    }

    /// <summary>
    /// Gets the controller input as an XInputBinding enum value.
    /// </summary>
    /// <returns>The XInputBinding if the value can be parsed, null otherwise.</returns>
    public XInputBinding? GetXInputBinding()
    {
        return Value != null ? BindingExtensions.ParseFromBindingString<XInputBinding>(Value.ToString()!) : null;
    }

    /// <summary>
    /// Sets the controller input from an XInputBinding enum value.
    /// </summary>
    /// <param name="binding">The XInput binding.</param>
    public void SetXInputBinding(XInputBinding binding)
    {
        Value = binding.ToBindingString();
    }
}