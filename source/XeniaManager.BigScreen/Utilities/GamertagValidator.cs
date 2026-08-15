using System;
using System.Text.RegularExpressions;

namespace XeniaManager.BigScreen.Utilities;

/// <summary>
/// Validates Xbox gamertag format.
/// </summary>
public static class GamertagValidator
{
    /// <summary>
    /// Regex pattern for valid gamertag format:
    /// must start with a letter, can contain letters and numbers, spaces allowed only between words.
    /// </summary>
    private static readonly Regex GamertagRegex = new(@"^[A-Za-z][A-Za-z0-9]*( [A-Za-z0-9]+)*$");

    /// <summary>
    /// Maximum allowed length for a gamertag.
    /// </summary>
    public const int MaxLength = 15;

    /// <summary>
    /// Validates the given gamertag against the format rules.
    /// </summary>
    public static GamertagValidationError Validate(string gamertag)
    {
        if (string.IsNullOrWhiteSpace(gamertag))
        {
            return GamertagValidationError.Empty;
        }

        if (gamertag.Length > MaxLength)
        {
            return GamertagValidationError.TooLong;
        }

        return GamertagRegex.IsMatch(gamertag)
            ? GamertagValidationError.None
            : GamertagValidationError.InvalidFormat;
    }
}

/// <summary>
/// The gamertag validation outcome.
/// </summary>
public enum GamertagValidationError
{
    /// <summary>
    /// The gamertag is valid.
    /// </summary>
    None,

    /// <summary>
    /// The gamertag is empty or whitespace.
    /// </summary>
    Empty,

    /// <summary>
    /// The gamertag exceeds <see cref="GamertagValidator.MaxLength"/> characters.
    /// </summary>
    TooLong,

    /// <summary>
    /// The gamertag contains invalid characters or spacing.
    /// </summary>
    InvalidFormat
}