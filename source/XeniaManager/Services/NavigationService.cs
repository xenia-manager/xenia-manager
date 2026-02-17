using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAvalonia.UI.Controls;
using FluentAvalonia.UI.Media.Animation;
using FluentIcons.Common;
using Microsoft.Extensions.DependencyInjection;
using XeniaManager.Controls;
using XeniaManager.Core.Logging;
using XeniaManager.Core.Manage;
using XeniaManager.Core.Models;
using XeniaManager.Core.Settings;
using XeniaManager.Core.Utilities;
using XeniaManager.Views.Pages;

namespace XeniaManager.Services;

public class NavigationService
{
    // Properties
    /// <summary>
    /// Content Frame where all Avalonia Pages are loaded
    /// </summary>
    private Frame? _contentFrame;

    /// <summary>
    /// NavigationView used by the NavigationService to show different Avalonia Pages in ContentFrame
    /// </summary>
    private NavigationView? _navigationView;

    /// <summary>
    /// Tag of the currently shown Avalonia Page
    /// </summary>
    private string? _currentPageTag;

    public string? CurrentPageTag => _currentPageTag;

    /// <summary>
    /// Event signalizing that the service navigated to the selected Avalonia Page
    /// </summary>
    public event EventHandler<string>? Navigated;

    // Functions
    /// <summary>
    /// Sets the ContentFrame used by the NavigationService
    /// </summary>
    /// <param name="frame">Frame the NavigationService will use</param>
    public void SetContentFrame(Frame frame)
    {
        Logger.Debug<NavigationService>($"Setting content frame");
        _contentFrame = frame;
        Logger.Debug<NavigationService>($"Content frame set");
    }

    /// <summary>
    /// Sets the NavigationView used by the NavigationService
    /// </summary>
    /// <param name="navigationView">NavigationView the NavigationService will use</param>
    public void SetNavigationView(NavigationView navigationView)
    {
        Logger.Debug<NavigationService>($"Setting navigation view");
        _navigationView = navigationView;
        Logger.Debug<NavigationService>($"Navigation view set");
    }

    /// <summary>
    /// Extracts tag from the NavigationViewItem and navigates to it
    /// </summary>
    /// <param name="navigationViewItem"></param>
    /// <param name="contentFrame"></param>
    public async Task Navigate(NavigationViewItem navigationViewItem, Frame? contentFrame = null)
    {
        string tag = navigationViewItem.Tag?.ToString() ?? string.Empty;
        Logger.Debug<NavigationService>($"Navigate called to: {tag}");
        await NavigateToTag(tag, contentFrame);
    }

