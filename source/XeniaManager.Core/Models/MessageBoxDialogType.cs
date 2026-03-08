namespace XeniaManager.Core.Models;

/// <summary>
/// Specifies which type of dialog to use for message boxes.
/// </summary>
public enum MessageBoxDialogType
{
    /// <summary>
    /// Uses ContentDialog (default). Best for simple dialogs at the window level.
    /// </summary>
    ContentDialog,

    /// <summary>
    /// Uses TaskDialog. Best for dialogs that need to appear over other TaskDialogs.
    /// </summary>
    TaskDialog
}