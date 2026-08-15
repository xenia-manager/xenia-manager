using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using FluentIcons.Common;

namespace XeniaManager.BigScreen.Controls.Primitives;

/// <summary>
/// A keycap + label hint for the controller/keyboard actions at the bottom of screens.
/// Shows a Fluent icon when <see cref="Icon"/> is set, otherwise the <see cref="Char"/> glyph.
/// </summary>
public partial class InputHint : UserControl
{
    /// <summary>
    /// Defines the <see cref="KeyColour"/> property.
    /// </summary>
    public static readonly StyledProperty<Color> KeyColourProperty =
        AvaloniaProperty.Register<InputHint, Color>(nameof(KeyColour), Color.FromRgb(0x24, 0x28, 0x2F));

    /// <summary>
    /// Defines the <see cref="Icon"/> property.
    /// </summary>
    public static readonly StyledProperty<Symbol?> IconProperty =
        AvaloniaProperty.Register<InputHint, Symbol?>(nameof(Icon));

    /// <summary>
    /// Defines the <see cref="Char"/> property.
    /// </summary>
    public static readonly StyledProperty<string> CharProperty =
        AvaloniaProperty.Register<InputHint, string>(nameof(Char), string.Empty);

    /// <summary>
    /// Defines the <see cref="Text"/> property.
    /// </summary>
    public static readonly StyledProperty<string> TextProperty =
        AvaloniaProperty.Register<InputHint, string>(nameof(Text), string.Empty);

    /// <summary>
    /// The keycap background colour.
    /// </summary>
    public Color KeyColour
    {
        get => GetValue(KeyColourProperty);
        set => SetValue(KeyColourProperty, value);
    }

    /// <summary>
    /// Optional Fluent icon shown inside the keycap. Takes priority over <see cref="Char"/>.
    /// </summary>
    public Symbol? Icon
    {
        get => GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    /// <summary>
    /// The character shown inside the keycap when no <see cref="Icon"/> is set (e.g. "Y", "A", "B").
    /// </summary>
    public string Char
    {
        get => GetValue(CharProperty);
        set => SetValue(CharProperty, value);
    }

    /// <summary>
    /// The label shown to the right of the keycap.
    /// </summary>
    public string Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    /// <summary>
    /// Pushes the keycap colour into the border and glyph (transparent background).
    /// </summary>
    private void SyncKeyColour()
    {
        KeyBox.BorderBrush = new SolidColorBrush(KeyColour);
        IconElement.Foreground = new SolidColorBrush(KeyColour);
        CharElement.Foreground = new SolidColorBrush(KeyColour);
    }

    /// <summary>
    /// Shows either the Fluent icon or the character glyph inside the keycap.
    /// </summary>
    private void SyncGlyph()
    {
        if (Icon is { } symbol)
        {
            IconElement.Symbol = symbol;
            IconElement.IsVisible = true;
            CharElement.IsVisible = false;
        }
        else
        {
            IconElement.IsVisible = false;
            CharElement.IsVisible = true;
        }
    }

    static InputHint()
    {
        KeyColourProperty.Changed.AddClassHandler<InputHint>((hint, _) => hint.SyncKeyColour());
        IconProperty.Changed.AddClassHandler<InputHint>((hint, _) => hint.SyncGlyph());
        CharProperty.Changed.AddClassHandler<InputHint>((hint, _) => hint.SyncGlyph());
    }

    public InputHint()
    {
        InitializeComponent();
        SyncKeyColour();
        SyncGlyph();
    }
}