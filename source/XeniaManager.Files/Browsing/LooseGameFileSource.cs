using XeniaManager.Logging;

namespace XeniaManager.Files.Browsing;

/// <summary>
/// <see cref="IGameFileSource"/> over an extracted (loose) game directory.
/// </summary>
public sealed class LooseGameFileSource : GameFileSourceBase
{
    private readonly string _root;
    private IReadOnlyList<GameFileNode>? _files;

    /// <summary>
    /// Initializes a new instance browsing the given directory.
    /// </summary>
    /// <param name="rootDirectory">The game directory to browse.</param>
    public LooseGameFileSource(string rootDirectory)
    {
        _root = Path.GetFullPath(rootDirectory);
    }

    /// <summary>
    /// Gets the browsed root directory (absolute path).
    /// </summary>
    public string RootDirectory
    {
        get
        {
            return _root;
        }
    }

    /// <inheritdoc />
    public override IReadOnlyList<GameFileNode> Files
    {
        get
        {
            return _files ??= CollectFiles();
        }
    }

    /// <inheritdoc />
    public override List<GameFileNode>? ListDirectory(string path)
    {
        string? directory = Resolve(path);
        if (directory == null || !Directory.Exists(directory))
        {
            return null;
        }

        try
        {
            List<GameFileNode> nodes = new List<GameFileNode>();
            foreach (string subDirectory in Directory.EnumerateDirectories(directory).OrderBy(d => d, StringComparer.OrdinalIgnoreCase))
            {
                nodes.Add(ToDirectoryNode(subDirectory));
            }

            foreach (string file in Directory.EnumerateFiles(directory).OrderBy(f => f, StringComparer.OrdinalIgnoreCase))
            {
                nodes.Add(ToFileNode(file));
            }

            return nodes;
        }
        catch (Exception ex)
        {
            Logger.Trace<LooseGameFileSource>($"Failed to list directory '{path}': {ex.Message}");
            return new List<GameFileNode>();
        }
    }

    /// <inheritdoc />
    public override byte[]? ReadFile(string path)
    {
        string? fullPath = Resolve(path);
        if (fullPath == null || !File.Exists(fullPath))
        {
            return null;
        }

        try
        {
            return File.ReadAllBytes(fullPath);
        }
        catch (Exception ex)
        {
            Logger.Trace<LooseGameFileSource>($"Failed to read file '{path}': {ex.Message}");
            return null;
        }
    }

    /// <inheritdoc />
    public override byte[]? ReadFileRange(string path, ulong offset, ulong length)
    {
        string? fullPath = Resolve(path);
        if (fullPath == null || !File.Exists(fullPath))
        {
            return null;
        }

        try
        {
            using FileStream fs = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            if (offset >= (ulong)fs.Length || length == 0)
            {
                return Array.Empty<byte>();
            }

            int toRead = (int)Math.Min(length, (ulong)fs.Length - offset);
            byte[] result = new byte[toRead];
            fs.Seek((long)offset, SeekOrigin.Begin);
            fs.ReadExactly(result, 0, toRead);
            return result;
        }
        catch (Exception ex)
        {
            Logger.Trace<LooseGameFileSource>($"Failed to read range '{path}'");
            Logger.LogExceptionDetails<LooseGameFileSource>(ex);
            return null;
        }
    }

    /// <summary>
    /// Resolves a browsing path to an absolute path, or null when it escapes the root.
    /// </summary>
    private string? Resolve(string path)
    {
        string combined = Path.GetFullPath(Path.Combine(_root, NormalizePath(path).Replace('/', Path.DirectorySeparatorChar)));
        if (!combined.Equals(_root, StringComparison.OrdinalIgnoreCase)
            && !combined.StartsWith(_root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return combined;
    }

    private IReadOnlyList<GameFileNode> CollectFiles()
    {
        List<GameFileNode> files = new List<GameFileNode>();
        Stack<string> pending = new Stack<string>();
        pending.Push(_root);
        while (pending.Count > 0)
        {
            string directory = pending.Pop();
            List<string> subDirectories;
            List<string> directoryFiles;
            try
            {
                subDirectories = Directory.EnumerateDirectories(directory).ToList();
                directoryFiles = Directory.EnumerateFiles(directory).ToList();
            }
            catch (Exception ex)
            {
                Logger.Trace<LooseGameFileSource>($"Skipping inaccessible directory '{directory}': {ex.Message}");
                continue;
            }

            foreach (string subDirectory in subDirectories)
            {
                pending.Push(subDirectory);
            }

            foreach (string file in directoryFiles)
            {
                files.Add(ToFileNode(file));
            }
        }

        return files;
    }

    private GameFileNode ToFileNode(string fullPath) => new GameFileNode
    {
        Name = Path.GetFileName(fullPath),
        FullPath = Path.GetRelativePath(_root, fullPath).Replace(Path.DirectorySeparatorChar, '/'),
        IsFile = true,
        Size = (ulong)new FileInfo(fullPath).Length
    };

    private GameFileNode ToDirectoryNode(string fullPath) => new GameFileNode
    {
        Name = Path.GetFileName(fullPath),
        FullPath = Path.GetRelativePath(_root, fullPath).Replace(Path.DirectorySeparatorChar, '/'),
        IsFile = false,
        Size = 0
    };

    /// <inheritdoc />
    public override string FormatName
    {
        get
        {
            return "Loose";
        }
    }

    /// <inheritdoc />
    public override string FormatDescription
    {
        get
        {
            return "Extracted directory";
        }
    }

    /// <inheritdoc />
    public override void Dispose()
    {
    }
}