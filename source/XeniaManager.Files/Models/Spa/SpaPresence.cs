namespace XeniaManager.Files.Models.Spa;

/// <summary>
/// Presence data stored in the SPA's XRPT section (section 0x0001, id "XRPT").
/// </summary>
/// <remarks>
/// Layout: 12-byte XRPT section header, then a default XPBM property bag,
/// then a 2-byte presence-mode count followed by one XPBM property bag per mode.
/// </remarks>
public sealed class SpaPresence
{
    /// <summary>
    /// Gets the default presence property bag.
    /// </summary>
    public SpaPropertyBag PropertyBag { get; } = new SpaPropertyBag();

    /// <summary>
    /// Gets the presence modes, one property bag per mode.
    /// </summary>
    public List<SpaPropertyBag> PresenceModes { get; } = [];
}