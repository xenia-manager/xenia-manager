using System.Collections;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Markup.Xaml.Templates;
using FluentIcons.Common;

namespace XeniaManager.Controls.Cards;

public class ComboBoxCard : ContentControl
{
    public static readonly StyledProperty<string?> TitleProperty = AvaloniaProperty.Register<ComboBoxCard, string?>(nameof(Title));

    public static readonly StyledProperty<string?> DescriptionProperty = AvaloniaProperty.Register<ComboBoxCard, string?>(nameof(Description));

    public static readonly StyledProperty<string?> TooltipProperty = AvaloniaProperty.Register<ComboBoxCard, string?>(nameof(Tooltip));

    public static readonly StyledProperty<Symbol?> IconProperty = AvaloniaProperty.Register<ComboBoxCard, Symbol?>(nameof(Icon));

    public static readonly StyledProperty<bool> ShowIconBackgroundProperty = AvaloniaProperty.Register<CardHeader, bool>(
        nameof(ShowIconBackground),
        false);

    public static readonly StyledProperty<IEnumerable?> ItemsSourceProperty = AvaloniaProperty.Register<ComboBoxCard, IEnumerable?>(nameof(ItemsSource));

    public static readonly StyledProperty<object?> SelectedItemProperty = AvaloniaProperty.Register<ComboBoxCard, object?>(
        nameof(SelectedItem),
        defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<int> SelectedIndexProperty = AvaloniaProperty.Register<ComboBoxCard, int>(
        nameof(SelectedIndex),
        -1,
        defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<DataTemplate?> ItemTemplateProperty = AvaloniaProperty.Register<ComboBoxCard, DataTemplate?>(nameof(ItemTemplate));

    public static readonly StyledProperty<double> ComboBoxMinWidthProperty = AvaloniaProperty.Register<ComboBoxCard, double>(
        nameof(ComboBoxMinWidth),
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

    public IEnumerable? ItemsSource
    {
        get
        {
            return GetValue(ItemsSourceProperty);
        }
        set
        {
            SetValue(ItemsSourceProperty, value);
        }
    }

    public object? SelectedItem
    {
        get
        {
            return GetValue(SelectedItemProperty);
        }
        set
        {
            SetValue(SelectedItemProperty, value);
        }
    }

    public int SelectedIndex
    {
        get
        {
            return GetValue(SelectedIndexProperty);
        }
        set
        {
            SetValue(SelectedIndexProperty, value);
        }
    }

    public DataTemplate? ItemTemplate
    {
        get
        {
            return GetValue(ItemTemplateProperty);
        }
        set
        {
            SetValue(ItemTemplateProperty, value);
        }
    }

    public double ComboBoxMinWidth
    {
        get
        {
            return GetValue(ComboBoxMinWidthProperty);
        }
        set
        {
            SetValue(ComboBoxMinWidthProperty, value);
        }
    }
}