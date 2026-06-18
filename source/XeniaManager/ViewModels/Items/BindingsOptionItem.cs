using CommunityToolkit.Mvvm.ComponentModel;

namespace XeniaManager.ViewModels.Items;

public partial class BindingsOptionItem : ObservableObject
{
    [ObservableProperty] private string _name;
    [ObservableProperty] private bool _isChecked;

    public BindingsOptionItem(string name, bool isChecked)
    {
        _name = name;
        _isChecked = isChecked;
    }
}
