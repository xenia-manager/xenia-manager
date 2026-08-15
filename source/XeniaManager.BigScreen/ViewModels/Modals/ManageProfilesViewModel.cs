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
/// selected profile, View imports, Start exports, A or Right on a profile row
/// moves the controller into the edit panel (B or Left returns; A activates
/// the panel rows - toggles, dropdown editors, gamertag focus, save).
/// Opened from Settings or the profile picker.
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
    /// The edit-panel rows in display order, used for the panel selection movement.
    /// </summary>
    private readonly List<ISelectable> _panelRows = [];

    /// <summary>
    /// The kind of the panel editor currently open (a dropdown), or null when
    /// the panel rows navigate normally.
    /// </summary>
    private ManageProfilesRowKind? _editorKind;

    /// <summary>
    /// The dropdown index snapshotted when a panel editor opened, restored on cancel.
    /// </summary>
    private int _editorOriginalIndex;

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
    /// Raised after a panel editor opened (a dropdown), so the view can open
    /// the matching native control.
    /// </summary>
    public event Action<ManageProfilesRowKind>? PanelEditorOpened;

    /// <summary>
    /// Raised after a panel editor closed (commit or cancel), so the view can
    /// close the native control.
    /// </summary>
    public event Action? PanelEditorClosed;

    /// <summary>
    /// Raised when the controller activates the gamertag row, so the view can
    /// focus the text box for keyboard entry.
    /// </summary>
    public event Action? GamertagFocusRequested;

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
    /// Row for the edit panel's gamertag field.
    /// </summary>
    public ManageProfilesRowViewModel RowGamertag { get; } = new(ManageProfilesRowKind.Gamertag);

    /// <summary>
    /// Row for the edit panel's country dropdown.
    /// </summary>
    public ManageProfilesRowViewModel RowCountry { get; } = new(ManageProfilesRowKind.Country);

    /// <summary>
    /// Row for the edit panel's language dropdown.
    /// </summary>
    public ManageProfilesRowViewModel RowLanguage { get; } = new(ManageProfilesRowKind.Language);

    /// <summary>
    /// Row for the edit panel's Xbox Live toggle.
    /// </summary>
    public ManageProfilesRowViewModel RowLiveToggle { get; } = new(ManageProfilesRowKind.LiveToggle);

    /// <summary>
    /// Row for the edit panel's subscription tier dropdown.
    /// </summary>
    public ManageProfilesRowViewModel RowSubscriptionTier { get; } = new(ManageProfilesRowKind.SubscriptionTier);

    /// <summary>
    /// Row for the edit panel's Save action.
    /// </summary>
    public ManageProfilesRowViewModel RowSave { get; } = new(ManageProfilesRowKind.Save);

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
    [ObservableProperty]
    public partial AccountInfo? SelectedProfile { get; set; }

    /// <summary>
    /// The gamertag for editing.
    /// </summary>
    [ObservableProperty]
    public partial string EditGamertag { get; set; } = string.Empty;

    /// <summary>
    /// The selected country index for the ComboBox.
    /// </summary>
    [ObservableProperty]
    public partial int SelectedCountryIndex { get; set; }

    /// <summary>
    /// The selected language index for the ComboBox.
    /// </summary>
    [ObservableProperty]
    public partial int SelectedLanguageIndex { get; set; }

    /// <summary>
    /// Whether Xbox Live is enabled for the selected profile.
    /// </summary>
    [ObservableProperty]
    public partial bool IsLiveEnabled { get; set; }

    /// <summary>
    /// The selected subscription tier index for the ComboBox.
    /// </summary>
    [ObservableProperty]
    public partial int SelectedSubscriptionTierIndex { get; set; }

    /// <summary>
    /// Indicates whether the Save button should be enabled.
    /// </summary>
    [ObservableProperty]
    public partial bool CanSave { get; set; }

    /// <summary>
    /// The validation error message for the gamertag.
    /// </summary>
    [ObservableProperty]
    public partial string GamertagErrorMessage { get; set; } = string.Empty;

    /// <summary>
    /// Indicates whether the gamertag has a validation error.
    /// </summary>
    [ObservableProperty]
    public partial bool HasGamertagError { get; set; }

    /// <summary>
    /// Whether the controller is inside the edit panel (panel rows move there
    /// instead of the profile list).
    /// </summary>
    [ObservableProperty]
    public partial bool IsPanelActive { get; set; }

    /// <summary>
    /// Whether a panel editor (dropdown) is open and takes Up/Down input.
    /// </summary>
    public bool IsEditorOpen => _editorKind.HasValue;

    /// <summary>
    /// Whether the status line below the list has a message.
    /// </summary>
    [ObservableProperty]
    public partial bool HasStatus { get; set; }

    /// <summary>
    /// The status line text (create/save/import/export feedback).
    /// </summary>
    [ObservableProperty]
    public partial string StatusText { get; set; } = string.Empty;

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

        _panelRows.Add(RowGamertag);
        _panelRows.Add(RowCountry);
        _panelRows.Add(RowLanguage);
        _panelRows.Add(RowLiveToggle);
        _panelRows.Add(RowSubscriptionTier);
        _panelRows.Add(RowSave);

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
        if (IsPanelActive)
        {
            return HandlePanelInput(command);
        }

        switch (command)
        {
            case NavigationCommand.MoveUp:
                TaskUtilities.RunSafely<ManageProfilesViewModel>(() => MoveAsync(-1), "Moving profile selection");
                return true;
            case NavigationCommand.MoveDown:
                TaskUtilities.RunSafely<ManageProfilesViewModel>(() => MoveAsync(1), "Moving profile selection");
                return true;
            case NavigationCommand.MoveRight:
                // Right from a profile row enters the edit panel (like the game
                // modal's A/Right pane entry)
                EnterPanel();
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
    /// moves the controller into the edit panel.
    /// </summary>
    private void ActivateSelected()
    {
        if (CreateStub.IsSelected)
        {
            TaskUtilities.RunSafely<ManageProfilesViewModel>(CreateWithConfirmAsync, "Creating profile");
            return;
        }

        EnterPanel();
    }

    /// <summary>
    /// Moves the controller into the edit panel, clearing the profile-row
    /// selection (the panel is the active column now).
    /// </summary>
    private void EnterPanel()
    {
        if (SelectedProfile == null)
        {
            return;
        }

        IsPanelActive = true;
        SelectionHelper.ClearSelection(Rows);
        CreateStub.IsSelected = false;
        Logger.Debug<ManageProfilesViewModel>("Entered edit panel");
    }

    /// <summary>
    /// Returns the controller to the profile list, restoring the edited
    /// profile's row selection.
    /// </summary>
    private void LeavePanel()
    {
        ResetPanel();
        ProfileItemViewModel? row = Rows.FirstOrDefault(r => ReferenceEquals(r.Profile, SelectedProfile));
        if (row != null)
        {
            SelectionHelper.SelectOnly(Rows, row);
        }

        ScrollRequested?.Invoke();
        Logger.Debug<ManageProfilesViewModel>("Left edit panel");
    }

    /// <summary>
    /// Closes the panel state (editor, selection, active flag) after the
    /// controller left the panel or the selected profile changed.
    /// </summary>
    private void ResetPanel()
    {
        ClosePanelEditor();
        IsPanelActive = false;
        SelectionHelper.ClearSelection(_panelRows);
    }

    /// <summary>
    /// Handles controller input while the edit panel is the active column:
    /// row movement, activation, editor cycling, and returning to the list.
    /// </summary>
    private bool HandlePanelInput(NavigationCommand command)
    {
        if (IsEditorOpen)
        {
            return HandlePanelEditorInput(command);
        }

        switch (command)
        {
            case NavigationCommand.MoveUp:
                MovePanelSelection(-1);
                return true;
            case NavigationCommand.MoveDown:
                MovePanelSelection(1);
                return true;
            case NavigationCommand.MoveLeft:
            case NavigationCommand.Back:
                LeavePanel();
                return true;
            case NavigationCommand.Activate:
                ActivatePanelRow();
                return true;
            default:
                return false;
        }
    }

    /// <summary>
    /// Moves the panel selection by the given step, clamped at both ends. The
    /// panel rows start unselected - the first move selects the first row.
    /// </summary>
    private void MovePanelSelection(int delta)
    {
        SelectionHelper.MoveSelection(_panelRows, delta);
    }

    /// <summary>
    /// Activates the selected panel row: toggles the live switch, saves on the
    /// Save row, opens the editor on the dropdown rows, or focuses the
    /// gamertag text box for keyboard entry.
    /// </summary>
    private void ActivatePanelRow()
    {
        switch (_panelRows.FirstOrDefault(r => r.IsSelected))
        {
            case ManageProfilesRowViewModel { Kind: ManageProfilesRowKind.LiveToggle }:
                IsLiveEnabled = !IsLiveEnabled;
                break;
            case ManageProfilesRowViewModel { Kind: ManageProfilesRowKind.Save }:
                Save();
                break;
            case ManageProfilesRowViewModel { Kind: ManageProfilesRowKind.Gamertag }:
                GamertagFocusRequested?.Invoke();
                break;
            case ManageProfilesRowViewModel { Kind: ManageProfilesRowKind.Country }:
                OpenPanelEditor(ManageProfilesRowKind.Country, SelectedCountryIndex);
                break;
            case ManageProfilesRowViewModel { Kind: ManageProfilesRowKind.Language }:
                OpenPanelEditor(ManageProfilesRowKind.Language, SelectedLanguageIndex);
                break;
            case ManageProfilesRowViewModel { Kind: ManageProfilesRowKind.SubscriptionTier }:
                if (IsLiveEnabled)
                {
                    OpenPanelEditor(ManageProfilesRowKind.SubscriptionTier, SelectedSubscriptionTierIndex);
                }

                break;
        }
    }

    /// <summary>
    /// Opens the editor for the given panel dropdown, snapshotting its index
    /// so a cancel can restore it.
    /// </summary>
    private void OpenPanelEditor(ManageProfilesRowKind kind, int originalIndex)
    {
        _editorKind = kind;
        _editorOriginalIndex = originalIndex;
        PanelEditorOpened?.Invoke(kind);
        Logger.Debug<ManageProfilesViewModel>($"Opened {kind} editor");
    }

    /// <summary>
    /// Handles input while a panel editor is open: Up/Down cycles the dropdown,
    /// A commits and B cancels (restoring the original selection).
    /// </summary>
    private bool HandlePanelEditorInput(NavigationCommand command)
    {
        switch (command)
        {
            case NavigationCommand.MoveUp:
                CyclePanelEditor(-1);
                return true;
            case NavigationCommand.MoveDown:
                CyclePanelEditor(1);
                return true;
            case NavigationCommand.Activate:
                CommitPanelEditor();
                return true;
            case NavigationCommand.Back:
                CancelPanelEditor();
                return true;
            default:
                return false;
        }
    }

    /// <summary>
    /// Cycles the open panel editor's dropdown selection by the given step,
    /// wrapping at both ends.
    /// </summary>
    private void CyclePanelEditor(int delta)
    {
        switch (_editorKind)
        {
            case ManageProfilesRowKind.Country:
                SelectedCountryIndex = WrapIndex(SelectedCountryIndex, Countries.Count, delta);
                break;
            case ManageProfilesRowKind.Language:
                SelectedLanguageIndex = WrapIndex(SelectedLanguageIndex, Languages.Count, delta);
                break;
            case ManageProfilesRowKind.SubscriptionTier:
                SelectedSubscriptionTierIndex =
                    WrapIndex(SelectedSubscriptionTierIndex, SubscriptionTiers.Count, delta);
                break;
        }
    }

    /// <summary>
    /// Steps a dropdown index by the given delta, wrapping at both ends.
    /// </summary>
    private static int WrapIndex(int current, int count, int delta)
    {
        if (count <= 0)
        {
            return current;
        }

        return (Math.Max(current, 0) + delta + count) % count;
    }

    /// <summary>
    /// Commits the open panel editor's selection and closes it.
    /// </summary>
    private void CommitPanelEditor()
    {
        ManageProfilesRowKind? kind = _editorKind;
        ClosePanelEditor();
        Logger.Debug<ManageProfilesViewModel>($"Committed {kind} editor");
    }

    /// <summary>
    /// Restores the open panel editor's original selection and closes it.
    /// </summary>
    private void CancelPanelEditor()
    {
        ManageProfilesRowKind? kind = _editorKind;
        switch (kind)
        {
            case ManageProfilesRowKind.Country:
                SelectedCountryIndex = _editorOriginalIndex;
                break;
            case ManageProfilesRowKind.Language:
                SelectedLanguageIndex = _editorOriginalIndex;
                break;
            case ManageProfilesRowKind.SubscriptionTier:
                SelectedSubscriptionTierIndex = _editorOriginalIndex;
                break;
        }

        ClosePanelEditor();
        Logger.Debug<ManageProfilesViewModel>($"Cancelled {kind} editor");
    }

    /// <summary>
    /// Closes the panel editor and notifies the view to close the native control.
    /// </summary>
    private void ClosePanelEditor()
    {
        _editorKind = null;
        PanelEditorClosed?.Invoke();
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
        ResetPanel();
        SelectionHelper.SelectOnly(Rows, row);
        CreateStub.IsSelected = false;
        SelectedProfile = row.Profile;
    }

    /// <summary>
    /// Selects the anchored create stub and clears the edit fields.
    /// </summary>
    private void SelectStub()
    {
        ResetPanel();
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