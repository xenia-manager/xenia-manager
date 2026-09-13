using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using XeniaManager.Core.Services;
using XeniaManager.Core.Utilities;
using XeniaManager.Files;
using XeniaManager.Logging;
using XeniaManager.Services;

namespace XeniaManager.ViewModels.Controls;

/// <summary>
/// A single SPA title row shown in the XEX file dialog.
/// </summary>
public sealed class XexTitleRow
{
    /// <summary>Title name with ID.</summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>Achievement count and gamerscore summary.</summary>
    public string Detail { get; init; } = string.Empty;
}

/// <summary>
/// A single SPA context row shown in the XEX file dialog.
/// </summary>
public sealed class XexContextRow
{
    /// <summary>Context ID in hex.</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>Resolved context name (may be empty when the string table has no entry).</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>Max/default value summary.</summary>
    public string Detail { get; init; } = string.Empty;
}

/// <summary>
/// A single SPA property row shown in the XEX file dialog.
/// </summary>
public sealed class XexPropertyRow
{
    /// <summary>Property ID in hex.</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>Resolved property name (may be empty when the string table has no entry).</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>Data size (formatted).</summary>
    public string Detail { get; init; } = string.Empty;
}

/// <summary>
/// A single SPA stats view row shown in the XEX file dialog.
/// </summary>
public sealed class XexStatsViewRow
{
    /// <summary>View ID in hex.</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>Resolved view name (may be empty when the string table has no entry).</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>Column/row count summary.</summary>
    public string Detail { get; init; } = string.Empty;
}

/// <summary>
/// A single SPA achievement row shown in the XEX file dialog, with locked and
/// unlocked descriptions and icons. The locked view is shown by default.
/// </summary>
public sealed partial class SpaAchievementRow : ObservableObject
{
    /// <summary>Achievement name.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>Achieved (unlocked) description.</summary>
    public string UnlockedDescription { get; init; } = string.Empty;

    /// <summary>Unachieved (locked) description.</summary>
    public string LockedDescription { get; init; } = string.Empty;

    /// <summary>Gamerscore value.</summary>
    public ushort Gamerscore { get; init; }

    /// <summary>Achievement icon image.</summary>
    public Bitmap? UnlockedImage { get; init; }

    /// <summary>Grayscale achievement icon shown in the locked view.</summary>
    public Bitmap? LockedImage { get; init; }

    /// <summary>Whether the unlocked description and icon are shown.</summary>
    [ObservableProperty] private bool _showUnlocked;

    /// <summary>Currently shown description.</summary>
    public string DisplayDescription
    {
        get
        {
            return ShowUnlocked ? UnlockedDescription : LockedDescription;
        }
    }

    /// <summary>Currently shown icon.</summary>
    public Bitmap? DisplayImage
    {
        get
        {
            return ShowUnlocked ? UnlockedImage : LockedImage;
        }
    }

    /// <summary>Whether an icon is available for the current view.</summary>
    public bool HasDisplayImage
    {
        get
        {
            return DisplayImage != null;
        }
    }

    partial void OnShowUnlockedChanged(bool value)
    {
        OnPropertyChanged(nameof(DisplayDescription));
        OnPropertyChanged(nameof(DisplayImage));
        OnPropertyChanged(nameof(HasDisplayImage));
    }
}

/// <summary>
/// ViewModel for the XEX file dialog, which shows the executable header and the
/// embedded SPA details (achievements, titles, contexts, properties, stats views)
/// read-only.
/// </summary>
public partial class XexFileDialogViewModel : ViewModelBase, IDisposable
{
    private readonly byte[] _xexBytes;
    private readonly IMessageBoxService _messageBoxService;
    private byte[]? _iconBytes;
    private bool _disposed;
    private bool _loaded;

    /// <summary>
    /// Resolves the top-level window hosting this dialog, used to parent message boxes and pickers.
    /// Set by the dialog view; null when the dialog is not shown.
    /// </summary>
    public Func<TopLevel?>? OwnerProvider { get; set; }

    private TopLevel? OwnerWindow
    {
        get
        {
            return OwnerProvider?.Invoke();
        }
    }

