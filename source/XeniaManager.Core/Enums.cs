using System.Text.Json.Serialization;

namespace XeniaManager.Core;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CompatibilityRating
{
    Unknown,
    Unplayable,
    Loads,
    Gameplay,
    Playable
}

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
/// Themes supported by Xenia Manager UI
/// </summary>
public enum Backdrop
{
    None,
    Mica
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

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum LibraryViewType
{
    Grid,
    List
}