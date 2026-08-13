using Avalonia.Controls;
using Avalonia.Interactivity;
using XeniaManager.BigScreen.ViewModels;

namespace XeniaManager.BigScreen.Views;

/// <summary>
/// Full-screen screenshot viewer: uniform-stretched image with caption and
/// chevron navigation through the gallery.
/// </summary>
public partial class GalleryViewerView : UserControl
{
    public GalleryViewerView()
    {
        InitializeComponent();
    }

    private void OnPrevClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is GalleryViewerViewModel vm)
        {
            vm.Step(-1);
        }
    }

    private void OnNextClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is GalleryViewerViewModel vm)
        {
            vm.Step(1);
        }
    }
}