    /// <summary>
    /// Navigate to a specific page using its tag
    /// </summary>
    /// <param name="tag">Tag of the page we're trying to navigate to</param>
    /// <param name="contentFrame">ContentFrame where we want to show the Avalonia Page, optional param and uses _contentFrame if it's null</param>
    public async Task NavigateToTag(string tag, Frame? contentFrame = null)
    {
        Frame? frame = contentFrame ?? _contentFrame;

        if (frame == null)
        {
            Logger.Error<NavigationService>("Cannot navigate because we're missing the ContentFrame");
            // TODO: Custom Exception
            throw new Exception($"Cannot navigate to {tag} because we're missing the ContentFrame");
        }

        Logger.Info<NavigationService>($"Starting navigation to tag: {tag}");

        _currentPageTag = tag;

        switch (tag)
        {
            case "Open":
                Logger.Info<NavigationService>("Processing 'Open' tag - attempting to launch Xenia");
                try
                {
                    Settings settings = App.Services.GetRequiredService<Settings>();
                    List<XeniaVersion> installedVersions = settings.GetInstalledVersions(settings);

                    Logger.Info<NavigationService>($"Found {installedVersions.Count} installed Xenia versions: [{string.Join(", ", installedVersions)}]");

                    switch (installedVersions.Count)
                    {
                        case 0:
                            Logger.Error<NavigationService>("No Xenia installations found");
                            // TODO: Custom Exception
                            throw new Exception("No Xenia installations found");
                        case 1:
                            Logger.Info<NavigationService>($"Only one Xenia version installed: {installedVersions[0]}, launching directly");
                            Launcher.LaunchEmulator(installedVersions[0]);
                            break;
                        default:
                            Logger.Info<NavigationService>($"Multiple Xenia versions installed ({installedVersions.Count}), showing selection dialog");
                            XeniaVersion? chosen = await XeniaSelectionDialog.ShowAsync(installedVersions);
                            if (chosen is { } version)
                            {
                                // User selected a version – proceed
                                Logger.Info<NavigationService>($"User selected Xenia version: {chosen}, proceeding with launch");
                                Launcher.LaunchEmulator(version);
                            }
                            else
                            {
                                //User closed / canceled
                                Logger.Info<NavigationService>("Xenia version selection was cancelled by user");
                            }
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error<NavigationService>($"Error occurred while processing 'Open' tag: {ex.Message}");
                    throw;
                }
                break;
            case "Library":
                Logger.Debug<NavigationService>("Navigating to Library page");
                frame.Navigate(typeof(LibraryPage), null, new EntranceNavigationTransitionInfo());
                break;
            case "XeniaSettings":
                Logger.Debug<NavigationService>("Navigating to Xenia Settings page");
                frame.Navigate(typeof(XeniaSettingsPage), null, new EntranceNavigationTransitionInfo());
                break;
            case "Manage":
                Logger.Debug<NavigationService>("Navigating to Manage page");
                frame.Navigate(typeof(ManagePage), null, new EntranceNavigationTransitionInfo());
                break;
            case "About":
                Logger.Debug<NavigationService>("Navigating to About page");
                frame.Navigate(typeof(AboutPage), null, new EntranceNavigationTransitionInfo());
                break;
            case "Settings":
                Logger.Debug<NavigationService>("Navigating to Settings page");
                frame.Navigate(typeof(SettingsPage), null, new EntranceNavigationTransitionInfo());
                break;
            default:
                Logger.Warning<NavigationService>($"Unknown navigation tag requested: {tag}");
                break;
        }

        // Update the icon if navigating by tag
        if (_navigationView != null)
        {
            NavigationViewItem? item = FindNavigationItemByTag(tag);
            if (item != null)
            {
                Logger.Trace<NavigationService>($"Found navigation item for tag '{tag}', updating icon");
                SetSelectedIcon(item);
            }
            else
            {
                Logger.Warning<NavigationService>($"Could not find navigation item for tag: {tag}");
            }
        }

        UpdateSelection(tag);

        Navigated?.Invoke(this, tag);
        Logger.Info<NavigationService>($"Navigation to tag '{tag}' completed successfully");
    }

    /// <summary>
    /// Updates the selected item in the NavigationView based on the provided tag
    /// </summary>
    /// <param name="tag">Tag of the item to be selected in the NavigationView</param>
    private void UpdateSelection(string tag)
    {
        if (_navigationView == null)
        {
            Logger.Trace<NavigationService>("Cannot update selection: NavigationView is null");
            return;
        }

        NavigationViewItem? item = FindNavigationItemByTag(tag);
        if (item != null)
        {
            _navigationView.SelectedItem = item;
            SetSelectedIcon(item);
            Logger.Trace<NavigationService>($"Selection updated to: {tag}");
        }
        else
        {
            Logger.Warning<NavigationService>($"Cannot find navigation item for tag: {tag}");
        }
    }

    /// <summary>
    /// Updates the icon variant (filled/regular) for navigation items to show which item is currently selected
    /// </summary>
    /// <param name="selectedItem">The NavigationViewItem that is currently selected</param>
    private void SetSelectedIcon(NavigationViewItem? selectedItem)
    {
        if (_navigationView == null)
        {
            Logger.Warning<NavigationService>("Cannot set selected icon: NavigationView is null");
            return;
        }

        Logger.Trace<NavigationService>("Updating icon variants for selected item");

        // Reset menu icons
        foreach (NavigationViewItem item in _navigationView.MenuItems.OfType<NavigationViewItem>())
        {
            if (item.Content is FluentIcons.Avalonia.Fluent.SymbolIcon icon)
            {
                icon.IconVariant = item == selectedItem ? IconVariant.Filled : IconVariant.Regular;
            }
        }

        // Reset footer icons
        foreach (NavigationViewItem item in _navigationView.FooterMenuItems.OfType<NavigationViewItem>())
        {
            if (item.Content is FluentIcons.Avalonia.Fluent.SymbolIcon icon)
            {
                icon.IconVariant = item == selectedItem ? IconVariant.Filled : IconVariant.Regular;
            }
        }
    }

    /// <summary>
    /// Finds a NavigationViewItem by its tag property by searching through both menu items and footer items
    /// </summary>
    /// <param name="tag">The tag to search for in the NavigationView items</param>
    /// <returns>The NavigationViewItem with the matching tag, or null if not found</returns>
    private NavigationViewItem? FindNavigationItemByTag(string tag)
    {
        if (_navigationView == null)
        {
            Logger.Trace<NavigationService>("Cannot find item: NavigationView is null");
            return null;
        }

        // Search in menu items
        NavigationViewItem? menuItem = _navigationView.MenuItems.OfType<NavigationViewItem>().FirstOrDefault(x => x.Tag?.ToString() == tag);

        if (menuItem != null)
        {
            Logger.Trace<NavigationService>($"Found item '{tag}' in menu items");
            return menuItem;
        }

        // Search in footer items
        NavigationViewItem? footerItem = _navigationView.FooterMenuItems.OfType<NavigationViewItem>().FirstOrDefault(x => x.Tag?.ToString() == tag);
        Logger.Trace<NavigationService>(footerItem != null ? $"Found item '{tag}' in footer items" : $"Item '{tag}' not found in menu or footer");

        return footerItem;
    }
}