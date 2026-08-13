using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using XeniaManager.BigScreen.Models;
using XeniaManager.BigScreen.Services;
using XeniaManager.BigScreen.Utilities;
using XeniaManager.BigScreen.ViewModels.Items;
using XeniaManager.Core.Logging;
using XeniaManager.Core.Manage;
using XeniaManager.Core.Models;
using XeniaManager.Core.Models.Files.Account;
using XeniaManager.Core.Utilities;

namespace XeniaManager.BigScreen.ViewModels.Modals;

/// <summary>
/// Full-screen profile management overlay: create, delete, import, export and
/// edit Canary profiles. B closes (confirming unsaved edits), X deletes the
/// selected profile, View imports, Start exports. Opened from Settings or the
/// profile picker.
/// </summary>
public partial class ManageProfilesViewModel : ModalViewModelBase
{
    /// <summary>
    /// Regex pattern for valid gamertag format.
    /// Must start with a letter, can contain letters and numbers, spaces allowed only between words.
    /// </summary>
    private static readonly Regex GamertagRegex = new(@"^[A-Za-z][A-Za-z0-9]*( [A-Za-z0-9]+)*$");

    /// <summary>
    /// Maximum allowed length for a gamertag.
    /// </summary>
    private const int MaxGamertagLength = 15;

    /// <summary>
    /// The values loaded when a profile was selected; used to detect unsaved edits.
    /// </summary>
    private readonly record struct EditBaseline(
        string Gamertag,
        int CountryIndex,
        int LanguageIndex,
        bool IsLiveEnabled,
        int SubscriptionTierIndex);

    private readonly IProfileService _profileService;
    private readonly IModalService _modalService;
    private EditBaseline _baseline;

    /// <summary>
    /// Whether the edited profile can be persisted: a profile is selected, the
    /// gamertag is valid and the country/language combos have a selection.
    /// </summary>
    private bool CanSaveProfile =>
        SelectedProfile != null && CanSave && SelectedCountryIndex >= 0 && SelectedLanguageIndex >= 0;

    /// <summary>
    /// Whether the subscription tier combo holds a valid selection.
    /// </summary>
    private bool HasValidSubscriptionIndex =>
        SelectedSubscriptionTierIndex >= 0 && SelectedSubscriptionTierIndex < SubscriptionTiers.Count;

    /// <summary>
    /// Raised when View (Import) is pressed - the view runs the file picker.
    /// </summary>
    public event Action? ImportRequested;

    /// <summary>
    /// Raised when Start (Export) is pressed - the view runs the file picker.
    /// </summary>
    public event Action? ExportRequested;

    /// <summary>
    /// Raised after the selection moved, so the view can scroll it into view.
    /// </summary>
    public event Action? ScrollRequested;

    /// <summary>
    /// The profile rows (active first, then alphabetical). Lives in its own
    /// scroll view - the create stub is anchored beneath it, not part of it.
    /// </summary>
    public ObservableCollection<ProfileItemViewModel> Rows { get; } = [];

    /// <summary>
    /// The anchored "Create New Profile" row beneath the list.
    /// </summary>
    public CreateProfileStubViewModel CreateStub { get; } = new();

    /// <summary>
    /// The list of available countries for the ComboBox.
    /// </summary>
    public ObservableCollection<EnumDisplayItem<XboxLiveCountry>> Countries { get; }

    /// <summary>
    /// The list of available languages for the ComboBox.
    /// </summary>
    public ObservableCollection<EnumDisplayItem<ConsoleLanguage>> Languages { get; }

    /// <summary>
    /// The list of available subscription tiers for the ComboBox.
    /// </summary>
    public ObservableCollection<EnumDisplayItem<SubscriptionTier>> SubscriptionTiers { get; }

    /// <summary>
    /// The currently selected (edited) profile.
    /// </summary>
    [ObservableProperty] private AccountInfo? _selectedProfile;

    /// <summary>
    /// The gamertag for editing.
    /// </summary>
    [ObservableProperty] private string _editGamertag = string.Empty;

    /// <summary>
    /// The selected country index for the ComboBox.
    /// </summary>
    [ObservableProperty] private int _selectedCountryIndex;

    /// <summary>
    /// The selected language index for the ComboBox.
    /// </summary>
    [ObservableProperty] private int _selectedLanguageIndex;

