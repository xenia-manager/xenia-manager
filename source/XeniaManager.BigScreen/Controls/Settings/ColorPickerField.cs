using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Media;
using XeniaManager.BigScreen.Models;

namespace XeniaManager.BigScreen.Controls.Settings;

/// <summary>
/// A colour setting: shows a preview swatch + RGB sliders; clicking the swatch
/// opens a dropdown of palette colours to pick from. While its row's editor is
/// open (<see cref="IsEditorActive"/>) the slider or swatch matching
/// <see cref="ActiveTarget"/> is highlighted for the controller.
/// </summary>
public class ColorPickerField : TemplatedControl
{
    private Border? _swatch;
    private Popup? _popup;
    private PalettePicker? _palettePicker;
    private Slider? _sliderR;
    private Slider? _sliderG;
    private Slider? _sliderB;
    private TextBlock? _valueR;
    private TextBlock? _valueG;
    private TextBlock? _valueB;
    private bool _syncing;

    /// <summary>
    /// Defines the <see cref="ActiveTarget"/> property.
    /// </summary>
    public static readonly StyledProperty<ColourEditorTarget> ActiveTargetProperty =
        AvaloniaProperty.Register<ColorPickerField, ColourEditorTarget>(nameof(ActiveTarget));

    /// <summary>
    /// Defines the <see cref="IsEditorActive"/> property.
    /// </summary>
    public static readonly StyledProperty<bool> IsEditorActiveProperty =
        AvaloniaProperty.Register<ColorPickerField, bool>(nameof(IsEditorActive));

    /// <summary>
    /// Defines the computed red-slider focus state.
    /// </summary>
    public static readonly DirectProperty<ColorPickerField, bool> IsRedActiveProperty =
        AvaloniaProperty.RegisterDirect<ColorPickerField, bool>(nameof(IsRedActive), field => field.IsRedActive);

    /// <summary>
    /// Defines the computed green-slider focus state.
    /// </summary>
    public static readonly DirectProperty<ColorPickerField, bool> IsGreenActiveProperty =
        AvaloniaProperty.RegisterDirect<ColorPickerField, bool>(nameof(IsGreenActive), field => field.IsGreenActive);

    /// <summary>
    /// Defines the computed blue-slider focus state.
    /// </summary>
    public static readonly DirectProperty<ColorPickerField, bool> IsBlueActiveProperty =
        AvaloniaProperty.RegisterDirect<ColorPickerField, bool>(nameof(IsBlueActive), field => field.IsBlueActive);

    /// <summary>
    /// Defines the computed preview focus state.
    /// </summary>
    public static readonly DirectProperty<ColorPickerField, bool> IsPreviewActiveProperty =
        AvaloniaProperty.RegisterDirect<ColorPickerField, bool>(nameof(IsPreviewActive),
            field => field.IsPreviewActive);

    private bool _isRedActive;
    private bool _isGreenActive;
    private bool _isBlueActive;
    private bool _isPreviewActive;

    /// <summary>
    /// The slider or preview swatch the controller is currently focused on.
    /// </summary>
    public ColourEditorTarget ActiveTarget
    {
        get
        {
            return GetValue(ActiveTargetProperty);
        }
        set
        {
            SetValue(ActiveTargetProperty, value);
        }
    }

    /// <summary>
    /// Whether this row's colour editor is open (only the active row highlights).
    /// </summary>
    public bool IsEditorActive
    {
        get
        {
            return GetValue(IsEditorActiveProperty);
        }
        set
        {
            SetValue(IsEditorActiveProperty, value);
        }
    }

    /// <summary>
    /// Whether the red slider is the controller's focused target.
    /// </summary>
    public bool IsRedActive
    {
        get
        {
            return _isRedActive;
        }
    }

    /// <summary>
    /// Whether the green slider is the controller's focused target.
    /// </summary>
    public bool IsGreenActive
    {
        get
        {
            return _isGreenActive;
        }
    }

    /// <summary>
    /// Whether the blue slider is the controller's focused target.
    /// </summary>
    public bool IsBlueActive
    {
        get
        {
            return _isBlueActive;
        }
    }

    /// <summary>
    /// Whether the preview swatch is the controller's focused target.
    /// </summary>
    public bool IsPreviewActive
    {
        get
        {
            return _isPreviewActive;
        }
    }

