using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using XeniaManager.Core.Database;
using XeniaManager.Core.Models.Database.Patches;
using XeniaManager.ViewModels.Items;

namespace XeniaManager.ViewModels.Controls;

/// <summary>
/// ViewModel for the patch selection dialog.
/// Manages the collection of available patches and handles user interactions.
/// </summary>
public partial class PatchSelectionDialogViewModel : ViewModelBase
{
    /// <summary>
    /// The title displayed at the top of the dialog.
    /// </summary>
    [ObservableProperty] private string _title = string.Empty;

    /// <summary>
    /// The current search text entered by the user.
    /// Used to filter the patches database.
    /// </summary>
    [ObservableProperty] private string _searchText = string.Empty;

    /// <summary>
    /// Collection of patch items currently displayed in the list.
    /// Limited to <see cref="MaxVisibleItems"/> entries for performance.
    /// </summary>
    [ObservableProperty] private ObservableCollection<PatchItemViewModel> _patches = [];

    /// <summary>
    /// The currently selected patch item in the list.
    /// Null when no selection has been made.
    /// </summary>
    [ObservableProperty] private PatchItemViewModel? _selectedPatch;

    /// <summary>
    /// Flag indicating whether there are more patches available beyond what's currently displayed.
    /// Used to show/hide the "More patches available..." indicator.
    /// </summary>
    [ObservableProperty] private bool _hasMoreItems;

    /// <summary>
    /// Maximum number of patch items to display in the list at once.
    /// This limit improves performance when dealing with large patch databases.
    /// </summary>
    private const int MaxVisibleItems = 1000;

    /// <summary>
    /// Initializes a new instance of the <see cref="PatchSelectionDialogViewModel"/> class.
    /// Loads the initial set of patches from both Canary and Netplay databases.
    /// </summary>
    public PatchSelectionDialogViewModel()
    {
        UpdateVisiblePatches();
    }

    /// <summary>
    /// Updates the visible patches collection by combining both Canary and Netplay patches.
    /// Displays only the first <see cref="MaxVisibleItems"/> patches and sets
    /// <see cref="HasMoreItems"/> to true if additional patches exist.
    /// </summary>
    public void UpdateVisiblePatches()
    {
        Patches.Clear();

        // Combine both Canary and Netplay patches
        IEnumerable<PatchItemViewModel> canaryPatches = PatchesDatabase.CanaryFilteredDatabase
            .Select(p => new PatchItemViewModel(p, PatchDatabaseType.Canary));
        IEnumerable<PatchItemViewModel> netplayPatches = PatchesDatabase.NetplayFilteredDatabase
            .Select(p => new PatchItemViewModel(p, PatchDatabaseType.Netplay));

        // Combine and take only the maximum visible items
        List<PatchItemViewModel> allPatches = canaryPatches.Concat(netplayPatches).Take(MaxVisibleItems).ToList();

        foreach (PatchItemViewModel patch in allPatches)
        {
            Patches.Add(patch);
        }

        // Check if there are more patches than what we're displaying
        int totalCount = PatchesDatabase.CanaryFilteredDatabase.Count + PatchesDatabase.NetplayFilteredDatabase.Count;
        HasMoreItems = totalCount > MaxVisibleItems;
    }

    /// <summary>
    /// Cancels the current patch selection by setting <see cref="SelectedPatch"/> to null.
    /// Invoked when the user clicks the Cancel button.
    /// </summary>
    [RelayCommand]
    private void Cancel()
    {
        SelectedPatch = null;
    }
}