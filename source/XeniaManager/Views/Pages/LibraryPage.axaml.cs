using Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;
using XeniaManager.ViewModels.Pages;

namespace XeniaManager.Views.Pages;

public partial class LibraryPage : UserControl
{
    // Variables
    private LibraryPageViewModel _viewModel { get; set; }

    // Constructor
    public LibraryPage()
    {
        InitializeComponent();
        _viewModel = App.Services.GetRequiredService<LibraryPageViewModel>();
        DataContext = _viewModel;
    }
}