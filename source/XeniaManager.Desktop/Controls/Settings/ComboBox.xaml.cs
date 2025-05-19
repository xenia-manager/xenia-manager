using System.Collections;
using System.Windows;
using System.Windows.Controls;

namespace XeniaManager.Desktop.Controls.Settings;

public partial class ComboBox : UserControl
{
    public ComboBox()
    {
        InitializeComponent();
    }

    // Label text
    public static readonly DependencyProperty LabelProperty =
        DependencyProperty.Register(nameof(Label), typeof(string),
            typeof(ComboBox), new PropertyMetadata("Label"));

    public string Label
    {
        get => (string)GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    // Tooltip text
    public static readonly DependencyProperty ToolTipTextProperty =
        DependencyProperty.Register(
            nameof(ToolTipText),
            typeof(string),
            typeof(ComboBox),    // your control’s CLR type!
            new PropertyMetadata(string.Empty)
        );

    public string ToolTipText
    {
        get => (string)GetValue(ToolTipTextProperty);
        set => SetValue(ToolTipTextProperty, value);
    }
    
    public static readonly DependencyProperty ItemsSourceProperty =
        DependencyProperty.Register(nameof(ItemsSource), typeof(IEnumerable),
            typeof(ComboBox), new PropertyMetadata(null));

    public IEnumerable ItemsSource
    {
        get => (IEnumerable)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }
    
    public static readonly DependencyProperty SelectedItemProperty =
        DependencyProperty.Register(nameof(SelectedItem), typeof(object),
            typeof(ComboBox), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    public object SelectedItem
    {
        get => GetValue(SelectedItemProperty);
        set => SetValue(SelectedItemProperty, value);
    }
}