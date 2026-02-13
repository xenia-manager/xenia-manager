using SharpCompress.Common;
using SharpCompress.Readers;
using XeniaManager.Core.Logging;

namespace XeniaManager.Core.Utilities;

/// <summary>
/// Provides methods for extracting files from various archive formats including ZIP, RAR, TAR, and 7-Zip.
/// </summary>
public class ArchiveExtractor
{
    private const string SupportedFormatsDisplay = ".zip, .rar, .tar, .tar.gz, .tgz, .tar.bz2, .tbz2, .tar.xz, .txz, .7z";

    /// <summary>
    /// Extracts all files or specific files from an archive to the specified output directory.
    /// Automatically detects the archive format.
    /// </summary>
    /// <param name="archivePath">The path to the archive to extract.</param>
    /// <param name="outputPath">The directory where files should be extracted.</param>
    /// <param name="filesToExtract">Optional array of specific files to extract. If null or empty, all files are extracted.</param>
    /// <exception cref="ArgumentException">Thrown when archivePath or outputPath is null, empty, or whitespace.</exception>
    /// <exception cref="FileNotFoundException">Thrown when the specified archive file does not exist.</exception>
    /// <exception cref="NotSupportedException">Thrown when the archive format is not supported.</exception>
    public static void ExtractArchive(string archivePath, string outputPath, string[]? filesToExtract = null)
    {
        ValidateInputs(archivePath, outputPath);
        string formatName = GetFormatName(archivePath);

        try
        {
            Directory.CreateDirectory(outputPath);

            ReaderOptions options = new ReaderOptions
            {
                ExtractFullPath = true,
                Overwrite = true
            };

            using FileStream stream = File.OpenRead(archivePath);
            using IReader reader = ReaderFactory.OpenReader(stream, options);
            int extractedCount = 0;

            while (reader.MoveToNextEntry())
            {
                if (!reader.Entry.IsDirectory && ShouldExtractEntry(reader.Entry, filesToExtract))
                {
                    reader.WriteEntryToDirectory(outputPath);
                    extractedCount++;
                }
            }

            Logger.Info<ArchiveExtractor>($"Successfully extracted {extractedCount} files from {formatName}: {archivePath} to {outputPath}");
        }
        catch (UnauthorizedAccessException ex)
        {
            Logger.Error<ArchiveExtractor>($"Access denied when extracting {formatName} {archivePath}: {ex.Message}");
            throw;
        }
        catch (InvalidFormatException ex)
        {
            Logger.Error<ArchiveExtractor>($"Invalid format stream type ({formatName}) {archivePath}: {ex.Message}");
            throw;
        }
        catch (IOException ex)
        {
            Logger.Error<ArchiveExtractor>($"IO error when extracting {formatName} {archivePath}: {ex.Message}");
            throw;
        }
        catch (Exception ex)
        {
            Logger.Error<ArchiveExtractor>($"Unexpected error when extracting {formatName} {archivePath}: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Extracts all files or specific files from an archive to the specified output directory asynchronously.
    /// Automatically detects the archive format.
    /// </summary>
    /// <param name="archivePath">The path to the archive to extract.</param>
    /// <param name="outputPath">The directory where files should be extracted.</param>
    /// <param name="filesToExtract">Optional array of specific files to extract. If null or empty, all files are extracted.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the extraction operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="ArgumentException">Thrown when archivePath or outputPath is null, empty, or whitespace.</exception>
    /// <exception cref="FileNotFoundException">Thrown when the specified archive file does not exist.</exception>
    /// <exception cref="NotSupportedException">Thrown when the archive format is not supported.</exception>
    /// <exception cref="OperationCanceledException">Thrown when the operation is canceled.</exception>
    public static async Task ExtractArchiveAsync(string archivePath, string outputPath, string[]? filesToExtract = null, CancellationToken cancellationToken = default)
    {
        ValidateInputs(archivePath, outputPath);
        string formatName = GetFormatName(archivePath);

        try
        {
            Directory.CreateDirectory(outputPath);
            string resolvedOutputPath = Path.GetFullPath(outputPath);

            await using FileStream stream = File.OpenRead(archivePath);
            await using IAsyncReader reader = await ReaderFactory.OpenAsyncReader(stream, cancellationToken: cancellationToken);
            int extractedCount = 0;

            while (await reader.MoveToNextEntryAsync(cancellationToken))
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!reader.Entry.IsDirectory && ShouldExtractEntry(reader.Entry, filesToExtract))
                {
                    string entryOutputPath = GetSafeEntryOutputPath(resolvedOutputPath, reader.Entry.Key);

                    string? entryDirectory = Path.GetDirectoryName(entryOutputPath);
                    if (!string.IsNullOrEmpty(entryDirectory))
                    {
                        Directory.CreateDirectory(entryDirectory);
                    }

                    await reader.WriteEntryToFileAsync(entryOutputPath, cancellationToken: cancellationToken);
                    extractedCount++;
                }
            }

            Logger.Info<ArchiveExtractor>($"Successfully extracted {extractedCount} files from {formatName}: {archivePath} to {outputPath}");
        }
        catch (OperationCanceledException)
        {
            Logger.Warning<ArchiveExtractor>($"Extraction was cancelled for {formatName}: {archivePath}");
            throw;
        }
        catch (UnauthorizedAccessException ex)
        {
            Logger.Error<ArchiveExtractor>($"Access denied when extracting {formatName} {archivePath}: {ex.Message}");
            throw;
        }
        catch (InvalidFormatException ex)
        {
            Logger.Error<ArchiveExtractor>($"Invalid format stream type ({formatName}) {archivePath}: {ex.Message}");
            throw;
        }
        catch (IOException ex)
        {
            Logger.Error<ArchiveExtractor>($"IO error when extracting {formatName} {archivePath}: {ex.Message}");
            throw;
        }
        catch (Exception ex)
        {
            Logger.Error<ArchiveExtractor>($"Unexpected error when extracting {formatName} {archivePath}: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Validates the archive and output paths, checking for existence and supported format.
    /// </summary>
    private static void ValidateInputs(string archivePath, string outputPath)
    {
        if (string.IsNullOrWhiteSpace(archivePath))
        {
            throw new ArgumentException("Archive path cannot be null or empty.", nameof(archivePath));
        }

        if (string.IsNullOrWhiteSpace(outputPath))
        {
            throw new ArgumentException("Output path cannot be null or empty.", nameof(outputPath));
        }

        if (!File.Exists(archivePath))
        {
            throw new FileNotFoundException($"Archive file does not exist: {archivePath}");
        }

        string extension = GetNormalizedExtension(archivePath);
        if (!IsSupportedFormat(extension))
        {
            throw new NotSupportedException($"Archive format '{extension}' is not supported. Supported formats: {SupportedFormatsDisplay}");
        }
    }

    /// <summary>
    /// Gets the normalized file extension, handling compound extensions like .tar.gz, .tar.bz2, .tar.xz.
    /// </summary>
    /// <remarks>
    /// <see cref="System.IO.Path.GetExtension"/> returns only the last extension (e.g. ".gz" for "file.tar.gz"),
    /// so compound tar extensions must be detected by inspecting the full filename.
    /// </remarks>
    private static string GetNormalizedExtension(string path)
    {
        string fileName = Path.GetFileName(path).ToLowerInvariant();

        if (fileName.EndsWith(".tar.gz"))
        {
            return ".tar.gz";
        }
        if (fileName.EndsWith(".tar.bz2"))
        {
            return ".tar.bz2";
        }
        if (fileName.EndsWith(".tar.xz"))
        {
            return ".tar.xz";
        }

        return Path.GetExtension(path).ToLowerInvariant();
    }

    /// <summary>
    /// Determines whether the given extension is a supported archive format.
    /// </summary>
    private static bool IsSupportedFormat(string extension) => extension is ".zip" or ".rar" or ".7z"
        or ".tar" or ".tar.gz" or ".tgz"
        or ".tar.bz2" or ".tbz2"
        or ".tar.xz" or ".txz";

    /// <summary>
    /// Gets a human-readable format name for logging purposes.
    /// </summary>
    private static string GetFormatName(string path) => GetNormalizedExtension(path) switch
    {
        ".zip" => "ZIP",
        ".rar" => "RAR",
        ".7z" => "7-Zip",
        ".tar" or ".tar.gz" or ".tgz"
            or ".tar.bz2" or ".tbz2"
            or ".tar.xz" or ".txz" => "TAR",
        _ => "Unknown"
    };

    /// <summary>
    /// Computes a safe output path for an archive entry, guarding against path traversal (Zip Slip).
    /// </summary>
    /// <param name="resolvedOutputPath">The fully resolved (absolute) output directory path.</param>
    /// <param name="entryKey">The entry key/path from the archive.</param>
    /// <returns>The safe, fully resolved output file path.</returns>
    /// <exception cref="IOException">Thrown when the entry path would escape the output directory.</exception>
    private static string GetSafeEntryOutputPath(string resolvedOutputPath, string? entryKey)
    {
        string key = entryKey ?? string.Empty;
        string fullPath = Path.GetFullPath(Path.Combine(resolvedOutputPath, key));

        // Append separator to prevent prefix false-matches (e.g., /output vs. /output-other)
        string normalizedBase = resolvedOutputPath.EndsWith(Path.DirectorySeparatorChar)
            ? resolvedOutputPath
            : resolvedOutputPath + Path.DirectorySeparatorChar;

        if (!fullPath.StartsWith(normalizedBase, StringComparison.OrdinalIgnoreCase))
        {
            throw new IOException($"Archive entry '{key}' would extract outside the target directory. Possible path traversal attack.");
        }

        return fullPath;
    }

    /// <summary>
    /// Determines whether an archive entry should be extracted based on the specified file filters.
    /// </summary>
    /// <param name="entry">The archive entry to check.</param>
    /// <param name="filesToExtract">Array of specific files to extract. If null or empty, all files are extracted.</param>
    /// <returns>True if the entry should be extracted, false otherwise.</returns>
    private static bool ShouldExtractEntry(IEntry entry, string[]? filesToExtract)
    {
        if (filesToExtract is not { Length: > 0 })
        {
            return true;
        }

        return !string.IsNullOrEmpty(entry.Key) && filesToExtract.Any(f =>
            entry.Key.Equals(f, StringComparison.OrdinalIgnoreCase) ||
            entry.Key.EndsWith(f, StringComparison.OrdinalIgnoreCase));
    }
}