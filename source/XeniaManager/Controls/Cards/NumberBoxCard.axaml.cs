using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using FluentIcons.Common;

namespace XeniaManager.Controls.Cards;

public class NumberBoxCard : ContentControl
{
    public static readonly StyledProperty<string?> TitleProperty = AvaloniaProperty.Register<NumberBoxCard, string?>(nameof(Title));

    public static readonly StyledProperty<string?> DescriptionProperty = AvaloniaProperty.Register<NumberBoxCard, string?>(nameof(Description));

    public static readonly StyledProperty<string?> TooltipProperty = AvaloniaProperty.Register<NumberBoxCard, string?>(nameof(Tooltip));

    public static readonly StyledProperty<Symbol?> IconProperty = AvaloniaProperty.Register<NumberBoxCard, Symbol?>(nameof(Icon));

    public static readonly StyledProperty<bool> ShowIconBackgroundProperty = AvaloniaProperty.Register<NumberBoxCard, bool>(
        nameof(ShowIconBackground),
        false);

    public static readonly StyledProperty<double> MinimumProperty = AvaloniaProperty.Register<NumberBoxCard, double>(nameof(Minimum), double.MinValue);

    public static readonly StyledProperty<double?> MaximumProperty = AvaloniaProperty.Register<NumberBoxCard, double?>(nameof(Maximum), double.MaxValue);

    public static readonly StyledProperty<double> ValueProperty = AvaloniaProperty.Register<NumberBoxCard, double>(
        nameof(Value),
        defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<double> NumberBoxMaxWidthProperty = AvaloniaProperty.Register<NumberBoxCard, double>(
        nameof(NumberBoxMaxWidth),
        180.0);

    public string? Title
    {
        get
        {
            return GetValue(TitleProperty);
        }
        set
        {
            SetValue(TitleProperty, value);
        }
    }

    public string? Description
    {
        get
        {
            return GetValue(DescriptionProperty);
        }
        set
        {
            SetValue(DescriptionProperty, value);
        }
    }

    public string? Tooltip
    {
        get
        {
            return GetValue(TooltipProperty);
        }
        set
        {
            SetValue(TooltipProperty, value);
        }
    }

    public Symbol? Icon
    {
        get
        {
            return GetValue(IconProperty);
        }
        set
        {
            SetValue(IconProperty, value);
        }
    }

    public double Minimum
    {
        get
        {
            return GetValue(MinimumProperty);
        }
        set
        {
            SetValue(MinimumProperty, value);
        }
    }

    public double? Maximum
    {
        get
        {
            return GetValue(MaximumProperty);
        }
        set
        {
            SetValue(MaximumProperty, value);
        }
    }

    public double Value
    {
        get
        {
            return GetValue(ValueProperty);
        }
        set
        {
            SetValue(ValueProperty, value);
        }
    }

    public double NumberBoxMaxWidth
    {
        get
        {
            return GetValue(NumberBoxMaxWidthProperty);
        }
        set
        {
            SetValue(NumberBoxMaxWidthProperty, value);
        }
    }

    public bool ShowIconBackground
    {
        get
        {
            return GetValue(ShowIconBackgroundProperty);
        }
        set
        {
            SetValue(ShowIconBackgroundProperty, value);
        }
    }
}