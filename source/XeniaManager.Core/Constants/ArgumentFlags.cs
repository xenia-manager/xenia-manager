namespace XeniaManager.Core.Constants;

/// <summary>
/// Constants for command-line argument flags.
/// </summary>
public static class ArgumentFlags
{
    /// <summary>
    /// Flag to explicitly specify a game title.
    /// The following argument should contain the exact game title (quotes are optional).
    /// </summary>
    public const string Game = "--game";

    /// <summary>
    /// Flag to pass raw Xenia emulator arguments.
    /// The following argument should contain a space‑separated list of Xenia options (quotes are optional).
    /// </summary>
    public const string XeniaArgs = "--xenia_args";
}