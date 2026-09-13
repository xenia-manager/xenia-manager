using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;
using Avalonia.Media.Imaging;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using XeniaManager.Controls;
using XeniaManager.Core.Services;
using XeniaManager.Core.Utilities;
using XeniaManager.Files;
using XeniaManager.Files.Browsing;
using XeniaManager.Files.Models.Spa;
using XeniaManager.Files.Utilities;
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

    /// <summary>Whether the node is visible under the current filter.</summary>
    [ObservableProperty] private bool _isVisible = true;

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
    private const int MaxPreviewBytes = 64 * 1024 * 1024;
    private const int MaxTextPreviewBytes = 1024 * 1024;
    private static readonly string[] PreviewableExtensions = [".png", ".jpg", ".jpeg", ".bmp"];
    private static readonly string[] TextPreviewExtensions = [".txt", ".ini", ".cfg", ".json", ".log", ".xml"];

    private readonly string _gamePath;
    private readonly IMessageBoxService _messageBoxService;
    private readonly Dictionary<string, GameFileTreeNode> _nodeLookup = new Dictionary<string, GameFileTreeNode>(StringComparer.OrdinalIgnoreCase);
    private IGameFileSource? _source;
    private int _detailsLoadId;
    private CancellationTokenSource? _searchCts;
    private const int SearchDebounceMs = 150;
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

    /// <summary>All selected tree nodes. Details are shown only when exactly one is selected.</summary>
    public ObservableCollection<GameFileTreeNode> SelectedTreeNodes { get; } = [];

    /// <summary>Whether the tree holds any entries.</summary>
    [ObservableProperty] private bool _hasContent;

    /// <summary>Whether the empty-tree overlay is visible.</summary>
    [ObservableProperty] private bool _isTreeEmptyVisible;

    /// <summary>Live file search text. Filters the tree in place.</summary>
    [ObservableProperty] private string _searchText = string.Empty;

    /// <summary>Whether the filtered no-results overlay is visible.</summary>
    [ObservableProperty] private bool _isSearchEmptyVisible;

    /// <summary>Whether the details pane is visible.</summary>
    [ObservableProperty] private bool _hasSelection;

    /// <summary>Whether exactly one file is selected and can be opened.</summary>
    public bool IsFileSelected
    {
        get
        {
            return SelectedEntry is { IsFile: true };
        }
    }

    /// <summary>Whether the current tree selection holds anything that can be extracted.</summary>
    public bool HasExtractSelection
    {
        get
        {
            return SelectedTreeNodes.Count > 0 || SelectedTreeNode != null;
        }
    }

    /// <summary>Selected entry display name.</summary>
    [ObservableProperty] private string _selectedName = string.Empty;

    /// <summary>Selected entry display size (or the folder label).</summary>
    [ObservableProperty] private string _selectedSizeText = string.Empty;

    /// <summary>Selected entry full browsing path.</summary>
    [ObservableProperty] private string _selectedPath = string.Empty;

    /// <summary>Whether the selected entry is an XEX with parsed details.</summary>
    [ObservableProperty] private bool _isXexSelected;

    /// <summary>Whether the selected entry is an STFS package with parsed details.</summary>
    [ObservableProperty] private bool _isStfsSelected;

    /// <summary>Whether an XEX/SPA or STFS icon is available.</summary>
    [ObservableProperty] private bool _hasSelectedIcon;

    /// <summary>XEX/SPA or STFS icon image.</summary>
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

    /// <summary>STFS signature type (CON/LIVE/PIRS).</summary>
    [ObservableProperty] private string _stfsSignatureType = string.Empty;

    /// <summary>STFS content type.</summary>
    [ObservableProperty] private string _stfsContentType = string.Empty;

    /// <summary>STFS title ID.</summary>
    [ObservableProperty] private string _stfsTitleId = string.Empty;

    /// <summary>STFS media ID.</summary>
    [ObservableProperty] private string _stfsMediaId = string.Empty;

    /// <summary>STFS display name.</summary>
    [ObservableProperty] private string _stfsDisplayName = string.Empty;

    /// <summary>STFS title name.</summary>
    [ObservableProperty] private string _stfsTitleName = string.Empty;

    /// <summary>STFS content size.</summary>
    [ObservableProperty] private string _stfsContentSize = string.Empty;

    /// <summary>Whether the SPA title row can be shown.</summary>
    public bool HasSpaTitle
    {
        get
        {
            return !string.IsNullOrWhiteSpace(SpaTitle);
        }
    }

    /// <summary>Whether the SPA achievements count row can be shown.</summary>
    public bool HasSpaAchievementsText
    {
        get
        {
            return !string.IsNullOrWhiteSpace(SpaAchievements);
        }
    }

    /// <summary>Whether the SPA gamerscore row can be shown.</summary>
    public bool HasSpaGamerscoreText
    {
        get
        {
            return !string.IsNullOrWhiteSpace(SpaGamerscore);
        }
    }

    /// <summary>Whether any SPA summary rows can be shown.</summary>
    public bool HasAnySpaSummary
    {
        get
        {
            return HasSpaTitle || HasSpaAchievementsText || HasSpaGamerscoreText;
        }
    }

    /// <summary>Whether the SPA summary section can be shown.</summary>
    public bool IsSpaSummaryVisible
    {
        get
        {
            return IsXexSelected && HasAnySpaSummary;
        }
    }

    /// <summary>Whether the STFS display name row can be shown.</summary>
    public bool HasStfsDisplayName
    {
        get
        {
            return !string.IsNullOrWhiteSpace(StfsDisplayName);
        }
    }

    /// <summary>Whether the STFS title name row can be shown.</summary>
    public bool HasStfsTitleName
    {
        get
        {
            return !string.IsNullOrWhiteSpace(StfsTitleName);
        }
    }

    /// <summary>Whether the STFS signature type row can be shown.</summary>
    public bool HasStfsSignatureType
    {
        get
        {
            return !string.IsNullOrWhiteSpace(StfsSignatureType);
        }
    }

    /// <summary>Whether the STFS content type row can be shown.</summary>
    public bool HasStfsContentType
    {
        get
        {
            return !string.IsNullOrWhiteSpace(StfsContentType);
        }
    }

    /// <summary>Whether the STFS title ID row can be shown.</summary>
    public bool HasStfsTitleId
    {
        get
        {
            return !string.IsNullOrWhiteSpace(StfsTitleId);
        }
    }

    /// <summary>Whether the STFS media ID row can be shown.</summary>
    public bool HasStfsMediaId
    {
        get
        {
            return !string.IsNullOrWhiteSpace(StfsMediaId);
        }
    }

    /// <summary>Whether the STFS content size row can be shown.</summary>
    public bool HasStfsContentSize
    {
        get
        {
            return !string.IsNullOrWhiteSpace(StfsContentSize);
        }
    }

    /// <summary>Whether any STFS summary rows can be shown.</summary>
    public bool HasAnyStfsSummary
    {
        get
        {
            return HasStfsDisplayName || HasStfsTitleName || HasStfsSignatureType || HasStfsContentType || HasStfsTitleId || HasStfsMediaId ||
                   HasStfsContentSize;
        }
    }

    /// <summary>Whether the STFS summary section can be shown.</summary>
    public bool IsStfsSummaryVisible
    {
        get
        {
            return IsStfsSelected && HasAnyStfsSummary;
        }
    }

    /// <summary>Container format short name (e.g., ISO, Loose).</summary>
    [ObservableProperty] private string _containerFormatName = string.Empty;

    /// <summary>Container format description.</summary>
    [ObservableProperty] private string _containerFormatDescription = string.Empty;

    /// <summary>Formatted summary of the container (file count + total size).</summary>
    [ObservableProperty] private string _containerSummary = string.Empty;

    /// <summary>Whether achievements are expanded in the bottom panel.</summary>
    [ObservableProperty] private bool _isAchievementsExpanded;

    /// <summary>Whether the achievements column is visible.</summary>
    public bool IsAchievementsColumnVisible
    {
        get
        {
            return HasAchievements && IsAchievementsExpanded;
        }
    }

    /// <summary>Column span for the details pane (1 when achievements shown, 2 when hidden).</summary>
    public int BottomDetailsColumnSpan
    {
        get
        {
            return IsAchievementsColumnVisible ? 1 : 2;
        }
    }

    public GameFilesDialogViewModel(string gamePath, string gameTitle)
    {
        _gamePath = gamePath;
        GameTitle = gameTitle;
        _messageBoxService = App.Services.GetRequiredService<IMessageBoxService>();
        SelectedTreeNodes.CollectionChanged += OnSelectedTreeNodesChanged;
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
        ContainerFormatName = source.FormatName;
        ContainerFormatDescription = source.FormatDescription;
        UpdateContainerSummary();
        BuildTree();
        return true;
    }

    private void UpdateContainerSummary()
    {
        if (_source == null)
        {
            ContainerSummary = string.Empty;
            return;
        }

        int fileCount = _source.Files.Count;
        ulong totalBytes = 0;
        foreach (GameFileNode file in _source.Files)
        {
            totalBytes += file.Size;
        }

        ContainerSummary = string.Format(LocalizationHelper.GetText("GameFilesDialog.Container.Summary"),
            fileCount, FileSizeFormatter.FormatBytes((long)totalBytes));
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

    partial void OnSelectedTreeNodeChanged(GameFileTreeNode? value)
    {
        // While multiple nodes are selected the details pane stays hidden.
        if (SelectedTreeNodes.Count > 1)
        {
            return;
        }

        SelectedEntry = value?.Entry;
    }

    private void OnSelectedTreeNodesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        OnPropertyChanged(nameof(HasExtractSelection));
        OnPropertyChanged(nameof(IsFileSelected));
        if (SelectedTreeNodes.Count == 1)
        {
            GameFileNode single = SelectedTreeNodes[0].Entry;
            if (!ReferenceEquals(SelectedEntry, single))
            {
                SelectedEntry = single;
            }
        }
        else if (SelectedEntry != null)
        {
            SelectedEntry = null;
        }
    }

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

    /// <summary>
    /// Extracts the selected files and folders (folders with their structure) to a user-chosen folder.
    /// </summary>
    [RelayCommand]
    private async Task ExtractSelection()
    {
        List<GameFileNode> files = [];
        IEnumerable<GameFileTreeNode> nodes = SelectedTreeNodes.Count > 0
            ? SelectedTreeNodes
            : SelectedTreeNode is { } single
                ? [single]
                : [];
        foreach (GameFileTreeNode node in nodes)
        {
            CollectFiles(node, files);
        }

        if (files.Count == 0)
        {
            await _messageBoxService.ShowInfoAsync(
                LocalizationHelper.GetText("GameFilesDialog.Extract.NoSelection.Title"),
                LocalizationHelper.GetText("GameFilesDialog.Extract.NoSelection.Message"),
                owner: OwnerWindow);
            return;
        }

        await PickAndExtractFilesAsync(files.DistinctBy(f => f.FullPath, StringComparer.OrdinalIgnoreCase).ToList());
    }

    /// <summary>
    /// Extracts the full container (with its structure) to a user-chosen folder.
    /// </summary>
    [RelayCommand]
    private async Task ExtractAll()
    {
        if (!HasContent || _source == null)
        {
            await _messageBoxService.ShowInfoAsync(
                LocalizationHelper.GetText("GameFilesDialog.Extract.NoSelection.Title"),
                LocalizationHelper.GetText("GameFilesDialog.Extract.NoSelection.Message"),
                owner: OwnerWindow);
            return;
        }

        List<GameFileNode> files = [];
        foreach (GameFileTreeNode root in RootNodes)
        {
            CollectFiles(root, files);
        }

        await PickAndExtractFilesAsync(files);
    }

    private static void CollectFiles(GameFileTreeNode node, List<GameFileNode> files)
    {
        if (node.Entry.IsFile)
        {
            files.Add(node.Entry);
            return;
        }

        foreach (GameFileTreeNode child in node.Children)
        {
            CollectFiles(child, files);
        }
    }

    private async Task PickAndExtractFilesAsync(IReadOnlyList<GameFileNode> files)
    {
        string? pickedDir = await PickOutputFolderAsync();
        if (pickedDir == null || _disposed)
        {
            return;
        }

        IGameFileSource? source = _source;
        if (source == null)
        {
            return;
        }

        string outputDir = Path.GetFullPath(pickedDir);
        int extracted;
        string? firstError;
        EventManager.Instance.DisableWindow();
        try
        {
            (extracted, firstError) = await Task.Run(() =>
            {
                int done = 0;
                string? error = null;
                foreach (GameFileNode file in files)
                {
                    try
                    {
                        byte[]? bytes = source.ReadFile(file.FullPath);
                        if (bytes == null)
                        {
                            throw new IOException($"File '{file.FullPath}' could not be read from the container.");
                        }

                        WriteExtractedFile(outputDir, file.FullPath, bytes);
                        done++;
                    }
                    catch (Exception ex)
                    {
                        Logger.Trace<GameFilesDialogViewModel>($"Failed to extract '{file.FullPath}': {ex.Message}");
                        error ??= ex.Message;
                    }
                }

                return (done, error);
            });
        }
        finally
        {
            EventManager.Instance.EnableWindow();
        }

        if (_disposed)
        {
            return;
        }

        if (firstError == null)
        {
            await _messageBoxService.ShowInfoAsync(
                LocalizationHelper.GetText("GameFilesDialog.Extract.Success.Title"),
                string.Format(LocalizationHelper.GetText("GameFilesDialog.Extract.BatchSuccess.Message"), extracted, files.Count, outputDir),
                owner: OwnerWindow);
        }
        else
        {
            await _messageBoxService.ShowErrorAsync(
                LocalizationHelper.GetText("GameFilesDialog.Extract.Failed.Title"),
                string.Format(LocalizationHelper.GetText("GameFilesDialog.Extract.BatchPartial.Message"), extracted, files.Count, firstError),
                owner: OwnerWindow);
        }
    }

    /// <summary>
    /// Asks the user for an extraction output folder.
    /// </summary>
    /// <returns>The chosen folder path, or null when cancelled or unavailable.</returns>
    private async Task<string?> PickOutputFolderAsync()
    {
        if ((OwnerWindow ?? App.MainWindow)?.StorageProvider is not { } storageProvider)
        {
            await _messageBoxService.ShowErrorAsync(
                LocalizationHelper.GetText("GameFilesDialog.MissingStorageProvider.Title"),
                LocalizationHelper.GetText("GameFilesDialog.MissingStorageProvider.Message"),
                owner: OwnerWindow);
            return null;
        }

        IReadOnlyList<IStorageFolder> folders = await storageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = LocalizationHelper.GetText("GameFilesDialog.Extract.FolderPicker.Title")
        });
        return folders.Count == 0 ? null : folders[0].Path.LocalPath;
    }

    /// <summary>
    /// Writes extracted bytes under the output directory, preserving container structure.
    /// </summary>
    /// <returns>The full output file path.</returns>
    private static string WriteExtractedFile(string outputDir, string fullPath, byte[] bytes)
    {
        string outputPath = ArchiveExtractor.GetSafeEntryOutputPath(outputDir,
            fullPath.Replace('/', Path.DirectorySeparatorChar));
        string? folder = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(folder))
        {
            Directory.CreateDirectory(folder);
        }

        File.WriteAllBytes(outputPath, bytes);
        return outputPath;
    }

    /// <summary>
    /// Opens the selected file in the matching viewer (text, nested container).
    /// Images and XEX files are already covered by the details pane.
    /// Double-taps on folders are handled by the tree itself (expand/collapse).
    /// </summary>
    [RelayCommand]
    private async Task OpenSelectedEntry()
    {
        if (_disposed || _source == null || SelectedEntry is not { IsFile: true } file)
        {
            return;
        }

        string extension = Path.GetExtension(file.Name).ToLowerInvariant();
        if (TextPreviewExtensions.Contains(extension))
        {
            await OpenTextFileAsync(file);
            return;
        }

        if (PreviewableExtensions.Contains(extension) || extension.Equals(".xex", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        await OpenNestedContainerAsync(file);
    }

    private async Task OpenTextFileAsync(GameFileNode file)
    {
        IGameFileSource? source = _source;
        if (source == null)
        {
            return;
        }

        byte[]? bytes = await Task.Run(() => source.ReadFile(file.FullPath));
        if (_disposed || bytes == null)
        {
            return;
        }

        if (bytes.Length > MaxTextPreviewBytes)
        {
            await _messageBoxService.ShowInfoAsync(
                LocalizationHelper.GetText("GameFilesDialog.OpenPreview.TooLarge.Title"),
                LocalizationHelper.GetText("GameFilesDialog.OpenPreview.TooLarge.Message"),
                owner: OwnerWindow);
            return;
        }

        string text;
        string encodingName;
        using (MemoryStream stream = new MemoryStream(bytes, false))
        {
            using StreamReader reader = new StreamReader(stream, System.Text.Encoding.UTF8, true);
            text = await reader.ReadToEndAsync();
            encodingName = reader.CurrentEncoding.WebName;
        }

        if (_disposed)
        {
            return;
        }

        await TextFileDialog.ShowAsync(file.Name, text, encodingName);
    }

    private async Task OpenNestedContainerAsync(GameFileNode file)
    {
        IGameFileSource? source = _source;
        if (source == null)
        {
            return;
        }

        string? tempPath = null;
        try
        {
            tempPath = await Task.Run(() =>
            {
                byte[]? data = source.ReadFile(file.FullPath);
                if (data == null || !HasStfsMagic(data))
                {
                    return null;
                }

                string directory = Path.Combine(Path.GetTempPath(), "XeniaManager", "GameFiles", Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(directory);
                string path = Path.Combine(directory, $"nested-{Guid.NewGuid():N}{Path.GetExtension(file.Name)}");
                File.WriteAllBytes(path, data);
                return path;
            });
        }
        catch (Exception ex)
        {
            Logger.Error<GameFilesDialogViewModel>($"Failed to open nested container '{file.FullPath}'");
            Logger.LogExceptionDetails<GameFilesDialogViewModel>(ex);
            await _messageBoxService.ShowErrorAsync(
                LocalizationHelper.GetText("GameFilesDialog.OpenPreview.Failed.Title"),
                string.Format(LocalizationHelper.GetText("GameFilesDialog.OpenPreview.Failed.Message"), file.Name, ex.Message),
                owner: OwnerWindow);
            return;
        }

        if (_disposed || tempPath == null)
        {
            DeleteTempFile(tempPath);
            if (!_disposed && tempPath == null)
            {
                await _messageBoxService.ShowInfoAsync(
                    LocalizationHelper.GetText("GameFilesDialog.OpenPreview.Unsupported.Title"),
                    LocalizationHelper.GetText("GameFilesDialog.OpenPreview.Unsupported.Message"),
                    owner: OwnerWindow);
            }

            return;
        }

        try
        {
            await GameFilesDialog.ShowAsync(tempPath, file.Name);
        }
        finally
        {
            DeleteTempFile(tempPath);
        }
    }

    private static void DeleteTempFile(string? tempPath)
    {
        if (string.IsNullOrEmpty(tempPath))
        {
            return;
        }

        try
        {
            File.Delete(tempPath);
            string? directory = Path.GetDirectoryName(tempPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.Delete(directory);
            }
        }
        catch (Exception ex)
        {
            Logger.Trace<GameFilesDialogViewModel>($"Failed to clean up nested container temp file '{tempPath}': {ex.Message}");
        }
    }

    /// <summary>
    /// Checks the STFS package magic (CON, PIRS or LIVE) at the start of the data.
    /// </summary>
    private static bool HasStfsMagic(byte[] bytes) => bytes.Length >= 4 && System.Text.Encoding.ASCII.GetString(bytes, 0, 4) is "CON " or "PIRS" or "LIVE";

    partial void OnSearchTextChanged(string value)
    {
        // Debounce: each keystroke runs a full tree walk + TreeView layout on the
        // UI thread, so only filter once typing settles (same pattern as LibraryPageViewModel).
        _searchCts?.Cancel();
        _searchCts?.Dispose();
        _searchCts = new CancellationTokenSource();
        CancellationToken token = _searchCts.Token;

        Task.Delay(SearchDebounceMs, token).ContinueWith(_ =>
        {
            if (!token.IsCancellationRequested && !_disposed && value == SearchText)
            {
                ApplyTreeFilter(value);
            }
        }, TaskScheduler.FromCurrentSynchronizationContext());
    }

    private void ApplyTreeFilter(string? query)
    {
        SelectedTreeNode = null;
        SelectedEntry = null;

        if (!HasContent)
        {
            IsSearchEmptyVisible = false;
            return;
        }

        if (string.IsNullOrWhiteSpace(query))
        {
            foreach (GameFileTreeNode root in RootNodes)
            {
                SetVisibleRecursive(root, true, false);
            }

            IsSearchEmptyVisible = false;
            return;
        }

        string trimmed = query.Trim();
        bool anyVisible = false;
        foreach (GameFileTreeNode root in RootNodes)
        {
            if (UpdateVisibility(root, trimmed))
            {
                anyVisible = true;
            }
        }

        IsSearchEmptyVisible = !anyVisible;
    }

    private void SetVisibleRecursive(GameFileTreeNode node, bool visible, bool expanded)
    {
        node.IsVisible = visible;
        node.IsExpanded = expanded && node.Children.Count > 0;
        foreach (GameFileTreeNode child in node.Children)
        {
            SetVisibleRecursive(child, visible, false);
        }
    }

    private bool UpdateVisibility(GameFileTreeNode node, string query)
    {
        bool selfMatch = node.Entry.Name.Contains(query, StringComparison.OrdinalIgnoreCase);
        bool childVisible = false;
        foreach (GameFileTreeNode child in node.Children)
        {
            if (UpdateVisibility(child, query))
            {
                childVisible = true;
            }
        }

        bool visible = selfMatch || childVisible;
        node.IsVisible = visible;
        node.IsExpanded = childVisible;
        return visible;
    }

    partial void OnSelectedEntryChanged(GameFileNode? value)
    {
        OnPropertyChanged(nameof(HasExtractSelection));
        OnPropertyChanged(nameof(IsFileSelected));
        _ = LoadDetailsAsync(value, ++_detailsLoadId);
    }

    partial void OnIsAchievementsExpandedChanged(bool value)
    {
        OnPropertyChanged(nameof(IsAchievementsColumnVisible));
        OnPropertyChanged(nameof(BottomDetailsColumnSpan));
    }

    partial void OnHasAchievementsChanged(bool value)
    {
        OnPropertyChanged(nameof(IsAchievementsColumnVisible));
        OnPropertyChanged(nameof(BottomDetailsColumnSpan));
    }

    partial void OnIsXexSelectedChanged(bool value) => OnPropertyChanged(nameof(IsSpaSummaryVisible));

    partial void OnIsStfsSelectedChanged(bool value) => OnPropertyChanged(nameof(IsStfsSummaryVisible));

    partial void OnStfsDisplayNameChanged(string value)
    {
        OnPropertyChanged(nameof(HasStfsDisplayName));
        OnPropertyChanged(nameof(HasAnyStfsSummary));
        OnPropertyChanged(nameof(IsStfsSummaryVisible));
    }

    partial void OnStfsTitleNameChanged(string value)
    {
        OnPropertyChanged(nameof(HasStfsTitleName));
        OnPropertyChanged(nameof(HasAnyStfsSummary));
        OnPropertyChanged(nameof(IsStfsSummaryVisible));
    }

    partial void OnStfsSignatureTypeChanged(string value)
    {
        OnPropertyChanged(nameof(HasStfsSignatureType));
        OnPropertyChanged(nameof(HasAnyStfsSummary));
        OnPropertyChanged(nameof(IsStfsSummaryVisible));
    }

    partial void OnStfsContentTypeChanged(string value)
    {
        OnPropertyChanged(nameof(HasStfsContentType));
        OnPropertyChanged(nameof(HasAnyStfsSummary));
        OnPropertyChanged(nameof(IsStfsSummaryVisible));
    }

    partial void OnStfsTitleIdChanged(string value)
    {
        OnPropertyChanged(nameof(HasStfsTitleId));
        OnPropertyChanged(nameof(HasAnyStfsSummary));
        OnPropertyChanged(nameof(IsStfsSummaryVisible));
    }

    partial void OnStfsMediaIdChanged(string value)
    {
        OnPropertyChanged(nameof(HasStfsMediaId));
        OnPropertyChanged(nameof(HasAnyStfsSummary));
        OnPropertyChanged(nameof(IsStfsSummaryVisible));
    }

    partial void OnStfsContentSizeChanged(string value)
    {
        OnPropertyChanged(nameof(HasStfsContentSize));
        OnPropertyChanged(nameof(HasAnyStfsSummary));
        OnPropertyChanged(nameof(IsStfsSummaryVisible));
    }

    partial void OnSpaTitleChanged(string value)
    {
        OnPropertyChanged(nameof(HasSpaTitle));
        OnPropertyChanged(nameof(HasAnySpaSummary));
        OnPropertyChanged(nameof(IsSpaSummaryVisible));
    }

    partial void OnSpaAchievementsChanged(string value)
    {
        OnPropertyChanged(nameof(HasSpaAchievementsText));
        OnPropertyChanged(nameof(HasAnySpaSummary));
        OnPropertyChanged(nameof(IsSpaSummaryVisible));
    }

    partial void OnSpaGamerscoreChanged(string value)
    {
        OnPropertyChanged(nameof(HasSpaGamerscoreText));
        OnPropertyChanged(nameof(HasAnySpaSummary));
        OnPropertyChanged(nameof(IsSpaSummaryVisible));
    }

    /// <summary>
    /// Releases the current detail images (icon, preview, achievement icons).
    /// </summary>
    private void ClearDetailImages()
    {
        SelectedIcon?.Dispose();
        SelectedIcon = null;
        PreviewImage?.Dispose();
        PreviewImage = null;
        foreach (SpaAchievementRow row in SpaAchievementRows)
        {
            row.Image?.Dispose();
        }
    }

    private async Task LoadDetailsAsync(GameFileNode? node, int loadId)
    {
        if (_disposed)
        {
            return;
        }

        ClearDetailImages();
        HasSelection = node != null;
        IsXexSelected = false;
        IsStfsSelected = false;
        HasSelectedIcon = false;
        HasPreviewImage = false;
        HasAchievements = false;
        SpaAchievementRows = [];
        XexTitleId = string.Empty;
        XexMediaId = string.Empty;
        XexVersion = string.Empty;
        XexDisc = string.Empty;
        XexImageSize = string.Empty;
        XexModuleFlags = string.Empty;
        SpaTitle = string.Empty;
        SpaAchievements = string.Empty;
        SpaGamerscore = string.Empty;
        StfsSignatureType = string.Empty;
        StfsContentType = string.Empty;
        StfsTitleId = string.Empty;
        StfsMediaId = string.Empty;
        StfsDisplayName = string.Empty;
        StfsTitleName = string.Empty;
        StfsContentSize = string.Empty;
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
            if (_disposed || loadId != _detailsLoadId)
            {
                return;
            }

            if (imageBytes != null)
            {
                Bitmap? preview = null;
                try
                {
                    using MemoryStream imageStream = new MemoryStream(imageBytes);
                    preview = new Bitmap(imageStream);
                }
                catch (Exception ex)
                {
                    Logger.Trace<GameFilesDialogViewModel>($"Failed to decode image preview for '{node.FullPath}': {ex.Message}");
                }

                if (_disposed || loadId != _detailsLoadId)
                {
                    preview?.Dispose();
                    return;
                }

                if (preview != null)
                {
                    PreviewImage = preview;
                    HasPreviewImage = true;
                }
            }
        }

        if (node.IsFile && node.Name.EndsWith(".xex", StringComparison.OrdinalIgnoreCase))
        {
            XexDetails? details = await Task.Run(() => TryParseXex(node.FullPath));
            if (_disposed || loadId != _detailsLoadId || details == null)
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
            IsAchievementsExpanded = false;
            OnPropertyChanged(nameof(IsAchievementsColumnVisible));
            OnPropertyChanged(nameof(BottomDetailsColumnSpan));
            if (details.Icon != null)
            {
                Bitmap? icon = null;
                try
                {
                    using MemoryStream stream = new MemoryStream(details.Icon);
                    icon = new Bitmap(stream);
                }
                catch (Exception ex)
                {
                    Logger.Trace<GameFilesDialogViewModel>($"Failed to decode XEX icon for '{node.FullPath}': {ex.Message}");
                }

                if (_disposed || loadId != _detailsLoadId)
                {
                    icon?.Dispose();
                    foreach (SpaAchievementRow row in rows)
                    {
                        row.Image?.Dispose();
                    }

                    return;
                }

                if (icon != null)
                {
                    SelectedIcon = icon;
                    HasSelectedIcon = true;
                }
            }
            else if (_disposed || loadId != _detailsLoadId)
            {
                foreach (SpaAchievementRow row in rows)
                {
                    row.Image?.Dispose();
                }

                return;
            }

            IsXexSelected = true;
            return;
        }

        // Try STFS package details for any other file (magic is checked inside).
        StfsDetails? stfsDetails = await Task.Run(() => TryParseStfs(node.FullPath));
        if (_disposed || loadId != _detailsLoadId || stfsDetails == null)
        {
            return;
        }

        StfsSignatureType = stfsDetails.SignatureType;
        StfsContentType = stfsDetails.ContentType;
        StfsTitleId = stfsDetails.TitleId;
        StfsMediaId = stfsDetails.MediaId;
        StfsDisplayName = stfsDetails.DisplayName;
        StfsTitleName = stfsDetails.TitleName;
        StfsContentSize = stfsDetails.ContentSize;
        if (stfsDetails.Thumbnail != null)
        {
            Bitmap? thumbnail = null;
            try
            {
                using MemoryStream stream = new MemoryStream(stfsDetails.Thumbnail);
                thumbnail = new Bitmap(stream);
            }
            catch (Exception ex)
            {
                Logger.Trace<GameFilesDialogViewModel>($"Failed to decode STFS thumbnail for '{node.FullPath}': {ex.Message}");
            }

            if (_disposed || loadId != _detailsLoadId)
            {
                thumbnail?.Dispose();
                return;
            }

            if (thumbnail != null)
            {
                SelectedIcon = thumbnail;
                HasSelectedIcon = true;
            }
        }

        if (_disposed || loadId != _detailsLoadId)
        {
            return;
        }

        IsStfsSelected = true;
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

    private StfsDetails? TryParseStfs(string fullPath)
    {
        try
        {
            byte[]? bytes = _source?.ReadFile(fullPath);
            if (bytes == null || bytes.Length < 4)
            {
                return null;
            }

            if (!HasStfsMagic(bytes))
            {
                return null;
            }

            // FromBytes validates size/header; catch invalid packages.
            StfsFile stfs;
            try
            {
                stfs = StfsFile.FromBytes(bytes);
            }
            catch
            {
                return null;
            }

            using (stfs)
            {
                // StfsFile keeps raw data; ensure we at least have metadata parsed.
                // FromBytes may succeed even for non-STFS; check signature round-trip.
                string sig = stfs.SignatureType.ToString();
                string contentType = stfs.Metadata.ContentType.ToString();
                string titleId = stfs.Metadata.TitleIdHex;
                string mediaId = stfs.Metadata.MediaIdHex;
                string displayName = stfs.Metadata.DisplayName?.Trim() ?? string.Empty;
                string titleName = stfs.Metadata.TitleName?.Trim() ?? string.Empty;
                string contentSize = FileSizeFormatter.FormatBytes(stfs.Metadata.ContentSize);
                byte[]? thumb = stfs.Metadata.ThumbnailImage is { Length: > 0 } t && stfs.TryGetIcon() != null
                    ? stfs.Metadata.ThumbnailImage
                    : stfs.Metadata.ThumbnailImage is { Length: > 0 } t2
                        ? t2
                        : null;
                // Prefer validated icon bytes if thumbnail looks like image.
                byte[]? icon = stfs.TryGetIcon();

                // If all key display fields are empty and no icon, treat as not an STFS worth showing.
                if (string.IsNullOrWhiteSpace(displayName) && string.IsNullOrWhiteSpace(titleName)
                                                           && titleId == "00000000" && mediaId == "00000000" && icon == null)
                {
                    return null;
                }

                return new StfsDetails(sig, contentType, titleId, mediaId, displayName, titleName, contentSize, icon ?? thumb);
            }
        }
        catch (Exception ex)
        {
            Logger.Trace<GameFilesDialogViewModel>($"Failed to parse STFS '{fullPath}': {ex.Message}");
            return null;
        }
    }

    private sealed record StfsDetails(
        string SignatureType,
        string ContentType,
        string TitleId,
        string MediaId,
        string DisplayName,
        string TitleName,
        string ContentSize,
        byte[]? Thumbnail);

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
        _detailsLoadId++; // Invalidate in-flight LoadDetailsAsync continuations.
        OwnerProvider = null;
        _searchCts?.Cancel();
        _searchCts?.Dispose();
        _searchCts = null;
        ClearDetailImages();
        SelectedTreeNode = null;
        SelectedEntry = null;
        SelectedTreeNodes.Clear();
        SpaAchievementRows.Clear();
        RootNodes.Clear();
        _nodeLookup.Clear();
        _source?.Dispose();
        _source = null;
        GC.SuppressFinalize(this);
    }
}