using Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;
using XeniaManager.Core.Services;
using XeniaManager.Services;
using XeniaManager.ViewModels.Pages;

namespace XeniaManager.Views.Pages;

public partial class AboutPage : UserControl
{
    // Variables
    private AboutPageViewModel _viewModel { get; set; }
    private PageGamepadNavigator? _gamepadNavigator;

    // Constructor
    public AboutPage()
    {
        InitializeComponent();
        _viewModel = App.Services.GetRequiredService<AboutPageViewModel>();
        DataContext = _viewModel;

        // Experimental: controller navigation, see PageGamepadNavigator. Only active while
        // this page is actually attached to the visual tree, same as LibraryPage.
        AttachedToVisualTree += OnAttachedToVisualTree;
        DetachedFromVisualTree += OnDetachedFromVisualTree;
    }

    private void OnAttachedToVisualTree(object? sender, Avalonia.VisualTreeAttachmentEventArgs e)
    {
        _gamepadNavigator = new PageGamepadNavigator(
            App.Services.GetRequiredService<GamepadService>(),
            App.Services.GetRequiredService<NavigationService>(),
            this);
        _gamepadNavigator.Activate();
    }

    private void OnDetachedFromVisualTree(object? sender, Avalonia.VisualTreeAttachmentEventArgs e)
    {
        _gamepadNavigator?.Deactivate();
        _gamepadNavigator = null;
    }
}