using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using XeniaManager.Core.Utilities;
using XeniaManager.Files;
using XeniaManager.Logging;
using XeniaManager.Files.Models.Stfs;

namespace XeniaManager.ViewModels.Items;

/// <summary>
/// ViewModel for displaying a header file in the UI.
/// </summary>
public partial class HeaderFileViewModel : ViewModelBase
{
    /// <summary>
    /// The underlying header file.
    /// </summary>
    public HeaderFile Header { get; }

    /// <summary>
    /// The thumbnail image embedded in the package (only available for package entries).
    /// </summary>
    [ObservableProperty] private Bitmap? _thumbnailImage;

    /// <summary>
    /// Gets whether a thumbnail image is available for display.
    /// </summary>
    public bool HasThumbnail
    {
        get
        {
            return ThumbnailImage != null;
        }
    }

    /// <summary>
    /// Gets the size details shown under the display name for title updates and marketplace content
    /// (e.g. "12.4 MB" for folders, "12.4 MB • v2" for packages with a version).
    /// Empty when the content type has no details or the size cannot be determined.
    /// </summary>
    public string SizeDetails
    {
        get
        {
            if (!_sizeDetailsResolved)
            {
                _sizeDetailsResolved = true;
                _sizeDetails = BuildSizeDetails();
            }

            return _sizeDetails ?? string.Empty;
        }
    }

    private string? _sizeDetails;
    private bool _sizeDetailsResolved;

    /// <summary>
    /// Gets whether size details should be shown for this entry.
    /// Only title updates and marketplace content show details.
    /// </summary>
    public bool HasSizeDetails
    {
        get
        {
            return Header.ContentType is Files.Models.Stfs.ContentType.TitleUpdates or Files.Models.Stfs.ContentType.MarketplaceContent
                   && !string.IsNullOrEmpty(SizeDetails);
        }
    }

    /// <summary>
    /// Builds the size details string, or null when it cannot be determined.
    /// Package entries use the package file size; folders use the recursive directory size.
    /// </summary>
    private string? BuildSizeDetails()
    {
        if (Header.ContentType is not (Files.Models.Stfs.ContentType.TitleUpdates or Files.Models.Stfs.ContentType.MarketplaceContent))
        {
            return null;
        }

        long size;
        try
        {
            string path = FilePath;
            if (File.Exists(path))
            {
                size = new FileInfo(path).Length;
            }
            else if (Directory.Exists(path))
            {
                size = new DirectoryInfo(path).EnumerateFiles("*", SearchOption.AllDirectories).Sum(file => file.Length);
            }
            else
            {
                return null;
            }
        }
        catch (Exception ex)
        {
            Logger.Warning<HeaderFileViewModel>($"Failed to determine size for '{Header.FileName}'");
            Logger.LogExceptionDetails<HeaderFileViewModel>(ex);
            return null;
        }

        string details = FileSizeFormatter.FormatBytes(size);
        if (Header.HasVersion && Header.Version != 0)
        {
            details += $" • v{Header.Version}";
        }

        return details;
    }

    /// <summary>
    /// Gets the display name of the header file.
    /// </summary>
    public string DisplayName
    {
        get
        {
            return Header.DisplayName;
        }
    }

    /// <summary>
    /// Gets the file name of the header file.
    /// </summary>
    public string FileName
    {
        get
        {
            return Header.FileName;
        }
    }

    /// <summary>
    /// Gets the content type of the header file.
    /// </summary>
    public string ContentType
    {
        get
        {
            return Header.ContentType.ToDisplayString();
        }
    }

    /// <summary>
    /// Gets the title ID of the header file.
    /// </summary>
    public string TitleId
    {
        get
        {
            return $"{Header.TitleId:X8}";
        }
    }

    /// <summary>
    /// Gets the original file path of the header file.
    /// </summary>
    public string HeaderFilePath
    {
        get
        {
            return Header.FilePath;
        }
    }

    /// <summary>
    /// Gets the reconstructed file path from the content type and file name.
    /// </summary>
    public string FilePath
    {
        get
        {
            // Package-file content points directly at the package itself (no sidecar header)
            if (Header.IsPackageEntry)
            {
                return Header.FilePath;
            }

            // Split the path to get the base directory (remove "\Headers\...")
            string[] parts = Regex.Split(HeaderFilePath, @"\\Headers", RegexOptions.IgnoreCase);
            string basePath = parts[0];

            // Primary reconstructed path
            string primaryPath = Path.Combine(basePath, Header.ContentType.ToHexString(), Header.FileName);

            if (File.Exists(primaryPath) || Directory.Exists(primaryPath))
            {
                return primaryPath;
            }

            // Backup path (remove \Headers\ and .header)
            string backupPath = HeaderFilePath
                .Replace(@"\Headers\", @"\", StringComparison.OrdinalIgnoreCase)
                .Replace(".header", "", StringComparison.OrdinalIgnoreCase);

            if (File.Exists(backupPath) || Directory.Exists(backupPath))
            {
                return backupPath;
            }

            // Return Empty string if both methods fail
            return string.Empty;
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="HeaderFileViewModel"/> class.
    /// </summary>
    /// <param name="header">The header file to wrap.</param>
    public HeaderFileViewModel(HeaderFile header)
    {
        Header = header;

        // Package entries carry their icon inside the package metadata
        if (header.IsPackageEntry && header.ThumbnailImage.Length > 0)
        {
            try
            {
                using MemoryStream ms = new MemoryStream(header.ThumbnailImage);
                ThumbnailImage = new Bitmap(ms);
            }
            catch (Exception ex)
            {
                Logger.Error<HeaderFileViewModel>($"Failed to load package thumbnail for '{header.FileName}'");
                Logger.LogExceptionDetails<HeaderFileViewModel>(ex);
            }
        }
    }
}