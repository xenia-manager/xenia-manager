using XeniaManager.Files.Models.Iso;

namespace XeniaManager.Files.Browsing;

/// <summary>
/// <see cref="IGameFileSource"/> over an ISO/XISO disc image.
/// </summary>
public sealed class IsoGameFileSource : GameFileSourceBase
{
    private readonly IsoFile _iso;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance wrapping a loaded <see cref="IsoFile"/>. Takes ownership.
    /// </summary>
    public IsoGameFileSource(IsoFile iso)
    {
        _iso = iso;
    }

    /// <inheritdoc />
    public override IReadOnlyList<GameFileNode> Files
    {
        get
        {
            return _iso.Files.Select(ToNode).ToList();
        }
    }

    /// <inheritdoc />
    public override List<GameFileNode>? ListDirectory(string path) => _iso.ListDirectory(path)?.Select(ToNode).ToList();

    /// <inheritdoc />
    public override byte[]? ReadFile(string path) => _iso.ReadFile(path);

    /// <inheritdoc />
    public override byte[]? ReadFileRange(string path, ulong offset, ulong length)
    {
        GdfxEntry? entry = _iso.Lookup(path);
        if (entry == null || !entry.IsFile)
        {
            return null;
        }

        return _iso.ReadFile(entry, offset, length);
    }

    /// <inheritdoc />
    public override string FormatName
    {
        get
        {
            return "ISO";
        }
    }

    /// <inheritdoc />
    public override string FormatDescription
    {
        get
        {
            return "Disc image (GDFX/XISO)";
        }
    }

    private static GameFileNode ToNode(GdfxEntry entry) => new GameFileNode
    {
        Name = entry.Name,
        FullPath = entry.FullPath,
        IsFile = entry.IsFile,
        Size = entry.Size
    };

    /// <inheritdoc />
    public override void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _iso.Dispose();
    }
}