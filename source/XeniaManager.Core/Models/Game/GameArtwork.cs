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
    public string Background { get; set; } = string.Empty;

    [JsonIgnore]
    public Bitmap? CachedBackground
    {
        get
        {
            try
            {
                return ArtworkManager.CacheLoadArtwork(AppPathResolver.GetFullPath(Background));
            }
            catch (Exception)
            {
               return null;
            }
        }
    }

    /// <summary>
    /// Path to the game's boxart
    /// </summary>
    [JsonPropertyName("boxart")]
    public string Boxart { get; set; } = string.Empty;

    [JsonIgnore]
    public Bitmap? CachedBoxart
    {
        get
        {
            try
            {
                return ArtworkManager.CacheLoadArtwork(AppPathResolver.GetFullPath(Boxart));
            }
            catch (Exception)
            {
                return null;
            }
        }
    }

    /// <summary>
    /// Path to the game's shortcut icon
    /// </summary>
    [JsonPropertyName("icon")]
    public string Icon { get; set; } = string.Empty;

    [JsonIgnore]
    public Bitmap? CachedIcon
    {
        get
        {
            try
            {
                return ArtworkManager.CacheLoadArtwork(AppPathResolver.GetFullPath(Icon));
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}