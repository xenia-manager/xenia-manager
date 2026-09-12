using XeniaManager.Files.Models;
using XeniaManager.Files.Utilities;
using XeniaManager.Logging;

namespace XeniaManager.Files.Browsing;

/// <summary>
/// Opens the right <see cref="IGameFileSource"/> for a library game path, magic-first with extension fallback.
/// Returns null when the path is missing or unsupported. Never throws.
/// </summary>
public static class GameFileSourceFactory
{
    /// <summary>
    /// Opens a read-only browsing source for the given game file or directory.
    /// XEX game files browse their containing directory.
    /// </summary>
    /// <param name="gamePath">The resolved game path (file or directory).</param>
    /// <returns>A browsing source, or null when the path is missing or unsupported.</returns>
    public static IGameFileSource? TryOpen(string gamePath)
    {
        try
        {
            if (Directory.Exists(gamePath))
            {
                // IdentifyFileType throws for non-SVOD directories, so probe SVOD directly here.
                if (SvodFile.IsSvodPackage(gamePath))
                {
                    return new SvodGameFileSource(SvodFile.Load(gamePath));
                }

                return new LooseGameFileSource(gamePath);
            }

            if (!File.Exists(gamePath))
            {
                return null;
            }

            FileSignature signature = FileIdentifier.IdentifyFileType(gamePath);
            switch (signature)
            {
                case FileSignature.SVOD:
                    return new SvodGameFileSource(SvodFile.Load(gamePath));
                case FileSignature.CON:
                case FileSignature.LIVE:
                case FileSignature.PIRS:
                    return new StfsGameFileSource(StfsFile.Load(gamePath));
                case FileSignature.XISO:
                case FileSignature.ISO:
                    return new IsoGameFileSource(IsoFile.Load(gamePath));
                case FileSignature.ZAR:
                    return new ZarGameFileSource(ZarFile.Load(gamePath));
                case FileSignature.XEX0:
                case FileSignature.XEXQ:
                case FileSignature.XEXH:
                case FileSignature.XEX25:
                case FileSignature.XEX1:
                case FileSignature.XEX2:
                    return OpenLooseFromFile(gamePath);
                default:
                    return null;
            }
        }
        catch (Exception ex)
        {
            Logger.Warning<IGameFileSource>($"Cannot browse game files at '{gamePath}': {ex.Message}");
            return null;
        }
    }

    private static IGameFileSource? OpenLooseFromFile(string filePath)
    {
        string? directory = Path.GetDirectoryName(Path.GetFullPath(filePath));
        if (string.IsNullOrEmpty(directory) || !Directory.Exists(directory))
        {
            return null;
        }

        return new LooseGameFileSource(directory);
    }
}