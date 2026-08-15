using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.VisualTree;
using XeniaManager.BigScreen.Controls.Settings;
using XeniaManager.BigScreen.Models;
using XeniaManager.BigScreen.Utilities;
using XeniaManager.BigScreen.ViewModels.Items;
using XeniaManager.BigScreen.ViewModels.Screens;
using XeniaManager.Core.Utilities;
using ISelectable = XeniaManager.BigScreen.Utilities.ISelectable;

namespace XeniaManager.BigScreen.Views.Screens;

/// <summary>
/// Full-screen settings screen: dashboard appearance and quit behaviour.
/// Controller row navigation (selection state) lives in the view model; this
/// view scrolls the selected row into view and opens/closes the native editor
/// controls on demand.
/// </summary>
public partial class SettingsView : UserControl
{
    /// <summary>
    /// Maps the fixed row view models to their cards, so a selection change
    /// can bring the matching card into view.
    /// </summary>
    private readonly Dictionary<ISelectable, Control> _rowCards = [];

    public SettingsView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        DataContextChanged += OnDataContextChanged;

        // Assign the appropriate palette to each colour field
        PrimaryColorField.Palette = ColorPickerField.BackgroundPalette;
        AccentColorField.Palette = ColorPickerField.AccentPalette;
    }

    /// <summary>
    /// Focuses the first interactive setting (called when the overlay opens).
    /// </summary>
    public void FocusFirst()
    {
        BackgroundModeCombo.Focus();
    }

    private void OnLoaded(object? sender, EventArgs e)
    {
        FocusFirst();
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is SettingsViewModel vm)
        {
            vm.RowSelectionChanged += OnRowSelectionChanged;
            vm.EditorOpened += OnEditorOpened;
            vm.EditorClosed += OnEditorClosed;
            vm.SelectImageRequested += OnSelectImageRequested;
            BuildRowMap(vm);
        }
    }

    /// <summary>
    /// Maps the fixed row view models to their card controls.
    /// </summary>
    private void BuildRowMap(SettingsViewModel vm)
    {
        _rowCards[vm.RowManageProfiles] = CardManageProfiles;
        _rowCards[vm.RowLibraryView] = CardLibraryView;
        _rowCards[vm.RowCardImage] = CardCardImage;
        _rowCards[vm.RowTimeFormat] = CardTimeFormat;
        _rowCards[vm.RowQuitToggle] = CardQuitToggle;
        _rowCards[vm.RowBackgroundMode] = CardBackgroundMode;
        _rowCards[vm.RowPrimaryColour] = CardPrimaryColour;
        _rowCards[vm.RowAccentColour] = CardAccentColour;
        _rowCards[vm.RowVignette] = CardVignette;
        _rowCards[vm.RowBackgroundImage] = CardBackgroundImage;
        _rowCards[vm.RowXConfig] = CardXConfig;
    }

    /// <summary>
    /// Scrolls the newly selected row into view (gamepad rows are templated,
    /// so their cards are looked up in the visual tree).
    /// </summary>
    private void OnRowSelectionChanged(ISelectable row)
    {
        Control? card = row is GamepadItemViewModel
            ? SvSettings.GetVisualDescendants().OfType<GamepadCard>()
                .FirstOrDefault(c => ReferenceEquals(c.DataContext, row))
            : _rowCards.GetValueOrDefault(row);
        card?.BringIntoView();
    }

    /// <summary>
    /// Opens the native control matching the editor the controller just opened
    /// (dropdowns, palette popups).
    /// </summary>
    private void OnEditorOpened(SettingsRowKind kind)
    {
        switch (kind)
        {
            case SettingsRowKind.LibraryView:
                LibraryViewModeCombo.IsDropDownOpen = true;
                LibraryViewModeCombo.Focus();
                break;
            case SettingsRowKind.CardImage:
                CardImageModeCombo.IsDropDownOpen = true;
                CardImageModeCombo.Focus();
                break;
            case SettingsRowKind.TimeFormat:
                TimeFormatCombo.IsDropDownOpen = true;
                TimeFormatCombo.Focus();
                break;
            case SettingsRowKind.BackgroundMode:
                BackgroundModeCombo.IsDropDownOpen = true;
                BackgroundModeCombo.Focus();
                break;
            case SettingsRowKind.PrimaryColour:
                PrimaryColorField.OpenPalette();
                break;
            case SettingsRowKind.AccentColour:
                AccentColorField.OpenPalette();
                break;
            case SettingsRowKind.XConfig:
                XConfigResolutionCombo.IsDropDownOpen = true;
                XConfigResolutionCombo.Focus();
                break;
        }
    }

    /// <summary>
    /// Closes every native editor control (commit or cancel).
    /// </summary>
    private void OnEditorClosed()
    {
        LibraryViewModeCombo.IsDropDownOpen = false;
        CardImageModeCombo.IsDropDownOpen = false;
        TimeFormatCombo.IsDropDownOpen = false;
        BackgroundModeCombo.IsDropDownOpen = false;
        PrimaryColorField.ClosePalette();
        AccentColorField.ClosePalette();
        XConfigResolutionCombo.IsDropDownOpen = false;
    }

    /// <summary>
    /// Opens the Manage Profiles overlay.
    /// </summary>
    private void OnManageProfilesClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is SettingsViewModel vm)
        {
            SettingsViewModel.OpenManageProfiles();
        }
    }

    /// <summary>
    /// Opens a file picker and applies the chosen image as the dashboard background.
    /// </summary>
    private async void OnSelectImageClick(object? sender, RoutedEventArgs e)
    {
        await ShowImagePickerAsync();
    }

    /// <summary>
    /// Opens the background image picker on controller activation of the row.
    /// </summary>
    private void OnSelectImageRequested()
    {
        TaskUtilities.RunSafely<SettingsView>(ShowImagePickerAsync, "Opening background image picker");
    }

    /// <summary>
    /// Shows the image file picker and applies the chosen image.
    /// </summary>
    private async Task ShowImagePickerAsync()
    {
        if (DataContext is not SettingsViewModel vm)
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
            Title = LocalizationHelper.GetText("Settings.SelectImageDialogTitle"),
            AllowMultiple = false,
            FileTypeFilter =
            [
                new FilePickerFileType("Images")
                {
                    Patterns = ImageFormats.FilePickerPatterns
                }
            ]
        };

        IReadOnlyList<IStorageFile> files = await topLevel.StorageProvider.OpenFilePickerAsync(options);
        if (files.Count > 0)
        {
            vm.SetBackgroundImage(files[0].Path.LocalPath);
        }
    }
}