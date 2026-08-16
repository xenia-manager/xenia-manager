using XeniaManager.Core.Models;

namespace XeniaManager.BigScreen.Models.Settings;

/// <summary>
/// A displayable emulator-version option for the XConfig card's version
/// dropdown (only versions that have an XConfig file).
/// </summary>
public class XConfigVersionOption(XeniaVersion version, string displayName)
{
    /// <summary>
    /// The emulator version.
    /// </summary>
    public XeniaVersion Version { get; } = version;

    /// <summary>
    /// Human-readable name shown in the dropdown (e.g. "Xenia Canary").
    /// </summary>
    public string DisplayName { get; } = displayName;
}