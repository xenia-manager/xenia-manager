using System.Collections.ObjectModel;
using System.ComponentModel;

// Imported Libraries
using XeniaManager.Core.Mousehook;
using Logger = XeniaManager.Core.Logger;

namespace XeniaManager.Desktop.ViewModel.Windows;

public class MousehookControlsEditorViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler PropertyChanged;
    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}