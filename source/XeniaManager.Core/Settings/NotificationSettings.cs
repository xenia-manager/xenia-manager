using System.Text.Json.Serialization;

namespace XeniaManager.Core.Settings;

/// <summary>
/// Subsection for UI settings
/// </summary>
public class NotificationSettings
{
    [JsonPropertyName("manager_update_available")]
    public bool ManagerUpdateAvailable { get; set; } = false;
}