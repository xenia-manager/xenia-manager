using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using XeniaManager.BigScreen.Controls;
using XeniaManager.BigScreen.Utilities;
using XeniaManager.BigScreen.ViewModels;
using XeniaManager.Core.Utilities;

namespace XeniaManager.BigScreen.Views;

/// <summary>
/// Full-screen settings screen: dashboard appearance and quit behaviour.
/// </summary>
public partial class SettingsView : UserControl
{
    public SettingsView()
    {
        InitializeComponent();
        Loaded += OnLoaded;

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

    /// <summary>
    /// Opens a file picker and applies the chosen image as the dashboard background.
    /// </summary>
    private async void OnSelectImageClick(object? sender, RoutedEventArgs e)
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
                    Patterns = ImageFormats.FilePickerPatterns,
                },
            ],
        };

        IReadOnlyList<IStorageFile> files = await topLevel.StorageProvider.OpenFilePickerAsync(options);
        if (files.Count > 0)
        {
            vm.SetBackgroundImage(files[0].Path.LocalPath);
        }
    }
}