    /// <summary>The displayed file name.</summary>
    [ObservableProperty] private string _fileName = string.Empty;

    /// <summary>XEX title ID (hex).</summary>
    [ObservableProperty] private string _xexTitleId = string.Empty;

    /// <summary>XEX media ID (hex).</summary>
    [ObservableProperty] private string _xexMediaId = string.Empty;

    /// <summary>XEX version (hex).</summary>
    [ObservableProperty] private string _xexVersion = string.Empty;

    /// <summary>XEX base version (hex).</summary>
    [ObservableProperty] private string _xexBaseVersion = string.Empty;

    /// <summary>XEX executable type (Retail/Debug).</summary>
    [ObservableProperty] private string _xexExecutableType = string.Empty;

    /// <summary>XEX disc number / total.</summary>
    [ObservableProperty] private string _xexDisc = string.Empty;

    /// <summary>XEX image size (formatted).</summary>
    [ObservableProperty] private string _xexImageSize = string.Empty;

    /// <summary>XEX module flags (hex).</summary>
    [ObservableProperty] private string _xexModuleFlags = string.Empty;

    /// <summary>Whether the XEX holds an embedded SPA.</summary>
    [ObservableProperty] private bool _hasSpaDetails;

    /// <summary>SPA title name.</summary>
    [ObservableProperty] private string _spaTitle = string.Empty;

    /// <summary>SPA default language.</summary>
    [ObservableProperty] private string _spaDefaultLanguage = string.Empty;

    /// <summary>SPA title type.</summary>
    [ObservableProperty] private string _spaTitleType = string.Empty;

    /// <summary>SPA title version.</summary>
    [ObservableProperty] private string _spaTitleVersion = string.Empty;

    /// <summary>SPA achievement count.</summary>
    [ObservableProperty] private string _spaAchievements = string.Empty;

    /// <summary>SPA total gamerscore.</summary>
    [ObservableProperty] private string _spaGamerscore = string.Empty;

    /// <summary>XEX/SPA title icon image.</summary>
    [ObservableProperty] private Bitmap? _selectedIcon;

    /// <summary>Whether a title icon is available.</summary>
    [ObservableProperty] private bool _hasSelectedIcon;

    /// <summary>SPA achievement rows.</summary>
    [ObservableProperty] private ObservableCollection<SpaAchievementRow> _achievements = [];

    /// <summary>Whether the SPA holds any achievements.</summary>
    [ObservableProperty] private bool _hasAchievements;

    /// <summary>Whether achievements show unlocked descriptions and icons instead of locked ones.</summary>
    [ObservableProperty] private bool _showUnlockedAchievements;

    /// <summary>SPA title rows.</summary>
    [ObservableProperty] private ObservableCollection<XexTitleRow> _titles = [];

    /// <summary>Whether the SPA holds any titles.</summary>
    [ObservableProperty] private bool _hasTitles;

    /// <summary>SPA context rows.</summary>
    [ObservableProperty] private ObservableCollection<XexContextRow> _contexts = [];

    /// <summary>Whether the SPA holds any contexts.</summary>
    [ObservableProperty] private bool _hasContexts;

    /// <summary>SPA property rows.</summary>
    [ObservableProperty] private ObservableCollection<XexPropertyRow> _properties = [];

    /// <summary>Whether the SPA holds any properties.</summary>
    [ObservableProperty] private bool _hasProperties;

    /// <summary>SPA stats view rows.</summary>
    [ObservableProperty] private ObservableCollection<XexStatsViewRow> _statsViews = [];

    /// <summary>Whether the SPA holds any stats views.</summary>
    [ObservableProperty] private bool _hasStatsViews;

    public XexFileDialogViewModel(string fileName, byte[] xexBytes)
    {
        _fileName = fileName;
        _xexBytes = xexBytes;
        _messageBoxService = App.Services.GetRequiredService<IMessageBoxService>();
    }

    /// <summary>
    /// Parses the XEX bytes. Returns false when the data is not a valid XEX.
    /// </summary>
    public async Task<bool> LoadAsync()
    {
        if (_disposed || _loaded)
        {
            return _loaded;
        }

        EventManager.Instance.DisableWindow();
        try
        {
            return await LoadCoreAsync();
        }
        finally
        {
            EventManager.Instance.EnableWindow();
        }
    }

