using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using XeniaManager.Core.Manage;
using XeniaManager.Core.Models;
using XeniaManager.Core.Models.Files.Account;
using XeniaManager.Core.Utilities;
using XeniaManager.Services;

namespace XeniaManager.ViewModels.Controls;

/// <summary>
/// ViewModel for the manage profiles dialog.
/// Manages the collection of account profiles and handles profile editing.
/// </summary>
public partial class ManageProfilesDialogViewModel : ViewModelBase
{
    /// <summary>
    /// Regex pattern for valid gamertag format.
    /// Must start with a letter, can contain letters and numbers, spaces allowed only between words.
    /// </summary>
    private static readonly Regex GamertagRegex = new Regex(@"^[A-Za-z][A-Za-z0-9]*( [A-Za-z0-9]+)*$");

    /// <summary>
    /// Maximum allowed length for a gamertag.
    /// </summary>
    private const int MaxGamertagLength = 15;

    /// <summary>
    /// The Xenia version to use for profile management.
    /// </summary>
    [ObservableProperty] private XeniaVersion _xeniaVersion = XeniaVersion.Canary;

    /// <summary>
    /// The list of account profiles to manage.
    /// </summary>
    [ObservableProperty] private List<AccountInfo> _profiles = [];

    /// <summary>
    /// The list of gamertags for the combobox.
    /// </summary>
    [ObservableProperty] private ObservableCollection<ProfileDisplayInfo> _gamertags = [];

    /// <summary>
    /// The currently selected gamertag.
    /// </summary>
    [ObservableProperty] private ProfileDisplayInfo? _selectedGamertag;

    /// <summary>
    /// The currently selected profile.
    /// </summary>
    [ObservableProperty] private AccountInfo? _selectedProfile;

    /// <summary>
    /// The gamertag for editing.
    /// </summary>
    [ObservableProperty] private string _editGamertag = string.Empty;

    /// <summary>
    /// The selected country display item for the ComboBox.
    /// </summary>
    [ObservableProperty] private EnumDisplayItem<XboxLiveCountry>? _selectedCountry;

    /// <summary>
    /// The selected language display item for the ComboBox.
    /// </summary>
    [ObservableProperty] private EnumDisplayItem<ConsoleLanguage>? _selectedLanguage;

    /// <summary>
    /// The list of available countries for the ComboBox.
    /// </summary>
    public ObservableCollection<EnumDisplayItem<XboxLiveCountry>> Countries { get; }

    /// <summary>
    /// The list of available languages for the ComboBox.
    /// </summary>
    public ObservableCollection<EnumDisplayItem<ConsoleLanguage>> Languages { get; }

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
    /// The message box service for showing confirmation dialogs.
    /// </summary>
    private readonly IMessageBoxService _messageBoxService;

    partial void OnSelectedGamertagChanged(ProfileDisplayInfo? value)
    {
        LoadSelectedProfile();
    }

    partial void OnEditGamertagChanged(string value)
    {
        ValidateGamertag();
    }

    /// <summary>
    /// Validates the gamertag according to the following rules:
    /// - Cannot be empty
    /// - Cannot be longer than 15 characters
    /// - Must start with a letter
    /// - Can contain letters and numbers
    /// - Spaces allowed only between words (not at start/end, no consecutive spaces)
    /// </summary>
    /// <returns>True if the gamertag is valid, false otherwise.</returns>
    private void ValidateGamertag()
    {
        HasGamertagError = false;
        GamertagErrorMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(EditGamertag))
        {
            HasGamertagError = true;
            GamertagErrorMessage = LocalizationHelper.GetText("ManageProfilesDialog.Edit.Gamertag.Error.Empty");
            CanSave = false;
            return;
        }

        if (EditGamertag.Length > MaxGamertagLength)
        {
            HasGamertagError = true;
            GamertagErrorMessage = string.Format(LocalizationHelper.GetText("ManageProfilesDialog.Edit.Gamertag.Error.TooLong"), MaxGamertagLength);
            CanSave = false;
            return;
        }

        if (!GamertagRegex.IsMatch(EditGamertag))
        {
            HasGamertagError = true;
            GamertagErrorMessage = LocalizationHelper.GetText("ManageProfilesDialog.Edit.Gamertag.Error.InvalidFormat");
            CanSave = false;
            return;
        }

