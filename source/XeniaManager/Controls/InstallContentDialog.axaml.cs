using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using FluentAvalonia.Core;
using FluentAvalonia.UI.Controls;
using Microsoft.Extensions.DependencyInjection;
using XeniaManager.Core.Files;
using XeniaManager.Core.Logging;
using XeniaManager.Core.Models;
using XeniaManager.Core.Models.Files.Stfs;
using XeniaManager.Core.Utilities;
using XeniaManager.Services;
using XeniaManager.ViewModels.Controls;
using XeniaManager.ViewModels.Items;

namespace XeniaManager.Controls;

/// <summary>
/// Represents a dialog that allows users to select and install content (DLC, updates, etc.) for a game.
/// This control provides a list of available content items with progress tracking and modern Fluent Design UI.
/// </summary>
public partial class InstallContentDialog : UserControl
{
    /// <summary>
    /// The ViewModel containing the dialog's data and logic.
    /// </summary>
    private readonly InstallContentDialogViewModel _viewModel;

    /// <summary>
    /// Initializes a new instance of the <see cref="InstallContentDialog"/> class.
    /// This constructor is required for the XAML loader.
    /// </summary>
    public InstallContentDialog()
    {
        InitializeComponent();
        _viewModel = new InstallContentDialogViewModel();
        DataContext = _viewModel;
    }

