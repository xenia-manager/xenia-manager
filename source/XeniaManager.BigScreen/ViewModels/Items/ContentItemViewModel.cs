using System.IO;
using System.Text.RegularExpressions;
using CommunityToolkit.Mvvm.ComponentModel;
using XeniaManager.BigScreen.Utilities;
using XeniaManager.Core.Files;
using XeniaManager.Core.Models.Files.Stfs;

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
    /// The content's display name.
    /// </summary>
    public string DisplayName => Header.DisplayName;

    /// <summary>
    /// The content's file (package) name.
    /// </summary>
    public string FileName => Header.FileName;

    /// <summary>
    /// The path to the header file on disk.
    /// </summary>
    public string HeaderFilePath => Header.FilePath;

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
            string basePath = Regex.Split(HeaderFilePath, @"\\Headers", RegexOptions.IgnoreCase)[0];
            string primaryPath = Path.Combine(basePath, Header.ContentType.ToHexString(), Header.FileName);
            if (ExistsOnDisk(primaryPath))
            {
                return primaryPath;
            }

            string backupPath = HeaderFilePath
                .Replace(@"\Headers\", @"\", System.StringComparison.OrdinalIgnoreCase)
                .Replace(".header", "", System.StringComparison.OrdinalIgnoreCase);
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
    }
}