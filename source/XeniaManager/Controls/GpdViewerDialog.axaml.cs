using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using FluentAvalonia.UI.Controls;
using XeniaManager.Core.Utilities;
using XeniaManager.Files;
using XeniaManager.Logging;
using XeniaManager.ViewModels.Controls;

namespace XeniaManager.Controls;

/// <summary>
/// Represents a dialog that shows the achievements and entry table of a GPD/XDBF file read-only.
/// </summary>
public partial class GpdViewerDialog : UserControl
{
    private readonly GpdViewerDialogViewModel? _viewModel;

    /// <summary>
    /// Initializes a new instance of the <see cref="GpdViewerDialog"/> class.
    /// This constructor is required for the AXAML loader.
    /// </summary>
    public GpdViewerDialog()
    {
        InitializeComponent();
    }

    private GpdViewerDialog(string fileName, GpdFile gpd)
    {
        InitializeComponent();
        _viewModel = new GpdViewerDialogViewModel(fileName, gpd);
        DataContext = _viewModel;
    }

    /// <summary>
    /// Shows the GPD viewer dialog for the given parsed file. Takes ownership of <paramref name="gpd"/>.
    /// </summary>
    /// <param name="fileName">The file name shown in the dialog.</param>
    /// <param name="gpd">The parsed GPD file, disposed when the dialog closes.</param>
    public static async Task ShowAsync(string fileName, GpdFile gpd)
    {
        GpdViewerDialog dialogContent = new GpdViewerDialog(fileName, gpd);
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
            Logger.Error<GpdViewerDialog>("Error showing GPD viewer dialog");
            Logger.LogExceptionDetails<GpdViewerDialog>(ex);
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