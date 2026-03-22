using System.Threading.Tasks;
using FluentAvalonia.UI.Controls;
using XeniaManager.Core.Models;
using XeniaManager.Core.Utilities;

namespace XeniaManager.Services;

/// <summary>
/// Provides a service for displaying message dialogs using FluentAvalonia's ContentDialog or TaskDialog,
/// serving as an alternative to traditional MessageBox implementations.
/// </summary>
public interface IMessageBoxService
{
    /// <summary>
    /// Shows an information message dialog.
    /// </summary>
    /// <param name="title">The title of the dialog</param>
    /// <param name="message">The message content</param>
    /// <param name="dialogType">The type of dialog to use (default: ContentDialog)</param>
    /// <returns>A task that completes when the dialog is closed</returns>
    Task ShowInfoAsync(string title, string message, MessageBoxDialogType dialogType = MessageBoxDialogType.ContentDialog);

    /// <summary>
    /// Shows a warning message dialog.
    /// </summary>
    /// <param name="title">The title of the dialog</param>
    /// <param name="message">The message content</param>
    /// <param name="dialogType">The type of dialog to use (default: ContentDialog)</param>
    /// <returns>A task that completes when the dialog is closed</returns>
    Task ShowWarningAsync(string title, string message, MessageBoxDialogType dialogType = MessageBoxDialogType.ContentDialog);

    /// <summary>
    /// Shows an error message dialog.
    /// </summary>
    /// <param name="title">The title of the dialog</param>
    /// <param name="message">The message content</param>
    /// <param name="dialogType">The type of dialog to use (default: ContentDialog)</param>
    /// <returns>A task that completes when the dialog is closed</returns>
    Task ShowErrorAsync(string title, string message, MessageBoxDialogType dialogType = MessageBoxDialogType.ContentDialog);

    /// <summary>
    /// Shows a confirmation dialog with Yes/No buttons.
    /// </summary>
    /// <param name="title">The title of the dialog</param>
    /// <param name="message">The message content</param>
    /// <param name="dialogType">The type of dialog to use (default: ContentDialog)</param>
    /// <returns>True if Yes was clicked, False if Now was clicked</returns>
    Task<bool> ShowConfirmationAsync(string title, string message, MessageBoxDialogType dialogType = MessageBoxDialogType.ContentDialog);

    /// <summary>
    /// Shows a custom message dialog with customizable buttons.
    /// </summary>
    /// <param name="title">The title of the dialog</param>
    /// <param name="message">The message content</param>
    /// <param name="primaryButtonText">Text for the primary button</param>
    /// <param name="secondaryButtonText">Text for the secondary button (optional)</param>
    /// <param name="closeButtonText">Text for the close button (optional, defaults to "Cancel")</param>
    /// <param name="dialogType">The type of dialog to use (default: ContentDialog)</param>
    /// <returns>The ContentDialogResult indicating which button was clicked</returns>
    Task<ContentDialogResult> ShowCustomDialogAsync(string title, string message,
        string primaryButtonText, string? secondaryButtonText = null, string? closeButtonText = null,
        MessageBoxDialogType dialogType = MessageBoxDialogType.ContentDialog);
}

/// <summary>
/// Implementation of the MessageBox service using FluentAvalonia's ContentDialog or TaskDialog.
/// </summary>
public class MessageBoxService : IMessageBoxService
{
    /// <summary>
    /// Shows an information message dialog.
    /// </summary>
    public async Task ShowInfoAsync(string title, string message, MessageBoxDialogType dialogType = MessageBoxDialogType.ContentDialog)
    {
        if (dialogType == MessageBoxDialogType.TaskDialog)
        {
            await ShowTaskDialogInfoAsync(title, message);
        }
        else
        {
            await ShowContentDialogInfoAsync(title, message);
        }
    }

