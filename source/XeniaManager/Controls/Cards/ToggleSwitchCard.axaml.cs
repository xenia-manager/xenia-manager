using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using FluentIcons.Common;

namespace XeniaManager.Controls.Cards;

public class ToggleSwitchCard : ContentControl
{
    public static readonly StyledProperty<string?> TitleProperty = AvaloniaProperty.Register<ToggleSwitchCard, string?>(nameof(Title));

    public static readonly StyledProperty<string?> DescriptionProperty = AvaloniaProperty.Register<ToggleSwitchCard, string?>(nameof(Description));

    public static readonly StyledProperty<string?> TooltipProperty = AvaloniaProperty.Register<ToggleSwitchCard, string?>(nameof(Tooltip));

    public static readonly StyledProperty<Symbol?> IconProperty = AvaloniaProperty.Register<ToggleSwitchCard, Symbol?>(nameof(Icon));

    public static readonly StyledProperty<bool> ShowIconBackgroundProperty = AvaloniaProperty.Register<CardHeader, bool>(
        nameof(ShowIconBackground),
        defaultValue: false);

    public static readonly StyledProperty<bool> IsCheckedProperty = AvaloniaProperty.Register<ToggleSwitchCard, bool>(
        nameof(IsChecked),
        defaultBindingMode: BindingMode.TwoWay);

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

    public bool ShowIconBackground
    {
        get => GetValue(ShowIconBackgroundProperty);
        set => SetValue(ShowIconBackgroundProperty, value);
    }

    public bool IsChecked
    {
        get => GetValue(IsCheckedProperty);
        set => SetValue(IsCheckedProperty, value);
    }
}