using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using XeniaManager.Core.Utilities;
using XeniaManager.Files;
using XeniaManager.Files.Models.Gpd;
using XeniaManager.ViewModels.Items;

namespace XeniaManager.ViewModels.Controls;

/// <summary>
/// A single XDBF/GPD entry table row shown in the GPD viewer.
/// </summary>
public sealed class GpdEntryRow
{
    /// <summary>Entry namespace name (Achievement, Image, Setting, Title, String).</summary>
    public string Namespace { get; init; } = string.Empty;

    /// <summary>Entry ID in hex.</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>Entry data size (formatted).</summary>
    public string Size { get; init; } = string.Empty;
}

/// <summary>
/// ViewModel for the read-only GPD entry viewer dialog.
/// </summary>
public partial class GpdViewerDialogViewModel : ViewModelBase, IDisposable
{
    private GpdFile? _gpd;
    private bool _disposed;

    /// <summary>The displayed file name.</summary>
    [ObservableProperty] private string _fileName = string.Empty;

    /// <summary>Achievement rows with names, gamerscore and unlock state.</summary>
    [ObservableProperty] private ObservableCollection<AchievementViewModel> _achievements = [];

    /// <summary>Raw entry table rows (namespace, ID, size).</summary>
    [ObservableProperty] private ObservableCollection<GpdEntryRow> _entries = [];

    /// <summary>Whether the GPD holds any achievements.</summary>
    public bool HasAchievements
    {
        get
        {
            return Achievements.Count > 0;
        }
    }

    /// <summary>Whether the GPD holds any entries.</summary>
    public bool HasEntries
    {
        get
        {
            return Entries.Count > 0;
        }
    }

    public GpdViewerDialogViewModel(string fileName, GpdFile gpd)
    {
        _fileName = fileName;
        _gpd = gpd;
        foreach (AchievementEntry achievement in gpd.Achievements)
        {
            Achievements.Add(new AchievementViewModel(achievement, gpd));
        }

        foreach (EntryTableEntry entry in gpd.Entries)
        {
            Entries.Add(new GpdEntryRow
            {
                Namespace = entry.Namespace.ToString(),
                Id = $"0x{entry.Id:X}",
                Size = FileSizeFormatter.FormatBytes((long)entry.Length)
            });
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        foreach (AchievementViewModel achievement in Achievements)
        {
            (achievement.AchievementImage as IDisposable)?.Dispose();
        }

        Achievements.Clear();
        Entries.Clear();
        _gpd?.Dispose();
        _gpd = null;
        GC.SuppressFinalize(this);
    }
}