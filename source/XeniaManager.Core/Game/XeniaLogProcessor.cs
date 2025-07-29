using System.Text.RegularExpressions;
using XeniaManager.Core.Profile;

namespace XeniaManager.Core.Game;

public static class XeniaLogProcessor
{
    public static readonly Regex GamerProfilesRegex = new Regex(@"\bLoaded\s(?<Gamertag>\w+)\s\(GUID:\s(?<GUID>[A-F0-9]+)\)\sto\sslot\s(?<Slot>[0-4])",
        RegexOptions.Compiled | RegexOptions.CultureInvariant
    );

    public static void ProcessLoadedGamerProfiles(Match logMatch, List<GamerProfile> profiles)
    {
        string gamertag = logMatch.Groups["Gamertag"].Value;
        string xuid = logMatch.Groups["GUID"].Value;
        string slot = logMatch.Groups["Slot"].Value;

        if (string.IsNullOrEmpty(gamertag) || string.IsNullOrEmpty(xuid) || string.IsNullOrEmpty(slot))
        {
            return;
        }

        Logger.Info($"Profile: {gamertag} ({xuid}) loaded to slot {slot}");

        GamerProfile existingByXuid = null;
        GamerProfile existingBySlot = null;

        foreach (GamerProfile profile in profiles)
        {
            if (profile.Xuid == xuid)
            {
                existingByXuid = profile;
            }
            else if (profile.Slot == slot)
            {
                existingBySlot = profile;
            }

            if (existingByXuid != null && existingBySlot != null)
            {
                break;
            }
        }

        if (existingByXuid != null)
        {
            existingByXuid.Name = gamertag;
            existingByXuid.Slot = slot;

            if (existingBySlot != null && existingBySlot != existingByXuid)
            {
                Logger.Info($"Removing existing profile '{existingBySlot.Name}' from slot '{slot}'");
                profiles.Remove(existingBySlot);
            }
        }
        else
        {
            if (existingBySlot != null)
            {
                Logger.Info($"Removing existing profile '{existingBySlot.Name}' from slot '{slot}'");
                profiles.Remove(existingBySlot);
            }

            profiles.Add(new GamerProfile
            {
                Name = gamertag,
                Xuid = xuid,
                Slot = slot,
            });
        }
    }
}