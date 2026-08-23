using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Avalonia.VisualTree;
using XeniaManager.BigScreen.Models;
using XeniaManager.BigScreen.Utilities;
using XeniaManager.BigScreen.ViewModels.Modals;
using XeniaManager.BigScreen.ViewModels.Items;
using XeniaManager.Core.Utilities;

namespace XeniaManager.BigScreen.Views.Modals;

/// <summary>
/// Full-screen profile management overlay: create, delete, import, export and
/// edit Canary profiles. File import/export runs through the window's storage
/// provider; destructive actions confirm via the modal system.
/// </summary>
public partial class ManageProfilesView : UserControl
{
    /// <summary>
    /// Scrolls the scroll viewer so the given control is fully visible.
    /// </summary>
    private void ScrollIntoView(Control target)
    {
        Point? position = target.TranslatePoint(new Point(0, 0), SvList);
        if (position == null)
        {
            return;
        }

        double top = position.Value.Y;
        double offset = ScrollViewerHelper.OffsetForElement(
            top, target.Bounds.Height, SvList.Viewport.Height, SvList.Offset.Y);
        SvList.Offset = SvList.Offset.WithY(offset);
    }

    /// <summary>
    /// Opens the native dropdown matching the panel editor the controller just
    /// opened.
    /// </summary>
    private void OnPanelEditorOpened(ManageProfilesRowKind kind)
    {
        switch (kind)
        {
            case ManageProfilesRowKind.Country:
                CmbCountry.IsDropDownOpen = true;
                CmbCountry.Focus();
                break;
            case ManageProfilesRowKind.Language:
                CmbLanguage.IsDropDownOpen = true;
                CmbLanguage.Focus();
                break;
            case ManageProfilesRowKind.SubscriptionTier:
                CmbSubscriptionTier.IsDropDownOpen = true;
                CmbSubscriptionTier.Focus();
                break;
        }
    }

    /// <summary>
    /// Closes every panel dropdown (editor commit or cancel).
    /// </summary>
    private void OnPanelEditorClosed()
    {
        CmbCountry.IsDropDownOpen = false;
        CmbLanguage.IsDropDownOpen = false;
        CmbSubscriptionTier.IsDropDownOpen = false;
    }

    /// <summary>
    /// Focuses the gamertag field so keyboard entry can start immediately
    /// (controller activation of the gamertag row).
    /// </summary>
    private void OnGamertagFocusRequested()
    {
        TxtGamertag.Focus();
    }

    /// <summary>
    /// Scrolls the selected row into view (posted so layout has settled) - the
    /// create stub sits below the fold with many profiles.
    /// </summary>
    private void OnScrollRequested()
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (DataContext is not ManageProfilesViewModel vm)
            {
                return;
            }

            Utilities.ISelectable? selected = vm.Rows.FirstOrDefault(i => i.IsSelected);
            if (selected == null)
            {
                return;
            }

