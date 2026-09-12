using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;
using Avalonia.Media.Imaging;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using XeniaManager.Core.Utilities;
using XeniaManager.Files;
using XeniaManager.Files.Browsing;
using XeniaManager.Files.Models.Spa;
using XeniaManager.Logging;
using XeniaManager.Services;

namespace XeniaManager.ViewModels.Controls;

/// <summary>
/// A single node of the game files tree, wrapping a <see cref="GameFileNode"/>
/// with its preloaded children and expansion state.
/// </summary>
public partial class GameFileTreeNode : ObservableObject
{
    /// <summary>The wrapped file or directory entry.</summary>
    public GameFileNode Entry { get; }

    /// <summary>Child nodes (empty for files).</summary>
    public ObservableCollection<GameFileTreeNode> Children { get; } = [];

    /// <summary>Whether the node is expanded in the tree.</summary>
    [ObservableProperty] private bool _isExpanded;

    public GameFileTreeNode(GameFileNode entry)
    {
        Entry = entry;
    }
}

/// <summary>
/// A single SPA achievement row shown in the game files details pane.
/// </summary>
public sealed class SpaAchievementRow
{
    /// <summary>Achievement name.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>Achieved description.</summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>Gamerscore value.</summary>
    public ushort Gamerscore { get; init; }

    /// <summary>Achievement icon image.</summary>
    public Bitmap? Image { get; init; }

    /// <summary>Whether an icon image is available.</summary>
    public bool HasImage { get; init; }
}

/// <summary>
/// ViewModel for the Game Files dialog, which browses a game's container
/// (ISO, ZAR, STFS, SVOD, or loose directory) read-only, with XEX/SPA details.
/// </summary>
public partial class GameFilesDialogViewModel : ViewModelBase, IDisposable
{
    private const int MaxSearchResults = 200;
    private const int MaxPreviewBytes = 64 * 1024 * 1024;
    private static readonly string[] PreviewableExtensions = [".png", ".jpg", ".jpeg", ".bmp"];

    private readonly string _gamePath;
    private readonly IMessageBoxService _messageBoxService;
    private readonly Dictionary<string, GameFileTreeNode> _nodeLookup = new Dictionary<string, GameFileTreeNode>(StringComparer.OrdinalIgnoreCase);
    private IGameFileSource? _source;
    private int _detailsLoadId;
    private bool _disposed;

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

    /// <summary>The game title shown in the dialog.</summary>
    [ObservableProperty] private string _gameTitle = string.Empty;

    /// <summary>Root nodes of the game files tree.</summary>
    [ObservableProperty] private ObservableCollection<GameFileTreeNode> _rootNodes = [];

    /// <summary>Currently selected tree node (drives the details pane).</summary>
    [ObservableProperty] private GameFileTreeNode? _selectedTreeNode;

    /// <summary>Currently selected entry (drives the details pane).</summary>
    [ObservableProperty] private GameFileNode? _selectedEntry;

    /// <summary>Whether the tree holds any entries.</summary>
    [ObservableProperty] private bool _hasContent;

    /// <summary>Whether the empty-tree overlay is visible.</summary>
    [ObservableProperty] private bool _isTreeEmptyVisible;

    /// <summary>Live file search text. Non-empty switches the list to flat results.</summary>
    [ObservableProperty] private string _searchText = string.Empty;

    /// <summary>Flat search results for the current search text.</summary>
    [ObservableProperty] private ObservableCollection<GameFileNode> _searchResults = [];

    /// <summary>Whether the list shows search results instead of the current directory.</summary>
    [ObservableProperty] private bool _isSearching;

    /// <summary>Whether the no-results overlay is visible.</summary>
    [ObservableProperty] private bool _isSearchEmptyVisible;

    /// <summary>Whether the details pane is visible.</summary>
    [ObservableProperty] private bool _hasSelection;

    /// <summary>Selected entry display name.</summary>
    [ObservableProperty] private string _selectedName = string.Empty;

    /// <summary>Selected entry display size (or the folder label).</summary>
    [ObservableProperty] private string _selectedSizeText = string.Empty;

    /// <summary>Selected entry full browsing path.</summary>
    [ObservableProperty] private string _selectedPath = string.Empty;

    /// <summary>Whether the selected entry is an XEX with parsed details.</summary>
    [ObservableProperty] private bool _isXexSelected;

    /// <summary>Whether an XEX/SPA icon is available.</summary>
    [ObservableProperty] private bool _hasSelectedIcon;

    /// <summary>XEX/SPA icon image.</summary>
    [ObservableProperty] private Bitmap? _selectedIcon;

