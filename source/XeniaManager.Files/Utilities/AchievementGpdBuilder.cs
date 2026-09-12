using XeniaManager.Files.Models;
using XeniaManager.Files.Models.Account;
using XeniaManager.Files.Models.Gpd;
using XeniaManager.Files.Models.Spa;
using XeniaManager.Files.Models.XConfig;
using XeniaManager.Logging;

namespace XeniaManager.Files.Utilities;

/// <summary>
/// Result of filling a title achievement GPD from a disc's SPA data.
/// </summary>
/// <param name="TitleId">The title ID the GPD was built for.</param>
/// <param name="AchievementsAdded">How many achievements were added (existing entries are kept).</param>
/// <param name="AchievementsTotal">How many achievements the GPD holds now.</param>
/// <param name="ProfileUpdated">Whether the profile GPD was created or had its title entry updated.</param>
public sealed record AchievementGpdBuildResult(uint TitleId, int AchievementsAdded, int AchievementsTotal, bool ProfileUpdated);

/// <summary>
/// Builds per-title achievement GPDs (<c>{TitleId}.gpd</c>) and profile GPD entries
/// (<c>FFFE07D1.gpd</c>) from the SPA data embedded in a game disc.
/// Existing achievements and unlock progress are never modified, only missing entries are added.
/// </summary>
public static class AchievementGpdBuilder
{
    /// <summary>
    /// Maps a profile's console language to the matching SPA string-table language.
    /// Returns <see cref="XLanguage.Invalid"/> when there is no match; callers then
    /// fall back to the SPA default language via <see cref="SpaFile.GetString"/>.
    /// </summary>
    public static XLanguage FromConsoleLanguage(ConsoleLanguage language) => language switch
    {
        ConsoleLanguage.English => XLanguage.English,
        ConsoleLanguage.Japanese => XLanguage.Japanese,
        ConsoleLanguage.German => XLanguage.German,
        ConsoleLanguage.French => XLanguage.French,
        ConsoleLanguage.Spanish => XLanguage.Spanish,
        ConsoleLanguage.Italian => XLanguage.Italian,
        ConsoleLanguage.Korean => XLanguage.Korean,
        ConsoleLanguage.TraditionalChinese => XLanguage.TChinese,
        ConsoleLanguage.Portuguese => XLanguage.Portuguese,
        ConsoleLanguage.SimplifiedChinese => XLanguage.SChinese,
        ConsoleLanguage.Polish => XLanguage.Polish,
        ConsoleLanguage.Russian => XLanguage.Russian,
        _ => XLanguage.Invalid
    };

    /// <summary>
    /// Opens the SPA embedded in a game disc (ISO/XISO, SVOD directory, STFS, XEX or ZAR).
    /// </summary>
    /// <param name="discPath">Path to the disc file or SVOD directory.</param>
    /// <param name="spa">The parsed SPA (caller must <c>Dispose()</c>) on success.</param>
    /// <returns>True when SPA was found and parsed, false otherwise.</returns>
    public static bool TryOpenSpa(string discPath, out SpaFile? spa)
    {
        spa = null;
        try
        {
            FileSignature signature;
            try
            {
                signature = FileIdentifier.IdentifyFileType(discPath);
            }
            catch (FileNotFoundException)
            {
                return false;
            }

            return signature switch
            {
                FileSignature.ISO or FileSignature.XISO => TryIsoSpa(discPath, out spa),
                FileSignature.SVOD => TrySvodSpa(discPath, out spa),
                FileSignature.CON or FileSignature.LIVE or FileSignature.PIRS => TryStfsSpa(discPath, out spa),
                FileSignature.XEX1 or FileSignature.XEX2 or FileSignature.XEX25 or FileSignature.XEX0 or FileSignature.XEXQ or FileSignature.XEXH => TryXexSpa(
                    discPath, out spa),
                FileSignature.ZAR => TryZarSpa(discPath, out spa),
                _ => false
            };
        }
        catch (Exception ex)
        {
            Logger.Trace<SpaFile>($"TryOpenSpa failed for '{discPath}': {ex.Message}");
            spa?.Dispose();
            spa = null;
            return false;
        }
    }