    /// <summary>
    /// Shows a warning message dialog.
    /// </summary>
    public async Task ShowWarningAsync(string title, string message, MessageBoxDialogType dialogType = MessageBoxDialogType.ContentDialog)
    {
        if (dialogType == MessageBoxDialogType.TaskDialog)
        {
            await ShowTaskDialogWarningAsync(title, message);
        }
        else
        {
            await ShowContentDialogWarningAsync(title, message);
        }
    }

    /// <summary>
    /// Shows an error message dialog.
    /// </summary>
    public async Task ShowErrorAsync(string title, string message, MessageBoxDialogType dialogType = MessageBoxDialogType.ContentDialog)
    {
        if (dialogType == MessageBoxDialogType.TaskDialog)
        {
            await ShowTaskDialogErrorAsync(title, message);
        }
        else
        {
            await ShowContentDialogErrorAsync(title, message);
        }
    }

    /// <summary>
    /// Shows a confirmation dialog with Yes/No buttons.
    /// </summary>
    public async Task<bool> ShowConfirmationAsync(string title, string message, MessageBoxDialogType dialogType = MessageBoxDialogType.ContentDialog)
    {
        if (dialogType == MessageBoxDialogType.TaskDialog)
        {
            return await ShowTaskDialogConfirmationAsync(title, message);
        }
        else
        {
            return await ShowContentDialogConfirmationAsync(title, message);
        }
    }

    /// <summary>
    /// Shows a custom message dialog with customizable buttons.
    /// </summary>
    public async Task<ContentDialogResult> ShowCustomDialogAsync(string title, string message,
        string primaryButtonText, string? secondaryButtonText = null, string? closeButtonText = null,
        MessageBoxDialogType dialogType = MessageBoxDialogType.ContentDialog)
    {
        if (dialogType == MessageBoxDialogType.TaskDialog)
        {
            return await ShowTaskDialogCustomAsync(title, message, primaryButtonText, secondaryButtonText, closeButtonText);
        }
        else
        {
            return await ShowContentDialogCustomAsync(title, message, primaryButtonText, secondaryButtonText, closeButtonText);
        }
    }

    /// <summary>
    /// Shows an information message dialog using ContentDialog.
    /// </summary>
    /// <param name="title">The title of the dialog</param>
    /// <param name="message">The message content to display</param>
    /// <returns>A task that completes when the dialog is closed</returns>
    private async Task ShowContentDialogInfoAsync(string title, string message)
    {
        ContentDialog dialog = new ContentDialog
        {
            Title = title,
            Content = message,
            PrimaryButtonText = LocalizationHelper.GetText("MessageBox.Ok"),
            DefaultButton = ContentDialogButton.Primary
        };

        await dialog.ShowAsync();
    }

    /// <summary>
    /// Shows a warning message dialog using ContentDialog.
    /// </summary>
    /// <param name="title">The title of the dialog</param>
    /// <param name="message">The message content to display</param>
    /// <returns>A task that completes when the dialog is closed</returns>
    private async Task ShowContentDialogWarningAsync(string title, string message)
    {
        ContentDialog dialog = new ContentDialog
        {
            Title = title,
            Content = message,
            PrimaryButtonText = LocalizationHelper.GetText("MessageBox.Ok"),
            DefaultButton = ContentDialogButton.Primary
        };

        await dialog.ShowAsync();
    }

    /// <summary>
    /// Shows an error message dialog using ContentDialog.
    /// </summary>
    /// <param name="title">The title of the dialog</param>
    /// <param name="message">The message content to display</param>
    /// <returns>A task that completes when the dialog is closed</returns>
    private async Task ShowContentDialogErrorAsync(string title, string message)
    {
        ContentDialog dialog = new ContentDialog
        {
            Title = title,
            Content = message,
            PrimaryButtonText = LocalizationHelper.GetText("MessageBox.Ok"),
            DefaultButton = ContentDialogButton.Primary
        };

        await dialog.ShowAsync();
    }

