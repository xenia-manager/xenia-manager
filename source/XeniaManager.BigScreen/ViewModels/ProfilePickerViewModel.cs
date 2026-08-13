using System;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using XeniaManager.BigScreen.Models;
using XeniaManager.BigScreen.Services;
using XeniaManager.BigScreen.Utilities;
using XeniaManager.BigScreen.ViewModels.Items;
using XeniaManager.Core.Logging;
using XeniaManager.Core.Models.Files.Account;
using XeniaManager.Core.Utilities;

namespace XeniaManager.BigScreen.ViewModels;

/// <summary>
/// Full-screen profile picker: lists all Canary profiles, A switches the active
/// profile, Y opens Manage Profiles, B closes. Opened from the header avatar chip.
/// </summary>
public class ProfilePickerViewModel : ModalViewModelBase
{
    private readonly IProfileService _profileService;
    private readonly IModalService _modalService;

    /// <summary>
    /// All Canary profiles, active one first.
    /// </summary>
    public ObservableCollection<ProfileItemViewModel> Profiles { get; } = [];

    /// <summary>
    /// Whether any profiles exist.
    /// </summary>
    public bool HasProfiles => Profiles.Count > 0;

    /// <summary>
    /// Whether the "no profiles" stub should show.
    /// </summary>
    public bool ShowEmpty => !HasProfiles;

    public ProfilePickerViewModel()
    {
        _profileService = App.Services.GetRequiredService<IProfileService>();
        _modalService = App.Services.GetRequiredService<IModalService>();
        Reload();
    }

    /// <summary>
    /// Rebuilds the profile list from the profile service, active first, then alphabetical.
    /// </summary>
    public void Reload()
    {
        Profiles.Clear();
        AccountInfo? active = _profileService.ActiveProfile;
        foreach (ProfileItemViewModel item in ProfileRowsHelper.BuildRows(_profileService.Profiles, active))
        {
            Profiles.Add(item);
        }

        if (Profiles.Count > 0)
        {
            Profiles[0].IsSelected = true;
        }

        TaskUtilities.RunSafely<ProfilePickerViewModel>(
            () => ProfileRowsHelper.LoadGamerscoresAsync(Profiles, _profileService), "Loading profile gamerscores");
    }

    /// <inheritdoc />
    public override bool HandleInput(NavigationCommand command)
    {
        switch (command)
        {
            case NavigationCommand.MoveUp:
                Move(-1);
                return true;
            case NavigationCommand.MoveDown:
                Move(1);
                return true;
            case NavigationCommand.Activate:
                SelectActive();
                return true;
            case NavigationCommand.CycleSort:
                // Y (Details) opens the full profile manager on top of the picker
                OpenManageProfiles();
                return true;
            case NavigationCommand.Back:
                Close();
                return true;
            default:
                return false;
        }
    }

    /// <summary>
    /// Moves the selection by the given step, clamped at both ends.
    /// </summary>
    public void Move(int delta) => SelectionHelper.MoveSelection(Profiles, delta);

    /// <summary>
    /// Switches to the selected profile and closes the picker.
    /// </summary>
    public void SelectActive()
    {
        ProfileItemViewModel? selected = Profiles.FirstOrDefault(p => p.IsSelected);
        if (selected != null)
        {
            _profileService.SwitchProfile(selected.Profile);
        }

        Close();
    }

    /// <summary>
    /// Switches to the given profile (mouse click) and closes the picker.
    /// </summary>
    public void SelectProfile(ProfileItemViewModel item)
    {
        SelectionHelper.SelectOnly(Profiles, item);
        SelectActive();
    }

    /// <summary>
    /// Opens the Manage Profiles overlay on top of the picker; closing it
    /// returns to the (refreshed) picker.
    /// </summary>
    public void OpenManageProfiles()
    {
        Logger.Debug<ProfilePickerViewModel>("Opening manage profiles from picker");
        TaskUtilities.RunSafely<ProfilePickerViewModel>(
            () => _modalService.ShowAsync(new ManageProfilesViewModel()), "Opening manage profiles");
    }
}