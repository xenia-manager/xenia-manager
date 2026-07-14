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
    /// <summary>
    /// Flag(s) to explicitly specify a game title.
    /// Supports "--game" and short alias "-g".
    /// </summary>
    public static readonly string[] Game = ["--game"];

    /// <summary>
    /// Flag to pass raw Xenia emulator arguments.
    /// The following argument should contain a space‑separated list of Xenia options (quotes are optional).
    /// </summary>
    /// <summary>
    /// Flag(s) to pass raw Xenia emulator arguments.
    /// Supports "--xenia_args" and short alias "-x".
    /// </summary>
    public static readonly string[] XeniaArgs = ["--xenia_args"];

    /// <summary>
    /// Flag(s) to explicitly specify which disc of a multi-disc game to launch (1-based).
    /// Supports "--disc" and short alias "-d". When omitted, the disc selection dialog is shown.
    /// </summary>
    public static readonly string[] Disc = ["--disc", "-d"];
}