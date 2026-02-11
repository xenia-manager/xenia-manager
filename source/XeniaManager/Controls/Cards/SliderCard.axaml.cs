using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using FluentIcons.Common;

namespace XeniaManager.Controls.Cards;

public partial class SliderCard : UserControl
{
    public static readonly StyledProperty<string?> TitleProperty = AvaloniaProperty.Register<SliderCard, string?>(nameof(Title));

    public static readonly StyledProperty<string?> DescriptionProperty = AvaloniaProperty.Register<SliderCard, string?>(nameof(Description));

    public static readonly StyledProperty<string?> TooltipProperty = AvaloniaProperty.Register<SliderCard, string?>(nameof(Tooltip));

    public static readonly StyledProperty<Symbol?> IconProperty = AvaloniaProperty.Register<SliderCard, Symbol?>(nameof(Icon));

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
        defaultValue: 220.0);

    public string? Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public string? Description
    {
        get => GetValue(DescriptionProperty);
        set => SetValue(DescriptionProperty, value);
    }

    public string? Tooltip
    {
        get => GetValue(TooltipProperty);
        set => SetValue(TooltipProperty, value);
    }

    public Symbol? Icon
    {
        get => GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    public double Minimum
    {
        get => GetValue(MinimumProperty);
        set => SetValue(MinimumProperty, value);
    }

    public double Maximum
    {
        get => GetValue(MaximumProperty);
        set => SetValue(MaximumProperty, value);
    }

    public double Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public double TickFrequency
    {
        get => GetValue(TickFrequencyProperty);
        set => SetValue(TickFrequencyProperty, value);
    }

    public bool IsSnapToTickEnabled
    {
        get => GetValue(IsSnapToTickEnabledProperty);
        set => SetValue(IsSnapToTickEnabledProperty, value);
    }

    public TickPlacement TickPlacement
    {
        get => GetValue(TickPlacementProperty);
        set => SetValue(TickPlacementProperty, value);
    }

    public double SliderMinWidth
    {
        get => GetValue(SliderMinWidthProperty);
        set => SetValue(SliderMinWidthProperty, value);
    }

    public SliderCard() => InitializeComponent();
}