using System.Text.Json.Serialization;
using Avalonia.Media.Imaging;
using XeniaManager.Core.Logging;
using XeniaManager.Core.Manage;
using XeniaManager.Core.Utilities;

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
    [JsonIgnore] private readonly Lock _backgroundLock = new Lock();

    [JsonIgnore]
    public Bitmap? CachedBackground
    {
        get
        {
            if (_cachedBackground != null)
            {
                return _cachedBackground;
            }
            lock (_backgroundLock)
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
    }

    /// <summary>
    /// Path to the game's boxart
    /// </summary>
    [JsonPropertyName("boxart")]
    public string Boxart { get; set; } = string.Empty;

    [JsonIgnore] private Bitmap? _cachedBoxart;
    [JsonIgnore] private readonly Lock _boxartLock = new Lock();

    [JsonIgnore]
    public Bitmap? CachedBoxart
    {
        get
        {
            if (_cachedBoxart != null)
            {
                return _cachedBoxart;
            }
            lock (_boxartLock)
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
    }

    /// <summary>
    /// Path to the game's shortcut icon
    /// </summary>
    [JsonPropertyName("icon")]
    public string Icon { get; set; } = string.Empty;

    [JsonIgnore] private Bitmap? _cachedIcon;
    [JsonIgnore] private readonly Lock _iconLock = new Lock();

    [JsonIgnore]
    public Bitmap? CachedIcon
    {
        get
        {
            if (_cachedIcon != null)
            {
                return _cachedIcon;
            }
            lock (_iconLock)
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

    /// <summary>
    /// Preloads the cached background by forcing it to be loaded from the disk
    /// </summary>
    public void PreloadBackground() => _ = CachedBackground;

    /// <summary>
    /// Preloads the cached boxart by forcing it to be loaded from the disk
    /// </summary>
    public void PreloadBoxart() => _ = CachedBoxart;

    /// <summary>
    /// Preloads all cached images by forcing them to load from the disk.
    /// </summary>
    public void PreloadAll()
    {
        PreloadBackground();
        PreloadBoxart();
        _ = CachedIcon;
    }

    /// <summary>
    /// Clears all cached images to force reload from the disk.
    /// </summary>
    public void ClearCachedImages()
    {
        lock (_backgroundLock)
        {
            _cachedBackground = null;
        }
        lock (_boxartLock)
        {
            _cachedBoxart = null;
        }
        lock (_iconLock)
        {
            _cachedIcon = null;
        }
    }

    /// <summary>
    /// Clears the cached icon.
    /// </summary>
    public void ClearCachedIcon()
    {
        lock (_iconLock)
        {
            _cachedIcon = null;
        }
    }

    /// <summary>
    /// Clears the cached boxart.
    /// </summary>
    public void ClearCachedBoxart()
    {
        lock (_boxartLock)
        {
            _cachedBoxart = null;
        }
    }

    /// <summary>
    /// Clears the cached background.
    /// </summary>
    public void ClearCachedBackground()
    {
        lock (_backgroundLock)
        {
            _cachedBackground = null;
        }
    }
}