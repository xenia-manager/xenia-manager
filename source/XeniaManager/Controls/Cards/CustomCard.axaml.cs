using Avalonia;
using Avalonia.Controls;

namespace XeniaManager.Controls.Cards;

public class CustomCard : ContentControl
{
    public static readonly StyledProperty<string?> TooltipProperty = AvaloniaProperty.Register<CustomCard, string?>(nameof(Tooltip));

    public string? Tooltip
    {
        get => GetValue(TooltipProperty);
        set => SetValue(TooltipProperty, value);
    }
}