    private static bool TryIsoSpa(string discPath, out SpaFile? spa)
    {
        spa = null;
        using IsoFile iso = IsoFile.Load(discPath);
        return iso.TryGetSpaFile(out spa);
    }

    private static bool TrySvodSpa(string discPath, out SpaFile? spa)
    {
        spa = null;
        using SvodFile svod = SvodFile.Load(discPath);
        return svod.TryGetSpaFile(out spa);
    }

    private static bool TryStfsSpa(string discPath, out SpaFile? spa)
    {
        spa = null;
        using StfsFile stfs = StfsFile.Load(discPath);
        return stfs.TryGetSpaFile(out spa);
    }

    private static bool TryXexSpa(string discPath, out SpaFile? spa)
    {
        spa = null;
        XexFile xex = XexFile.Load(discPath);
        return xex.TryGetSpaFile(out spa);
    }

    private static bool TryZarSpa(string discPath, out SpaFile? spa)
    {
        spa = null;
        using ZarFile zar = ZarFile.Load(discPath);
        return zar.TryGetSpaFile(out spa);
    }

    /// <summary>
    /// Creates or fills the title achievement GPD from the disc's SPA data and ensures
    /// the profile GPD has a matching title entry. Missing achievements are added;
    /// existing entries (including unlock state) are left untouched.
    /// </summary>
    /// <param name="discPath">Path to the disc file or SVOD directory.</param>
    /// <param name="titleGpdPath">Destination path of the <c>{TitleId}.gpd</c> file.</param>
    /// <param name="profileGpdPath">Path of the <c>FFFE07D1.gpd</c> file (skipped when null).</param>
    /// <param name="userLanguage">The player's language; falls back to the SPA default language.</param>
    /// <exception cref="FileNotFoundException">Thrown when the disc does not exist.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the disc holds no achievement data.</exception>
    public static AchievementGpdBuildResult EnsureAchievements(string discPath, string titleGpdPath, string? profileGpdPath, XLanguage userLanguage)
    {
        EnsureDiscExists(discPath);
        using SpaFile spa = OpenSpaOrThrow(discPath);
        return EnsureFromSpa(spa, titleGpdPath, profileGpdPath, userLanguage);
    }

    /// <summary>
    /// Copies achievement images from the disc's SPA data into the title GPD.
    /// </summary>
    /// <param name="discPath">Path to the disc file or SVOD directory.</param>
    /// <param name="titleGpdPath">Path of the existing <c>{TitleId}.gpd</c> file.</param>
    /// <param name="overwriteAll">True to replace existing images, false to only fill missing ones.</param>
    /// <returns>How many images were written.</returns>
    /// <exception cref="FileNotFoundException">Thrown when the disc or GPD does not exist.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the disc holds no achievement data.</exception>
    public static int FetchImages(string discPath, string titleGpdPath, bool overwriteAll)
    {
        EnsureDiscExists(discPath);
        if (!File.Exists(titleGpdPath))
        {
            throw new FileNotFoundException($"Achievement GPD does not exist at {titleGpdPath}", titleGpdPath);
        }

        using SpaFile spa = OpenSpaOrThrow(discPath);
        return FetchFromSpa(spa, titleGpdPath, overwriteAll);
    }