    /// <summary>
    /// Shows a TaskDialog to allow the user to select and install content for a game.
    /// </summary>
    /// <param name="xeniaVersion">The Xenia version to use for installation.</param>
    /// <returns>True if the user installed content, false if the user canceled the dialog.</returns>
    public static async Task ShowAsync(XeniaVersion xeniaVersion = XeniaVersion.Canary)
    {
        InstallContentDialog dialog = new InstallContentDialog()
        {
            _viewModel =
            {
                XeniaVersion = xeniaVersion
            }
        };

        TaskDialog taskDialog = new TaskDialog
        {
            Title = LocalizationHelper.GetText("InstallContentDialog.ContentDialog.Title"),
            Content = dialog,
            ShowProgressBar = false,
            XamlRoot = App.MainWindow
        };

        // Add Install and Cancel buttons
        TaskDialogButton installButton = new TaskDialogButton
        {
            Text = LocalizationHelper.GetText("InstallContentDialog.ContentDialog.InstallButton.Text"),
            IsEnabled = false,
            DialogResult = "InstallContent"
        };

        TaskDialogButton cancelButton = new TaskDialogButton
        {
            Text = LocalizationHelper.GetText("InstallContentDialog.ContentDialog.CancelButton.Text"),
            DialogResult = TaskDialogStandardResult.Cancel
        };

        taskDialog.Buttons.Add(installButton);
        taskDialog.Buttons.Add(cancelButton);

        // Bind button states to ViewModel
        dialog._viewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(InstallContentDialogViewModel.CanInstall))
            {
                installButton.IsEnabled = dialog._viewModel.CanInstall;
            }
        };

        // Use the closing event to handle installation with deferral
        taskDialog.Closing += async (s, e) =>
        {
            // Only use deferral if the Installation button was clicked
            if (ReferenceEquals(e.Result, "InstallContent"))
            {
                // Cancel the default close behavior
                e.Cancel = true;

                // Get a deferral to keep the dialog open during installation
                Deferral? deferral = e.GetDeferral();

                try
                {
                    // Start the installation
                    await dialog._viewModel.InstallCommand.ExecuteAsync(null);

                    // Wait for installation to complete
                    while (dialog._viewModel.IsInstalling)
                    {
                        await Task.Delay(100);
                    }

                    // Show the messagebox with installation results
                    await ShowInstallationResults(dialog._viewModel);
                }
                catch (Exception ex)
                {
                    Logger.Error<InstallContentDialog>("Installation failed");
                    Logger.LogExceptionDetails<InstallContentDialog>(ex);
                }
                finally
                {
                    // Complete the deferral to allow the dialog to close
                    deferral.Complete();
                    taskDialog.Hide(TaskDialogStandardResult.OK);
                }
            }
        };

        // Handle Cancel button click
        cancelButton.Click += (s, e) =>
        {
            taskDialog.Hide(TaskDialogStandardResult.Cancel);
        };

        // Handle Add Content button click
        dialog.AddContentButton.Click += async (s, e) =>
        {
            await dialog.OpenFilePicker();
        };

        try
        {
            await taskDialog.ShowAsync();
        }
        catch (Exception ex)
        {
            Logger.Error<InstallContentDialog>("Error showing install content dialog");
            Logger.LogExceptionDetails<InstallContentDialog>(ex);
        }
    }

    /// <summary>
    /// Opens a file picker to select STFS content files.
    /// </summary>
    public async Task OpenFilePicker()
    {
        IStorageProvider? storageProvider = App.MainWindow?.StorageProvider;
        if (storageProvider == null)
        {
            Logger.Error<InstallContentDialog>("Storage provider is not available");
            return;
        }

        FilePickerOpenOptions options = new FilePickerOpenOptions
        {
            Title = LocalizationHelper.GetText("InstallContentDialog.FilePicker.Title"),
            AllowMultiple = true,
            FileTypeFilter = new List<FilePickerFileType>
            {
                new FilePickerFileType("STFS Content Files")
                {
                    Patterns = ["*"]
                }
            }
        };

        try
        {
            IReadOnlyList<IStorageFile> files = await storageProvider.OpenFilePickerAsync(options);

            foreach (IStorageFile file in files)
            {
                try
                {
                    // Get the file path
                    string filePath = file.TryGetLocalPath() ?? file.Path.ToString();

                    // Parse the STFS file
                    StfsFile stfsFile = StfsFile.Load(filePath);

                    if (stfsFile.Metadata.ContentType != ContentType.GameOnDemand)
                    {
                        // Create a content item and add to the list
                        ContentItemViewModel contentItem = new ContentItemViewModel(stfsFile, _viewModel.RemoveContent);
                        _viewModel.ContentItems.Add(contentItem);

                        Logger.Info<InstallContentDialog>($"Added STFS content: {stfsFile.Metadata.DisplayName}");
                    }
                    else
                    {
                        Logger.Warning<InstallContentDialog>("Selected file is currently not supported for installing");
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error<InstallContentDialog>($"Failed to load STFS file: {file.Name}");
                    Logger.LogExceptionDetails<InstallContentDialog>(ex);
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Error<InstallContentDialog>("Failed to open file picker");
            Logger.LogExceptionDetails<InstallContentDialog>(ex);
        }
    }

    /// <summary>
    /// Shows a messagebox with the installation results.
    /// </summary>
    private static async Task ShowInstallationResults(InstallContentDialogViewModel viewModel)
    {
        List<string> installedList = viewModel.InstalledContentList;
        List<string> failedList = viewModel.FailedContentList;

        string message;
        string title;

        if (installedList.Count > 0 && failedList.Count == 0)
        {
            // All successful
            title = LocalizationHelper.GetText("InstallContentDialog.Results.Success.Title");
            message = LocalizationHelper.GetText("InstallContentDialog.Results.Success.Message") + "\n\n" +
                      string.Join("\n", installedList);
        }
        else if (installedList.Count > 0 && failedList.Count > 0)
        {
            // Partial success
            title = LocalizationHelper.GetText("InstallContentDialog.Results.Partial.Title");
            message = LocalizationHelper.GetText("InstallContentDialog.Results.Partial.Message") + "\n\n" +
                      LocalizationHelper.GetText("InstallContentDialog.Results.Successful") + ":\n" +
                      string.Join("\n", installedList) + "\n\n" +
                      LocalizationHelper.GetText("InstallContentDialog.Results.Failed") + ":\n" +
                      string.Join("\n", failedList);
        }
        else
        {
            // All failed
            title = LocalizationHelper.GetText("InstallContentDialog.Results.Failed.Title");
            message = LocalizationHelper.GetText("InstallContentDialog.Results.Failed.Message") + "\n\n" +
                      string.Join("\n", failedList);
        }

        IMessageBoxService messageBox = App.Services.GetRequiredService<IMessageBoxService>();
        await messageBox.ShowInfoAsync(title, message);
    }
}