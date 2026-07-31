using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using FluentAvalonia.UI.Controls;
using Microsoft.Extensions.DependencyInjection;
using XeniaManager.Core.Logging;
using XeniaManager.Core.Services;
using XeniaManager.Core.Utilities;
using XeniaManager.Services;
using XeniaManager.ViewModels;

namespace XeniaManager.Views;

public partial class MainView : UserControl
{
    // Properties
    private MainViewModel _viewModel { get; set; }
    private NavigationService _navigationService { get; set; }
    private IMessageBoxService _messageBoxService { get; set; }
    private XInputService? _xInputService;

    // Experimental controller navigation of the side menu: tracks the currently
    // highlighted item and whether the menu is the active navigation context
    private readonly object _navigationOwner = new object();
    private List<FANavigationViewItem> _menuItems = [];
    private int _menuCursorIndex = -1;

    // Constructor
    public MainView()
    {
        InitializeComponent();
        _viewModel = App.Services.GetRequiredService<MainViewModel>();
        _messageBoxService = App.Services.GetRequiredService<IMessageBoxService>();
        DataContext = _viewModel;

        // Set up the navigation service
        _navigationService = App.Services.GetRequiredService<NavigationService>();
        _navigationService.SetContentFrame(ContentFrame);
        _navigationService.SetNavigationView(NavigationView);
        _navigationService.SetControllerMenuActivator(ActivateControllerMenuNavigation);

        // Auto-fit pane width to content when opening
        NavigationView.PaneOpening += NavigationView_OnPaneOpening;

        // Experimental: controller navigation of the side menu
        _xInputService = App.Services.GetRequiredService<XInputService>();
        _xInputService.NavigationActionTriggered += OnControllerNavigationAction;

        // Navigate to Library (Default Page)
        _ = _navigationService.NavigateToTag("Library");
    }

    // Functions
    private void NavigationView_OnPaneOpening(FANavigationView sender, EventArgs args)
    {
        try
        {
            double calculatedWidth = CalculateRequiredPaneWidth();
            Logger.Debug<MainView>($"Auto-fitting pane width to {calculatedWidth:F0}px");
            sender.OpenPaneLength = calculatedWidth;
        }
        catch (Exception ex)
        {
            Logger.Error<MainView>("Failed to auto-fit pane width, falling back to default");
            Logger.LogExceptionDetails<MainView>(ex);
        }
    }

    private double CalculateRequiredPaneWidth()
    {
        IEnumerable<FANavigationViewItem> items = NavigationView.MenuItems
            .Concat(NavigationView.FooterMenuItems)
            .OfType<FANavigationViewItem>()
            .Where(i => i.Content is string text && !string.IsNullOrEmpty(text));

        double maxTextWidth = 0;
        double fontSize = NavigationView.FontSize > 0 ? NavigationView.FontSize : 14;
        Typeface typeface = new Typeface(NavigationView.FontFamily ?? FontFamily.Default);

        foreach (FANavigationViewItem item in items)
        {
            FormattedText formattedText = new FormattedText(
                (string)item.Content!,
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                typeface,
                fontSize,
                null);

            Logger.Debug<MainView>($"Item \"{item.Content}\" measured at {formattedText.Width:F1}px");

            if (formattedText.Width > maxTextWidth)
            {
                maxTextWidth = formattedText.Width;
            }
        }

        const double nonTextWidth = 76;
        double clampedWidth = Math.Clamp(maxTextWidth + nonTextWidth, 180, 500);
        Logger.Debug<MainView>($"Max text width: {maxTextWidth:F1}px, total: {maxTextWidth + nonTextWidth:F0}px, clamped: {clampedWidth:F0}px");
        return clampedWidth;
    }

