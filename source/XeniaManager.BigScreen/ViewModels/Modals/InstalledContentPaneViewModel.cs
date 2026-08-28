using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using XeniaManager.BigScreen.Factories;
using XeniaManager.BigScreen.Models;
using XeniaManager.BigScreen.Services;
using XeniaManager.BigScreen.Utilities;
using XeniaManager.BigScreen.ViewModels.Items;
using XeniaManager.Files;
using XeniaManager.Logging;
using XeniaManager.Files.Models.Stfs;
using XeniaManager.Core.Models.Game;
using XeniaManager.Core.Models.Items;
using XeniaManager.Core.Utilities;

namespace XeniaManager.BigScreen.ViewModels.Modals;

/// <summary>
/// The game modal's installed content pane: the game's title updates or
/// marketplace content (X switches between them), each row deletable.
/// </summary>
public partial class InstalledContentPaneViewModel : ViewModelBase, IGameModalPane
{
    private readonly Game _game;
    private readonly IModalService _modalService;

    /// <summary>
    /// Whether the pane shows the empty state (nothing installed of this type).
    /// </summary>
    public bool ShowEmpty => Rows.Count == 0;

    /// <summary>
    /// The content rows for the pane's content type.
    /// </summary>
    public ObservableCollection<ContentItemViewModel> Rows { get; } = [];

    /// <summary>
    /// The content type shown by this pane (installer / title updates or
    /// marketplace content), fixed by the menu entry that opened it.
    /// </summary>
    public ContentType ContentType { get; }

    /// <summary>
    /// The content type's display name.
    /// </summary>
    public string TypeText => ContentType == ContentType.MarketplaceContent
        ? LocalizationHelper.GetText("GameModal.Content.MarketplaceContent")
        : LocalizationHelper.GetText("GameModal.Content.TitleUpdates");

    /// <summary>
    /// The content count shown in the pane header.
    /// </summary>
    public string CountText => string.Format(LocalizationHelper.GetText("GameModal.Content.Count"), Rows.Count);

    /// <summary>
    /// Re-scans the game's content headers of the current type (from the boot
    /// preload cache) and rebuilds the rows.
    /// </summary>
    private void Reload()
    {
        GameContent content = GameDataCache.GetContent(_game);
        List<HeaderFile> headers = ContentType == ContentType.MarketplaceContent
            ? content.MarketplaceContentHeaderFiles
            : content.InstallerHeaderFiles;

        Rows.Clear();
        foreach (HeaderFile header in headers)
        {
            Rows.Add(new ContentItemViewModel(header));
        }

        Logger.Debug<InstalledContentPaneViewModel>($"Loaded {Rows.Count} {ContentType} items for '{_game.Title}'");
    }

    /// <summary>
    /// Deletes the content's package file or directory, plus its header file.
    /// </summary>
    private static void DeleteItemFromDisk(ContentItemViewModel item)
    {
        if (!string.IsNullOrEmpty(item.FilePath))
        {
            if (File.Exists(item.FilePath))
            {
                File.Delete(item.FilePath);
            }
            else if (Directory.Exists(item.FilePath))
            {
                Directory.Delete(item.FilePath, true);
            }
        }

        if (File.Exists(item.HeaderFilePath))
        {
            File.Delete(item.HeaderFilePath);
        }
    }

    /// <summary>
    /// Confirms and deletes the selected content (package file/directory plus
    /// its header file), then removes the row.
    /// </summary>
    private async Task DeleteSelectedAsync()
    {
        ContentItemViewModel? item = Rows.FirstOrDefault(r => r.IsSelected);
        if (item == null)
        {
            return;
        }

        bool confirmed = await ModalFactory.ConfirmAsync(_modalService,
            LocalizationHelper.GetText("GameModal.Content.Delete.Confirmation.Title"),
            string.Format(LocalizationHelper.GetText("GameModal.Content.Delete.Confirmation.Message"),
                item.DisplayName),
            LocalizationHelper.GetText("GameModal.Content.Delete.Confirm"),
            LocalizationHelper.GetText("Modal.Cancel")) == true;
        if (!confirmed)
        {
            return;
        }

        try
        {
            DeleteItemFromDisk(item);

            Rows.Remove(item);
            GameDataCache.RefreshContent(_game);
            Logger.Info<InstalledContentPaneViewModel>($"Deleted '{item.DisplayName}'");
        }
        catch (Exception ex)
        {
            Logger.Error<InstalledContentPaneViewModel>($"Failed to delete '{item.DisplayName}'");
            Logger.LogExceptionDetails<InstalledContentPaneViewModel>(ex);
        }
    }

    /// <summary>
    /// Handles pane input: Up/Down moves the rows and A deletes the selected
    /// row (with confirmation).
    /// </summary>
    public bool HandleInput(NavigationCommand command)
    {
        switch (command)
        {
            case NavigationCommand.MoveUp:
                SelectionHelper.MoveSelection(Rows, -1);
                return true;
            case NavigationCommand.MoveDown:
                SelectionHelper.MoveSelection(Rows, 1);
                return true;
            case NavigationCommand.Activate:
                if (Rows.Count == 0)
                {
                    return true;
                }

                TaskUtilities.RunSafely<InstalledContentPaneViewModel>(
                    DeleteSelectedAsync, "Deleting selected content");
                return true;
            default:
                return false;
        }
    }

    /// <summary>
    /// Selects the first content row when the pane becomes active.
    /// </summary>
    public void OnPaneEntered()
    {
        SelectionHelper.SelectOnlyAt(Rows, 0);
    }

    /// <summary>
    /// Clears the content selection when the pane loses focus.
    /// </summary>
    public void OnPaneExited()
    {
        SelectionHelper.ClearSelection(Rows);
    }

    /// <summary>
    /// Loads the game's content of the given type.
    /// </summary>
    public InstalledContentPaneViewModel(Game game, ContentType contentType)
    {
        _game = game;
        _modalService = App.Services.GetRequiredService<IModalService>();
        Rows.CollectionChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(CountText));
            OnPropertyChanged(nameof(ShowEmpty));
        };
        ContentType = contentType;
        Reload();
    }
}