    internal static AchievementGpdBuildResult EnsureFromSpa(SpaFile spa, string titleGpdPath, string? profileGpdPath, XLanguage userLanguage)
    {
        if (!spa.IncludeInProfile || spa.IsSystemApp)
        {
            throw new InvalidOperationException($"Title 0x{spa.TitleId:X8} is not included in profiles");
        }

        XLanguage language = ResolveLanguage(spa, userLanguage);
        using GpdFile titleGpd = File.Exists(titleGpdPath) ? GpdFile.Load(titleGpdPath) : GpdFile.Create(true);
        bool isNewFile = !File.Exists(titleGpdPath);
        HashSet<uint> existingIds = titleGpd.AllAchievements.Select(a => a.AchievementId).ToHashSet();
        int added = 0;
        foreach (SpaAchievement spaAchievement in spa.SpaAchievements)
        {
            if (!existingIds.Add(spaAchievement.Id))
            {
                continue;
            }

            titleGpd.AddAchievement(new AchievementEntry
            {
                AchievementId = spaAchievement.Id,
                ImageId = spaAchievement.ImageId,
                Gamerscore = spaAchievement.Gamerscore,
                Flags = spaAchievement.Flags,
                Name = spa.GetString((ushort)language, spaAchievement.LabelId),
                UnlockedDescription = spa.GetString((ushort)language, spaAchievement.DescriptionId),
                LockedDescription = spa.GetString((ushort)language, spaAchievement.UnachievedId)
            });
            added++;
        }

        // Then the game icon, then the title name entry (skipped when any entry
        // exists, mirroring the emulator, so corrupt entries never duplicate).
        if (spa.GetTitleIcon() is { Length: > 0 } titleIcon
            && !titleGpd.Entries.Any(e => e.Namespace == EntryNamespace.Image && e.Id == 0x8000))
        {
            titleGpd.AddImage(0x8000, titleIcon);
        }

        string titleName = spa.TitleName(language);
        if (!string.IsNullOrEmpty(titleName)
            && !titleGpd.Entries.Any(e => e.Namespace == EntryNamespace.String && e.Id == 0x8000))
        {
            titleGpd.AddString(0x8000, titleName);
        }

        // Re-save files predating the end-of-data marker so the emulator accepts them.
        if (isNewFile || added > 0 || IsMissingEndOfDataMarker(titleGpd))
        {
            titleGpd.Save(titleGpdPath);
        }

        Logger.Info<GpdFile>($"Added {added} achievements to {titleGpdPath} (title 0x{spa.TitleId:X8})");

        bool profileUpdated = profileGpdPath != null && EnsureProfileEntry(spa, profileGpdPath);
        return new AchievementGpdBuildResult(spa.TitleId, added, titleGpd.AllAchievements.Count(), profileUpdated);
    }

    /// <summary>
    /// Re-resolves achievement names and descriptions from the disc's SPA data
    /// and rewrites the ones that changed. IDs, images, gamerscore, flags, and
    /// unlock state are preserved. Achievements missing from either side are left alone.
    /// </summary>
    /// <param name="discPath">Path to the disc file or SVOD directory.</param>
    /// <param name="titleGpdPath">Path of the existing <c>{TitleId}.gpd</c> file.</param>
    /// <param name="userLanguage">The player's language; falls back to the SPA default language.</param>
    /// <returns>How many achievements were updated.</returns>
    /// <exception cref="FileNotFoundException">Thrown when the disc or GPD does not exist.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the disc holds no achievement data.</exception>
    public static int RefreshStrings(string discPath, string titleGpdPath, XLanguage userLanguage)
    {
        EnsureDiscExists(discPath);
        if (!File.Exists(titleGpdPath))
        {
            throw new FileNotFoundException($"Achievement GPD does not exist at {titleGpdPath}", titleGpdPath);
        }

        using SpaFile spa = OpenSpaOrThrow(discPath);
        return RefreshStringsFromSpa(spa, titleGpdPath, userLanguage);
    }

    internal static int RefreshStringsFromSpa(SpaFile spa, string titleGpdPath, XLanguage userLanguage)
    {
        using GpdFile titleGpd = GpdFile.Load(titleGpdPath);
        XLanguage language = ResolveLanguage(spa, userLanguage);
        int updated = 0;
        foreach (SpaAchievement spaAchievement in spa.SpaAchievements)
        {
            AchievementEntry? existing = titleGpd.Achievements.FirstOrDefault(a => a.AchievementId == spaAchievement.Id);
            if (existing == null)
            {
                continue;
            }

            string name = spa.GetString((ushort)language, spaAchievement.LabelId);
            string unlocked = spa.GetString((ushort)language, spaAchievement.DescriptionId);
            string locked = spa.GetString((ushort)language, spaAchievement.UnachievedId);
            if (existing.Name == name && existing.UnlockedDescription == unlocked && existing.LockedDescription == locked)
            {
                continue;
            }

            existing.Name = name;
            existing.UnlockedDescription = unlocked;
            existing.LockedDescription = locked;
            titleGpd.UpdateAchievement(existing.AchievementId, existing);
            updated++;
        }

        // Re-save files predating the end-of-data marker so the emulator accepts them.
        if (updated > 0 || IsMissingEndOfDataMarker(titleGpd))
        {
            titleGpd.Save(titleGpdPath);
        }

        Logger.Info<GpdFile>($"Updated {updated} achievement strings in {titleGpdPath}");
        return updated;
    }

