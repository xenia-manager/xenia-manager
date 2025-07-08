using System.ComponentModel;
using System.Runtime.CompilerServices;

// Imported Libraries
using XeniaManager.Core.Enum;

namespace XeniaManager.Desktop.ViewModel.Windows;
public class XeniaSelectionViewModel : INotifyPropertyChanged
{
    #region Variables
    public bool CanaryInstalled { get; set; } = App.Settings.IsXeniaInstalled(XeniaVersion.Canary);
    public bool MousehookInstalled { get; set; } = App.Settings.IsXeniaInstalled(XeniaVersion.Mousehook);
    public bool NetplayInstalled { get; set; } = App.Settings.IsXeniaInstalled(XeniaVersion.Netplay);

    #endregion

    #region Constructors
    public XeniaSelectionViewModel()
    {

    }
    #endregion

    #region Functions
    public event PropertyChangedEventHandler? PropertyChanged;
    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
    #endregion
}