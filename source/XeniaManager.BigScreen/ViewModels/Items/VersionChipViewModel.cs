using FluentIcons.Common;
using XeniaManager.Core.Models;

namespace XeniaManager.BigScreen.ViewModels.Items;

/// <summary>
/// One emulator-version chip in the profile picker, used to switch which
/// version's profile set is shown.
/// </summary>
public class VersionChipViewModel(XeniaVersion version, Symbol icon, string name, bool isSelected)
{
    /// <summary>
    /// Xenia Version that is currently in use.
    /// </summary>
    public XeniaVersion Version { get; } = version;

    /// <summary>
    /// Symbol for Xenia Version.
    /// </summary>
    public Symbol Icon { get; } = icon;

    /// <summary>
    /// Name of the Xenia Version.
    /// </summary>
    public string Name { get; } = name;

    /// <summary>
    /// Is this Xenia Version selected
    /// </summary>
    public bool IsSelected { get; } = isSelected;
}