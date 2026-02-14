namespace XeniaManager.Core.Models.Account;

/// <summary>
/// Represents the buttons that can be used in an Xbox passcode.
/// </summary>
public enum PasscodeButton : byte
{
    /// <summary>
    /// Null/Placeholder
    /// </summary>
    Null = 0,
    
    /// <summary>
    /// D-pad Up
    /// </summary>
    DPadUp = 1,
    
    /// <summary>
    /// D-pad Down
    /// </summary>
    DPadDown = 2,
    
    /// <summary>
    /// D-pad Left
    /// </summary>
    DPadLeft = 3,
    
    /// <summary>
    /// D-pad Right
    /// </summary>
    DPadRight = 4,
    
    /// <summary>
    /// X
    /// </summary>
    X = 5,
    
    /// <summary>
    /// Y (Not valid for passwords)
    /// </summary>
    Y = 6,
    
    /// <summary>
    /// A (Not valid for passwords)
    /// </summary>
    A = 7,
    
    /// <summary>
    /// B (Not valid for password)
    /// </summary>
    B = 8,
    
    /// <summary>
    /// Left Trigger
    /// </summary>
    LeftTrigger = 9,
    
    /// <summary>
    /// Right Trigger
    /// </summary>
    RightTrigger = 10,
    
    /// <summary>
    /// Left Bumper
    /// </summary>
    LeftBumper = 11,
    
    /// <summary>
    /// Right Bumper
    /// </summary>
    RightBumper = 12
}