namespace XeniaManager.Core.Models.Files.Account;

/// <summary>
/// Flags used in the reserved flags field of the account data structure.
/// These flags control various account settings and capabilities.
/// </summary>
[Flags]
public enum ReservedFlags : uint
{
    /// <summary>
    /// No flags set.
    /// </summary>
    None = 0x00000000,
    
    /// <summary>
    /// Indicates that the passcode is enabled for this account.
    /// </summary>
    PasscodeEnabled = 0x10000000,
    
    /// <summary>
    /// Indicates that LIVE functionality is enabled for this account.
    /// </summary>
    LiveEnabled = 0x20000000,
    
    /// <summary>
    /// Indicates that the account is in recovery mode.
    /// </summary>
    Recovering = 0x40000000
}