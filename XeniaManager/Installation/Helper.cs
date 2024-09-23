using System;
using System.Diagnostics;
using System.Globalization;

// Imported
using Newtonsoft.Json.Linq;
using Serilog;

namespace XeniaManager.Installation
{
    public static partial class InstallationManager
    {
        /// <summary>
        /// Stores the unique identifier for Xenia builds
        /// </summary>
        public static string tagName;

        /// <summary>
        /// Stores release date of the Xenia Build
        /// </summary>
        public static DateTime releaseDate;

        /// <summary>
        /// Function that grabs the download link of the selected build.
        /// </summary>
        /// <param name="url">URL of the builds releases page API</param>
        /// <param name="releaseNumber">Which release we want, by default it's the latest</param>
        /// <param name="assetNumber">What asset we want to grab</param>
        /// <returns>Download URL of the latest release</returns>
        public static async Task<string> DownloadLinkGrabber(string url, int releaseNumber = 0 ,int assetNumber = 0)
        {
            try
            {
                // Grabs the selected release
                JObject release = await Github.GrabRelease(url, releaseNumber);
                if (release == null)
                {
                    Log.Error("No releases found");
                    return null;
                }

                // Grabbing assets from the release
                JArray assets = release["assets"] as JArray;
                if (assets == null)
                {
                    Log.Error("No assets in the github release found");
                    return null;
                }

                // Grabbing Xenia specific asset
                JObject asset = assets[assetNumber] as JObject;
                string assetDownloadURL = asset["browser_download_url"].ToString();
                if (assetDownloadURL != null)
                {
                    Log.Information($"Download URL of the build: {assetDownloadURL}");
                    tagName = (string)release["tag_name"];
                    bool isDateParsed = DateTime.TryParseExact(
                            release["published_at"].Value<string>(),
                            "MM/dd/yyyy HH:mm:ss",
                            CultureInfo.InvariantCulture,
                            DateTimeStyles.None,
                            out releaseDate
                        );

                    if (!isDateParsed)
                    {
                        Log.Warning($"Failed to parse release date from response: {release["published_at"].Value<string>()}");
                    }

                    Log.Information($"Release date of the build: {releaseDate.ToString()}");
                    return assetDownloadURL;
                }
                else
                {
                    Log.Error("No download URL found");
                    return null;
                }
            }
            catch (Exception ex)
            {
                Log.Error($"{ex.Message}\nFull Error:\n{ex}");
                return null;
            }
        }

        /// <summary>
        /// Generates Xenia's configuration file
        /// </summary>
        private static async void GenerateConfigFile(string executableLocation, string configurationFilePath)
        {
            try
            {
                Log.Information("Generating configuration file by launching the emulator");
                Process xenia = new Process();
                xenia.StartInfo.FileName = executableLocation;
                xenia.StartInfo.WorkingDirectory = Path.GetDirectoryName(executableLocation);
                xenia.Start();
                Log.Information("Emulator Launched");
                Log.Information("Waiting for configuration file to be generated");
                while (!File.Exists(configurationFilePath))
                {
                    await Task.Delay(100);
                }
                Log.Information("Configuration file found");
                Log.Information("Closing the emulator");
                xenia.Kill();
                Log.Information("Emulator closed");
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message + "\nFull Error:\n" + ex);
                return;
            }
        }
    }
}
