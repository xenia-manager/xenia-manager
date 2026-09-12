using XeniaManager.Files.Models.Zar;

namespace XeniaManager.Files.Browsing;

/// <summary>
/// <see cref="IGameFileSource"/> over a ZAR archive.
/// </summary>
public sealed class ZarGameFileSource : GameFileSourceBase
{
    private readonly ZarFile _zar;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance wrapping a loaded <see cref="ZarFile"/>. Takes ownership.
    /// </summary>
    public ZarGameFileSource(ZarFile zar)
    {
        _zar = zar;
    }

    /// <inheritdoc />
    public override IReadOnlyList<GameFileNode> Files
    {
        get
        {
            // An invalid ZarFile has an empty file tree that cannot be walked.
            if (!_zar.IsValid)
            {
                return Array.Empty<GameFileNode>();
            }

            return _zar.Files.Select(e => new GameFileNode
            {
                Name = LeafName(e.Name),
                FullPath = e.Name,
                IsFile = true,
                Size = e.Size
            }).ToList();
        }
    }

    /// <inheritdoc />
    public override List<GameFileNode>? ListDirectory(string path)
    {
        string normalized = NormalizePath(path);
        // An invalid ZarFile has an empty file tree that cannot be walked.
        if (!_zar.IsValid)
        {
            return normalized.Length == 0 ? new List<GameFileNode>() : null;
        }

        List<DirEntry>? children = _zar.ListDirectory(normalized);
        if (children == null)
        {
            return null;
        }

        return children.Select(e => new GameFileNode
        {
            Name = e.Name,
            FullPath = JoinPath(normalized, e.Name),
            IsFile = e.IsFile,
            Size = e.Size
        }).ToList();
    }

    /// <inheritdoc />
    public override byte[]? ReadFile(string path)
    {
        if (!_zar.IsValid)
        {
            return null;
        }

        return _zar.ReadFile(path);
    }

    /// <inheritdoc />
    public override void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _zar.Dispose();
    }
}