using Avalonia.Controls;
using Avalonia.Interactivity;
using Microsoft.Extensions.DependencyInjection;
using XeniaManager.Core.Services;
using XeniaManager.Services;
using XeniaManager.ViewModels.Pages;

namespace XeniaManager.Views.Pages;

public partial class SettingsPage : UserControl
{
    // Variables
    private SettingsPageViewModel _viewModel { get; set; }
    private PageGamepadNavigator? _gamepadNavigator;

    // Constructor
    public SettingsPage()
    {
        InitializeComponent();
        _viewModel = App.Services.GetRequiredService<SettingsPageViewModel>();
        DataContext = _viewModel;

        // Experimental: controller navigation, see PageGamepadNavigator. Only active while
        // this page is actually attached to the visual tree, same as LibraryPage.
        AttachedToVisualTree += OnAttachedToVisualTree;
        DetachedFromVisualTree += OnDetachedFromVisualTree;
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        // Refresh settings to ensure the UI reflects current values
        _viewModel.RefreshSettings();
    }

    private void OnAttachedToVisualTree(object? sender, Avalonia.VisualTreeAttachmentEventArgs e)
    {
        _gamepadNavigator = new PageGamepadNavigator(
            App.Services.GetRequiredService<GamepadService>(),
            App.Services.GetRequiredService<NavigationService>(),
            this,
            // LB/RB cycle the General/UI/Debug tabs.
            direction => _viewModel.CycleSelectedTab(direction),
            // Land the cursor on the new tab's first control after cycling, not literally the
            // first navigable item on the page (matches XeniaSettingsPage).
            SettingsContentPanel);
        _gamepadNavigator.Activate();
    }

    private void OnDetachedFromVisualTree(object? sender, Avalonia.VisualTreeAttachmentEventArgs e)
    {
        _gamepadNavigator?.Deactivate();
        _gamepadNavigator = null;
    }
}