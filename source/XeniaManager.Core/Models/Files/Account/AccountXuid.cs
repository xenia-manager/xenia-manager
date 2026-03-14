using System.Globalization;
using System.Security.Cryptography;
using XeniaManager.Core.Logging;

namespace XeniaManager.Core.Models.Files.Account;

/// <summary>
/// Represents an Xbox User Identifier (XUID) used in the Xenia emulator.
/// XUIDs are 64-bit values that identify users in the Xbox ecosystem.
/// </summary>
public struct AccountXuid : IEquatable<AccountXuid>
{
    private const ulong OfflinePrefix = 0xE03UL << 52;
    private const ulong DefaultValue = 0xB13EBABEBABEBABE;

    /// <summary>
    /// The underlying 64-bit value of the XUID.
    /// </summary>
    public ulong Value;

    /// <summary>
    /// Initializes a new instance of the AccountXuid struct with the specified value.
    /// </summary>
    /// <param name="value">The 64-bit value to assign to the XUID.</param>
    public AccountXuid(ulong value) => Value = value;

    /// <summary>
    /// Determines if this XUID represents an offline account.
    /// Offline XUIDs have the top 12 bits set to 0xE000.
    /// The default value is also treated as an offline XUID.
    /// </summary>
    public bool IsOffline => (Value & 0xF000000000000000UL) == 0xE000000000000000UL || Value == DefaultValue;

    /// <summary>
    /// Determines if this XUID represents an online account.
    /// Online XUIDs have the top 16 bits set to 0x0009.
    /// </summary>
    public bool IsOnline => (Value & 0xFFFF000000000000UL) == 0x0009000000000000UL;

    /// <summary>
    /// Determines if this XUID represents a team (developer) account.
    /// Team XUIDs have the top 8 bits set to 0xFE.
    /// </summary>
    public bool IsTeam => (Value & 0xFF00000000000000UL) == 0xFE00000000000000UL;

    /// <summary>
    /// Determines if this XUID is valid (either offline or online, but not both).
    /// </summary>
    public bool IsValid => IsOffline != IsOnline;

    /// <summary>
    /// Creates a default XUID with the value 0xB13EBABEBABEBABE.
    /// </summary>
    /// <returns>A new AccountXuid instance with the default value.</returns>
    public static AccountXuid CreateDefault() => new AccountXuid(DefaultValue);

    /// <summary>
    /// Generates a random offline XUID.
    /// Layout: top 12 bits = 0xE03 prefix, bottom 31 bits = random identifier.
    /// </summary>
    /// <returns>A new randomly generated offline AccountXuid.</returns>
    public static AccountXuid GenerateOfflineXuid()
    {
        Span<byte> buf = stackalloc byte[4];
        RandomNumberGenerator.Fill(buf);

        uint random = BitConverter.ToUInt32(buf) & 0x7FFFFFFFu;
        return new AccountXuid(OfflinePrefix | random);
    }

    /// <summary>
    /// Implicitly converts an AccountXuid to its underlying ulong value.
    /// </summary>
    /// <param name="x">The AccountXuid to convert.</param>
    /// <returns>The underlying ulong value of the XUID.</returns>
    public static implicit operator ulong(AccountXuid x) => x.Value;

    /// <summary>
    /// Implicitly converts an ulong value to an AccountXuid.
    /// </summary>
    /// <param name="v">The ulong value to convert.</param>
    /// <returns>A new AccountXuid with the specified value.</returns>
    public static implicit operator AccountXuid(ulong v) => new AccountXuid(v);

    /// <summary>
    /// Determines whether this XUID is equal to another XUID.
    /// </summary>
    /// <param name="other">The other XUID to compare with.</param>
    /// <returns>True if the XUIDs have the same value, false otherwise.</returns>
    public bool Equals(AccountXuid other) => Value == other.Value;

    /// <summary>
    /// Determines whether this XUID is equal to another object.
    /// </summary>
    /// <param name="obj">The object to compare with.</param>
    /// <returns>True if the object is an AccountXuid with the same value, false otherwise.</returns>
    public override bool Equals(object? obj) => obj is AccountXuid other && Equals(other);

    /// <summary>
    /// Gets the hash code for this XUID.
    /// </summary>
    /// <returns>The hash code of the underlying value.</returns>
    public override int GetHashCode() => Value.GetHashCode();

