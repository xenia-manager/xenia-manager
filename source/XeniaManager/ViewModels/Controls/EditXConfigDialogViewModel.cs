using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using XeniaManager.Core.Files;
using XeniaManager.Core.Logging;
using XeniaManager.Core.Manage;
using XeniaManager.Core.Models;
using XeniaManager.Core.Models.Files.Account;
using XeniaManager.Core.Models.Files.XConfig;

namespace XeniaManager.ViewModels.Controls;

/// <summary>
/// ViewModel for the Edit XConfig dialog.
/// Manages the XConfig settings (resolution, language, country, default profile)
/// and provides commands for saving changes back to the XConfig file.
/// </summary>
public partial class EditXConfigDialogViewModel : ViewModelBase
{
    /// <summary>
    /// Sentinel entry representing "no profile selected" in the default profile combobox.
    /// Uses XUID 0 which maps to no default profile in XConfig.
    /// </summary>
    private static readonly ProfileDisplayInfo NoneProfile = new ProfileDisplayInfo("None", new AccountXuid(0), new AccountXuid(0));

    /// <summary>
    /// The XConfigFile instance being edited.
    /// </summary>
    [ObservableProperty] private XConfigFile _xconfig;

    /// <summary>
    /// The Xenia version this XConfig belongs to.
    /// </summary>
    [ObservableProperty] private XeniaVersion _xeniaVersion;

    /// <summary>
    /// Selected index for the resolution combobox.
    /// </summary>
    [ObservableProperty] private int _selectedResolutionIndex = -1;

    /// <summary>
    /// Selected index for the language combobox.
    /// </summary>
    [ObservableProperty] private int _selectedLanguageIndex = -1;

    /// <summary>
    /// Selected index for the country combobox.
    /// </summary>
    [ObservableProperty] private int _selectedCountryIndex = -1;

    /// <summary>
    /// Selected index for the default profile combobox.
    /// </summary>
    [ObservableProperty] private int _selectedProfileIndex = -1;

    /// <summary>
    /// Available resolution options for the combobox.
    /// </summary>
    public ObservableCollection<EnumDisplayItem<XConfigResolution>> Resolutions { get; }

    /// <summary>
    /// Available language options for the combobox.
    /// </summary>
    public ObservableCollection<EnumDisplayItem<XLanguage>> Languages { get; }

    /// <summary>
    /// Available country options for the combobox.
    /// </summary>
    public ObservableCollection<EnumDisplayItem<XOnlineCountry>> Countries { get; }

