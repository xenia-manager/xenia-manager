using System;
using System.IO;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using XeniaManager.BigScreen.Constants;
using XeniaManager.BigScreen.Utilities;
using XeniaManager.Files;
using XeniaManager.Logging;
using XeniaManager.Files.Models.Gpd;
using XeniaManager.Core.Utilities;

namespace XeniaManager.BigScreen.ViewModels.Items;

/// <summary>
/// A single achievement row: name, gamerscore, description and unlock date.
/// The image shows in full color when unlocked and black and white when
/// locked (secret locked achievements stay hidden behind the lock icon).
/// </summary>
public partial class AchievementItemViewModel : ObservableObject, ISelectable
{
    private readonly GpdFile? _gpdFile;
    private Bitmap? _cachedImage;

    /// <summary>
    /// Whether the active profile has earned this achievement.
    /// </summary>
    public bool IsUnlocked
    {
        get
        {
            return Achievement.IsEarned;
        }
    }

    /// <summary>
    /// Whether this achievement is a secret/hidden achievement (its name and
    /// description should stay hidden until unlocked).
    /// </summary>
    public bool IsSecret
    {
        get
        {
            return !Achievement.ShowUnachieved;
        }
    }

    /// <summary>
    /// Whether this row is spoiler-gated: locked AND secret, so its name,
    /// description and gamerscore are hidden behind placeholders.
    /// </summary>
    public bool IsSpoilerGated
    {
        get
        {
            return !IsUnlocked && IsSecret;
        }
    }

    /// <summary>
    /// Whether this row currently has selection in the achievements list.
    /// </summary>
    [ObservableProperty]
    public partial bool IsSelected { get; set; }

    /// <summary>
    /// Whether the achievement image can be decoded: a GPD is available and
    /// the achievement carries an image.
    /// </summary>
    public bool CanLoadImage
    {
        get
        {
            return _gpdFile != null && Achievement.ImageId != 0;
        }
    }

    /// <summary>
    /// Whether the achievement image is available to show.
    /// </summary>
    public bool HasAchievementImage
    {
        get
        {
            return AchievementImage != null;
        }
    }

    /// <summary>
    /// Whether a lock icon shows instead of the image (locked without art,
    /// or a locked secret hiding its surprise).
    /// </summary>
    public bool ShowLockedIcon
    {
        get
        {
            return !IsUnlocked && !HasAchievementImage;
        }
    }

    /// <summary>
    /// Whether a lock-open icon shows instead of the image (unlocked without art).
    /// </summary>
    public bool ShowLockOpenIcon
    {
        get
        {
            return IsUnlocked && !HasAchievementImage;
        }
    }

    /// <summary>
    /// The Core achievement entry this row represents.
    /// </summary>
    public AchievementEntry Achievement { get; }

    /// <summary>
    /// The achievement's display name (raw GPD name, used for sorting).
    /// </summary>
    public string Name
    {
        get
        {
            return Achievement.Name;
        }
    }

    /// <summary>
    /// The achievement name as shown: hidden behind a placeholder while
    /// spoiler-gated (secret locked achievements reveal surprises).
    /// </summary>
    public string DisplayName
    {
        get
        {
            return IsSpoilerGated
                ? LocalizationHelper.GetText("GameModal.Achievements.HiddenName")
                : Achievement.Name;
        }
    }

    /// <summary>
    /// The gamerscore awarded by this achievement.
    /// </summary>
    public int Gamerscore
    {
        get
        {
            return Achievement.Gamerscore;
        }
    }

    /// <summary>
    /// The achievement description (unlocked or locked variant); spoiler-gated
    /// rows show a spoiler warning instead of the real text.
    /// </summary>
    public string Description
    {
        get
        {
            return IsSpoilerGated
                ? LocalizationHelper.GetText("GameModal.Achievements.SpoilerWarning")
                : Achievement.IsEarned
                    ? Achievement.UnlockedDescription
                    : Achievement.LockedDescription;
        }
    }

    /// <summary>
    /// The unlock date, or a "not unlocked" label when locked.
    /// </summary>
    public string UnlockDateDisplay
    {
        get
        {
            return Achievement.UnlockDateTime?.ToString(FormatConstants.AchievementUnlockFormat)
                   ?? LocalizationHelper.GetText("GameModal.Achievements.NotUnlocked");
        }
    }

    /// <summary>
    /// Decodes the achievement image from the GPD: full color when unlocked,
    /// black and white when locked. Returns null when there is no image,
    /// decoding fails, or a locked secret hides its surprise.
    /// </summary>
    private Bitmap? LoadImage()
    {
        if (!CanLoadImage || (!IsUnlocked && IsSpoilerGated))
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

            using MemoryStream stream = new MemoryStream(image.ImageData);
            // WriteableBitmap derives from Bitmap, keeping the declared type.
            return IsUnlocked ? new Bitmap(stream) : GrayscaleImage.ToGrayscale(image.ImageData);
        }
        catch (Exception ex)
        {
            Logger.Warning<AchievementItemViewModel>($"Failed to decode achievement image '{Achievement.Name}'");
            Logger.LogExceptionDetails<AchievementItemViewModel>(ex);
            return null;
        }
    }

    /// <summary>
    /// The achievement image, decoded lazily (grayscale while locked).
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