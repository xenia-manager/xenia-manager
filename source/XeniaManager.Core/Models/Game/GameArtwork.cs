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
    public string Background { get; set; }

    [JsonIgnore]
    public string FullPathBackground
    {
        get
        {
            if (string.IsNullOrEmpty(Background))
                return string.Empty;
            
            // If it's already an absolute path, return it as-is
            if (Path.IsPathRooted(Background))
                return Background;
            
            // Otherwise, resolve it relative to the app base directory
            return AppPathResolver.GetFullPath(Background);
        }
    }

    /// <summary>
    /// Path to the game's boxart
    /// </summary>
    [JsonPropertyName("boxart")]
    public string Boxart { get; set; }

    [JsonIgnore]
    public Bitmap CachedBoxart => ArtworkManager.CacheLoadArtwork(AppPathResolver.GetFullPath(Boxart));

    /// <summary>
    /// Path to the game's shortcut icon
    /// </summary>
    [JsonPropertyName("icon")]
    public string Icon { get; set; }

    [JsonIgnore]
    public string FullPathIcon
    {
        get
        {
            if (string.IsNullOrEmpty(Icon))
                return string.Empty;
            
            // If it's already an absolute path, return it as-is
            if (Path.IsPathRooted(Icon))
                return Icon;
            
            // Otherwise, resolve it relative to the app base directory
            return AppPathResolver.GetFullPath(Icon);
        }
    }
}