            foreach (Button button in RowsList.GetVisualDescendants().OfType<Button>())
            {
                if (ReferenceEquals(button.DataContext, selected))
                {
                    ScrollIntoView(button);
                    return;
                }
            }
        });
    }

    private void OnProfileRowClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not ManageProfilesViewModel vm ||
            e.Source is not Button { DataContext: ProfileItemViewModel item })
        {
            return;
        }

        vm.SelectRow(item);
    }

    /// <summary>
    /// Switches the managed version when its chip is clicked.
    /// </summary>
    private void OnVersionChipClick(object? sender, RoutedEventArgs e)
    {
        if (e.Source is not Button { DataContext: VersionChipViewModel chip } ||
            DataContext is not ManageProfilesViewModel vm)
        {
            return;
        }

        vm.SelectVersion(chip);
    }

    /// <summary>
    /// Creates a new profile when the anchored create row is clicked.
    /// </summary>
    private void OnCreateRowClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is ManageProfilesViewModel vm)
        {
            _ = vm.CreateWithConfirmAsync();
        }
    }

    /// <summary>
    /// Runs the import file picker and hands the chosen path to the view model.
    /// </summary>
    private async void OnImportRequested()
    {
        if (DataContext is not ManageProfilesViewModel vm)
        {
            return;
        }

        TopLevel? topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null)
        {
            return;
        }

        FilePickerOpenOptions options = new()
        {
            Title = LocalizationHelper.GetText("ManageProfiles.Import.FilePicker.Title"),
            AllowMultiple = false,
            FileTypeFilter =
            [
                new FilePickerFileType("Xenia Account File")
                {
                    Patterns = ["*.xaccount", "*.zip"]
                }
            ]
        };

        IReadOnlyList<IStorageFile> files = await topLevel.StorageProvider.OpenFilePickerAsync(options);
        if (files.Count > 0)
        {
            await vm.ImportFromAsync(files[0].Path.LocalPath);
        }
    }

    /// <summary>
    /// Runs the export file picker and hands the chosen path to the view model.
    /// </summary>
    private async void OnExportRequested()
    {
        if (DataContext is not ManageProfilesViewModel { SelectedProfile: not null } vm)
        {
            return;
        }

        TopLevel? topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null)
        {
            return;
        }

        string suggestedName =
            $"{vm.SelectedProfile.Gamertag} - {vm.SelectedProfile.PathXuidText()}.xaccount";
        IStorageFile? file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = LocalizationHelper.GetText("ManageProfiles.Export.FilePicker.Title"),
            FileTypeChoices =
            [
                new FilePickerFileType("Xenia Account File")
                {
                    Patterns = ["*.xaccount"]
                }
            ],
            SuggestedFileName = suggestedName,
            DefaultExtension = "xaccount",
            ShowOverwritePrompt = true
        });

        if (file != null)
        {
            await vm.ExportSelectedAsync(file.Path.LocalPath);
        }
    }

    private void OnSaveClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is ManageProfilesViewModel vm)
        {
            vm.Save();
        }
    }

    /// <summary>
    /// Moves focus to the gamertag field so keyboard edits are immediately possible,
    /// and wires the View/Start import-export requests to the file pickers.
    /// </summary>
    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        if (DataContext is ManageProfilesViewModel vm)
        {
            vm.ImportRequested += OnImportRequested;
            vm.ExportRequested += OnExportRequested;
            vm.ScrollRequested += OnScrollRequested;
            vm.PanelEditorOpened += OnPanelEditorOpened;
            vm.PanelEditorClosed += OnPanelEditorClosed;
            vm.GamertagFocusRequested += OnGamertagFocusRequested;
        }

        Dispatcher.UIThread.Post(() =>
        {
            if (DataContext is ManageProfilesViewModel { HasProfiles: true })
            {
                TxtGamertag.Focus();
                TxtGamertag.SelectAll();
            }
        });
    }

    /// <summary>
    /// Unsubscribes the view event handlers when the modal closes.
    /// </summary>
    private void OnDetachedFromVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
    {
        if (DataContext is ManageProfilesViewModel vm)
        {
            vm.ImportRequested -= OnImportRequested;
            vm.ExportRequested -= OnExportRequested;
            vm.ScrollRequested -= OnScrollRequested;
            vm.PanelEditorOpened -= OnPanelEditorOpened;
            vm.PanelEditorClosed -= OnPanelEditorClosed;
            vm.GamertagFocusRequested -= OnGamertagFocusRequested;
        }
    }

    public ManageProfilesView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        DetachedFromVisualTree += OnDetachedFromVisualTree;

        RowsList.AddHandler(Button.ClickEvent, OnProfileRowClick, RoutingStrategies.Bubble);
        VersionChipsList.AddHandler(Button.ClickEvent, OnVersionChipClick, RoutingStrategies.Bubble);
    }
}