    internal static int FetchFromSpa(SpaFile spa, string titleGpdPath, bool overwriteAll)
    {
        using GpdFile titleGpd = GpdFile.Load(titleGpdPath);
        int written = 0;
        foreach ((ulong id, ImageEntry image) in spa.EnumerateImagesWithIds())
        {
            uint imageId = (uint)id;
            if (image.ImageData.Length == 0 || !image.IsValidPng)
            {
                continue;
            }

            if (!overwriteAll
                && titleGpd.Entries.Any(e => e.Namespace == EntryNamespace.Image && e.Id == imageId))
            {
                continue;
            }

            if (overwriteAll)
            {
                titleGpd.RemoveImage(imageId);
            }

            titleGpd.AddImage(imageId, image.ImageData);
            written++;
        }

        // Re-save files predating the end-of-data marker so the emulator accepts them.
        if (written > 0 || IsMissingEndOfDataMarker(titleGpd))
        {
            titleGpd.Save(titleGpdPath);
        }

        Logger.Info<GpdFile>($"Wrote {written} images to {titleGpdPath} (overwrite: {overwriteAll})");
        return written;
    }

    private static bool EnsureProfileEntry(SpaFile spa, string profileGpdPath)
    {
        using GpdFile profileGpd = File.Exists(profileGpdPath) ? GpdFile.Load(profileGpdPath) : GpdFile.Create(true);
        bool isNewFile = !File.Exists(profileGpdPath);
        TitleEntry? existing = profileGpd.Titles.FirstOrDefault(t => t.TitleId == spa.TitleId);
        bool updated;
        if (existing == null)
        {
            TitleEntry filled = GpdFile.FillTitlePlayedData(spa);
            filled.SetLastPlayedTime(DateTime.Now);
            profileGpd.AddTitle(filled);
            updated = true;
        }
        else if (existing.AchievementCount < spa.SpaAchievements.Count || existing.GamerscoreTotal < (int)spa.TotalGamerscore)
        {
            existing.AchievementCount = spa.SpaAchievements.Count;
            existing.GamerscoreTotal = (int)spa.TotalGamerscore;
            if (string.IsNullOrEmpty(existing.TitleName))
            {
                existing.TitleName = spa.TitleName();
            }

            profileGpd.UpdateTitleEntry(spa.TitleId, existing);
            updated = true;
        }
        else
        {
            updated = false;
        }

        // Re-save files predating the end-of-data marker so the emulator accepts them.
        if (isNewFile || updated || IsMissingEndOfDataMarker(profileGpd))
        {
            profileGpd.Save(profileGpdPath);
        }

        return isNewFile || updated;
    }

    /// <summary>
    /// Resolves which string-table language to read: the requested one when the
    /// title exists in it, otherwise the SPA default language. Per-string reads
    /// via <see cref="SpaFile.GetString"/> still fall back further to English.
    /// </summary>
    private static XLanguage ResolveLanguage(SpaFile spa, XLanguage userLanguage) =>
        string.IsNullOrEmpty(spa.TitleName(userLanguage)) ? spa.DefaultLanguage : userLanguage;

    /// <summary>
    /// Whether the GPD's free table lacks the end-of-data marker the emulator
    /// requires (files written before the marker was maintained).
    /// </summary>
    private static bool IsMissingEndOfDataMarker(GpdFile gpd) =>
        !gpd.FreeSpaceEntries.Any(f => f.OffsetSpecifier == (uint)gpd.Data.Length);

    private static void EnsureDiscExists(string discPath)
    {
        if (!File.Exists(discPath) && !Directory.Exists(discPath))
        {
            throw new FileNotFoundException($"Disc does not exist at {discPath}", discPath);
        }
    }

    private static SpaFile OpenSpaOrThrow(string discPath)
    {
        if (!TryOpenSpa(discPath, out SpaFile? spa) || spa == null || !spa.IsValid || spa.TitleId == 0)
        {
            spa?.Dispose();
            throw new InvalidOperationException($"No achievement data found on disc '{discPath}'");
        }

        return spa;
    }
}