using System;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using XeniaManager.BigScreen.Constants;
using XeniaManager.BigScreen.Models.Settings;
using XeniaManager.BigScreen.Utilities;
using XeniaManager.Core.Models;

namespace XeniaManager.BigScreen.ViewModels.Items;

/// <summary>
/// ViewModel for a single screenshot tile in the gallery.
/// </summary>
public partial class ScreenshotItemViewModel(
    XeniaVersion version,
    string path,
    string title,
    DateTime capturedAt,
    string gameTitle,
    Bitmap? thumbnail)
    : ObservableObject, ISelectable
{
    /// <summary>
    /// Whether this tile currently has focus/selection in the gallery.
    /// </summary>
    [ObservableProperty]
    public partial bool IsSelected { get; set; }

    /// <summary>
    /// The hour format used by the capture date, following the persisted setting.
    /// </summary>
    [ObservableProperty]
    public partial TimeFormat TimeFormat { get; set; } = TimeFormat.TwelveHour;

    /// <summary>
    /// The emulator version the screenshot was captured with.
    /// </summary>
    public XeniaVersion Version { get; } = version;

    /// <summary>
    /// The screenshot file name (shown in the modal as a subtitle).
    /// </summary>
    public string Title { get; } = title;

    /// <summary>
    /// When the screenshot was captured (file write time).
    /// </summary>
    public DateTime CapturedAt { get; } = capturedAt;

    /// <summary>
    /// The capture date formatted for display (e.g. "5 Aug 2026, 14:30").
    /// </summary>
    public string CapturedAtText
    {
        get
        {
            return CapturedAt.ToString(FormatConstants.GetCaptureDateFormat(TimeFormat));
        }
    }

    /// <summary>
    /// The game title the screenshot belongs to, when it can be matched to the library.
    /// </summary>
    public string GameTitle { get; } = gameTitle;

    /// <summary>
    /// The full path to the screenshot file on disk.
    /// </summary>
    public string Path { get; } = path;

    /// <summary>
    /// The screenshot's decoded thumbnail (card-sized), or null when unreadable.
    /// </summary>
    public Bitmap? Thumbnail { get; } = thumbnail;

    /// <summary>
    /// Whether the thumbnail loaded successfully.
    /// </summary>
    public bool HasImage
    {
        get
        {
            return Thumbnail != null;
        }
    }

    partial void OnTimeFormatChanged(TimeFormat value) => OnPropertyChanged(nameof(CapturedAtText));
}