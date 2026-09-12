namespace XeniaManager.Files.Browsing;

/// <summary>
/// Base implementation of <see cref="IGameFileSource"/> with shared path helpers
/// and default XEX discovery over <see cref="IGameFileSource.Files"/>.
/// </summary>
public abstract class GameFileSourceBase : IGameFileSource
{
    /// <inheritdoc />
    public abstract IReadOnlyList<GameFileNode> Files { get; }

    /// <inheritdoc />
    public abstract List<GameFileNode>? ListDirectory(string path);

    /// <inheritdoc />
    public abstract byte[]? ReadFile(string path);

    /// <inheritdoc />
    public virtual string? FindDefaultXexPath()
    {
        GameFileNode? exact = Files.FirstOrDefault(f => f.Name.Equals("default.xex", StringComparison.OrdinalIgnoreCase));
        if (exact != null)
        {
            return exact.FullPath;
        }

        return Files.FirstOrDefault(f => f.Name.EndsWith(".xex", StringComparison.OrdinalIgnoreCase))?.FullPath;
    }

    /// <inheritdoc />
    public abstract void Dispose();

    /// <summary>
    /// Normalizes a browsing path to '/'-separated segments without empty entries,
    /// so forward and backslash separators resolve identically ("" for root).
    /// </summary>
    protected static string NormalizePath(string path) =>
        string.Join('/', path.Split(['/', '\\'], StringSplitOptions.RemoveEmptyEntries));

    /// <summary>
    /// Joins a parent browsing path with a leaf name.
    /// </summary>
    protected static string JoinPath(string parent, string leaf) => parent.Length == 0 ? leaf : $"{parent}/{leaf}";

    /// <summary>
    /// Returns the leaf name of a '/'-separated browsing path.
    /// </summary>
    protected static string LeafName(string fullPath)
    {
        int index = fullPath.LastIndexOf('/');
        return index < 0 ? fullPath : fullPath[(index + 1)..];
    }
}