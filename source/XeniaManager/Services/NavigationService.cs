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
using XeniaManager.Core.Models.Game;
using XeniaManager.Core.Services;
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
    private FAFrame? _contentFrame;

    /// <summary>
    /// NavigationView used by the NavigationService to show different Avalonia Pages in ContentFrame
    /// </summary>
    private FANavigationView? _navigationView;

    /// <summary>
    /// Tag of the currently shown Avalonia Page
    /// </summary>
    private string? _currentPageTag;

    public string? CurrentPageTag => _currentPageTag;

    /// <summary>
    /// Event signalizing that the service navigated to the selected Avalonia Page
    /// </summary>
    public event EventHandler<string>? Navigated;

    /// <summary>
    /// Service for showing message boxes
    /// </summary>
    private readonly IMessageBoxService _messageBoxService = App.Services.GetRequiredService<IMessageBoxService>();

    // Functions
    /// <summary>
    /// Sets the ContentFrame used by the NavigationService
    /// </summary>
    /// <param name="frame">Frame the NavigationService will use</param>
    public void SetContentFrame(FAFrame frame)
    {
        Logger.Debug<NavigationService>($"Setting content frame");
        _contentFrame = frame;
        Logger.Debug<NavigationService>($"Content frame set");
    }

    /// <summary>
    /// Sets the NavigationView used by the NavigationService
    /// </summary>
    /// <param name="navigationView">NavigationView the NavigationService will use</param>
    public void SetNavigationView(FANavigationView navigationView)
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
    public async Task Navigate(FANavigationViewItem navigationViewItem, FAFrame? contentFrame = null)
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
    public async Task NavigateToTag(string tag, FAFrame? contentFrame = null)
    {
        FAFrame? frame = contentFrame ?? _contentFrame;

        if (frame == null)
        {
            Logger.Error<NavigationService>("Cannot navigate because we're missing the ContentFrame");
            // TODO: Custom Exception
            throw new Exception($"Cannot navigate to {tag} because we're missing the ContentFrame");
        }

        Logger.Info<NavigationService>($"Starting navigation to tag: {tag}");

        _currentPageTag = tag;
        Settings settings = App.Services.GetRequiredService<Settings>();
        List<XeniaVersion> installedVersions = settings.GetInstalledVersions(settings);

        switch (tag)
        {
            case "Open":
                Logger.Info<NavigationService>("Processing 'Open' tag - attempting to launch Xenia");
                try
                {
                    Logger.Info<NavigationService>($"Found {installedVersions.Count} installed Xenia versions: [{string.Join(", ", installedVersions)}]");

                    switch (installedVersions.Count)
                    {
                        case 0:
                            Logger.Error<NavigationService>("No Xenia installations found");
                            await _messageBoxService.ShowWarningAsync(
                                LocalizationHelper.GetText("NavigationService.NoXeniaInstalled.Title"),
                                LocalizationHelper.GetText("NavigationService.NoXeniaInstalled.Message"));
                            return;
                        case 1:
                            Logger.Info<NavigationService>($"Only one Xenia version installed: {installedVersions[0]}, launching directly");
                            EventManager.Instance.DisableWindow();
                            await Launcher.LaunchEmulatorAsync(installedVersions[0]);
                            EventManager.Instance.EnableWindow();
                            break;
                        default:
                            Logger.Info<NavigationService>($"Multiple Xenia versions installed ({installedVersions.Count}), showing selection dialog");
                            XeniaVersion? chosen = await XeniaSelectionDialog.ShowAsync(installedVersions);
                            if (chosen is { } version)
                            {
                                // User selected a version – proceed
                                Logger.Info<NavigationService>($"User selected Xenia version: {chosen}, proceeding with launch");
                                EventManager.Instance.DisableWindow();
                                await Launcher.LaunchEmulatorAsync(version);
                                EventManager.Instance.EnableWindow();
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
                    EventManager.Instance.EnableWindow();
                    throw;
                }
                break;
            case "Library":
                Logger.Debug<NavigationService>("Navigating to Library page");
                GroupManager.SetActiveFilter(null);
                frame.Navigate(typeof(LibraryPage), null, new FAEntranceNavigationTransitionInfo());
                break;
            case "AddGroup":
                Logger.Info<NavigationService>("Processing 'AddGroup' tag");
                await HandleAddGroupAsync(frame);
                return;
            case "XeniaSettings":
                Logger.Debug<NavigationService>("Navigating to Xenia Settings page");
                Logger.Info<NavigationService>($"Found {installedVersions.Count} installed Xenia versions, allowing navigation to Xenia Settings");
                frame.Navigate(typeof(XeniaSettingsPage), null, new FAEntranceNavigationTransitionInfo());
                break;
            case "Manage":
                Logger.Debug<NavigationService>("Navigating to Manage page");
                frame.Navigate(typeof(ManagePage), null, new FAEntranceNavigationTransitionInfo());
                break;
            case "About":
                Logger.Debug<NavigationService>("Navigating to About page");
                frame.Navigate(typeof(AboutPage), null, new FAEntranceNavigationTransitionInfo());
                break;
            case "Settings":
                Logger.Debug<NavigationService>("Navigating to Settings page");
                frame.Navigate(typeof(SettingsPage), null, new FAEntranceNavigationTransitionInfo());
                break;
            default:
                if (tag.StartsWith("Group:", StringComparison.Ordinal))
                {
                    await HandleGroupFilterAsync(tag, frame);
                    break;
                }

                Logger.Warning<NavigationService>($"Unknown navigation tag requested: {tag}");
                break;
        }

        // Update the icon if navigating by tag
        if (_navigationView != null)
        {
            FANavigationViewItem? item = FindNavigationItemByTag(tag);
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
    /// Prompts for a group name, creates the group, then selects it in the library.
    /// </summary>
    private async Task HandleAddGroupAsync(FAFrame frame)
    {
        string? name = await GroupNameDialog.ShowAsync();
        if (string.IsNullOrWhiteSpace(name))
        {
            Logger.Info<NavigationService>("Add group cancelled or empty name");
            // Restore previous library selection visually
            string restoreTag = GroupManager.ActiveFilterGroupId is Guid activeId
                ? $"Group:{activeId}"
                : "Library";
            UpdateSelection(restoreTag);
            return;
        }

        GameGroup group = GroupManager.CreateGroup(name);
        GroupManager.SetActiveFilter(group.Id);
        frame.Navigate(typeof(LibraryPage), null, new FAEntranceNavigationTransitionInfo());
        _currentPageTag = $"Group:{group.Id}";
        UpdateSelection(_currentPageTag);
        SetSelectedIcon(FindNavigationItemByTag("Library"));
        Navigated?.Invoke(this, _currentPageTag);
        Logger.Info<NavigationService>($"Created and selected group '{group.Name}'");
    }

    /// <summary>
    /// Navigates to the library and filters it to the selected group.
    /// </summary>
    private async Task HandleGroupFilterAsync(string tag, FAFrame frame)
    {
        string idText = tag["Group:".Length..];
        if (!Guid.TryParse(idText, out Guid groupId))
        {
            Logger.Warning<NavigationService>($"Invalid group navigation tag: {tag}");
            await _messageBoxService.ShowWarningAsync(
                LocalizationHelper.GetText("MainView.Navigation.Error.Title"),
                $"Invalid group id in tag '{tag}'");
            return;
        }

        if (GroupManager.Groups.All(g => g.Id != groupId))
        {
            Logger.Warning<NavigationService>($"Group not found for tag: {tag}");
            return;
        }

        Logger.Info<NavigationService>($"Filtering library by group {groupId}");
        GroupManager.SetActiveFilter(groupId);
        frame.Navigate(typeof(LibraryPage), null, new FAEntranceNavigationTransitionInfo());
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

        FANavigationViewItem? item = FindNavigationItemByTag(tag);
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
    private void SetSelectedIcon(FANavigationViewItem? selectedItem)
    {
        if (_navigationView == null)
        {
            Logger.Warning<NavigationService>("Cannot set selected icon: NavigationView is null");
            return;
        }

        Logger.Trace<NavigationService>("Updating icon variants for selected item");

        // Reset menu icons
        foreach (FANavigationViewItem item in _navigationView.MenuItems.OfType<FANavigationViewItem>())
        {
            if (item.Content is FluentIcons.Avalonia.Fluent.SymbolIcon icon)
            {
                icon.IconVariant = item == selectedItem ? IconVariant.Filled : IconVariant.Regular;
            }
        }

        // Reset footer icons
        foreach (FANavigationViewItem item in _navigationView.FooterMenuItems.OfType<FANavigationViewItem>())
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
    private FANavigationViewItem? FindNavigationItemByTag(string tag)
    {
        if (_navigationView == null)
        {
            Logger.Trace<NavigationService>("Cannot find item: NavigationView is null");
            return null;
        }

        FANavigationViewItem? menuItem = FindNavigationItemByTagRecursive(
            _navigationView.MenuItems.OfType<FANavigationViewItem>(), tag);

        if (menuItem != null)
        {
            Logger.Trace<NavigationService>($"Found item '{tag}' in menu items");
            return menuItem;
        }

        FANavigationViewItem? footerItem = FindNavigationItemByTagRecursive(
            _navigationView.FooterMenuItems.OfType<FANavigationViewItem>(), tag);
        Logger.Trace<NavigationService>(footerItem != null ? $"Found item '{tag}' in footer items" : $"Item '{tag}' not found in menu or footer");

        return footerItem;
    }

    private static FANavigationViewItem? FindNavigationItemByTagRecursive(
        IEnumerable<FANavigationViewItem> items, string tag)
    {
        foreach (FANavigationViewItem item in items)
        {
            if (item.Tag?.ToString() == tag)
            {
                return item;
            }

            FANavigationViewItem? nested = FindNavigationItemByTagRecursive(
                item.MenuItems.OfType<FANavigationViewItem>(), tag);
            if (nested != null)
            {
                return nested;
            }
        }

        return null;
    }
}