    /// <summary>
    /// Whether Xbox Live is enabled for the selected profile.
    /// </summary>
    [ObservableProperty] private bool _isLiveEnabled;

    /// <summary>
    /// The selected subscription tier index for the ComboBox.
    /// </summary>
    [ObservableProperty] private int _selectedSubscriptionTierIndex;

    /// <summary>
    /// Indicates whether the Save button should be enabled.
    /// </summary>
    [ObservableProperty] private bool _canSave;

    /// <summary>
    /// The validation error message for the gamertag.
    /// </summary>
    [ObservableProperty] private string _gamertagErrorMessage = string.Empty;

    /// <summary>
    /// Indicates whether the gamertag has a validation error.
    /// </summary>
    [ObservableProperty] private bool _hasGamertagError;

    /// <summary>
    /// Whether the status line below the list has a message.
    /// </summary>
    [ObservableProperty] private bool _hasStatus;

    /// <summary>
    /// The status line text (create/save/import/export feedback).
    /// </summary>
    [ObservableProperty] private string _statusText = string.Empty;

    /// <summary>
    /// Whether any profiles exist.
    /// </summary>
    public bool HasProfiles => Rows.Count > 0;

    /// <summary>
    /// Whether the "no profiles" stub should show.
    /// </summary>
    public bool ShowEmpty => !HasProfiles;

    /// <summary>
    /// Whether the edit panel is shown. Hidden while the create stub is
    /// selected - there is nothing to edit.
    /// </summary>
    public bool ShowEditPanel => !CreateStub.IsSelected;

    /// <summary>
    /// Whether the edit fields differ from the values loaded when the profile
    /// was selected (unsaved edits).
    /// </summary>
    private bool IsDirty => SelectedProfile != null && (
        EditGamertag != _baseline.Gamertag ||
        SelectedCountryIndex != _baseline.CountryIndex ||
        SelectedLanguageIndex != _baseline.LanguageIndex ||
        IsLiveEnabled != _baseline.IsLiveEnabled ||
        SelectedSubscriptionTierIndex != _baseline.SubscriptionTierIndex);

    public ManageProfilesViewModel()
    {
        _profileService = App.Services.GetRequiredService<IProfileService>();
        _modalService = App.Services.GetRequiredService<IModalService>();
        Countries = new ObservableCollection<EnumDisplayItem<XboxLiveCountry>>(
            Enum.GetValues<XboxLiveCountry>().Select(v => new EnumDisplayItem<XboxLiveCountry>(v)));
        Languages = new ObservableCollection<EnumDisplayItem<ConsoleLanguage>>(
            Enum.GetValues<ConsoleLanguage>().Select(v => new EnumDisplayItem<ConsoleLanguage>(v)));
        SubscriptionTiers = new ObservableCollection<EnumDisplayItem<SubscriptionTier>>(
            Enum.GetValues<SubscriptionTier>().Select(v => new EnumDisplayItem<SubscriptionTier>(v)));
        CreateStub.PropertyChanged += (_, _) => OnPropertyChanged(nameof(ShowEditPanel));
        Reload();
    }

    partial void OnSelectedProfileChanged(AccountInfo? value) => LoadSelectedProfile();

    partial void OnEditGamertagChanged(string value)
    {
        ValidateGamertag();
        OnPropertyChanged(nameof(IsDirty));
    }

    partial void OnSelectedCountryIndexChanged(int value) => OnPropertyChanged(nameof(IsDirty));

    partial void OnSelectedLanguageIndexChanged(int value) => OnPropertyChanged(nameof(IsDirty));

    partial void OnIsLiveEnabledChanged(bool value) => OnPropertyChanged(nameof(IsDirty));

    partial void OnSelectedSubscriptionTierIndexChanged(int value) => OnPropertyChanged(nameof(IsDirty));

    /// <summary>
    /// Validates the gamertag according to the following rules:
    /// - Cannot be empty
    /// - Cannot be longer than 15 characters
    /// - Must start with a letter
    /// - Can contain letters and numbers
    /// - Spaces allowed only between words (not at start/end, no consecutive spaces)
    /// </summary>
    private void ValidateGamertag()
    {
        HasGamertagError = false;
        GamertagErrorMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(EditGamertag))
        {
            HasGamertagError = true;
            GamertagErrorMessage = LocalizationHelper.GetText("ManageProfiles.Edit.Gamertag.Error.Empty");
            CanSave = false;
            return;
        }