    private async Task<bool> LoadCoreAsync()
    {
        ParsedXex? parsed = await Task.Run(TryParse);
        if (_disposed || parsed == null)
        {
            return false;
        }

        XexTitleId = parsed.TitleId;
        XexMediaId = parsed.MediaId;
        XexVersion = parsed.Version;
        XexBaseVersion = parsed.BaseVersion;
        XexExecutableType = parsed.ExecutableType;
        XexDisc = parsed.Disc;
        XexImageSize = parsed.ImageSize;
        XexModuleFlags = parsed.ModuleFlags;
        HasSpaDetails = parsed.HasSpa;
        SpaTitle = parsed.SpaTitle;
        SpaDefaultLanguage = parsed.SpaDefaultLanguage;
        SpaTitleType = parsed.SpaTitleType;
        SpaTitleVersion = parsed.SpaTitleVersion;
        SpaAchievements = parsed.SpaAchievements;
        SpaGamerscore = parsed.SpaGamerscore;
        _iconBytes = parsed.Icon;

        List<SpaAchievementRow> achievementRows = new List<SpaAchievementRow>();
        foreach (SpaAchievementDetails achievement in parsed.AchievementList)
        {
            Bitmap? image = DecodeBitmap(achievement.Image);
            Bitmap? lockedImage = achievement.Image != null
                ? GrayscaleImage.ToGrayscale(achievement.Image) ?? image
                : image;
            achievementRows.Add(new SpaAchievementRow
            {
                Name = achievement.Name,
                UnlockedDescription = achievement.Description,
                LockedDescription = achievement.LockedDescription,
                Gamerscore = achievement.Gamerscore,
                UnlockedImage = image,
                LockedImage = lockedImage,
                ShowUnlocked = ShowUnlockedAchievements
            });
        }

        Achievements = new ObservableCollection<SpaAchievementRow>(achievementRows);
        HasAchievements = achievementRows.Count > 0;
        Titles = new ObservableCollection<XexTitleRow>(parsed.Titles);
        HasTitles = parsed.Titles.Count > 0;
        Contexts = new ObservableCollection<XexContextRow>(parsed.Contexts);
        HasContexts = parsed.Contexts.Count > 0;
        Properties = new ObservableCollection<XexPropertyRow>(parsed.Properties);
        HasProperties = parsed.Properties.Count > 0;
        StatsViews = new ObservableCollection<XexStatsViewRow>(parsed.StatsViews);
        HasStatsViews = parsed.StatsViews.Count > 0;

        if (_disposed)
        {
            return false;
        }

        if (parsed.Icon != null)
        {
            Bitmap? icon = DecodeBitmap(parsed.Icon);
            if (icon != null)
            {
                SelectedIcon = icon;
                HasSelectedIcon = true;
            }
        }

        _loaded = true;
        return true;
    }

