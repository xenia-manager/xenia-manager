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
                    EmulatorVersion.Canary =>
                        "https://api.github.com/repos/xenia-canary/xenia-canary-releases/releases/latest",
                    EmulatorVersion.Mousehook =>
                        "https://api.github.com/repos/marinesciencedude/xenia-canary-mousehook/releases",
                    EmulatorVersion.Netplay => "https://api.github.com/repos/AdrianCassar/xenia-canary/releases/latest",
                    _ => throw new InvalidOperationException("Unexpected build type")
                };

                // Initialize HttpClient and set headers
                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("User-Agent",
                        "Xenia Manager (https://github.com/xenia-manager/xenia-manager)");
                    client.DefaultRequestHeaders.Add("Accept", "application/vnd.github.v3+json");

                    // Send GET request to GitHub API
                    HttpResponseMessage response;

                    try
                    {
                        response = await client.GetAsync(url);
                    }
                    catch (HttpRequestException)
                    {
                        Log.Error("No internet connection");
                        return (false, null);
                    }

                    // Checking if the response is success
                    if (!response.IsSuccessStatusCode)
                    {
                        return (false, null);
                    }

                    // Read the response
                    string json = await response.Content.ReadAsStringAsync();

                    // Parse the response as JSON Object
                    JObject latestRelease = null;
                    switch (xeniaVersion)
                    {
                        case EmulatorVersion.Canary:
                            // Parsing the JSONObject since it's the latest release
                            latestRelease = JObject.Parse(json);
                            
                            // Retrieve the `tag_name` field
                            string? latestCommitSha = latestRelease["tag_name"]?.ToString();
                            if (string.IsNullOrEmpty(latestCommitSha))
                            {
                                Log.Warning("Couldn't find the tag_name for Xenia Canary latest release");
                                return (false, null);
                            }

                            // Checking if we got proper tag_name
                            if (latestCommitSha == "canary_experimental")
                            {
                                // Need to parse tag_name from title
                                string releaseName = latestRelease["name"]?.ToString();

                                if (!string.IsNullOrEmpty(releaseName))
                                {
                                    // Check if the release name contains an underscore
                                    if (releaseName.Contains("_"))
                                    {
                                        // Extract substring before the first underscore
                                        latestCommitSha = releaseName.Substring(0, releaseName.IndexOf("_"));
                                    }
                                    else if (releaseName.Length == 7)
                                    {
                                        // Assume the format is only commitSha if length is 7
                                        latestCommitSha = releaseName;
                                    }
                                }
                            }
                            
                            Log.Information(latestCommitSha);
                            
                            // Compare versions
                            if (!string.Equals(latestCommitSha, ConfigurationManager.AppConfig.XeniaCanary.Version, StringComparison.OrdinalIgnoreCase))
                            {
                                Log.Information($"Xenia Canary has a new update: {latestCommitSha}");
                                return (true, latestRelease);
                            }
                            break;
                        case EmulatorVersion.Mousehook:
                            // Parse the JSON as an array since we're fetching multiple releases for Mousehook
                            JArray mousehookReleases = JArray.Parse(json);

                            // Sorting release by "published_at" and removing the release with the netplay builds
                            mousehookReleases = new JArray(
                                mousehookReleases
                                    .Where(r => r["target_commitish"]?.ToString().ToLower() == "mousehook")
                                    .OrderByDescending(r => DateTime.Parse(r["published_at"].ToString()))
                            );

                            // Checking if there are any releases
                            if (mousehookReleases.Count == 0)
                            {
                                Log.Warning("Couldn't find the latest release for Xenia Canary");
                                return (false, null);
                            }

                            // Returning latest release
                            latestRelease = (JObject)mousehookReleases[0];
                            
                            // Compare versions
                            if (!string.Equals((string)latestRelease["tag_name"], ConfigurationManager.AppConfig.XeniaMousehook.Version, StringComparison.OrdinalIgnoreCase))
                            {
                                Log.Information($"Xenia Mousehook has a new update: {latestRelease["tag_name"]}");
                                return (true, latestRelease);
                            }
                            break;
                        case EmulatorVersion.Netplay:
                            // For Netplay, just parse the JSON as an object (single latest release)
                            latestRelease = JObject.Parse(json);
                            
                            // Compare versions
                            if (!string.Equals((string)latestRelease["tag_name"], ConfigurationManager.AppConfig.XeniaNetplay.Version, StringComparison.OrdinalIgnoreCase))
                            {
                                Log.Information($"Xenia Netplay has a new update: {(string)latestRelease["tag_name"]}");
                                return (true, latestRelease);
                            }
                            break;
                        default:
                            break;
                    }
                    return (false, null);
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
                    EmulatorVersion.Mousehook => ConfigurationManager.AppConfig.XeniaMousehook,
                    EmulatorVersion.Netplay => ConfigurationManager.AppConfig.XeniaNetplay,
                    _ => throw new InvalidOperationException("Unexpected build type")
                };
                emulatorInfo.LastUpdateCheckDate = DateTime.Now;
                ConfigurationManager.SaveConfigurationFile();
            }
        }
    }
}