    /// <summary>
    /// Re-evaluates the four target highlights after the active target or the
    /// editor-active state changed through Avalonia's direct-property system.
    /// </summary>
    private void OnTargetStateChanged()
    {
        SetAndRaise(IsRedActiveProperty, ref _isRedActive,
            IsEditorActive && ActiveTarget == ColourEditorTarget.Red);
        SetAndRaise(IsGreenActiveProperty, ref _isGreenActive,
            IsEditorActive && ActiveTarget == ColourEditorTarget.Green);
        SetAndRaise(IsBlueActiveProperty, ref _isBlueActive,
            IsEditorActive && ActiveTarget == ColourEditorTarget.Blue);
        SetAndRaise(IsPreviewActiveProperty, ref _isPreviewActive,
            IsEditorActive && ActiveTarget == ColourEditorTarget.Preview);
    }

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
        get
        {
            return GetValue(ColorProperty);
        }
        set
        {
            SetValue(ColorProperty, value);
        }
    }

    /// <summary>
    /// The palette colours shown when the swatch is clicked.
    /// </summary>
    public IReadOnlyList<Color> Palette
    {
        get
        {
            return GetValue(PaletteProperty);
        }
        set
        {
            SetValue(PaletteProperty, value);
        }
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
    /// Converts a slider value to a colour channel byte.
    /// </summary>
    private static byte ToByte(double value) => (byte)Math.Clamp(Math.Round(value), 0, 255);

    /// <summary>
    /// Rebuilds the colour from the three sliders as any of them changes.
    /// </summary>
    private void OnRgbSliderChanged(object? sender, RangeBaseValueChangedEventArgs e)
    {
        if (_syncing || _sliderR == null || _sliderG == null || _sliderB == null)
        {
            return;
        }

        Color = Color.FromRgb(ToByte(_sliderR.Value), ToByte(_sliderG.Value), ToByte(_sliderB.Value));
    }

    /// <summary>
    /// Pushes the current colour into the swatch, palette picker and RGB sliders.
    /// </summary>
    private void SyncFromColor()
    {
        _syncing = true;
        try
        {
            _swatch?.Background = new SolidColorBrush(Color);

            _palettePicker?.SelectedColor = Color;

            _sliderR?.SetValue(Slider.ValueProperty, Color.R);
            _sliderG?.SetValue(Slider.ValueProperty, Color.G);
            _sliderB?.SetValue(Slider.ValueProperty, Color.B);

            _valueR?.SetValue(TextBlock.TextProperty, Color.R.ToString());
            _valueG?.SetValue(TextBlock.TextProperty, Color.G.ToString());
            _valueB?.SetValue(TextBlock.TextProperty, Color.B.ToString());
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
        _popup = e.NameScope.Get<Popup>("PART_Popup");
        _palettePicker = e.NameScope.Get<PalettePicker>("PART_Palette");
        _sliderR = e.NameScope.Get<Slider>("PART_SliderR");
        _sliderG = e.NameScope.Get<Slider>("PART_SliderG");
        _sliderB = e.NameScope.Get<Slider>("PART_SliderB");
        _valueR = e.NameScope.Get<TextBlock>("PART_ValueR");
        _valueG = e.NameScope.Get<TextBlock>("PART_ValueG");
        _valueB = e.NameScope.Get<TextBlock>("PART_ValueB");

        if (_swatch != null)
        {
            _swatch.PointerPressed += OnSwatchPressed;
        }

        _popup?.PlacementTarget = this;

        if (_palettePicker != null)
        {
            _palettePicker.SelectedColorChanged += OnPickerColourChanged;
        }

        foreach (Slider? slider in new[]
                 {
                     _sliderR, _sliderG, _sliderB
                 })
        {
            if (slider != null)
            {
                slider.ValueChanged += OnRgbSliderChanged;
            }
        }

        SyncPalette();
        SyncFromColor();
    }

    static ColorPickerField()
    {
        ColorProperty.Changed.AddClassHandler<ColorPickerField>((field, _) => field.SyncFromColor());
        PaletteProperty.Changed.AddClassHandler<ColorPickerField>((field, _) => field.SyncPalette());
        ActiveTargetProperty.Changed.AddClassHandler<ColorPickerField>((field, _) => field.OnTargetStateChanged());
        IsEditorActiveProperty.Changed.AddClassHandler<ColorPickerField>((field, _) => field.OnTargetStateChanged());
    }

    public ColorPickerField()
    {
        Focusable = false;
    }
}