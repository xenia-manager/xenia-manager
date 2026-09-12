using XeniaManager.Files.Models.Iso;

namespace XeniaManager.Files.Browsing;

/// <summary>
/// <see cref="IGameFileSource"/> over an SVOD (GOD / installed game) package.
/// </summary>
public sealed class SvodGameFileSource : GameFileSourceBase
{
    private readonly SvodFile _svod;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance wrapping a loaded <see cref="SvodFile"/>. Takes ownership.
    /// </summary>
    public SvodGameFileSource(SvodFile svod)
    {
        _svod = svod;
    }

    /// <inheritdoc />
    public override IReadOnlyList<GameFileNode> Files
    {
        get
        {
            return _svod.Files.Select(ToNode).ToList();
        }
    }

    /// <inheritdoc />
    public override List<GameFileNode>? ListDirectory(string path) => _svod.ListDirectory(path)?.Select(ToNode).ToList();

    /// <inheritdoc />
    public override byte[]? ReadFile(string path) => _svod.ReadFile(path);

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
        _svod.Dispose();
    }
}