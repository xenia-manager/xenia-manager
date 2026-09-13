using CommunityToolkit.Mvvm.ComponentModel;

namespace XeniaManager.ViewModels.Controls;

/// <summary>
/// ViewModel for the read-only text file viewer dialog.
/// </summary>
public partial class TextFileDialogViewModel : ViewModelBase
{
    /// <summary>The displayed file name.</summary>
    [ObservableProperty] private string _fileName = string.Empty;

    /// <summary>The decoded text content.</summary>
    [ObservableProperty] private string _content = string.Empty;

    /// <summary>The detected encoding display name.</summary>
    [ObservableProperty] private string _encodingName = string.Empty;

    public TextFileDialogViewModel(string fileName, string content, string encodingName)
    {
        _fileName = fileName;
        _content = content;
        _encodingName = encodingName;
    }
}