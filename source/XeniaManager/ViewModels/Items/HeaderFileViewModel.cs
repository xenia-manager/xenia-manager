using System;
using System.IO;
using System.Text.RegularExpressions;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
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
    public bool HasThumbnail => ThumbnailImage != null;

    /// <summary>
    /// Gets the display name of the header file.
    /// </summary>
    public string DisplayName => Header.DisplayName;

    /// <summary>
    /// Gets the file name of the header file.
    /// </summary>
    public string FileName => Header.FileName;

    /// <summary>
    /// Gets the content type of the header file.
    /// </summary>
    public string ContentType => Header.ContentType.ToDisplayString();

    /// <summary>
    /// Gets the title ID of the header file.
    /// </summary>
    public string TitleId => $"{Header.TitleId:X8}";

    /// <summary>
    /// Gets the original file path of the header file.
    /// </summary>
    public string HeaderFilePath => Header.FilePath;

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