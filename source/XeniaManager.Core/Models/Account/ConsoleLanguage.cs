namespace XeniaManager.Core.Models.Account;

/// <summary>
/// Represents the languages available for the Xbox console interface.
/// Used in the account data structure to specify the user's preferred language.
/// </summary>
public enum ConsoleLanguage : byte
{
    /// <summary>
    /// No language selected
    /// </summary>
    None = 0,

    /// <summary>
    /// English
    /// </summary>
    English = 1,

    /// <summary>
    /// Japanese
    /// </summary>
    Japanese = 2,

    /// <summary>
    /// German
    /// </summary>
    German = 3,

    /// <summary>
    /// French
    /// </summary>
    French = 4,

    /// <summary>
    /// Spanish
    /// </summary>
    Spanish = 5,

    /// <summary>
    /// Italian
    /// </summary>
    Italian = 6,

    /// <summary>
    /// Korean
    /// </summary>
    Korean = 7,

    /// <summary>
    /// Traditional Chinese
    /// </summary>
    TChinese = 8,

    /// <summary>
    /// Portuguese
    /// </summary>
    Portuguese = 9,

    /// <summary>
    /// Simplified Chinese
    /// </summary>
    SChinese = 10,

    /// <summary>
    /// Polish
    /// </summary>
    Polish = 11,

    /// <summary>
    /// Russian
    /// </summary>
    Russian = 12
}