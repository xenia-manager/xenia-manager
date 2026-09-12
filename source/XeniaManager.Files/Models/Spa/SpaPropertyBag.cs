namespace XeniaManager.Files.Models.Spa;

/// <summary>
/// Property bag: sets of context and property IDs (XPBM section payload).
/// </summary>
/// <remarks>
/// Layout after the 12-byte XPBM section header: contexts_count (4),
/// properties_count (4), then contexts_count context IDs (4 each) followed
/// by properties_count property IDs (4 each). All big-endian.
/// </remarks>
public sealed class SpaPropertyBag
{
    /// <summary>
    /// Gets the context IDs in the bag.
    /// </summary>
    public List<uint> Contexts { get; } = [];

    /// <summary>
    /// Gets the property IDs in the bag.
    /// </summary>
    public List<uint> Properties { get; } = [];
}