using System.Globalization;

namespace XeniaManager.Core.Utilities;

/// <summary>
/// Formats playtime values into localized human-readable strings.
/// </summary>
public static class PlaytimeFormatter
{
    /// <summary>
    /// Formats playtime (in minutes) into a localized string.
    /// </summary>
    /// <param name="playtime">Playtime in minutes</param>
    /// <param name="culture">Culture info (defaults to current culture)</param>
    /// <returns>Localized playtime string</returns>
    public static string Format(double playtime, CultureInfo? culture = null)
    {
        culture ??= CultureInfo.CurrentCulture;

        if (playtime == 0)
        {
            return LocalizationHelper.GetText("LibraryPage.GameButton.Playtime.NeverPlayed");
        }
        else if (playtime < 60)
        {
            return string.Format(
                LocalizationHelper.GetText("LibraryPage.GameButton.Playtime.Minutes"),
                playtime.ToString("N0", culture)
            );
        }
        else
        {
            return string.Format(
                LocalizationHelper.GetText("LibraryPage.GameButton.Playtime.Hours"),
                (playtime / 60).ToString("N1", culture)
            );
        }
    }
}