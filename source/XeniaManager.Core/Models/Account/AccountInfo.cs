namespace XeniaManager.Core.Models.Account;

/// <summary>
/// Represents the decrypted account data structure as stored in the Xenia emulator profile.
/// The account file is a HMAC-RC4 encrypted file stored in profiles to hold information
/// such as the profile's gamertag, PUID (Passport User ID), online key, and more.
/// </summary>
public class AccountInfo
{
    /// <summary>
    /// Total size of the decrypted account payload (380 bytes).
    /// </summary>
    public const int DataSize = 380;

    /// <summary>
    /// Reserved flags field at offset 0x00 (4 bytes).
    /// </summary>
    public ReservedFlags ReservedFlags;

    /// <summary>
    /// Live flags field at offset 0x04 (4 bytes).
    /// </summary>
    public uint LiveFlags;

    /// <summary>
    /// Gamertag field at offset 0x08 (32 bytes, 16 Unicode characters).
    /// </summary>
    public string Gamertag { get; set; } = string.Empty;

    /// <summary>
    /// Xbox User ID field at offset 0x28 (8 bytes).
    /// </summary>
    public AccountXuid Xuid;

    /// <summary>
    /// Cached user flags field at offset 0x30 (4 bytes).
    /// </summary>
    public uint CachedUserFlags;

    /// <summary>
    /// Xbox Live service provider field at offset 0x34 (4 bytes, ASCII).
    /// </summary>
    public string ServiceProvider { get; set; } = string.Empty;

    /// <summary>
    /// Passcode field at offset 0x38 (4 bytes).
    /// </summary>
    public PasscodeButton[] Passcode { get; set; } = new PasscodeButton[4];

    /// <summary>
    /// Online domain field at offset 0x3C (20 bytes, ASCII).
    /// </summary>
    public string OnlineDomain { get; set; } = string.Empty;

    /// <summary>
    /// Online Kerberos realm field at offset 0x50 (24 bytes, ASCII).
    /// </summary>
    public string OnlineKerberosRealm { get; set; } = string.Empty;

    /// <summary>
    /// Online key field at offset 0x68 (16 bytes).
    /// </summary>
    public byte[] OnlineKey { get; set; } = new byte[16];

    // These last 3 aren't used by anything
    /// <summary>
    /// User passport member name field at offset 0x78 (114 bytes, ASCII).
    /// </summary>
    public string UserPassportMembername { get; set; } = string.Empty;

    /// <summary>
    /// User passport password field at offset 0xEA (32 bytes, ASCII).
    /// </summary>
    public string UserPassportPassword { get; set; } = string.Empty;

    /// <summary>
    /// Owner passport membername field at offset 0x10A (114 bytes, ASCII).
    /// </summary>
    public string OwnerPassportMembername { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether the passcode is enabled for this account.
    /// Corresponds to the PasscodeEnabled flag in ReservedFlags.
    /// </summary>
    public bool IsPasscodeEnabled
    {
        get => ReservedFlags.HasFlag(ReservedFlags.PasscodeEnabled);
        set => ReservedFlags = value
            ? ReservedFlags | ReservedFlags.PasscodeEnabled
            : ReservedFlags & ~ReservedFlags.PasscodeEnabled;
    }

    /// <summary>
    /// Gets or sets whether LIVE is enabled for this account.
    /// Corresponds to the LiveEnabled flag in ReservedFlags.
    /// </summary>
    public bool IsLiveEnabled
    {
        get => ReservedFlags.HasFlag(ReservedFlags.LiveEnabled);
        set => ReservedFlags = value
            ? ReservedFlags | ReservedFlags.LiveEnabled
            : ReservedFlags & ~ReservedFlags.LiveEnabled;
    }

    /// <summary>
    /// Gets or sets whether the account is in recovery mode.
    /// Corresponds to the Recovering flag in ReservedFlags.
    /// </summary>
    public bool IsRecovering
    {
        get => ReservedFlags.HasFlag(ReservedFlags.Recovering);
        set => ReservedFlags = value
            ? ReservedFlags | ReservedFlags.Recovering
            : ReservedFlags & ~ReservedFlags.Recovering;
    }

    /// <summary>
    /// Gets or sets whether a payment instrument credit card is enabled.
    /// Corresponds to bit 0 in CachedUserFlags.
    /// </summary>
    public bool PaymentInstrumentCreditCard
    {
        get => (CachedUserFlags & 1u) != 0;
        set => CachedUserFlags = value
            ? CachedUserFlags | 1u
            : CachedUserFlags & ~1u;
    }

    /// <summary>
    /// Gets or sets the country associated with the account.
    /// Corresponds to bits 8-15 in CachedUserFlags.
    /// </summary>
    public XboxLiveCountry Country
    {
        get => (XboxLiveCountry)((CachedUserFlags >> 8) & 0xFF);
        set => CachedUserFlags = (CachedUserFlags & ~(0xFFu << 8))
                                 | ((uint)(byte)value << 8);
    }

    /// <summary>
    /// Gets or sets the subscription tier for the account.
    /// Corresponds to bits 16-19 in CachedUserFlags.
    /// </summary>
    public SubscriptionTier SubscriptionTier
    {
        get => (SubscriptionTier)((CachedUserFlags >> 20) & 0xF);
        set => CachedUserFlags = (CachedUserFlags & ~(0xFu << 20))
                                 | ((uint)(byte)value << 20);
    }

    /// <summary>
    /// Gets or sets whether parental controls are enabled.
    /// Corresponds to bit 24 in CachedUserFlags.
    /// </summary>
    public bool ParentalControlsEnabled
    {
        get => (CachedUserFlags & (1u << 24)) != 0;
        set => CachedUserFlags = value
            ? CachedUserFlags | (1u << 24)
            : CachedUserFlags & ~(1u << 24);
    }

    /// <summary>
    /// Gets or sets the console language for the account.
    /// Corresponds to bits 25-29 in CachedUserFlags.
    /// </summary>
    public ConsoleLanguage Language
    {
        get => (ConsoleLanguage)((CachedUserFlags >> 25) & 0x1F);
        set => CachedUserFlags = (CachedUserFlags & ~(0x1Fu << 25))
                                 | (((uint)value & 0x1F) << 25);
    }
}