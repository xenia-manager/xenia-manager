using System.Text.Json.Serialization;

namespace XeniaManager.Core;

public enum Theme
{
    //System,
    Light,
    Dark
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum XeniaVersion
{
    Canary,
    Mousehook,
    Netplay,
    Custom
}