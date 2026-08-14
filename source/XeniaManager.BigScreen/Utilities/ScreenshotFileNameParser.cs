using System;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using XeniaManager.BigScreen.Constants;

namespace XeniaManager.BigScreen.Utilities;

/// <summary>
/// Decodes Xenia screenshot metadata from the file name
/// ("{GAMEID} - {yyyy-MM-ddTHH-mm-ss}.png"), so capture dates and game IDs are
/// accurate even when files were copied or moved (write times lie).
/// </summary>
public static class ScreenshotFileNameParser
{
    private static readonly Regex GameIdPattern = new(@"^([0-9A-Fa-f]{8})", RegexOptions.Compiled);

    /// <summary>
    /// The 8-hex-digit game ID prefix of a Xenia screenshot file name, or an
    /// empty string when the name doesn't match (caller falls back to the
    /// parent folder name).
    /// </summary>
    public static string ExtractGameId(string fileName)
    {
        Match match = GameIdPattern.Match(fileName);
        return match.Success ? match.Groups[1].Value : string.Empty;
    }

    /// <summary>
    /// The capture timestamp embedded in a Xenia screenshot file name
    /// ("{yyyy-MM-ddTHH-mm-ss}"), or null when the name doesn't carry one
    /// (caller falls back to the file write time).
    /// </summary>
    public static DateTime? ExtractCapturedAt(string fileName)
    {
        string[] parts = fileName.Split(" - ", StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2)
        {
            return null;
        }

        string timestamp = Path.GetFileNameWithoutExtension(parts[^1]);
        if (DateTime.TryParseExact(timestamp, FormatConstants.ScreenshotFileNameDateFormat,
                CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsed))
        {
            return parsed;
        }

        return null;
    }
}