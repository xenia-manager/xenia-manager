using Avalonia;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using FluentIcons.Common;

namespace XeniaManager.Controls.Cards;

public class CardHeader : TemplatedControl
{
    public static readonly StyledProperty<string?> TitleProperty = AvaloniaProperty.Register<CardHeader, string?>(nameof(Title));

    public static readonly StyledProperty<string?> DescriptionProperty = AvaloniaProperty.Register<CardHeader, string?>(nameof(Description));

    public static readonly StyledProperty<string?> TooltipProperty = AvaloniaProperty.Register<CardHeader, string?>(nameof(Tooltip));

    public static readonly StyledProperty<Symbol?> IconProperty = AvaloniaProperty.Register<CardHeader, Symbol?>(nameof(Icon));

    public static readonly StyledProperty<bool> ShowIconBackgroundProperty = AvaloniaProperty.Register<CardHeader, bool>(
        nameof(ShowIconBackground),
        false);

    public static readonly StyledProperty<object?> ActionContentProperty = AvaloniaProperty.Register<CardHeader, object?>(nameof(ActionContent));

    public static readonly StyledProperty<IDataTemplate?> ActionContentTemplateProperty =
        AvaloniaProperty.Register<CardHeader, IDataTemplate?>(nameof(ActionContentTemplate));

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

    public object? ActionContent
    {
        get
        {
            return GetValue(ActionContentProperty);
        }
        set
        {
            SetValue(ActionContentProperty, value);
        }
    }

    public IDataTemplate? ActionContentTemplate
    {
        get
        {
            return GetValue(ActionContentTemplateProperty);
        }
        set
        {
            SetValue(ActionContentTemplateProperty, value);
        }
    }
}