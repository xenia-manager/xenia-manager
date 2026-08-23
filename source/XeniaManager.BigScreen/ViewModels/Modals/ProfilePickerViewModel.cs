using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using XeniaManager.BigScreen.Factories;
using XeniaManager.BigScreen.Models;
using XeniaManager.BigScreen.Services;
using XeniaManager.BigScreen.Utilities;
using XeniaManager.BigScreen.ViewModels.Items;
using XeniaManager.Core.Converters;
using XeniaManager.Core.Logging;
using XeniaManager.Core.Models;

namespace XeniaManager.BigScreen.ViewModels.Modals;

/// <summary>
/// Full-screen profile picker: lists the profiles of one emulator version
/// (selected via the version chips or left/right), A switches the active
/// profile, Y opens Manage Profiles, B closes. Opened from the header avatar chip.
/// </summary>
public class ProfilePickerViewModel : ModalViewModelBase
{
    private readonly IProfileService _profileService;
    private readonly IModalService _modalService;

    private XeniaVersion _version;

    /// <summary>
    /// All profiles of the selected version, active one first.
    /// </summary>
    public ObservableCollection<ProfileItemViewModel> Profiles { get; } = [];

    /// <summary>
    /// Emulator-version chips, one per version that has profiles.
    /// </summary>
    public ObservableCollection<VersionChipViewModel> VersionChips { get; } = [];

    /// <summary>
    /// Whether any profiles exist.
    /// </summary>
    public bool HasProfiles => Profiles.Count > 0;

    /// <summary>
    /// Whether any version chips exist.
    /// </summary>
    public bool HasVersionChips => VersionChips.Count > 0;

    /// <summary>
    /// Whether the "no profiles" stub should show.
    /// </summary>
    public bool ShowEmpty => !HasProfiles;

    /// <summary>
    /// Rebuilds the profile list and version chips from the profile service.
    /// </summary>
    public void Reload()
    {
        VersionProfileState state = _profileService.StateFor(_version);
        Profiles.Clear();
        foreach (ProfileItemViewModel item in ProfileRowsHelper.BuildRows(state.Profiles, state.ActiveProfile))
        {
            Profiles.Add(item);
        }

        if (Profiles.Count > 0)
        {
            Profiles[0].IsSelected = true;
        }

        BuildChips();
        TaskUtilities.RunSafely<ProfilePickerViewModel>(
            () => ProfileRowsHelper.LoadGamerscoresAsync(Profiles, _profileService, _version),
            "Loading profile gamerscores");
    }

    /// <summary>
    /// Rebuilds the version chips for every installed version that has profiles,
    /// marking the selected one.
    /// </summary>
    private void BuildChips()
    {
        VersionChips.Clear();
        IReadOnlyList<XeniaVersion> versions = _profileService.VersionsWithProfiles;
        if (versions.Count == 0)
        {
            versions = _profileService.InstalledVersions;
        }

        foreach (XeniaVersion version in versions)
        {
            VersionChips.Add(new VersionChipViewModel(
                version,
                IconFactory.GetVersionIcon(version),
                (string)XeniaVersionToStringConverter.Instance.Convert(version, typeof(string), null,
                    System.Globalization.CultureInfo.InvariantCulture)!,
                version == _version));
        }
    }

    /// <summary>
    /// Switches the shown version by the given step, wrapping around.
    /// </summary>
    public void SwitchVersion(int delta)
    {
        IReadOnlyList<XeniaVersion> versions = _profileService.VersionsWithProfiles;
        if (versions.Count == 0)
        {
            versions = _profileService.InstalledVersions;
        }

        if (versions.Count == 0)
        {
            return;
        }

        int index = versions.ToList().IndexOf(_version);
        if (index < 0)
        {
            index = 0;
        }

        _version = versions[(index + delta + versions.Count) % versions.Count];
        Reload();
    }

    /// <summary>
    /// Selects the given version chip (mouse click) and shows its profiles.
    /// </summary>
    public void SelectVersion(VersionChipViewModel chip)
    {
        _version = chip.Version;
        Reload();
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
            _profileService.SwitchProfile(_version, selected.Profile);
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
    /// Opens the Manage Profiles overlay on top of the picker; when it closes,
    /// the picker reloads so created/edited profiles show up immediately.
    /// </summary>
    public async Task OpenManageProfilesAsync()
    {
        Logger.Debug<ProfilePickerViewModel>("Opening manage profiles from picker");
        await _modalService.ShowAsync(new ManageProfilesViewModel(_version));
        Reload();
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
            case NavigationCommand.MoveLeft:
                SwitchVersion(-1);
                return true;
            case NavigationCommand.MoveRight:
                SwitchVersion(1);
                return true;
            case NavigationCommand.Activate:
                SelectActive();
                return true;
            case NavigationCommand.Details:
                TaskUtilities.RunSafely<ProfilePickerViewModel>(OpenManageProfilesAsync, "Opening manage profiles");
                return true;
            case NavigationCommand.Back:
                Close();
                return true;
            default:
                return false;
        }
    }

    public ProfilePickerViewModel()
    {
        _profileService = App.Services.GetRequiredService<IProfileService>();
        _modalService = App.Services.GetRequiredService<IModalService>();
        _version = _profileService.ActiveVersion ?? XeniaVersion.Canary;
        Reload();
    }
}