using System.Text.Json.Serialization;

namespace XeniaManager.Core.Enum;

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

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum LibraryViewType
{
    Grid,
    List
}