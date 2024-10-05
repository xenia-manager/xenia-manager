using System;
using System.Globalization;


// Imported
using Newtonsoft.Json.Linq;
using Serilog;
using XeniaManager.Downloader;

namespace XeniaManager.Installation
{
    public partial class Xenia
    {
        /// <summary>
        /// Updates the Xenia Canary to the latest version
        /// </summary>
        /// <param name="latestRelease">Latest release as a JObject gotten from Github API</param>
        public async Task CanaryUpdate(JObject latestRelease)
        {
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
            }

            Log.Information($"User chose to update to the new version (Release date: {latestRelease["published_at"]})");

            // Grab the asset for Xenia Canary
            JArray assets = (JArray)latestRelease["assets"];
            JObject xeniaRelease = (JObject)assets.FirstOrDefault(file => file["name"].ToString() == "xenia_canary.zip");

            // Check if we found the xenia canary zip from assets
            if (xeniaRelease == null)
            {
                Log.Error("Couldn't find the download asset");
                return;
            }
            string url = xeniaRelease["browser_download_url"].ToString();
            Log.Information($"Download link for the new Xenia {EmulatorVersion.Canary} build: {url}");

            // Perform download and extraction
            await DownloadManager.DownloadAndExtractAsync(url, Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Downloads\xenia.zip"), Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Emulators\Xenia Canary\"));
            Log.Information($"Download and extraction of the latest Xenia {EmulatorVersion.Canary} build completed.");

            // Update configuration with the new version details
            ConfigurationManager.AppConfig.XeniaCanary.Version = (string)latestRelease["tag_name"];
            ConfigurationManager.AppConfig.XeniaCanary.ReleaseDate = releaseDate;
            ConfigurationManager.AppConfig.XeniaCanary.LastUpdateCheckDate = DateTime.Now;
            ConfigurationManager.SaveConfigurationFile(); // Save changes
            Log.Information($"Xenia {EmulatorVersion.Canary} updated to version {ConfigurationManager.AppConfig.XeniaCanary.Version}");
        }
    }
}
