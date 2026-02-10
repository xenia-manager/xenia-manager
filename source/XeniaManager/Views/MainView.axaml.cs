using System;
using Avalonia.Controls;
using FluentAvalonia.UI.Controls;
using Microsoft.Extensions.DependencyInjection;
using XeniaManager.Core.Logging;
using XeniaManager.Services;
using XeniaManager.ViewModels;

namespace XeniaManager.Views;

public partial class MainView : UserControl
{
    // Properties
    private MainViewModel _viewModel { get; set; }
    private NavigationService _navigationService { get; set; }

    // Constructor
    public MainView()
    {
        InitializeComponent();
        _viewModel = App.Services.GetRequiredService<MainViewModel>();
        DataContext = _viewModel;
        
        // Set up the navigation service
        _navigationService = App.Services.GetRequiredService<NavigationService>();
        _navigationService.SetContentFrame(ContentFrame);
        _navigationService.SetNavigationView(NavigationView);
        
        // Navigate to Library (Default Page)
        _ = _navigationService.NavigateToTag("Library");
    }
    
    // Functions
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
        }
    }
}