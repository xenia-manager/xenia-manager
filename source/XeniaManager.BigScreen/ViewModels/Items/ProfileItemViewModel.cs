using CommunityToolkit.Mvvm.ComponentModel;
using XeniaManager.BigScreen.Utilities;
using XeniaManager.Core.Models;
using XeniaManager.Files.Models.Account;

namespace XeniaManager.BigScreen.ViewModels.Items;

/// <summary>
/// A single profile row in the profile picker / manage profiles list.
/// </summary>
public partial class ProfileItemViewModel : ObservableObject, ISelectable
{
    /// <summary>
    /// The Core profile this row represents.
    /// </summary>
    public AccountInfo Profile { get; }

    /// <summary>
    /// The profile's gamertag.
    /// </summary>
    public string Gamertag
    {
        get
        {
            return Profile.Gamertag;
        }
    }

    /// <summary>
    /// The profile's country and language as a single detail line (e.g. "United States · English").
    /// </summary>
    public string DetailsText
    {
        get
        {
            return
                $"{new EnumDisplayItem<XboxLiveCountry>(Profile.Country).DisplayName} · {new EnumDisplayItem<ConsoleLanguage>(Profile.Language).DisplayName}";
        }
    }

    /// <summary>
    /// The profile's total gamerscore, or empty until loaded from its GPD.
    /// </summary>
    [ObservableProperty]
    public partial string GamerscoreText { get; set; } = string.Empty;

    /// <summary>
    /// Whether the profile's gamerscore has been resolved from its GPD.
    /// </summary>
    public bool HasGamerscore
    {
        get
        {
            return !string.IsNullOrEmpty(GamerscoreText);
        }
    }

    /// <summary>
    /// Whether this is the active profile.
    /// </summary>
    [ObservableProperty]
    public partial bool IsActive { get; set; }

    /// <summary>
    /// Whether this row currently has selection.
    /// </summary>
    [ObservableProperty]
    public partial bool IsSelected { get; set; }

    public ProfileItemViewModel(AccountInfo profile, bool isActive = false)
    {
        Profile = profile;
        IsActive = isActive;
    }

    partial void OnGamerscoreTextChanged(string value) => OnPropertyChanged(nameof(HasGamerscore));
}