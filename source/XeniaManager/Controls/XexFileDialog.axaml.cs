using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using FluentAvalonia.UI.Controls;
using Microsoft.Extensions.DependencyInjection;
using XeniaManager.Core.Utilities;
using XeniaManager.Logging;
using XeniaManager.Services;
using XeniaManager.ViewModels.Controls;

namespace XeniaManager.Controls;

/// <summary>
/// Represents a dialog that shows the executable header and embedded SPA details
/// of an XEX file read-only.
/// </summary>
public partial class XexFileDialog : UserControl
{
    private readonly XexFileDialogViewModel? _viewModel;

    /// <summary>
    /// Initializes a new instance of the <see cref="XexFileDialog"/> class.
    /// This constructor is required for the AXAML loader.
    /// </summary>
    public XexFileDialog()
    {
        InitializeComponent();
    }

    private XexFileDialog(string fileName, byte[] xexBytes)
    {
        InitializeComponent();
        _viewModel = new XexFileDialogViewModel(fileName, xexBytes);
        _viewModel.OwnerProvider = () => TopLevel.GetTopLevel(this);
        DataContext = _viewModel;
    }

    /// <summary>
    /// Shows the XEX file dialog for the given raw bytes.
    /// </summary>
    /// <param name="fileName">The file name shown in the dialog.</param>
    /// <param name="xexBytes">The raw XEX bytes to parse.</param>
    public static async Task ShowAsync(string fileName, byte[] xexBytes)
    {
        XexFileDialog dialogContent = new XexFileDialog(fileName, xexBytes);
        FATaskDialog? taskDialog = null;
        try
        {
            if (dialogContent._viewModel == null || !await dialogContent._viewModel.LoadAsync())
            {
                IMessageBoxService messageBoxService = App.Services.GetRequiredService<IMessageBoxService>();
                await messageBoxService.ShowInfoAsync(
                    LocalizationHelper.GetText("GameFilesDialog.OpenPreview.Unsupported.Title"),
                    LocalizationHelper.GetText("GameFilesDialog.OpenPreview.Unsupported.Message"));
                return;
            }

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
            Logger.Error<XexFileDialog>("Error showing XEX file dialog");
            Logger.LogExceptionDetails<XexFileDialog>(ex);
        }
        finally
        {
            dialogContent._viewModel?.Dispose();
            dialogContent.DataContext = null;
            if (taskDialog != null)
            {
                taskDialog.Content = null;
                taskDialog.Buttons.Clear();
            }
        }
    }
}