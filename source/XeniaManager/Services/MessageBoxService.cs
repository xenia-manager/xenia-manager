using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Threading;
using FluentAvalonia.UI.Controls;
using XeniaManager.Core.Models;
using XeniaManager.Core.Services;
using XeniaManager.Core.Utilities;

namespace XeniaManager.Services;

/// <summary>
/// Provides a service for displaying message dialogs using FluentAvalonia's FAContentDialog or FATaskDialog,
/// serving as an alternative to traditional MessageBox implementations.
/// </summary>
public interface IMessageBoxService
{
    /// <summary>
    /// Shows an information message dialog.
    /// </summary>
    /// <param name="title">The title of the dialog</param>
    /// <param name="message">The message content</param>
    /// <param name="dialogType">The type of dialog to use (default: FAContentDialog)</param>
    /// <returns>A task that completes when the dialog is closed</returns>
    Task ShowInfoAsync(string title, string message, MessageBoxDialogType dialogType = MessageBoxDialogType.ContentDialog);

    /// <summary>
    /// Shows a warning message dialog.
    /// </summary>
    /// <param name="title">The title of the dialog</param>
    /// <param name="message">The message content</param>
    /// <param name="dialogType">The type of dialog to use (default: FAContentDialog)</param>
    /// <returns>A task that completes when the dialog is closed</returns>
    Task ShowWarningAsync(string title, string message, MessageBoxDialogType dialogType = MessageBoxDialogType.ContentDialog);

    /// <summary>
    /// Shows an error message dialog.
    /// </summary>
    /// <param name="title">The title of the dialog</param>
    /// <param name="message">The message content</param>
    /// <param name="dialogType">The type of dialog to use (default: FAContentDialog)</param>
    /// <returns>A task that completes when the dialog is closed</returns>
    Task ShowErrorAsync(string title, string message, MessageBoxDialogType dialogType = MessageBoxDialogType.ContentDialog);

    /// <summary>
    /// Shows a confirmation dialog with Yes/No buttons.
    /// </summary>
    /// <param name="title">The title of the dialog</param>
    /// <param name="message">The message content</param>
    /// <param name="dialogType">The type of dialog to use (default: FAContentDialog)</param>
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
    /// <param name="dialogType">The type of dialog to use (default: FAContentDialog)</param>
    /// <returns>The FAContentDialogResult indicating which button was clicked</returns>
    Task<FAContentDialogResult> ShowCustomDialogAsync(string title, string message,
        string primaryButtonText, string? secondaryButtonText = null, string? closeButtonText = null,
        MessageBoxDialogType dialogType = MessageBoxDialogType.ContentDialog);
}

/// <summary>
/// Implementation of the MessageBox service using FluentAvalonia's FAContentDialog or FATaskDialog.
/// </summary>
public class MessageBoxService : IMessageBoxService
{
    private readonly GamepadService _gamepadService;