        if (EditGamertag.Length > MaxGamertagLength)
        {
            HasGamertagError = true;
            GamertagErrorMessage = string.Format(
                LocalizationHelper.GetText("ManageProfiles.Edit.Gamertag.Error.TooLong"), MaxGamertagLength);
            CanSave = false;
            return;
        }

        if (!GamertagRegex.IsMatch(EditGamertag))
        {
            HasGamertagError = true;
            GamertagErrorMessage = LocalizationHelper.GetText("ManageProfiles.Edit.Gamertag.Error.InvalidFormat");
            CanSave = false;
            return;
        }

        CanSave = true;
    }

    /// <summary>
    /// Loads the selected profile's data into the edit fields and captures the
    /// baseline used for unsaved-change detection.
    /// </summary>
    private void LoadSelectedProfile()
    {
        if (SelectedProfile == null)
        {
            EditGamertag = string.Empty;
            SelectedCountryIndex = Countries.IndexOf(Countries.FirstOrDefault(c => c.Value == XboxLiveCountry.Unknown)
                                                     ?? Countries.First());
            SelectedLanguageIndex = Languages.IndexOf(Languages.FirstOrDefault(l => l.Value == ConsoleLanguage.English)
                                                      ?? Languages.First());
            IsLiveEnabled = false;
            SelectedSubscriptionTierIndex = SubscriptionTiers.IndexOf(
                SubscriptionTiers.FirstOrDefault(s => s.Value == SubscriptionTier.NoSubscription)
                ?? SubscriptionTiers.First());
            CanSave = false;
            return;
        }

        EditGamertag = SelectedProfile.Gamertag;
        SelectedCountryIndex = Countries.IndexOf(Countries.FirstOrDefault(c => c.Value == SelectedProfile.Country)
                                                 ?? Countries.First());
        SelectedLanguageIndex = Languages.IndexOf(Languages.FirstOrDefault(l => l.Value == SelectedProfile.Language)
                                                  ?? Languages.First());
        IsLiveEnabled = SelectedProfile.IsLiveEnabled;
        SelectedSubscriptionTierIndex = SubscriptionTiers.IndexOf(
            SubscriptionTiers.FirstOrDefault(s => s.Value == SelectedProfile.SubscriptionTier)
            ?? SubscriptionTiers.First());
        _baseline = new EditBaseline(EditGamertag, SelectedCountryIndex, SelectedLanguageIndex, IsLiveEnabled,
            SelectedSubscriptionTierIndex);
        ValidateGamertag();
        OnPropertyChanged(nameof(IsDirty));
    }

    /// <summary>
    /// Rebuilds the profile rows from the profile service (active first, then
    /// alphabetical), keeping the previously edited row selected when it exists.
    /// </summary>
    public void Reload()
    {
        string? previousXuid = SelectedProfile.PathXuidText();
        Rows.Clear();
        AccountInfo? active = _profileService.ActiveProfile;
        foreach (ProfileItemViewModel item in ProfileRowsHelper.BuildRows(_profileService.Profiles, active))
        {
            Rows.Add(item);
        }

        CreateStub.IsSelected = false;
        OnPropertyChanged(nameof(HasProfiles));
        OnPropertyChanged(nameof(ShowEmpty));

        if (!SelectByXuid(previousXuid))
        {
            SelectFirst();
        }

        ScrollRequested?.Invoke();
        TaskUtilities.RunSafely<ManageProfilesViewModel>(
            () => ProfileRowsHelper.LoadGamerscoresAsync(Rows, _profileService), "Loading profile gamerscores");
    }

    /// <inheritdoc />
    public override bool HandleInput(NavigationCommand command)
    {
        switch (command)
        {
            case NavigationCommand.MoveUp:
                TaskUtilities.RunSafely<ManageProfilesViewModel>(() => MoveAsync(-1), "Moving profile selection");
                return true;
            case NavigationCommand.MoveDown:
                TaskUtilities.RunSafely<ManageProfilesViewModel>(() => MoveAsync(1), "Moving profile selection");
                return true;
            case NavigationCommand.Activate:
                ActivateSelected();
                return true;
            case NavigationCommand.CycleSort:
                // X deletes the selected profile (with confirmation)
                TaskUtilities.RunSafely<ManageProfilesViewModel>(DeleteSelectedAsync, "Deleting profile");
                return true;
            case NavigationCommand.ToggleView:
                // View imports
                ImportRequested?.Invoke();
                return true;
            case NavigationCommand.Start:
                // Start exports
                ExportRequested?.Invoke();
                return true;
            case NavigationCommand.Back:
                TaskUtilities.RunSafely<ManageProfilesViewModel>(CloseWithConfirmAsync, "Closing manage profiles");
                return true;
            default:
                return false;
        }
    }

    /// <summary>
    /// Activates the selected row: creates a new profile on the stub, otherwise
    /// keeps the profile loaded in the edit panel.
    /// </summary>
    private void ActivateSelected()
    {
        if (CreateStub.IsSelected)
        {
            TaskUtilities.RunSafely<ManageProfilesViewModel>(CreateWithConfirmAsync, "Creating profile");
        }
    }

    /// <summary>
    /// Moves the selection by the given step across the profile rows and the
    /// anchored create stub (stub = the last navigation slot), confirming
    /// unsaved edits before leaving the current profile.
    /// </summary>
    public async Task MoveAsync(int delta)
    {
        int navCount = Rows.Count + 1; // profiles + the anchored create stub
        if (navCount == 1)
        {
            // Only the stub exists - nothing to move between
            return;
        }

        int current = SelectionHelper.IndexOfSelected(Rows);
        if (current < 0)
        {
            current = CreateStub.IsSelected ? Rows.Count : 0;
        }

        int target = Math.Clamp(current + delta, 0, navCount - 1);
        if (target == current)
        {
            return;
        }

        if (target == Rows.Count)
        {
            if (!await ConfirmOrSaveAsync())
            {
                return;
            }

            SelectStub();
        }
        else
        {
            await SwitchRowAsync(Rows[target].Profile.PathXuidText());
        }

        ScrollRequested?.Invoke();
    }

    /// <summary>
    /// Selects the given row (mouse click), confirming unsaved edits first.
    /// </summary>
    public void SelectRow(ProfileItemViewModel item)
    {
        TaskUtilities.RunSafely<ManageProfilesViewModel>(
            () => SwitchRowAsync(item.Profile.PathXuidText()), "Selecting profile row");
    }

    /// <summary>
    /// Switches the edited profile, prompting to save or discard pending edits.
    /// Cancelling the prompt (B) keeps the current row selected.
    /// </summary>
    private async Task SwitchRowAsync(string? xuid)
    {
        if (!await ConfirmOrSaveAsync())
        {
            return;
        }

        if (xuid == null || !SelectByXuid(xuid))
        {
            SelectFirst();
        }

        ScrollRequested?.Invoke();
    }

    /// <summary>
    /// Prompts to save or discard pending edits, saving when chosen.
    /// Returns false when the prompt was cancelled (stay put).
    /// </summary>
    private async Task<bool> ConfirmOrSaveAsync()
    {
        if (!IsDirty)
        {
            return true;
        }

        bool? choice = await ConfirmUnsavedAsync();
        if (choice == null)
        {
            return false;
        }

        if (choice == true)
        {
            Save();
        }

        return true;
    }

    /// <summary>
    /// Prompts to save or discard pending edits; true = save, false = discard,
    /// null = prompt cancelled (stay put).
    /// </summary>
    private async Task<bool?> ConfirmUnsavedAsync()
    {
        if (!IsDirty)
        {
            return null;
        }

        return await _modalService.ShowAsync<bool?>(new ConfirmationModalViewModel(
            LocalizationHelper.GetText("ManageProfiles.Unsaved.Title"),
            string.Format(LocalizationHelper.GetText("ManageProfiles.Unsaved.Message"), SelectedProfile?.Gamertag),
            LocalizationHelper.GetText("ManageProfiles.Unsaved.Save"),
            LocalizationHelper.GetText("ManageProfiles.Unsaved.Discard")));
    }

    /// <summary>
    /// Closes the overlay, prompting to save pending edits first. Cancelling
    /// the prompt (B) keeps the overlay open.
    /// </summary>
    private async Task CloseWithConfirmAsync()
    {
        if (!await ConfirmOrSaveAsync())
        {
            return;
        }

        Close();
    }

    /// <summary>
    /// Creates a new account with the default name "New User" and selects it,
    /// confirming pending edits to the current profile first. Cancelling the
    /// prompt (B) keeps the current profile selected.
    /// </summary>
    public async Task CreateWithConfirmAsync()
    {
        if (!await ConfirmOrSaveAsync())
        {
            return;
        }

        CreateAccount();
    }

    /// <summary>
    /// Creates a new account with the default name "New User" and selects it.
    /// </summary>
    public void CreateAccount()
    {
        AccountInfo newAccount = ProfileManager.CreateAccount(XeniaVersion.Canary, "New User");
        Rows.Add(new ProfileItemViewModel(newAccount, false));
        CreateStub.IsSelected = false;
        SelectByXuid(newAccount.PathXuidText());
        ScrollRequested?.Invoke();
        OnPropertyChanged(nameof(HasProfiles));
        OnPropertyChanged(nameof(ShowEmpty));
        SetStatus(string.Format(LocalizationHelper.GetText("ManageProfiles.Create.Success"), newAccount.Gamertag));
    }

    /// <summary>
    /// Deletes the selected profile after a warning confirmation, refreshing the
    /// active profile when it was the one deleted.
    /// </summary>
    public async Task DeleteSelectedAsync()
    {
        if (SelectedProfile == null)
        {
            return;
        }

        string confirmationMessage = string.Format(
            LocalizationHelper.GetText("ManageProfiles.Delete.Confirmation.Message"), SelectedProfile.Gamertag);
        bool confirmed = await _modalService.ShowAsync<bool?>(new ConfirmationModalViewModel(
            LocalizationHelper.GetText("ManageProfiles.Delete.Confirmation.Title"),
            confirmationMessage,
            LocalizationHelper.GetText("Modal.Confirm"),
            LocalizationHelper.GetText("Modal.Cancel"))) ?? false;
        if (!confirmed)
        {
            return;
        }

        if (ProfileManager.DeleteAccount(XeniaVersion.Canary, SelectedProfile))
        {
            ProfileItemViewModel? row = Rows.FirstOrDefault(r => ReferenceEquals(r.Profile, SelectedProfile));
            if (row != null)
            {
                Rows.Remove(row);
            }

            OnPropertyChanged(nameof(HasProfiles));
            OnPropertyChanged(nameof(ShowEmpty));
            _profileService.Refresh();
            Reload();
            SetStatus(LocalizationHelper.GetText("ManageProfiles.Delete.Success"));
        }
        else
        {
            SetStatus(LocalizationHelper.GetText("ManageProfiles.Delete.Error"), isError: true);
        }
    }

    /// <summary>
    /// Saves the edited profile data and persists all profiles to disk.
    /// </summary>
    public void Save()
    {
        if (!CanSaveProfile)
        {
            return;
        }

        if (SelectedProfile is not { } profile)
        {
            return;
        }

        profile.Gamertag = EditGamertag;
        profile.Country = Countries[SelectedCountryIndex].Value;
        profile.Language = Languages[SelectedLanguageIndex].Value;
        profile.IsLiveEnabled = IsLiveEnabled;
        if (HasValidSubscriptionIndex)
        {
            profile.SubscriptionTier = SubscriptionTiers[SelectedSubscriptionTierIndex].Value;
        }

        int savedCount = ProfileManager.SaveProfiles(_profileService.Profiles.ToList(), XeniaVersion.Canary);
        _profileService.Refresh();
        Reload();
        if (savedCount > 0)
        {
            SetStatus(string.Format(LocalizationHelper.GetText("ManageProfiles.Save.Success"), savedCount));
        }
        else
        {
            SetStatus(LocalizationHelper.GetText("ManageProfiles.Save.Error"), isError: true);
        }
    }

    /// <summary>
    /// Exports the selected profile to the given path, optionally including saves
    /// (confirmed via the modal system).
    /// </summary>
    public async Task ExportSelectedAsync(string outputPath)
    {
        if (SelectedProfile == null)
        {
            return;
        }

        bool exportSaves = await _modalService.ShowAsync<bool?>(new ConfirmationModalViewModel(
            LocalizationHelper.GetText("ManageProfiles.Export.Confirmation.Title"),
            LocalizationHelper.GetText("ManageProfiles.Export.Confirmation.Message"),
            LocalizationHelper.GetText("Modal.Confirm"),
            LocalizationHelper.GetText("Modal.Cancel"))) ?? false;

        try
        {
            bool result =
                await ProfileManager.ExportProfile(XeniaVersion.Canary, SelectedProfile, exportSaves, outputPath);
            if (result)
            {
                SetStatus(string.Format(LocalizationHelper.GetText("ManageProfiles.Export.Success"),
                    SelectedProfile.Gamertag));
            }
            else
            {
                SetStatus(LocalizationHelper.GetText("ManageProfiles.Export.Failed"), isError: true);
            }
        }
        catch (Exception ex)
        {
            Logger.Error<ManageProfilesViewModel>("Failed to export profile");
            Logger.LogExceptionDetails<ManageProfilesViewModel>(ex);
            SetStatus(LocalizationHelper.GetText("ManageProfiles.Export.Failed"), isError: true);
        }
    }

    /// <summary>
    /// Imports a profile from the given path, asking for confirmation when a
    /// profile with the same XUID already exists.
    /// </summary>
    public async Task ImportFromAsync(string zipPath)
    {
        try
        {
            AccountInfo? imported = await ProfileManager.ImportProfileWithReplacement(
                XeniaVersion.Canary, zipPath, _profileService.Profiles.ToList(), async existing =>
                {
                    string message = string.Format(
                        LocalizationHelper.GetText("ManageProfiles.Import.Replace.Confirmation.Message"),
                        existing.Gamertag,
                        existing.PathXuid?.ToString() ?? "Unknown");
                    return await _modalService.ShowAsync<bool?>(new ConfirmationModalViewModel(
                        LocalizationHelper.GetText("ManageProfiles.Import.Replace.Confirmation.Title"),
                        message,
                        LocalizationHelper.GetText("Modal.Confirm"),
                        LocalizationHelper.GetText("Modal.Cancel"))) ?? false;
                });

            if (imported != null)
            {
                _profileService.Refresh();
                Reload();
                SelectByXuid(imported.PathXuidText());
                SetStatus(string.Format(LocalizationHelper.GetText("ManageProfiles.Import.Success"),
                    imported.Gamertag));
            }
            else
            {
                SetStatus(LocalizationHelper.GetText("ManageProfiles.Import.Canceled"));
            }
        }
        catch (Exception ex)
        {
            Logger.Error<ManageProfilesViewModel>("Failed to import profile");
            Logger.LogExceptionDetails<ManageProfilesViewModel>(ex);
            SetStatus(LocalizationHelper.GetText("ManageProfiles.Import.Failed"), isError: true);
        }
    }

    /// <summary>
    /// Selects the row matching the given XUID (clearing the create stub),
    /// returning whether it was found.
    /// </summary>
    private bool SelectByXuid(string? xuid)
    {
        if (xuid != null)
        {
            ProfileItemViewModel? row = Rows.FirstOrDefault(r =>
                r.Profile.PathXuidText()?.Equals(xuid, StringComparison.OrdinalIgnoreCase) == true);
            if (row != null)
            {
                SelectRowOnly(row);
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Selects the first profile row, or clears the edit panel when none exist.
    /// </summary>
    private void SelectFirst()
    {
        if (Rows.Count > 0)
        {
            SelectRowOnly(Rows[0]);
        }
        else
        {
            SelectedProfile = null;
        }
    }

    /// <summary>
    /// Selects the given row and deselects the create stub.
    /// </summary>
    private void SelectRowOnly(ProfileItemViewModel row)
    {
        SelectionHelper.SelectOnly(Rows, row);
        CreateStub.IsSelected = false;
        SelectedProfile = row.Profile;
    }

    /// <summary>
    /// Selects the anchored create stub and clears the edit fields.
    /// </summary>
    private void SelectStub()
    {
        SelectionHelper.ClearSelection(Rows);
        CreateStub.IsSelected = true;
        SelectedProfile = null;
    }

    /// <summary>
    /// Sets the status line below the list.
    /// </summary>
    private void SetStatus(string message, bool isError = false)
    {
        StatusText = message;
        HasStatus = true;
        Logger.Info<ManageProfilesViewModel>($"{(isError ? "Error: " : "")}{message}");
    }
}