        CanSave = true;
    }

    /// <summary>
    /// Loads the selected profile data into the edit fields.
    /// </summary>
    private void LoadSelectedProfile()
    {
        if (SelectedGamertag == null)
        {
            SelectedProfile = null;
            EditGamertag = string.Empty;
            SelectedCountry = Countries.FirstOrDefault(c => c.Value == XboxLiveCountry.Unknown);
            SelectedLanguage = Languages.FirstOrDefault(l => l.Value == ConsoleLanguage.English);
            CanSave = false;
            return;
        }

        SelectedProfile = Profiles.Find(p => p.Gamertag == SelectedGamertag.Gamertag);
        if (SelectedProfile != null)
        {
            EditGamertag = SelectedProfile.Gamertag;
            SelectedCountry = Countries.FirstOrDefault(c => c.Value == SelectedProfile.Country);
            SelectedLanguage = Languages.FirstOrDefault(l => l.Value == SelectedProfile.Language);
            ValidateGamertag();
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ManageProfilesDialogViewModel"/> class.
    /// </summary>
    public ManageProfilesDialogViewModel()
    {
        Profiles = [];
        Gamertags = [];
        Countries = new ObservableCollection<EnumDisplayItem<XboxLiveCountry>>(
            Enum.GetValues<XboxLiveCountry>().Select(v => new EnumDisplayItem<XboxLiveCountry>(v)));
        Languages = new ObservableCollection<EnumDisplayItem<ConsoleLanguage>>(
            Enum.GetValues<ConsoleLanguage>().Select(v => new EnumDisplayItem<ConsoleLanguage>(v)));
        _messageBoxService = App.Services.GetRequiredService<IMessageBoxService>();
    }

    /// <summary>
    /// Loads profiles into the dialog.
    /// </summary>
    /// <param name="profiles">The list of profiles to load.</param>
    /// <param name="version">The Xenia version.</param>
    public void LoadProfiles(List<AccountInfo> profiles, XeniaVersion version)
    {
        Profiles = profiles;
        XeniaVersion = version;
        Gamertags = new ObservableCollection<ProfileDisplayInfo>(Profiles.ConvertAll(p => new ProfileDisplayInfo(p.Gamertag, p.PathXuid, p.Xuid)));
        SelectedGamertag = Gamertags.Count > 0 ? Gamertags[0] : null;
    }

    /// <summary>
    /// Creates a new account with the default name "New User".
    /// </summary>
    [RelayCommand]
    private void CreateAccount()
    {
        AccountInfo newAccount = ProfileManager.CreateAccount(XeniaVersion, "New User");
        Profiles.Add(newAccount);

        ProfileDisplayInfo newDisplayInfo = new ProfileDisplayInfo(newAccount.Gamertag, newAccount.PathXuid, newAccount.Xuid);
        Gamertags.Add(newDisplayInfo);
        SelectedGamertag = newDisplayInfo;
    }

    /// <summary>
    /// Deletes the selected account after confirmation.
    /// </summary>
    [RelayCommand]
    private async Task DeleteAccount()
    {
        if (SelectedProfile == null || SelectedGamertag == null)
        {
            return;
        }

        // Show confirmation dialog
        string confirmationMessage = string.Format(
            LocalizationHelper.GetText("ManageProfilesDialog.DeleteAccount.Confirmation.Message"),
            SelectedProfile.Gamertag);

        bool confirmed = await _messageBoxService.ShowConfirmationAsync(
            LocalizationHelper.GetText("ManageProfilesDialog.DeleteAccount.Confirmation.Title"),
            confirmationMessage);

        if (!confirmed)
        {
            return;
        }

        // Delete the account
        bool success = ProfileManager.DeleteAccount(XeniaVersion, SelectedProfile);

        if (success)
        {
            // Remove from collections
            Profiles.Remove(SelectedProfile);
            Gamertags.Remove(SelectedGamertag);

            // Clear selection
            SelectedProfile = null;
            SelectedGamertag = null;
            EditGamertag = string.Empty;

            // Select first remaining profile if any exist
            if (Gamertags.Count > 0)
            {
                SelectedGamertag = Gamertags[0];
            }
        }
        else
        {
            await _messageBoxService.ShowErrorAsync(
                LocalizationHelper.GetText("ManageProfilesDialog.DeleteAccount.Error.Title"),
                LocalizationHelper.GetText("ManageProfilesDialog.DeleteAccount.Error.Message"));
        }
    }

    /// <summary>
    /// Saves the edited profile data to the profiles list.
    /// </summary>
    [RelayCommand]
    private void Save()
    {
        if (SelectedProfile == null || SelectedCountry == null || SelectedLanguage == null)
        {
            return;
        }

        // Update the profile with edited values
        SelectedProfile.Gamertag = EditGamertag;
        SelectedProfile.Country = SelectedCountry.Value;
        SelectedProfile.Language = SelectedLanguage.Value;

        // Update the gamertag in the gamertags list
        if (SelectedGamertag != null)
        {
            int gamertagIndex = Gamertags.IndexOf(SelectedGamertag);
            if (gamertagIndex >= 0)
            {
                ProfileDisplayInfo newDisplayInfo = new ProfileDisplayInfo(EditGamertag, SelectedProfile.PathXuid, SelectedProfile.Xuid);
                Gamertags[gamertagIndex] = newDisplayInfo;
                SelectedGamertag = newDisplayInfo;
            }
        }
    }
}