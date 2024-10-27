using System;
using System.Diagnostics;
using System.Text.RegularExpressions;

// Imported
using Serilog;

namespace XeniaManager
{
    public static partial class GameManager 
    {
        /// <summary>
        /// Processes the parsed match line for gamerprofiles
        /// </summary>
        /// <param name="match">Line matched by regex</param>
        /// <param name="currentProfiles">Current list of GamerProfiles</param>
        private static void GamerProfilesProcess(Match match, List<GamerProfile> currentProfiles)
        {
            // Checking if the profile is already in the list of profiles
            GamerProfile profile = currentProfiles.FirstOrDefault(p => p.GUID == match.Groups["GUID"].Value);
            if (profile != null)
            {
                profile.Name = match.Groups["Gamertag"].Value;
                profile.Slot = match.Groups["Slot"].Value;
            }
            else
            {
                // Check if there is already a profile in the new slot
                GamerProfile existingProfileInSlot = currentProfiles.FirstOrDefault(p => p.Slot == match.Groups["Slot"].Value);
                if (existingProfileInSlot != null)
                {
                    // Log the removal of the existing profile
                    Log.Information($"Removing existing profile '{existingProfileInSlot.Name}' from slot '{match.Groups["Slot"].Value}'");
                    currentProfiles.Remove(existingProfileInSlot); // Remove the existing profile from the list
                }
                        
                // Adding new profile to the list
                profile = new GamerProfile
                {
                    Name = match.Groups["Gamertag"].Value,
                    GUID = match.Groups["GUID"].Value,
                    Slot = match.Groups["Slot"].Value,
                };
                currentProfiles.Add(profile);
            }
        }
    }
}