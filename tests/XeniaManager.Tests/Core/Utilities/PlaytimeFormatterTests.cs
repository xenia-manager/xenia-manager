using System.Globalization;
using XeniaManager.Core.Utilities;

namespace XeniaManager.Tests.Core.Utilities;

public class PlaytimeFormatterTests
{
    [Test]
    public void Format_Zero_ReturnsNeverPlayedFallback()
    {
        string result = PlaytimeFormatter.Format(0);
        // Without Avalonia app, LocalizationHelper returns [key]
        Assert.That(result, Is.EqualTo("[LibraryPage.GameButton.Playtime.NeverPlayed]"));
    }

    [Test]
    public void Format_LessThan60_ReturnsMinutesFallback()
    {
        string result = PlaytimeFormatter.Format(30);
        Assert.That(result, Is.EqualTo("[LibraryPage.GameButton.Playtime.Minutes]"));
    }

    [Test]
    public void Format_Exactly60_ReturnsHoursFallback()
    {
        string result = PlaytimeFormatter.Format(60);
        Assert.That(result, Is.EqualTo("[LibraryPage.GameButton.Playtime.Hours]"));
    }

    [Test]
    public void Format_GreaterThan60_ReturnsHoursFallback()
    {
        string result = PlaytimeFormatter.Format(90);
        Assert.That(result, Is.EqualTo("[LibraryPage.GameButton.Playtime.Hours]"));
    }

    [Test]
    public void Format_Negative_ReturnsMinutesFallback()
    {
        // Negative < 60 branch, still minutes
        string result = PlaytimeFormatter.Format(-5);
        Assert.That(result, Is.EqualTo("[LibraryPage.GameButton.Playtime.Minutes]"));
    }

    [Test]
    public void Format_WithExplicitCulture_DoesNotThrow()
    {
        CultureInfo culture = new CultureInfo("en-US");
        Assert.DoesNotThrow(() => PlaytimeFormatter.Format(30, culture));
        Assert.DoesNotThrow(() => PlaytimeFormatter.Format(90, culture));
        Assert.DoesNotThrow(() => PlaytimeFormatter.Format(0, culture));
    }

    [Test]
    public void Format_WithGermanCulture_DoesNotThrow()
    {
        CultureInfo culture = new CultureInfo("de-DE");
        string result = PlaytimeFormatter.Format(30, culture);
        Assert.That(result, Is.Not.Null);
    }

    [Test]
    public void Format_NullCulture_UsesCurrentCulture()
    {
        string result1 = PlaytimeFormatter.Format(30, null);
        string result2 = PlaytimeFormatter.Format(30, CultureInfo.CurrentCulture);
        Assert.That(result1, Is.EqualTo(result2));
    }

    [Test]
    public void Format_Hours_CultureAffectsNumberFormatting_WhenResourcesPresent()
    {
        // Even with fallback brackets, ensure method handles N1 formatting without exception
        // We test that 90 minutes = 1.5 hours would be formatted with N1 if resources had placeholder
        // Here fallback is bracket, but we verify no exception and consistent fallback
        CultureInfo en = new CultureInfo("en-US");
        CultureInfo fr = new CultureInfo("fr-FR");
        string enResult = PlaytimeFormatter.Format(90, en);
        string frResult = PlaytimeFormatter.Format(90, fr);
        // Both fall back to same bracket key, so equal
        Assert.That(enResult, Is.EqualTo("[LibraryPage.GameButton.Playtime.Hours]"));
        Assert.That(frResult, Is.EqualTo("[LibraryPage.GameButton.Playtime.Hours]"));
    }
}