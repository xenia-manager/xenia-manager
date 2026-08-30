using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using FluentIcons.Common;

namespace XeniaManager.Controls.Cards;

public class SliderCard : ContentControl
{
    public static readonly StyledProperty<string?> TitleProperty = AvaloniaProperty.Register<SliderCard, string?>(nameof(Title));

    public static readonly StyledProperty<string?> DescriptionProperty = AvaloniaProperty.Register<SliderCard, string?>(nameof(Description));

    public static readonly StyledProperty<string?> TooltipProperty = AvaloniaProperty.Register<SliderCard, string?>(nameof(Tooltip));

    public static readonly StyledProperty<Symbol?> IconProperty = AvaloniaProperty.Register<SliderCard, Symbol?>(nameof(Icon));

    public static readonly StyledProperty<bool> ShowIconBackgroundProperty = AvaloniaProperty.Register<CardHeader, bool>(
        nameof(ShowIconBackground),
        false);

    public static readonly StyledProperty<double> MinimumProperty = AvaloniaProperty.Register<SliderCard, double>(nameof(Minimum), 0);

    public static readonly StyledProperty<double> MaximumProperty = AvaloniaProperty.Register<SliderCard, double>(nameof(Maximum), 100);

    public static readonly StyledProperty<double> ValueProperty = AvaloniaProperty.Register<SliderCard, double>(
        nameof(Value),
        defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<double> TickFrequencyProperty = AvaloniaProperty.Register<SliderCard, double>(nameof(TickFrequency), 1);

    public static readonly StyledProperty<bool> IsSnapToTickEnabledProperty = AvaloniaProperty.Register<SliderCard, bool>(nameof(IsSnapToTickEnabled));

    public static readonly StyledProperty<TickPlacement> TickPlacementProperty = AvaloniaProperty.Register<SliderCard, TickPlacement>(
        nameof(TickPlacement),
        TickPlacement.None);

    public static readonly StyledProperty<double> SliderMinWidthProperty = AvaloniaProperty.Register<SliderCard, double>(
        nameof(SliderMinWidth),
        220.0);

    public static readonly StyledProperty<string?> ValueFormatProperty = AvaloniaProperty.Register<SliderCard, string?>(
        nameof(ValueFormat),
        "F0");

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

    public double Maximum
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

    public double TickFrequency
    {
        get
        {
            return GetValue(TickFrequencyProperty);
        }
        set
        {
            SetValue(TickFrequencyProperty, value);
        }
    }

    public bool IsSnapToTickEnabled
    {
        get
        {
            return GetValue(IsSnapToTickEnabledProperty);
        }
        set
        {
            SetValue(IsSnapToTickEnabledProperty, value);
        }
    }

    public TickPlacement TickPlacement
    {
        get
        {
            return GetValue(TickPlacementProperty);
        }
        set
        {
            SetValue(TickPlacementProperty, value);
        }
    }

    public double SliderMinWidth
    {
        get
        {
            return GetValue(SliderMinWidthProperty);
        }
        set
        {
            SetValue(SliderMinWidthProperty, value);
        }
    }

    public string? ValueFormat
    {
        get
        {
            return GetValue(ValueFormatProperty);
        }
        set
        {
            SetValue(ValueFormatProperty, value);
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