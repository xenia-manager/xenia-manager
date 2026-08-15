using System.Collections.Generic;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Media;

namespace XeniaManager.BigScreen.Controls.Settings;

/// <summary>
/// A colour setting: shows a preview swatch + hex field; clicking the swatch
/// opens a dropdown of palette colours to pick from.
/// </summary>
public class ColorPickerField : TemplatedControl
{
    /// <summary>
    /// Length of a 24-bit hex colour string (e.g. "1C1F25").
    /// </summary>
    private const int HexColorLength = 6;

    private Border? _swatch;
    private TextBox? _hexBox;
    private Popup? _popup;
    private PalettePicker? _palettePicker;
    private bool _syncing;

    /// <summary>
    /// Defines the <see cref="Color"/> property.
    /// </summary>
    public static readonly StyledProperty<Color> ColorProperty =
        AvaloniaProperty.Register<ColorPickerField, Color>(nameof(Color));

    /// <summary>
    /// Defines the <see cref="Palette"/> property.
    /// </summary>
    public static readonly StyledProperty<IReadOnlyList<Color>> PaletteProperty =
        AvaloniaProperty.Register<ColorPickerField, IReadOnlyList<Color>>(nameof(Palette));

    /// <summary>
    /// Muted slate/grey palette used for background/primary colours.
    /// </summary>
    public static readonly Color[] BackgroundPalette =
    [
        Color.FromRgb(0x14, 0x16, 0x1A), // Near black slate
        Color.FromRgb(0x1C, 0x1F, 0x25), // Slate
        Color.FromRgb(0x24, 0x28, 0x2F), // Light slate
        Color.FromRgb(0x2E, 0x33, 0x3B), // Grey slate
        Color.FromRgb(0x3A, 0x40, 0x49), // Grey
        Color.FromRgb(0x49, 0x4F, 0x59), // Light grey
        Color.FromRgb(0x5A, 0x61, 0x6B), // Silver grey
        Color.FromRgb(0x6E, 0x75, 0x80) // Pale grey
    ];

    /// <summary>
    /// Vibrant palette used for the accent colour.
    /// </summary>
    public static readonly Color[] AccentPalette =
    [
        Color.FromRgb(0xC0, 0x2B, 0x1D), // Red
        Color.FromRgb(0x2E, 0xCC, 0x40), // Bright green
        Color.FromRgb(0x10, 0x7C, 0x41), // Dark green
        Color.FromRgb(0x3A, 0x96, 0xDD), // Sky blue
        Color.FromRgb(0x1F, 0x4E, 0x79), // Navy
        Color.FromRgb(0x7D, 0x5B, 0xA6), // Purple
        Color.FromRgb(0xE6, 0x7E, 0x22), // Orange
        Color.FromRgb(0xF1, 0xC4, 0x0F), // Yellow
        Color.FromRgb(0xC0, 0xC0, 0xC0), // Light grey
        Color.FromRgb(0xFF, 0xFF, 0xFF) // White
    ];

    /// <summary>
    /// The currently selected colour.
    /// </summary>
    public Color Color
    {
        get => GetValue(ColorProperty);
        set => SetValue(ColorProperty, value);
    }

    /// <summary>
    /// The palette colours shown when the swatch is clicked.
    /// </summary>
    public IReadOnlyList<Color> Palette
    {
        get => GetValue(PaletteProperty);
        set => SetValue(PaletteProperty, value);
    }

    /// <summary>
    /// Applies a colour picked from the palette dropdown and closes it.
    /// </summary>
    private void OnPickerColourChanged(object? sender, ColorChangedEventArgs e)
    {
        Color = e.NewColor;
        _popup?.IsOpen = false;
    }

    /// <summary>
    /// Pushes the current palette into the picker control.
    /// </summary>
    private void SyncPalette()
    {
        if (_palettePicker != null)
        {
            _palettePicker.Palette = Palette;
            _palettePicker.SelectedColor = Color;
        }
    }

    /// <summary>
    /// Opens the palette dropdown when the preview swatch is clicked.
    /// </summary>
    private void OnSwatchPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed && _popup != null)
        {
            _popup.IsOpen = true;
            e.Handled = true;
        }
    }

    /// <summary>
    /// Parses the hex box and applies the colour when valid.
    /// </summary>
    private void OnHexTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (_syncing || _hexBox == null)
        {
            return;
        }

        string text = _hexBox.Text?.Trim().TrimStart('#') ?? string.Empty;
        if (text.Length == HexColorLength &&
            uint.TryParse(text, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out uint value))
        {
            Color = Color.FromRgb(
                (byte)((value >> 16) & 0xFF),
                (byte)((value >> 8) & 0xFF),
                (byte)(value & 0xFF));
        }
    }

    /// <summary>
    /// Applies the current colour on Enter without losing focus.
    /// </summary>
    private void OnHexKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key is Key.Enter or Key.Tab)
        {
            e.Handled = true;
            OnHexTextChanged(_hexBox, new TextChangedEventArgs(TextBox.TextChangedEvent, _hexBox));
        }
    }

    /// <summary>
    /// Pushes the current colour into the swatch, hex box and palette picker.
    /// </summary>
    private void SyncFromColor()
    {
        _syncing = true;
        try
        {
            _swatch?.Background = new SolidColorBrush(Color);

            _hexBox?.Text = $"#{Color.R:X2}{Color.G:X2}{Color.B:X2}";

            _palettePicker?.SelectedColor = Color;
        }
        finally
        {
            _syncing = false;
        }
    }

    /// <summary>
    /// Opens the palette dropdown (controller activation of the row).
    /// </summary>
    public void OpenPalette()
    {
        if (_popup != null)
        {
            _popup.IsOpen = true;
        }
    }

    /// <summary>
    /// Closes the palette dropdown (editor commit or cancel).
    /// </summary>
    public void ClosePalette()
    {
        if (_popup != null)
        {
            _popup.IsOpen = false;
        }
    }

    /// <inheritdoc/>
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        _swatch = e.NameScope.Get<Border>("PART_Swatch");
        _hexBox = e.NameScope.Get<TextBox>("PART_HexBox");
        _popup = e.NameScope.Get<Popup>("PART_Popup");
        _palettePicker = e.NameScope.Get<PalettePicker>("PART_Palette");

        if (_swatch != null)
        {
            _swatch.PointerPressed += OnSwatchPressed;
        }

        _popup?.PlacementTarget = this;

        if (_hexBox != null)
        {
            _hexBox.TextChanged += OnHexTextChanged;
            _hexBox.KeyDown += OnHexKeyDown;
        }

        if (_palettePicker != null)
        {
            _palettePicker.SelectedColorChanged += OnPickerColourChanged;
        }

        SyncPalette();
        SyncFromColor();
    }

    static ColorPickerField()
    {
        ColorProperty.Changed.AddClassHandler<ColorPickerField>((field, _) => field.SyncFromColor());
        PaletteProperty.Changed.AddClassHandler<ColorPickerField>((field, _) => field.SyncPalette());
    }

    public ColorPickerField()
    {
        Focusable = false;
    }
}