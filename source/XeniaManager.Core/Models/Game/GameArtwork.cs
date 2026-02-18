using System.Text.Json.Serialization;
using Avalonia.Media.Imaging;
using XeniaManager.Core.Logging;
using XeniaManager.Core.Manage;
using XeniaManager.Core.Utilities.Paths;

namespace XeniaManager.Core.Models.Game;

/// <summary>
/// All the game artwork used by Xenia Manager
/// </summary>
public class GameArtwork
{
    /// <summary>
    /// Path to the game's background
    /// </summary>
    [JsonPropertyName("background")]
    public string Background { get; set; } = string.Empty;

    [JsonIgnore] private Bitmap? _cachedBackground;

    [JsonIgnore]
    public Bitmap? CachedBackground
    {
        get
        {
            if (_cachedBackground == null && !string.IsNullOrEmpty(Background))
            {
                try
                {
                    _cachedBackground = ArtworkManager.CacheLoadArtwork(AppPathResolver.GetFullPath(Background));
                }
                catch (Exception ex)
                {
                    Logger.Warning<GameArtwork>($"Failed to load background {Background}");
                    Logger.LogExceptionDetails<GameArtwork>(ex);
                    return null;
                }
            }
            return _cachedBackground;
        }
    }

    /// <summary>
    /// Path to the game's boxart
    /// </summary>
    [JsonPropertyName("boxart")]
    public string Boxart { get; set; } = string.Empty;

    [JsonIgnore] private Bitmap? _cachedBoxart;

    [JsonIgnore]
    public Bitmap? CachedBoxart
    {
        get
        {
            if (_cachedBoxart == null && !string.IsNullOrEmpty(Boxart))
            {
                try
                {
                    _cachedBoxart = ArtworkManager.CacheLoadArtwork(AppPathResolver.GetFullPath(Boxart));
                }
                catch (Exception ex)
                {
                    Logger.Warning<GameArtwork>($"Failed to load boxart {Boxart}");
                    Logger.LogExceptionDetails<GameArtwork>(ex);
                    return null;
                }
            }
            return _cachedBoxart;
        }
    }

    /// <summary>
    /// Path to the game's shortcut icon
    /// </summary>
    [JsonPropertyName("icon")]
    public string Icon { get; set; } = string.Empty;

    [JsonIgnore] private Bitmap? _cachedIcon;

    [JsonIgnore]
    public Bitmap? CachedIcon
    {
        get
        {
            if (_cachedIcon == null && !string.IsNullOrEmpty(Icon))
            {
                try
                {
                    _cachedIcon = ArtworkManager.CacheLoadArtwork(AppPathResolver.GetFullPath(Icon));
                }
                catch (Exception ex)
                {
                    Logger.Warning<GameArtwork>($"Failed to load icon {Icon}");
                    Logger.LogExceptionDetails<GameArtwork>(ex);
                    return null;
                }
            }
            return _cachedIcon;
        }
    }
}