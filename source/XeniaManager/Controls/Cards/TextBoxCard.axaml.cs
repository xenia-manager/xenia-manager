using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using FluentIcons.Common;

namespace XeniaManager.Controls.Cards;

public class TextBoxCard : ContentControl
{
    public static readonly StyledProperty<string?> TitleProperty = AvaloniaProperty.Register<TextBoxCard, string?>(nameof(Title));

    public static readonly StyledProperty<string?> DescriptionProperty = AvaloniaProperty.Register<TextBoxCard, string?>(nameof(Description));

    public static readonly StyledProperty<string?> TooltipProperty = AvaloniaProperty.Register<TextBoxCard, string?>(nameof(Tooltip));

    public static readonly StyledProperty<Symbol?> IconProperty = AvaloniaProperty.Register<TextBoxCard, Symbol?>(nameof(Icon));

    public static readonly StyledProperty<bool> ShowIconBackgroundProperty = AvaloniaProperty.Register<CardHeader, bool>(
        nameof(ShowIconBackground),
        false);

    public static readonly StyledProperty<string?> TextProperty = AvaloniaProperty.Register<TextBoxCard, string?>(
        nameof(Text),
        defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<double> TextBoxMinWidthProperty = AvaloniaProperty.Register<TextBoxCard, double>(
        nameof(TextBoxMinWidth),
        160.0);

    public static readonly StyledProperty<double> TextBoxMaxWidthProperty = AvaloniaProperty.Register<TextBoxCard, double>(
        nameof(TextBoxMaxWidth),
        160.0);

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

    public string? Text
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

    public double TextBoxMinWidth
    {
        get
        {
            return GetValue(TextBoxMinWidthProperty);
        }
        set
        {
            SetValue(TextBoxMinWidthProperty, value);
        }
    }

    public double TextBoxMaxWidth
    {
        get
        {
            return GetValue(TextBoxMaxWidthProperty);
        }
        set
        {
            SetValue(TextBoxMaxWidthProperty, value);
        }
    }
}