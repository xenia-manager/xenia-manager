namespace XeniaManager.Files.Models.Spa;

/// <summary>
/// Title type stored in the SPA title header (XTHD section).
/// </summary>
public enum TitleType : uint
{
    /// <summary>
    /// System application.
    /// </summary>
    System = 0,

    /// <summary>
    /// Full title.
    /// </summary>
    Full = 1,

    /// <summary>
    /// Demo title.
    /// </summary>
    Demo = 2,

    /// <summary>
    /// Download title.
    /// </summary>
    Download = 3,

    /// <summary>
    /// Unknown title type.
    /// </summary>
    Unknown = 4,

    /// <summary>
    /// Application.
    /// </summary>
    App = 5
}