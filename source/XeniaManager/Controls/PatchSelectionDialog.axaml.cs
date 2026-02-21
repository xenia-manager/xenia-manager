using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using FluentAvalonia.UI.Controls;
using Microsoft.Extensions.DependencyInjection;
using XeniaManager.Core.Database;
using XeniaManager.Core.Logging;
using XeniaManager.Core.Models.Database.Patches;
using XeniaManager.Core.Utilities;
using XeniaManager.Services;
using XeniaManager.ViewModels.Controls;

namespace XeniaManager.Controls;

/// <summary>
/// Represents a dialog that allows users to select a single patch for a game from the Canary patches database.
/// This control provides a searchable list of available patches with a modern Fluent Design UI.
/// </summary>
public partial class PatchSelectionDialog : UserControl
{
    /// <summary>
    /// The ViewModel containing the dialog's data and logic.
    /// </summary>
    private readonly PatchSelectionDialogViewModel _viewModel;

    /// <summary>
    /// The service used to display error messages to the user.
    /// </summary>
    private readonly IMessageBoxService _messageBox;

    /// <summary>
    /// CancellationTokenSource for debouncing search operations.
    /// </summary>
    private CancellationTokenSource? _searchCancellationTokenSource;

    /// <summary>
    /// Debounce delay for search operations in milliseconds.
    /// </summary>
    private const int SearchDebounceDelay = 200;

    /// <summary>
    /// Flag to track if the initial search has been performed.
    /// </summary>
    private bool _isInitialLoad = true;

    /// <summary>
    /// Initializes a new instance of the <see cref="PatchSelectionDialog"/> class.
    /// This constructor is required for disabling warning caused by the builder.
    /// </summary>
    public PatchSelectionDialog() : this(string.Empty)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PatchSelectionDialog"/> class.
    /// </summary>
    /// <param name="searchQuery">The initial search query to populate the search box with.</param>
    private PatchSelectionDialog(string searchQuery)
    {
        InitializeComponent();
        _viewModel = App.Services.GetRequiredService<PatchSelectionDialogViewModel>();
        _messageBox = App.Services.GetRequiredService<IMessageBoxService>();
        DataContext = _viewModel;
        _viewModel.SearchText = searchQuery;

        // Perform initial search without debouncing (fire-and-forget)
        _ = InitializeSearchAsync();
    }

    /// <summary>
    /// Initializes the search on the dialog load without debouncing.
    /// </summary>
    private async Task InitializeSearchAsync()
    {
        try
        {
            // Search both databases simultaneously
            await Task.WhenAll(
                PatchesDatabase.SearchCanaryDatabase(_viewModel.SearchText),
                PatchesDatabase.SearchNetplayDatabase(_viewModel.SearchText)
            );
            _viewModel.UpdateVisiblePatches();
        }
        catch (Exception ex)
        {
            Logger.Error<PatchSelectionDialog>("Failed to search patches");
            Logger.LogExceptionDetails<PatchSelectionDialog>(ex);
            await _messageBox.ShowErrorAsync(LocalizationHelper.GetText("PatchSelectionDialog.SearchBox.Failed.Title"),
                string.Format(LocalizationHelper.GetText("PatchSelectionDialog.SearchBox.Failed.Message"),
                    ex));
        }
        finally
        {
            _isInitialLoad = false;
        }
    }

    /// <summary>
    /// Shows a dialog to allow the user to select a patch from the Canary patches database.
    /// </summary>
    /// <param name="searchQuery">The initial search query to populate the search box with.</param>
    /// <returns>The selected patch information, or null if the user canceled the dialog.</returns>
    public static async Task<PatchInfo?> ShowAsync(string searchQuery)
    {
        PatchSelectionDialog dialog = new PatchSelectionDialog(searchQuery);

        ContentDialog contentDialog = new ContentDialog
        {
            Title = LocalizationHelper.GetText("PatchSelectionDialog.ContentDialog.Title"),
            Content = dialog,
            PrimaryButtonText = LocalizationHelper.GetText("PatchSelectionDialog.ContentDialog.PrimaryButton.Text"),
            CloseButtonText = LocalizationHelper.GetText("PatchSelectionDialog.ContentDialog.CloseButton.Text"),
            DefaultButton = ContentDialogButton.Primary
        };

        ContentDialogResult result = await contentDialog.ShowAsync();

        return result == ContentDialogResult.Primary ? dialog._viewModel.SelectedPatch?.PatchInfo : null;
    }

    /// <summary>
    /// Handles the TextChanged event of the search TextBox.
    /// Triggers a search in both Canary and Netplay databases and updates the visible patches list.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>
    private async void TextBox_OnTextChanged(object? sender, TextChangedEventArgs e)
    {
        try
        {
            // Skip the event on the initial load since we already performed the search
            if (_isInitialLoad)
            {
                return;
            }

            // Cancel any pending search operation
            _searchCancellationTokenSource?.Cancel();
            _searchCancellationTokenSource?.Dispose();

            // Create a new cancellation token source for this search
            _searchCancellationTokenSource = new CancellationTokenSource();
            CancellationToken cancellationToken = _searchCancellationTokenSource.Token;

            try
            {
                // Wait for the debounced delay before searching
                await Task.Delay(SearchDebounceDelay,
                    cancellationToken);

                // Search both databases simultaneously
                await Task.WhenAll(
                    PatchesDatabase.SearchCanaryDatabase(_viewModel.SearchText),
                    PatchesDatabase.SearchNetplayDatabase(_viewModel.SearchText)
                );
                _viewModel.UpdateVisiblePatches();
            }
            catch (OperationCanceledException)
            {
                // Search was canceled due to new input, this is expected
            }
        }
        catch (Exception ex)
        {
            Logger.Error<PatchSelectionDialog>("Failed to search patches");
            Logger.LogExceptionDetails<PatchSelectionDialog>(ex);
            await _messageBox.ShowErrorAsync(LocalizationHelper.GetText("PatchSelectionDialog.SearchBox.Failed.Title"),
                string.Format(LocalizationHelper.GetText("PatchSelectionDialog.SearchBox.Failed.Message"),
                    ex));
        }
    }
}