    /// <summary>
    /// Saves the title icon to a user-chosen PNG file.
    /// </summary>
    [RelayCommand]
    private async Task ExtractXexIcon()
    {
        if (_disposed || _iconBytes == null)
        {
            await _messageBoxService.ShowInfoAsync(
                LocalizationHelper.GetText("GameFilesDialog.Extract.NoSelection.Title"),
                LocalizationHelper.GetText("GameFilesDialog.Extract.NoSelection.Message"),
                owner: OwnerWindow);
            return;
        }

        if ((OwnerWindow ?? App.MainWindow)?.StorageProvider is not { } storageProvider)
        {
            await _messageBoxService.ShowErrorAsync(
                LocalizationHelper.GetText("GameFilesDialog.MissingStorageProvider.Title"),
                LocalizationHelper.GetText("GameFilesDialog.MissingStorageProvider.Message"),
                owner: OwnerWindow);
            return;
        }

        IStorageFile? picked = await storageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = LocalizationHelper.GetText("GameFilesDialog.XexIcon.FilePicker.Title"),
            FileTypeChoices =
            [
                new FilePickerFileType("PNG image")
                {
                    Patterns = ["*.png"]
                }
            ],
            SuggestedFileName = Path.GetFileNameWithoutExtension(FileName) + ".png",
            DefaultExtension = "png",
            ShowOverwritePrompt = true
        });
        if (picked == null || _disposed)
        {
            return;
        }

        try
        {
            string outputPath = picked.Path.LocalPath;
            await File.WriteAllBytesAsync(outputPath, _iconBytes);
            await _messageBoxService.ShowInfoAsync(
                LocalizationHelper.GetText("GameFilesDialog.XexIcon.Success.Title"),
                string.Format(LocalizationHelper.GetText("GameFilesDialog.XexIcon.Success.Message"), outputPath),
                owner: OwnerWindow);
        }
        catch (Exception ex)
        {
            Logger.Error<XexFileDialogViewModel>($"Failed to save XEX icon for '{FileName}'");
            Logger.LogExceptionDetails<XexFileDialogViewModel>(ex);
            await _messageBoxService.ShowErrorAsync(
                LocalizationHelper.GetText("GameFilesDialog.XexIcon.Failed.Title"),
                string.Format(LocalizationHelper.GetText("GameFilesDialog.XexIcon.Failed.Message"), ex.Message),
                owner: OwnerWindow);
        }
    }

    partial void OnShowUnlockedAchievementsChanged(bool value)
    {
        foreach (SpaAchievementRow row in Achievements)
        {
            row.ShowUnlocked = value;
        }
    }

    private static Bitmap? DecodeBitmap(byte[]? bytes)
    {
        if (bytes == null || bytes.Length == 0)
        {
            return null;
        }

        try
        {
            using MemoryStream stream = new MemoryStream(bytes, false);
            return new Bitmap(stream);
        }
        catch (Exception ex)
        {
            Logger.Trace<XexFileDialogViewModel>($"Failed to decode image: {ex.Message}");
            return null;
        }
    }

    private ParsedXex? TryParse()
    {
        try
        {
            XexFile xex = XexFile.FromBytes(_xexBytes);
            if (!xex.IsValid)
            {
                return null;
            }

            string version = "–";
            string baseVersion = "–";
            string executableType = "–";
            string disc = "–";
            if (xex.Execution.HasValue)
            {
                version = $"0x{xex.Execution.Value.Version:X8}";
                baseVersion = $"0x{xex.Execution.Value.BaseVersion:X8}";
                executableType = xex.Execution.Value.ExecutableType switch
                {
                    0x00 => "Retail",
                    0x01 => "Debug",
                    0x02 => "Debug Retail",
                    _ => $"Unknown (0x{xex.Execution.Value.ExecutableType:X2})"
                };
                disc = $"{xex.Execution.Value.DiscNum} / {xex.Execution.Value.DiscTotal}";
            }

            ParsedXex parsed = new ParsedXex(
                xex.TitleId,
                xex.MediaId,
                version,
                baseVersion,
                executableType,
                disc,
                FileSizeFormatter.FormatBytes(xex.SecurityInfo.ImageSize),
                $"0x{xex.Header.ModuleFlags:X8}");

            if (!xex.TryGetSpaFile(out SpaFile? spa) || spa == null)
            {
                return parsed;
            }

            using (spa)
            {
                ushort language = (ushort)spa.DefaultLanguage;
                parsed.HasSpa = true;
                parsed.SpaTitle = spa.TitleName();
                parsed.SpaDefaultLanguage = spa.DefaultLanguage.ToString();
                parsed.SpaTitleType = spa.TitleType.ToString();
                parsed.SpaTitleVersion = spa.TitleHeader is { } header
                    ? $"v{header.Major}.{header.Minor}.{header.Build}.{header.Revision} (flags 0x{header.Flags:X8})"
                    : string.Empty;
                parsed.SpaAchievements = spa.SpaAchievements.Count.ToString();
                parsed.SpaGamerscore = spa.TotalGamerscore.ToString();
                parsed.Icon = spa.GetTitleIcon() ?? spa.GetAnyValidIcon();

                foreach (Files.Models.Spa.SpaAchievement achievement in spa.SpaAchievements)
                {
                    string description = spa.GetString(language, achievement.DescriptionId);
                    string lockedDescription = spa.GetString(language, achievement.UnachievedId);
                    byte[]? image = spa.GetImage(achievement.ImageId)?.ImageData;
                    parsed.AchievementList.Add(new SpaAchievementDetails(
                        spa.GetString(language, achievement.LabelId),
                        description,
                        string.IsNullOrWhiteSpace(lockedDescription) ? description : lockedDescription,
                        achievement.Gamerscore,
                        image));
                }

                foreach (Files.Models.Gpd.TitleEntry title in spa.Titles)
                {
                    string titleName = string.IsNullOrWhiteSpace(title.TitleName)
                        ? $"0x{title.TitleId:X8}"
                        : $"{title.TitleName} (0x{title.TitleId:X8})";
                    parsed.Titles.Add(new XexTitleRow
                    {
                        Title = titleName,
                        Detail = $"{title.AchievementCount} achievements, {title.GamerscoreTotal}G"
                    });
                }

                foreach (Files.Models.Spa.SpaContext context in spa.Contexts)
                {
                    parsed.Contexts.Add(new XexContextRow
                    {
                        Id = $"0x{context.Id:X8}",
                        Name = spa.GetString(language, context.StringId),
                        Detail = $"Max {context.MaxValue}, default {context.DefaultValue}"
                    });
                }

                foreach (Files.Models.Spa.SpaProperty property in spa.Properties)
                {
                    parsed.Properties.Add(new XexPropertyRow
                    {
                        Id = $"0x{property.Id:X8}",
                        Name = spa.GetString(language, property.StringId),
                        Detail = FileSizeFormatter.FormatBytes(property.DataSize)
                    });
                }

                foreach (Files.Models.Spa.SpaStatsView view in spa.StatsViews)
                {
                    parsed.StatsViews.Add(new XexStatsViewRow
                    {
                        Id = $"0x{view.Id:X8}",
                        Name = spa.GetString(language, view.StringId),
                        Detail = $"{view.Columns.Count} columns, {view.Rows.Count} rows"
                    });
                }
            }

            return parsed;
        }
        catch (Exception ex)
        {
            Logger.Trace<XexFileDialogViewModel>($"Failed to parse XEX '{FileName}': {ex.Message}");
            return null;
        }
    }

    private sealed record SpaAchievementDetails(
        string Name,
        string Description,
        string LockedDescription,
        ushort Gamerscore,
        byte[]? Image);

    private sealed class ParsedXex(
        string titleId,
        string mediaId,
        string version,
        string baseVersion,
        string executableType,
        string disc,
        string imageSize,
        string moduleFlags)
    {
        public string TitleId { get; } = titleId;
        public string MediaId { get; } = mediaId;
        public string Version { get; } = version;
        public string BaseVersion { get; } = baseVersion;
        public string ExecutableType { get; } = executableType;
        public string Disc { get; } = disc;
        public string ImageSize { get; } = imageSize;
        public string ModuleFlags { get; } = moduleFlags;
        public bool HasSpa { get; set; }
        public string SpaTitle { get; set; } = string.Empty;
        public string SpaDefaultLanguage { get; set; } = string.Empty;
        public string SpaTitleType { get; set; } = string.Empty;
        public string SpaTitleVersion { get; set; } = string.Empty;
        public string SpaAchievements { get; set; } = string.Empty;
        public string SpaGamerscore { get; set; } = string.Empty;
        public byte[]? Icon { get; set; }
        public List<SpaAchievementDetails> AchievementList { get; } = [];
        public List<XexTitleRow> Titles { get; } = [];
        public List<XexContextRow> Contexts { get; } = [];
        public List<XexPropertyRow> Properties { get; } = [];
        public List<XexStatsViewRow> StatsViews { get; } = [];
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        OwnerProvider = null;
        SelectedIcon?.Dispose();
        SelectedIcon = null;
        foreach (SpaAchievementRow row in Achievements)
        {
            row.UnlockedImage?.Dispose();
            if (!ReferenceEquals(row.LockedImage, row.UnlockedImage))
            {
                row.LockedImage?.Dispose();
            }
        }

        Achievements.Clear();
        Titles.Clear();
        Contexts.Clear();
        Properties.Clear();
        StatsViews.Clear();
        _iconBytes = null;
        GC.SuppressFinalize(this);
    }
}