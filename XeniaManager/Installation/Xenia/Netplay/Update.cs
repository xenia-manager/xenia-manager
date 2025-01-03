using System;
using System.Globalization;
using Newtonsoft.Json.Linq;

// Imported
using Serilog;
using Tomlyn.Model;
using Tomlyn;
using XeniaManager.Downloader;

namespace XeniaManager.Installation
{
    public partial class Xenia
    {
        /// <summary>
        /// Updates the Xenia Netplay to the latest version
        /// </summary>
        /// <param name="latestRelease">Latest release as a JObject gotten from GitHub API</param>
        public async Task NetplayUpdate(JObject latestRelease)
        {
            // Parse release date from response
            bool isDateParsed = DateTime.TryParseExact(
                latestRelease["published_at"].Value<string>(),
                "MM/dd/yyyy HH:mm:ss",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out DateTime releaseDate
            );

            if (!isDateParsed)
            {
                Log.Warning(
                    $"Failed to parse release date from response: {latestRelease["published_at"].Value<string>()}");
            }

            Log.Information($"User chose to update to the new version (Release date: {latestRelease["published_at"]})");

            // Grab the asset for Xenia Netplay
            JArray assets = (JArray)latestRelease["assets"];
            JObject xeniaRelease =
                (JObject)assets.FirstOrDefault(file => file["name"].ToString() == "xenia_canary_netplay.zip");

            // Check if we found the xenia Netplay zip from assets
            if (xeniaRelease == null)
            {
                Log.Error("Couldn't find the download asset");
                return;
            }

            string url = xeniaRelease["browser_download_url"].ToString();
            Log.Information($"Download link for the new Xenia {EmulatorVersion.Netplay} build: {url}");

            // Perform download and extraction
            await DownloadManager.DownloadAndExtractAsync(url,
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Downloads\xenia.zip"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Emulators\Xenia Netplay\"));
            Log.Information(
                $"Download and extraction of the latest Xenia {EmulatorVersion.Netplay} build completed.");

            // Update configuration with the new version details
            ConfigurationManager.AppConfig.XeniaNetplay.Version = (string)latestRelease["tag_name"];
            ConfigurationManager.AppConfig.XeniaNetplay.ReleaseDate = releaseDate;
            ConfigurationManager.AppConfig.XeniaNetplay.LastUpdateCheckDate = DateTime.Now;
            ConfigurationManager.SaveConfigurationFile(); // Save changes
            Log.Information(
                $"Xenia {EmulatorVersion.Netplay} updated to version {ConfigurationManager.AppConfig.XeniaNetplay.Version}");
        }
    }
}