    /// <summary>
    /// Available profile options for the default profile combobox, including "None" as the first entry.
    /// </summary>
    public ObservableCollection<ProfileDisplayInfo> Profiles { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="EditXConfigDialogViewModel"/> class.
    /// </summary>
    public EditXConfigDialogViewModel()
    {
        Logger.Trace<EditXConfigDialogViewModel>("Creating EditXConfigDialogViewModel");

        _xconfig = XConfigFile.Create();
        _xeniaVersion = XeniaVersion.Canary;

        // Populate enum-based combobox collections
        Resolutions = new ObservableCollection<EnumDisplayItem<XConfigResolution>>(
            Enum.GetValues<XConfigResolution>().Select(v => new EnumDisplayItem<XConfigResolution>(v)));
        Languages = new ObservableCollection<EnumDisplayItem<XLanguage>>(
            Enum.GetValues<XLanguage>().Select(v => new EnumDisplayItem<XLanguage>(v)));
        Countries = new ObservableCollection<EnumDisplayItem<XOnlineCountry>>(
            Enum.GetValues<XOnlineCountry>().Select(v => new EnumDisplayItem<XOnlineCountry>(v)));

        // Start with "None" as the default profile option
        Profiles = [NoneProfile];

        Logger.Debug<EditXConfigDialogViewModel>($"Initialized with {Resolutions.Count} resolutions, {Languages.Count} languages, {Countries.Count} countries");
    }

    /// <summary>
    /// Loads the current XConfig settings and available profiles into the dialog's combobox selections.
    /// </summary>
    /// <param name="accounts">The list of available profiles for the selected Xenia version.</param>
    public void LoadXConfig(List<AccountInfo> accounts)
    {
        Logger.Trace<EditXConfigDialogViewModel>("Loading XConfig settings into dialog");

        // Match current XConfig values to combobox indices
        SelectedResolutionIndex = FindIndex(Resolutions, r => (int)r.Value == (int)Xconfig.AvHdmiScreenSize);
        SelectedLanguageIndex = FindIndex(Languages, l => (uint)l.Value == (uint)Xconfig.Language);
        SelectedCountryIndex = FindIndex(Countries, c => (byte)c.Value == (byte)Xconfig.Country);

        Logger.Debug<EditXConfigDialogViewModel>($"Initial selection indices - Resolution: {SelectedResolutionIndex}, Language: {SelectedLanguageIndex}, Country: {SelectedCountryIndex}");

        // Add available profiles to the combobox
        foreach (AccountInfo account in accounts)
        {
            Profiles.Add(new ProfileDisplayInfo(account.Gamertag, account.PathXuid, account.Xuid));
        }

        Logger.Debug<EditXConfigDialogViewModel>($"Added {accounts.Count} profiles to selection list");

        // Select the matching profile or "None" if no default profile is set
        if (Xconfig.DefaultProfile != 0)
        {
            SelectedProfileIndex = FindIndex(Profiles, p => p.DisplayXuid.Value == Xconfig.DefaultProfile);
            if (SelectedProfileIndex < 0)
            {
                Logger.Debug<EditXConfigDialogViewModel>($"Default profile XUID 0x{Xconfig.DefaultProfile:X16} not found in profiles, defaulting to None");
                SelectedProfileIndex = 0;
            }
            else
            {
                Logger.Debug<EditXConfigDialogViewModel>($"Found default profile at index {SelectedProfileIndex}");
            }
        }
        else
        {
            Logger.Debug<EditXConfigDialogViewModel>("No default profile set, defaulting to None");
            SelectedProfileIndex = 0;
        }
    }

    /// <summary>
    /// Finds the index of the first element in a collection matching the given predicate.
    /// </summary>
    /// <typeparam name="T">The type of elements in the collection.</typeparam>
    /// <param name="collection">The collection to search.</param>
    /// <param name="predicate">The predicate to match.</param>
    /// <returns>The index of the first match, or -1 if no match is found.</returns>
    private static int FindIndex<T>(ObservableCollection<T> collection, Func<T, bool> predicate)
    {
        for (int i = 0; i < collection.Count; i++)
        {
            if (predicate(collection[i]))
            {
                return i;
            }
        }
        return -1;
    }

    /// <summary>
    /// Saves the current dialog selections back to the XConfig file and persists it to disk.
    /// </summary>
    [RelayCommand]
    private void Save()
    {
        Logger.Trace<EditXConfigDialogViewModel>("Starting Save operation");

        // Apply resolution selection
        if (SelectedResolutionIndex >= 0 && SelectedResolutionIndex < Resolutions.Count)
        {
            XConfigResolution resolution = Resolutions[SelectedResolutionIndex].Value;
            Logger.Debug<EditXConfigDialogViewModel>($"Setting resolution to {resolution}");
            Xconfig.AvHdmiScreenSize = resolution;
        }

        // Apply language selection
        if (SelectedLanguageIndex >= 0 && SelectedLanguageIndex < Languages.Count)
        {
            XLanguage language = Languages[SelectedLanguageIndex].Value;
            Logger.Debug<EditXConfigDialogViewModel>($"Setting language to {language}");
            Xconfig.Language = language;
        }

        // Apply country selection
        if (SelectedCountryIndex >= 0 && SelectedCountryIndex < Countries.Count)
        {
            XOnlineCountry country = Countries[SelectedCountryIndex].Value;
            Logger.Debug<EditXConfigDialogViewModel>($"Setting country to {country}");
            Xconfig.Country = country;
        }

        // Apply default profile selection
        if (SelectedProfileIndex >= 0 && SelectedProfileIndex < Profiles.Count)
        {
            ulong profileXuid = Profiles[SelectedProfileIndex].DisplayXuid.Value;
            Logger.Debug<EditXConfigDialogViewModel>($"Setting default profile XUID to 0x{profileXuid:X16}");
            Xconfig.DefaultProfile = profileXuid;
        }

        Logger.Info<EditXConfigDialogViewModel>("Saving XConfig file to disk");
        XConfigManager.SaveXConfig(Xconfig, XeniaVersion);
        Logger.Info<EditXConfigDialogViewModel>("XConfig file saved successfully");
    }
}