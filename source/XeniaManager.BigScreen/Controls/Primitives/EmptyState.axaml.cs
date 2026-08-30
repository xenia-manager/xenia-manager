using Avalonia;
using Avalonia.Controls;
using FluentIcons.Common;

namespace XeniaManager.BigScreen.Controls.Primitives;

/// <summary>
/// A reusable "nothing here yet" stub: a card surface with a large icon and
/// a secondary-text label. Size and font variants are set at the usage site.
/// </summary>
public partial class EmptyState : UserControl
{
    /// <summary>
    /// Defines the <see cref="Symbol"/> property.
    /// </summary>
    public static readonly StyledProperty<Symbol?> SymbolProperty =
        AvaloniaProperty.Register<EmptyState, Symbol?>(nameof(Symbol));

    /// <summary>
    /// Defines the <see cref="Text"/> property.
    /// </summary>
    public static readonly StyledProperty<string> TextProperty =
        AvaloniaProperty.Register<EmptyState, string>(nameof(Text), string.Empty);

    /// <summary>
    /// Defines the <see cref="SymbolSize"/> property.
    /// </summary>
    public static readonly StyledProperty<double> SymbolSizeProperty =
        AvaloniaProperty.Register<EmptyState, double>(nameof(SymbolSize), 64);

    /// <summary>
    /// Defines the <see cref="TextSize"/> property.
    /// </summary>
    public static readonly StyledProperty<double> TextSizeProperty =
        AvaloniaProperty.Register<EmptyState, double>(nameof(TextSize), 22);

    /// <summary>
    /// The Fluent icon shown above the label.
    /// </summary>
    public Symbol? Symbol
    {
        get
        {
            return GetValue(SymbolProperty);
        }
        set
        {
            SetValue(SymbolProperty, value);
        }
    }

    /// <summary>
    /// The label below the icon.
    /// </summary>
    public string Text
    {
        get
        {
            return GetValue(TextProperty);
        }
        set
        {
            SetValue(TextProperty, value);
        }
    }

    /// <summary>
    /// The icon's font size.
    /// </summary>
    public double SymbolSize
    {
        get
        {
            return GetValue(SymbolSizeProperty);
        }
        set
        {
            SetValue(SymbolSizeProperty, value);
        }
    }

    /// <summary>
    /// The label's font size.
    /// </summary>
    public double TextSize
    {
        get
        {
            return GetValue(TextSizeProperty);
        }
        set
        {
            SetValue(TextSizeProperty, value);
        }
    }

    public EmptyState()
    {
        InitializeComponent();
    }
}