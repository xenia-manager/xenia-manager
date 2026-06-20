using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using FluentAvalonia.UI.Controls;
using Microsoft.Extensions.DependencyInjection;
using XeniaManager.Core.Logging;
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

        // Auto-fit pane width to content when opening
        NavigationView.PaneOpening += NavigationView_OnPaneOpening;

        // Navigate to Library (Default Page)
        _ = _navigationService.NavigateToTag("Library");
    }

    // Functions
    private void NavigationView_OnPaneOpening(NavigationView sender, EventArgs args)
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
        IEnumerable<NavigationViewItem> items = NavigationView.MenuItems
            .Concat(NavigationView.FooterMenuItems)
            .OfType<NavigationViewItem>()
            .Where(i => i.Content is string text && !string.IsNullOrEmpty(text));

        double maxTextWidth = 0;
        double fontSize = NavigationView.FontSize > 0 ? NavigationView.FontSize : 14;
        Typeface typeface = new Typeface(NavigationView.FontFamily ?? FontFamily.Default);

        foreach (NavigationViewItem item in items)
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

    private async void NavigationView_OnItemInvoked(object? sender, NavigationViewItemInvokedEventArgs e)
    {
        try
        {
            if (e.InvokedItemContainer is NavigationViewItem selectedItem)
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
}