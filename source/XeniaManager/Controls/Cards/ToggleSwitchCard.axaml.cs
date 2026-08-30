using System.Windows.Input;
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
        false);

    public static readonly StyledProperty<bool> IsCheckedProperty = AvaloniaProperty.Register<ToggleSwitchCard, bool>(
        nameof(IsChecked),
        defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<ICommand?> CommandProperty = AvaloniaProperty.Register<ToggleSwitchCard, ICommand?>(nameof(Command));

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

    public bool IsChecked
    {
        get
        {
            return GetValue(IsCheckedProperty);
        }
        set
        {
            SetValue(IsCheckedProperty, value);
        }
    }

    public ICommand? Command
    {
        get
        {
            return GetValue(CommandProperty);
        }
        set
        {
            SetValue(CommandProperty, value);
        }
    }
}