    /// <summary>Decoded preview of the selected image file.</summary>
    [ObservableProperty] private Bitmap? _previewImage;

    /// <summary>Whether an image-file preview is available.</summary>
    [ObservableProperty] private bool _hasPreviewImage;

    /// <summary>SPA achievement rows for the selected XEX.</summary>
    [ObservableProperty] private ObservableCollection<SpaAchievementRow> _spaAchievementRows = [];

    /// <summary>Whether the selected XEX has SPA achievements to list.</summary>
    [ObservableProperty] private bool _hasAchievements;

    /// <summary>XEX title ID (hex).</summary>
    [ObservableProperty] private string _xexTitleId = string.Empty;

    /// <summary>XEX media ID (hex).</summary>
    [ObservableProperty] private string _xexMediaId = string.Empty;

    /// <summary>XEX version (hex).</summary>
    [ObservableProperty] private string _xexVersion = string.Empty;

    /// <summary>XEX disc number / total.</summary>
    [ObservableProperty] private string _xexDisc = string.Empty;

    /// <summary>XEX image size (formatted).</summary>
    [ObservableProperty] private string _xexImageSize = string.Empty;

    /// <summary>XEX module flags (hex).</summary>
    [ObservableProperty] private string _xexModuleFlags = string.Empty;

    /// <summary>SPA title name.</summary>
    [ObservableProperty] private string _spaTitle = string.Empty;

    /// <summary>SPA achievement count.</summary>
    [ObservableProperty] private string _spaAchievements = string.Empty;

    /// <summary>SPA total gamerscore.</summary>
    [ObservableProperty] private string _spaGamerscore = string.Empty;

    public GameFilesDialogViewModel(string gamePath, string gameTitle)
    {
        _gamePath = gamePath;
        GameTitle = gameTitle;
        _messageBoxService = App.Services.GetRequiredService<IMessageBoxService>();
    }

    /// <summary>
    /// Opens the game container. Returns false when the path is missing or unsupported.
    /// </summary>
    public async Task<bool> LoadAsync()
    {
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
        IGameFileSource? source = await Task.Run(() => GameFileSourceFactory.TryOpen(_gamePath));
        if (source == null)
        {
            return false;
        }

        _source = source;
        BuildTree();
        return true;
    }

    /// <summary>
    /// Preloads the full container tree (sorted directories-first).
    /// </summary>
    private void BuildTree()
    {
        RootNodes.Clear();
        _nodeLookup.Clear();
        if (_source != null)
        {
            foreach (GameFileNode child in SortedChildren(string.Empty))
            {
                RootNodes.Add(BuildNode(child));
            }
        }

        HasContent = RootNodes.Count > 0;
        IsTreeEmptyVisible = !HasContent;
    }

    private GameFileTreeNode BuildNode(GameFileNode entry)
    {
        GameFileTreeNode node = new GameFileTreeNode(entry);
        _nodeLookup[entry.FullPath] = node;
        if (!entry.IsFile)
        {
            foreach (GameFileNode child in SortedChildren(entry.FullPath))
            {
                node.Children.Add(BuildNode(child));
            }
        }

        return node;
    }

    private List<GameFileNode> SortedChildren(string path) =>
        _source?.ListDirectory(path)
            ?.OrderByDescending(e => !e.IsFile)
            .ThenBy(e => e.Name, StringComparer.OrdinalIgnoreCase)
            .ToList() ?? new List<GameFileNode>();

    /// <summary>
    /// Expands the tree to the given entry and selects it.
    /// </summary>
    /// <param name="entry">The entry to reveal.</param>
    public void RevealInTree(GameFileNode entry)
    {
        // Expand every ancestor so the node becomes visible.
        string prefix = string.Empty;
        foreach (string part in entry.FullPath.Split('/'))
        {
            prefix = prefix.Length == 0 ? part : $"{prefix}/{part}";
            if (_nodeLookup.TryGetValue(prefix, out GameFileTreeNode? ancestor))
            {
                ancestor.IsExpanded = true;
            }
        }

        if (_nodeLookup.TryGetValue(entry.FullPath, out GameFileTreeNode? node))
        {
            SelectedTreeNode = node;
        }
    }

    /// <summary>
    /// Reveals an activated search result in the tree and selects it.
    /// </summary>
    /// <param name="node">The activated search result.</param>
    public void RevealSearchResult(GameFileNode? node)
    {
        if (node == null)
        {
            return;
        }

        SearchText = string.Empty;
        RevealInTree(node);
    }

