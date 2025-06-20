
// Imported Libraries
using Wpf.Ui.Controls;
using XeniaManager.Core;
using XeniaManager.Desktop.ViewModel.Windows;

namespace XeniaManager.Desktop.Views.Windows;

/// <summary>
/// Interaction logic for XeniaSelection.xaml
/// </summary>
public partial class XeniaSelection : FluentWindow
{
    #region Variables
    public XeniaSelectionViewModel ViewModel { get; set; }
    public XeniaVersion? SelectedXenia { get; set; } = null;
    #endregion
    #region Constructors
    public XeniaSelection()
    {
        InitializeComponent();
        this.ViewModel = new XeniaSelectionViewModel();
        this.DataContext = ViewModel;
    }
    #endregion

    #region Events & Functions

    #endregion

    private void BtnSelection_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        Button button = (Button)sender;
        if (button.Tag is XeniaVersion version)
        {
            SelectedXenia = version;
            DialogResult = true;
            Close();
        }
    }
}