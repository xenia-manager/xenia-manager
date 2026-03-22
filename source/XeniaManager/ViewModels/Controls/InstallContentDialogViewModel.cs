using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using XeniaManager.Core.Files;
using XeniaManager.Core.Logging;
using XeniaManager.Core.Models;
using XeniaManager.Core.Services;
using XeniaManager.Core.Utilities;
using XeniaManager.ViewModels.Items;

namespace XeniaManager.ViewModels.Controls;

/// <summary>
/// ViewModel for the installation content dialog.
/// Manages the collection of available content items and handles installation logic.
/// </summary>
public partial class InstallContentDialogViewModel : ViewModelBase
{
    /// <summary>
    /// The Xenia version to use for installation.
    /// </summary>
    [ObservableProperty] private XeniaVersion _xeniaVersion = XeniaVersion.Canary;

    /// <summary>
    /// Collection of content items available for installation.
    /// </summary>
    [ObservableProperty] private ObservableCollection<ContentItemViewModel> _contentItems = [];

    /// <summary>
    /// The currently selected content item in the list.
    /// </summary>
    [ObservableProperty] private ContentItemViewModel? _selectedContent;

    partial void OnSelectedContentChanged(ContentItemViewModel? value)
    {
        UpdateCanInstall();
    }

    partial void OnContentItemsChanged(ObservableCollection<ContentItemViewModel> value)
    {
        UpdateCanInstall();
    }

    /// <summary>
    /// Updates the CanInstall property based on whether there are items in the list.
    /// </summary>
    private void UpdateCanInstall()
    {
        CanInstall = ContentItems.Count > 0 && !IsInstalling;
    }

    /// <summary>
    /// Indicates whether an installation is currently in progress.
    /// </summary>
    [ObservableProperty] private bool _isInstalling;

    partial void OnIsInstallingChanged(bool value)
    {
        UpdateCanInstall();
    }

    /// <summary>
    /// Indicates whether the Installation button should be enabled.
    /// </summary>
    [ObservableProperty] private bool _canInstall;

    /// <summary>
    /// Initializes a new instance of the <see cref="InstallContentDialogViewModel"/> class.
    /// </summary>
    public InstallContentDialogViewModel()
    {
        ContentItems = [];
        ContentItems.CollectionChanged += (_, _) => UpdateCanInstall();
    }

    /// <summary>
    /// Adds new content items to the list (typically from a file picker).
    /// </summary>
    [RelayCommand]
    private async Task AddContent()
    {
        // The dialog will handle the file picker
        // This command is just a placeholder for the UI button
        await Task.CompletedTask;
    }

    /// <summary>
    /// Removes a content item from the list.
    /// </summary>
    /// <param name="contentItem">The content item to remove.</param>
    public void RemoveContent(ContentItemViewModel contentItem)
    {
        if (contentItem != null && ContentItems.Contains(contentItem))
        {
            ContentItems.Remove(contentItem);
            contentItem.StfsFile?.Dispose();

            // If the removed item was selected, clear selection
            if (SelectedContent == contentItem)
            {
                SelectedContent = null;
            }
        }
    }

    /// <summary>
    /// Starts the installation of all content items in the list.
    /// </summary>
    [RelayCommand]
    private async Task InstallAsync()
    {
        if (ContentItems.Count == 0)
        {
            return;
        }

        IsInstalling = true;
        CanInstall = false;

        List<string> installedContent = [];
        List<string> failedContent = [];

        try
        {
            EventManager.Instance.DisableWindow();
            foreach (ContentItemViewModel contentItem in ContentItems)
            {
                StfsFile? stfsFile = contentItem.StfsFile;

                if (stfsFile == null)
                {
                    failedContent.Add($"{contentItem.DisplayName} - No STFS data");
                    continue;
                }

                try
                {
                    // Get the Xenia content folder based on the game's Xenia version
                    // TODO: Add proper management of installing content to either 0 or ProfileXUID
                    string contentFolder = Path.Combine(AppPathResolver.GetFullPath(XeniaVersionInfo.GetXeniaVersionInfo(XeniaVersion).ContentFolderLocation),
                        "0000000000000000");

                    // Extract the STFS file to Xenia's structure
                    stfsFile.ExtractToXeniaStructure(contentFolder);
                    installedContent.Add($"{contentItem.DisplayName} ({contentItem.ContentType})");
                }
                catch (Exception ex)
                {
                    Logger.Error<InstallContentDialogViewModel>($"Failed to install {contentItem.DisplayName}");
                    Logger.LogExceptionDetails<InstallContentDialogViewModel>(ex);
                    failedContent.Add($"{contentItem.DisplayName} - {ex.Message}");
                }
            }
            // Store the results for the messagebox
            InstalledContentList = installedContent;
            FailedContentList = failedContent;
        }
        finally
        {
            IsInstalling = false;
            EventManager.Instance.EnableWindow();
        }
    }

    /// <summary>
    /// List of successfully installed content names.
    /// </summary>
    public List<string> InstalledContentList { get; private set; } = [];

    /// <summary>
    /// List of failed content installation names.
    /// </summary>
    public List<string> FailedContentList { get; private set; } = [];

    /// <summary>
    /// Cancels the current operation and closes the dialog.
    /// </summary>
    [RelayCommand]
    private void Cancel()
    {
        // The dialog will handle closing itself
    }
}