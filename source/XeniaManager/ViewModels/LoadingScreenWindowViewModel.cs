using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;

namespace XeniaManager.ViewModels;

public partial class LoadingScreenWindowViewModel : ViewModelBase
{
    [ObservableProperty] private Bitmap? _gameBackground;
    [ObservableProperty] private string _loadingText = "Loading...";
}
