using System;
using System.Globalization;

// Imported
using Newtonsoft.Json.Linq;
using Serilog;

namespace XeniaManager.Installation
{
    public static partial class InstallationManager
    {
        /// <summary>
        /// Checks for updates and if there is a new update, grabs the information about the latest Xenia Manager release
        /// </summary>
        public static async Task<bool> ManagerUpdateChecker()
        {
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("User-Agent", "Xenia Manager (https://github.com/xenia-manager/xenia-manager)");
                client.DefaultRequestHeaders.Add("Accept", "application/vnd.github.v3+json");

                HttpResponseMessage response;
                try
                {
                    response = await client.GetAsync("https://api.github.com/repos/xenia-manager/xenia-manager/releases/latest");
                    if (!response.IsSuccessStatusCode)
                    {
                        Log.Error("Failed to fetch information about newest Xenia Manager release");
                        return false;
                    }
                }
                catch (HttpRequestException)
                {
                    Log.Error("Failed to fetch information about newest Xenia Manager release");
                    return false;
                }
                
                // Fetching information about the latest release
                string json = await response.Content.ReadAsStringAsync();
                JObject latestRelease = JObject.Parse(json);
                string version = (string)latestRelease["tag_name"];
                
                // Parse the release date from response
                DateTime releaseDate;
                bool isDateParsed = DateTime.TryParseExact(
                    latestRelease["published_at"].Value<string>(),
                    "MM/dd/yyyy HH:mm:ss",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out releaseDate
                );
                
                if (version != ConfigurationManager.AppConfig.Manager.Version)
                {
                    LatestXeniaManagerRelease = new UpdateInfo();
                    LatestXeniaManagerRelease.Version = version;
                    if (isDateParsed)
                    {
                        LatestXeniaManagerRelease.ReleaseDate = releaseDate;
                    }
                    else
                    {
                        Log.Warning($"Failed to parse release date from response: {latestRelease["published_at"].Value<string>()}");
                        LatestXeniaManagerRelease.ReleaseDate = DateTime.Now;
                    }
                    LatestXeniaManagerRelease.UpdateAvailable = false;
                    LatestXeniaManagerRelease.LastUpdateCheckDate = DateTime.Now;
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }
    }
}