using XeniaManager.Files.Models.XConfig;

namespace XeniaManager.BigScreen.Models.Settings;

/// <summary>
/// A displayable XConfig resolution option for the settings dropdown. The
/// "R" prefix on the enum member names (resolution) is stripped for display.
/// </summary>
public class XConfigResolutionOption(XConfigResolution value, string displayName)
{
    /// <summary>
    /// The resolution value.
    /// </summary>
    public XConfigResolution Value { get; } = value;

    /// <summary>
    /// Human-readable name shown in the dropdown (no "R" prefix).
    /// </summary>
    public string DisplayName { get; } = displayName;
}