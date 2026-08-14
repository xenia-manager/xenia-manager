using Avalonia.Controls;
using Avalonia.Interactivity;
using XeniaManager.BigScreen.ViewModels.Modals;

namespace XeniaManager.BigScreen.Views.Modals;

/// <summary>
/// Full-screen screenshot viewer modal: uniform-stretched image with caption
/// and chevron navigation through the surrounding screenshots.
/// </summary>
public partial class ScreenshotViewerView : UserControl
{
    public ScreenshotViewerView()
    {
        InitializeComponent();
    }

    private void OnPrevClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is ScreenshotViewerViewModel vm)
        {
            vm.Step(-1);
        }
    }

    private void OnNextClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is ScreenshotViewerViewModel vm)
        {
            vm.Step(1);
        }
    }
}