    partial void OnSelectedTreeNodeChanged(GameFileTreeNode? value) => SelectedEntry = value?.Entry;

    /// <summary>
    /// Opens the game location in the system file explorer.
    /// </summary>
    [RelayCommand]
    private async Task OpenExplorerFolder()
    {
        try
        {
            string? directory = Directory.Exists(_gamePath) ? _gamePath : Path.GetDirectoryName(_gamePath);
            if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
            {
                await _messageBoxService.ShowInfoAsync(
                    LocalizationHelper.GetText("GameFilesDialog.LoadError.Title"),
                    LocalizationHelper.GetText("GameFilesDialog.LoadError.Message"),
                    owner: OwnerWindow);
                return;
            }

            Process.Start(new ProcessStartInfo
            {
                FileName = directory,
                UseShellExecute = true,
                Verb = "open"
            });
        }
        catch (Exception ex)
        {
            Logger.Error<GameFilesDialogViewModel>($"Failed to open game location for: {GameTitle}");
            Logger.LogExceptionDetails<GameFilesDialogViewModel>(ex);
            await _messageBoxService.ShowErrorAsync(
                LocalizationHelper.GetText("GameFilesDialog.ExplorerError.Title"),
                string.Format(LocalizationHelper.GetText("GameFilesDialog.ExplorerError.Message"), ex.Message),
                owner: OwnerWindow);
        }
    }

    partial void OnSearchTextChanged(string value)
    {
        SelectedTreeNode = null;
        SelectedEntry = null;
        if (_source == null || string.IsNullOrWhiteSpace(value))
        {
            IsSearching = false;
            SearchResults.Clear();
            IsSearchEmptyVisible = false;
            return;
        }

        IsSearching = true;
        List<GameFileNode> matches = _source.Files
            .Where(f => f.Name.Contains(value, StringComparison.OrdinalIgnoreCase))
            .OrderBy(f => f.FullPath, StringComparer.OrdinalIgnoreCase)
            .Take(MaxSearchResults)
            .ToList();
        SearchResults = new ObservableCollection<GameFileNode>(matches);
        IsSearchEmptyVisible = matches.Count == 0;
    }

    partial void OnSelectedEntryChanged(GameFileNode? value)
    {
        _ = LoadDetailsAsync(value, ++_detailsLoadId);
        if (IsSearching && value != null)
        {
            RevealInTree(value);
        }
    }

