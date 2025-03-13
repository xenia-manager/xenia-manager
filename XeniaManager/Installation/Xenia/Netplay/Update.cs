using System;
using System.Globalization;
using System.Windows;
using System.Windows.Input;
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
            ConfigurationManager.AppConfig.XeniaNetplay.NightlyVersion = null;
            ConfigurationManager.AppConfig.XeniaNetplay.ReleaseDate = releaseDate;
            ConfigurationManager.AppConfig.XeniaNetplay.LastUpdateCheckDate = DateTime.Now;
            ConfigurationManager.SaveConfigurationFile(); // Save changes
            Log.Information(
                $"Xenia {EmulatorVersion.Netplay} updated to version {ConfigurationManager.AppConfig.XeniaNetplay.Version}");
        }

        /// <summary>
        /// Updates Xenia Netplay to latest Nightly build
        /// </summary>
        public async Task<bool> NetplayNightlyUpdate()
        {
            // Urls
            string latestCommitUrl =
                "https://api.github.com/repos/AdrianCassar/xenia-canary/git/refs/heads/netplay_canary_experimental";
            string nightlyReleaseUrl =
                "https://nightly.link/AdrianCassar/xenia-canary/workflows/Windows_build/netplay_canary_experimental/xenia_canary_netplay_windows.zip";

            // Version for this nightly build
            string nigthlyVersion = string.Empty;

            // Grabbing Netplay Nigthly Version
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("User-Agent",
                    "Xenia Manager (https://github.com/xenia-manager/xenia-manager)");
                client.DefaultRequestHeaders.Add("Accept", "application/vnd.github.v3+json");

                HttpResponseMessage response;
                try
                {
                    response = await client.GetAsync(latestCommitUrl);
                    if (!response.IsSuccessStatusCode)
                    {
                        Log.Error("Failed to fetch information about newest Xenia Netplay Nightly release");
                        return false;
                    }

                    string content = await client.GetStringAsync(latestCommitUrl);

                    JObject json = JObject.Parse(content);
                    nigthlyVersion = json["object"]["sha"]?.ToString().Substring(0, 7) ?? null;
                    Log.Information($"Netplay Nightly Version: {nigthlyVersion}");
                }
                catch (HttpRequestException)
                {
                    Log.Error("Failed to fetch information about newest Xenia Netplay Nightly release");
                    return false;
                }
            }

            if (nigthlyVersion == ConfigurationManager.AppConfig.XeniaNetplay.NightlyVersion)
            {
                MessageBoxResult forceUpdate =
                    MessageBox.Show(
                        "The latest version is already installed.\nDo you want to reinstall Xenia Netplay's latest Nightly build?",
                        "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (forceUpdate != MessageBoxResult.Yes)
                {
                    return false;
                }
            }

            // Download and install latest release
            await DownloadManager.DownloadAndExtractAsync(nightlyReleaseUrl,
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Downloads\xenia.zip"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Emulators\Xenia Netplay\"));
            Log.Information(
                $"Download and extraction of the latest Xenia {EmulatorVersion.Netplay} Nightly build completed.");

            ConfigurationManager.AppConfig.XeniaNetplay.NightlyVersion = nigthlyVersion;
            ConfigurationManager.SaveConfigurationFile();
            Mouse.OverrideCursor = null;
            MessageBox.Show("Latest Xenia Netplay Nightly build has been installed.");
            return true;
        }
    }
}