    /// <summary>
    /// Shows a confirmation dialog with Yes/No buttons using ContentDialog.
    /// </summary>
    /// <param name="title">The title of the dialog</param>
    /// <param name="message">The message content to display</param>
    /// <returns>True if Yes was clicked, false if No was clicked</returns>
    private async Task<bool> ShowContentDialogConfirmationAsync(string title, string message)
    {
        ContentDialog dialog = new ContentDialog
        {
            Title = title,
            Content = message,
            PrimaryButtonText = LocalizationHelper.GetText("MessageBox.Yes"),
            SecondaryButtonText = LocalizationHelper.GetText("MessageBox.No"),
            DefaultButton = ContentDialogButton.Primary
        };

        ContentDialogResult result = await dialog.ShowAsync();
        return result == ContentDialogResult.Primary;
    }

    /// <summary>
    /// Shows a custom message dialog with customizable buttons using ContentDialog.
    /// </summary>
    /// <param name="title">The title of the dialog</param>
    /// <param name="message">The message content to display</param>
    /// <param name="primaryButtonText">Text for the primary button</param>
    /// <param name="secondaryButtonText">Text for the secondary button (optional)</param>
    /// <param name="closeButtonText">Text for the close button (optional, defaults to "Cancel")</param>
    /// <returns>The ContentDialogResult indicating which button was clicked</returns>
    private async Task<ContentDialogResult> ShowContentDialogCustomAsync(string title, string message,
        string primaryButtonText, string? secondaryButtonText = null, string? closeButtonText = null)
    {
        ContentDialog dialog = new ContentDialog
        {
            Title = title,
            Content = message,
            PrimaryButtonText = primaryButtonText,
            SecondaryButtonText = secondaryButtonText,
            CloseButtonText = closeButtonText,
            DefaultButton = ContentDialogButton.Primary
        };

        // If no secondary or close button is provided, default to just primary
        if (string.IsNullOrEmpty(secondaryButtonText) && string.IsNullOrEmpty(closeButtonText))
        {
            dialog.SecondaryButtonText = null;
            dialog.CloseButtonText = null;
        }
        // If only a secondary button is provided, don't show the close button
        else if (!string.IsNullOrEmpty(secondaryButtonText) && string.IsNullOrEmpty(closeButtonText))
        {
            dialog.CloseButtonText = null;
        }
        // If closeButtonText is null or empty, use default localized Cancel text
        else if (string.IsNullOrEmpty(closeButtonText))
        {
            dialog.CloseButtonText = LocalizationHelper.GetText("MessageBox.Cancel");
        }

        return await dialog.ShowAsync();
    }

    /// <summary>
    /// Shows an information message dialog using TaskDialog.
    /// </summary>
    /// <param name="title">The title of the dialog</param>
    /// <param name="message">The message content to display</param>
    /// <returns>A task that completes when the dialog is closed</returns>
    private async Task ShowTaskDialogInfoAsync(string title, string message)
    {
        TaskDialog dialog = new TaskDialog
        {
            Title = title,
            Content = message,
            XamlRoot = App.MainWindow
        };

        TaskDialogButton okButton = new TaskDialogButton
        {
            Text = LocalizationHelper.GetText("MessageBox.Ok"),
            DialogResult = "OK"
        };

        dialog.Buttons.Add(okButton);
        await dialog.ShowAsync();
    }

    /// <summary>
    /// Shows a warning message dialog using TaskDialog.
    /// </summary>
    /// <param name="title">The title of the dialog</param>
    /// <param name="message">The message content to display</param>
    /// <returns>A task that completes when the dialog is closed</returns>
    private async Task ShowTaskDialogWarningAsync(string title, string message)
    {
        TaskDialog dialog = new TaskDialog
        {
            Title = title,
            Content = message,
            XamlRoot = App.MainWindow
        };

        TaskDialogButton okButton = new TaskDialogButton
        {
            Text = LocalizationHelper.GetText("MessageBox.Ok"),
            DialogResult = "OK"
        };

        dialog.Buttons.Add(okButton);
        await dialog.ShowAsync();
    }

