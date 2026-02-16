using System.Text.Json.Serialization;
using Avalonia.Media.Imaging;
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
    public string? Background { get; set; }

    [JsonIgnore]
    public Bitmap? FullPathBackground
    {
        get
        {
            if (Background != null)
            {
                return ArtworkManager.CacheLoadArtwork(AppPathResolver.GetFullPath(Background));
            }
            return null;
        }
    }

    /// <summary>
    /// Path to the game's boxart
    /// </summary>
    [JsonPropertyName("boxart")]
    public string? Boxart { get; set; }

    [JsonIgnore]
    public Bitmap? CachedBoxart
    {
        get
        {
            if (Boxart != null)
            {
                return ArtworkManager.CacheLoadArtwork(AppPathResolver.GetFullPath(Boxart));
            }
            return null;
        }
    }

    /// <summary>
    /// Path to the game's shortcut icon
    /// </summary>
    [JsonPropertyName("icon")]
    public string? Icon { get; set; }

    [JsonIgnore]
    public Bitmap? FullPathIcon
    {
        get
        {
            if (Icon != null)
            {
                return ArtworkManager.CacheLoadArtwork(AppPathResolver.GetFullPath(Icon));
            }
            return null;
        }
    }
}