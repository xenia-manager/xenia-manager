using System;
using System.IO;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using XeniaManager.Core.Files;
using XeniaManager.Core.Models.Files.Gpd;
using XeniaManager.Core.Utilities;

namespace XeniaManager.ViewModels.Items;

/// <summary>
/// ViewModel for displaying an achievement in the UI.
/// </summary>
public partial class AchievementViewModel : ViewModelBase
{
    /// <summary>
    /// The underlying achievement entry.
    /// </summary>
    public AchievementEntry Achievement { get; }

    /// <summary>
    /// The GPD file containing achievement images.
    /// </summary>
    private readonly GpdFile? _gpdFile;

    /// <summary>
    /// Cached achievement image.
    /// </summary>
    private IImage? _cachedImage;

    /// <summary>
    /// Gets the achievement ID.
    /// </summary>
    public uint AchievementId => Achievement.AchievementId;

    /// <summary>
    /// Gets the gamerscore value.
    /// </summary>
    public int Gamerscore => Achievement.Gamerscore;

    /// <summary>
    /// Gets the achievement name.
    /// </summary>
    public string Name => Achievement.Name;

    /// <summary>
    /// Gets the description (unlocked or locked based on status).
    /// </summary>
    public string Description => Achievement.IsEarned ? Achievement.UnlockedDescription : Achievement.LockedDescription;

    /// <summary>
    /// Gets whether the achievement is unlocked.
    /// </summary>
    public bool IsUnlocked => Achievement.IsEarned;

    /// <summary>
    /// Gets the unlocked date time (null if not unlocked).
    /// </summary>
    public DateTime? UnlockDateTime => Achievement.UnlockDateTime;

    /// <summary>
    /// Gets the formatted unlocked date.
    /// </summary>
    public string UnlockDateDisplay => UnlockDateTime?.ToString("yyyy-MM-dd HH:mm") ?? LocalizationHelper.GetText("InstalledContentDialog.NotUnlocked");

    /// <summary>
    /// Gets the icon symbol based on unlocked status.
    /// </summary>
    public string UnlockIcon => IsUnlocked ? "Checkmark" : "LockClosed";

    /// <summary>
    /// Gets the achievement image if available and unlocked, otherwise null.
    /// The image is cached after the first load to avoid repeated decoding.
    /// Images are only shown for unlocked achievements to avoid spoilers.
    /// </summary>
    public IImage? AchievementImage
    {
        get
        {
            // Don't show image for locked achievements (avoid spoilers)
            if (!IsUnlocked)
            {
                return null;
            }

            // Return a cached image if already loaded
            if (_cachedImage != null)
            {
                return _cachedImage;
            }

            // Load and decode the image
            if (_gpdFile == null || Achievement.ImageId == 0)
            {
                return null;
            }

            ImageEntry? imageData = _gpdFile.GetImage(Achievement.ImageId);
            if (imageData?.ImageData == null)
            {
                return null;
            }

            // Decode and cache the image
            using MemoryStream ms = new MemoryStream(imageData.ImageData);
            _cachedImage = new Bitmap(ms);
            return _cachedImage;
        }
    }

    /// <summary>
    /// Gets whether an achievement image is available.
    /// </summary>
    public bool HasAchievementImage => AchievementImage != null;

    /// <summary>
    /// Gets whether to show the LockOpen icon (unlocked without an image).
    /// </summary>
    public bool ShowLockOpenIcon => IsUnlocked && !HasAchievementImage;

    /// <summary>
    /// Gets or sets whether advanced achievement features are enabled.
    /// Propagated from the parent ViewModel.
    /// </summary>
    [ObservableProperty] private bool _areAchievementFeaturesEnabled;

    /// <summary>
    /// Gets or sets whether the achievement is selected.
    /// </summary>
    [ObservableProperty] private bool _isSelected;

    /// <summary>
    /// Initializes a new instance of the <see cref="AchievementViewModel"/> class.
    /// </summary>
    /// <param name="achievement">The achievement entry to wrap.</param>
    /// <param name="gpdFile">The GPD file containing achievement images.</param>
    /// <param name="areSecretFeaturesEnabled">Check for enabling secret features</param>
    public AchievementViewModel(AchievementEntry achievement, GpdFile? gpdFile = null, bool areSecretFeaturesEnabled = false)
    {
        Achievement = achievement;
        _gpdFile = gpdFile;
        _areAchievementFeaturesEnabled = areSecretFeaturesEnabled;
    }

    /// <summary>
    /// Refreshes all properties to update the UI.
    /// </summary>
    public void Refresh(bool clearImageCache = false)
    {
        if (clearImageCache)
        {
            _cachedImage = null;
        }

        OnPropertyChanged(nameof(IsUnlocked));
        OnPropertyChanged(nameof(UnlockDateTime));
        OnPropertyChanged(nameof(UnlockDateDisplay));
        OnPropertyChanged(nameof(Description));
        OnPropertyChanged(nameof(AchievementImage));
        OnPropertyChanged(nameof(HasAchievementImage));
        OnPropertyChanged(nameof(ShowLockOpenIcon));
    }
}