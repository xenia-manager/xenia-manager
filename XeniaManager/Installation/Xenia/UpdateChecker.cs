using System;
using System.Globalization;

// Imported
using Newtonsoft.Json.Linq;
using Serilog;

namespace XeniaManager.Installation
{
    public partial class Xenia
    {
        /// <summary>
        /// Function that checks for Xenia updates
        /// </summary>
        /// <param name="xeniaVersion">Xenia version that we're checking updates for</param>
        public async Task<(bool updateAvailable, JObject? release)> CheckForUpdates(EmulatorVersion xeniaVersion)
        {
            try
            {
                Log.Information($"Checking for Xenia {xeniaVersion} updates");

                // Construct the URL based on update type
                string url = xeniaVersion switch
                {
                    EmulatorVersion.Canary => "https://api.github.com/repos/xenia-canary/xenia-canary/releases?per_page=2",
                    EmulatorVersion.Netplay => "https://api.github.com/repos/AdrianCassar/xenia-canary/releases/latest",
                    _ => throw new InvalidOperationException("Unexpected build type")
                };

                // Initialize HttpClient and set headers
                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("User-Agent", "Xenia Manager (https://github.com/xenia-manager/xenia-manager)");
                    client.DefaultRequestHeaders.Add("Accept", "application/vnd.github.v3+json");

                    // Send GET request to GitHub API
                    HttpResponseMessage response = await client.GetAsync(url);

                    // Checking if the response is success
                    if (!response.IsSuccessStatusCode)
                    {
                        return (false, null);
                    }

                    // Read the response
                    string json = await response.Content.ReadAsStringAsync();

                    // Parse the response as JSON Object
                    JObject latestRelease;
                    if (xeniaVersion == EmulatorVersion.Canary)
                    {
                        // Parse the JSON as an array since we're fetching multiple releases for Canary
                        JArray releases = JArray.Parse(json);

                        // Ensure there are at least two releases to fetch the second latest
                        if (releases.Count < 2)
                        {
                            Log.Warning("Not enough releases found to retrieve the second latest release.");
                            return (false, null);
                        }

                        // Access the second latest release (index 1)
                        latestRelease = (JObject)releases[1];
                    }
                    else
                    {
                        // For Netplay, just parse the JSON as an object (single latest release)
                        latestRelease = JObject.Parse(json);
                    }

                    // Parse release date from response
                    DateTime releaseDate;
                    bool isDateParsed = DateTime.TryParseExact(
                        latestRelease["published_at"].Value<string>(),
                        "MM/dd/yyyy HH:mm:ss",
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.None,
                        out releaseDate
                    );
                    if (!isDateParsed)
                    {
                        Log.Warning($"Failed to parse release date from response: {latestRelease["published_at"].Value<string>()}");
                        return (false, null);
                    }

                    // Retrieve the current configuration based on update type
                    EmulatorInfo emulatorInfo = xeniaVersion switch
                    {
                        EmulatorVersion.Canary => ConfigurationManager.AppConfig.XeniaCanary,
                        EmulatorVersion.Netplay => ConfigurationManager.AppConfig.XeniaNetplay,
                        _ => throw new InvalidOperationException("Unexpected build type")
                    };

                    // Check if the release date's match, if they don't, it means new update is available
                    if (releaseDate != emulatorInfo.ReleaseDate)
                    {
                        return (true, latestRelease);
                    }
                    else
                    {
                        return (false, null);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"An error occurred while checking for updates: {ex.Message}");
                return (false, null);
            }
            finally 
            {
                EmulatorInfo emulatorInfo = xeniaVersion switch
                {
                    EmulatorVersion.Canary => ConfigurationManager.AppConfig.XeniaCanary,
                    EmulatorVersion.Netplay => ConfigurationManager.AppConfig.XeniaNetplay,
                    _ => throw new InvalidOperationException("Unexpected build type")
                };
                emulatorInfo.LastUpdateCheckDate = DateTime.Now;
                ConfigurationManager.SaveConfigurationFile();
            }
        }
    }
}
