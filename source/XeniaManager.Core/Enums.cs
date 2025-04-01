using System.Text.Json.Serialization;

namespace XeniaManager.Core;

/// <summary>
/// Themes supported by Xenia Manager UI
/// </summary>
public enum Theme
{
    //System,
    Light,
    Dark
}

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