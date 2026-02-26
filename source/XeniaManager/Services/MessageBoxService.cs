using System.Threading.Tasks;
using FluentAvalonia.UI.Controls;
using XeniaManager.Core.Utilities;

namespace XeniaManager.Services;

/// <summary>
/// Provides a service for displaying message dialogs using FluentAvalonia's ContentDialog,
/// serving as an alternative to traditional MessageBox implementations in Avalonia.
/// </summary>
public interface IMessageBoxService
{
    /// <summary>
    /// Shows an information message dialog.
    /// </summary>
    /// <param name="title">The title of the dialog</param>
    /// <param name="message">The message content</param>
    /// <returns>A task that completes when the dialog is closed</returns>
    Task ShowInfoAsync(string title, string message);

    /// <summary>
    /// Shows a warning message dialog.
    /// </summary>
    /// <param name="title">The title of the dialog</param>
    /// <param name="message">The message content</param>
    /// <returns>A task that completes when the dialog is closed</returns>
    Task ShowWarningAsync(string title, string message);

    /// <summary>
    /// Shows an error message dialog.
    /// </summary>
    /// <param name="title">The title of the dialog</param>
    /// <param name="message">The message content</param>
    /// <returns>A task that completes when the dialog is closed</returns>
    Task ShowErrorAsync(string title, string message);

    /// <summary>
    /// Shows a confirmation dialog with Yes/No buttons.
    /// </summary>
    /// <param name="title">The title of the dialog</param>
    /// <param name="message">The message content</param>
    /// <returns>True if Yes was clicked, False if No was clicked</returns>
    Task<bool> ShowConfirmationAsync(string title, string message);

    /// <summary>
    /// Shows a custom message dialog with customizable buttons.
    /// </summary>
    /// <param name="title">The title of the dialog</param>
    /// <param name="message">The message content</param>
    /// <param name="primaryButtonText">Text for the primary button</param>
    /// <param name="secondaryButtonText">Text for the secondary button (optional)</param>
    /// <param name="closeButtonText">Text for the close button (optional, defaults to "Cancel")</param>
    /// <returns>The ContentDialogResult indicating which button was clicked</returns>
    Task<ContentDialogResult> ShowCustomDialogAsync(
        string title,
        string message,
        string primaryButtonText,
        string? secondaryButtonText = null,
        string? closeButtonText = null);
}

/// <summary>
/// Implementation of the MessageBox service using FluentAvalonia's ContentDialog.
/// </summary>
public class MessageBoxService : IMessageBoxService
{
    /// <summary>
    /// Shows an information message dialog.
    /// </summary>
    public async Task ShowInfoAsync(string title, string message)
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
    /// Shows a warning message dialog.
    /// </summary>
    public async Task ShowWarningAsync(string title, string message)
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
    /// Shows an error message dialog.
    /// </summary>
    public async Task ShowErrorAsync(string title, string message)
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
    /// Shows a confirmation dialog with Yes/No buttons.
    /// </summary>
    public async Task<bool> ShowConfirmationAsync(string title, string message)
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
    /// Shows a custom message dialog with customizable buttons.
    /// </summary>
    public async Task<ContentDialogResult> ShowCustomDialogAsync(
        string title,
        string message,
        string primaryButtonText,
        string? secondaryButtonText = null,
        string? closeButtonText = null)
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
}