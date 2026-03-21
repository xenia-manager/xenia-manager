using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using XeniaManager.Core.Logging;
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
    /// The selected country index for the ComboBox.
    /// </summary>
    [ObservableProperty] private int _selectedCountryIndex;

    /// <summary>
    /// The selected language index for the ComboBox.
    /// </summary>
    [ObservableProperty] private int _selectedLanguageIndex;

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
            SelectedCountryIndex = Countries.IndexOf(Countries.FirstOrDefault(c => c.Value == XboxLiveCountry.Unknown) ?? Countries.First());
            SelectedLanguageIndex = Languages.IndexOf(Languages.FirstOrDefault(l => l.Value == ConsoleLanguage.English) ?? Languages.First());
            CanSave = false;
            return;
        }

        SelectedProfile = Profiles.Find(p => p.Gamertag == SelectedGamertag.Gamertag);
        if (SelectedProfile != null)
        {
            EditGamertag = SelectedProfile.Gamertag;
            SelectedCountryIndex = Countries.IndexOf(Countries.FirstOrDefault(c => c.Value == SelectedProfile.Country) ?? Countries.First());
            SelectedLanguageIndex = Languages.IndexOf(Languages.FirstOrDefault(l => l.Value == SelectedProfile.Language) ?? Languages.First());
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
        if (SelectedProfile == null || SelectedCountryIndex < 0 || SelectedLanguageIndex < 0)
        {
            return;
        }

        // Update the profile with edited values
        SelectedProfile.Gamertag = EditGamertag;
        SelectedProfile.Country = Countries[SelectedCountryIndex].Value;
        SelectedProfile.Language = Languages[SelectedLanguageIndex].Value;

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

    /// <summary>
    /// Exports the selected profile.
    /// </summary>
    [RelayCommand]
    private async Task ExportProfile()
    {
        if (SelectedProfile == null || SelectedGamertag == null)
        {
            return;
        }

        // Get the storage provider
        Window? topLevel = App.MainWindow;
        if (topLevel == null)
        {
            await _messageBoxService.ShowErrorAsync(
                LocalizationHelper.GetText("ManageProfilesDialog.ExportProfile.Error.Title"),
                LocalizationHelper.GetText("ManageProfilesDialog.ExportProfile.Error.WindowError"),
                MessageBoxDialogType.TaskDialog);
            return;
        }

        IStorageProvider storageProvider = topLevel.StorageProvider;

        // Ask the user where to save the file
        IStorageFile? file = await storageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = LocalizationHelper.GetText("ManageProfilesDialog.ExportProfile.FilePicker.Title"),
            FileTypeChoices =
            [
                new FilePickerFileType("ZIP Archive")
                {
                    Patterns = ["*.zip"]
                }
            ],
            SuggestedFileName = $"{SelectedGamertag.Gamertag} - {SelectedGamertag.DisplayXuid.ToString()}.zip",
            DefaultExtension = "zip",
            ShowOverwritePrompt = true
        });

        // If user canceled, return early
        if (file == null)
        {
            return;
        }

        // Ask if the user wants to export saves too
        bool exportSaves = await _messageBoxService.ShowConfirmationAsync(
            LocalizationHelper.GetText("ManageProfilesDialog.ExportProfile.Confirmation.Title"),
            LocalizationHelper.GetText("ManageProfilesDialog.ExportProfile.Confirmation.Message"));

        try
        {
            string zipPath = file.Path.LocalPath;
            bool result = await ProfileManager.ExportProfile(XeniaVersion, SelectedProfile, exportSaves, zipPath);

            if (result)
            {
                await _messageBoxService.ShowInfoAsync(
                    LocalizationHelper.GetText("ManageProfilesDialog.ExportProfile.Success.Title"),
                    string.Format(LocalizationHelper.GetText("ManageProfilesDialog.ExportProfile.Success.Message"), SelectedProfile.Gamertag, zipPath));
            }
            else
            {
                await _messageBoxService.ShowErrorAsync(
                    LocalizationHelper.GetText("ManageProfilesDialog.ExportProfile.Error.Title"),
                    LocalizationHelper.GetText("ManageProfilesDialog.ExportProfile.Error.Failed.Message"));
            }
        }
        catch (Exception ex)
        {
            Logger.Error<ManageProfilesDialogViewModel>("Failed to export profile");
            Logger.LogExceptionDetails<ManageProfilesDialogViewModel>(ex);
            await _messageBoxService.ShowErrorAsync(
                LocalizationHelper.GetText("ManageProfilesDialog.ExportProfile.Error.Title"),
                ex.Message);
        }
    }

    /// <summary>
    /// Imports a profile from an external source.
    /// </summary>
    [RelayCommand]
    private async Task ImportProfile()
    {
        // Get the storage provider
        Window? topLevel = App.MainWindow;
        if (topLevel == null)
        {
            await _messageBoxService.ShowErrorAsync(
                LocalizationHelper.GetText("ManageProfilesDialog.ImportProfile.Error.Title"),
                LocalizationHelper.GetText("ManageProfilesDialog.ImportProfile.Error.WindowError"),
                MessageBoxDialogType.TaskDialog);
            return;
        }

        IStorageProvider storageProvider = topLevel.StorageProvider;

        // Ask the user to select a zip file
        IReadOnlyList<IStorageFile> files = await storageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = LocalizationHelper.GetText("ManageProfilesDialog.ImportProfile.FilePicker.Title"),
            FileTypeFilter =
            [
                new FilePickerFileType("ZIP Archive")
                {
                    Patterns = ["*.zip"]
                }
            ],
            AllowMultiple = false
        });

        // If user canceled, return early
        if (files.Count == 0)
        {
            return;
        }

        try
        {
            string zipPath = files[0].Path.LocalPath;

            // Track if a profile was replaced
            AccountInfo? replacedProfile = null;

            // Import the profile with replacement handling
            AccountInfo? importedProfile = await ProfileManager.ImportProfileWithReplacement(XeniaVersion, zipPath, Profiles, async (existingProfile) =>
            {
                // Store reference to the profile being replaced
                replacedProfile = existingProfile;

                // This callback is invoked when a profile with the same XUID already exists
                string confirmationMessage = string.Format(LocalizationHelper.GetText("ProfileManager.Import.ConfirmReplace.Message"),
                    existingProfile.Gamertag,
                    existingProfile.PathXuid?.ToString() ?? "Unknown");

                bool replace = await _messageBoxService.ShowConfirmationAsync(LocalizationHelper.GetText("ProfileManager.Import.ConfirmReplace.Title"),
                    confirmationMessage);

                return replace;
            });

            if (importedProfile != null)
            {
                // If a profile was replaced, remove the old ProfileDisplayInfo from Gamertags
                if (replacedProfile != null)
                {
                    ProfileDisplayInfo? oldDisplayInfo = Gamertags.FirstOrDefault(g => g.DisplayXuid == replacedProfile.PathXuid);
                    if (oldDisplayInfo != null)
                    {
                        Gamertags.Remove(oldDisplayInfo);
                    }
                }

                // Add the imported profile to the gamertags list
                ProfileDisplayInfo newDisplayInfo = new ProfileDisplayInfo(importedProfile.Gamertag, importedProfile.PathXuid, importedProfile.Xuid);
                Gamertags.Add(newDisplayInfo);

                // Select the newly imported profile
                SelectedGamertag = newDisplayInfo;

                await _messageBoxService.ShowInfoAsync(LocalizationHelper.GetText("ManageProfilesDialog.ImportProfile.Success.Title"),
                    string.Format(LocalizationHelper.GetText("ManageProfilesDialog.ImportProfile.Success.Message"), importedProfile.Gamertag));
            }
            else
            {
                // Import was canceled or failed (e.g., the user chose not to replace the profile)
                await _messageBoxService.ShowInfoAsync(LocalizationHelper.GetText("ManageProfilesDialog.ImportProfile.Canceled.Title"),
                    LocalizationHelper.GetText("ManageProfilesDialog.ImportProfile.Canceled.Message"));
            }
        }
        catch (Exception ex)
        {
            Logger.Error<ManageProfilesDialogViewModel>("Failed to import profile");
            Logger.LogExceptionDetails<ManageProfilesDialogViewModel>(ex);
            await _messageBoxService.ShowErrorAsync(LocalizationHelper.GetText("ManageProfilesDialog.ImportProfile.Error.Title"),
                string.Format(LocalizationHelper.GetText("ManageProfilesDialog.ImportProfile.Error.Failed.Message"), ex.Message));
        }
    }
}