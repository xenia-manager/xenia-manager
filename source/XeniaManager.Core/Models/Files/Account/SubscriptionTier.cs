namespace XeniaManager.Core.Models.Files.Account;

/// <summary>
/// Represents the subscription tiers available for Xbox Live accounts.
/// Used in the account data structure to specify the user's subscription level.
/// </summary>
public enum SubscriptionTier : byte
{
    /// <summary>
    /// No subscription (free account).
    /// </summary>
    NoSubscription = 0,
    
    /// <summary>
    /// Silver subscription tier.
    /// </summary>
    Silver = 3,
    
    /// <summary>
    /// Gold subscription tier.
    /// </summary>
    Gold = 6,
    
    /// <summary>
    /// Family subscription tier.
    /// </summary>
    Family = 9
}