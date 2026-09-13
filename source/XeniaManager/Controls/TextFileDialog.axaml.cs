using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using FluentAvalonia.UI.Controls;
using XeniaManager.Core.Utilities;
using XeniaManager.Logging;
using XeniaManager.ViewModels.Controls;

namespace XeniaManager.Controls;

/// <summary>
/// Represents a dialog that shows the decoded text content of a single file read-only.
/// </summary>
public partial class TextFileDialog : UserControl
{
    private readonly TextFileDialogViewModel? _viewModel;

    /// <summary>
    /// Initializes a new instance of the <see cref="TextFileDialog"/> class.
    /// This constructor is required for the AXAML loader.
    /// </summary>
    public TextFileDialog()
    {
        InitializeComponent();
    }

    private TextFileDialog(string fileName, string content, string encodingName)
    {
        InitializeComponent();
        _viewModel = new TextFileDialogViewModel(fileName, content, encodingName);
        DataContext = _viewModel;
    }

    /// <summary>
    /// Shows the text file dialog for the given decoded content.
    /// </summary>
    /// <param name="fileName">The file name shown in the dialog.</param>
    /// <param name="content">The decoded text content.</param>
    /// <param name="encodingName">The detected encoding display name.</param>
    public static async Task ShowAsync(string fileName, string content, string encodingName)
    {
        TextFileDialog dialogContent = new TextFileDialog(fileName, content, encodingName);
        FATaskDialog? taskDialog = null;
        try
        {
            taskDialog = new FATaskDialog
            {
                Title = fileName,
                Content = dialogContent,
                XamlRoot = App.MainWindow
            };

            taskDialog.Resources.Add("TaskDialogMaxWidth", 800.0);

            FATaskDialogButton closeButton = new FATaskDialogButton
            {
                Text = LocalizationHelper.GetText("GameFilesDialog.CloseButton"),
                DialogResult = "Close"
            };
            taskDialog.Buttons.Add(closeButton);

            await taskDialog.ShowAsync();
        }
        catch (Exception ex)
        {
            Logger.Error<TextFileDialog>("Error showing text file dialog");
            Logger.LogExceptionDetails<TextFileDialog>(ex);
        }
        finally
        {
            dialogContent.DataContext = null;
            if (taskDialog != null)
            {
                taskDialog.Content = null;
                taskDialog.Buttons.Clear();
            }
        }
    }
}