    /// <summary>
    /// Returns a string representation of this XUID in hexadecimal format.
    /// </summary>
    /// <returns>The XUID value formatted as a hexadecimal string.</returns>
    public override string ToString() => Value.ToString("X16");

    /// <summary>
    /// Compares two XUIDs for equality.
    /// </summary>
    /// <param name="a">The first XUID to compare.</param>
    /// <param name="b">The second XUID to compare.</param>
    /// <returns>True if the XUIDs have the same value, false otherwise.</returns>
    public static bool operator ==(AccountXuid a, AccountXuid b) => a.Value == b.Value;

    /// <summary>
    /// Compares two XUIDs for inequality.
    /// </summary>
    /// <param name="a">The first XUID to compare.</param>
    /// <param name="b">The second XUID to compare.</param>
    /// <returns>True if the XUIDs have different values, false otherwise.</returns>
    public static bool operator !=(AccountXuid a, AccountXuid b) => a.Value != b.Value;

    /// <summary>
    /// Determines whether a hexadecimal string represents a valid XUID format.
    /// </summary>
    /// <param name="hex">The hexadecimal string to validate.</param>
    /// <returns>True if the string represents a valid XUID format, false otherwise.</returns>
    public static bool IsValidFormat(string hex)
    {
        Logger.Trace<AccountXuid>($"Validating XUID format for hex string: '{hex}'");

        if (string.IsNullOrWhiteSpace(hex))
        {
            Logger.Debug<AccountXuid>($"Hex string is null, empty, or whitespace. Validation failed.");
            return false;
        }

        if (hex.Length != 16)
        {
            Logger.Debug<AccountXuid>($"Hex string length is {hex.Length}, expected 16 characters. Validation failed.");
            return false;
        }

        if (!ulong.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out ulong value))
        {
            Logger.Debug<AccountXuid>($"Failed to parse hex string '{hex}' as a valid unsigned long integer. Validation failed.");
            return false;
        }

        bool isValid = IsValidFormat(value);
        Logger.Info<AccountXuid>($"Hex string '{hex}' validation result: {(isValid ? "valid" : "invalid")} XUID format");
        return isValid;
    }


    /// <summary>
    /// Determines whether a raw 64-bit value has a valid XUID format.
    /// This checks structure and reserved ranges only; it does not create an AccountXuid.
    /// </summary>
    /// <param name="value">The 64-bit value to validate.</param>
    /// <returns>True if the value matches a valid XUID format.</returns>
    public static bool IsValidFormat(ulong value)
    {
        Logger.Trace<AccountXuid>($"Validating XUID format for raw value: 0x{value:X16}");

        // Zero is never valid
        if (value == 0)
        {
            Logger.Debug<AccountXuid>("Value is zero, which is never valid (FAILED)");
            return false;
        }

        // Accept the default value as valid
        if (value == DefaultValue)
        {
            Logger.Debug<AccountXuid>($"Value matches default placeholder value 0x{DefaultValue:X16} (PASSED)");
            return true;
        }

        // Reject team / developer XUIDs
        if ((value & 0xFF00000000000000UL) == 0xFE00000000000000UL)
        {
            Logger.Debug<AccountXuid>($"Value 0x{value:X16} matches team/developer XUID pattern (FAILED)");
            return false;
        }

        // Offline XUID check
        bool isOffline = (value & 0xF000000000000000UL) == 0xE000000000000000UL;
        Logger.Debug<AccountXuid>($"Offline XUID check for value 0x{value:X16}: {(isOffline ? "match" : "no match")}");

        // Online XUID check
        bool isOnline = (value & 0xFFFF000000000000UL) == 0x0009000000000000UL;
        Logger.Debug<AccountXuid>($"Online XUID check for value 0x{value:X16}: {(isOnline ? "match" : "no match")}");

        // For the default value, we've already returned true, so now check if it's a valid offline/online combination
        // Must be exactly one of offline or online
        if (isOffline == isOnline)
        {
            Logger.Debug<AccountXuid>($"Value 0x{value:X16} is neither offline nor online XUID (both false) or is both (both true) (FAILED)");
            return false;
        }

        // Optional: offline random bits must not be zero
        if (isOffline && (value & 0x000000007FFFFFFFUL) == 0)
        {
            Logger.Debug<AccountXuid>($"Offline XUID 0x{value:X16} has zero random bits, which is invalid (FAILED)");
            return false;
        }

        Logger.Info<AccountXuid>($"Value 0x{value:X16} passed all validation checks (PASSED)");
        return true;
    }
}