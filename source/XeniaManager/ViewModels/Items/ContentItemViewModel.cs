using System;
using System.IO;
using System.Windows.Input;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using XeniaManager.Core.Files;
using XeniaManager.Core.Models.Files.Stfs;

namespace XeniaManager.ViewModels.Items;

/// <summary>
/// Represents a content item that can be installed for a game (DLC, update, etc.).
/// </summary>
public partial class ContentItemViewModel : ObservableObject
{
    /// <summary>
    /// The display name of the content item (shown on top).
    /// </summary>
    [ObservableProperty] private string _displayName = string.Empty;

    /// <summary>
    /// The type of content (DLC, Update, Save, etc.).
    /// </summary>
    [ObservableProperty] private string _contentType = string.Empty;

    /// <summary>
    /// The full path to the STFS file.
    /// </summary>
    [ObservableProperty] private string _filePath = string.Empty;

    /// <summary>
    /// The thumbnail image from the STFS package.
    /// </summary>
    [ObservableProperty] private Bitmap? _thumbnailImage;

    /// <summary>
    /// The title thumbnail image from the STFS package.
    /// </summary>
    [ObservableProperty] private Bitmap? _titleThumbnailImage;

    /// <summary>
    /// The underlying STFS file object.
    /// </summary>
    public StfsFile? StfsFile { get; set; }

    /// <summary>
    /// Command to remove this content item from the list.
    /// </summary>
    public ICommand RemoveCommand { get; }

    /// <summary>
    /// Installation progress (0-100).
    /// </summary>
    [ObservableProperty] private double _installationProgress;

    /// <summary>
    /// Indicates whether this item is currently being installed.
    /// </summary>
    [ObservableProperty] private bool _isInstalling;

    /// <summary>
    /// Indicates whether this item has completed installation.
    /// </summary>
    [ObservableProperty] private bool _installationComplete;

    /// <summary>
    /// Indicates whether installation failed for this item.
    /// </summary>
    [ObservableProperty] private bool _installationFailed;

    /// <summary>
    /// Error message if installation failed.
    /// </summary>
    [ObservableProperty] private string _installationErrorMessage = string.Empty;

    /// <summary>
    /// Callback action to execute when removing this item.
    /// </summary>
    private readonly Action<ContentItemViewModel>? _onRemove;

    /// <summary>
    /// Initializes a new instance of the <see cref="ContentItemViewModel"/> class.
    /// </summary>
    public ContentItemViewModel()
    {
        RemoveCommand = new RelayCommand(Remove);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ContentItemViewModel"/> class with STFS file data.
    /// </summary>
    /// <param name="stfsFile">The STFS file to display.</param>
    /// <param name="onRemove">Callback action when this item is removed.</param>
    public ContentItemViewModel(StfsFile stfsFile, Action<ContentItemViewModel>? onRemove = null) : this()
    {
        StfsFile = stfsFile;
        _onRemove = onRemove;

        // Set display name from STFS metadata
        if (!string.IsNullOrEmpty(stfsFile.Metadata.DisplayName))
        {
            DisplayName = stfsFile.Metadata.DisplayName;
        }
        else if (!string.IsNullOrEmpty(stfsFile.Metadata.TitleName))
        {
            DisplayName = stfsFile.Metadata.TitleName;
        }
        else
        {
            DisplayName = Path.GetFileNameWithoutExtension(stfsFile.PackagePath ?? string.Empty);
        }
        ContentType = $"{stfsFile.Metadata.ContentType.ToDisplayString()} ({stfsFile.Metadata.ContentType.ToHexString()})";
        FilePath = stfsFile.PackagePath ?? string.Empty;

        // Load thumbnail images
        if (stfsFile.Metadata.ThumbnailImage.Length > 0)
        {
            try
            {
                using MemoryStream ms = new MemoryStream(stfsFile.Metadata.ThumbnailImage);
                ThumbnailImage = new Bitmap(ms);
            }
            catch (Exception)
            {
                ThumbnailImage = null;
            }
        }

        if (stfsFile.Metadata.TitleThumbnailImage.Length > 0)
        {
            try
            {
                using MemoryStream ms = new MemoryStream(stfsFile.Metadata.TitleThumbnailImage);
                TitleThumbnailImage = new Bitmap(ms);
            }
            catch (Exception)
            {
                TitleThumbnailImage = null;
            }
        }
    }

    /// <summary>
    /// Removes this content item from the list.
    /// </summary>
    private void Remove()
    {
        _onRemove?.Invoke(this);
    }
}