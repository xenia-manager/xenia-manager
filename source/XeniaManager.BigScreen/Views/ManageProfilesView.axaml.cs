using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Avalonia.VisualTree;
using XeniaManager.BigScreen.Utilities;
using XeniaManager.BigScreen.ViewModels;
using XeniaManager.BigScreen.ViewModels.Items;
using XeniaManager.Core.Utilities;

namespace XeniaManager.BigScreen.Views;

/// <summary>
/// Full-screen profile management overlay: create, delete, import, export and
/// edit Canary profiles. File import/export runs through the window's storage
/// provider; destructive actions confirm via the modal system.
/// </summary>
public partial class ManageProfilesView : UserControl
{
    public ManageProfilesView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        DetachedFromVisualTree += OnDetachedFromVisualTree;

        // A mouse click on a row loads it into the edit panel; the create stub
        // creates a new profile
        RowsList.AddHandler(Button.ClickEvent, OnProfileRowClick, RoutingStrategies.Bubble);
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
        }
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
        double bottom = top + target.Bounds.Height;
        double viewport = SvList.Viewport.Height;
        double offset = SvList.Offset.Y;

        if (top < 0)
        {
            SvList.Offset = SvList.Offset.WithY(Math.Max(0, offset + top));
        }
        else if (bottom > viewport)
        {
            SvList.Offset = SvList.Offset.WithY(Math.Max(0, offset + bottom - viewport));
        }
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
                    Patterns = ["*.xaccount", "*.zip"],
                },
            ],
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
                    Patterns = ["*.xaccount"],
                },
            ],
            SuggestedFileName = suggestedName,
            DefaultExtension = "xaccount",
            ShowOverwritePrompt = true,
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
}