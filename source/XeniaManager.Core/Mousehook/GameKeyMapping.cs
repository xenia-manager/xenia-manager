namespace XeniaManager.Core.Mousehook;

public class GameKeyMapping
{
    public List<string> TitleIds { get; set; } = new();
    public string GameTitle { get; set; }
    public string Mode { get; set; }

    /// <summary>
    /// Keybindings
    /// Key = Xbox360 Key
    /// Value = Keyboard & Mouse
    /// </summary>
    public Dictionary<string, string> KeyBindings { get; set; }

    /// <summary>
    /// Simple check to see if the keybindings are commented out
    /// </summary>
    public bool IsCommented { get; set; }
}