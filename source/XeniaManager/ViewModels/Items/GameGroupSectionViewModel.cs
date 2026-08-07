using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using XeniaManager.ViewModels.Pages;

namespace XeniaManager.ViewModels.Items;

/// <summary>
/// A collapsible library section representing a game group (or the ungrouped bucket).
/// </summary>
public partial class GameGroupSectionViewModel : ViewModelBase
{
    private readonly LibraryPageViewModel _library;

    /// <summary>
    /// Group id, or null for the Ungrouped section.
    /// </summary>
    public Guid? GroupId { get; }

    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private bool _isExpanded = true;
    [ObservableProperty] private ObservableCollection<GameItemViewModel> _games = [];

    public bool IsUserGroup => GroupId.HasValue;

    public string HeaderText => $"{Name} ({Games.Count})";

    public GameGroupSectionViewModel(Guid? groupId, string name, LibraryPageViewModel library)
    {
        GroupId = groupId;
        Name = name;
        _library = library;
    }

    public void SetGames(IEnumerable<GameItemViewModel> games)
    {
        Games = new ObservableCollection<GameItemViewModel>(games);
        OnPropertyChanged(nameof(HeaderText));
    }

    [RelayCommand]
    private async Task DeleteGroup()
    {
        if (GroupId is Guid id)
        {
            await _library.DeleteGroupAsync(id, Name);
        }
    }
}
