using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Media;

namespace XeniaManager.BigScreen.Controls.Settings;

/// <summary>
/// A colour palette row: takes an array of colours and returns the
/// selected one via <see cref="SelectedColor"/> when a swatch is clicked.
/// </summary>
public class PalettePicker : TemplatedControl
{
    /// <summary>
    /// Defines the <see cref="Palette"/> property.
    /// </summary>
    public static readonly StyledProperty<IReadOnlyList<Color>> PaletteProperty =
        AvaloniaProperty.Register<PalettePicker, IReadOnlyList<Color>>(nameof(Palette));

    /// <summary>
    /// Defines the <see cref="SelectedColor"/> property.
    /// </summary>
    public static readonly StyledProperty<Color> SelectedColorProperty =
        AvaloniaProperty.Register<PalettePicker, Color>(nameof(SelectedColor));

    /// <summary>
    /// Raised when the user picks a colour from the palette.
    /// </summary>
    public event EventHandler<ColorChangedEventArgs>? SelectedColorChanged;

    /// <summary>
    /// The colours shown in the row.
    /// </summary>
    public IReadOnlyList<Color> Palette
    {
        get => GetValue(PaletteProperty);
        set => SetValue(PaletteProperty, value);
    }

    /// <summary>
    /// The colour picked by the user.
    /// </summary>
    public Color SelectedColor
    {
        get => GetValue(SelectedColorProperty);
        set => SetValue(SelectedColorProperty, value);
    }

    private StackPanel? _panel;

    static PalettePicker()
    {
        PaletteProperty.Changed.AddClassHandler<PalettePicker>((picker, _) => picker.BuildSwatches());
        SelectedColorProperty.Changed.AddClassHandler<PalettePicker>((picker, _) => picker.UpdateHighlight());
    }

    public PalettePicker()
    {
        Focusable = false;
    }

    /// <inheritdoc/>
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        _panel = e.NameScope.Get<StackPanel>("PART_Palette");
        BuildSwatches();
    }

    /// <summary>
    /// Rebuilds the swatches from the current palette.
    /// </summary>
    private void BuildSwatches()
    {
        if (_panel == null)
        {
            return;
        }

        _panel.Children.Clear();

        foreach (Color colour in Palette)
        {
            Border swatch = new()
            {
                Width = 44,
                Height = 34,
                CornerRadius = new CornerRadius(7),
                Background = new SolidColorBrush(colour),
                BorderBrush = new SolidColorBrush(Color.FromArgb(60, 0, 0, 0)),
                BorderThickness = new Thickness(1),
                Focusable = true,
                Tag = colour
            };
            swatch.PointerPressed += OnSwatchPressed;
            swatch.KeyDown += OnSwatchKeyDown;
            _panel.Children.Add(swatch);
        }

        UpdateHighlight();
    }

    /// <summary>
    /// Highlights the swatch matching the selected colour.
    /// </summary>
    private void UpdateHighlight()
    {
        if (_panel == null)
        {
            return;
        }

        foreach (object child in _panel.Children)
        {
            if (child is not Border { Tag: Color colour } swatch)
            {
                continue;
            }

            bool selected = colour == SelectedColor;
            swatch.BorderBrush = new SolidColorBrush(selected
                ? Color.FromArgb(200, 255, 255, 255)
                : Color.FromArgb(60, 0, 0, 0));
            swatch.BorderThickness = new Thickness(selected ? 3 : 1);
        }
    }

    /// <summary>
    /// Picks the clicked colour.
    /// </summary>
    private void OnSwatchPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed && sender is Border { Tag: Color colour })
        {
            PickColour(colour);
            e.Handled = true;
        }
    }

    /// <summary>
    /// Picks a colour with the keyboard.
    /// </summary>
    private void OnSwatchKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key is Key.Enter or Key.Space)
        {
            if (sender is Border { Tag: Color colour })
            {
                PickColour(colour);
                e.Handled = true;
            }
        }
    }

    /// <summary>
    /// Applies the picked colour and raises <see cref="SelectedColorChanged"/>.
    /// </summary>
    private void PickColour(Color colour)
    {
        SelectedColor = colour;
        SelectedColorChanged?.Invoke(this, new ColorChangedEventArgs(colour, colour));
    }
}