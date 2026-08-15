using System;
using System.IO;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using XeniaManager.BigScreen.Constants;
using XeniaManager.BigScreen.Utilities;
using XeniaManager.Core.Files;
using XeniaManager.Core.Logging;
using XeniaManager.Core.Models.Files.Gpd;
using XeniaManager.Core.Utilities;

namespace XeniaManager.BigScreen.ViewModels.Items;

/// <summary>
/// A single achievement row: name, gamerscore, description and unlock date,
/// with the achievement image shown only when unlocked (no spoilers).
/// </summary>
public partial class AchievementItemViewModel : ObservableObject, ISelectable
{
    private readonly GpdFile? _gpdFile;
    private Bitmap? _cachedImage;

    /// <summary>
    /// Whether the active profile has earned this achievement.
    /// </summary>
    public bool IsUnlocked => Achievement.IsEarned;

    /// <summary>
    /// Whether this achievement is a secret/hidden achievement (its name and
    /// description should stay hidden until unlocked).
    /// </summary>
    public bool IsSecret => !Achievement.ShowUnachieved;

    /// <summary>
    /// Whether this row is spoiler-gated: locked AND secret, so its name,
    /// description and gamerscore are hidden behind placeholders.
    /// </summary>
    public bool IsSpoilerGated => !IsUnlocked && IsSecret;

    /// <summary>
    /// Whether this row currently has selection in the achievements list.
    /// </summary>
    [ObservableProperty]
    public partial bool IsSelected { get; set; }

    /// <summary>
    /// Whether the achievement image can be decoded: the achievement is
    /// unlocked, a GPD is available and the achievement carries an image.
    /// </summary>
    public bool CanLoadImage => IsUnlocked && _gpdFile != null && Achievement.ImageId != 0;

    /// <summary>
    /// Whether the achievement image is available to show.
    /// </summary>
    public bool HasAchievementImage => AchievementImage != null;

    /// <summary>
    /// Whether a lock-open icon shows instead of the image (unlocked without art).
    /// </summary>
    public bool ShowLockOpenIcon => IsUnlocked && !HasAchievementImage;

    /// <summary>
    /// The Core achievement entry this row represents.
    /// </summary>
    public AchievementEntry Achievement { get; }

    /// <summary>
    /// The achievement's display name (raw GPD name, used for sorting).
    /// </summary>
    public string Name => Achievement.Name;

    /// <summary>
    /// The achievement name as shown: hidden behind a placeholder while
    /// spoiler-gated (secret locked achievements reveal surprises).
    /// </summary>
    public string DisplayName => IsSpoilerGated
        ? LocalizationHelper.GetText("GameModal.Achievements.HiddenName")
        : Achievement.Name;

    /// <summary>
    /// The gamerscore awarded by this achievement.
    /// </summary>
    public int Gamerscore => Achievement.Gamerscore;

    /// <summary>
    /// The achievement description (unlocked or locked variant); spoiler-gated
    /// rows show a spoiler warning instead of the real text.
    /// </summary>
    public string Description => IsSpoilerGated
        ? LocalizationHelper.GetText("GameModal.Achievements.SpoilerWarning")
        : Achievement.IsEarned
            ? Achievement.UnlockedDescription
            : Achievement.LockedDescription;

    /// <summary>
    /// The unlock date, or a "not unlocked" label when locked.
    /// </summary>
    public string UnlockDateDisplay => Achievement.UnlockDateTime?.ToString(FormatConstants.AchievementUnlockFormat)
                                       ?? LocalizationHelper.GetText("GameModal.Achievements.NotUnlocked");

    /// <summary>
    /// Decodes the achievement image from the GPD. Returns null when the
    /// achievement is locked (spoiler guard), has no image or the decode fails.
    /// </summary>
    private Bitmap? LoadImage()
    {
        if (!CanLoadImage)
        {
            return null;
        }

        try
        {
            ImageEntry? image = _gpdFile!.GetImage(Achievement.ImageId);
            if (image == null)
            {
                return null;
            }

            using MemoryStream stream = new(image.ImageData);
            return new Bitmap(stream);
        }
        catch (Exception ex)
        {
            Logger.Warning<AchievementItemViewModel>($"Failed to decode achievement image '{Achievement.Name}'");
            Logger.LogExceptionDetails<AchievementItemViewModel>(ex);
            return null;
        }
    }

    /// <summary>
    /// The achievement image, decoded lazily and only when unlocked.
    /// </summary>
    public Bitmap? AchievementImage
    {
        get
        {
            if (_cachedImage == null)
            {
                _cachedImage = LoadImage();
            }

            return _cachedImage;
        }
    }

    public AchievementItemViewModel(AchievementEntry achievement, GpdFile? gpdFile)
    {
        Achievement = achievement;
        _gpdFile = gpdFile;
    }
}