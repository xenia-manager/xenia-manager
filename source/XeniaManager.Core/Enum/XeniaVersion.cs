using System.Text.Json.Serialization;

namespace XeniaManager.Core.Enum;

/// <summary>
/// Xenia versions supported by Xenia Manager
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum XeniaVersion
{
    Canary,
    Mousehook,
    Netplay,
    Custom
}
