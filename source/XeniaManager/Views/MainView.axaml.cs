using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using FluentAvalonia.UI.Controls;
using FluentIcons.Avalonia.Fluent;
using FluentIcons.Common;
using Microsoft.Extensions.DependencyInjection;
using XeniaManager.Core.Logging;
using XeniaManager.Core.Manage;
using XeniaManager.Core.Models.Game;
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

        RefreshGroupNavigationItems();
        EventManager.Instance.GameGroupsChanged += OnGameGroupsChanged;

        // Navigate to Library (Default Page)
        _ = _navigationService.NavigateToTag("Library");
    }

    // Functions
    private void OnGameGroupsChanged()
    {
        if (!Dispatcher.UIThread.CheckAccess())
        {
            Dispatcher.UIThread.Post(RefreshGroupNavigationItems);
            return;
        }

        RefreshGroupNavigationItems();
    }

    /// <summary>
    /// Rebuilds nested Library navigation items for each game group plus "New Group".
    /// </summary>
    private void RefreshGroupNavigationItems()
    {
        try
        {
            LibraryNavItem.MenuItems.Clear();

            foreach (GameGroup group in GroupManager.Groups.OrderBy(g => g.Name, StringComparer.CurrentCultureIgnoreCase))
            {
                FANavigationViewItem groupItem = new FANavigationViewItem
                {
                    Content = group.Name,
                    Tag = $"Group:{group.Id}",
                    IconSource = new SymbolIconSource { Symbol = Symbol.Folder }
                };
                LibraryNavItem.MenuItems.Add(groupItem);
            }

            FANavigationViewItem addGroupItem = new FANavigationViewItem
            {
                Content = LocalizationHelper.GetText("MainView.Navigation.AddGroup"),
                Tag = "AddGroup",
                IconSource = new SymbolIconSource { Symbol = Symbol.Add }
            };
            LibraryNavItem.MenuItems.Add(addGroupItem);

            LibraryNavItem.IsExpanded = true;
            Logger.Debug<MainView>($"Refreshed group navigation items ({GroupManager.Groups.Count} groups)");
        }
        catch (Exception ex)
        {
            Logger.Error<MainView>("Failed to refresh group navigation items");
            Logger.LogExceptionDetails<MainView>(ex);
        }
    }

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
            .SelectMany(FlattenNavigationItems)
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

    private static IEnumerable<FANavigationViewItem> FlattenNavigationItems(FANavigationViewItem item)
    {
        yield return item;
        foreach (FANavigationViewItem child in item.MenuItems.OfType<FANavigationViewItem>())
        {
            foreach (FANavigationViewItem nested in FlattenNavigationItems(child))
            {
                yield return nested;
            }
        }
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
}
