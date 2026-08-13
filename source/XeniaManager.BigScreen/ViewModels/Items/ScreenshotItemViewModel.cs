using System;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using XeniaManager.BigScreen.Constants;
using XeniaManager.BigScreen.Models;
using XeniaManager.BigScreen.Utilities;

namespace XeniaManager.BigScreen.ViewModels.Items;

/// <summary>
/// ViewModel for a single screenshot tile in the gallery.
/// </summary>
public partial class ScreenshotItemViewModel(
    string path,
    string title,
    DateTime capturedAt,
    string gameTitle,
    Bitmap? image)
    : ObservableObject, ISelectable
{
    /// <summary>
    /// Whether this tile currently has focus/selection in the gallery.
    /// </summary>
    [ObservableProperty] private bool _isSelected;

    /// <summary>
    /// The hour format used by the capture date, following the persisted setting.
    /// </summary>
    [ObservableProperty] private TimeFormat _timeFormat = TimeFormat.TwelveHour;

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
    public string CapturedAtText => CapturedAt.ToString(FormatConstants.GetCaptureDateFormat(TimeFormat));

    /// <summary>
    /// The game title the screenshot belongs to, when it can be matched to the library.
    /// </summary>
    public string GameTitle { get; } = gameTitle;

    /// <summary>
    /// The full path to the screenshot file on disk.
    /// </summary>
    public string Path { get; } = path;

    /// <summary>
    /// The screenshot image, or null when unreadable.
    /// </summary>
    public Bitmap? Image { get; } = image;

    /// <summary>
    /// Whether the image loaded successfully.
    /// </summary>
    public bool HasImage => Image != null;

    partial void OnTimeFormatChanged(TimeFormat value) => OnPropertyChanged(nameof(CapturedAtText));
}