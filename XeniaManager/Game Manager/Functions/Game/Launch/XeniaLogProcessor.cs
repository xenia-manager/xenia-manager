using System.Text.RegularExpressions;

// Imported
using Serilog;

namespace XeniaManager
{
    public static partial class GameManager
    {
        /// <summary>
        /// Processes the parsed match line for gamer profiles
        /// </summary>
        /// <param name="match">Line matched by regex</param>
        /// <param name="currentProfiles">Current list of GamerProfiles</param>
        private static void GamerProfilesProcess(Match match, List<GamerProfile> currentProfiles)
        {
            // Extract necessary values once and check if all required groups are present
            string gamertag = match.Groups["Gamertag"].Value;
            string xuid = match.Groups["GUID"].Value;
            string slot = match.Groups["Slot"].Value;

            if (string.IsNullOrEmpty(gamertag) || string.IsNullOrEmpty(xuid) || string.IsNullOrEmpty(slot))
            {
                return;
            }

            Log.Information($"Profile: {gamertag} ({xuid}) - Slot {slot}");
            // Find the profile by GUID or Slot directly
            GamerProfile profile = currentProfiles.FirstOrDefault(p => p.Xuid == xuid);

            if (profile != null)
            {
                // Update existing profile's details if found by GUID
                profile.Name = gamertag;
                profile.Slot = slot;
            }
            else
            {
                // Remove any existing profile with the same slot
                GamerProfile existingProfileInSlot = currentProfiles.FirstOrDefault(p => p.Slot == slot);
                if (existingProfileInSlot != null)
                {
                    Log.Information($"Removing existing profile '{existingProfileInSlot.Name}' from slot '{slot}'");
                    currentProfiles.Remove(existingProfileInSlot);
                }

                // Add the new profile
                currentProfiles.Add(new GamerProfile
                {
                    Name = gamertag,
                    Xuid = xuid,
                    Slot = slot,
                });
            }
        }
    }
}