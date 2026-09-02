using System;
using System.IO;
using System.Text.RegularExpressions;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using XeniaManager.BigScreen.Utilities;
using XeniaManager.Files;
using XeniaManager.Logging;
using XeniaManager.Files.Models.Stfs;

namespace XeniaManager.BigScreen.ViewModels.Items;

/// <summary>
/// ViewModel for an installed content row (title update or marketplace item):
/// the header file with the reconstructed content path for deletion.
/// </summary>
public partial class ContentItemViewModel : ObservableObject, ISelectable
{
    /// <summary>
    /// The Core header file this row represents.
    /// </summary>
    public HeaderFile Header { get; }

    /// <summary>
    /// Whether this row currently has selection in the content list.
    /// </summary>
    [ObservableProperty]
    public partial bool IsSelected { get; set; }

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
    /// The content's display name.
    /// </summary>
    public string DisplayName
    {
        get
        {
            return Header.DisplayName;
        }
    }

    /// <summary>
    /// The content's file (package) name.
    /// </summary>
    public string FileName
    {
        get
        {
            return Header.FileName;
        }
    }

    /// <summary>
    /// The path to the header file on disk.
    /// </summary>
    public string HeaderFilePath
    {
        get
        {
            return Header.FilePath;
        }
    }

    /// <summary>
    /// Whether the given path exists as a file or a directory.
    /// </summary>
    private static bool ExistsOnDisk(string path) => File.Exists(path) || Directory.Exists(path);

    /// <summary>
    /// The reconstructed content path (package file or directory), or an empty
    /// string when neither the primary nor the backup path exists.
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

            string basePath = Regex.Split(HeaderFilePath, @"\\Headers", RegexOptions.IgnoreCase)[0];
            string primaryPath = Path.Combine(basePath, Header.ContentType.ToHexString(), Header.FileName);
            if (ExistsOnDisk(primaryPath))
            {
                return primaryPath;
            }

            string backupPath = HeaderFilePath
                .Replace(@"\Headers\", @"\", StringComparison.OrdinalIgnoreCase)
                .Replace(".header", "", StringComparison.OrdinalIgnoreCase);
            if (ExistsOnDisk(backupPath))
            {
                return backupPath;
            }

            return string.Empty;
        }
    }

    public ContentItemViewModel(HeaderFile header)
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
                Logger.Error<ContentItemViewModel>($"Failed to load package thumbnail for '{header.FileName}'");
                Logger.LogExceptionDetails<ContentItemViewModel>(ex);
            }
        }
    }
}