    public MessageBoxService(GamepadService gamepadService)
    {
        _gamepadService = gamepadService;
    }

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
    public async Task<FAContentDialogResult> ShowCustomDialogAsync(string title, string message,
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

    // --- Experimental controller navigation for dialogs -------------------------------
    //
    // Every FAContentDialog/FATaskDialog shown by this service used to be unreachable by
    // controller: GamepadService's navigation context stack only knew about whatever page
    // opened the dialog, which kept reacting to D-Pad/stick input underneath the (visually
    // blocking) dialog, while the dialog's own Ok/Yes/No/etc. buttons couldn't be activated
    // at all. DiscSelectionDialog hit and fixed the same problem for its own dialog earlier;
    // this generalizes that same approach (push a navigation context for the dialog's
    // lifetime, manual cursor via a "▸ " text prefix on whichever button is highlighted,
    // rather than relying on FluentAvalonia's internal focus API which isn't confirmed to be
    // reliably reachable from application code) to every dialog this service shows.

    /// <summary>
    /// Wires up controller navigation for a dialog for as long as it's open: Up/Down/Left/
    /// Right move a "▸ " text-prefix cursor between <paramref name="buttons"/>, Confirm
    /// invokes <paramref name="onConfirm"/> for the highlighted one, Back invokes
    /// <paramref name="onBack"/>. Returns a cleanup action that must be called (in a
    /// <c>finally</c>, once the dialog's ShowAsync has completed) to restore each button's
    /// original text and unwind the navigation context.
    /// </summary>
    private Action AttachGamepadNavigation(
        IReadOnlyList<(Func<string?> GetText, Action<string?> SetText)> buttons,
        int defaultIndex,
        Action<int> onConfirm,
        Action onBack)
    {
        object owner = new object();
        int cursorIndex = -1;

        void SetCursor(int newIndex)
        {
            if (cursorIndex >= 0 && cursorIndex < buttons.Count)
            {
                buttons[cursorIndex].SetText(StripCursorPrefix(buttons[cursorIndex].GetText()));
            }

            cursorIndex = newIndex;

            if (cursorIndex >= 0 && cursorIndex < buttons.Count)
            {
                buttons[cursorIndex].SetText("▸ " + StripCursorPrefix(buttons[cursorIndex].GetText()));
            }
        }

        EventHandler<ControllerNavigationAction>? handler = null;
        handler = (_, action) =>
        {
            Dispatcher.UIThread.Post(() =>
            {
                if (!_gamepadService.IsActiveNavigationContext(owner) || buttons.Count == 0)
                {
                    return;
                }

                switch (action)
                {
                    case ControllerNavigationAction.Up:
                    case ControllerNavigationAction.Left:
                        SetCursor(cursorIndex <= 0 ? buttons.Count - 1 : cursorIndex - 1);
                        break;
                    case ControllerNavigationAction.Down:
                    case ControllerNavigationAction.Right:
                        SetCursor(cursorIndex < 0 ? 0 : (cursorIndex + 1) % buttons.Count);
                        break;
                    case ControllerNavigationAction.Confirm:
                        if (cursorIndex >= 0 && cursorIndex < buttons.Count)
                        {
                            onConfirm(cursorIndex);
                        }
                        break;
                    case ControllerNavigationAction.Back:
                        onBack();
                        break;
                }
            });
        };

        _gamepadService.PushNavigationContext(owner);
        _gamepadService.NavigationActionTriggered += handler;
        if (buttons.Count > 0)
        {
            Dispatcher.UIThread.Post(() => SetCursor(defaultIndex));
        }

        return () =>
        {
            foreach ((Func<string?> getText, Action<string?> setText) in buttons)
            {
                setText(StripCursorPrefix(getText()));
            }

            _gamepadService.NavigationActionTriggered -= handler;
            _gamepadService.PopNavigationContext(owner);
        };
    }

    private static string StripCursorPrefix(string? text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        return text.StartsWith("▸ ") ? text[2..] : text;
    }

    /// <summary>
    /// Builds the button list for <see cref="AttachGamepadNavigation"/> from an FAContentDialog's
    /// Primary/Secondary/Close buttons (whichever have text set), and shows it. Confirm invokes
    /// the highlighted button's result; Back always closes with <see cref="FAContentDialogResult.None"/>.
    /// </summary>
    private async Task<FAContentDialogResult> ShowContentDialogAsync(FAContentDialog dialog)
    {
        List<(FAContentDialogResult Result, Func<string?> GetText, Action<string?> SetText)> entries = [];
        if (!string.IsNullOrEmpty(dialog.PrimaryButtonText))
        {
            entries.Add((FAContentDialogResult.Primary, () => dialog.PrimaryButtonText, t => dialog.PrimaryButtonText = t));
        }
        if (!string.IsNullOrEmpty(dialog.SecondaryButtonText))
        {
            entries.Add((FAContentDialogResult.Secondary, () => dialog.SecondaryButtonText, t => dialog.SecondaryButtonText = t));
        }
        if (!string.IsNullOrEmpty(dialog.CloseButtonText))
        {
            entries.Add((FAContentDialogResult.None, () => dialog.CloseButtonText, t => dialog.CloseButtonText = t));
        }

        int defaultIndex = Math.Max(0, entries.FindIndex(e => e.Result == dialog.DefaultButton switch
        {
            FAContentDialogButton.Primary => FAContentDialogResult.Primary,
            FAContentDialogButton.Secondary => FAContentDialogResult.Secondary,
            _ => FAContentDialogResult.None
        }));

        List<(Func<string?> GetText, Action<string?> SetText)> buttons = entries.ConvertAll(e => (e.GetText, e.SetText));

        Action detach = AttachGamepadNavigation(
            buttons,
            defaultIndex,
            confirmedIndex => dialog.Hide(entries[confirmedIndex].Result),
            () => dialog.Hide(FAContentDialogResult.None));

        try
        {
            return await dialog.ShowAsync();
        }
        finally
        {
            detach();
        }
    }

    /// <summary>
    /// Same idea as <see cref="ShowContentDialogAsync"/>, for FATaskDialog: builds the button
    /// list from its (already-added) <see cref="FATaskDialogButton"/>s. Confirm hides the
    /// dialog with the highlighted button's <see cref="FATaskDialogButton.DialogResult"/>
    /// (matching what clicking it does); Back hides with a null result, same as
    /// DiscSelectionDialog's cancel behavior.
    /// </summary>
    private async Task<object?> ShowTaskDialogAsync(FATaskDialog dialog)
    {
        IList<FATaskDialogButton> taskButtons = dialog.Buttons;
        List<(Func<string?> GetText, Action<string?> SetText)> buttons = taskButtons
            .Select(b => ((Func<string?>)(() => b.Text), (Action<string?>)(t => b.Text = t)))
            .ToList();

        Action detach = AttachGamepadNavigation(
            buttons,
            0,
            confirmedIndex => dialog.Hide(taskButtons[confirmedIndex].DialogResult),
            () => dialog.Hide(null));

        try
        {
            return await dialog.ShowAsync(true);
        }
        finally
        {
            detach();
        }
    }

    /// <summary>
    /// Shows an information message dialog using FAContentDialog.
    /// </summary>
    /// <param name="title">The title of the dialog</param>
    /// <param name="message">The message content to display</param>
    /// <returns>A task that completes when the dialog is closed</returns>
    private async Task ShowContentDialogInfoAsync(string title, string message)
    {
        FAContentDialog dialog = new FAContentDialog
        {
            Title = title,
            Content = message,
            PrimaryButtonText = LocalizationHelper.GetText("MessageBox.Ok"),
            DefaultButton = FAContentDialogButton.Primary
        };

        await ShowContentDialogAsync(dialog);
    }

    /// <summary>
    /// Shows a warning message dialog using FAContentDialog.
    /// </summary>
    /// <param name="title">The title of the dialog</param>
    /// <param name="message">The message content to display</param>
    /// <returns>A task that completes when the dialog is closed</returns>
    private async Task ShowContentDialogWarningAsync(string title, string message)
    {
        FAContentDialog dialog = new FAContentDialog
        {
            Title = title,
            Content = message,
            PrimaryButtonText = LocalizationHelper.GetText("MessageBox.Ok"),
            DefaultButton = FAContentDialogButton.Primary
        };

        await ShowContentDialogAsync(dialog);
    }

    /// <summary>
    /// Shows an error message dialog using FAContentDialog.
    /// </summary>
    /// <param name="title">The title of the dialog</param>
    /// <param name="message">The message content to display</param>
    /// <returns>A task that completes when the dialog is closed</returns>
    private async Task ShowContentDialogErrorAsync(string title, string message)
    {
        FAContentDialog dialog = new FAContentDialog
        {
            Title = title,
            Content = message,
            PrimaryButtonText = LocalizationHelper.GetText("MessageBox.Ok"),
            DefaultButton = FAContentDialogButton.Primary
        };

        await ShowContentDialogAsync(dialog);
    }

    /// <summary>
    /// Shows a confirmation dialog with Yes/No buttons using FAContentDialog.
    /// </summary>
    /// <param name="title">The title of the dialog</param>
    /// <param name="message">The message content to display</param>
    /// <returns>True if Yes was clicked, false if No was clicked</returns>
    private async Task<bool> ShowContentDialogConfirmationAsync(string title, string message)
    {
        FAContentDialog dialog = new FAContentDialog
        {
            Title = title,
            Content = message,
            PrimaryButtonText = LocalizationHelper.GetText("MessageBox.Yes"),
            SecondaryButtonText = LocalizationHelper.GetText("MessageBox.No"),
            DefaultButton = FAContentDialogButton.Primary
        };

        FAContentDialogResult result = await ShowContentDialogAsync(dialog);
        return result == FAContentDialogResult.Primary;
    }

    /// <summary>
    /// Shows a custom message dialog with customizable buttons using FAContentDialog.
    /// </summary>
    /// <param name="title">The title of the dialog</param>
    /// <param name="message">The message content to display</param>
    /// <param name="primaryButtonText">Text for the primary button</param>
    /// <param name="secondaryButtonText">Text for the secondary button (optional)</param>
    /// <param name="closeButtonText">Text for the close button (optional, defaults to "Cancel")</param>
    /// <returns>The FAContentDialogResult indicating which button was clicked</returns>
    private async Task<FAContentDialogResult> ShowContentDialogCustomAsync(string title, string message,
        string primaryButtonText, string? secondaryButtonText = null, string? closeButtonText = null)
    {
        FAContentDialog dialog = new FAContentDialog
        {
            Title = title,
            Content = message,
            PrimaryButtonText = primaryButtonText,
            SecondaryButtonText = secondaryButtonText,
            CloseButtonText = closeButtonText,
            DefaultButton = FAContentDialogButton.Primary
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

        return await ShowContentDialogAsync(dialog);
    }

    /// <summary>
    /// Shows an information message dialog using FATaskDialog.
    /// </summary>
    /// <param name="title">The title of the dialog</param>
    /// <param name="message">The message content to display</param>
    /// <returns>A task that completes when the dialog is closed</returns>
    private async Task ShowTaskDialogInfoAsync(string title, string message)
    {
        FATaskDialog dialog = new FATaskDialog
        {
            Title = title,
            Content = message,
            XamlRoot = App.MainWindow
        };

        FATaskDialogButton okButton = new FATaskDialogButton
        {
            Text = LocalizationHelper.GetText("MessageBox.Ok"),
            DialogResult = "OK"
        };

        dialog.Buttons.Add(okButton);
        await ShowTaskDialogAsync(dialog);
    }

    /// <summary>
    /// Shows a warning message dialog using FATaskDialog.
    /// </summary>
    /// <param name="title">The title of the dialog</param>
    /// <param name="message">The message content to display</param>
    /// <returns>A task that completes when the dialog is closed</returns>
    private async Task ShowTaskDialogWarningAsync(string title, string message)
    {
        FATaskDialog dialog = new FATaskDialog
        {
            Title = title,
            Content = message,
            XamlRoot = App.MainWindow
        };

        FATaskDialogButton okButton = new FATaskDialogButton
        {
            Text = LocalizationHelper.GetText("MessageBox.Ok"),
            DialogResult = "OK"
        };

        dialog.Buttons.Add(okButton);
        await ShowTaskDialogAsync(dialog);
    }

    /// <summary>
    /// Shows an error message dialog using FATaskDialog.
    /// </summary>
    /// <param name="title">The title of the dialog</param>
    /// <param name="message">The message content to display</param>
    /// <returns>A task that completes when the dialog is closed</returns>
    private async Task ShowTaskDialogErrorAsync(string title, string message)
    {
        FATaskDialog dialog = new FATaskDialog
        {
            Title = title,
            Content = message,
            XamlRoot = App.MainWindow
        };

        FATaskDialogButton okButton = new FATaskDialogButton
        {
            Text = LocalizationHelper.GetText("MessageBox.Ok"),
            DialogResult = "OK"
        };

        dialog.Buttons.Add(okButton);
        await ShowTaskDialogAsync(dialog);
    }

    /// <summary>
    /// Shows a confirmation dialog with Yes/No buttons using FATaskDialog.
    /// </summary>
    /// <param name="title">The title of the dialog</param>
    /// <param name="message">The message content to display</param>
    /// <returns>True if Yes was clicked, false if No was clicked</returns>
    private async Task<bool> ShowTaskDialogConfirmationAsync(string title, string message)
    {
        FATaskDialog dialog = new FATaskDialog
        {
            Title = title,
            Content = message,
            XamlRoot = App.MainWindow
        };

        FATaskDialogButton yesButton = new FATaskDialogButton
        {
            Text = LocalizationHelper.GetText("MessageBox.Yes"),
            DialogResult = "Yes"
        };

        FATaskDialogButton noButton = new FATaskDialogButton
        {
            Text = LocalizationHelper.GetText("MessageBox.No"),
            DialogResult = "No"
        };

        dialog.Buttons.Add(yesButton);
        dialog.Buttons.Add(noButton);

        object? result = await ShowTaskDialogAsync(dialog);
        return ReferenceEquals(result, "Yes");
    }

    /// <summary>
    /// Shows a custom message dialog with customizable buttons using FATaskDialog.
    /// </summary>
    /// <param name="title">The title of the dialog</param>
    /// <param name="message">The message content to display</param>
    /// <param name="primaryButtonText">Text for the primary button</param>
    /// <param name="secondaryButtonText">Text for the secondary button (optional)</param>
    /// <param name="closeButtonText">Text for the close button (optional, defaults to "Cancel")</param>
    /// <returns>The FAContentDialogResult indicating which button was clicked</returns>
    private async Task<FAContentDialogResult> ShowTaskDialogCustomAsync(string title, string message,
        string primaryButtonText, string? secondaryButtonText = null, string? closeButtonText = null)
    {
        FATaskDialog dialog = new FATaskDialog
        {
            Title = title,
            Content = message,
            XamlRoot = App.MainWindow
        };

        FATaskDialogButton primaryButton = new FATaskDialogButton
        {
            Text = primaryButtonText,
            DialogResult = "Primary"
        };

        dialog.Buttons.Add(primaryButton);

        if (!string.IsNullOrEmpty(secondaryButtonText))
        {
            FATaskDialogButton secondaryButton = new FATaskDialogButton
            {
                Text = secondaryButtonText,
                DialogResult = "Secondary"
            };
            dialog.Buttons.Add(secondaryButton);
        }

        if (!string.IsNullOrEmpty(closeButtonText))
        {
            FATaskDialogButton closeButton = new FATaskDialogButton
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
            FATaskDialogButton cancelButton = new FATaskDialogButton
            {
                Text = LocalizationHelper.GetText("MessageBox.Cancel"),
                DialogResult = "Cancel"
            };
            dialog.Buttons.Add(cancelButton);
        }

        object? result = await ShowTaskDialogAsync(dialog);

        if (ReferenceEquals(result, "Primary"))
        {
            return FAContentDialogResult.Primary;
        }
        else if (ReferenceEquals(result, "Secondary"))
        {
            return FAContentDialogResult.Secondary;
        }
        else
        {
            return FAContentDialogResult.None;
        }
    }
}
