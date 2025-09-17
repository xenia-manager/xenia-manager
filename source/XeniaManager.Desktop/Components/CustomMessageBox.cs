// Imported
using Wpf.Ui.Controls;
using XeniaManager.Desktop.Utilities;
namespace XeniaManager.Desktop.Components;

/// <summary>
/// Customized MessageBox
/// </summary>
public static class CustomMessageBox
{
    // Variables
    private static MessageBox _messageBox { get; set; }

    // Async Functions
    /// <summary>
    /// Displays a message box with the specified title and message.
    /// </summary>
    /// <param name="title">The title of the message box.</param>
    /// <param name="message">The content of the message.</param>
    public static async Task ShowAsync(string title, string message)
    {
        _messageBox = new MessageBox
        {
            Title = title,
            Content = message
        };
        await _messageBox.ShowDialogAsync();
    }

    /// <summary>
    /// Displays an error message box for the provided exception.
    /// </summary>
    /// <param name="ex">The exception to display.</param>
    public static async Task ShowAsync(Exception ex) => await ShowAsync(LocalizationHelper.GetUiText("MessageBox_Error"), LocalizationHelper.GetLocalizedErrorMessage(ex));

    /// <summary>
    /// Displays a message box with Yes and No options and returns the result.
    /// </summary>
    /// <param name="title">The title of the message box.</param>
    /// <param name="message">The content of the message.</param>
    /// <returns>MessageBoxResult.Primary if clicked on Yes, MessageBoxResult.None if clicked on No</returns>
    public static async Task<MessageBoxResult> YesNoAsync(string title, string message)
    {
        _messageBox = new MessageBox
        {
            Title = title,
            Content = message.Replace("\\n", Environment.NewLine),
            PrimaryButtonText = "Yes",
            CloseButtonText = "No",
        };
        return await _messageBox.ShowDialogAsync();
    }

    // Synchronous Functions
    /// <summary>
    /// Displays a message box with the specified title and message synchronously.
    /// </summary>
    /// <param name="title">The title of the message box.</param>
    /// <param name="message">The content of the message.</param>
    public static void Show(string title, string message)
    {
        _messageBox = new MessageBox
        {
            Title = title,
            Content = message
        };
        _messageBox.ShowDialogAsync().Wait();
    }

    /// <summary>
    /// Displays an error message box for the provided exception synchronously.
    /// </summary>
    /// <param name="ex">The exception to display.</param>
    public static void Show(Exception ex) => Show(LocalizationHelper.GetUiText("MessageBox_Error"), LocalizationHelper.GetLocalizedErrorMessage(ex));

    /// <summary>
    /// Displays a message box with Yes and No options and returns the result synchronously.
    /// </summary>
    /// <param name="title">The title of the message box.</param>
    /// <param name="message">The content of the message.</param>
    /// <returns>MessageBoxResult.Primary if clicked on Yes, MessageBoxResult.None if clicked on No</returns>
    public static MessageBoxResult YesNo(string title, string message)
    {
        _messageBox = new MessageBox
        {
            Title = title,
            Content = message.Replace("\\n", Environment.NewLine),
            PrimaryButtonText = "Yes",
            CloseButtonText = "No",
        };
        return _messageBox.ShowDialogAsync().Result;
    }
}