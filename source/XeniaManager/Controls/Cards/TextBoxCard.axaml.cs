using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using FluentIcons.Common;

namespace XeniaManager.Controls.Cards;

public partial class TextBoxCard : UserControl
{
    public static readonly StyledProperty<string?> TitleProperty = AvaloniaProperty.Register<TextBoxCard, string?>(nameof(Title));

    public static readonly StyledProperty<string?> DescriptionProperty = AvaloniaProperty.Register<TextBoxCard, string?>(nameof(Description));

    public static readonly StyledProperty<string?> TooltipProperty = AvaloniaProperty.Register<TextBoxCard, string?>(nameof(Tooltip));

    public static readonly StyledProperty<Symbol?> IconProperty = AvaloniaProperty.Register<TextBoxCard, Symbol?>(nameof(Icon));

    public static readonly StyledProperty<string?> TextProperty = AvaloniaProperty.Register<TextBoxCard, string?>(
        nameof(Text),
        defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<double> TextBoxMinWidthProperty = AvaloniaProperty.Register<TextBoxCard, double>(
        nameof(TextBoxMinWidth),
        defaultValue: 160.0);

    public static readonly StyledProperty<double> TextBoxMaxWidthProperty = AvaloniaProperty.Register<TextBoxCard, double>(
        nameof(TextBoxMaxWidth),
        defaultValue: 160.0);

    public string? Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public double TextBoxMinWidth
    {
        get => GetValue(TextBoxMinWidthProperty);
        set => SetValue(TextBoxMinWidthProperty, value);
    }

    public double TextBoxMaxWidth
    {
        get => GetValue(TextBoxMaxWidthProperty);
        set => SetValue(TextBoxMaxWidthProperty, value);
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

    public string? Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public TextBoxCard() => InitializeComponent();
}