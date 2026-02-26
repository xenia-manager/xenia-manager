using System.Text.Json.Serialization;

namespace XeniaManager.Core.Models.Database.Patches;

/// <summary>
/// Represents a single patch file entry in the patches database.
/// Contains metadata about patch files for Xenia emulator games.
/// </summary>
public class PatchInfo
{
    /// <summary>
    /// The name of the patch file
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// The SHA hash of the patch file
    /// </summary>
    [JsonPropertyName("sha")]
    public string? Sha { get; set; }

    /// <summary>
    /// The size of the patch file in bytes
    /// </summary>
    [JsonPropertyName("size")]
    public int Size { get; set; }

    /// <summary>
    /// The download URL for the patch file
    /// </summary>
    [JsonPropertyName("download_url")]
    public string? DownloadUrl { get; set; }
}