    private async Task LoadDetailsAsync(GameFileNode? node, int loadId)
    {
        HasSelection = node != null;
        IsXexSelected = false;
        HasSelectedIcon = false;
        SelectedIcon = null;
        HasPreviewImage = false;
        PreviewImage = null;
        HasAchievements = false;
        SpaAchievementRows = [];
        if (node == null)
        {
            return;
        }

        SelectedName = node.Name;
        SelectedPath = node.FullPath;
        SelectedSizeText = node.IsFile
            ? FileSizeFormatter.FormatBytes((long)node.Size)
            : LocalizationHelper.GetText("GameFilesDialog.Folder.Label");

        if (node.IsFile && IsPreviewableImage(node.Name))
        {
            byte[]? imageBytes = await Task.Run(() => TryReadPreview(node.FullPath));
            if (loadId != _detailsLoadId)
            {
                return;
            }

            if (imageBytes != null)
            {
                try
                {
                    using MemoryStream imageStream = new MemoryStream(imageBytes);
                    PreviewImage = new Bitmap(imageStream);
                    HasPreviewImage = true;
                }
                catch (Exception ex)
                {
                    Logger.Trace<GameFilesDialogViewModel>($"Failed to decode image preview for '{node.FullPath}': {ex.Message}");
                }
            }
        }

        if (!node.IsFile || !node.Name.EndsWith(".xex", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        XexDetails? details = await Task.Run(() => TryParseXex(node.FullPath));
        if (loadId != _detailsLoadId || details == null)
        {
            return;
        }

        XexTitleId = details.TitleId;
        XexMediaId = details.MediaId;
        XexVersion = details.Version;
        XexDisc = details.Disc;
        XexImageSize = details.ImageSize;
        XexModuleFlags = details.ModuleFlags;
        SpaTitle = details.SpaTitle;
        SpaAchievements = details.Achievements;
        SpaGamerscore = details.Gamerscore;
        List<SpaAchievementRow> rows = new List<SpaAchievementRow>();
        foreach (SpaAchievementDetails achievement in details.AchievementList)
        {
            Bitmap? image = null;
            if (achievement.Image != null)
            {
                try
                {
                    using MemoryStream imageStream = new MemoryStream(achievement.Image);
                    image = new Bitmap(imageStream);
                }
                catch (Exception ex)
                {
                    Logger.Trace<GameFilesDialogViewModel>($"Failed to decode achievement icon '{achievement.Name}': {ex.Message}");
                }
            }

            rows.Add(new SpaAchievementRow
            {
                Name = achievement.Name,
                Description = achievement.Description,
                Gamerscore = achievement.Gamerscore,
                Image = image,
                HasImage = image != null
            });
        }

        SpaAchievementRows = new ObservableCollection<SpaAchievementRow>(rows);
        HasAchievements = rows.Count > 0;
        if (details.Icon != null)
        {
            try
            {
                using MemoryStream stream = new MemoryStream(details.Icon);
                SelectedIcon = new Bitmap(stream);
                HasSelectedIcon = true;
            }
            catch (Exception ex)
            {
                Logger.Trace<GameFilesDialogViewModel>($"Failed to decode XEX icon for '{node.FullPath}': {ex.Message}");
            }
        }

        IsXexSelected = true;
    }

    private static bool IsPreviewableImage(string fileName) =>
        PreviewableExtensions.Contains(Path.GetExtension(fileName).ToLowerInvariant());

    private byte[]? TryReadPreview(string fullPath)
    {
        try
        {
            byte[]? bytes = _source?.ReadFile(fullPath);
            if (bytes == null || bytes.Length == 0 || bytes.Length > MaxPreviewBytes)
            {
                return null;
            }

            return bytes;
        }
        catch (Exception ex)
        {
            Logger.Trace<GameFilesDialogViewModel>($"Failed to read preview for '{fullPath}': {ex.Message}");
            return null;
        }
    }

    private XexDetails? TryParseXex(string fullPath)
    {
        try
        {
            byte[]? bytes = _source?.ReadFile(fullPath);
            if (bytes == null || bytes.Length == 0)
            {
                return null;
            }

            XexFile xex = XexFile.FromBytes(bytes);
            if (!xex.IsValid)
            {
                return null;
            }

            string version = "–";
            string disc = "–";
            if (xex.Execution.HasValue)
            {
                version = $"0x{xex.Execution.Value.Version:X8}";
                disc = $"{xex.Execution.Value.DiscNum} / {xex.Execution.Value.DiscTotal}";
            }

            string spaTitle = string.Empty;
            string achievements = string.Empty;
            string gamerscore = string.Empty;
            byte[]? icon = null;
            List<SpaAchievementDetails> achievementList = new List<SpaAchievementDetails>();
            if (xex.TryGetSpaFile(out SpaFile? spa) && spa != null)
            {
                using (spa)
                {
                    spaTitle = spa.TitleName();
                    achievements = spa.SpaAchievements.Count.ToString();
                    gamerscore = spa.TotalGamerscore.ToString();
                    icon = spa.GetTitleIcon() ?? spa.GetAnyValidIcon();
                    ushort language = (ushort)spa.DefaultLanguage;
                    foreach (SpaAchievement achievement in spa.SpaAchievements)
                    {
                        achievementList.Add(new SpaAchievementDetails(
                            spa.GetString(language, achievement.LabelId),
                            spa.GetString(language, achievement.DescriptionId),
                            achievement.Gamerscore,
                            spa.GetImage(achievement.ImageId)?.ImageData));
                    }
                }
            }

            return new XexDetails(
                xex.TitleId,
                xex.MediaId,
                version,
                disc,
                FileSizeFormatter.FormatBytes(xex.SecurityInfo.ImageSize),
                $"0x{xex.Header.ModuleFlags:X8}",
                spaTitle,
                achievements,
                gamerscore,
                icon,
                achievementList);
        }
        catch (Exception ex)
        {
            Logger.Trace<GameFilesDialogViewModel>($"Failed to parse XEX '{fullPath}': {ex.Message}");
            return null;
        }
    }

    private sealed record SpaAchievementDetails(string Name, string Description, ushort Gamerscore, byte[]? Image);

    private sealed record XexDetails(
        string TitleId,
        string MediaId,
        string Version,
        string Disc,
        string ImageSize,
        string ModuleFlags,
        string SpaTitle,
        string Achievements,
        string Gamerscore,
        byte[]? Icon,
        IReadOnlyList<SpaAchievementDetails> AchievementList);

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

          _disposed = true;
          OwnerProvider = null;
          _source?.Dispose();
        _source = null;
        GC.SuppressFinalize(this);
    }
}