    /// <summary>
    /// Shows an error message dialog using TaskDialog.
    /// </summary>
    /// <param name="title">The title of the dialog</param>
    /// <param name="message">The message content to display</param>
    /// <returns>A task that completes when the dialog is closed</returns>
    private async Task ShowTaskDialogErrorAsync(string title, string message)
    {
        TaskDialog dialog = new TaskDialog
        {
            Title = title,
            Content = message,
            XamlRoot = App.MainWindow
        };

        TaskDialogButton okButton = new TaskDialogButton
        {
            Text = LocalizationHelper.GetText("MessageBox.Ok"),
            DialogResult = "OK"
        };

        dialog.Buttons.Add(okButton);
        await dialog.ShowAsync();
    }

    /// <summary>
    /// Shows a confirmation dialog with Yes/No buttons using TaskDialog.
    /// </summary>
    /// <param name="title">The title of the dialog</param>
    /// <param name="message">The message content to display</param>
    /// <returns>True if Yes was clicked, false if No was clicked</returns>
    private async Task<bool> ShowTaskDialogConfirmationAsync(string title, string message)
    {
        TaskDialog dialog = new TaskDialog
        {
            Title = title,
            Content = message,
            XamlRoot = App.MainWindow
        };

        TaskDialogButton yesButton = new TaskDialogButton
        {
            Text = LocalizationHelper.GetText("MessageBox.Yes"),
            DialogResult = "Yes"
        };

        TaskDialogButton noButton = new TaskDialogButton
        {
            Text = LocalizationHelper.GetText("MessageBox.No"),
            DialogResult = "No"
        };

        dialog.Buttons.Add(yesButton);
        dialog.Buttons.Add(noButton);

        object? result = await dialog.ShowAsync();
        return ReferenceEquals(result, "Yes");
    }

    /// <summary>
    /// Shows a custom message dialog with customizable buttons using TaskDialog.
    /// </summary>
    /// <param name="title">The title of the dialog</param>
    /// <param name="message">The message content to display</param>
    /// <param name="primaryButtonText">Text for the primary button</param>
    /// <param name="secondaryButtonText">Text for the secondary button (optional)</param>
    /// <param name="closeButtonText">Text for the close button (optional, defaults to "Cancel")</param>
    /// <returns>The ContentDialogResult indicating which button was clicked</returns>
    private async Task<ContentDialogResult> ShowTaskDialogCustomAsync(string title, string message,
        string primaryButtonText, string? secondaryButtonText = null, string? closeButtonText = null)
    {
        TaskDialog dialog = new TaskDialog
        {
            Title = title,
            Content = message,
            XamlRoot = App.MainWindow
        };

        TaskDialogButton primaryButton = new TaskDialogButton
        {
            Text = primaryButtonText,
            DialogResult = "Primary"
        };

        dialog.Buttons.Add(primaryButton);

        if (!string.IsNullOrEmpty(secondaryButtonText))
        {
            TaskDialogButton secondaryButton = new TaskDialogButton
            {
                Text = secondaryButtonText,
                DialogResult = "Secondary"
            };
            dialog.Buttons.Add(secondaryButton);
        }

        if (!string.IsNullOrEmpty(closeButtonText))
        {
            TaskDialogButton closeButton = new TaskDialogButton
            {
                Text = closeButtonText,
                DialogResult = "Close"
            };
            dialog.Buttons.Add(closeButton);
        }
        else if (string.IsNullOrEmpty(secondaryButtonText))
        {
            // If no secondary or close button, don't add any
        }
        else
        {
            // If only a secondary button, add a Cancel button
            TaskDialogButton cancelButton = new TaskDialogButton
            {
                Text = LocalizationHelper.GetText("MessageBox.Cancel"),
                DialogResult = "Cancel"
            };
            dialog.Buttons.Add(cancelButton);
        }

        object? result = await dialog.ShowAsync();

        if (ReferenceEquals(result, "Primary"))
        {
            return ContentDialogResult.Primary;
        }
        else if (ReferenceEquals(result, "Secondary"))
        {
            return ContentDialogResult.Secondary;
        }
        else
        {
            return ContentDialogResult.None;
        }
    }
}