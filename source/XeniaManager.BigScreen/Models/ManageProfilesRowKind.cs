namespace XeniaManager.BigScreen.Models;

/// <summary>
/// The kinds of rows in the Manage Profiles edit panel, in display order.
/// The controller enters the panel from a profile row and navigates these
/// rows like the settings screen.
/// </summary>
public enum ManageProfilesRowKind
{
    Gamertag,
    Country,
    Language,
    LiveToggle,
    SubscriptionTier,
    Save
}
