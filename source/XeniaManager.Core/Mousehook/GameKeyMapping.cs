namespace XeniaManager.Core.Mousehook;

public class GameKeyMapping
{
    public List<string> TitleIds { get; set; } = new();
    public string GameTitle { get; set; }
    public string Mode { get; set; }

    /// <summary>
    /// Keybindings
    /// Key = Xbox360 Key
    /// Value = List of Keyboard & Mouse bindings
    /// </summary>
    public Dictionary<string, List<string>> KeyBindings { get; set; } = new();

    /// <summary>
    /// Simple check to see if the keybindings are commented out
    /// </summary>
    public bool IsCommented { get; set; }

    /// <summary>
    /// Helper method to add a binding to an existing key
    /// </summary>
    /// <param name="key">The Xbox 360 key</param>
    /// <param name="binding">The keyboard/mouse binding to add</param>
    public void AddKeyBinding(string key, string binding)
    {
        if (string.IsNullOrWhiteSpace(binding))
        {
            return;
        }

        if (!KeyBindings.ContainsKey(key))
        {
            KeyBindings[key] = new List<string>();
        }

        if (!KeyBindings[key].Contains(binding))
        {
            KeyBindings[key].Add(binding);
        }
    }

    /// <summary>
    /// Helper method to remove a specific binding from a key
    /// </summary>
    /// <param name="key">The Xbox 360 key</param>
    /// <param name="binding">The keyboard/mouse binding to remove</param>
    public void RemoveKeyBinding(string key, string binding)
    {
        if (KeyBindings.TryGetValue(key, out var bindings))
        {
            bindings.Remove(binding);
            if (bindings.Count == 0)
            {
                KeyBindings.Remove(key);
            }
        }
    }
}