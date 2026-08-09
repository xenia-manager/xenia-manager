using Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;
using XeniaManager.Core.Services;
using XeniaManager.Services;
using XeniaManager.ViewModels.Pages;

namespace XeniaManager.Views.Pages;

public partial class XeniaSettingsPage : UserControl
{
    // Variables
    private XeniaSettingsPageViewModel _viewModel { get; set; }
    private PageGamepadNavigator? _gamepadNavigator;

    // Constructor
    public XeniaSettingsPage()
    {
        InitializeComponent();
        _viewModel = App.Services.GetRequiredService<XeniaSettingsPageViewModel>();
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
            this,
            // LB/RB cycle the config editor's section tabs (Audio/CPU/Display/...), evaluated
            // fresh each call since ConfigEditorViewModel is replaced whenever the config file
            // is (re)loaded.
            direction => _viewModel.ConfigEditorViewModel?.CycleSelectedSection(direction),
            // After cycling tabs, land the cursor on the new tab's first setting rather than
            // the config-file picker ComboBox above the tab strip.
            ConfigEditorControlInstance);
        _gamepadNavigator.Activate();
    }

    private void OnDetachedFromVisualTree(object? sender, Avalonia.VisualTreeAttachmentEventArgs e)
    {
        _gamepadNavigator?.Deactivate();
        _gamepadNavigator = null;
    }
}