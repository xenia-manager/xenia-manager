namespace XeniaManager.Core.Models.InputListener;

/// <summary>
/// Provides data for keyboard and mouse key events.
/// </summary>
public sealed class KeyEventArgs : EventArgs
{
    /// <summary>
    /// Gets the key name in Xenia format.
    /// </summary>
    public string Key { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="KeyEventArgs"/> class.
    /// </summary>
    /// <param name="key">The key name in Xenia format.</param>
    public KeyEventArgs(string key)
    {
        Key = key ?? throw new ArgumentNullException(nameof(key));
    }
}