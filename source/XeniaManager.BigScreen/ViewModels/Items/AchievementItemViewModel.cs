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
    /// The Core achievement entry this row represents.
    /// </summary>
    public AchievementEntry Achievement { get; }

    /// <summary>
    /// Whether this row currently has selection in the achievements list.
    /// </summary>
    [ObservableProperty] private bool _isSelected;

    /// <summary>
    /// The achievement's display name.
    /// </summary>
    public string Name => Achievement.Name;

    /// <summary>
    /// The gamerscore awarded by this achievement.
    /// </summary>
    public int Gamerscore => Achievement.Gamerscore;

    /// <summary>
    /// The achievement description (unlocked or locked variant).
    /// </summary>
    public string Description => Achievement.IsEarned
        ? Achievement.UnlockedDescription
        : Achievement.LockedDescription;

    /// <summary>
    /// Whether the active profile has earned this achievement.
    /// </summary>
    public bool IsUnlocked => Achievement.IsEarned;

    /// <summary>
    /// The unlock date, or a "not unlocked" label when locked.
    /// </summary>
    public string UnlockDateDisplay => Achievement.UnlockDateTime?.ToString(FormatConstants.AchievementUnlockFormat)
        ?? LocalizationHelper.GetText("GameModal.Achievements.NotUnlocked");

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

    /// <summary>
    /// Whether the achievement image is available to show.
    /// </summary>
    public bool HasAchievementImage => AchievementImage != null;

    /// <summary>
    /// Whether a lock-open icon shows instead of the image (unlocked without art).
    /// </summary>
    public bool ShowLockOpenIcon => IsUnlocked && !HasAchievementImage;

    public AchievementItemViewModel(AchievementEntry achievement, GpdFile? gpdFile)
    {
        Achievement = achievement;
        _gpdFile = gpdFile;
    }

    /// <summary>
    /// Decodes the achievement image from the GPD. Returns null when the
    /// achievement is locked (spoiler guard), has no image or the decode fails.
    /// </summary>
    private Bitmap? LoadImage()
    {
        if (!IsUnlocked || _gpdFile == null || Achievement.ImageId == 0)
        {
            return null;
        }

        try
        {
            ImageEntry? image = _gpdFile.GetImage(Achievement.ImageId);
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
}
