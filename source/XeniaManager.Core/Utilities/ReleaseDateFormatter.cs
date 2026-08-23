using System.Globalization;

namespace XeniaManager.Core.Utilities;

/// <summary>
/// Formats marketplace database release dates into human-readable form.
/// </summary>
public static class ReleaseDateFormatter
{
    /// <summary>
    /// Formats an ISO release date string (e.g. "2010-05-18") into a readable
    /// date with an ordinal day (e.g. "18th May 2010"). Returns the input
    /// unchanged when it can't be parsed.
    /// </summary>
    /// <param name="releaseDate">The raw release date string from the database.</param>
    /// <returns>The formatted date, or the original string when unparseable.</returns>
    public static string? Format(string? releaseDate)
    {
        if (string.IsNullOrWhiteSpace(releaseDate)
            || !DateTime.TryParse(releaseDate, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime date))
        {
            return releaseDate;
        }

        int day = date.Day;
        string suffix = (day % 100) switch
        {
            11 or 12 or 13 => "th",
            _ => (day % 10) switch
            {
                1 => "st",
                2 => "nd",
                3 => "rd",
                _ => "th",
            },
        };

        return $"{day}{suffix} {date.ToString("MMMM yyyy", CultureInfo.InvariantCulture)}";
    }
}
