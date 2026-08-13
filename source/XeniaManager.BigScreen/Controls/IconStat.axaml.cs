using Avalonia;
using Avalonia.Controls;
using FluentIcons.Common;

namespace XeniaManager.BigScreen.Controls;

/// <summary>
/// A compact stat row: a Fluent icon followed by a short text value.
/// </summary>
public partial class IconStat : UserControl
{
    /// <summary>
    /// Defines the <see cref="Icon"/> property.
    /// </summary>
    public static readonly StyledProperty<Symbol> IconProperty =
        AvaloniaProperty.Register<IconStat, Symbol>(nameof(Icon));

    /// <summary>
    /// Defines the <see cref="Stat"/> property.
    /// </summary>
    public static readonly StyledProperty<string> StatProperty =
        AvaloniaProperty.Register<IconStat, string>(nameof(Stat), string.Empty);

    /// <summary>
    /// Defines the <see cref="IconSize"/> property.
    /// </summary>
    public static readonly StyledProperty<double> IconSizeProperty =
        AvaloniaProperty.Register<IconStat, double>(nameof(IconSize), 16);

    /// <summary>
    /// Defines the <see cref="Spacing"/> property.
    /// </summary>
    public static readonly StyledProperty<double> SpacingProperty =
        AvaloniaProperty.Register<IconStat, double>(nameof(Spacing), 8);

    /// <summary>
    /// Defines the <see cref="IconRotation"/> property.
    /// </summary>
    public static readonly StyledProperty<double> IconRotationProperty =
        AvaloniaProperty.Register<IconStat, double>(nameof(IconRotation), 0);

    /// <summary>
    /// The Fluent icon shown at the start of the row.
    /// </summary>
    public Symbol Icon
    {
        get => GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    /// <summary>
    /// The text shown next to the icon.
    /// </summary>
    public string Stat
    {
        get => GetValue(StatProperty);
        set => SetValue(StatProperty, value);
    }

    /// <summary>
    /// The icon size in pixels.
    /// </summary>
    public double IconSize
    {
        get => GetValue(IconSizeProperty);
        set => SetValue(IconSizeProperty, value);
    }

    /// <summary>
    /// The gap between the icon and the text.
    /// </summary>
    public double Spacing
    {
        get => GetValue(SpacingProperty);
        set => SetValue(SpacingProperty, value);
    }

    /// <summary>
    /// Rotation (degrees) applied to the icon glyph only.
    /// </summary>
    public double IconRotation
    {
        get => GetValue(IconRotationProperty);
        set => SetValue(IconRotationProperty, value);
    }

    public IconStat()
    {
        InitializeComponent();
    }
}