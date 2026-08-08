using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace XeniaManager.BigScreen.Controls;

public partial class SettingsOverlay : UserControl
{
    /// <summary>
    /// Raised when the user wants to pick a background image.
    /// </summary>
    public event EventHandler? PickImageRequested;

    public SettingsOverlay()
    {
        InitializeComponent();
        Loaded += OnLoaded;

        // Assign the appropriate palette to each colour field
        PrimaryColorField.Palette = ColorPickerField.BackgroundPalette;
        AccentColorField.Palette = ColorPickerField.AccentPalette;
    }

    private void OnLoaded(object? sender, EventArgs e)
    {
        FocusFirst();
    }

    /// <summary>
    /// Focuses the first interactive setting (called when the overlay opens).
    /// </summary>
    public void FocusFirst()
    {
        BackgroundModeCombo.Focus();
    }

    private void OnSelectImageClick(object? sender, RoutedEventArgs e)
    {
        PickImageRequested?.Invoke(this, EventArgs.Empty);
    }
}
