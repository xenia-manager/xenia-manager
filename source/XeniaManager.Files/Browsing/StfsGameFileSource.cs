using XeniaManager.Files.Models.Stfs;

namespace XeniaManager.Files.Browsing;

/// <summary>
/// <see cref="IGameFileSource"/> over an STFS (CON/LIVE/PIRS) package.
/// </summary>
public sealed class StfsGameFileSource : GameFileSourceBase
{
    private readonly StfsFile _stfs;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance wrapping a loaded <see cref="StfsFile"/>. Takes ownership.
    /// </summary>
    public StfsGameFileSource(StfsFile stfs)
    {
        _stfs = stfs;
    }

    /// <inheritdoc />
    public override IReadOnlyList<GameFileNode> Files
    {
        get
        {
            List<GameFileNode> files = new List<GameFileNode>();
            Stack<string> pending = new Stack<string>();
            pending.Push(string.Empty);
            while (pending.Count > 0)
            {
                string dir = pending.Pop();
                List<StfsFileEntry>? children = _stfs.ListDirectory(dir);
                if (children == null)
                {
                    continue;
                }

                foreach (StfsFileEntry child in children)
                {
                    string fullPath = JoinPath(dir, child.FileName);
                    if (child.IsDirectory)
                    {
                        pending.Push(fullPath);
                    }
                    else
                    {
                        files.Add(ToNode(fullPath, child));
                    }
                }
            }

            return files;
        }
    }

    /// <inheritdoc />
    public override List<GameFileNode>? ListDirectory(string path)
    {
        string normalized = NormalizePath(path);
        List<StfsFileEntry>? children = _stfs.ListDirectory(normalized);
        if (children == null)
        {
            return null;
        }

        return children.Select(e => ToNode(JoinPath(normalized, e.FileName), e)).ToList();
    }

    /// <inheritdoc />
    public override byte[]? ReadFile(string path) => _stfs.ReadFile(path);

    private static GameFileNode ToNode(string fullPath, StfsFileEntry entry) => new GameFileNode
    {
        Name = entry.FileName,
        FullPath = fullPath,
        IsFile = !entry.IsDirectory,
        Size = entry.IsDirectory ? 0 : (ulong)entry.FileSize
    };

    /// <inheritdoc />
    public override void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _stfs.Dispose();
    }
}