    private async void NavigationView_OnItemInvoked(object? sender, FANavigationViewItemInvokedEventArgs e)
    {
        try
        {
            if (e.InvokedItemContainer is FANavigationViewItem selectedItem)
            {
                await _navigationService.Navigate(selectedItem, ContentFrame);
            }
        }
        catch (Exception ex)
        {
            Logger.Error<MainView>("Failed to navigate to page");
            Logger.LogExceptionDetails<MainView>(ex);
            await _messageBoxService.ShowErrorAsync(LocalizationHelper.GetText("MainView.Navigation.Error.Title"),
                string.Format(LocalizationHelper.GetText("MainView.Navigation.Error.Message"), ex));
        }
    }

    // --- Controller navigation of the side menu (experimental) ------------

    /// <summary>
    /// Makes the side navigation menu (Open Xenia, Library, Xenia Settings, Manage Xenia,
    /// plus the footer items Settings/About) the active controller navigation context, and
    /// highlights the first item. Called by <see cref="NavigationService.FocusNavigationMenu"/>
    /// when the user presses B on a page, mimicking backing out to a console dashboard menu.
    /// </summary>
    private void ActivateControllerMenuNavigation()
    {
        if (_xInputService == null)
        {
            return;
        }

        _menuItems = NavigationView.MenuItems
            .Concat(NavigationView.FooterMenuItems)
            .OfType<FANavigationViewItem>()
            .Where(i => i.IsEnabled)
            .ToList();

        if (_menuItems.Count == 0)
        {
            return;
        }

        _xInputService.PushNavigationContext(_navigationOwner);
        SetMenuCursor(0);
    }

    /// <summary>
    /// Deactivates controller navigation of the side menu, e.g. once a page has been chosen
    /// and that page becomes the active navigation context instead.
    /// </summary>
    private void DeactivateControllerMenuNavigation()
    {
        if (_xInputService == null || !_xInputService.IsActiveNavigationContext(_navigationOwner))
        {
            return;
        }

        SetMenuCursor(-1);
        _xInputService.PopNavigationContext(_navigationOwner);
    }

    private void SetMenuCursor(int newIndex)
    {
        if (_menuCursorIndex >= 0 && _menuCursorIndex < _menuItems.Count)
        {
            _menuItems[_menuCursorIndex].Classes.Remove("controllerCursor");
        }
        _menuCursorIndex = newIndex;
        if (_menuCursorIndex >= 0 && _menuCursorIndex < _menuItems.Count)
        {
            _menuItems[_menuCursorIndex].Classes.Add("controllerCursor");
        }
    }

    private async void OnControllerNavigationAction(object? sender, ControllerNavigationAction action)
    {
        if (_xInputService == null || !_xInputService.IsActiveNavigationContext(_navigationOwner))
        {
            return;
        }

        switch (action)
        {
            case ControllerNavigationAction.Up:
                Dispatcher.UIThread.Post(() => SetMenuCursor(_menuCursorIndex <= 0 ? _menuItems.Count - 1 : _menuCursorIndex - 1));
                break;
            case ControllerNavigationAction.Down:
                Dispatcher.UIThread.Post(() => SetMenuCursor(_menuCursorIndex < 0 ? 0 : (_menuCursorIndex + 1) % _menuItems.Count));
                break;
            case ControllerNavigationAction.Confirm:
                if (_menuCursorIndex < 0 || _menuCursorIndex >= _menuItems.Count)
                {
                    break;
                }

                FANavigationViewItem selected = _menuItems[_menuCursorIndex];
                await Dispatcher.UIThread.InvokeAsync(async () =>
                {
                    try
                    {
                        DeactivateControllerMenuNavigation();
                        await _navigationService.Navigate(selected, ContentFrame);
                    }
                    catch (Exception ex)
                    {
                        Logger.Error<MainView>("Failed to navigate to page via controller");
                        Logger.LogExceptionDetails<MainView>(ex);
                    }
                });
                break;
            // Back (B) isn't handled here: if the user is already on the menu, B has
            // nowhere further to go back to in this first pass
        }
    }
}