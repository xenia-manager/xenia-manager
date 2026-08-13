namespace XeniaManager.BigScreen.Models;

/// <summary>
/// A displayable card image mode option for the settings dropdown.
/// </summary>
public class CardImageModeOption(CardImageMode mode, string displayName)
{
    /// <summary>
    /// The card image mode value.
    /// </summary>
    public CardImageMode Mode { get; } = mode;

    /// <summary>
    /// Human-readable name shown in the dropdown.
    /// </summary>
    public string DisplayName { get; } = displayName;
}
