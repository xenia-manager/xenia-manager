using System.ComponentModel;

// Imported Libraries
using Wpf.Ui.Controls;
using XeniaManager.Desktop.ViewModel.Windows;

namespace XeniaManager.Desktop.Views.Windows;
/// <summary>
/// Interaction logic for MousehookControlsEditor.xaml
/// </summary>
public partial class MousehookControlsEditor : FluentWindow
{
    public MousehookControlsEditorViewModel ViewModel { get; set; }
    public MousehookControlsEditor()
    {
        InitializeComponent();
        this.ViewModel = new MousehookControlsEditorViewModel();
        this.DataContext = ViewModel;
    }
}